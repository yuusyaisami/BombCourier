using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BC.Rendering
{
    public sealed class ToyDioramaPostProcessFeature : ScriptableRendererFeature
    {
        private const string DefaultBlueNoiseResourcePath = "ToyDioramaBlueNoise";

        [SerializeField] private Shader compositeShader;
        [SerializeField] private Shader bloomShader;
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingPostProcessing;
        [SerializeField] private bool sceneViewEnabled;
        [SerializeField] private bool forceLowQualityTier;
        [SerializeField] private ToyDioramaPostProcessPreset selectedPreset;
        [SerializeField] private ToyDioramaPostProcessSettings settings = new ToyDioramaPostProcessSettings();

        private Material preBloomCompositeMaterial;
        private Material finalCompositeMaterial;
        private Material bloomMaterial;
        private ToyDioramaCompositePass preBloomPass;
        private ToyDioramaBloomPass bloomPass;
        private ToyDioramaCompositePass finalCompositePass;
        private Texture2D defaultBlueNoiseTexture;
        private bool hasLoggedRuntimeResourceError;
        private readonly ToyDioramaPostProcessSettings resolvedRuntimeSettings = new ToyDioramaPostProcessSettings();

        public ToyDioramaPostProcessSettings Settings => settings;

        public bool SceneViewEnabled
        {
            get => sceneViewEnabled;
            set => sceneViewEnabled = value;
        }

        public ToyDioramaPostProcessPreset SelectedPreset
        {
            get => selectedPreset;
            set => selectedPreset = value;
        }

        public bool ForceLowQualityTier
        {
            get => forceLowQualityTier;
            set => forceLowQualityTier = value;
        }

        public int LastRecordedPreBloomRasterPassCount => preBloomPass?.LastRecordedRasterPassCount ?? 0;

        public int LastRecordedBloomRasterPassCount => bloomPass?.LastRecordedRasterPassCount ?? 0;

        public int LastRecordedFinalCompositeRasterPassCount => finalCompositePass?.LastRecordedRasterPassCount ?? 0;

        public int LastRecordedTotalRasterPassCount =>
            LastRecordedPreBloomRasterPassCount +
            LastRecordedBloomRasterPassCount +
            LastRecordedFinalCompositeRasterPassCount;

        public bool TryGetRuntimeResourceError(out string errorMessage)
        {
            if (HasValidRuntimeResources())
            {
                errorMessage = null;
                return false;
            }

            StringBuilder builder = new StringBuilder("ToyDiorama runtime resources are incomplete.");

            AppendMissingRuntimeResource(builder, compositeShader == null, " Missing composite shader.");
            AppendMissingRuntimeResource(builder, bloomShader == null, " Missing bloom shader.");
            AppendMissingRuntimeResource(builder, preBloomCompositeMaterial == null, " Missing pre-bloom composite material.");
            AppendMissingRuntimeResource(builder, finalCompositeMaterial == null, " Missing final composite material.");
            AppendMissingRuntimeResource(builder, bloomMaterial == null, " Missing bloom material.");
            AppendMissingRuntimeResource(builder, preBloomPass == null, " Missing pre-bloom pass instance.");
            AppendMissingRuntimeResource(builder, bloomPass == null, " Missing bloom pass instance.");
            AppendMissingRuntimeResource(builder, finalCompositePass == null, " Missing final composite pass instance.");
            AppendMissingRuntimeResource(builder, defaultBlueNoiseTexture == null, " Missing default blue-noise texture resource.");

            errorMessage = builder.ToString();
            return true;
        }

        public ToyDioramaQualityTier GetResolvedQualityTier()
        {
            return ResolveRuntimeSettings().QualityTier;
        }

        public bool ShouldApplyToCameraType(CameraType cameraType)
        {
            switch (cameraType)
            {
                case CameraType.Game:
                    return true;

                case CameraType.SceneView:
                    return sceneViewEnabled;

                default:
                    return false;
            }
        }

        public string GetCameraPolicySummary()
        {
            return sceneViewEnabled
                ? "Applies to Game Camera and Scene View. Preview, Reflection, and other utility cameras stay off."
                : "Applies to Game Camera only. Scene View, Preview, Reflection, and other utility cameras stay off.";
        }

        public void ApplySelectedPreset()
        {
            ApplyPreset(selectedPreset);
        }

        public void ApplyPreset(ToyDioramaPostProcessPreset preset)
        {
            if (preset == null)
            {
                return;
            }

            settings ??= new ToyDioramaPostProcessSettings();
            selectedPreset = preset;
            settings.ApplyPreset(preset);
        }

        public override void Create()
        {
            compositeShader ??= Shader.Find("BC/PostProcess/ToyDioramaComposite");
            bloomShader ??= Shader.Find("Hidden/BC/PostProcess/ToyDioramaBloom");
            defaultBlueNoiseTexture ??= Resources.Load<Texture2D>(DefaultBlueNoiseResourcePath);

            if (compositeShader != null && (preBloomCompositeMaterial == null || preBloomCompositeMaterial.shader != compositeShader))
            {
                CoreUtils.Destroy(preBloomCompositeMaterial);
                preBloomCompositeMaterial = CoreUtils.CreateEngineMaterial(compositeShader);
            }

            if (compositeShader != null && (finalCompositeMaterial == null || finalCompositeMaterial.shader != compositeShader))
            {
                CoreUtils.Destroy(finalCompositeMaterial);
                finalCompositeMaterial = CoreUtils.CreateEngineMaterial(compositeShader);
            }

            if (bloomShader != null && (bloomMaterial == null || bloomMaterial.shader != bloomShader))
            {
                CoreUtils.Destroy(bloomMaterial);
                bloomMaterial = CoreUtils.CreateEngineMaterial(bloomShader);
            }

            preBloomPass ??= new ToyDioramaCompositePass(ToyDioramaCompositePass.CompositeStage.PreBloom);
            bloomPass ??= new ToyDioramaBloomPass();
            finalCompositePass ??= new ToyDioramaCompositePass(ToyDioramaCompositePass.CompositeStage.FinalComposite);

            if (HasValidRuntimeResources())
            {
                hasLoggedRuntimeResourceError = false;
            }
            else
            {
                ReportRuntimeResourceErrorOnce();
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            defaultBlueNoiseTexture ??= Resources.Load<Texture2D>(DefaultBlueNoiseResourcePath);
            ResetRecordedRasterPassCounts();

            if (!HasValidRuntimeResources())
            {
                Create();

                if (!HasValidRuntimeResources())
                {
                    ReportRuntimeResourceErrorOnce();
                    return;
                }
            }

            ToyDioramaPostProcessSettings runtimeSettings = ResolveRuntimeSettings();

            if (!runtimeSettings.Enabled)
            {
                return;
            }

            if (!ShouldApplyToCameraType(renderingData.cameraData.cameraType))
            {
                return;
            }
            bool requiresBloomPass = runtimeSettings.RequiresBloomPass();
            bool requiresFinalCompositePass = runtimeSettings.RequiresFinalCompositePass();

            preBloomPass.renderPassEvent = passEvent;
            preBloomPass.Setup(
                preBloomCompositeMaterial,
                runtimeSettings,
                defaultBlueNoiseTexture,
                !requiresFinalCompositePass);

            renderer.EnqueuePass(preBloomPass);

            if (requiresBloomPass)
            {
                bloomPass.renderPassEvent = passEvent;
                bloomPass.Setup(bloomMaterial, runtimeSettings);
                renderer.EnqueuePass(bloomPass);
            }

            if (requiresFinalCompositePass)
            {
                finalCompositePass.renderPassEvent = passEvent;
                finalCompositePass.Setup(finalCompositeMaterial, runtimeSettings, defaultBlueNoiseTexture, false);
                renderer.EnqueuePass(finalCompositePass);
            }
        }

        private ToyDioramaPostProcessSettings ResolveRuntimeSettings()
        {
            settings ??= new ToyDioramaPostProcessSettings();
            resolvedRuntimeSettings.CopyFrom(settings);

            if (forceLowQualityTier)
            {
                resolvedRuntimeSettings.QualityTier = ToyDioramaQualityTier.Low;
            }

            return resolvedRuntimeSettings;
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(preBloomCompositeMaterial);
            CoreUtils.Destroy(finalCompositeMaterial);
            CoreUtils.Destroy(bloomMaterial);
            preBloomCompositeMaterial = null;
            finalCompositeMaterial = null;
            bloomMaterial = null;
            preBloomPass = null;
            bloomPass = null;
            finalCompositePass = null;
            defaultBlueNoiseTexture = null;
            hasLoggedRuntimeResourceError = false;
        }

        private bool HasValidRuntimeResources()
        {
            return compositeShader != null &&
                bloomShader != null &&
                preBloomCompositeMaterial != null &&
                finalCompositeMaterial != null &&
                bloomMaterial != null &&
                preBloomPass != null &&
                bloomPass != null &&
                finalCompositePass != null &&
                defaultBlueNoiseTexture != null;
        }

        private void ReportRuntimeResourceErrorOnce()
        {
            if (hasLoggedRuntimeResourceError || !TryGetRuntimeResourceError(out string errorMessage))
            {
                return;
            }

            Debug.LogError(errorMessage, this);
            hasLoggedRuntimeResourceError = true;
        }

        private void ResetRecordedRasterPassCounts()
        {
            preBloomPass?.ResetRecordedRasterPassCount();
            bloomPass?.ResetRecordedRasterPassCount();
            finalCompositePass?.ResetRecordedRasterPassCount();
        }

        private static void AppendMissingRuntimeResource(StringBuilder builder, bool isMissing, string message)
        {
            if (isMissing)
            {
                builder.Append(message);
            }
        }
    }
}
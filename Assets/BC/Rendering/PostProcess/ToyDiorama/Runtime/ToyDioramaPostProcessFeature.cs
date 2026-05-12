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
        [SerializeField] private ToyDioramaPostProcessPreset selectedPreset;
        [SerializeField] private ToyDioramaPostProcessSettings settings = new ToyDioramaPostProcessSettings();

        private Material preBloomCompositeMaterial;
        private Material finalCompositeMaterial;
        private Material bloomMaterial;
        private ToyDioramaCompositePass preBloomPass;
        private ToyDioramaBloomPass bloomPass;
        private ToyDioramaCompositePass finalCompositePass;
        private ToyDioramaRenderTargets renderTargets;
        private Texture2D defaultBlueNoiseTexture;

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

            renderTargets ??= new ToyDioramaRenderTargets();
            preBloomPass ??= new ToyDioramaCompositePass(ToyDioramaCompositePass.CompositeStage.PreBloom);
            bloomPass ??= new ToyDioramaBloomPass();
            finalCompositePass ??= new ToyDioramaCompositePass(ToyDioramaCompositePass.CompositeStage.FinalComposite);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings == null ||
                !settings.Enabled ||
                preBloomCompositeMaterial == null ||
                finalCompositeMaterial == null ||
                bloomMaterial == null ||
                preBloomPass == null ||
                bloomPass == null ||
                finalCompositePass == null ||
                renderTargets == null)
            {
                return;
            }

            if (!ShouldApplyToCameraType(renderingData.cameraData.cameraType))
            {
                return;
            }

            defaultBlueNoiseTexture ??= Resources.Load<Texture2D>(DefaultBlueNoiseResourcePath);

            bool requiresBloomPass = settings.RequiresBloomPass();
            bool requiresFinalCompositePass = settings.RequiresFinalCompositePass();

            renderTargets.Reset();

            preBloomPass.renderPassEvent = passEvent;
            preBloomPass.Setup(
                preBloomCompositeMaterial,
                settings,
                defaultBlueNoiseTexture,
                renderTargets,
                !requiresFinalCompositePass);

            renderer.EnqueuePass(preBloomPass);

            if (requiresBloomPass)
            {
                bloomPass.renderPassEvent = passEvent;
                bloomPass.Setup(bloomMaterial, settings, renderTargets);
                renderer.EnqueuePass(bloomPass);
            }

            if (requiresFinalCompositePass)
            {
                finalCompositePass.renderPassEvent = passEvent;
                finalCompositePass.Setup(finalCompositeMaterial, settings, defaultBlueNoiseTexture, renderTargets, false);
                renderer.EnqueuePass(finalCompositePass);
            }
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
            renderTargets = null;
            defaultBlueNoiseTexture = null;
        }
    }
}
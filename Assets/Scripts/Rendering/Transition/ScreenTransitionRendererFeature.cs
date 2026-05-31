using BC.Rendering.Transition;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BC.Rendering
{
    public sealed class ScreenTransitionRendererFeature : ScriptableRendererFeature
    {
        [Header("Material")]
        [SerializeField] private Material transitionMaterial;

        [Header("Pass")]
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingPostProcessing;

        private ScreenTransitionPass pass;

        public override void Create()
        {
            pass = new ScreenTransitionPass
            {
                renderPassEvent = passEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (transitionMaterial == null)
                return;

            ScreenTransitionServiceMB service = ScreenTransitionServiceMB.Instance;
            if (service == null)
                return;

            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
                return;

            pass.Setup(transitionMaterial, service);
            renderer.EnqueuePass(pass);
        }

        private sealed class ScreenTransitionPass : ScriptableRenderPass
        {
            private const string CapturePassName = "Screen Transition Capture From";
            private const string CompositePassName = "Screen Transition Composite";

            private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
            private static readonly int TextureFromId = Shader.PropertyToID("_TextureFrom");
            private static readonly int ProgressId = Shader.PropertyToID("_Progress");
            private static readonly int ModeId = Shader.PropertyToID("_Mode");
            private static readonly int FeatherId = Shader.PropertyToID("_Feather");
            private static readonly int DirectionId = Shader.PropertyToID("_Direction");
            private static readonly int CenterId = Shader.PropertyToID("_Center");
            private static readonly int AspectId = Shader.PropertyToID("_Aspect");
            private static readonly int NoiseTexId = Shader.PropertyToID("_NoiseTex");
            private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
            private static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
            private static readonly int SeedId = Shader.PropertyToID("_Seed");
            private static readonly int UseExplicitToId = Shader.PropertyToID("_UseExplicitTo");
            private static readonly int FromUvScaleOffsetId = Shader.PropertyToID("_FromUvScaleOffset");
            private static readonly int ToUvScaleOffsetId = Shader.PropertyToID("_ToUvScaleOffset");

            private Material material;
            private ScreenTransitionServiceMB service;

            private sealed class CapturePassData
            {
                internal TextureHandle sourceColorTexture;
                internal Material material;
            }

            private sealed class CompositePassData
            {
                internal TextureHandle sourceColorTexture;
                internal TextureHandle fromTexture;
                internal Material material;
                internal float progress;
                internal int mode;
                internal float feather;
                internal Vector2 direction;
                internal Vector2 center;
                internal float aspect;
                internal Texture noiseTexture;
                internal float noiseScale;
                internal float noiseStrength;
                internal float seed;
            }

            public void Setup(Material transitionMaterial, ScreenTransitionServiceMB transitionService)
            {
                material = transitionMaterial;
                service = transitionService;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null || service == null)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                    return;

                TextureHandle activeColorTexture = resourceData.activeColorTexture;
                if (!activeColorTexture.IsValid())
                    return;

                bool needsFromTexture = service.CaptureRequested || service.IsTransitioning;

                if (needsFromTexture)
                {
                    TextureDesc colorDesc = renderGraph.GetTextureDesc(activeColorTexture);
                    if (!service.EnsureFromTexture(colorDesc.width, colorDesc.height, colorDesc.format))
                        return;
                }

                if (service.CaptureRequested)
                {
                    RTHandle fromHandle = service.FromTextureHandle;
                    if (fromHandle == null)
                        return;

                    TextureHandle fromTexture = renderGraph.ImportTexture(fromHandle);
                    RecordCapturePass(renderGraph, activeColorTexture, fromTexture);
                    service.NotifyFromCaptureCompleted();
                }

                if (!service.IsTransitioning)
                    return;

                RTHandle fromRtHandle = service.FromTextureHandle;
                if (!service.HasCapturedFromTexture || fromRtHandle == null)
                {
                    service.ReportMissingFromTextureIfNeeded();
                    return;
                }

                TextureHandle importedFrom = renderGraph.ImportTexture(fromRtHandle);
                TextureHandle compositeOutput = RecordCompositePass(
                    renderGraph,
                    activeColorTexture,
                    importedFrom,
                    service.Progress,
                    (int)service.ActiveMode,
                    service.ActiveFeather,
                    service.ActiveDirection,
                    service.ActiveCenter,
                    (float)renderGraph.GetTextureDesc(activeColorTexture).width / Mathf.Max(1f, renderGraph.GetTextureDesc(activeColorTexture).height),
                    service.ActiveNoiseTexture,
                    service.ActiveNoiseScale,
                    service.ActiveNoiseStrength,
                    service.ActiveSeed);

                resourceData.cameraColor = compositeOutput;
            }

            private void RecordCapturePass(
                RenderGraph renderGraph,
                TextureHandle sourceColorTexture,
                TextureHandle destinationFromTexture)
            {
                using var builder = renderGraph.AddRasterRenderPass<CapturePassData>(CapturePassName, out CapturePassData data);
                data.sourceColorTexture = sourceColorTexture;
                data.material = material;

                builder.UseTexture(data.sourceColorTexture, AccessFlags.Read);
                builder.SetRenderAttachment(destinationFromTexture, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (CapturePassData d, RasterGraphContext ctx) =>
                {
                    d.material.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, d.material, 0, MeshTopology.Triangles, 3);
                });
            }

            private TextureHandle RecordCompositePass(
                RenderGraph renderGraph,
                TextureHandle sourceColorTexture,
                TextureHandle fromTexture,
                float progress,
                int mode,
                float feather,
                Vector2 direction,
                Vector2 center,
                float aspect,
                Texture noiseTexture,
                float noiseScale,
                float noiseStrength,
                float seed)
            {
                TextureDesc destinationDesc = renderGraph.GetTextureDesc(sourceColorTexture);
                destinationDesc.name = CompositePassName;
                destinationDesc.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

                using var builder = renderGraph.AddRasterRenderPass<CompositePassData>(CompositePassName, out CompositePassData data);
                data.sourceColorTexture = sourceColorTexture;
                data.fromTexture = fromTexture;
                data.material = material;
                data.progress = progress;
                data.mode = mode;
                data.feather = feather;
                data.direction = direction;
                data.center = center;
                data.aspect = aspect;
                data.noiseTexture = noiseTexture;
                data.noiseScale = noiseScale;
                data.noiseStrength = noiseStrength;
                data.seed = seed;

                builder.UseTexture(data.sourceColorTexture, AccessFlags.Read);
                builder.UseTexture(data.fromTexture, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (CompositePassData d, RasterGraphContext ctx) =>
                {
                    d.material.SetTexture(TextureFromId, d.fromTexture);
                    d.material.SetFloat(ProgressId, d.progress);
                    d.material.SetInt(ModeId, d.mode);
                    d.material.SetFloat(UseExplicitToId, 0f);
                    d.material.SetVector(FromUvScaleOffsetId, new Vector4(1f, 1f, 0f, 0f));
                    d.material.SetVector(ToUvScaleOffsetId, new Vector4(1f, 1f, 0f, 0f));
                    d.material.SetFloat(FeatherId, d.feather);
                    d.material.SetVector(DirectionId, d.direction);
                    d.material.SetVector(CenterId, d.center);
                    d.material.SetFloat(AspectId, d.aspect);
                    d.material.SetTexture(NoiseTexId, d.noiseTexture);
                    d.material.SetFloat(NoiseScaleId, d.noiseScale);
                    d.material.SetFloat(NoiseStrengthId, d.noiseStrength);
                    d.material.SetFloat(SeedId, d.seed);
                    d.material.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, d.material, 1, MeshTopology.Triangles, 3);
                });

                return destination;
            }
        }
    }
}

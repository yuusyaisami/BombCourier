using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BC.Rendering
{
    public sealed class ToyDioramaCompositePass : ScriptableRenderPass
    {
        public enum CompositeStage
        {
            PreBloom,
            FinalComposite
        }

        private const string PreBloomPassName = "Toy Diorama Pre-Bloom";
        private const string FinalCompositePassName = "Toy Diorama Final Composite";
        private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
        private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
        private static readonly int BloomTextureId = Shader.PropertyToID("_ToyDioramaBloomTex");
        private static readonly MaterialPropertyBlock SharedPropertyBlock = new MaterialPropertyBlock();

        private readonly CompositeStage stage;
        private Material material;
        private ToyDioramaPostProcessSettings settings;
        private Texture2D fallbackBlueNoiseTexture;
        private bool presentToCameraColor;

        public int LastRecordedRasterPassCount { get; private set; }

        public ToyDioramaCompositePass(CompositeStage stage)
        {
            this.stage = stage;
        }

        public void Setup(
            Material material,
            ToyDioramaPostProcessSettings settings,
            Texture2D fallbackBlueNoiseTexture,
            bool presentToCameraColor)
        {
            this.material = material;
            this.settings = settings;
            this.fallbackBlueNoiseTexture = fallbackBlueNoiseTexture;
            this.presentToCameraColor = presentToCameraColor;
            requiresIntermediateTexture = true;
            ConfigureInput(stage == CompositeStage.PreBloom && NeedsDepthInput(settings)
                ? ScriptableRenderPassInput.Depth
                : ScriptableRenderPassInput.None);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            LastRecordedRasterPassCount = 0;

            if (material == null || settings == null || !settings.Enabled)
            {
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            ToyDioramaRenderTargets renderTargets = frameData.GetOrCreate<ToyDioramaRenderTargets>();

            if (resourceData.isActiveTargetBackBuffer)
            {
                return;
            }

            settings.ApplyToMaterial(material, fallbackBlueNoiseTexture);

            if (stage == CompositeStage.PreBloom)
            {
                RecordPreBloomPass(renderGraph, resourceData, renderTargets);
                return;
            }

            RecordFinalCompositePass(renderGraph, resourceData, renderTargets);
        }

        private void RecordPreBloomPass(
            RenderGraph renderGraph,
            UniversalResourceData resourceData,
            ToyDioramaRenderTargets renderTargets)
        {
            bool depthInputRequested = NeedsDepthInput(settings);
            TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
            bool depthAvailable = depthInputRequested && cameraDepthTexture.IsValid();

            material.SetFloat(ToyDioramaPostProcessSettings.ShaderIds.DepthAvailable, depthAvailable ? 1f : 0f);

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = "_ToyDioramaPreBloomColor";
            destinationDesc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PreBloomPassName, out PassData passData))
            {
                passData.sourceTexture = source;
                passData.material = material;
                passData.passIndex = 0;

                builder.UseTexture(passData.sourceTexture, AccessFlags.Read);

                if (depthAvailable)
                {
                    builder.UseTexture(cameraDepthTexture, AccessFlags.Read);
                }

                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data.sourceTexture, data.material, data.passIndex);
                });
            }

            LastRecordedRasterPassCount = 1;

            renderTargets.PreBloomColor = destination;

            if (presentToCameraColor)
            {
                resourceData.cameraColor = destination;
            }
        }

        private void RecordFinalCompositePass(
            RenderGraph renderGraph,
            UniversalResourceData resourceData,
            ToyDioramaRenderTargets renderTargets)
        {
            if (!renderTargets.PreBloomColor.IsValid())
            {
                return;
            }

            TextureHandle source = renderTargets.PreBloomColor;
            bool hasBloomTexture = renderTargets.BloomColor.IsValid();
            TextureHandle bloom = hasBloomTexture ? renderTargets.BloomColor : TextureHandle.nullHandle;

            TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = "_ToyDioramaCompositeColor";
            destinationDesc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(FinalCompositePassName, out PassData passData))
            {
                passData.sourceTexture = source;
                passData.bloomTexture = bloom;
                passData.hasBloomTexture = hasBloomTexture;
                passData.material = material;
                passData.passIndex = 1;

                builder.UseTexture(passData.sourceTexture, AccessFlags.Read);

                if (passData.hasBloomTexture)
                {
                    builder.UseTexture(passData.bloomTexture, AccessFlags.Read);
                }

                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    if (data.hasBloomTexture)
                    {
                        ExecutePass(context.cmd, data.sourceTexture, data.bloomTexture, data.material, data.passIndex);
                        return;
                    }

                    ExecutePass(context.cmd, data.sourceTexture, data.material, data.passIndex);
                });
            }

            LastRecordedRasterPassCount = 1;

            resourceData.cameraColor = destination;
        }

        internal void ResetRecordedRasterPassCount()
        {
            LastRecordedRasterPassCount = 0;
        }

        private static bool NeedsDepthInput(ToyDioramaPostProcessSettings settings)
        {
            return settings != null && settings.RequiresDepthTexture();
        }

        private static void ExecutePass(
            RasterCommandBuffer commandBuffer,
            RTHandle sourceTexture,
            Material material,
            int passIndex)
        {
            ExecutePass(commandBuffer, sourceTexture, null, material, passIndex);
        }

        private static void ExecutePass(
            RasterCommandBuffer commandBuffer,
            RTHandle sourceTexture,
            RTHandle bloomTexture,
            Material material,
            int passIndex)
        {
            SharedPropertyBlock.Clear();
            SharedPropertyBlock.SetTexture(BlitTextureId, sourceTexture);
            SharedPropertyBlock.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));

            if (bloomTexture != null)
            {
                SharedPropertyBlock.SetTexture(BloomTextureId, bloomTexture);
            }

            commandBuffer.DrawProcedural(
                Matrix4x4.identity,
                material,
                passIndex,
                MeshTopology.Triangles,
                3,
                1,
                SharedPropertyBlock);
        }

        private sealed class PassData
        {
            internal TextureHandle sourceTexture;
            internal TextureHandle bloomTexture;
            internal bool hasBloomTexture;
            internal Material material;
            internal int passIndex;
        }
    }
}
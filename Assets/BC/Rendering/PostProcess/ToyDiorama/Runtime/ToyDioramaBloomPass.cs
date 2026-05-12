using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BC.Rendering
{
    public sealed class ToyDioramaBloomPass : ScriptableRenderPass
    {
        private const string PrefilterPassName = "Toy Diorama Bloom Prefilter";
        private const string BlurHorizontalPassName = "Toy Diorama Bloom Blur Horizontal";
        private const string BlurVerticalPassName = "Toy Diorama Bloom Blur Vertical";
        private const string CompositePassName = "Toy Diorama Bloom Composite";

        private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
        private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
        private static readonly int BloomSourceTextureId = Shader.PropertyToID("_ToyDioramaBloomSourceTex");
        private static readonly int SourceTexelSizeId = Shader.PropertyToID("_ToyDioramaSourceTexelSize");
        private static readonly MaterialPropertyBlock SharedPropertyBlock = new MaterialPropertyBlock();

        private Material material;
        private ToyDioramaPostProcessSettings settings;

        public int LastRecordedRasterPassCount { get; private set; }

        public void Setup(Material material, ToyDioramaPostProcessSettings settings)
        {
            this.material = material;
            this.settings = settings;
            requiresIntermediateTexture = true;
            ConfigureInput(ScriptableRenderPassInput.None);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            LastRecordedRasterPassCount = 0;

            if (material == null ||
                settings == null ||
                !settings.Enabled ||
                !settings.RequiresBloomPass())
            {
                return;
            }

            ToyDioramaRenderTargets renderTargets = frameData.GetOrCreate<ToyDioramaRenderTargets>();

            if (!renderTargets.PreBloomColor.IsValid())
            {
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
            {
                return;
            }

            settings.ApplyBloomToMaterial(material);

            ToyDioramaDebugView resolvedDebugView = settings.GetResolvedDebugView();

            TextureHandle source = renderTargets.PreBloomColor;
            TextureDesc halfResolutionDesc = renderGraph.GetTextureDesc(source);
            int downsampleDivisor = Mathf.Max(1, settings.GetBloomDownsampleDivisor());
            int blurPassPairCount = Mathf.Max(1, settings.GetBloomBlurPassPairCount());
            halfResolutionDesc.width = Mathf.Max(1, halfResolutionDesc.width / downsampleDivisor);
            halfResolutionDesc.height = Mathf.Max(1, halfResolutionDesc.height / downsampleDivisor);
            halfResolutionDesc.clearBuffer = false;
            halfResolutionDesc.name = "_ToyDioramaBloomPrefilter";

            TextureHandle prefilter = renderGraph.CreateTexture(halfResolutionDesc);
            RecordBloomPass(renderGraph, PrefilterPassName, source, TextureHandle.nullHandle, prefilter, 0);

            if (resolvedDebugView == ToyDioramaDebugView.BloomPrefilter)
            {
                renderTargets.BloomColor = prefilter;
                return;
            }

            TextureHandle blurred = prefilter;

            for (int blurPairIndex = 0; blurPairIndex < blurPassPairCount; blurPairIndex++)
            {
                TextureDesc blurDesc = halfResolutionDesc;
                blurDesc.name = $"_ToyDioramaBloomBlurHorizontal{blurPairIndex}";
                TextureHandle blurHorizontal = renderGraph.CreateTexture(blurDesc);
                RecordBloomPass(renderGraph, BlurHorizontalPassName, blurred, TextureHandle.nullHandle, blurHorizontal, 1);

                blurDesc.name = $"_ToyDioramaBloomBlurVertical{blurPairIndex}";
                TextureHandle blurVertical = renderGraph.CreateTexture(blurDesc);
                RecordBloomPass(renderGraph, BlurVerticalPassName, blurHorizontal, TextureHandle.nullHandle, blurVertical, 2);
                blurred = blurVertical;
            }

            if (resolvedDebugView == ToyDioramaDebugView.BloomBlur)
            {
                renderTargets.BloomColor = blurred;
                return;
            }

            TextureDesc compositeDesc = halfResolutionDesc;
            compositeDesc.name = "_ToyDioramaBloomComposite";
            TextureHandle composite = renderGraph.CreateTexture(compositeDesc);
            RecordBloomPass(renderGraph, CompositePassName, blurred, source, composite, 3);

            renderTargets.BloomColor = composite;
        }

        private void RecordBloomPass(
            RenderGraph renderGraph,
            string passName,
            TextureHandle source,
            TextureHandle secondary,
            TextureHandle destination,
            int passIndex)
        {
            TextureDesc sourceDesc = renderGraph.GetTextureDesc(source);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out PassData passData))
            {
                passData.sourceTexture = source;
                passData.secondaryTexture = secondary;
                passData.material = material;
                passData.passIndex = passIndex;
                passData.sourceTexelSize = new Vector4(
                    1f / Mathf.Max(1, sourceDesc.width),
                    1f / Mathf.Max(1, sourceDesc.height),
                    sourceDesc.width,
                    sourceDesc.height);

                builder.UseTexture(passData.sourceTexture, AccessFlags.Read);

                if (passData.secondaryTexture.IsValid())
                {
                    builder.UseTexture(passData.secondaryTexture, AccessFlags.Read);
                }

                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(
                        context.cmd,
                        data.sourceTexture,
                        data.secondaryTexture,
                        data.material,
                        data.passIndex,
                        data.sourceTexelSize);
                });
            }

            LastRecordedRasterPassCount++;
        }

        internal void ResetRecordedRasterPassCount()
        {
            LastRecordedRasterPassCount = 0;
        }

        private static void ExecutePass(
            RasterCommandBuffer commandBuffer,
            RTHandle sourceTexture,
            RTHandle secondaryTexture,
            Material material,
            int passIndex,
            Vector4 sourceTexelSize)
        {
            SharedPropertyBlock.Clear();
            SharedPropertyBlock.SetTexture(BlitTextureId, sourceTexture);
            SharedPropertyBlock.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
            SharedPropertyBlock.SetVector(SourceTexelSizeId, sourceTexelSize);

            if (secondaryTexture != null)
            {
                SharedPropertyBlock.SetTexture(BloomSourceTextureId, secondaryTexture);
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
            internal TextureHandle secondaryTexture;
            internal Material material;
            internal int passIndex;
            internal Vector4 sourceTexelSize;
        }
    }
}
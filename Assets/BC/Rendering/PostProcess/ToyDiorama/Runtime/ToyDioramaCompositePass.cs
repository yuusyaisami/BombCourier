using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace BC.Rendering
{
    public sealed class ToyDioramaCompositePass : ScriptableRenderPass
    {
        private const string PassName = "Toy Diorama Composite";

        private Material material;
        private ToyDioramaPostProcessSettings settings;

        public void Setup(Material material, ToyDioramaPostProcessSettings settings)
        {
            this.material = material;
            this.settings = settings;
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null || settings == null || !settings.Enabled)
            {
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
            {
                return;
            }

            settings.ApplyToMaterial(material);

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = "_ToyDioramaCompositeColor";
            destinationDesc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

            RenderGraphUtils.BlitMaterialParameters blitParameters = new RenderGraphUtils.BlitMaterialParameters(
                source,
                destination,
                material,
                0);

            renderGraph.AddBlitPass(blitParameters, PassName);
            resourceData.cameraColor = destination;
        }
    }
}
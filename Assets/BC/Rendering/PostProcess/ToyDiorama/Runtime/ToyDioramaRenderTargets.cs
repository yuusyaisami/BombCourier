using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace BC.Rendering
{
    public sealed class ToyDioramaRenderTargets : ContextItem
    {
        public TextureHandle PreBloomColor { get; set; } = TextureHandle.nullHandle;

        public TextureHandle BloomColor { get; set; } = TextureHandle.nullHandle;

        public override void Reset()
        {
            PreBloomColor = TextureHandle.nullHandle;
            BloomColor = TextureHandle.nullHandle;
        }
    }
}
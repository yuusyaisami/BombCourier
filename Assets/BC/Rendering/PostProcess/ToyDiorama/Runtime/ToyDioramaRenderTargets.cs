using UnityEngine.Rendering.RenderGraphModule;

namespace BC.Rendering
{
    public sealed class ToyDioramaRenderTargets
    {
        public TextureHandle PreBloomColor { get; set; } = TextureHandle.nullHandle;

        public TextureHandle BloomColor { get; set; } = TextureHandle.nullHandle;

        public void Reset()
        {
            PreBloomColor = TextureHandle.nullHandle;
            BloomColor = TextureHandle.nullHandle;
        }
    }
}
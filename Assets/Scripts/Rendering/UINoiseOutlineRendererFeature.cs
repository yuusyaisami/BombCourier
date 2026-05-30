using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BC.Rendering
{
    // ====================================================================
    // Registry: RendererFeature と MonoBehaviour の仲介用 static レジストリ
    // ====================================================================

    /// <summary>
    /// 3D オブジェクトの UINoiseOutline 表示登録管理。
    /// UINoiseOutlineWorldMB (MonoBehaviour) が Add/Remove し、
    /// UINoiseOutlineRendererFeature がフレームごとにスナップショットを取る。
    /// </summary>
    public static class UINoiseOutlineRegistry
    {
        private static readonly HashSet<Renderer>       Entries     = new(64);
        private static readonly List<Renderer>          DeadEntries = new(16);
        private static          Renderer[]              snapshot    = System.Array.Empty<Renderer>();

        public static void Add(Renderer renderer)
        {
            if (renderer == null) return;
            Entries.Add(renderer);
        }

        public static void Remove(Renderer renderer)
        {
            if (renderer == null) return;
            Entries.Remove(renderer);
        }

        public static void ClearAll()
        {
            Entries.Clear();
        }

        public static Renderer[] CreateSnapshotArray()
        {
            DeadEntries.Clear();
            foreach (Renderer r in Entries)
            {
                if (r == null) DeadEntries.Add(r);
            }
            foreach (Renderer dead in DeadEntries) Entries.Remove(dead);

            if (snapshot.Length != Entries.Count)
                snapshot = new Renderer[Entries.Count];

            Entries.CopyTo(snapshot);
            return snapshot;
        }
    }

    // ====================================================================
    // ScriptableRendererFeature
    // ====================================================================

    /// <summary>
    /// 3D オブジェクトにスクリーンスペースのノイズアニメーション Outline を描画する URP RendererFeature。
    /// UINoiseOutlineWorld.shader の Pass 0 (Mask) と Pass 1 (Composite) を順に実行する。
    /// InteractionOutlineRendererFeature と同パターン。
    /// </summary>
    public sealed class UINoiseOutlineRendererFeature : ScriptableRendererFeature
    {
        [Header("Material")]
        [Tooltip("UINoiseOutlineWorld.shader を使ったマテリアル。")]
        [SerializeField] private Material outlineMaterial;

        [Header("Pass")]
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;

        private UINoiseOutlinePass pass;

        public override void Create()
        {
            pass = new UINoiseOutlinePass
            {
                renderPassEvent = passEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (outlineMaterial == null)
                return;

            UnityEngine.CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType != UnityEngine.CameraType.Game &&
                cameraType != UnityEngine.CameraType.SceneView)
            {
                return;
            }

            pass.Setup(outlineMaterial);
            renderer.EnqueuePass(pass);
        }

        // ----------------------------------------------------------------
        // Inner Pass
        // ----------------------------------------------------------------

        private sealed class UINoiseOutlinePass : ScriptableRenderPass
        {
            private const string MaskPassName      = "UI Noise Outline Mask";
            private const string CompositePassName = "UI Noise Outline Composite";

            private static readonly int BlitTextureId    = Shader.PropertyToID("_BlitTexture");
            private static readonly int BlitScaleBiasId  = Shader.PropertyToID("_BlitScaleBias");
            private static readonly int MaskTextureId    = Shader.PropertyToID("_NoiseOutlineWorldMaskTex");
            private static readonly int MaskTexelSizeId  = Shader.PropertyToID("_NoiseOutlineWorldMaskTexelSize");

            private Material material;

            public void Setup(Material mat)
            {
                material = mat;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            private sealed class MaskPassData
            {
                internal TextureHandle maskTexture;
                internal TextureHandle activeDepthTexture;
                internal Renderer[]    renderers;
                internal Material      material;
            }

            private sealed class CompositePassData
            {
                internal TextureHandle sourceColorTexture;
                internal TextureHandle maskTexture;
                internal Material      material;
                internal Vector4       maskTexelSize;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null) return;

                Renderer[] renderers = UINoiseOutlineRegistry.CreateSnapshotArray();
                if (renderers.Length == 0) return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer) return;

                TextureHandle activeColorTexture = resourceData.activeColorTexture;
                TextureHandle activeDepthTexture = resourceData.activeDepthTexture;

                if (!activeColorTexture.IsValid() || !activeDepthTexture.IsValid()) return;

                TextureDesc maskDesc = renderGraph.GetTextureDesc(activeColorTexture);
                maskDesc.name        = "_UINoiseOutlineWorldMask";
                maskDesc.clearBuffer = true;
                maskDesc.clearColor  = Color.clear;

                TextureHandle maskTexture = renderGraph.CreateTexture(maskDesc);

                RecordMaskPass(renderGraph, maskTexture, activeDepthTexture, renderers);

                TextureDesc colorDesc = renderGraph.GetTextureDesc(activeColorTexture);
                Vector4 maskTexelSize = new(
                    1f / Mathf.Max(1, colorDesc.width),
                    1f / Mathf.Max(1, colorDesc.height),
                    colorDesc.width,
                    colorDesc.height);

                TextureHandle compositeOutput = RecordCompositePass(
                    renderGraph, activeColorTexture, maskTexture, maskTexelSize);

                resourceData.cameraColor = compositeOutput;
            }

            private void RecordMaskPass(
                RenderGraph renderGraph,
                TextureHandle maskTexture,
                TextureHandle activeDepthTexture,
                Renderer[] renderers)
            {
                using var builder = renderGraph.AddUnsafePass<MaskPassData>(MaskPassName, out MaskPassData data);
                data.maskTexture       = maskTexture;
                data.activeDepthTexture = activeDepthTexture;
                data.renderers         = renderers;
                data.material          = material;

                builder.UseTexture(data.maskTexture,        AccessFlags.Write);
                builder.UseTexture(data.activeDepthTexture, AccessFlags.Read);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (MaskPassData d, UnsafeGraphContext ctx) =>
                {
                    ctx.cmd.SetRenderTarget(d.maskTexture, d.activeDepthTexture);
                    CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    cmd.ClearRenderTarget(false, true, Color.clear);

                    foreach (Renderer r in d.renderers)
                    {
                        if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                        int subMeshCount = r.sharedMaterials?.Length ?? 1;
                        for (int s = 0; s < subMeshCount; s++)
                        {
                            cmd.DrawRenderer(r, d.material, s, 0); // Pass 0 = Mask
                        }
                    }
                });
            }

            private TextureHandle RecordCompositePass(
                RenderGraph renderGraph,
                TextureHandle sourceColorTexture,
                TextureHandle maskTexture,
                Vector4 maskTexelSize)
            {
                TextureDesc destDesc = renderGraph.GetTextureDesc(sourceColorTexture);
                destDesc.name        = CompositePassName;
                destDesc.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(destDesc);

                using var builder = renderGraph.AddRasterRenderPass<CompositePassData>(CompositePassName, out CompositePassData data);
                data.sourceColorTexture = sourceColorTexture;
                data.maskTexture        = maskTexture;
                data.material           = material;
                data.maskTexelSize      = maskTexelSize;

                builder.UseTexture(data.sourceColorTexture, AccessFlags.Read);
                builder.UseTexture(data.maskTexture,        AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (CompositePassData d, RasterGraphContext ctx) =>
                {
                    d.material.SetVector(Shader.PropertyToID("_NoiseOutlineWorldMaskTexelSize"), d.maskTexelSize);
                    d.material.SetTexture(Shader.PropertyToID("_NoiseOutlineWorldMaskTex"), d.maskTexture);
                    d.material.SetVector(Shader.PropertyToID("_BlitScaleBias"), new Vector4(1, 1, 0, 0));
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, d.material, 1, MeshTopology.Triangles, 3);
                });

                return destination;
            }
        }
    }
}

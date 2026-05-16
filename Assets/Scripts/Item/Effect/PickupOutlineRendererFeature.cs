using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BC.Rendering
{
    public sealed class PickupOutlineRendererFeature : ScriptableRendererFeature
    {
        [Header("Materials")]
        [SerializeField] private Material candidateOutlineMaterial;
        [SerializeField] private Material bestOutlineMaterial;

        [Header("Pass")]
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;

        private PickupOutlinePass pass;

        public override void Create()
        {
            pass = new PickupOutlinePass
            {
                renderPassEvent = passEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (candidateOutlineMaterial == null || bestOutlineMaterial == null)
                return;

            var cameraType = renderingData.cameraData.cameraType;

            if (cameraType != UnityEngine.CameraType.Game &&
                cameraType != UnityEngine.CameraType.SceneView)
            {
                return;
            }

            pass.Setup(candidateOutlineMaterial, bestOutlineMaterial);
            renderer.EnqueuePass(pass);
        }

        private sealed class PickupOutlinePass : ScriptableRenderPass
        {
            private const string CandidateMaskPassName = "Pickup Outline Candidate Mask";
            private const string BestMaskPassName = "Pickup Outline Best Mask";
            private const string CompositePassName = "Pickup Outline Composite";

            private static readonly int MaskTextureId = Shader.PropertyToID("_PickupOutlineMaskTex");
            private static readonly int MaskTexelSizeId = Shader.PropertyToID("_PickupOutlineMaskTexelSize");
            private static readonly MaterialPropertyBlock SharedPropertyBlock = new();

            private Material candidateMaterial;
            private Material bestMaterial;

            public void Setup(Material candidateMaterial, Material bestMaterial)
            {
                this.candidateMaterial = candidateMaterial;
                this.bestMaterial = bestMaterial;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            private sealed class MaskPassData
            {
                internal TextureHandle maskTexture;
                internal TextureHandle activeDepthTexture;
                internal PickupOutlineEntry[] entries;
                internal Material material;
                internal PickupOutlineKind kind;
            }

            private sealed class CompositePassData
            {
                internal TextureHandle activeColorTexture;
                internal TextureHandle activeDepthTexture;
                internal TextureHandle candidateMaskTexture;
                internal TextureHandle bestMaskTexture;
                internal bool hasCandidateMask;
                internal bool hasBestMask;
                internal PickupOutlineEntry[] entries;
                internal Material candidateMaterial;
                internal Material bestMaterial;
                internal Vector4 maskTexelSize;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (candidateMaterial == null || bestMaterial == null)
                    return;

                PickupOutlineEntry[] entries = PickupOutlineRegistry.CreateSnapshotArray();

                if (entries.Length == 0)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                if (resourceData.isActiveTargetBackBuffer)
                    return;

                TextureHandle activeColorTexture = resourceData.activeColorTexture;
                TextureHandle activeDepthTexture = resourceData.activeDepthTexture;

                if (!activeColorTexture.IsValid() || !activeDepthTexture.IsValid())
                    return;

                bool hasCandidate = ContainsKind(entries, PickupOutlineKind.Candidate);
                bool hasBest = ContainsKind(entries, PickupOutlineKind.Best);

                if (!hasCandidate && !hasBest)
                    return;

                TextureDesc maskDesc = renderGraph.GetTextureDesc(activeColorTexture);
                maskDesc.clearBuffer = true;
                maskDesc.clearColor = Color.clear;

                TextureHandle candidateMask = TextureHandle.nullHandle;

                if (hasCandidate)
                {
                    maskDesc.name = "_PickupOutlineCandidateMask";
                    candidateMask = renderGraph.CreateTexture(maskDesc);
                    RecordMaskPass(
                        renderGraph,
                        CandidateMaskPassName,
                        candidateMask,
                        activeDepthTexture,
                        entries,
                        candidateMaterial,
                        PickupOutlineKind.Candidate);
                }

                TextureHandle bestMask = TextureHandle.nullHandle;

                if (hasBest)
                {
                    maskDesc.name = "_PickupOutlineBestMask";
                    bestMask = renderGraph.CreateTexture(maskDesc);
                    RecordMaskPass(
                        renderGraph,
                        BestMaskPassName,
                        bestMask,
                        activeDepthTexture,
                        entries,
                        bestMaterial,
                        PickupOutlineKind.Best);
                }

                TextureDesc activeColorDesc = renderGraph.GetTextureDesc(activeColorTexture);
                Vector4 maskTexelSize = new(
                    1f / Mathf.Max(1, activeColorDesc.width),
                    1f / Mathf.Max(1, activeColorDesc.height),
                    activeColorDesc.width,
                    activeColorDesc.height);

                RecordCompositePass(
                    renderGraph,
                    activeColorTexture,
                    activeDepthTexture,
                    candidateMask,
                    bestMask,
                    hasCandidate,
                    hasBest,
                    entries,
                    maskTexelSize);
            }

            private void RecordMaskPass(
                RenderGraph renderGraph,
                string passName,
                TextureHandle maskTexture,
                TextureHandle activeDepthTexture,
                PickupOutlineEntry[] entries,
                Material material,
                PickupOutlineKind kind)
            {
                using (var builder = renderGraph.AddUnsafePass<MaskPassData>(passName, out MaskPassData passData))
                {
                    passData.maskTexture = maskTexture;
                    passData.activeDepthTexture = activeDepthTexture;
                    passData.entries = entries;
                    passData.material = material;
                    passData.kind = kind;

                    builder.UseTexture(passData.maskTexture, AccessFlags.Write);

                    // 既存Depthで可視面だけをmask化する。裏側や壁の向こうの輪郭を拾わないための制御。
                    builder.UseTexture(passData.activeDepthTexture, AccessFlags.Read);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc(static (MaskPassData data, UnsafeGraphContext context) =>
                    {
                        ExecuteMaskPass(data, context);
                    });
                }
            }

            private void RecordCompositePass(
                RenderGraph renderGraph,
                TextureHandle activeColorTexture,
                TextureHandle activeDepthTexture,
                TextureHandle candidateMaskTexture,
                TextureHandle bestMaskTexture,
                bool hasCandidateMask,
                bool hasBestMask,
                PickupOutlineEntry[] entries,
                Vector4 maskTexelSize)
            {
                using (var builder = renderGraph.AddUnsafePass<CompositePassData>(CompositePassName, out CompositePassData passData))
                {
                    passData.activeColorTexture = activeColorTexture;
                    passData.activeDepthTexture = activeDepthTexture;
                    passData.candidateMaskTexture = candidateMaskTexture;
                    passData.bestMaskTexture = bestMaskTexture;
                    passData.hasCandidateMask = hasCandidateMask;
                    passData.hasBestMask = hasBestMask;
                    passData.entries = entries;
                    passData.candidateMaterial = candidateMaterial;
                    passData.bestMaterial = bestMaterial;
                    passData.maskTexelSize = maskTexelSize;

                    builder.UseTexture(passData.activeColorTexture, AccessFlags.Write);

                    if (passData.hasCandidateMask)
                    {
                        builder.UseTexture(passData.candidateMaskTexture, AccessFlags.Read);
                    }

                    if (passData.hasBestMask)
                    {
                        builder.UseTexture(passData.bestMaskTexture, AccessFlags.Read);
                    }

                    // CompositeではDepthを書かないが、Colorと同じTarget状態を保つためDepthも束ねる。
                    builder.UseTexture(passData.activeDepthTexture, AccessFlags.Read);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc(static (CompositePassData data, UnsafeGraphContext context) =>
                    {
                        ExecuteCompositePass(data, context);
                    });
                }
            }

            private static void ExecuteMaskPass(MaskPassData data, UnsafeGraphContext context)
            {
                context.cmd.SetRenderTarget(data.maskTexture, data.activeDepthTexture);

                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                cmd.ClearRenderTarget(false, true, Color.clear);

                for (int i = 0; i < data.entries.Length; i++)
                {
                    PickupOutlineEntry entry = data.entries[i];

                    if (entry.Kind != data.kind)
                        continue;

                    Renderer renderer = entry.Renderer;

                    if (renderer == null ||
                        !renderer.enabled ||
                        !renderer.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    if (data.material == null)
                        continue;

                    Material[] sharedMaterials = renderer.sharedMaterials;

                    int subMeshCount = sharedMaterials != null && sharedMaterials.Length > 0
                        ? sharedMaterials.Length
                        : 1;

                    for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
                    {
                        cmd.DrawRenderer(renderer, data.material, subMeshIndex, 0);
                    }
                }
            }

            private static void ExecuteCompositePass(CompositePassData data, UnsafeGraphContext context)
            {
                context.cmd.SetRenderTarget(data.activeColorTexture, data.activeDepthTexture);

                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                if (data.hasCandidateMask)
                {
                    DrawCompositeLayer(cmd, data.candidateMaterial, data.candidateMaskTexture, data.maskTexelSize);
                }

                if (data.hasBestMask)
                {
                    DrawCompositeLayer(cmd, data.bestMaterial, data.bestMaskTexture, data.maskTexelSize);
                }
            }

            private static void DrawCompositeLayer(
                CommandBuffer cmd,
                Material material,
                RTHandle maskTexture,
                Vector4 maskTexelSize)
            {
                if (material == null || maskTexture == null)
                    return;

                SharedPropertyBlock.Clear();
                SharedPropertyBlock.SetTexture(MaskTextureId, maskTexture);
                SharedPropertyBlock.SetVector(MaskTexelSizeId, maskTexelSize);

                cmd.DrawProcedural(
                    Matrix4x4.identity,
                    material,
                    1,
                    MeshTopology.Triangles,
                    3,
                    1,
                    SharedPropertyBlock);
            }

            private static bool ContainsKind(PickupOutlineEntry[] entries, PickupOutlineKind kind)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].Kind == kind)
                        return true;
                }

                return false;
            }
        }
    }
}
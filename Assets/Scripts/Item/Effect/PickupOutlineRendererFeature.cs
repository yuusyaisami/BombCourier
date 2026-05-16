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
            private const string CandidateCompositePassName = "Pickup Outline Candidate Composite";
            private const string BestCompositePassName = "Pickup Outline Best Composite";

            private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
            private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
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
                internal TextureHandle sourceColorTexture;
                internal TextureHandle maskTexture;
                internal Material material;
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

                TextureHandle compositeColor = activeColorTexture;

                if (hasCandidate)
                {
                    compositeColor = RecordCompositePass(
                        renderGraph,
                        CandidateCompositePassName,
                        compositeColor,
                        candidateMask,
                        candidateMaterial,
                        maskTexelSize);
                }

                if (hasBest)
                {
                    compositeColor = RecordCompositePass(
                        renderGraph,
                        BestCompositePassName,
                        compositeColor,
                        bestMask,
                        bestMaterial,
                        maskTexelSize);
                }

                resourceData.cameraColor = compositeColor;
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

            private TextureHandle RecordCompositePass(
                RenderGraph renderGraph,
                string passName,
                TextureHandle sourceColorTexture,
                TextureHandle maskTexture,
                Material material,
                Vector4 maskTexelSize)
            {
                TextureDesc destinationDesc = renderGraph.GetTextureDesc(sourceColorTexture);
                destinationDesc.name = passName;
                destinationDesc.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

                using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(passName, out CompositePassData passData))
                {
                    passData.sourceColorTexture = sourceColorTexture;
                    passData.maskTexture = maskTexture;
                    passData.material = material;
                    passData.maskTexelSize = maskTexelSize;

                    builder.UseTexture(passData.sourceColorTexture, AccessFlags.Read);
                    builder.UseTexture(passData.maskTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
                    {
                        ExecuteCompositePass(data, context);
                    });
                }

                return destination;
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

            private static void ExecuteCompositePass(CompositePassData data, RasterGraphContext context)
            {
                DrawCompositeLayer(context.cmd, data.material, data.sourceColorTexture, data.maskTexture, data.maskTexelSize);
            }

            private static void DrawCompositeLayer(
                RasterCommandBuffer cmd,
                Material material,
                RTHandle sourceColorTexture,
                RTHandle maskTexture,
                Vector4 maskTexelSize)
            {
                if (material == null || sourceColorTexture == null || maskTexture == null)
                    return;

                SharedPropertyBlock.Clear();
                SharedPropertyBlock.SetTexture(BlitTextureId, sourceColorTexture);
                SharedPropertyBlock.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
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
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
            private Material candidateMaterial;
            private Material bestMaterial;

            public void Setup(Material candidateMaterial, Material bestMaterial)
            {
                this.candidateMaterial = candidateMaterial;
                this.bestMaterial = bestMaterial;
            }

            private sealed class PassData
            {
                internal TextureHandle activeColorTexture;
                internal PickupOutlineEntry[] entries;
                internal Material candidateMaterial;
                internal Material bestMaterial;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (candidateMaterial == null || bestMaterial == null)
                    return;

                PickupOutlineEntry[] entries = PickupOutlineRegistry.CreateSnapshotArray();

                if (entries.Length == 0)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                using (var builder = renderGraph.AddUnsafePass<PassData>(
                    "Pickup Outline",
                    out PassData passData))
                {
                    passData.activeColorTexture = resourceData.activeColorTexture;
                    passData.entries = entries;
                    passData.candidateMaterial = candidateMaterial;
                    passData.bestMaterial = bestMaterial;

                    builder.UseTexture(passData.activeColorTexture, AccessFlags.Write);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
                    {
                        ExecutePass(data, context);
                    });
                }
            }

            private static void ExecutePass(PassData data, UnsafeGraphContext context)
            {
                context.cmd.SetRenderTarget(data.activeColorTexture);

                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                for (int i = 0; i < data.entries.Length; i++)
                {
                    PickupOutlineEntry entry = data.entries[i];

                    Renderer renderer = entry.Renderer;

                    if (renderer == null ||
                        !renderer.enabled ||
                        !renderer.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    Material material = entry.Kind == PickupOutlineKind.Best
                        ? data.bestMaterial
                        : data.candidateMaterial;

                    if (material == null)
                        continue;

                    Material[] sharedMaterials = renderer.sharedMaterials;
                    int subMeshCount = sharedMaterials != null && sharedMaterials.Length > 0
                        ? sharedMaterials.Length
                        : 1;

                    for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
                    {
                        cmd.DrawRenderer(renderer, material, subMeshIndex, 0);
                    }
                }
            }
        }
    }
}
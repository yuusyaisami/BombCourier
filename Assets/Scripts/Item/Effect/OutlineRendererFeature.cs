using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BC.Rendering
{
    public sealed class OutlineRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;

        private OutlinePass pass;

        public override void Create()
        {
            pass = new OutlinePass
            {
                renderPassEvent = passEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (outlineMaterial == null)
                return;

            pass.Setup(outlineMaterial);
            renderer.EnqueuePass(pass);
        }

        private sealed class OutlinePass : ScriptableRenderPass
        {
            private readonly List<Renderer> renderers = new();
            private Material outlineMaterial;

            public void Setup(Material material)
            {
                outlineMaterial = material;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (outlineMaterial == null)
                    return;

                Camera camera = renderingData.cameraData.camera;
                if (camera == null)
                    return;

                OutlineCameraBridgeMB bridge = camera.GetComponent<OutlineCameraBridgeMB>();
                if (bridge == null)
                    return;

                int count = bridge.CopyRenderers(renderers);
                if (count <= 0)
                    return;

                CommandBuffer cmd = CommandBufferPool.Get("BC Outline Pass");

                for (int i = 0; i < renderers.Count; i++)
                {
                    Renderer renderer = renderers[i];

                    if (renderer == null)
                        continue;

                    int subMeshCount = GetSubMeshCount(renderer);

                    for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
                    {
                        cmd.DrawRenderer(renderer, outlineMaterial, subMeshIndex, 0);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private static int GetSubMeshCount(Renderer renderer)
            {
                if (renderer is MeshRenderer)
                {
                    MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        return Mathf.Max(1, meshFilter.sharedMesh.subMeshCount);
                    }
                }

                if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    if (skinnedMeshRenderer.sharedMesh != null)
                    {
                        return Mathf.Max(1, skinnedMeshRenderer.sharedMesh.subMeshCount);
                    }
                }

                return Mathf.Max(1, renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 1);
            }
        }
    }
}
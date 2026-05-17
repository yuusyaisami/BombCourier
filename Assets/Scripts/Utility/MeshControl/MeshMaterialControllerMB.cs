using System.Collections.Generic;
using UnityEngine;

namespace BC.Utility
{
    // RendererのMaterialPropertyBlockを操作して、マテリアルの色を変更するためのクラス。
    // 主に、ゴールゲートやボーナスアイテムなどのオブジェクトの透明度を制御するために使用します。
    public sealed class MeshMaterialControllerMB : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SurfaceModeId = Shader.PropertyToID("_SurfaceMode");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        private const float OpaqueSurfaceMode = 0f;
        private const float TransparentSurfaceMode = 1f;
        private const int OpaqueSrcBlend = 1;
        private const int OpaqueDstBlend = 0;
        private const int TransparentSrcBlend = 5;
        private const int TransparentDstBlend = 10;
        private const int OpaqueRenderQueue = 2000;
        private const int TransparentRenderQueue = 3000;

        [SerializeField] private List<Renderer> targetRenderer;
        [SerializeField, Range(0f, 1f)] private float alpha = 0.5f;

        private MaterialPropertyBlock propertyBlock;
        private Color baseColor = Color.white;
        private bool lastTransparentState;

        private void Awake()
        {
            if (targetRenderer == null || targetRenderer.Count == 0)
                targetRenderer = new List<Renderer>(GetComponents<Renderer>());

            propertyBlock = new MaterialPropertyBlock();

            // Materialの元の色を取得
            if (targetRenderer.Count > 0)
            {
                var mat = targetRenderer[0].sharedMaterial;
                if (mat != null && mat.HasProperty(BaseColorId))
                    baseColor = mat.GetColor(BaseColorId);
            }

            lastTransparentState = alpha < 0.999f;
            ApplySurfaceMode(lastTransparentState);
            SetAlpha(alpha);
        }

        public void SetAlpha(float value)
        {
            alpha = Mathf.Clamp01(value);
            bool useTransparentSurface = alpha < 0.999f;
            if (useTransparentSurface != lastTransparentState)
            {
                ApplySurfaceMode(useTransparentSurface);
                lastTransparentState = useTransparentSurface;
            }

            foreach (var renderer in targetRenderer)
            {
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(propertyBlock);

                var color = baseColor;
                color.a = alpha;

                propertyBlock.SetColor(BaseColorId, color);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void ApplySurfaceMode(bool transparent)
        {
            foreach (Renderer renderer in targetRenderer)
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.materials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null || !material.HasProperty(SurfaceModeId))
                        continue;

                    material.SetFloat(SurfaceModeId, transparent ? TransparentSurfaceMode : OpaqueSurfaceMode);
                    material.SetFloat(SrcBlendId, transparent ? TransparentSrcBlend : OpaqueSrcBlend);
                    material.SetFloat(DstBlendId, transparent ? TransparentDstBlend : OpaqueDstBlend);
                    material.SetFloat(ZWriteId, transparent ? 0f : 1f);
                    material.renderQueue = transparent ? TransparentRenderQueue : OpaqueRenderQueue;
                    material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
                    material.SetOverrideTag("Queue", transparent ? "Transparent" : "Geometry");
                    material.SetShaderPassEnabled("ShadowCaster", !transparent);
                    material.SetShaderPassEnabled("DepthOnly", !transparent);
                    material.SetShaderPassEnabled("DepthNormalsOnly", !transparent);
                }
            }
        }
    }
}
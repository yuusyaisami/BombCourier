using System.Collections.Generic;
using UnityEngine;

namespace BC.Utility
{
    // RendererのMaterialPropertyBlockを操作して、マテリアルの色を変更するためのクラス。
    // 主に、ゴールゲートやボーナスアイテムなどのオブジェクトの透明度を制御するために使用します。
    public sealed class MeshMaterialControllerMB : MonoBehaviour
    {
        [SerializeField] private List<Renderer> targetRenderer;
        [SerializeField, Range(0f, 1f)] private float alpha = 0.5f;

        private MaterialPropertyBlock propertyBlock;
        private Color[] rendererBaseColors = System.Array.Empty<Color>();
        private float lastAppliedAlpha = -1f;

        private void Awake()
        {
            EnsureTargets();
            CacheBaseColors();
            SetAlpha(alpha);
        }

        private void Reset()
        {
            targetRenderer = new List<Renderer>(GetComponentsInChildren<Renderer>(true));
        }

        private void OnValidate()
        {
            alpha = Mathf.Clamp01(alpha);

            if (!Application.isPlaying)
            {
                EnsureTargets();
                CacheBaseColors();
            }
        }

        public void SetAlpha(float value)
        {
            float clampedAlpha = Mathf.Clamp01(value);
            alpha = clampedAlpha;

            EnsureTargets();

            if (Mathf.Approximately(lastAppliedAlpha, clampedAlpha))
                return;

            CacheBaseColors();

            for (int rendererIndex = 0; rendererIndex < targetRenderer.Count; rendererIndex++)
            {
                Renderer renderer = targetRenderer[rendererIndex];
                if (renderer == null)
                    continue;

                Color baseColor = rendererBaseColors.Length > rendererIndex
                    ? rendererBaseColors[rendererIndex]
                    : RendererVisualStateUtility.ResolveBaseColor(renderer);

                RendererVisualStateUtility.Apply(
                    renderer,
                    RendererVisualState.FromBaseColor(baseColor).WithAlpha(clampedAlpha),
                    false,
                    propertyBlock);
            }

            lastAppliedAlpha = clampedAlpha;
        }

        private void EnsureTargets()
        {
            if (targetRenderer == null || targetRenderer.Count == 0)
                targetRenderer = new List<Renderer>(GetComponentsInChildren<Renderer>(true));

            propertyBlock ??= new MaterialPropertyBlock();
        }

        private void CacheBaseColors()
        {
            if (targetRenderer == null)
            {
                rendererBaseColors = System.Array.Empty<Color>();
                return;
            }

            if (rendererBaseColors.Length != targetRenderer.Count)
                rendererBaseColors = new Color[targetRenderer.Count];

            for (int rendererIndex = 0; rendererIndex < targetRenderer.Count; rendererIndex++)
            {
                Renderer renderer = targetRenderer[rendererIndex];
                if (renderer == null)
                {
                    rendererBaseColors[rendererIndex] = Color.white;
                    continue;
                }

                rendererBaseColors[rendererIndex] = RendererVisualStateUtility.ResolveBaseColor(renderer);
            }
        }
    }
}
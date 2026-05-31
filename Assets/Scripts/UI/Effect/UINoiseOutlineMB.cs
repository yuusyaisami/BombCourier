using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BC.UI.Effect
{
    // フォーカス状態をノイズアニメーション付きアウトラインで視覚化する UI コンポーネント。
    // Awake 時に子 GameObject を生成し、そこに Image + UINoiseOutline.shader のマテリアルを配置する。
    // SetFocused(bool) を呼ぶことで、_Intensity を DOTween でアニメーションする。
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class UINoiseOutlineMB : MonoBehaviour
    {
        [Header("Outline")]
        [SerializeField] private Color outlineColor = new Color(1f, 0.85f, 0f, 1f);
        [SerializeField] private Color outlineInnerColor = new Color(1f, 0.95f, 0.55f, 0.35f);
        [SerializeField, Range(0f, 0.15f)] private float outlineWidth = 0.05f;
        [SerializeField, Range(1f, 30f)] private float noiseScale = 8f;
        [SerializeField, Range(0f, 5f)] private float noiseSpeed = 1.5f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.15f;
        [SerializeField] private Vector2 padding = new Vector2(6f, 6f);

        [Header("Editor Preview")]
        [SerializeField] private bool showOutlineInEditor = true;
        [SerializeField, Range(0f, 1f)] private float editorPreviewIntensity = 1f;

        private static readonly int PropOutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int PropOutlineInnerColor = Shader.PropertyToID("_OutlineInnerColor");
        private static readonly int PropOutlineWidth = Shader.PropertyToID("_OutlineWidth");
        private static readonly int PropNoiseScale = Shader.PropertyToID("_NoiseScale");
        private static readonly int PropNoiseSpeed = Shader.PropertyToID("_NoiseSpeed");
        private static readonly int PropIntensity = Shader.PropertyToID("_Intensity");
        private static readonly int PropRectSize = Shader.PropertyToID("_RectSize");

        private Image outlineImage;
        private Material outlineMaterial;
        private Tweener currentTween;
        private bool ownsOutlineMaterial;
        private bool isFocused;
        private Vector2 lastAppliedRectSize;

        private void Awake()
        {
            EnsureOutlineChild();

            if (Application.isPlaying)
                SetFocusedImmediate(false);
        }

        private void OnEnable()
        {
            EnsureOutlineChild();

            if (Application.isPlaying)
                SetFocusedImmediate(false);
        }

        private void LateUpdate()
        {
            if (outlineImage == null || outlineMaterial == null)
                return;

            RectTransform rt = outlineImage.rectTransform;
            if (rt == null)
                return;

            Vector2 rectSize = rt.rect.size;
            if ((rectSize - lastAppliedRectSize).sqrMagnitude > 0.0001f)
                ApplyProperties();
        }

        private void EnsureOutlineChild()
        {
            // 既存の子を探す（ホットリロード対策）
            Transform existing = transform.Find("[NoiseOutline]");
            if (existing != null)
            {
                outlineImage = existing.GetComponent<Image>();
                if (outlineImage != null)
                {
                    RectTransform existingRect = existing.GetComponent<RectTransform>();
                    if (existingRect != null)
                    {
                        existingRect.anchorMin = Vector2.zero;
                        existingRect.anchorMax = Vector2.one;
                        existingRect.offsetMin = -padding;
                        existingRect.offsetMax = padding;
                        existingRect.localScale = Vector3.one;
                    }

                    outlineMaterial = outlineImage.material;
                    bool hasExpectedShader = outlineMaterial != null &&
                                             outlineMaterial.shader != null &&
                                             outlineMaterial.shader.name == "BC/UI/NoiseOutline";

                    if (!hasExpectedShader)
                    {
                        Shader existingShader = Shader.Find("BC/UI/NoiseOutline");
                        if (existingShader == null)
                        {
                            Debug.LogError($"[{nameof(UINoiseOutlineMB)}] Shader 'BC/UI/NoiseOutline' not found.", this);
                            return;
                        }

                        outlineMaterial = new Material(existingShader) { name = "UINoiseOutline_Mat" };
                        outlineImage.material = outlineMaterial;
                        ownsOutlineMaterial = true;
                    }

                    EnsureRuntimeMaterialInstance();

                    ApplyProperties();
                    return;
                }
            }

            // 新規生成
            GameObject child = new GameObject("[NoiseOutline]");
            child.transform.SetParent(transform, false);
            child.transform.SetAsFirstSibling();

            RectTransform rt = child.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = -padding;
            rt.offsetMax = padding;
            rt.localScale = Vector3.one;

            outlineImage = child.AddComponent<Image>();
            outlineImage.raycastTarget = false;

            Shader createdShader = Shader.Find("BC/UI/NoiseOutline");
            if (createdShader == null)
            {
                Debug.LogError($"[{nameof(UINoiseOutlineMB)}] Shader 'BC/UI/NoiseOutline' not found.", this);
                return;
            }

            outlineMaterial = new Material(createdShader) { name = "UINoiseOutline_Mat" };
            outlineImage.material = outlineMaterial;
            outlineImage.color = Color.white;
            ownsOutlineMaterial = true;

            EnsureRuntimeMaterialInstance();

            ApplyProperties();
        }

        private void EnsureRuntimeMaterialInstance()
        {
            if (!Application.isPlaying)
                return;

            if (outlineImage == null || outlineMaterial == null)
                return;

            // prefab / editor 由来の共有 material を実行時インスタンスに分離する。
            if (ownsOutlineMaterial)
                return;

            outlineMaterial = new Material(outlineMaterial)
            {
                name = "UINoiseOutline_RuntimeMat"
            };
            outlineImage.material = outlineMaterial;
            ownsOutlineMaterial = true;
        }

        private void ApplyProperties()
        {
            if (outlineMaterial == null) return;

            float currentIntensity = outlineMaterial.HasFloat(PropIntensity)
                ? outlineMaterial.GetFloat(PropIntensity)
                : 0f;

            outlineMaterial.SetColor(PropOutlineColor, outlineColor);
            outlineMaterial.SetColor(PropOutlineInnerColor, outlineInnerColor);
            outlineMaterial.SetFloat(PropOutlineWidth, outlineWidth);
            outlineMaterial.SetFloat(PropNoiseScale, noiseScale);
            outlineMaterial.SetFloat(PropNoiseSpeed, noiseSpeed);
            if (outlineImage != null)
            {
                Vector2 rectSize = outlineImage.rectTransform.rect.size;
                outlineMaterial.SetVector(PropRectSize, new Vector4(rectSize.x, rectSize.y, 0f, 0f));
                lastAppliedRectSize = rectSize;
            }

            if (Application.isPlaying)
                outlineMaterial.SetFloat(PropIntensity, currentIntensity);
            else
                outlineMaterial.SetFloat(PropIntensity, showOutlineInEditor ? editorPreviewIntensity : 0f);
        }

        /// <summary>フォーカス状態を設定する。<paramref name="focused"/> が true のとき Outline を表示する。</summary>
        public void SetFocused(bool focused)
        {
            if (outlineMaterial == null) return;

            isFocused = focused;

            currentTween?.Kill();
            float target = focused ? 1f : 0f;

            if (fadeDuration <= 0f)
            {
                outlineMaterial.SetFloat(PropIntensity, target);
                return;
            }

            currentTween = DOTween
                .To(() => outlineMaterial.GetFloat(PropIntensity),
                    v => outlineMaterial.SetFloat(PropIntensity, v),
                    target,
                    fadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private void SetFocusedImmediate(bool focused)
        {
            if (outlineMaterial == null)
                return;

            isFocused = focused;
            currentTween?.Kill();
            outlineMaterial.SetFloat(PropIntensity, focused ? 1f : 0f);
        }

        /// <summary>アウトラインカラーと幅を実行時に変更する。</summary>
        public void SetStyle(Color color, float width)
        {
            outlineColor = color;
            outlineInnerColor = new Color(color.r, color.g, color.b, color.a * 0.35f);
            outlineWidth = width;
            ApplyProperties();
        }

        private void OnDestroy()
        {
            currentTween?.Kill();
            if (outlineMaterial != null && ownsOutlineMaterial)
            {
                if (Application.isPlaying)
                    Destroy(outlineMaterial);
                else
                    DestroyImmediate(outlineMaterial);
            }

            outlineMaterial = null;
            ownsOutlineMaterial = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureOutlineChild();
            if (outlineMaterial != null)
                ApplyProperties();
        }
#endif
    }
}

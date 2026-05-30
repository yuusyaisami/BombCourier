using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BC.UI.Effect
{
    // ボタン選択時に高速フラッシュ演出を行い、演出中は入力を遮断するコンポーネント。
    // PlayFlashAsync を await すると演出完了後に制御が戻る。
    // フラッシュには BC/UI/ButtonFlash シェーダーのマテリアルが必要。
    // Image コンポーネントと同じ GameObject に配置すること。
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public sealed class UIButtonFlashMB : MonoBehaviour
    {
        [Header("Flash")]
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField, Min(1)] private int   flashCount    = 3;
        [SerializeField, Min(0.01f)] private float flashOnDuration  = 0.055f;
        [SerializeField, Min(0.01f)] private float flashOffDuration = 0.055f;

        [Header("Blocking")]
        [Tooltip("フラッシュ中に無効化する CanvasGroup。null の場合は自動的に親から探す。")]
        [SerializeField] private CanvasGroup blockingCanvasGroup;

        private static readonly int PropIntensity = Shader.PropertyToID("_Intensity");

        private Image    targetImage;
        private Material flashMaterial;
        private bool     isPlaying;

        private void Awake()
        {
            targetImage = GetComponent<Image>();

            Shader shader = Shader.Find("BC/UI/ButtonFlash");
            if (shader == null)
            {
                Debug.LogError($"[{nameof(UIButtonFlashMB)}] Shader 'BC/UI/ButtonFlash' not found.", this);
                return;
            }

            // 元マテリアルを壊さないよう Instance を作る
            flashMaterial = new Material(shader) { name = "UIButtonFlash_Mat" };
            flashMaterial.SetFloat(PropIntensity, 0f);

            // テクスチャを元 Image から引き継ぐ
            if (targetImage.sprite != null)
                flashMaterial.mainTexture = targetImage.sprite.texture;

            targetImage.material = flashMaterial;

            // blockingCanvasGroup が未設定なら親を検索
            if (blockingCanvasGroup == null)
                blockingCanvasGroup = GetComponentInParent<CanvasGroup>(true);
        }

        /// <summary>
        /// フラッシュ演出を再生し完了を待つ。演出中は <see cref="blockingCanvasGroup"/> が無効化される。
        /// <paramref name="onComplete"/> は演出完了後に呼ばれる。
        /// </summary>
        public async UniTask PlayFlashAsync(CancellationToken ct, Action onComplete = null)
        {
            if (isPlaying) return;
            if (flashMaterial == null) { onComplete?.Invoke(); return; }

            isPlaying = true;

            if (blockingCanvasGroup != null)
                blockingCanvasGroup.interactable = false;

            try
            {
                for (int i = 0; i < flashCount; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    await DOTween.To(
                            () => flashMaterial.GetFloat(PropIntensity),
                            v  => flashMaterial.SetFloat(PropIntensity, v),
                            1f,
                            flashOnDuration)
                        .SetEase(Ease.Linear)
                        .SetUpdate(true)
                        .WithCancellation(ct);

                    ct.ThrowIfCancellationRequested();
                    await DOTween.To(
                            () => flashMaterial.GetFloat(PropIntensity),
                            v  => flashMaterial.SetFloat(PropIntensity, v),
                            0f,
                            flashOffDuration)
                        .SetEase(Ease.Linear)
                        .SetUpdate(true)
                        .WithCancellation(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセル時はリセットして終了
                flashMaterial.SetFloat(PropIntensity, 0f);
                throw;
            }
            finally
            {
                flashMaterial.SetFloat(PropIntensity, 0f);
                isPlaying = false;

                if (blockingCanvasGroup != null)
                    blockingCanvasGroup.interactable = true;
            }

            onComplete?.Invoke();
        }

        private void OnDestroy()
        {
            if (flashMaterial != null) Destroy(flashMaterial);
        }
    }
}

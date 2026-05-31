using System;
using System.Threading;
using BC.Audio;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
namespace BC.UI
{
    // フェードエフェクトのUIロジックを管理するクラス
    public class UIFadeEffectMB : MonoBehaviour, IUIFadeEffect
    {
        // 単一フェードにて使用するImage
        [SerializeField] private Image fadeImage;
        // 上下フェードにて使用するImage
        [SerializeField] private Image topFadeImage;
        [SerializeField] private Image bottomFadeImage;
        [SerializeField] private Canvas fadeCanvas;
        [SerializeField] private FadeType defaultFadeType = FadeType.Single;
        [SerializeField] private float defaultFadeAmount = 1f;

        [Header("Sound")]
        // フェードアウト時に再生するサウンド。
        [SerializeField] private AudioDataSO fadeOutSound;
        // フェードイン時に再生するサウンド。
        [SerializeField] private AudioDataSO fadeInSound;
        // この値以上のアルファ変化量のときだけサウンドを再生する。
        [SerializeField, Min(0f)] private float minFadeDeltaForSound = 0.5f;

        private CancellationTokenSource activeFadeCancellationTokenSource;
        private FadeType activeFadeType;
        private float activeFadeAmount;

        public Canvas FadeCanvas => fadeCanvas;

        public void EnsureVisibleHierarchy()
        {
            Transform current = transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                    current.gameObject.SetActive(true);

                current = current.parent;
            }

            if (fadeCanvas != null)
                fadeCanvas.enabled = true;
        }

        private void Awake()
        {
            if (fadeCanvas == null)
            {
                Debug.LogError($"{nameof(UIFadeEffectMB)}: Fade Canvas is not assigned.", this);
                return;
            }

            // 初期状態を設定する
            SetFadeType(defaultFadeType);
            ApplyFadeState(defaultFadeType, defaultFadeAmount);
        }

        private void OnRectTransformDimensionsChange()
        {
            // Window サイズ変更時も最後のフェード形状を維持する。
            ApplyFadeState(activeFadeType, activeFadeAmount);
        }

        private void OnDestroy()
        {
            if (activeFadeCancellationTokenSource == null)
            {
                return;
            }

            activeFadeCancellationTokenSource.Cancel();
            activeFadeCancellationTokenSource.Dispose();
            activeFadeCancellationTokenSource = null;
        }

        // フェードは二つの種類がある
        // 単一フェードは画面全体を一枚のImageで覆う
        // 上下フェードは画面の上下から二枚のImageで覆う
        public void SetFadeType(FadeType fadeType)
        {
            switch (fadeType)
            {
                case FadeType.Single:
                    SetImageActive(fadeImage, true);
                    SetImageActive(topFadeImage, false);
                    SetImageActive(bottomFadeImage, false);
                    break;
                case FadeType.TopBottom:
                    SetImageActive(fadeImage, false);
                    SetImageActive(topFadeImage, true);
                    SetImageActive(bottomFadeImage, true);
                    break;
            }
        }

        public void StartFade(FadeType fadeType, float amount, float duration)
        {
            StartFadeAsync(fadeType, amount, duration).Forget();
        }

        public UniTask StartFadeAsync(FadeType fadeType, float amount, float duration)
        {
            return StartFadeAsync(fadeType, amount, duration, true);
        }

        /// <summary>
        /// フェードを開始する非同期メソッド。既にフェードが進行中の場合はキャンセルしてから新しいフェードを開始する。
        /// </summary>
        /// <param name="fadeType">フェードの種類</param>
        /// <param name="amount">フェードの目標アルファ値 (0 = 完全に透明, 1 = 完全に不透明)</param>
        /// <param name="duration">フェードの時間</param>
        /// <param name="playSound">サウンドを再生するかどうか (default: true)</param>
        /// <returns></returns>
        public async UniTask StartFadeAsync(FadeType fadeType, float amount, float duration, bool playSound = true)
        {
            CancellationTokenSource fadeCancellationTokenSource = BeginNewFade();
            float clampedAmount = Mathf.Clamp01(amount);
            float safeDuration = Mathf.Max(0f, duration);

            try
            {
                SetFadeType(fadeType);
                SetCanvasVisible(true);

                // 変化量がしきい値以上ならフェード方向に応じてサウンドを再生する。
                if (playSound && AudioSystemMB.Instance != null)
                {
                    float startAmount = GetCurrentAmount(fadeType);
                    float delta = Mathf.Abs(clampedAmount - startAmount);
                    if (delta >= minFadeDeltaForSound)
                    {
                        // ターゲットが現在値より大きい = 画面が濃くなる = フェードアウト気味。
                        if (clampedAmount > startAmount && fadeOutSound != null)
                            AudioSystemMB.Instance.PlaySE(fadeOutSound);
                        else if (clampedAmount < startAmount && fadeInSound != null)
                            AudioSystemMB.Instance.PlaySE(fadeInSound);
                    }
                }

                await FadeRoutine(fadeType, clampedAmount, safeDuration, fadeCancellationTokenSource.Token);
            }
            catch (OperationCanceledException) when (fadeCancellationTokenSource.IsCancellationRequested)
            {
            }
            finally
            {
                CompleteFade(fadeCancellationTokenSource);
            }
        }

        private CancellationTokenSource BeginNewFade()
        {
            if (activeFadeCancellationTokenSource != null)
            {
                activeFadeCancellationTokenSource.Cancel();
            }

            activeFadeCancellationTokenSource = new CancellationTokenSource();
            return activeFadeCancellationTokenSource;
        }

        private void CompleteFade(CancellationTokenSource fadeCancellationTokenSource)
        {
            if (ReferenceEquals(activeFadeCancellationTokenSource, fadeCancellationTokenSource))
            {
                activeFadeCancellationTokenSource = null;
            }

            fadeCancellationTokenSource.Dispose();
        }

        private async UniTask FadeRoutine(FadeType fadeType, float targetAmount, float duration, CancellationToken cancellationToken)
        {
            float startAmount = GetCurrentAmount(fadeType);

            if (duration <= 0f)
            {
                ApplyFadeState(fadeType, targetAmount);
                SetCanvasVisible(targetAmount > 0f);
                return;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float currentAmount = Mathf.Lerp(startAmount, targetAmount, Mathf.Clamp01(elapsed / duration));
                ApplyFadeState(fadeType, currentAmount);

                elapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            ApplyFadeState(fadeType, targetAmount);
            SetCanvasVisible(targetAmount > 0f);
        }

        private float GetCurrentAmount(FadeType fadeType)
        {
            switch (fadeType)
            {
                case FadeType.Single:
                    return fadeImage != null ? fadeImage.color.a : 0f;
                case FadeType.TopBottom:
                    return GetTopBottomAmount();
                default:
                    return 0f;
            }
        }

        private float GetTopBottomAmount()
        {
            if (topFadeImage == null)
            {
                return 0f;
            }

            RectTransform referenceRect = GetTopBottomReferenceRect();
            if (referenceRect == null)
            {
                return 0f;
            }

            float referenceHeight = referenceRect.rect.height;
            if (referenceHeight <= Mathf.Epsilon)
            {
                return 0f;
            }

            return Mathf.Clamp01((topFadeImage.rectTransform.rect.height / referenceHeight) * 2f);
        }

        private void ApplyFadeState(FadeType fadeType, float amount)
        {
            activeFadeType = fadeType;
            activeFadeAmount = Mathf.Clamp01(amount);

            switch (fadeType)
            {
                case FadeType.Single:
                    ApplySingleFade(activeFadeAmount);
                    break;
                case FadeType.TopBottom:
                    ApplyTopBottomFade(activeFadeAmount);
                    break;
            }
        }

        private void ApplySingleFade(float amount)
        {
            EnsureFullScreenRect(fadeImage);
            SetImageAlpha(fadeImage, amount);
        }

        private void ApplyTopBottomFade(float amount)
        {
            if (topFadeImage == null || bottomFadeImage == null)
            {
                return;
            }

            EnsureTopBottomBarRect(topFadeImage.rectTransform, true);
            EnsureTopBottomBarRect(bottomFadeImage.rectTransform, false);

            RectTransform referenceRect = GetTopBottomReferenceRect();
            if (referenceRect == null)
            {
                return;
            }

            float coverHeight = referenceRect.rect.height * amount * 0.5f;

            topFadeImage.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0f, coverHeight);
            bottomFadeImage.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0f, coverHeight);

            float alpha = amount > 0f ? 1f : 0f;
            SetImageAlpha(topFadeImage, alpha);
            SetImageAlpha(bottomFadeImage, alpha);
        }

        private RectTransform GetTopBottomReferenceRect()
        {
            if (topFadeImage != null && topFadeImage.rectTransform.parent is RectTransform topParentRect)
            {
                return topParentRect;
            }

            if (bottomFadeImage != null && bottomFadeImage.rectTransform.parent is RectTransform bottomParentRect)
            {
                return bottomParentRect;
            }

            return fadeCanvas != null ? fadeCanvas.transform as RectTransform : null;
        }

        private static void EnsureFullScreenRect(Image image)
        {
            if (image == null)
            {
                return;
            }

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureTopBottomBarRect(RectTransform rect, bool isTop)
        {
            if (rect == null)
            {
                return;
            }

            float y = isTop ? 1f : 0f;
            rect.anchorMin = new Vector2(0f, y);
            rect.anchorMax = new Vector2(1f, y);
            rect.pivot = new Vector2(0.5f, y);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);
            rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
        }

        private void SetCanvasVisible(bool isVisible)
        {
            if (fadeCanvas == null)
            {
                return;
            }

            fadeCanvas.enabled = isVisible;
        }

        private static void SetImageActive(Image image, bool isActive)
        {
            if (image == null)
            {
                return;
            }

            image.gameObject.SetActive(isActive);
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}

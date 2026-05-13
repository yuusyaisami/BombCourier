using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BC.UI
{
    // ホバー時のアニメーション種別
    public enum HoverAnimationType
    {
        None,
        StripedShader, // BC/UI/AnimatedUI シェーダーのストライプ機能を有効化する
    }

    // クリック時のアニメーション種別
    public enum ClickAnimationType
    {
        None,
        ScalePunch, // スケールを一時的に大きくして元に戻す
    }

    // UI インタラクション（ホバー・クリック）のアニメーションを汎用的に管理するクラス。
    // このコンポーネントを Button や Image などの UI 要素に追加して使用する。
    // ホバー・クリックそれぞれ Enum で独立したアニメーション種別を選べる。
    public class UIInteractionAnimationMB : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [Header("Hover Animation")]
        [SerializeField] private HoverAnimationType hoverAnimationType = HoverAnimationType.StripedShader;

        [Header("Click Animation")]
        [SerializeField] private ClickAnimationType clickAnimationType = ClickAnimationType.ScalePunch;

        [Header("References")]
        // アニメーション対象の Graphic（Image など）。未指定の場合は同 GameObject の Graphic を使用する
        [SerializeField] private Graphic targetGraphic;

        [Header("Scale Punch Settings")]
        [SerializeField] private float clickScaleMultiplier = 1.15f;
        [SerializeField] private float clickScaleDuration   = 0.12f;

        private static readonly int StripedOverlayEnabledId = Shader.PropertyToID("_StripedOverlayEnabled");

        private Material       _materialInstance;
        private Vector3        _originalScale;
        private CancellationTokenSource _clickCts;

        // ---- Unity lifecycle ----

        private void Awake()
        {
            _originalScale = transform.localScale;

            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }
        }

        private void OnDestroy()
        {
            _clickCts?.Cancel();
            _clickCts?.Dispose();
            _clickCts = null;

            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
                _materialInstance = null;
            }
        }

        // ---- Pointer events ----

        public void OnPointerEnter(PointerEventData eventData) => HandleHoverEnter();
        public void OnPointerExit(PointerEventData eventData)  => HandleHoverExit();
        public void OnPointerClick(PointerEventData eventData) => HandleClick();

        // ---- Hover ----

        private void HandleHoverEnter()
        {
            switch (hoverAnimationType)
            {
                case HoverAnimationType.StripedShader:
                    SetStripedOverlayEnabled(true);
                    break;
            }
        }

        private void HandleHoverExit()
        {
            switch (hoverAnimationType)
            {
                case HoverAnimationType.StripedShader:
                    SetStripedOverlayEnabled(false);
                    break;
            }
        }

        private void SetStripedOverlayEnabled(bool enabled)
        {
            if (targetGraphic == null) return;

            EnsureMaterialInstance();

            if (_materialInstance != null)
            {
                _materialInstance.SetFloat(StripedOverlayEnabledId, enabled ? 1f : 0f);
            }
        }

        private void EnsureMaterialInstance()
        {
            if (_materialInstance != null) return;
            if (targetGraphic == null) return;

            _materialInstance = new Material(targetGraphic.material);
            targetGraphic.material = _materialInstance;
        }

        // ---- Click ----

        private void HandleClick()
        {
            switch (clickAnimationType)
            {
                case ClickAnimationType.ScalePunch:
                    PlayScalePunchAsync().Forget();
                    break;
            }
        }

        private async UniTaskVoid PlayScalePunchAsync()
        {
            _clickCts?.Cancel();
            _clickCts?.Dispose();
            _clickCts = new CancellationTokenSource();

            try
            {
                Vector3 targetScale  = _originalScale * clickScaleMultiplier;
                float   halfDuration = clickScaleDuration * 0.5f;

                await TweenScaleAsync(_originalScale, targetScale,  halfDuration, _clickCts.Token);
                await TweenScaleAsync(targetScale,  _originalScale, halfDuration, _clickCts.Token);
            }
            catch (OperationCanceledException)
            {
                // アニメーションがキャンセルされた場合はスケールを元に戻す
                transform.localScale = _originalScale;
            }
        }

        private async UniTask TweenScaleAsync(Vector3 from, Vector3 to, float duration, CancellationToken token)
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(duration, 0.001f);

            while (elapsed < safeDuration)
            {
                float t = Mathf.Clamp01(elapsed / safeDuration);
                transform.localScale = Vector3.Lerp(from, to, t);
                elapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            transform.localScale = to;
        }
    }
}

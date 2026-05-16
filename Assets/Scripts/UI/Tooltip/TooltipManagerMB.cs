// tooltipを管理するクラス
using BC.UI;
using UnityEngine;

namespace BC.Managers
{
    public sealed class TooltipManagerMB : MonoBehaviour
    {
        public static TooltipManagerMB Instance { get; private set; }

        [SerializeField] private TooltipAdapterMB tooltipPrefab; // ツールチップのプレハブを割り当てるためのフィールド
        [SerializeField] private Canvas tooltipCanvas; // ツールチップを表示するCanvasを割り当てるためのフィールド
        [SerializeField] private Vector2 screenOffset = new(16f, 16f); // 対象から少しずらして表示するためのオフセット
        [SerializeField] private Vector2 viewportPadding = new(12f, 12f); // 画面端に寄りすぎないための余白

        private TooltipAdapterMB currentTooltip; // 現在表示されているツールチップのインスタンスを保持するフィールド
        private RectTransform tooltipCanvasRect;
        private TooltipTargetMB currentOwner;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (!TryInitializeTooltip())
            {
                enabled = false;
                return;
            }

            currentTooltip.ImmediatelyHideTooltip(); // 最初はツールチップを非表示にしておく
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ツールチップを表示するためのメソッド
        public void ShowTooltip(TooltipTargetMB owner, string text, Transform anchorTransform)
        {
            if (owner == null || anchorTransform == null)
                return;

            if (!TryEnsureTooltipReady())
                return;

            if (string.IsNullOrWhiteSpace(text))
            {
                HideTooltip(owner);
                return;
            }

            currentOwner = owner;
            Vector2 tooltipSize = currentTooltip.ShowTooltip(text);
            currentTooltip.RectTransform?.SetAsLastSibling();
            PositionTooltip(anchorTransform.position, tooltipSize);
        }

        // ツールチップを非表示にするためのメソッド
        public void HideTooltip(TooltipTargetMB owner = null)
        {
            if (owner != null && currentOwner != owner)
                return;

            currentOwner = null;

            if (currentTooltip == null)
                return;

            currentTooltip.HideTooltip();
        }

        private bool TryInitializeTooltip()
        {
            if (tooltipPrefab == null)
            {
                Debug.LogError($"{nameof(TooltipManagerMB)}: Tooltip prefab is not assigned.", this);
                return false;
            }

            if (tooltipCanvas == null)
            {
                Debug.LogError($"{nameof(TooltipManagerMB)}: Tooltip canvas is not assigned.", this);
                return false;
            }

            tooltipCanvasRect = tooltipCanvas.GetComponent<RectTransform>();
            if (tooltipCanvasRect == null)
            {
                Debug.LogError($"{nameof(TooltipManagerMB)}: Tooltip canvas requires a RectTransform.", tooltipCanvas);
                return false;
            }

            currentTooltip = Instantiate(tooltipPrefab, tooltipCanvasRect, false); // プレハブからツールチップのインスタンスを生成し、Canvasの子にする
            if (currentTooltip == null)
            {
                Debug.LogError($"{nameof(TooltipManagerMB)}: Failed to instantiate tooltip prefab.", this);
                return false;
            }

            return true;
        }

        private bool TryEnsureTooltipReady()
        {
            return currentTooltip != null && tooltipCanvasRect != null;
        }

        private void PositionTooltip(Vector3 worldPosition, Vector2 tooltipSize)
        {
            UnityEngine.Camera canvasCamera = GetCanvasCamera();
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldPosition);
            screenPoint += screenOffset;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(tooltipCanvasRect, screenPoint, canvasCamera, out Vector2 localPoint))
                return;

            SetTooltipPosition(localPoint, tooltipSize);
        }

        private void SetTooltipPosition(Vector2 desiredLocalPoint, Vector2 tooltipSize)
        {
            RectTransform tooltipRect = currentTooltip.RectTransform;
            if (tooltipRect == null)
            {
                currentTooltip.transform.localPosition = desiredLocalPoint;
                return;
            }

            Rect canvasRect = tooltipCanvasRect.rect;
            Vector2 pivot = tooltipRect.pivot;

            float minX = canvasRect.xMin + viewportPadding.x + (tooltipSize.x * pivot.x);
            float maxX = canvasRect.xMax - viewportPadding.x - (tooltipSize.x * (1f - pivot.x));
            float minY = canvasRect.yMin + viewportPadding.y + (tooltipSize.y * pivot.y);
            float maxY = canvasRect.yMax - viewportPadding.y - (tooltipSize.y * (1f - pivot.y));

            desiredLocalPoint.x = minX > maxX ? canvasRect.center.x : Mathf.Clamp(desiredLocalPoint.x, minX, maxX);
            desiredLocalPoint.y = minY > maxY ? canvasRect.center.y : Mathf.Clamp(desiredLocalPoint.y, minY, maxY);
            tooltipRect.anchoredPosition = desiredLocalPoint;
        }

        private UnityEngine.Camera GetCanvasCamera()
        {
            if (tooltipCanvas == null || tooltipCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return tooltipCanvas.worldCamera != null ? tooltipCanvas.worldCamera : UnityEngine.Camera.main;
        }
    }
}
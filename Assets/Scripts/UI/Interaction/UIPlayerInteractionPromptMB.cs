using BC.Base;
using BC.Inputs;
using BC.Manager;
using BC.Player;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BC.UI
{
    [DisallowMultipleComponent]
    public sealed class UIPlayerInteractionPromptMB : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private bool resolvePlayerFromGameLogic = true;
        [SerializeField, ShowIf("resolvePlayerFromGameLogic", false)] private PlayerItemHandleStateMB interactionSource;

        [Header("Input")]
        [SerializeField] private InputActionReference interactAction;
        [SerializeField] private Sprite fallbackActionIcon;

        [Header("Camera")]
        [SerializeField] private UnityEngine.Camera worldCamera;

        [Header("Canvas")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform root;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Visuals")]
        [SerializeField] private Image actionIconImage;
        [SerializeField] private TMP_Text actionDetailText;

        [Header("Hold")]
        [SerializeField] private Image holdRingImage;

        [Header("Position")]
        [SerializeField] private Vector2 screenOffset = new(0f, 48f);
        [SerializeField] private Vector2 viewportPadding = new(24f, 24f);

        private RectTransform canvasRect;
        private IInteractionTarget lastInteractable;
        private InputPromptDeviceKind lastDeviceKind = InputPromptDeviceKind.Unknown;

        private void Reset()
        {
            canvas = GetComponentInParent<Canvas>();
            root = transform as RectTransform;
            canvasGroup = GetComponent<CanvasGroup>();

            if (actionDetailText == null)
                actionDetailText = GetComponentInChildren<TMP_Text>(true);
        }

        private void Awake()
        {
            ResolveCanvasReferences();
            EnsureActionDetailText();

            if (worldCamera == null)
                worldCamera = UnityEngine.Camera.main;

            HideImmediate();
        }

        private void Start()
        {
            if (!resolvePlayerFromGameLogic)
                return;

            GameLogicManagerMB gameLogic = GameLogicManagerMB.Instance;
            if (gameLogic == null)
                return;

            SetPlayer(gameLogic.PlayerInstance);
            gameLogic.OnPlayerSpawned += SetPlayer;
        }

        private void OnDestroy()
        {
            GameLogicManagerMB gameLogic = GameLogicManagerMB.Instance;
            if (gameLogic != null)
                gameLogic.OnPlayerSpawned -= SetPlayer;
        }

        private void LateUpdate()
        {
            if (interactionSource == null)
            {
                ClearActionDetailText();
                Hide();
                return;
            }

            IInteractionTarget interactable = ResolveDisplayInteractable();

            if (interactable == null)
            {
                lastInteractable = null;
                ClearActionDetailText();
                Hide();
                return;
            }

            if (!InteractionPromptResolver.TryResolveWorldPosition(interactable, out Vector3 promptWorldPosition))
            {
                ClearActionDetailText();
                Hide();
                return;
            }

            if (!TryUpdatePosition(promptWorldPosition))
            {
                ClearActionDetailText();
                Hide();
                return;
            }

            InputPromptDeviceKind currentDeviceKind = InputManagerMB.Instance != null
                ? InputManagerMB.Instance.CurrentPromptDeviceKind
                : InputPromptDeviceKind.Unknown;

            if (!ReferenceEquals(lastInteractable, interactable) || currentDeviceKind != lastDeviceKind)
            {
                RefreshIcon();
            }

            RefreshDetailText(interactable);
            RefreshHold(interactable);
            lastInteractable = interactable;
            lastDeviceKind = currentDeviceKind;
            Show();
        }

        private void SetPlayer(PlayerMB player)
        {
            interactionSource = player != null
                ? player.GetComponent<PlayerItemHandleStateMB>()
                : null;

            lastInteractable = null;
            ClearActionDetailText();
            HideImmediate();
        }

        private IInteractionTarget ResolveDisplayInteractable()
        {
            // Hold中はActiveを優先する。
            // Hold開始後にBest候補が微妙に変わっても、UIが別対象へ飛ばないようにする。
            if (interactionSource.ActiveInteractable != null)
                return interactionSource.ActiveInteractable;

            return interactionSource.CurrentBestInteractable;
        }

        private bool TryUpdatePosition(Vector3 worldPosition)
        {
            ResolveCanvasReferences();

            if (canvas == null || canvasRect == null || root == null)
                return false;

            if (worldCamera == null)
                worldCamera = UnityEngine.Camera.main;

            if (worldCamera == null)
                return false;

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPosition);

            if (screenPoint.z <= 0f)
                return false;

            if (screenPoint.x < 0f || screenPoint.x > Screen.width ||
                screenPoint.y < 0f || screenPoint.y > Screen.height)
            {
                return false;
            }

            UnityEngine.Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPoint,
                    canvasCamera,
                    out Vector2 localPoint))
            {
                return false;
            }

            localPoint += screenOffset;
            root.anchoredPosition = ClampToCanvas(localPoint);
            return true;
        }

        private Vector2 ClampToCanvas(Vector2 localPoint)
        {
            Rect rect = canvasRect.rect;

            localPoint.x = Mathf.Clamp(
                localPoint.x,
                rect.xMin + viewportPadding.x,
                rect.xMax - viewportPadding.x);

            localPoint.y = Mathf.Clamp(
                localPoint.y,
                rect.yMin + viewportPadding.y,
                rect.yMax - viewportPadding.y);

            return localPoint;
        }

        private void RefreshIcon()
        {
            if (actionIconImage == null)
            {
                return;
            }

            InputManagerMB inputManager = InputManagerMB.Instance;
            Sprite resolvedIcon = InputPromptIconResolver.ResolveIcon(inputManager, interactAction, fallbackActionIcon);
            if (!ReferenceEquals(actionIconImage.sprite, resolvedIcon))
            {
                actionIconImage.sprite = resolvedIcon;
            }

            actionIconImage.enabled = resolvedIcon != null;
        }

        private void RefreshDetailText(IInteractionTarget interactable)
        {
            EnsureActionDetailText();

            if (actionDetailText == null)
                return;

            string detailText = InteractionPromptResolver.ResolveDetailText(interactable);
            bool hasDetailText = !string.IsNullOrEmpty(detailText);

            if (!hasDetailText)
            {
                ClearActionDetailText();
                return;
            }

            if (!actionDetailText.gameObject.activeSelf)
                actionDetailText.gameObject.SetActive(true);

            if (actionDetailText.text != detailText)
                actionDetailText.text = detailText;
        }

        private void RefreshHold(IInteractionTarget interactable)
        {
            if (holdRingImage == null)
                return;

            if (interactable == null || interactable.RequiredHoldDuration <= 0f)
            {
                holdRingImage.fillAmount = 0f;
                holdRingImage.enabled = false;
                return;
            }

            holdRingImage.enabled = true;
            holdRingImage.fillAmount = interactionSource.ActiveInteractable != null
                ? interactionSource.ActiveHoldProgress
                : 0f;
        }

        private void Show()
        {
            if (root != null && !root.gameObject.activeSelf)
            {
                root.gameObject.SetActive(true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void Hide()
        {
            if (root != null && root.gameObject.activeSelf)
            {
                root.gameObject.SetActive(false);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void HideImmediate()
        {
            Hide();
            ClearActionDetailText();

            if (holdRingImage != null)
            {
                holdRingImage.fillAmount = 0f;
                holdRingImage.enabled = false;
            }
        }

        private void ClearActionDetailText()
        {
            if (actionDetailText == null)
                return;

            if (!string.IsNullOrEmpty(actionDetailText.text))
                actionDetailText.text = string.Empty;

            if (actionDetailText.gameObject.activeSelf)
                actionDetailText.gameObject.SetActive(false);
        }

        private void EnsureActionDetailText()
        {
            if (actionDetailText != null)
                return;

            ResolveCanvasReferences();

            RectTransform iconParent = actionIconImage != null
                ? actionIconImage.rectTransform.parent as RectTransform
                : null;
            RectTransform parentRect = iconParent != null ? iconParent : root;

            if (parentRect == null)
                return;

            GameObject detailTextObject = new GameObject("ActionDetailText", typeof(RectTransform));
            RectTransform detailTextRect = detailTextObject.GetComponent<RectTransform>();
            detailTextRect.SetParent(parentRect, false);
            detailTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            detailTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            detailTextRect.pivot = new Vector2(0.5f, 1f);

            Vector2 iconAnchoredPosition = actionIconImage != null
                ? actionIconImage.rectTransform.anchoredPosition
                : Vector2.zero;
            float iconHeight = actionIconImage != null
                ? Mathf.Max(24f, actionIconImage.rectTransform.rect.height)
                : 32f;

            detailTextRect.anchoredPosition = iconAnchoredPosition + new Vector2(0f, -(iconHeight * 0.5f + 8f));
            detailTextRect.sizeDelta = new Vector2(180f, 32f);

            TextMeshProUGUI detailText = detailTextObject.AddComponent<TextMeshProUGUI>();
            detailText.alignment = TextAlignmentOptions.Center;
            detailText.enableAutoSizing = true;
            detailText.fontSize = 18f;
            detailText.fontSizeMin = 12f;
            detailText.fontSizeMax = 18f;
            detailText.raycastTarget = false;
            detailText.color = Color.white;
            detailText.text = string.Empty;

            actionDetailText = detailText;
            detailTextObject.SetActive(false);
        }

        private void ResolveCanvasReferences()
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            if (root == null)
                root = transform as RectTransform;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasRect == null && canvas != null)
                canvasRect = canvas.transform as RectTransform;
        }
    }
}
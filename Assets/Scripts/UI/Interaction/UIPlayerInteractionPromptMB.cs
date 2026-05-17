using BC.Base;
using BC.Inputs;
using BC.Manager;
using BC.Player;
using Sirenix.OdinInspector;
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
        }

        private void Awake()
        {
            ResolveCanvasReferences();

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
                Hide();
                return;
            }

            IInteractionTarget interactable = ResolveDisplayInteractable();

            if (interactable == null)
            {
                lastInteractable = null;
                Hide();
                return;
            }

            if (!InteractionPromptResolver.TryResolveWorldPosition(interactable, out Vector3 promptWorldPosition))
            {
                Hide();
                return;
            }

            if (!TryUpdatePosition(promptWorldPosition))
            {
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
            InputPromptDeviceKind deviceKind = inputManager != null
                ? inputManager.ResolvePromptDeviceKind(interactAction != null ? interactAction.action : null)
                : InputPromptDeviceKind.Unknown;

            Sprite resolvedIcon = InputPromptIconResolver.ResolveIcon(inputManager, interactAction, fallbackActionIcon);
            if (!ReferenceEquals(actionIconImage.sprite, resolvedIcon))
            {
                actionIconImage.sprite = resolvedIcon;
            }

            actionIconImage.enabled = resolvedIcon != null;
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

            if (holdRingImage != null)
            {
                holdRingImage.fillAmount = 0f;
                holdRingImage.enabled = false;
            }
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
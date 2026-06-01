using System;
using BC.Base;
using BC.Manager;
using BC.UI;
using UnityEngine;

namespace BC.Managers
{
    [Serializable]
    public struct ToastRequestData
    {
        public Sprite icon;
        [TextArea] public string text;
        [Min(0.0f)] public float visibleDuration;
        [Min(0.0f)] public float fadeInDuration;
        [Min(0.0f)] public float fadeOutDuration;

        public bool HasVisibleContent => icon != null || !string.IsNullOrWhiteSpace(text);

        public ToastRequestData Sanitize()
        {
            return new ToastRequestData
            {
                icon = icon,
                text = text ?? string.Empty,
                visibleDuration = Mathf.Max(0.0f, visibleDuration),
                fadeInDuration = Mathf.Max(0.0f, fadeInDuration),
                fadeOutDuration = Mathf.Max(0.0f, fadeOutDuration),
            };
        }
    }

    [Serializable]
    public struct ScreenOverlayDisplayId : IEquatable<ScreenOverlayDisplayId>
    {
        [SerializeField] private string value;

        public ScreenOverlayDisplayId(string value)
        {
            this.value = value ?? string.Empty;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(value);

        public bool Equals(ScreenOverlayDisplayId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ScreenOverlayDisplayId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return IsValid ? Value : "(None)";
        }
    }

    public enum ScreenOverlayContentKind
    {
        Image = 0,
        Text = 1,
        Prefab = 2,
    }

    [Serializable]
    public struct ScreenOverlayShowRequestData
    {
        private const int DefaultsVersionEnabled = 1;

        public ScreenOverlayDisplayId displayId;
        public ScreenOverlayContentKind contentKind;
        public Vector2 anchoredPosition;
        public int sortOrder;

        public Sprite sprite;
        public Vector2 size;
        public Color imageColor;

        [TextArea]
        public string text;
        [Min(0f)]
        public float fontSize;
        public Color textColor;

        public GameObject prefab;

        [SerializeField, HideInInspector] private int defaultsVersion;

        public void EnsureDefaultsInitialized()
        {
            if (defaultsVersion >= DefaultsVersionEnabled)
                return;

            if (imageColor == default)
                imageColor = Color.white;

            if (textColor == default)
                textColor = Color.white;

            if (fontSize <= 0f)
                fontSize = 36f;

            if (size == Vector2.zero)
                size = new Vector2(128f, 128f);

            defaultsVersion = DefaultsVersionEnabled;
        }

        public bool HasVisibleContent =>
            contentKind switch
            {
                ScreenOverlayContentKind.Image => sprite != null,
                ScreenOverlayContentKind.Text => !string.IsNullOrWhiteSpace(text),
                ScreenOverlayContentKind.Prefab => prefab != null,
                _ => false,
            };

        public ScreenOverlayShowRequestData Sanitize()
        {
            EnsureDefaultsInitialized();

            return new ScreenOverlayShowRequestData
            {
                displayId = displayId,
                contentKind = contentKind,
                anchoredPosition = anchoredPosition,
                sortOrder = sortOrder,
                sprite = sprite,
                size = new Vector2(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y)),
                imageColor = imageColor,
                text = text ?? string.Empty,
                fontSize = Mathf.Max(1f, fontSize),
                textColor = textColor,
                prefab = prefab,
                defaultsVersion = DefaultsVersionEnabled,
            };
        }
    }

    [Serializable]
    public struct ScreenOverlayHideRequestData
    {
        public ScreenOverlayDisplayId displayId;
    }

    [DisallowMultipleComponent]
    public sealed class ToastSystemManagerMB : MonoBehaviour
    {
        public static ToastSystemManagerMB Instance { get; private set; }

        [Header("References")]
        [SerializeField] private UIManagerMB uiManager;
        [SerializeField] private UIToastStackMB toastStackUI;

        [Header("High Jump Toast")]
        [SerializeField]
        private ToastRequestData highJumpToast = new ToastRequestData
        {
            text = "ハイジャンプ!",
            visibleDuration = 1.1f,
            fadeInDuration = 0.12f,
            fadeOutDuration = 0.2f,
        };

        private GameLogicManagerMB gameLogicManager;
        private EntityMoveMotorMB playerMoveMotor;
        private bool hasLoggedMissingToastStack;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                EnsureToastStackUI();
                return;
            }

            if (Instance != this)
                Destroy(gameObject);
        }

        private void Start()
        {
            TryBindGameLogic();
        }

        private void Update()
        {
            if (toastStackUI == null)
                EnsureToastStackUI();

            if (gameLogicManager == null)
                TryBindGameLogic();
        }

        private void OnDestroy()
        {
            UnbindMoveMotor();
            UnbindGameLogic();

            if (Instance == this)
                Instance = null;
        }

        public void ShowToast(ToastRequestData request)
        {
            ToastRequestData sanitizedRequest = request.Sanitize();
            if (!sanitizedRequest.HasVisibleContent)
                return;

            EnsureToastStackUI();
            toastStackUI?.ShowToast(sanitizedRequest);
        }

        public void ShowHighJumpToast()
        {
            ShowToast(highJumpToast);
        }

        private void TryBindGameLogic()
        {
            GameLogicManagerMB manager = GameLogicManagerMB.Instance;
            if (manager == null)
                return;

            if (ReferenceEquals(gameLogicManager, manager))
            {
                HandlePlayerSpawned(gameLogicManager.PlayerInstance);
                return;
            }

            UnbindGameLogic();
            gameLogicManager = manager;
            gameLogicManager.OnPlayerSpawned += HandlePlayerSpawned;
            HandlePlayerSpawned(gameLogicManager.PlayerInstance);
        }

        private void UnbindGameLogic()
        {
            if (gameLogicManager != null)
                gameLogicManager.OnPlayerSpawned -= HandlePlayerSpawned;

            gameLogicManager = null;
        }

        private void HandlePlayerSpawned(PlayerMB player)
        {
            BindMoveMotor(player != null ? player.MoveController : null);
        }

        private void BindMoveMotor(EntityMoveMotorMB moveMotor)
        {
            if (ReferenceEquals(playerMoveMotor, moveMotor))
                return;

            UnbindMoveMotor();
            playerMoveMotor = moveMotor;

            if (playerMoveMotor != null)
                playerMoveMotor.CushionHighJumped += HandleCushionHighJumped;
        }

        private void UnbindMoveMotor()
        {
            if (playerMoveMotor != null)
                playerMoveMotor.CushionHighJumped -= HandleCushionHighJumped;

            playerMoveMotor = null;
        }

        private void HandleCushionHighJumped(CushionHighJumpEventData eventData)
        {
            ShowHighJumpToast();
        }

        private void EnsureToastStackUI()
        {
            if (toastStackUI != null)
                return;

            uiManager ??= UIManagerMB.Instance;
            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManagerMB>(FindObjectsInactive.Include);

            if (uiManager != null)
                toastStackUI = uiManager.ToastStackUI;

            toastStackUI ??= GetComponentInChildren<UIToastStackMB>(true);
            if (toastStackUI != null)
                return;

            if (!hasLoggedMissingToastStack)
            {
                hasLoggedMissingToastStack = true;
                Debug.LogWarning($"{nameof(ToastSystemManagerMB)}: {nameof(UIToastStackMB)} was not found in scene/UIManager. Toast表示は無効です。", this);
            }
        }
    }

}

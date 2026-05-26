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
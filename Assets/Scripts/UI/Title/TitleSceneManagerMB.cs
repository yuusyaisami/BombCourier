using System.Threading;
using BC.Audio;
using BC.Manager;
using BC.Managers;
using BC.UI;
using BC.UI.Components;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.UI.Title
{
    // タイトルシーンのページルーター。
    // GameRoot → TitleMain → StageSelect のナビゲーションを一元管理する。
    // UISettingMB はタイトル専用の別インスタンスをここから開閉する。
    public sealed class TitleSceneManagerMB : MonoBehaviour
    {
        public static TitleSceneManagerMB Instance { get; private set; }

        [Header("Pages")]
        [SerializeField] private UIGameRootPageMB gameRootPage;
        [SerializeField] private UITitleMainPageMB titleMainPage;
        [SerializeField] private UIStageSelectPageMB stageSelectPage;

        [Header("Settings Overlay")]
        [Tooltip("タイトルシーン用の UISettingMB インスタンス。")]
        [SerializeField] private UISettingMB settingPanel;

        private bool isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 初期状態: GameRoot のみ表示、TitleMain は非表示のまま事前準備して初回表示スパイクを避ける。
            titleMainPage?.PrewarmInitialVisuals();
            if (stageSelectPage != null) stageSelectPage.gameObject.SetActive(false);
        }

        private void Start()
        {
            // シーン開始時に EventSystem/入力モジュールを project-wide アクションへ統一しておく。
            // これでモジュール駆動の標準ナビと、UIStageSelectNavigationMB のカスタムナビが同じアセットを使う。
            UINavigationBootstrap.EnsureConfigured();

            // GameRoot ページを表示してゲーム開始を待つ
            ShowGameRootAsync(destroyCancellationToken).Forget();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ----------------------------------------------------------------
        // Navigation API
        // ----------------------------------------------------------------

        /// <summary>GameRoot ページを表示する（Start 時に内部呼び出し）。</summary>
        private async UniTaskVoid ShowGameRootAsync(CancellationToken ct)
        {
            if (gameRootPage == null) return;
            InputManagerMB.EnsureInstance().UnlockCursor();
            await gameRootPage.ShowAsync(ct);
        }

        /// <summary>GameRoot → TitleMain へ遷移する。</summary>
        public async UniTask GoToMainPageAsync(bool resetBGM, CancellationToken ct)
        {
            if (!TryLockTransition()) return;

            try
            {
                InputManagerMB.EnsureInstance().UnlockCursor();

                if (gameRootPage != null && gameRootPage.IsShowing)
                    await gameRootPage.HideAsync(ct);

                if (stageSelectPage != null && stageSelectPage.IsShowing)
                    await stageSelectPage.HideAsync(ct);

                // タイトルメインページ表示と同時にタイトル BGM を再生する。
                if (resetBGM)
                {
                    GameSoundDataManagerMB.Instance?.PlayTitleBGM();
                }

                if (titleMainPage != null)
                    await titleMainPage.ShowAsync(ct);
            }
            finally
            {
                UnlockTransition();
            }
        }

        /// <summary>TitleMain → StageSelect へ遷移する。</summary>
        public async UniTask GoToStageSelectAsync(CancellationToken ct)
        {
            if (!TryLockTransition()) return;

            try
            {
                InputManagerMB.EnsureInstance().UnlockCursor();

                if (titleMainPage != null && titleMainPage.IsShowing)
                    await titleMainPage.HideAsync(ct);

                if (stageSelectPage != null)
                    await stageSelectPage.ShowAsync(ct);
            }
            finally
            {
                UnlockTransition();
            }
        }

        /// <summary>設定パネルを開く。TitleMain は閉じてから表示する。</summary>
        public async UniTask OpenSettingsAsync(CancellationToken ct)
        {
            if (!TryLockTransition()) return;

            if (settingPanel == null)
            {
                Debug.LogWarning($"[{nameof(TitleSceneManagerMB)}] settingPanel is not assigned.", this);
                return;
            }

            try
            {
                InputManagerMB.EnsureInstance().UnlockCursor();

                if (titleMainPage != null && titleMainPage.IsShowing)
                    await titleMainPage.HideAsync(ct);

                settingPanel.gameObject.SetActive(true);
                await settingPanel.ShowPanelAsync().AttachExternalCancellation(ct).SuppressCancellationThrow();
            }
            finally
            {
                UnlockTransition();
            }
        }

        /// <summary>設定を閉じたあとに TitleMain を復帰させる。</summary>
        public async UniTask ReturnToTitleMainFromSettingsAsync(CancellationToken ct)
        {
            if (!TryLockTransition()) return;

            try
            {
                InputManagerMB.EnsureInstance().UnlockCursor();

                if (settingPanel != null && settingPanel.gameObject.activeSelf)
                    settingPanel.gameObject.SetActive(true);

                if (gameRootPage != null && gameRootPage.IsShowing)
                    await gameRootPage.HideAsync(ct);

                if (stageSelectPage != null && stageSelectPage.IsShowing)
                    await stageSelectPage.HideAsync(ct);

                if (titleMainPage != null)
                    await titleMainPage.RestoreFromSettingsAsync(ct);
            }
            finally
            {
                UnlockTransition();
            }
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private bool TryLockTransition()
        {
            if (isTransitioning)
            {
                Debug.LogWarning($"[{nameof(TitleSceneManagerMB)}] Transition already in progress. Ignored.");
                return false;
            }
            isTransitioning = true;
            return true;
        }

        private void UnlockTransition()
        {
            isTransitioning = false;
        }
    }
}

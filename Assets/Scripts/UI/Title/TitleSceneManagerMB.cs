using System.Threading;
using BC.Audio;
using BC.Managers;
using BC.UI;
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

            // 初期状態: GameRoot のみ表示、他は非表示
            if (titleMainPage != null) titleMainPage.gameObject.SetActive(false);
            if (stageSelectPage != null) stageSelectPage.gameObject.SetActive(false);
        }

        private void Start()
        {
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
            await gameRootPage.ShowAsync(ct);
        }

        /// <summary>GameRoot → TitleMain へ遷移する。</summary>
        public async UniTask GoToMainPageAsync(CancellationToken ct)
        {
            if (!TryLockTransition()) return;

            try
            {
                if (gameRootPage != null && gameRootPage.IsShowing)
                    await gameRootPage.HideAsync(ct);

                if (stageSelectPage != null && stageSelectPage.IsShowing)
                    await stageSelectPage.HideAsync(ct);

                // タイトルメインページ表示と同時にタイトル BGM を再生する。
                GameBGMManagerMB.Instance?.PlayTitleBGM();

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

        /// <summary>設定パネルを開く（オーバーレイ表示）。</summary>
        public async UniTask OpenSettingsAsync(CancellationToken ct)
        {
            if (settingPanel == null)
            {
                Debug.LogWarning($"[{nameof(TitleSceneManagerMB)}] settingPanel is not assigned.", this);
                return;
            }

            // TitleMain は非表示にせず上に重ねる
            settingPanel.gameObject.SetActive(true);
            await settingPanel.ShowPanelAsync().AttachExternalCancellation(ct).SuppressCancellationThrow();
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

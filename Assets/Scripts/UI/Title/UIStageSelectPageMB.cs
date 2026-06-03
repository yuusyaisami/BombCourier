using System.Collections.Generic;
using System.Threading;
using BC.Base;
using BC.Rendering.Transition;
using BC.Stage;
using BC.UI.Components;
using BC.UI.Effect;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BC.UI.Title
{
    // ステージセレクトページ。
    // 2 ページ構成 (Page1: Stage 1-8 / Page2: Stage 9-12)。
    // ステージアイテムのフォーカス変化に連動して背景画像をクロスフェードする。
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIStageSelectPageMB : TitlePageBase
    {
        [Header("Stage Items")]
        [Tooltip("Page1: 0-7, Page2: 8-11 の順番で設定すること。")]
        [SerializeField] private List<UIStageSelectItemMB> stageItems = new();

        [Header("Page Containers")]
        [Tooltip("Page1 の RectTransform (スライドアニメーションの対象)。")]
        [SerializeField] private RectTransform page1Container;
        [Tooltip("Page2 の RectTransform。")]
        [SerializeField] private RectTransform page2Container;
        [SerializeField, Min(0f)] private float pageSlideWidth = 1920f;
        [SerializeField, Min(0f)] private float pageSlideDuration = 0.35f;

        [Header("Background")]
        [SerializeField] private UIScreenTransitionImageMB backgroundTransitionImage;
        [SerializeField, Min(0f)] private float bgCrossFadeDuration = 0.4f;

        [Header("Stage Detail")]
        [SerializeField] private TextMeshProUGUI stageTitleText;
        [SerializeField] private Image clearedRewardImage;
        [SerializeField] private Sprite clearedRewardEarnedSprite;
        [SerializeField] private Image bonusRewardImage;
        [SerializeField] private Sprite bonusRewardEarnedSprite;
        [SerializeField] private Image fastClearRewardImage;
        [SerializeField] private Sprite fastClearRewardEarnedSprite;
        [SerializeField] private Sprite rewardUncollectedSprite;

        [Header("Stage Registry")]
        [SerializeField] private StageRegistrySO stageRegistry;

        [Header("Screen Transition")]
        [SerializeField] private ScreenTransitionProfileSO stageLoadTransitionProfile;

        [Header("Navigation")]
        [SerializeField] private UIStageSelectNavigationMB navigationMB;

        [Header("Arrow Buttons")]
        [SerializeField] private UIButtonMB prevPageButton;
        [SerializeField] private UIButtonMB nextPageButton;

        [Header("Back Button")]
        [SerializeField] private UIButtonMB backButton;

        [Header("Page Animation")]
        [SerializeField, Min(0f)] private float pageInDuration = 0.35f;
        [SerializeField, Min(0f)] private float pageOutDuration = 0.3f;

        [Header("Tutorial")]
        [Tooltip("チュートリアル確認モーダル。null のとき対象ステージでもモーダルを表示しない。")]
        [SerializeField] private UITutorialConfirmModalMB tutorialConfirmModal;

        private CanvasGroup pageCanvasGroup;
        private int currentPageIndex;
        private bool isSwitchingPage;
        private bool stageItemsBound;
        private const int GameSceneBuildIndex = 1;

        public bool IsModalInputActive => tutorialConfirmModal != null && tutorialConfirmModal.IsOpen;

        private void Awake()
        {
            pageCanvasGroup = GetComponent<CanvasGroup>();
            pageCanvasGroup.alpha = 0f;
            pageCanvasGroup.interactable = false;

            ResetStageDetailPanel();

            // ステージアイテム初期化
            SetupStageItems();

            // ページコンテナ初期配置
            SetupPageContainers();

            // ボタンイベント
            if (prevPageButton != null) prevPageButton.AddClickListener(() => SwitchToPageAsync(currentPageIndex - 1, destroyCancellationToken).Forget());
            if (nextPageButton != null) nextPageButton.AddClickListener(() => SwitchToPageAsync(currentPageIndex + 1, destroyCancellationToken).Forget());
            if (backButton != null) backButton.AddClickListener(OnBackButtonClicked);
        }

        private void SetupStageItems()
        {
            if (stageRegistry == null) return;
            List<StageData> stages = stageRegistry.StageData;

            // navigationMB に全アイテムを渡す
            if (navigationMB != null)
                navigationMB.Initialize(stageItems.ToArray());

            for (int i = 0; i < stageItems.Count; i++)
            {
                UIStageSelectItemMB item = stageItems[i];
                if (item == null) continue;

                StageData data = (i < stages.Count) ? stages[i] : null;
                bool unlocked = TitleStageProgressServiceMB.IsUnlockedPersisted(i);
                TitleStageProgressServiceMB.StageProgressData stageProgress = TitleStageProgressServiceMB.GetStageProgressPersisted(i);

                item.Setup(data, i, unlocked, stageProgress.TotalStarCount);

                if (!stageItemsBound)
                {
                    item.OnFocused += OnItemFocused;
                    item.OnSelected += OnItemSelected;
                }
            }

            stageItemsBound = true;
        }

        private void SetupPageContainers()
        {
            if (page1Container != null)
                page1Container.anchoredPosition = Vector2.zero;

            if (page2Container != null)
                page2Container.anchoredPosition = new Vector2(pageSlideWidth, 0f);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            IsShowing = true;
            gameObject.SetActive(true);
            currentPageIndex = 0;
            pageCanvasGroup.blocksRaycasts = true;
            SetupPageContainers();
            SetupStageItems();

            // ページ1の最初の解放済みアイテムにフォーカス
            if (navigationMB != null)
            {
                int firstUnlocked = 0;
                for (int i = 0; i < stageItems.Count; i++)
                {
                    if (stageItems[i] != null && stageItems[i].IsUnlocked)
                    {
                        firstUnlocked = i;
                        break;
                    }
                }
                navigationMB.SetFocus(firstUnlocked);
                SetInitialBackgroundForItem(firstUnlocked);
            }

            await pageCanvasGroup
                .DOFade(1f, pageInDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .WithCancellation(ct);

            pageCanvasGroup.interactable = true;
            UpdateArrowButtons();
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            pageCanvasGroup.interactable = false;
            pageCanvasGroup.blocksRaycasts = false;
            ResetStageDetailPanel();

            await pageCanvasGroup
                .DOFade(0f, pageOutDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .WithCancellation(ct);

            IsShowing = false;
            gameObject.SetActive(false);
        }

        /// <summary>指定ページインデックスにスライドアニメーションで切り替える。</summary>
        public async UniTask SwitchToPageAsync(int targetPage, CancellationToken ct)
        {
            if (isSwitchingPage) return;
            if (targetPage == currentPageIndex) return;
            if (targetPage < 0 || targetPage > 1) return;

            isSwitchingPage = true;
            pageCanvasGroup.interactable = false;

            float direction = (targetPage > currentPageIndex) ? -1f : 1f;

            RectTransform current = (currentPageIndex == 0) ? page1Container : page2Container;
            RectTransform next = (targetPage == 0) ? page1Container : page2Container;

            // next コンテナを画面外に配置してからスライドイン
            if (next != null)
                next.anchoredPosition = new Vector2(-direction * pageSlideWidth, 0f);

            UniTask slideOut = (current != null)
                ? current.DOAnchorPos(new Vector2(direction * pageSlideWidth, 0f), pageSlideDuration)
                    .SetEase(Ease.OutCubic).SetUpdate(true).WithCancellation(ct)
                : UniTask.CompletedTask;

            UniTask slideIn = (next != null)
                ? next.DOAnchorPos(Vector2.zero, pageSlideDuration)
                    .SetEase(Ease.OutCubic).SetUpdate(true).WithCancellation(ct)
                : UniTask.CompletedTask;

            await UniTask.WhenAll(slideOut, slideIn);

            currentPageIndex = targetPage;
            isSwitchingPage = false;
            pageCanvasGroup.interactable = true;
            UpdateArrowButtons();
        }

        private void OnItemFocused(UIStageSelectItemMB item)
        {
            CrossFadeBackground(item.StageData?.backgroundSprite).Forget();
            RefreshStageDetailPanel(item);
        }

        private void OnItemSelected(UIStageSelectItemMB item)
        {
            LoadStageAsync(item.StageIndex).Forget();
        }

        private async UniTaskVoid LoadStageAsync(int stageIndex)
        {
            pageCanvasGroup.interactable = false;

            ApplicationKernelMB appKernelMB = ApplicationKernelMB.Instance;
            KernelValueStoreService kernelStore = appKernelMB != null ? appKernelMB.Kernel?.KernelValueStore : null;
            kernelStore?.Set(ValueKeys.Kernel.Stage.SelectedStageIndex, Mathf.Max(0, stageIndex));

            // チュートリアル確認：対象ステージなら「はい/いいえ」をユーザーに問い合わせる
            bool isTutorial = false;
            if (tutorialConfirmModal != null && tutorialConfirmModal.ShouldShowForStage(stageIndex))
            {
                pageCanvasGroup.blocksRaycasts = false;
                isTutorial = await tutorialConfirmModal.ShowConfirmAsync(destroyCancellationToken);
            }
            kernelStore?.Set(ValueKeys.AppSettings.TutorialMode, isTutorial);

            // SceneManagerService 経由でロードシーンを表示してから遷移
            SceneManagerService sceneMgr = ApplicationKernelMB.Instance?.Kernel?.SceneManager;
            if (sceneMgr != null)
            {
                ScreenTransitionRequest transitionRequest = new(
                    stageLoadTransitionProfile,
                    overrideDuration: null,
                    captureFromCurrentFrame: true,
                    waitUntilToReady: true);

                await sceneMgr.LoadSceneWithTransitionAsync(GameSceneBuildIndex, transitionRequest, LoadSceneMode.Single, destroyCancellationToken);
                return;
            }

            // フォールバック: LoadingScene だけ表示してから直接ロード
            LoadingSceneService loadingScene = ApplicationKernelMB.Instance?.Kernel?.LoadingScene;
            if (loadingScene != null)
                await loadingScene.ShowAsync();

            SceneManager.LoadScene(GameSceneBuildIndex);
        }

        private async UniTaskVoid CrossFadeBackground(Sprite newSprite)
        {
            if (backgroundTransitionImage == null)
                return;

            if (newSprite == null)
                newSprite = backgroundTransitionImage.CurrentSprite;

            if (newSprite == null)
                return;

            await backgroundTransitionImage.TransitionToSpriteAsync(
                newSprite,
                bgCrossFadeDuration,
                null,
                destroyCancellationToken);
        }

        private void SetInitialBackgroundForItem(int index)
        {
            if (backgroundTransitionImage == null || stageItems == null || index < 0 || index >= stageItems.Count)
                return;

            UIStageSelectItemMB item = stageItems[index];
            if (item == null || item.StageData == null || item.StageData.backgroundSprite == null)
                return;

            backgroundTransitionImage.SetImmediateSprite(item.StageData.backgroundSprite);
        }

        private void UpdateArrowButtons()
        {
            if (prevPageButton != null) prevPageButton.Interactable = currentPageIndex > 0;
            if (nextPageButton != null) nextPageButton.Interactable = currentPageIndex < 1;
        }

        private void OnBackButtonClicked()
        {
            TitleSceneManagerMB.Instance?.GoToMainPageAsync(false, destroyCancellationToken).Forget();
        }

        private void RefreshStageDetailPanel(UIStageSelectItemMB item)
        {
            if (item == null)
            {
                ResetStageDetailPanel();
                return;
            }

            if (stageTitleText != null)
            {
                stageTitleText.text = item.StageData != null ? item.StageData.stageName ?? string.Empty : string.Empty;
            }

            TitleStageProgressServiceMB.StageProgressData progress = TitleStageProgressServiceMB.GetStageProgressPersisted(item.StageIndex);
            ApplyRewardSprite(clearedRewardImage, progress.IsCleared, clearedRewardEarnedSprite);
            ApplyRewardSprite(bonusRewardImage, progress.HasBonusReward, bonusRewardEarnedSprite);
            ApplyRewardSprite(fastClearRewardImage, progress.HasFastClearReward, fastClearRewardEarnedSprite);
        }

        private void ResetStageDetailPanel()
        {
            if (stageTitleText != null)
            {
                stageTitleText.text = string.Empty;
            }

            ApplyRewardSprite(clearedRewardImage, false, clearedRewardEarnedSprite);
            ApplyRewardSprite(bonusRewardImage, false, bonusRewardEarnedSprite);
            ApplyRewardSprite(fastClearRewardImage, false, fastClearRewardEarnedSprite);
        }

        private void ApplyRewardSprite(Image image, bool earned, Sprite earnedSprite)
        {
            if (image == null)
                return;

            image.sprite = earned ? earnedSprite : rewardUncollectedSprite;
            image.enabled = image.sprite != null;
        }
    }
}

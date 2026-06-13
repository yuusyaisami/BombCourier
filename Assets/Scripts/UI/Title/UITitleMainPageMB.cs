using System.Threading;
using BC.Audio;
using BC.Rendering.Transition;
using BC.Stage;
using BC.UI.Components;
using BC.UI.Effect;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace BC.UI.Title
{
    // タイトルメインページ。ロゴアニメーション、ゲームプレイ/設定ボタン、右パネル画像を管理する。
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UITitleMainPageMB : TitlePageBase
    {
        [Header("Logo")]
        [SerializeField] private RectTransform logoTransform;
        [SerializeField, Min(0f)] private float logoSpinDuration = 0.7f;
        [SerializeField, Min(0f)] private float logoSpinTurns = 3f;
        [SerializeField] private float logoFinalAngle = 20f; // ぐるぐるして最終的に傾く角度

        [Header("Buttons")]
        [SerializeField] private CanvasGroup buttonsCanvasGroup;
        [SerializeField] private UIButtonMB playButton;
        [SerializeField] private UIButtonMB settingsButton;
        [SerializeField, Min(0f)] private float buttonsFadeDuration = 0.4f;

        [Header("Right Panel")]
        [SerializeField] private UIScreenTransitionImageMB rightPanelTransitionImage;
        [SerializeField] private TitleWeightedSpriteSO titleWeightedSprites;
        [SerializeField] private Sprite settingsPanelSprite;
        [SerializeField, Min(0f)] private float rightPanelCrossFadeDuration = 0.3f;
        [SerializeField] private ScreenTransitionProfileSO rightPanelTransitionProfile;

        [Header("Page Animation")]
        [SerializeField, Min(0f)] private float pageInDuration = 0.35f;
        [SerializeField, Min(0f)] private float pageOutDuration = 0.3f;

        [Header("Sound")]
        [Tooltip("ロゴを回転させるときの SE です。")]
        [SerializeField] private AudioDataSO logoSpinSound;

        [Header("All Clear Reward")]
        [Tooltip("全ステージ星3コンプ達成時のみ表示する特典パネルへの入口ボタン。")]
        [SerializeField] private UIButtonMB allClearButton;
        [Tooltip("特典パネルを一度も開いていない間だけ表示する「NEW」バッジ。開封後は永続的に非表示。")]
        [SerializeField] private GameObject allClearNewBadge;
        [Tooltip("星3コンプ判定に使うステージ総数の取得元。")]
        [SerializeField] private StageRegistrySO stageRegistry;

        private CanvasGroup pageCanvasGroup;
        private bool settingsFocused;
        private Sprite preparedInitialRightPanelSprite;

        private void Awake()
        {
            EnsureReferences();
            ApplyHiddenCanvasState();
            ResetLogoTransform();

            // ボタンイベント登録
            if (playButton != null) playButton.AddClickListener(OnPlayButtonClicked);
            if (settingsButton != null) settingsButton.AddClickListener(OnSettingsButtonClicked);
            if (allClearButton != null) allClearButton.AddClickListener(OnAllClearButtonClicked);

            if (settingsButton != null)
            {
                settingsButton.Focused -= OnSettingsButtonFocused;
                settingsButton.Focused += OnSettingsButtonFocused;
                settingsButton.Deselected -= OnSettingsButtonDeselected;
                settingsButton.Deselected += OnSettingsButtonDeselected;
            }
        }

        public void PrewarmInitialVisuals()
        {
            EnsureReferences();
            ApplyHiddenCanvasState();
            ResetLogoTransform();

            // タイトルが見え始める前（プリウォーム時点）から特典ボタン/バッジを正しい状態にしておく。
            RefreshAllClearButtonState();

            CachePreparedInitialRightPanelSprite();

            if (preparedInitialRightPanelSprite != null && rightPanelTransitionImage != null)
                rightPanelTransitionImage.SetImmediateSprite(preparedInitialRightPanelSprite);

            Canvas.ForceUpdateCanvases();
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            EnsureReferences();
            IsShowing = true;
            gameObject.SetActive(true);
            ApplyHiddenCanvasState();
            ResetLogoTransform();

            // 特典ボタン/バッジの状態を、ページが見え始める前に確定する。
            // （イントロ演出のフェードイン中に、未解放のボタンが一瞬見えてしまう問題の対策）
            RefreshAllClearButtonState();

            await pageCanvasGroup
                .DOFade(1f, pageInDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .WithCancellation(ct);

            // 右パネルの初期画像を設定
            SetInitialRightPanelSprite();

            // ロゴアニメーション ("ぐるぐるバン！")
            await PlayLogoIntroAsync(ct);
            if (logoSpinSound != null)
                AudioSystemMB.Instance?.PlaySE(logoSpinSound);

            // ボタンをフェードイン
            if (buttonsCanvasGroup != null)
            {
                await buttonsCanvasGroup
                    .DOFade(1f, buttonsFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .WithCancellation(ct);

                buttonsCanvasGroup.interactable = true;
                buttonsCanvasGroup.blocksRaycasts = true;
            }

            pageCanvasGroup.interactable = true;
            pageCanvasGroup.blocksRaycasts = true;

            // ゲームプレイボタンに初期フォーカスを当てる
            UINavigationBootstrap.EnsureConfigured();
            if (playButton != null)
                playButton.Select();

        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            EnsureReferences();
            pageCanvasGroup.interactable = false;
            pageCanvasGroup.blocksRaycasts = false;
            if (buttonsCanvasGroup != null)
            {
                buttonsCanvasGroup.interactable = false;
                buttonsCanvasGroup.blocksRaycasts = false;
            }

            await pageCanvasGroup
                .DOFade(0f, pageOutDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .WithCancellation(ct);

            IsShowing = false;
            gameObject.SetActive(false);
        }

        public async UniTask RestoreFromSettingsAsync(CancellationToken ct)
        {
            EnsureReferences();
            IsShowing = true;
            gameObject.SetActive(true);
            pageCanvasGroup.alpha = 0f;
            pageCanvasGroup.interactable = false;
            pageCanvasGroup.blocksRaycasts = false;

            // ページが見え始める前に特典ボタン/バッジを確定する。
            // （特に「削除直後の復帰」でボタンが一瞬残らないよう、フェードイン前に隠す）
            RefreshAllClearButtonState();

            await pageCanvasGroup
                .DOFade(1f, pageInDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .WithCancellation(ct);

            SetInitialRightPanelSprite();

            if (buttonsCanvasGroup != null)
            {
                buttonsCanvasGroup.alpha = 0f;
                buttonsCanvasGroup.interactable = false;
                buttonsCanvasGroup.blocksRaycasts = false;
                await buttonsCanvasGroup
                    .DOFade(1f, buttonsFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .WithCancellation(ct);

                buttonsCanvasGroup.interactable = true;
                buttonsCanvasGroup.blocksRaycasts = true;
            }

            pageCanvasGroup.interactable = true;
            pageCanvasGroup.blocksRaycasts = true;

            if (playButton != null)
                playButton.Select();

        }

        private async UniTask PlayLogoIntroAsync(CancellationToken ct)
        {
            if (logoTransform == null) return;

            logoTransform.localScale = Vector3.zero;
            logoTransform.localEulerAngles = Vector3.zero;

            float totalRotation = 360f * logoSpinTurns + logoFinalAngle;

            // スケールと回転を同時にアニメーション
            UniTask scaleTask = logoTransform
                .DOScale(Vector3.one, logoSpinDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .WithCancellation(ct);

            UniTask rotateTask = logoTransform
                .DOLocalRotate(new Vector3(0f, 0f, totalRotation), logoSpinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .WithCancellation(ct);

            await UniTask.WhenAll(scaleTask, rotateTask);
        }

        private void SetInitialRightPanelSprite()
        {
            Sprite sprite = ConsumePreparedInitialRightPanelSprite();

            if (sprite != null && rightPanelTransitionImage != null)
                rightPanelTransitionImage.SetImmediateSprite(sprite);
        }

        private void SetSettingsFocus(bool focused)
        {
            if (settingsFocused == focused) return;
            settingsFocused = focused;

            Sprite targetSprite = focused ? settingsPanelSprite
                : (titleWeightedSprites != null ? titleWeightedSprites.PickRandom() : null);

            TransitionRightPanelAsync(targetSprite).Forget();
        }

        private async UniTaskVoid TransitionRightPanelAsync(Sprite newSprite)
        {
            if (rightPanelTransitionImage == null)
                return;

            if (newSprite == null)
                newSprite = rightPanelTransitionImage.CurrentSprite;

            if (newSprite == null)
                return;

            await rightPanelTransitionImage.TransitionToSpriteAsync(
                newSprite,
                rightPanelCrossFadeDuration,
                rightPanelTransitionProfile,
                destroyCancellationToken);
        }

        private void OnPlayButtonClicked()
        {
            TitleSceneManagerMB manager = TitleSceneManagerMB.Instance;
            if (manager == null) return;
            manager.GoToStageSelectAsync(destroyCancellationToken).Forget();
        }

        private void OnSettingsButtonClicked()
        {
            TitleSceneManagerMB manager = TitleSceneManagerMB.Instance;
            if (manager == null) return;
            manager.OpenSettingsAsync(destroyCancellationToken).Forget();
        }

        // 全ステージ星3コンプ達成時のみ特典ボタンを表示し、未開封の間だけ NEW バッジを併記する。
        // ShowAsync / RestoreFromSettingsAsync の表示のたびに呼び、達成・開封・進捗削除の変化を反映する。
        private void RefreshAllClearButtonState()
        {
            int stageCount = stageRegistry != null && stageRegistry.StageData != null
                ? stageRegistry.StageData.Count
                : 0;

            bool unlocked = allClearButton != null
                && TitleStageProgressServiceMB.AreAllStagesFullyCompleted(stageCount);

            if (allClearButton != null)
                allClearButton.gameObject.SetActive(unlocked);

            // バッジは「達成済み かつ 未開封」のときだけ表示。条件未達なら常に隠す。
            if (allClearNewBadge != null)
                allClearNewBadge.SetActive(unlocked && !TitleStageProgressServiceMB.HasSeenAllClearReward());
        }

        private void OnAllClearButtonClicked()
        {
            // 開封フラグを立て、NEW バッジを即時に隠す（以後は永続的に出さない）。
            TitleStageProgressServiceMB.MarkAllClearRewardSeen();
            if (allClearNewBadge != null)
                allClearNewBadge.SetActive(false);

            TitleSceneManagerMB manager = TitleSceneManagerMB.Instance;
            if (manager == null) return;
            manager.OpenAllClearAsync(destroyCancellationToken).Forget();
        }

        private void OnSettingsButtonFocused(UIButtonMB button)
        {
            SetSettingsFocus(true);
        }

        private void OnSettingsButtonDeselected(UIButtonMB button)
        {
            SetSettingsFocus(false);
        }

        private void EnsureReferences()
        {
            pageCanvasGroup ??= GetComponent<CanvasGroup>();

            if (rightPanelTransitionImage != null)
                rightPanelTransitionImage.SetDefaultProfile(rightPanelTransitionProfile);
        }

        private void ApplyHiddenCanvasState()
        {
            if (pageCanvasGroup != null)
            {
                pageCanvasGroup.alpha = 0f;
                pageCanvasGroup.interactable = false;
                pageCanvasGroup.blocksRaycasts = false;
            }

            if (buttonsCanvasGroup != null)
            {
                buttonsCanvasGroup.alpha = 0f;
                buttonsCanvasGroup.interactable = false;
                buttonsCanvasGroup.blocksRaycasts = false;
            }
        }

        private void ResetLogoTransform()
        {
            if (logoTransform == null)
                return;

            logoTransform.localScale = Vector3.zero;
            logoTransform.localEulerAngles = Vector3.zero;
        }

        private void CachePreparedInitialRightPanelSprite()
        {
            if (preparedInitialRightPanelSprite != null || titleWeightedSprites == null)
                return;

            preparedInitialRightPanelSprite = titleWeightedSprites.PickRandom();
        }

        private Sprite ConsumePreparedInitialRightPanelSprite()
        {
            if (preparedInitialRightPanelSprite != null)
            {
                Sprite sprite = preparedInitialRightPanelSprite;
                preparedInitialRightPanelSprite = null;
                return sprite;
            }

            return titleWeightedSprites != null
                ? titleWeightedSprites.PickRandom()
                : null;
        }
    }
}

using System.Threading;
using BC.Audio;
using BC.Rendering.Transition;
using BC.UI.Effect;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private UINoiseOutlineMB playButtonOutline;
        [SerializeField] private UINoiseOutlineMB settingsButtonOutline;
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
        [Tooltip("ボタンにフォーカスしたときの SE です。")]
        [SerializeField] private AudioDataSO buttonFocusSound;
        [Tooltip("ボタンをクリックしたときの SE です。")]
        [SerializeField] private AudioDataSO buttonClickSound;
        [Tooltip("ロゴを回転させるときの SE です。")]
        [SerializeField] private AudioDataSO logoSpinSound;

        private CanvasGroup pageCanvasGroup;
        private bool settingsFocused;
        private Sprite preparedInitialRightPanelSprite;

        private void Awake()
        {
            EnsureReferences();
            ApplyHiddenCanvasState();
            ResetLogoTransform();

            if (playButtonOutline == null && playButton != null)
                playButtonOutline = playButton.GetComponentInChildren<UINoiseOutlineMB>(true);

            if (settingsButtonOutline == null && settingsButton != null)
                settingsButtonOutline = settingsButton.GetComponentInChildren<UINoiseOutlineMB>(true);

            playButtonOutline?.SetFocused(false);
            settingsButtonOutline?.SetFocused(false);

            // ボタンイベント登録
            if (playButton != null) playButton.onClick.AddListener(OnPlayButtonClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsButtonClicked);

            // フォーカス SE 登録
            RegisterButtonSoundEvents(playButton);
            RegisterButtonSoundEvents(settingsButton);

            // Settings ボタンのフォーカスイベント登録
            RegisterSettingsButtonFocusEvents();
        }

        public void PrewarmInitialVisuals()
        {
            EnsureReferences();
            ApplyHiddenCanvasState();
            ResetLogoTransform();
            CachePreparedInitialRightPanelSprite();

            if (preparedInitialRightPanelSprite != null && rightPanelTransitionImage != null)
                rightPanelTransitionImage.SetImmediateSprite(preparedInitialRightPanelSprite);

            Canvas.ForceUpdateCanvases();
        }

        private void RegisterSettingsButtonFocusEvents()
        {
            if (settingsButton == null) return;

            EventTrigger trigger = settingsButton.GetComponent<EventTrigger>();
            if (trigger == null) trigger = settingsButton.gameObject.AddComponent<EventTrigger>();

            var onSelect = new EventTrigger.Entry { eventID = EventTriggerType.Select };
            onSelect.callback.AddListener(_ => SetSettingsFocus(true));
            trigger.triggers.Add(onSelect);

            var onDeselect = new EventTrigger.Entry { eventID = EventTriggerType.Deselect };
            onDeselect.callback.AddListener(_ => SetSettingsFocus(false));
            trigger.triggers.Add(onDeselect);
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            EnsureReferences();
            IsShowing = true;
            gameObject.SetActive(true);
            ApplyHiddenCanvasState();
            ResetLogoTransform();

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
            if (playButton != null)
                EventSystem.current?.SetSelectedGameObject(playButton.gameObject);

            RefreshButtonOutlines();
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
                EventSystem.current?.SetSelectedGameObject(playButton.gameObject);

            RefreshButtonOutlines();
        }

        private void Update()
        {
            if (!IsShowing)
                return;

            RefreshButtonOutlines();
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

        private void RefreshButtonOutlines()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

            bool playFocused = selected != null && playButton != null &&
                               (selected == playButton.gameObject || selected.transform.IsChildOf(playButton.transform));
            bool settingsFocusedNow = selected != null && settingsButton != null &&
                                      (selected == settingsButton.gameObject || selected.transform.IsChildOf(settingsButton.transform));

            playButtonOutline?.SetFocused(playFocused);
            settingsButtonOutline?.SetFocused(settingsFocusedNow);
        }

        private void OnPlayButtonClicked()
        {
            PlayClickSound();
            TitleSceneManagerMB manager = TitleSceneManagerMB.Instance;
            if (manager == null) return;
            manager.GoToStageSelectAsync(destroyCancellationToken).Forget();
        }

        private void OnSettingsButtonClicked()
        {
            PlayClickSound();
            TitleSceneManagerMB manager = TitleSceneManagerMB.Instance;
            if (manager == null) return;
            manager.OpenSettingsAsync(destroyCancellationToken).Forget();
        }

        private void RegisterButtonSoundEvents(Button button)
        {
            if (button == null) return;

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();

            var onSelect = new EventTrigger.Entry { eventID = EventTriggerType.Select };
            onSelect.callback.AddListener(_ => PlayFocusSound());
            trigger.triggers.Add(onSelect);

            var onPointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            onPointerEnter.callback.AddListener(_ =>
            {
                PlayFocusSound();
                SelectIfNeeded(button.gameObject);
            });
            trigger.triggers.Add(onPointerEnter);
        }

        private static void SelectIfNeeded(GameObject target)
        {
            if (target == null || EventSystem.current == null)
                return;

            if (EventSystem.current.currentSelectedGameObject == target)
                return;

            EventSystem.current.SetSelectedGameObject(target);
        }

        private void PlayFocusSound()
        {
            if (buttonFocusSound != null)
                AudioSystemMB.Instance?.PlaySE(buttonFocusSound);
        }

        private void PlayClickSound()
        {
            if (buttonClickSound != null)
                AudioSystemMB.Instance?.PlaySE(buttonClickSound);
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

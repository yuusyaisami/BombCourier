using System.Threading;
using BC.Audio;
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
        [SerializeField, Min(0f)] private float buttonsFadeDuration = 0.4f;

        [Header("Right Panel")]
        [SerializeField] private Image rightPanelImageA;
        [SerializeField] private Image rightPanelImageB;
        [SerializeField] private TitleWeightedSpriteSO titleWeightedSprites;
        [SerializeField] private Sprite settingsPanelSprite;
        [SerializeField, Min(0f)] private float rightPanelCrossFadeDuration = 0.3f;

        [Header("Page Animation")]
        [SerializeField, Min(0f)] private float pageInDuration = 0.35f;
        [SerializeField, Min(0f)] private float pageOutDuration = 0.3f;

        [Header("Sound")]
        [Tooltip("ボタンにフォーカスしたときの SE です。")]
        [SerializeField] private AudioDataSO buttonFocusSound;
        [Tooltip("ボタンをクリックしたときの SE です。")]
        [SerializeField] private AudioDataSO buttonClickSound;

        private CanvasGroup pageCanvasGroup;
        private Image activeRightPanel;
        private Image inactiveRightPanel;
        private bool settingsFocused;

        private void Awake()
        {
            pageCanvasGroup = GetComponent<CanvasGroup>();
            pageCanvasGroup.alpha = 0f;
            pageCanvasGroup.interactable = false;

            if (buttonsCanvasGroup != null)
            {
                buttonsCanvasGroup.alpha = 0f;
                buttonsCanvasGroup.interactable = false;
            }

            if (logoTransform != null)
            {
                logoTransform.localScale = Vector3.zero;
                logoTransform.localEulerAngles = Vector3.zero;
            }

            activeRightPanel = rightPanelImageA;
            inactiveRightPanel = rightPanelImageB;

            // ボタンイベント登録
            if (playButton != null) playButton.onClick.AddListener(OnPlayButtonClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsButtonClicked);

            // フォーカス SE 登録
            RegisterButtonSoundEvents(playButton);
            RegisterButtonSoundEvents(settingsButton);

            // Settings ボタンのフォーカスイベント登録
            RegisterSettingsButtonFocusEvents();
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
            IsShowing = true;
            gameObject.SetActive(true);
            pageCanvasGroup.alpha = 0f;

            await pageCanvasGroup
                .DOFade(1f, pageInDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .WithCancellation(ct);

            // 右パネルの初期画像を設定
            SetInitialRightPanelSprite();

            // ロゴアニメーション ("ぐるぐるバン！")
            await PlayLogoIntroAsync(ct);

            // ボタンをフェードイン
            if (buttonsCanvasGroup != null)
            {
                await buttonsCanvasGroup
                    .DOFade(1f, buttonsFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .WithCancellation(ct);

                buttonsCanvasGroup.interactable = true;
            }

            pageCanvasGroup.interactable = true;

            // ゲームプレイボタンに初期フォーカスを当てる
            if (playButton != null)
                EventSystem.current?.SetSelectedGameObject(playButton.gameObject);
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            pageCanvasGroup.interactable = false;
            if (buttonsCanvasGroup != null)
                buttonsCanvasGroup.interactable = false;

            await pageCanvasGroup
                .DOFade(0f, pageOutDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .WithCancellation(ct);

            IsShowing = false;
            gameObject.SetActive(false);
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
            Sprite sprite = titleWeightedSprites != null
                ? titleWeightedSprites.PickRandom()
                : null;

            if (activeRightPanel != null) { activeRightPanel.sprite = sprite; activeRightPanel.color = Color.white; }
            if (inactiveRightPanel != null) { inactiveRightPanel.sprite = null; inactiveRightPanel.color = Color.clear; }
        }

        private void SetSettingsFocus(bool focused)
        {
            if (settingsFocused == focused) return;
            settingsFocused = focused;

            Sprite targetSprite = focused ? settingsPanelSprite
                : (titleWeightedSprites != null ? titleWeightedSprites.PickRandom() : null);

            CrossFadeRightPanel(targetSprite).Forget();
        }

        private async UniTaskVoid CrossFadeRightPanel(Sprite newSprite)
        {
            CancellationToken ct = destroyCancellationToken;

            // inactivePanel に新しい画像をセットしてフェードイン、activePanel はフェードアウト
            inactiveRightPanel.sprite = newSprite;
            inactiveRightPanel.color = Color.clear;

            UniTask fadeIn = inactiveRightPanel.DOColor(Color.white, rightPanelCrossFadeDuration).SetUpdate(true).WithCancellation(ct);
            UniTask fadeOut = activeRightPanel.DOColor(Color.clear, rightPanelCrossFadeDuration).SetUpdate(true).WithCancellation(ct);

            await UniTask.WhenAll(fadeIn, fadeOut);

            // A/B を入れ替える
            (activeRightPanel, inactiveRightPanel) = (inactiveRightPanel, activeRightPanel);
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
            onPointerEnter.callback.AddListener(_ => PlayFocusSound());
            trigger.triggers.Add(onPointerEnter);
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
    }
}

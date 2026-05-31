using System;
using System.Threading;
using BC.Audio;
using BC.Stage;
using BC.UI.Effect;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BC.UI.Title
{
    // ステージセレクト内の個別ステージ項目。
    // Setup() でデータを受け取り、フォーカス / 選択イベントを外部に通知する。
    [RequireComponent(typeof(Button))]
    public sealed class UIStageSelectItemMB : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UINoiseOutlineMB noiseOutline;
        [SerializeField] private UIButtonFlashMB buttonFlash;
        [SerializeField] private Image previewImage;
        [SerializeField] private Image lockedOverlay;
        [SerializeField] private TextMeshProUGUI stageIndexText;

        [Header("Sound")]
        [Tooltip("フォーカスしたときの SE です。")]
        [SerializeField] private AudioDataSO focusSound;
        [Tooltip("選択したときの SE です。")]
        [SerializeField] private AudioDataSO selectSound;

        public int StageIndex { get; private set; }
        public bool IsUnlocked { get; private set; }
        public StageData StageData { get; private set; }

        public event Action<UIStageSelectItemMB> OnFocused;
        public event Action<UIStageSelectItemMB> OnSelected;

        private UIStageSelectPageMB selectPage;

        private Button button;
        private CanvasGroup canvasGroup;
        private bool initialized;
        private bool isFocused;

        private void Awake()
        {
            EnsureInitialized();
            noiseOutline?.SetFocused(false);
        }

        /// <summary>ステージデータを設定し表示を更新する。</summary>
        public void Setup(StageData data, int index, bool isUnlocked, int starCount)
        {
            EnsureInitialized();

            StageData = data;
            StageIndex = index;
            IsUnlocked = isUnlocked;

            if (previewImage != null)
                previewImage.sprite = data?.previewSprite;

            if (stageIndexText != null)
            {
                stageIndexText.text = (index + 1).ToString();
                stageIndexText.color = ResolveTitleColor(starCount);
            }

            if (lockedOverlay != null)
                lockedOverlay.gameObject.SetActive(!isUnlocked);

            if (button != null)
                button.interactable = isUnlocked;

            if (canvasGroup != null)
                canvasGroup.alpha = isUnlocked ? 1f : 0.45f;
        }

        private static Color ResolveTitleColor(int starCount)
        {
            return starCount switch
            {
                3 => Color.yellow,
                2 => new Color(0.5f, 0.85f, 1f, 1f),
                1 => Color.white,
                _ => Color.white,
            };
        }

        /// <summary>フォーカス状態を設定する。Outline アニメーションが切り替わる。</summary>
        public void SetFocused(bool focused)
        {
            EnsureInitialized();

            if (isFocused == focused)
                return;

            isFocused = focused;
            noiseOutline?.SetFocused(focused);
            if (focused)
            {
                if (focusSound != null)
                    AudioSystemMB.Instance?.PlaySE(focusSound);
                else
                    selectPage?.PlayNavFocusSound();

                OnFocused?.Invoke(this);
            }
        }

        public bool IsSelectionTarget(GameObject selectedObject)
        {
            if (selectedObject == null)
                return false;

            EnsureInitialized();

            if (button != null)
            {
                if (selectedObject == button.gameObject)
                    return true;

                Transform selectedTransform = selectedObject.transform;
                if (selectedTransform != null && selectedTransform.IsChildOf(button.transform))
                    return true;
            }

            Transform selfTransform = transform;
            return selectedObject.transform != null && selectedObject.transform.IsChildOf(selfTransform);
        }

        public bool CanReceiveNavigationFocus()
        {
            EnsureInitialized();
            return IsUnlocked && isActiveAndEnabled && gameObject.activeInHierarchy && button != null && button.interactable;
        }

        public GameObject GetSelectionObject()
        {
            EnsureInitialized();
            return button != null ? button.gameObject : gameObject;
        }

        private void OnClick()
        {
            if (!IsUnlocked) return;
            PlaySelectSequenceAsync(destroyCancellationToken).Forget();
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            button = GetComponent<Button>();
            if (stageIndexText == null)
                stageIndexText = GetComponentInChildren<TextMeshProUGUI>(true);

            selectPage = GetComponentInParent<UIStageSelectPageMB>();

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (noiseOutline == null)
                noiseOutline = GetComponentInChildren<UINoiseOutlineMB>(true);

            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
                button.onClick.AddListener(OnClick);

                EventTrigger trigger = button.GetComponent<EventTrigger>();
                if (trigger == null)
                    trigger = button.gameObject.AddComponent<EventTrigger>();

                EventTrigger.Entry onPointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                onPointerEnter.callback.AddListener(_ => EventSystem.current?.SetSelectedGameObject(button.gameObject));
                trigger.triggers.Add(onPointerEnter);

                // Navigation は UIStageSelectNavigationMB が明示指定するため無効化
                Navigation nav = button.navigation;
                nav.mode = Navigation.Mode.Explicit;
                button.navigation = nav;
            }



            initialized = true;
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClick);
        }

        private async UniTaskVoid PlaySelectSequenceAsync(CancellationToken ct)
        {
            if (selectSound != null)
                AudioSystemMB.Instance?.PlaySE(selectSound);
            else
                selectPage?.PlayNavClickSound();

            if (buttonFlash != null)
                await buttonFlash.PlayFlashAsync(ct);

            OnSelected?.Invoke(this);
        }
    }
}

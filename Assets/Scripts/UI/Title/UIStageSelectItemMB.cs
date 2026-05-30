using System;
using System.Threading;
using BC.Audio;
using BC.Stage;
using BC.UI.Effect;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BC.UI.Title
{
    // ステージセレクト内の個別ステージ項目。
    // Setup() でデータを受け取り、フォーカス / 選択イベントを外部に通知する。
    [RequireComponent(typeof(Button))]
    public sealed class UIStageSelectItemMB : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UINoiseOutlineMB noiseOutline;
        [SerializeField] private UIButtonFlashMB  buttonFlash;
        [SerializeField] private Image            previewImage;
        [SerializeField] private Image            lockedOverlay;

        [Header("Sound")]
        [Tooltip("フォーカスしたときの SE です。")]
        [SerializeField] private AudioDataSO focusSound;
        [Tooltip("選択したときの SE です。")]
        [SerializeField] private AudioDataSO selectSound;

        public int    StageIndex  { get; private set; }
        public bool   IsUnlocked  { get; private set; }
        public StageData StageData { get; private set; }

        public event Action<UIStageSelectItemMB> OnFocused;
        public event Action<UIStageSelectItemMB> OnSelected;

        private Button     button;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            button      = GetComponent<Button>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            button.onClick.AddListener(OnClick);

            // Navigation は UIStageSelectNavigationMB が明示指定するため無効化
            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.Explicit;
            button.navigation = nav;
        }

        /// <summary>ステージデータを設定し表示を更新する。</summary>
        public void Setup(StageData data, int index, bool isUnlocked)
        {
            StageData       = data;
            StageIndex      = index;
            IsUnlocked      = isUnlocked;

            if (previewImage != null)
                previewImage.sprite = data?.previewSprite;

            if (lockedOverlay != null)
                lockedOverlay.gameObject.SetActive(!isUnlocked);

            button.interactable = isUnlocked;

            if (canvasGroup != null)
                canvasGroup.alpha = isUnlocked ? 1f : 0.45f;
        }

        /// <summary>フォーカス状態を設定する。Outline アニメーションが切り替わる。</summary>
        public void SetFocused(bool focused)
        {
            noiseOutline?.SetFocused(focused);
            if (focused)
            {
                if (focusSound != null)
                    AudioSystemMB.Instance?.PlaySE(focusSound);
                OnFocused?.Invoke(this);
            }
        }

        private void OnClick()
        {
            if (!IsUnlocked) return;
            PlaySelectSequenceAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid PlaySelectSequenceAsync(CancellationToken ct)
        {
            if (selectSound != null)
                AudioSystemMB.Instance?.PlaySE(selectSound);

            if (buttonFlash != null)
                await buttonFlash.PlayFlashAsync(ct);

            OnSelected?.Invoke(this);
        }
    }
}

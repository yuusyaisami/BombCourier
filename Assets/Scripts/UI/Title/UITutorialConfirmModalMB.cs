using System.Collections.Generic;
using System.Threading;
using BC.Base;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BC.UI.Effect;

namespace BC.UI.Title
{
    // チュートリアルモードで起動するか確認するモーダルダイアログ。
    // UIStageSelectPageMB がステージ選択時に ShowConfirmAsync を呼び出す。
    // 「はい」→ true / 「いいえ」→ false を返す。
    //
    // 表示対象ステージは stageIndices で指定する（インデックス番号リスト）。
    // stageIndices に含まれないステージが選択された場合は表示せず直接 false を返す。
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UITutorialConfirmModalMB : MonoBehaviour
    {
        [Header("Tutorial Stages")]
        [Tooltip("このモーダルを表示するステージのインデックス番号リスト。")]
        [SerializeField] private List<int> stageIndices = new();

        [Header("Buttons")]
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private UINoiseOutlineMB yesButtonOutline;
        [SerializeField] private UINoiseOutlineMB noButtonOutline;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

        private CanvasGroup canvasGroup;
        private UniTaskCompletionSource<bool> pendingResult;
        private GameObject previousSelectedObject;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.ignoreParentGroups = true;

            yesButtonOutline ??= yesButton != null ? yesButton.GetComponentInChildren<UINoiseOutlineMB>(true) : null;
            noButtonOutline ??= noButton != null ? noButton.GetComponentInChildren<UINoiseOutlineMB>(true) : null;

            yesButtonOutline?.SetFocused(false);
            noButtonOutline?.SetFocused(false);

            if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
            if (noButton != null) noButton.onClick.AddListener(OnNoClicked);

            RegisterFocusEvents(yesButton);
            RegisterFocusEvents(noButton);
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>指定ステージでこのモーダルを表示すべきか返す。</summary>
        public bool ShouldShowForStage(int stageIndex)
        {
            return stageIndices != null && stageIndices.Contains(stageIndex);
        }

        /// <summary>
        /// モーダルを表示してユーザーの選択を待機する。
        /// 「はい」→ true / 「いいえ」→ false を返す。
        /// </summary>
        public async UniTask<bool> ShowConfirmAsync(CancellationToken ct)
        {
            if (pendingResult != null)
            {
                Debug.LogWarning($"[{nameof(UITutorialConfirmModalMB)}] ShowConfirmAsync was called while another request is pending.", this);
                return false;
            }

            pendingResult = new UniTaskCompletionSource<bool>();
            previousSelectedObject = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            IsOpen = true;

            // フェードイン
            canvasGroup.ignoreParentGroups = true;
            canvasGroup.blocksRaycasts = true;
            await canvasGroup
                .DOFade(1f, fadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .WithCancellation(ct);

            canvasGroup.interactable = true;
            SelectDefaultButton();
            RefreshButtonOutlines();

            // ボタン押下を待機（キャンセル時は false 扱い）
            bool result = false;
            try
            {
                result = await pendingResult.Task.AttachExternalCancellation(ct);
            }
            catch (System.OperationCanceledException)
            {
                result = false;
            }

            // フェードアウト
            canvasGroup.interactable = false;
            ClearModalSelection();
            await canvasGroup
                .DOFade(0f, fadeDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .WithCancellation(destroyCancellationToken);

            canvasGroup.blocksRaycasts = false;
            pendingResult = null;
            previousSelectedObject = null;
            IsOpen = false;
            RefreshButtonOutlines();

            return result;
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            RefreshButtonOutlines();
        }

        // ------------------------------------------------------------------
        // Private
        // ------------------------------------------------------------------

        private void OnYesClicked()
        {
            pendingResult?.TrySetResult(true);
        }

        private void OnNoClicked()
        {
            pendingResult?.TrySetResult(false);
        }

        private void RegisterFocusEvents(Button button)
        {
            if (button == null)
                return;

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry onSelect = new() { eventID = EventTriggerType.Select };
            onSelect.callback.AddListener(_ => RefreshButtonOutlines());
            trigger.triggers.Add(onSelect);

            EventTrigger.Entry onDeselect = new() { eventID = EventTriggerType.Deselect };
            onDeselect.callback.AddListener(_ => RefreshButtonOutlines());
            trigger.triggers.Add(onDeselect);

            EventTrigger.Entry onPointerEnter = new() { eventID = EventTriggerType.PointerEnter };
            onPointerEnter.callback.AddListener(_ => SelectIfNeeded(button.gameObject));
            trigger.triggers.Add(onPointerEnter);
        }

        private void SelectDefaultButton()
        {
            if (EventSystem.current == null)
                return;

            GameObject target = yesButton != null
                ? yesButton.gameObject
                : noButton != null
                    ? noButton.gameObject
                    : null;

            if (target != null)
                EventSystem.current.SetSelectedGameObject(target);
        }

        private void ClearModalSelection()
        {
            if (EventSystem.current == null)
                return;

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null)
                return;

            if (yesButton != null && (selected == yesButton.gameObject || selected.transform.IsChildOf(yesButton.transform)))
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            if (noButton != null && (selected == noButton.gameObject || selected.transform.IsChildOf(noButton.transform)))
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            if (previousSelectedObject != null)
                EventSystem.current.SetSelectedGameObject(previousSelectedObject);
        }

        private void RefreshButtonOutlines()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

            bool yesFocused = selected != null && yesButton != null &&
                              (selected == yesButton.gameObject || selected.transform.IsChildOf(yesButton.transform));
            bool noFocused = selected != null && noButton != null &&
                             (selected == noButton.gameObject || selected.transform.IsChildOf(noButton.transform));

            yesButtonOutline?.SetFocused(yesFocused);
            noButtonOutline?.SetFocused(noFocused);
        }

        private static void SelectIfNeeded(GameObject target)
        {
            if (target == null || EventSystem.current == null)
                return;

            if (EventSystem.current.currentSelectedGameObject == target)
                return;

            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}

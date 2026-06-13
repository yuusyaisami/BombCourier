using System.Threading;
using BC.Audio;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BC.UI.Components
{
    // 汎用の「はい / いいえ」確認ダイアログ。
    // UITutorialConfirmModalMB の確認フローを一般化したもので、特定ステージ依存(stageIndices)を持たない。
    // 呼び出し側は ShowConfirmAsync を await し、「はい」→ true /「いいえ」(またはキャンセル)→ false を受け取る。
    //
    // 親 CanvasGroup（設定画面など）の上に重ねて使う想定のため canvasGroup.ignoreParentGroups = true とし、
    // 親側を interactable=false にしてもこのダイアログだけは操作可能なまま入力を捕捉する。
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIConfirmDialogMB : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private UIButtonMB yesButton;
        [SerializeField] private UIButtonMB noButton;

        [Tooltip("開いた直後に「いいえ」へフォーカスを当てるか。セーブ削除など破壊的操作では true 推奨（安全側）。")]
        [SerializeField] private bool focusNoButtonByDefault = true;

        [Header("Sound")]
        [Tooltip("ダイアログ表示時に再生するサウンド。")]
        [SerializeField] private AudioDataSO showSound;

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

            if (yesButton != null)
                yesButton.AddClickListener(OnYesClicked);
            if (noButton != null)
                noButton.AddClickListener(OnNoClicked);
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>
        /// ダイアログを表示してユーザーの選択を待機する。
        /// 「はい」→ true /「いいえ」またはキャンセル → false。
        /// </summary>
        public async UniTask<bool> ShowConfirmAsync(CancellationToken ct)
        {
            if (pendingResult != null)
            {
                // 二重表示は不正な状態。前の要求を壊さないよう、ここでは false を返して無視する。
                Debug.LogWarning($"[{nameof(UIConfirmDialogMB)}] ShowConfirmAsync was called while another request is pending.", this);
                return false;
            }

            pendingResult = new UniTaskCompletionSource<bool>();
            previousSelectedObject = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            IsOpen = true;

            // 親（設定パネル等。開くとき自身を SetAsLastSibling する）より確実に手前へ重ねる。
            transform.SetAsLastSibling();

            if (showSound != null)
                AudioSystemMB.Instance?.PlaySE(showSound);

            // フェードイン（親グループの影響を無視し、自前で raycast を受ける）
            canvasGroup.ignoreParentGroups = true;
            canvasGroup.blocksRaycasts = true;
            await canvasGroup
                .DOFade(1f, fadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .WithCancellation(ct);

            canvasGroup.interactable = true;
            SelectDefaultButton();

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

            // フェードアウト（呼び出し側 ct がキャンセルされても閉じアニメは破棄トークンで完遂させる）
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

            return result;
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

        private void SelectDefaultButton()
        {
            if (EventSystem.current == null)
                return;

            // 破壊的操作では「いいえ」を初期選択にして、誤って「はい」を即決しないようにする。
            UIButtonMB preferred = focusNoButtonByDefault ? noButton : yesButton;
            UIButtonMB fallback = focusNoButtonByDefault ? yesButton : noButton;

            if (preferred != null)
                preferred.Select();
            else
                fallback?.Select();
        }

        private void ClearModalSelection()
        {
            if (EventSystem.current == null)
                return;

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null)
                return;

            // ダイアログ内のボタンが選択されたまま閉じると行き先を失うので、いったん解除するか
            // ダイアログを開く前の選択へ戻す。
            if (yesButton != null && yesButton.IsSelectionTarget(selected))
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            if (noButton != null && noButton.IsSelectionTarget(selected))
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            if (previousSelectedObject != null)
                EventSystem.current.SetSelectedGameObject(previousSelectedObject);
        }
    }
}

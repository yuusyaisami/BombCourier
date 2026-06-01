using System.Collections.Generic;
using System.Threading;
using BC.Base;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("Animation")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

        private CanvasGroup canvasGroup;
        private UniTaskCompletionSource<bool> pendingResult;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
            if (noButton != null) noButton.onClick.AddListener(OnNoClicked);
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
            pendingResult = new UniTaskCompletionSource<bool>();

            // フェードイン
            canvasGroup.blocksRaycasts = true;
            await canvasGroup
                .DOFade(1f, fadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .WithCancellation(ct);

            canvasGroup.interactable = true;

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
            await canvasGroup
                .DOFade(0f, fadeDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .WithCancellation(destroyCancellationToken);

            canvasGroup.blocksRaycasts = false;
            pendingResult = null;

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
    }
}

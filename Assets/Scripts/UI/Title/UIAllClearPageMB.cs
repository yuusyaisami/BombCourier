using BC.Audio;
using BC.UI;
using BC.UI.Components;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace BC.UI.Title
{
    // 全ステージ星3コンプ達成者向けの特典パネル。
    // 設定画面(UISettingMB)と同様、GameObject は出しっぱなしで CanvasGroup の alpha により表示/非表示する
    // オーバーレイ方式。開閉は TitleSceneManagerMB がルーティングする（タイトルメインを隠してから表示）。
    // パネルの中身（特典の演出・イラスト等）は Editor 上で子要素として配置する。
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIAllClearPageMB : MonoBehaviour
    {
        [Header("General")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;
        [Tooltip("このパネルを閉じてタイトルメインへ戻るボタン。")]
        [SerializeField] private UIButtonMB closeButton;
        [SerializeField] private UIFallEffectMB fallEffect; // 落下エフェクト（全クリ特典の演出の一部）。Inspector で子要素からアタッチする想定。

        [Header("Sound")]
        [Tooltip("パネルを開いたときに再生するサウンド。")]
        [SerializeField] private AudioDataSO openSound;

        private bool isShowing;
        // モーダルゲートを Push 済みかを覚え、二重 Pop / Pop 漏れを防ぐ（UISettingMB と同方式）。
        private bool modalGatePushed;

        public bool IsShowing => isShowing;

        private void Awake()
        {
            EnsureCanvasGroup();

            // gameObject は破棄せず CanvasGroup で隠す（初期化順序のバグ回避は UISettingMB と同方針）。
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (closeButton != null)
            {
                closeButton.RemoveClickListener(ClosePanel);
                closeButton.AddClickListener(ClosePanel);
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.RemoveClickListener(ClosePanel);

            // モーダルゲートを押したまま破棄されると裏側の入力が永久ロックされるため、必ず Pop する。
            if (modalGatePushed)
            {
                modalGatePushed = false;
                UiModalGate.Pop();
            }
        }

        // ------------------------------------------------------------------
        // Show / Hide
        // ------------------------------------------------------------------

        /// <summary>パネルをフェードインして表示する。</summary>
        public async UniTask ShowPanelAsync()
        {
            if (isShowing)
                return;

            EnsureCanvasGroup();
            isShowing = true;

            modalGatePushed = true;
            UiModalGate.Push();

            // 最前面へ持ってきてから表示する（他オーバーレイより確実に手前へ重なるように）。
            transform.SetAsLastSibling();

            // 落下エフェクトを開始する（全クリ特典の演出の一部）。
            if (fallEffect != null)
                fallEffect.StartFallEffect();

            if (openSound != null)
                AudioSystemMB.Instance?.PlaySE(openSound);

            // カスタムナビが読む project-wide UI マップを有効化しておく（idempotent）。
            UINavigationBootstrap.EnsureConfigured();

            canvasGroup.blocksRaycasts = true;

            await canvasGroup
                .DOFade(1f, fadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .WithCancellation(destroyCancellationToken);

            canvasGroup.interactable = true;

            // 閉じるボタンに初期フォーカスを当てる（ゲームパッド/キーボード操作の起点）。
            if (closeButton != null)
                closeButton.Select();
        }

        /// <summary>パネルをフェードアウトして隠す。</summary>
        public async UniTask HidePanelAsync()
        {
            if (!isShowing)
                return;
            isShowing = false;

            canvasGroup.interactable = false;

            // 落下エフェクトを終了する（全クリ特典の演出の一部）。
            if (fallEffect != null)
                fallEffect.EndFallEffect();

            await canvasGroup
                .DOFade(0f, fadeDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .WithCancellation(destroyCancellationToken);

            canvasGroup.blocksRaycasts = false;

            if (modalGatePushed)
            {
                modalGatePushed = false;
                UiModalGate.Pop();
            }
        }

        // ------------------------------------------------------------------
        // Close
        // ------------------------------------------------------------------

        // 閉じるボタンのクリックハンドラ。UIButtonMB.AddClickListener は引数なし UnityAction を取る。
        private void ClosePanel()
        {
            ClosePanelAndReturnAsync().Forget();
        }

        private async UniTaskVoid ClosePanelAndReturnAsync()
        {
            await HidePanelAsync();

            TitleSceneManagerMB manager = TitleSceneManagerMB.Instance;
            if (manager == null)
                return;

            await manager.ReturnToTitleMainFromAllClearAsync(destroyCancellationToken);
        }

        private void EnsureCanvasGroup()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
        }
    }
}

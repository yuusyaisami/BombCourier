using System.Threading;
using BC.Base;
using BC.Managers;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Febucci.TextAnimatorCore;
//using Febucci.UI.Core;

using UnityEngine;
using UnityEngine.InputSystem;
namespace BC.UI
{
    public class UITalkSystemMB : MonoBehaviour
    {
        [SerializeField] private TextAnimator bodyTextAnimator; // 会話本文
        [SerializeField] private TextAnimator speakerNameTextAnimator; // 話者名
        [SerializeField] private CanvasGroup canvasGroup; // 会話UI全体のCanvasGroup
        [SerializeField] private Transform talkUIRoot; // 会話UIの親Transform (nullの場合はこれ自身のTransformを使用)
        [SerializeField] private SpriteAnimationPlayerMB speakerIconAnimator; // 話者アイコンのアニメーター 
        [SerializeField] private SpriteAnimationPlayerMB nextIndicatorAnimator; // 次へ進むインジケーターのアニメーター
        [SerializeField] private AnimationSpriteClip nextIndicatorClip; // 次へ進むインジケーターのアニメーション
        [SerializeField] private InputActionReference nextTalkInputAction; // 次の会話に進む入力アクション
        private bool isShowingTalk; // 会話UIが表示されているかどうかのフラグ

        public InputAction NextTalkInputAction => nextTalkInputAction != null ? nextTalkInputAction.action : null;

        private void Awake()
        {
            // 起動時は会話UIを閉じた状態にしておく。
            InitializeTalkUI();
        }

        private void OnEnable()
        {
            // 会話中だけ入力を受け付ける。
            nextTalkInputAction?.action.Enable();
        }

        private void OnDisable()
        {
            nextTalkInputAction?.action.Disable();
        }

        private void OnDestroy()
        {
            nextTalkInputAction?.action.Disable();
        }

        private void InitializeTalkUI()
        {
            // UI全体を非表示・非操作にして、会話開始時のアニメーションから見せる。
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            // ルートをY方向につぶして、ShowTalk時に伸びる見た目にする。
            if (TalkRoot != null)
            {
                Vector3 scale = TalkRoot.localScale;
                scale.y = 0f;
                TalkRoot.localScale = scale;
            }

            StopNextIndicator();
        }

        private Transform TalkRoot => talkUIRoot != null ? talkUIRoot : transform;

        public async UniTask ShowTalk(TalkRequestData talkRequestData, CancellationToken cancellationToken)
        {
            // 文字サイズなどの見た目設定を先に反映する。
            ApplyTextEffect(talkRequestData.textEffectData);

            // 新しい会話を出す前に、前回のインジケーター表示を止める。
            StopNextIndicator();

            // 話者名と本文を更新する。
            if (speakerNameTextAnimator != null)
            {
                speakerNameTextAnimator.SetText(talkRequestData.speakerName ?? string.Empty);
            }

            if (bodyTextAnimator != null)
            {
                bodyTextAnimator.SetText(talkRequestData.dialogueText ?? string.Empty);
            }

            if (!isShowingTalk)
            {
                isShowingTalk = true;

                // 初回表示だけ出現アニメーションを再生する。
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }

                await TalkRoot.DOScaleY(1f, 0.3f).SetEase(Ease.OutBack).AsyncWaitForCompletion();

                if (canvasGroup != null)
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }

            }
            else if (canvasGroup != null)
            {
                // すでに表示中なら、入力受付だけ確実に有効化する。
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // 本文アニメーション中の入力は全文表示に使い、表示完了後の入力で次へ進む。
            while (!cancellationToken.IsCancellationRequested)
            {
                bool bodyAnimationCompleted = bodyTextAnimator == null || bodyTextAnimator.AllLettersShown;

                if (bodyAnimationCompleted)
                {
                    PlayNextIndicator();
                }

                if (nextTalkInputAction != null && nextTalkInputAction.action.WasPressedThisFrame())
                {
                    if (!bodyAnimationCompleted && bodyTextAnimator != null)
                    {
                        // まだ文字送り中なら、残りを即座に表示して確定する。
                        bodyTextAnimator.SetVisibilityEntireText(true, false);
                        PlayNextIndicator();
                    }
                    else
                    {
                        break;
                    }
                }

                await UniTask.Yield();
            }
        }

        public void ApplyTextEffect(TextEffectData textEffectData)
        {
            if (!textEffectData.applyFontSize)
            {
                return;
            }

            // 現在の TextAnimator 実装では、この経路からフォントサイズを直接変えない。
        }

        public async UniTask HideTalk(float duration)
        {
            // 非操作化してから閉じる。
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            StopNextIndicator();

            await TalkRoot.DOScaleY(0f, duration).SetEase(Ease.InBack).AsyncWaitForCompletion();
            isShowingTalk = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (speakerNameTextAnimator != null)
            {
                speakerNameTextAnimator.SetText(string.Empty);
            }

            if (bodyTextAnimator != null)
            {
                // 次の会話で前回の本文が残らないように消しておく。
                bodyTextAnimator.SetText(string.Empty);
            }
        }

        private void PlayNextIndicator()
        {
            if (nextIndicatorAnimator == null || !nextIndicatorClip.IsValid)
            {
                return;
            }

            if (nextIndicatorAnimator.IsPlaying)
            {
                return;
            }

            // 会話を進められる状態だけ、待機インジケーターをループ再生する。
            nextIndicatorAnimator.Play(nextIndicatorClip, SpriteAnimationPlayMode.Loop);
        }

        private void StopNextIndicator()
        {
            if (nextIndicatorAnimator == null)
            {
                return;
            }

            nextIndicatorAnimator.Stop();
        }
    }
}
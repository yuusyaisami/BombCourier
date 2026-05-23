using System.Threading;
using BC.Base;
using BC.Managers;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Febucci.TextAnimatorForUnity;

using UnityEngine;
using UnityEngine.InputSystem;
namespace BC.UI
{
    public class UITalkSystemMB : MonoBehaviour
    {
        [Header("Text Animator / Typewriter")]
        [SerializeField] private TypewriterComponent bodyTypewriter; // 会話本文の typewriter
        [SerializeField] private TypewriterComponent speakerNameTypewriter; // 話者名の typewriter

        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup; // 会話UI全体のCanvasGroup
        [SerializeField] private Transform talkUIRoot; // 会話UIの親Transform (nullの場合はこれ自身のTransformを使用)
        [SerializeField] private SpriteAnimationPlayerMB nextIndicatorAnimator; // 次へ進むインジケーターのアニメーター
        [SerializeField] private AnimationSpriteClip nextIndicatorClip; // 次へ進むインジケーターのアニメーション

        [Header("Input")]
        [SerializeField] private InputActionReference nextTalkInputAction; // 次の会話に進む入力アクション
        [SerializeField] private bool advanceOnSkipInput = true; // 文字送り中の入力で全文表示したあと、同じ入力で次stepへ進むか。
        private bool isShowingTalk; // 会話UIが表示されているかどうかのフラグ
        private bool bodyTextCompleted; // 本文の表示完了状態。Typewriterイベントで更新する。

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

            // Inspector未設定でも動作が壊れにくいよう、コード側でもイベント購読を貼る。
            if (bodyTypewriter != null)
            {
                bodyTypewriter.onTextShowed.RemoveListener(NotifyBodyTextShowed);
                bodyTypewriter.onTextShowed.AddListener(NotifyBodyTextShowed);
            }
        }

        private void OnDisable()
        {
            if (bodyTypewriter != null)
            {
                bodyTypewriter.onTextShowed.RemoveListener(NotifyBodyTextShowed);
            }

            nextTalkInputAction?.action.Disable();
        }

        private void OnDestroy()
        {
            if (bodyTypewriter != null)
            {
                bodyTypewriter.onTextShowed.RemoveListener(NotifyBodyTextShowed);
            }

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
            if (bodyTypewriter == null)
            {
                Debug.LogError($"{nameof(UITalkSystemMB)}: {nameof(bodyTypewriter)} is not assigned.", this);
                return;
            }

            // 文字サイズなどの見た目設定を先に反映する。
            ApplyTextEffect(talkRequestData.textEffectData);

            // 新しい会話を出す前に、前回のインジケーター表示を止める。
            StopNextIndicator();

            // 新しい本文を開始する時点で、会話制御側の完了フラグを初期化する。
            bodyTextCompleted = string.IsNullOrEmpty(talkRequestData.dialogueText);

            // 話者名と本文を typewriter 経由で更新する。
            if (speakerNameTypewriter != null)
            {
                speakerNameTypewriter.ShowText(talkRequestData.speakerName ?? string.Empty);
            }

            bodyTypewriter.ShowText(talkRequestData.dialogueText ?? string.Empty);

            // 設定差異に左右されないよう、明示的に開始しておく。
            bodyTypewriter.StartShowingText(true);

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

            // 直前の台詞で使った submit が同一フレームに持ち越されると、
            // 次の台詞を即時確定して二重に進んだように見えるため、解除まで待機する。
            bool waitForSubmitRelease = IsSubmitActuated();

            // 本文アニメーション中の入力は skip に使い、表示完了後の入力で次へ進む。
            while (!cancellationToken.IsCancellationRequested)
            {
                if (waitForSubmitRelease)
                {
                    if (IsSubmitActuated())
                    {
                        await UniTask.Yield();
                        continue;
                    }

                    waitForSubmitRelease = false;
                }

                if (bodyTextCompleted)
                {
                    PlayNextIndicator();
                }

                if (nextTalkInputAction != null && nextTalkInputAction.action.WasPressedThisFrame())
                {
                    if (!bodyTextCompleted)
                    {
                        // 文字送り中の入力は本文を即時表示し、会話制御上は完了扱いにする。
                        bodyTypewriter.SkipTypewriter();
                        bodyTextCompleted = true;
                        PlayNextIndicator();

                        // 1回入力で skip と同時に次stepへ進めるかどうかは既存フラグで制御する。
                        if (advanceOnSkipInput)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                await UniTask.Yield();
            }
        }

        private bool IsSubmitActuated()
        {
            InputAction action = nextTalkInputAction != null ? nextTalkInputAction.action : null;
            return action != null && action.IsPressed();
        }

        // Typewriter の本文表示が完了した時に呼ばれる。Inspector からの登録でもコード登録でも使える。
        public void NotifyBodyTextShowed()
        {
            bodyTextCompleted = true;
            TalkSystemManagerMB.Instance?.NotifyTalkTypingCompleted();
            PlayNextIndicator();
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
            bodyTextCompleted = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (speakerNameTypewriter != null)
            {
                speakerNameTypewriter.ShowText(string.Empty);
            }

            if (bodyTypewriter != null)
            {
                // 次の会話で前回の本文が残らないように消しておく。
                bodyTypewriter.ShowText(string.Empty);
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
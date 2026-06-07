using System.Threading;
using BC.Audio;
using BC.Base;
using BC.Managers;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Febucci.TextAnimatorCore.Text;
using Febucci.TextAnimatorForUnity;
using System.Reflection;

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

        [Header("Character Voice")]
        [SerializeField] private AudioDataSO defaultAudioDataSO; // TalkAdapter を使わない dialogue で使う既定の文字SE。
        [SerializeField] private bool muteInsideHalfWidthParentheses = true; // 半角 () 内の文字SEを無音化する。
        [SerializeField] private bool muteInsideFullWidthParentheses = true; // 全角 （）内の文字SEを無音化する。

        private bool isShowingTalk; // 会話UIが表示されているかどうかのフラグ
        private bool bodyTextCompleted; // 本文の表示完了状態。Typewriterイベントで更新する。
        private AudioDataSO currentTalkCharacterSound; // 現在の話者の文字江サウンド。
        private string currentDialogueText = string.Empty;
        private int fallbackVisibleCharIndex;
        private bool isMuteWithinParentheses;
        private bool waitForAdvanceRelease;

        private static readonly string[] CharacterDataCharMemberNames = { "character", "Character", "c", "char" };
        private static MemberInfo cachedCharacterDataCharMember;
        private static bool hasResolvedCharacterDataCharMember;

        public InputAction NextTalkInputAction => nextTalkInputAction != null ? nextTalkInputAction.action : null;
        public AudioDataSO DefaultAudioDataSO => defaultAudioDataSO;

        // 話者のキャラクターサウンドを設定する。TalkSystemManagerMB から会話弓に呼ばれる。
        public void SetCharacterSound(AudioDataSO sound)
        {
            currentTalkCharacterSound = sound;
        }

        // TalkAdapter を持たない dialogue では、この UI に設定された既定SEを使う。
        public void UseDefaultCharacterSound()
        {
            currentTalkCharacterSound = defaultAudioDataSO;
        }

        private void OnCharacterVisible(CharacterData characterData)
        {
            if (currentTalkCharacterSound == null) return;

            if (ShouldMuteCharacterSound(characterData))
                return;

            AudioSystemMB.Instance?.PlaySE(currentTalkCharacterSound);
        }

        private void Awake()
        {
            // 起動時は会話UIを閉じた状態にしておく。
            InitializeTalkUI();
        }

        private void OnEnable()
        {
            // project-wide の UI/Submit を共有しているため、ここでは有効化だけ行い、
            // OnDisable 側でローカル所有物のように Disable しない。
            nextTalkInputAction?.action.Enable();

            // Inspector未設定でも動作が壊れにくいよう、コード側でもイベント購読を貼る。
            if (bodyTypewriter != null)
            {
                bodyTypewriter.onTextShowed.RemoveListener(NotifyBodyTextShowed);
                bodyTypewriter.onTextShowed.AddListener(NotifyBodyTextShowed);
                bodyTypewriter.onCharacterVisible.RemoveListener(OnCharacterVisible);
                bodyTypewriter.onCharacterVisible.AddListener(OnCharacterVisible);
            }
        }

        private void OnDisable()
        {
            if (bodyTypewriter != null)
            {
                bodyTypewriter.onTextShowed.RemoveListener(NotifyBodyTextShowed);
                bodyTypewriter.onCharacterVisible.RemoveListener(OnCharacterVisible);
            }
        }

        private void OnDestroy()
        {
            if (bodyTypewriter != null)
            {
                bodyTypewriter.onTextShowed.RemoveListener(NotifyBodyTextShowed);
                bodyTypewriter.onCharacterVisible.RemoveListener(OnCharacterVisible);
            }
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

        public async UniTask ShowTalk(TalkRequestData talkRequestData, string speakerDisplayName, CancellationToken cancellationToken)
        {
            if (bodyTypewriter == null)
            {
                Debug.LogError($"{nameof(UITalkSystemMB)}: {nameof(bodyTypewriter)} is not assigned.", this);
                return;
            }

            // 会話UIが非アクティブ開始でも入力が死なないよう、表示時に明示的に有効化する。
            if (nextTalkInputAction != null)
                nextTalkInputAction.action.Enable();

            // 文字サイズなどの見た目設定を先に反映する。
            ApplyTextEffect(talkRequestData.textEffectData);

            // 新しい会話を出す前に、前回のインジケーター表示を止める。
            StopNextIndicator();

            // 新しい本文を開始する時点で、会話制御側の完了フラグを初期化する。
            bodyTextCompleted = string.IsNullOrEmpty(talkRequestData.dialogueText);
            currentDialogueText = talkRequestData.dialogueText ?? string.Empty;
            fallbackVisibleCharIndex = 0;
            isMuteWithinParentheses = false;

            // 話者名と本文を typewriter 経由で更新する。
            if (speakerNameTypewriter != null)
            {
                speakerNameTypewriter.ShowText(speakerDisplayName ?? string.Empty);
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
            waitForAdvanceRelease = IsSubmitActuated();

            // 本文アニメーション中の入力は skip に使い、表示完了後の入力で次へ進む。
            while (!cancellationToken.IsCancellationRequested)
            {
                if (waitForAdvanceRelease)
                {
                    if (IsSubmitActuated())
                    {
                        await UniTask.Yield();
                        continue;
                    }

                    waitForAdvanceRelease = false;
                }

                if (bodyTextCompleted)
                {
                    PlayNextIndicator();
                }

                if (WasAdvancePressedThisFrame())
                {
                    if (ConsumeAdvancePressed())
                    {
                        break;
                    }
                }

                await UniTask.Yield();
            }
        }

        private bool IsSubmitActuated()
        {
            // 設定画面などモーダルUIが開いている間は入力を無視する。
            if (UiModalGate.IsAnyOpen)
                return false;

            InputAction action = nextTalkInputAction != null ? nextTalkInputAction.action : null;
            if (action != null && action.IsPressed())
                return true;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.enterKey.isPressed || Keyboard.current.numpadEnterKey.isPressed || Keyboard.current.spaceKey.isPressed)
                    return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return true;

            if (Gamepad.current != null)
            {
                Gamepad gp = Gamepad.current;
                if (gp.buttonSouth.isPressed || gp.startButton.isPressed)
                    return true;
            }

            return false;
        }

        private bool WasAdvancePressedThisFrame()
        {
            // 設定画面などモーダルUIが開いている間は、裏のTalkを進めない（生入力の貫通対策）。
            if (UiModalGate.IsAnyOpen)
                return false;

            InputAction action = nextTalkInputAction != null ? nextTalkInputAction.action : null;
            if (action != null && action.WasPressedThisFrame())
                return true;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                    return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;

            if (Gamepad.current != null)
            {
                Gamepad gp = Gamepad.current;
                if (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame)
                    return true;
            }

            return false;
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
            currentDialogueText = string.Empty;
            fallbackVisibleCharIndex = 0;
            isMuteWithinParentheses = false;
            waitForAdvanceRelease = false;

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

        private bool ConsumeAdvancePressed()
        {
            if (bodyTextCompleted)
                return true;

            // 会話中の skip 入力は必ずこの行で消費する。
            // 同一押下で次の会話まで進めると、UI 表示と actor presentation の reset が同一フレームで競合する。
            if (advanceOnSkipInput)
            {
                // 旧設定の serialized 値は残すが、same-press advance 自体は明示的に禁止する。
            }

            bodyTypewriter?.SkipTypewriter();
            bodyTextCompleted = true;
            waitForAdvanceRelease = true;
            PlayNextIndicator();
            return false;
        }

        private bool ShouldMuteCharacterSound(CharacterData characterData)
        {
            if (!TryResolveVisibleCharacter(characterData, out char visibleChar))
                return isMuteWithinParentheses;

            if (IsOpeningParenthesis(visibleChar))
            {
                if (ShouldMuteByParenthesisKind(visibleChar))
                {
                    isMuteWithinParentheses = true;
                    return true;
                }

                return isMuteWithinParentheses;
            }

            if (IsClosingParenthesis(visibleChar))
            {
                if (!ShouldMuteByParenthesisKind(visibleChar))
                    return isMuteWithinParentheses;

                bool wasMute = isMuteWithinParentheses;
                isMuteWithinParentheses = false;
                return wasMute;
            }

            return isMuteWithinParentheses;
        }

        private bool ShouldMuteByParenthesisKind(char bracket)
        {
            return bracket switch
            {
                '(' => muteInsideHalfWidthParentheses,
                ')' => muteInsideHalfWidthParentheses,
                '（' => muteInsideFullWidthParentheses,
                '）' => muteInsideFullWidthParentheses,
                _ => false,
            };
        }

        private static bool IsOpeningParenthesis(char c)
        {
            return c == '(' || c == '（';
        }

        private static bool IsClosingParenthesis(char c)
        {
            return c == ')' || c == '）';
        }

        private bool TryResolveVisibleCharacter(CharacterData characterData, out char visibleChar)
        {
            if (TryResolveCharacterFromCharacterData(characterData, out visibleChar))
                return true;

            // CharacterData から文字取得できない場合のフォールバック。
            // dialogueText の先頭から順に進めるため、タグを含む文本ではズレる可能性がある。
            if (string.IsNullOrEmpty(currentDialogueText) || fallbackVisibleCharIndex >= currentDialogueText.Length)
            {
                visibleChar = default;
                return false;
            }

            visibleChar = currentDialogueText[fallbackVisibleCharIndex];
            fallbackVisibleCharIndex++;
            return true;
        }

        private static bool TryResolveCharacterFromCharacterData(CharacterData characterData, out char visibleChar)
        {
            if (!hasResolvedCharacterDataCharMember)
            {
                ResolveCharacterDataCharMember();
                hasResolvedCharacterDataCharMember = true;
            }

            if (cachedCharacterDataCharMember == null)
            {
                visibleChar = default;
                return false;
            }

            object value = cachedCharacterDataCharMember switch
            {
                FieldInfo fieldInfo => fieldInfo.GetValue(characterData),
                PropertyInfo propertyInfo => propertyInfo.GetValue(characterData),
                _ => null,
            };

            if (value is char ch)
            {
                visibleChar = ch;
                return true;
            }

            if (value is string str && str.Length > 0)
            {
                visibleChar = str[0];
                return true;
            }

            visibleChar = default;
            return false;
        }

        private static void ResolveCharacterDataCharMember()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            System.Type type = typeof(CharacterData);

            for (int i = 0; i < CharacterDataCharMemberNames.Length; i++)
            {
                string memberName = CharacterDataCharMemberNames[i];

                FieldInfo field = type.GetField(memberName, flags);
                if (field != null)
                {
                    cachedCharacterDataCharMember = field;
                    return;
                }

                PropertyInfo property = type.GetProperty(memberName, flags);
                if (property != null)
                {
                    cachedCharacterDataCharMember = property;
                    return;
                }
            }
        }
    }
}

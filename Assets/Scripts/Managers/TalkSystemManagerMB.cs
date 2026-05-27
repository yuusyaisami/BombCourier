using System;
using System.Threading;
using BC.ActionSystem;
using BC.Base;
using BC.Camera;
using BC.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.Managers
{
    public enum TalkStateId
    {
        None = 0,
        Normal1 = 10,
        Signature1 = 20,
        Surprised = 30,
        Happy = 40,
        Narrow = 50,
        Sad = 60,
        Angry = 70,
        Laugh = 80,
    }

    [Serializable]
    public struct TextEffectData
    {
        public bool applyFontSize;
        public int fontSize;
    }

    [Serializable]
    public struct TalkRequestData
    {
        private const int ActionToggleVersionEnabled = 1;

        public TalkStateId talkStateId; // 会話中の actor presentation を選ぶ状態。
        public CharacterIdReference speakerCharacter;
        public string speakerName;
        [TextArea]
        public string dialogueText;

        public TextEffectData textEffectData; // 文字サイズなど、会話テキストの見た目を制御する設定。

        public bool isWaitingActionCompleted; // 会話前後の inline action を完了待ちするか。
        public bool useOnStartTalkAction;
        public InlineAction onStartTalkAction; // 会話開始時に走らせる action。
        public bool useOnCompleteTalkAction;
        public InlineAction onCompleteTalkAction; // 会話終了時に走らせる action。

        [SerializeField, HideInInspector] private int actionToggleVersion;

        public InlineAction OnStartTalkAction => useOnStartTalkAction ? onStartTalkAction : null;
        public InlineAction OnCompleteTalkAction => useOnCompleteTalkAction ? onCompleteTalkAction : null;
        public bool HasSpeakerCharacter => speakerCharacter.IsAssigned;

        public string ResolveSpeakerDisplayName()
        {
            if (CharacterIdRegistry.TryGetDescriptor(speakerCharacter, out CharacterIdDescriptor descriptor))
            {
                if (!string.IsNullOrWhiteSpace(descriptor.DisplayName))
                    return descriptor.DisplayName;

                if (!string.IsNullOrWhiteSpace(descriptor.Path))
                    return descriptor.Path;
            }

            if (!string.IsNullOrWhiteSpace(speakerName))
                return speakerName;

            return speakerCharacter.IsAssigned
                ? speakerCharacter.ToString()
                : string.Empty;
        }

        public void EnsureInlineActionFlagsInitialized()
        {
            if (actionToggleVersion >= ActionToggleVersionEnabled)
                return;

            useOnStartTalkAction = false; // 既存の会話アクションはすべてオフにする。必要なものだけオンにしてもらう。
            useOnCompleteTalkAction = false;
            actionToggleVersion = ActionToggleVersionEnabled;
        }
    }

    [Serializable]
    public struct HideTalkRequestData
    {
        [Min(0f)]
        public float duration;
        public bool applyTalkStateOverride; // true のときだけ hide 時に talkStateId を actor へ流す。
        public TalkStateId talkStateId;

        public static HideTalkRequestData Default => new HideTalkRequestData
        {
            duration = 0.3f,
            applyTalkStateOverride = false,
            talkStateId = TalkStateId.None,
        };

        public float Duration => Mathf.Max(0f, duration);
    }

    public readonly struct TalkChoiceOptionRequestData
    {
        public readonly string DisplayText;

        public TalkChoiceOptionRequestData(string displayText)
        {
            DisplayText = displayText ?? string.Empty;
        }
    }

    public readonly struct TalkChoiceRequestData
    {
        public readonly TalkChoiceOptionRequestData[] Options;
        public readonly int DefaultSelectionIndex;
        public readonly bool WrapSelection;

        public TalkChoiceRequestData(
            TalkChoiceOptionRequestData[] options,
            int defaultSelectionIndex,
            bool wrapSelection)
        {
            Options = options ?? Array.Empty<TalkChoiceOptionRequestData>();
            DefaultSelectionIndex = defaultSelectionIndex;
            WrapSelection = wrapSelection;
        }

        public bool HasOptions => Options != null && Options.Length > 0;
    }

    public readonly struct TalkChoiceSelectionResult
    {
        public static readonly TalkChoiceSelectionResult None = new TalkChoiceSelectionResult(-1, string.Empty);

        public readonly int SelectedIndex;
        public readonly string SelectedText;

        public TalkChoiceSelectionResult(int selectedIndex, string selectedText)
        {
            SelectedIndex = selectedIndex;
            SelectedText = selectedText ?? string.Empty;
        }

        public bool HasSelection => SelectedIndex >= 0;
    }

    [Serializable]
    public class TalkSequenceData
    {
        public TalkRequestData[] talkRequests;
    }

    // 会話の再生順序と入力待機をまとめる MonoBehaviour。
    // UI はここ、camera focus は SceneCameraService、具体的な action は ActionSystem に委譲する。
    public class TalkSystemManagerMB : MonoBehaviour
    {
        public static TalkSystemManagerMB Instance { get; private set; }

        [SerializeField] private UITalkSystemMB talkSystemUIManagerMB;
        [SerializeField] private UITalkChoiceSystemMB talkChoiceUIManagerMB;

        private CancellationTokenSource cancellationTokenSource;
        private EntityRef activeTalkActor;
        private bool isConversationFocusActive;
        private SceneCameraFocusContext activeConversationFocusContext;
        private SceneKernel sceneKernel;
        private readonly CharacterDataBaseService characterDataBase = new();

        private void Awake()
        {
            talkSystemUIManagerMB ??= GetComponentInChildren<UITalkSystemMB>(true);
            talkChoiceUIManagerMB ??= GetComponentInChildren<UITalkChoiceSystemMB>(true);

            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }

            EndConversationCameraFocus();
            activeTalkActor = default;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 重複会話が来たら、古い処理を止めて新しい会話へ切り替える。
        public async UniTask ShowTalk(EntityRef actor, EntityRef viewer, TalkRequestData talkRequestData)
        {
            talkRequestData.EnsureInlineActionFlagsInitialized();
            talkRequestData.speakerName = talkRequestData.ResolveSpeakerDisplayName();

            if (talkSystemUIManagerMB == null)
            {
                Debug.LogWarning($"{nameof(TalkSystemManagerMB)}: ShowTalk skipped because {nameof(talkSystemUIManagerMB)} is null.", this);
                return;
            }

            // 連続で会話が来たら、前の待機処理を止めて新しい会話に切り替える。
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationTokenSource currentTalkCancellation = cancellationTokenSource;
            activeTalkActor = actor;

            BeginConversationCameraFocus(actor, viewer);

            await ExecuteInlineActionAsync(
                actor,
                viewer,
                talkRequestData.OnStartTalkAction,
                talkRequestData.isWaitingActionCompleted,
                currentTalkCancellation.Token,
                "start");

            if (currentTalkCancellation.IsCancellationRequested)
            {
                return;
            }

            talkChoiceUIManagerMB?.ClearChoicesImmediate();
            await talkSystemUIManagerMB.ShowTalk(talkRequestData, currentTalkCancellation.Token);

            // Typewriter 側 callback が取りこぼされても、ShowTalk 復帰時点で speaking 表現は必ず落とす。
            NotifyTalkTypingCompleted();

            if (currentTalkCancellation.IsCancellationRequested)
            {
                return;
            }

            // complete action では HideTalk が現在の会話待機 token を止めることがあります。
            // ここで同じ token を action 実行にも渡すと、HideTalk 自身が complete action を
            // 自己キャンセルしてしまい、UI が閉じる前に action 連鎖が中断されます。
            await ExecuteInlineActionAsync(
                actor,
                viewer,
                talkRequestData.OnCompleteTalkAction,
                talkRequestData.isWaitingActionCompleted,
                CancellationToken.None,
                "complete");

        }

        public void RegisterTalkAdapter(CharacterIdReference characterId, EntityRef entity, TalkAdapterMB adapter)
        {
            characterDataBase.Register(characterId, entity, adapter);
        }

        public void UnregisterTalkAdapter(TalkAdapterMB adapter)
        {
            characterDataBase.Unregister(adapter);
        }

        public bool TryResolveSpeakerAdapter(TalkRequestData talkRequestData, out TalkAdapterMB adapter, out EntityRef speakerEntity)
        {
            adapter = null;
            speakerEntity = default;

            if (!talkRequestData.HasSpeakerCharacter)
                return false;

            if (!TryResolveSceneKernel(out SceneKernel resolvedSceneKernel))
            {
                Debug.LogWarning($"{nameof(TalkSystemManagerMB)}: failed to resolve scene kernel for speaker lookup.", this);
                return false;
            }

            bool resolved = characterDataBase.TryResolveTalkAdapter(
                talkRequestData.speakerCharacter,
                resolvedSceneKernel,
                out adapter,
                out speakerEntity);

            if (!resolved)
            {
                Debug.LogWarning(
                    $"{nameof(TalkSystemManagerMB)}: speaker '{talkRequestData.speakerCharacter}' is not registered in {nameof(CharacterDataBaseService)}.",
                    this);
            }

            return resolved;
        }

        public async UniTask HideTalk(EntityRef actor, HideTalkRequestData requestData)
        {
            if (!activeTalkActor.IsValid)
            {
                Debug.LogWarning($"{nameof(TalkSystemManagerMB)}: HideTalk ignored because there is no active talk actor. requestActor={actor}", this);
                return;
            }

            if (!activeTalkActor.Equals(actor))
            {
                Debug.LogWarning($"{nameof(TalkSystemManagerMB)}: ignored hide request from non-active actor {actor}.", this);
                return;
            }

            // 非表示要求が来たら、待機中の入力処理を止める。
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }

            if (talkSystemUIManagerMB != null)
            {
                // UI を閉じるアニメーションを終えたあと、カメラを通常状態へ戻す。
                await talkSystemUIManagerMB.HideTalk(requestData.Duration);
            }

            talkChoiceUIManagerMB?.ClearChoicesImmediate();

            EndConversationCameraFocus();
            activeTalkActor = default;
        }

        public async UniTask<TalkChoiceSelectionResult> ShowChoicesAsync(
            TalkChoiceRequestData requestData,
            CancellationToken cancellationToken = default)
        {
            if (talkChoiceUIManagerMB == null || !requestData.HasOptions)
                return TalkChoiceSelectionResult.None;

            cancellationTokenSource ??= new CancellationTokenSource();

            using CancellationTokenSource linkedCancellation = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, cancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token);

            return await talkChoiceUIManagerMB.ShowChoicesAsync(
                requestData,
                talkSystemUIManagerMB != null ? talkSystemUIManagerMB.NextTalkInputAction : null,
                linkedCancellation.Token);
        }

        // Typewriter 完了時に speaking 表現を解除する通知入口。
        public void NotifyTalkTypingCompleted()
        {
            if (!activeTalkActor.IsValid)
                return;

            if (!TryResolveSceneKernel(out SceneKernel resolvedSceneKernel) || resolvedSceneKernel.EntityComponents == null)
                return;

            if (resolvedSceneKernel.EntityComponents.TryResolve(activeTalkActor, out TalkAdapterMB talkAdapter))
                talkAdapter.NotifyTalkTypingCompleted();
        }

        private void BeginConversationCameraFocus(EntityRef actor, EntityRef viewer)
        {
            SceneCameraFocusContext nextContext = new SceneCameraFocusContext(actor, viewer);

            // ShowTalk が連続で呼ばれても、同じ会話対象なら talk camera を再初期化しない。
            // HideTalk まで同じフォーカスを維持して、毎行ごとの位置リセットを防ぐ。
            if (isConversationFocusActive &&
                activeConversationFocusContext.FocusTargetEntity.Equals(nextContext.FocusTargetEntity) &&
                activeConversationFocusContext.ObserverEntity.Equals(nextContext.ObserverEntity))
            {
                return;
            }

            if (TryResolveSceneKernel(out SceneKernel resolvedSceneKernel) && resolvedSceneKernel.Cameras != null)
            {
                // 会話中の話者/observer 切り替えは focus を落とさず差し替える。
                // EndFocus を挟むと 1 フレームだけ入力ロックと TalkCamera が外れてしまう。
                resolvedSceneKernel.Cameras.BeginFocus(nextContext);
                activeConversationFocusContext = nextContext;
                isConversationFocusActive = true;
            }
        }

        private UniTask ExecuteInlineActionAsync(
            EntityRef actor,
            EntityRef viewer,
            InlineAction inlineAction,
            bool waitForCompletion,
            CancellationToken cancellationToken,
            string phaseLabel)
        {
            if (inlineAction == null)
            {
                return UniTask.CompletedTask;
            }

            if (!waitForCompletion)
            {
                InlineActionExecutionUtility.ExecuteDetachedAndForget(this, actor, inlineAction, viewer, $"Talk {phaseLabel}");
                return UniTask.CompletedTask;
            }

            return ExecuteInlineActionAwaitedAsync(actor, viewer, inlineAction, cancellationToken, phaseLabel);
        }

        private async UniTask ExecuteInlineActionAwaitedAsync(
            EntityRef actor,
            EntityRef viewer,
            InlineAction inlineAction,
            CancellationToken cancellationToken,
            string phaseLabel)
        {
            ActionExecutionResult result = await InlineActionExecutionUtility.ExecuteDetachedAsync(this, actor, inlineAction, viewer, cancellationToken);

            if (result.IsFailed)
            {
                Debug.LogWarning($"{nameof(TalkSystemManagerMB)}: failed to execute {phaseLabel} talk action. {result.Message}", this);
            }
        }

        private void EndConversationCameraFocus()
        {
            if (TryResolveSceneKernel(out SceneKernel resolvedSceneKernel) && resolvedSceneKernel.Cameras != null)
                resolvedSceneKernel.Cameras.EndFocus();

            activeConversationFocusContext = default;
            isConversationFocusActive = false;
        }

        private bool TryResolveSceneKernel(out SceneKernel resolvedSceneKernel)
        {
            if (sceneKernel != null)
            {
                resolvedSceneKernel = sceneKernel;
                return true;
            }

            SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

            if (kernelMB == null)
                kernelMB = UnityEngine.Object.FindAnyObjectByType<SceneKernelMB>();

            sceneKernel = kernelMB != null ? kernelMB.Kernel : null;
            resolvedSceneKernel = sceneKernel;
            return resolvedSceneKernel != null;
        }
    }
}
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
    [Serializable]
    public struct TextEffectData
    {
        public bool applyFontSize;
        public int fontSize;
    }

    [Serializable]
    public struct TalkRequestData
    {
        public string speakerName;
        [TextArea]
        public string dialogueText;

        public TextEffectData textEffectData;
        public bool isWaitingActionCompleted; // action の完了を待つかどうか
        public InlineAction onStartTalkAction; // 会話開始時に実行する action
        public InlineAction onCompleteTalkAction; // 会話終了時に実行する action
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

    public class TalkSystemManagerMB : MonoBehaviour
    {
        public static TalkSystemManagerMB Instance { get; private set; }

        [SerializeField] private UITalkSystemMB talkSystemUIManagerMB;
        [SerializeField] private UITalkChoiceSystemMB talkChoiceUIManagerMB;

        private CancellationTokenSource cancellationTokenSource;
        private SceneKernel sceneKernel;

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

            ReleaseTalkPresentation();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 重複命令が入った場合は cancel して新しい命令を実行する。
        public async UniTask ShowTalk(EntityRef actor, EntityRef viewer, TalkRequestData talkRequestData)
        {
            if (talkSystemUIManagerMB == null)
            {
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

            BeginTalkPresentation(actor, viewer);

            await ExecuteInlineActionAsync(
                actor,
                viewer,
                talkRequestData.onStartTalkAction,
                talkRequestData.isWaitingActionCompleted,
                currentTalkCancellation.Token,
                "start");

            if (currentTalkCancellation.IsCancellationRequested)
            {
                return;
            }

            talkChoiceUIManagerMB?.ClearChoicesImmediate();
            await talkSystemUIManagerMB.ShowTalk(talkRequestData, currentTalkCancellation.Token);

            if (currentTalkCancellation.IsCancellationRequested)
            {
                return;
            }

            await ExecuteInlineActionAsync(
                actor,
                viewer,
                talkRequestData.onCompleteTalkAction,
                talkRequestData.isWaitingActionCompleted,
                currentTalkCancellation.Token,
                "complete");
        }

        public async UniTask HideTalk(float duration = 0.3f)
        {
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
                await talkSystemUIManagerMB.HideTalk(duration);
            }

            talkChoiceUIManagerMB?.ClearChoicesImmediate();

            ReleaseTalkPresentation();
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

        private void BeginTalkPresentation(EntityRef actor, EntityRef viewer)
        {
            ReleaseTalkPresentation();

            if (TryResolveSceneKernel(out SceneKernel resolvedSceneKernel) && resolvedSceneKernel.Cameras != null)
                resolvedSceneKernel.Cameras.BeginTalk(new TalkCameraContext(actor, viewer));
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
                InlineActionExecutionUtility.ExecuteAndForget(this, actor, inlineAction, viewer, $"Talk {phaseLabel}");
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
            ActionExecutionResult result = await InlineActionExecutionUtility.ExecuteAsync(this, actor, inlineAction, viewer, cancellationToken);

            if (result.IsFailed)
            {
                Debug.LogWarning($"{nameof(TalkSystemManagerMB)}: failed to execute {phaseLabel} talk action. {result.Message}", this);
            }
        }

        private void ReleaseTalkPresentation()
        {
            if (TryResolveSceneKernel(out SceneKernel resolvedSceneKernel) && resolvedSceneKernel.Cameras != null)
                resolvedSceneKernel.Cameras.EndTalk();
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
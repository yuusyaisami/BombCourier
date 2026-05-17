using System;
using System.Threading;
using BC.ActionSystem;
using BC.Base;
using BC.UI;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
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
        public string dialogueText;
        public CinemachineCamera changeCinemachineCamera;

        public TextEffectData textEffectData;
        public bool isWaitingActionCompleted; // action の完了を待つかどうか
        public InlineAction onStartTalkAction; // 会話開始時に実行する action
        public InlineAction onCompleteTalkAction; // 会話終了時に実行する action
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

        // 会話中だけ前面に出すカメラと、通常時に戻すための優先度。
        [SerializeField] private int talkCameraPriority = 100;
        [SerializeField] private int inactiveCameraPriority = 0;

        private CancellationTokenSource cancellationTokenSource;
        private CinemachineCamera activeTalkCamera;

        private void Awake()
        {
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

            SetCameraPriority(activeTalkCamera, inactiveCameraPriority);
            activeTalkCamera = null;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 重複命令が入った場合は cancel して新しい命令を実行する。
        public async UniTask ShowTalk(EntityRef actor, TalkRequestData talkRequestData)
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

            // 会話用カメラを前面に出してから UI を表示する。
            SetCameraPriority(activeTalkCamera, inactiveCameraPriority);
            activeTalkCamera = talkRequestData.changeCinemachineCamera;
            SetCameraPriority(activeTalkCamera, talkCameraPriority);

            await ExecuteInlineActionAsync(
                actor,
                talkRequestData.onStartTalkAction,
                talkRequestData.isWaitingActionCompleted,
                currentTalkCancellation.Token,
                "start");

            if (currentTalkCancellation.IsCancellationRequested)
            {
                return;
            }

            await talkSystemUIManagerMB.ShowTalk(talkRequestData, currentTalkCancellation.Token);

            if (currentTalkCancellation.IsCancellationRequested)
            {
                return;
            }

            await ExecuteInlineActionAsync(
                actor,
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

            SetCameraPriority(activeTalkCamera, inactiveCameraPriority);
            activeTalkCamera = null;
        }

        private static void SetCameraPriority(CinemachineCamera camera, int priority)
        {
            if (camera == null)
            {
                return;
            }

            // Cinemachine 3系の PrioritySettings を使って優先度だけ差し替える。
            PrioritySettings settings = camera.Priority;
            settings.Enabled = true;
            settings.Value = priority;
            camera.Priority = settings;
        }

        private UniTask ExecuteInlineActionAsync(
            EntityRef actor,
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
                InlineActionExecutionUtility.ExecuteAndForget(this, actor, inlineAction, default, $"Talk {phaseLabel}");
                return UniTask.CompletedTask;
            }

            return ExecuteInlineActionAwaitedAsync(actor, inlineAction, cancellationToken, phaseLabel);
        }

        private async UniTask ExecuteInlineActionAwaitedAsync(
            EntityRef actor,
            InlineAction inlineAction,
            CancellationToken cancellationToken,
            string phaseLabel)
        {
            ActionExecutionResult result = await InlineActionExecutionUtility.ExecuteAsync(this, actor, inlineAction, default, cancellationToken);

            if (result.IsFailed)
            {
                Debug.LogWarning($"{nameof(TalkSystemManagerMB)}: failed to execute {phaseLabel} talk action. {result.Message}", this);
            }
        }
    }
}
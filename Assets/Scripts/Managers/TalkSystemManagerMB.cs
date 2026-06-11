using System;
using System.Threading;
using BC.ActionSystem;
using BC.Audio;
using BC.Base;
using BC.Camera;
using BC.Localization;
using BC.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

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
        [TextArea]
        public string dialogueText;

        // ローカライズ: セリフ表示テキスト。entry が見つからなければ上の dialogueText をフォールバック表示する。
        public LocalizedStringTable table;
        public string entry;
        public bool applySetTable;

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
            return TalkSpeakerDisplayNameUtility.ResolveSpeakerDisplayName(speakerCharacter);
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

    internal static class TalkSpeakerDisplayNameUtility
    {
        public static string ResolveSpeakerDisplayName(CharacterIdReference speakerCharacter)
        {
            if (CharacterIdRegistry.TryGetDescriptor(speakerCharacter, out CharacterIdDescriptor descriptor))
            {
                if (!string.IsNullOrWhiteSpace(descriptor.DisplayName))
                    return descriptor.DisplayName;

                if (!string.IsNullOrWhiteSpace(descriptor.Path))
                    return descriptor.Path;
            }

            return speakerCharacter.IsAssigned
                ? speakerCharacter.ToString()
                : string.Empty;
        }
    }

    [Serializable]
    public struct DialogueRequestData
    {
        private const int ActionToggleVersionEnabled = 1;

        public CharacterIdReference speakerCharacter;
        [TextArea]
        public string dialogueText;

        // ローカライズ: セリフ表示テキスト。entry が見つからなければ上の dialogueText をフォールバック表示する。
        public LocalizedStringTable table;
        public string entry;
        public bool applySetTable;

        public TextEffectData textEffectData;
        [Min(0f)]
        public float hideDuration;

        public bool isWaitingActionCompleted;
        public bool useOnStartDialogueAction;
        public InlineAction onStartDialogueAction;
        public bool useOnCompleteDialogueAction;
        public InlineAction onCompleteDialogueAction;

        [SerializeField, HideInInspector] private int actionToggleVersion;

        public InlineAction OnStartDialogueAction => useOnStartDialogueAction ? onStartDialogueAction : null;
        public InlineAction OnCompleteDialogueAction => useOnCompleteDialogueAction ? onCompleteDialogueAction : null;
        public bool HasSpeakerCharacter => speakerCharacter.IsAssigned;
        public float HideDuration => Mathf.Max(0f, hideDuration);

        public string ResolveSpeakerDisplayName()
        {
            return TalkSpeakerDisplayNameUtility.ResolveSpeakerDisplayName(speakerCharacter);
        }

        public void EnsureInlineActionFlagsInitialized()
        {
            if (actionToggleVersion >= ActionToggleVersionEnabled)
                return;

            useOnStartDialogueAction = false;
            useOnCompleteDialogueAction = false;
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
        public readonly LocalizedStringTable Table;
        public readonly string Entry;
        public readonly bool ApplySetTable;
        public readonly string FallbackText;
        public readonly string DisplayText; // 解決済みの表示テキスト（未解決時は FallbackText）。

        public TalkChoiceOptionRequestData(LocalizedStringTable table, string entry, bool applySetTable, string fallbackText)
        {
            Table = table;
            Entry = entry ?? string.Empty;
            ApplySetTable = applySetTable;
            FallbackText = fallbackText ?? string.Empty;
            DisplayText = FallbackText;
        }

        private TalkChoiceOptionRequestData(
            LocalizedStringTable table, string entry, bool applySetTable, string fallbackText, string displayText)
        {
            Table = table;
            Entry = entry ?? string.Empty;
            ApplySetTable = applySetTable;
            FallbackText = fallbackText ?? string.Empty;
            DisplayText = string.IsNullOrEmpty(displayText) ? FallbackText : displayText;
        }

        public TalkChoiceOptionRequestData WithResolvedDisplayText(string resolvedDisplayText)
            => new TalkChoiceOptionRequestData(Table, Entry, ApplySetTable, FallbackText, resolvedDisplayText);
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
        private static readonly ValueModifierTagId DialogueInputLockTag = new ValueModifierTagId(15001);

        public static TalkSystemManagerMB Instance { get; private set; }

        [SerializeField] private UITalkSystemMB talkSystemUIManagerMB;
        [SerializeField] private UITalkChoiceSystemMB talkChoiceUIManagerMB;

        [Header("Localization")]
        [Tooltip("話者名を解決する統一 String Table。Key は SpeakerCharacter の Path（例: Npc.Vanilla）。見つからなければ DisplayName。")]
        public LocalizedStringTable speakerNameTable;

        [Header("Camera")]
        [SerializeField] private bool lockConversationFocusUntilHide = true;

        private CancellationTokenSource cancellationTokenSource;
        private EntityRef activeTalkOwnerActor;
        private EntityRef activeTalkPresentationActor;
        private TalkAdapterMB activeTalkPresentationAdapter;
        private bool isConversationFocusActive;
        private SceneCameraFocusContext activeConversationFocusContext;
        private SceneKernel sceneKernel;
        private readonly CharacterDataBaseService characterDataBase = new();
        private EntityRef activeDialogueInputLockEntity;
        private bool dialogueInputLockActive;

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
            ClearDialogueInputLock();
            activeTalkOwnerActor = default;
            activeTalkPresentationActor = default;
            activeTalkPresentationAdapter = null;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 重複会話が来たら、古い処理を止めて新しい会話へ切り替える。
        public async UniTask ShowTalk(
            EntityRef actor,
            EntityRef viewer,
            TalkRequestData talkRequestData,
            CancellationToken cancellationToken = default)
        {
            await ShowTalk(actor, actor, null, viewer, talkRequestData, cancellationToken);
        }

        public async UniTask<bool> ShowDialogue(
            EntityRef actor,
            EntityRef viewer,
            DialogueRequestData dialogueRequestData,
            CancellationToken cancellationToken = default)
        {
            dialogueRequestData.EnsureInlineActionFlagsInitialized();
            dialogueRequestData.dialogueText = await ResolveLocalizedTextAsync(
                dialogueRequestData.table,
                dialogueRequestData.entry,
                dialogueRequestData.applySetTable,
                dialogueRequestData.dialogueText,
                dialogueHeldTable);
            string dialogueSpeakerName = await ResolveSpeakerDisplayNameAsync(dialogueRequestData.speakerCharacter);

            if (talkSystemUIManagerMB == null)
            {
                Debug.LogWarning($"{nameof(TalkSystemManagerMB)}: ShowDialogue skipped because {nameof(talkSystemUIManagerMB)} is null.", this);
                return false;
            }

            ClearDialogueInputLock();

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationTokenSource currentDialogueCancellation = cancellationTokenSource;

            using CancellationTokenSource linkedCancellation = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(currentDialogueCancellation.Token, cancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(currentDialogueCancellation.Token);

            CancellationToken effectiveCancellation = linkedCancellation.Token;

            activeTalkOwnerActor = actor;
            activeTalkPresentationActor = actor;
            activeTalkPresentationAdapter = null;
            ApplyDialogueInputLock(ResolveDialogueInputLockEntity(actor, viewer));

            // ShowDialogue は TalkAdapter を介さない純粋なダイアログ表示なので、
            // TalkCamera / focus 演出は開始しない。
            // 既存の会話 focus が残っていた場合だけ明示的に解除する。
            EndConversationCameraFocus();

            await ExecuteInlineActionAsync(
                activeTalkOwnerActor,
                viewer,
                dialogueRequestData.OnStartDialogueAction,
                dialogueRequestData.isWaitingActionCompleted,
                effectiveCancellation,
                "start dialogue");

            if (effectiveCancellation.IsCancellationRequested)
                return false;

            talkChoiceUIManagerMB?.ClearChoicesImmediate();
            talkSystemUIManagerMB.UseDefaultCharacterSound();

            await talkSystemUIManagerMB.ShowTalk(ToTalkRequestData(dialogueRequestData), dialogueSpeakerName, effectiveCancellation);

            NotifyTalkTypingCompleted();

            if (effectiveCancellation.IsCancellationRequested)
                return false;

            await ExecuteInlineActionAsync(
                activeTalkOwnerActor,
                viewer,
                dialogueRequestData.OnCompleteDialogueAction,
                dialogueRequestData.isWaitingActionCompleted,
                CancellationToken.None,
                "complete dialogue");

            return true;
        }

        internal async UniTask ShowTalk(
            EntityRef ownerActor,
            EntityRef presentationActor,
            TalkAdapterMB presentationAdapter,
            EntityRef viewer,
            TalkRequestData talkRequestData,
            CancellationToken cancellationToken = default)
        {
            talkRequestData.EnsureInlineActionFlagsInitialized();
            talkRequestData.dialogueText = await ResolveLocalizedTextAsync(
                talkRequestData.table,
                talkRequestData.entry,
                talkRequestData.applySetTable,
                talkRequestData.dialogueText,
                dialogueHeldTable);
            string talkSpeakerName = await ResolveSpeakerDisplayNameAsync(talkRequestData.speakerCharacter);

            if (talkSystemUIManagerMB == null)
            {
                Debug.LogWarning($"{nameof(TalkSystemManagerMB)}: ShowTalk skipped because {nameof(talkSystemUIManagerMB)} is null.", this);
                return;
            }

            // 連続で会話が来たら、前の待機処理を止めて新しい会話に切り替える。
            ClearDialogueInputLock();

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }

            cancellationTokenSource = new CancellationTokenSource();
            CancellationTokenSource currentTalkCancellation = cancellationTokenSource;
            // 会話は「新しい会話開始/HideTalk による内部中断」と
            // 「Action sequence 側からの外部 cancellation」の2系統で止まる。
            // linked token に集約して UI/typewriter/start action へ渡し、どの層で待っていても
            // 同じ中断要求で抜けられるようにする。
            using CancellationTokenSource linkedCancellation = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(currentTalkCancellation.Token, cancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(currentTalkCancellation.Token);
            CancellationToken effectiveCancellation = linkedCancellation.Token;

            activeTalkOwnerActor = ownerActor.IsValid ? ownerActor : presentationActor;
            activeTalkPresentationActor = presentationActor.IsValid ? presentationActor : activeTalkOwnerActor;
            activeTalkPresentationAdapter = presentationAdapter;

            EntityRef focusTargetActor = ResolveCameraFocusActor(activeTalkPresentationActor, activeTalkOwnerActor, viewer);
            BeginConversationCameraFocus(focusTargetActor, viewer);

            try
            {
                await ExecuteInlineActionAsync(
                    activeTalkOwnerActor,
                    viewer,
                    talkRequestData.OnStartTalkAction,
                    talkRequestData.isWaitingActionCompleted,
                    effectiveCancellation,
                    "start");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 外部 cancellation は HideTalk を経由しないため、ここで UI と会話所有状態を
                // 明示的に戻す。これをしないと入力ロックや camera focus が残り得る。
                await CleanupExternallyCanceledTalkAsync(currentTalkCancellation);
                throw;
            }

            if (effectiveCancellation.IsCancellationRequested)
            {
                await ThrowIfExternalTalkCancellationAsync(currentTalkCancellation, cancellationToken);
                return;
            }

            talkChoiceUIManagerMB?.ClearChoicesImmediate();

            // 話者の TalkAdapterMB からキャラクターサウンドを取得して UI に渡す。
            {
                AudioDataSO characterSound = ResolveActivePresentationAdapter()?.TalkCharacterSound;
                talkSystemUIManagerMB.SetCharacterSound(characterSound);
            }

            try
            {
                await talkSystemUIManagerMB.ShowTalk(talkRequestData, talkSpeakerName, effectiveCancellation);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 表示 tween / typewriter 待機中の cancellation も、start action と同じ cleanup 契約に揃える。
                await CleanupExternallyCanceledTalkAsync(currentTalkCancellation);
                throw;
            }

            // Typewriter 側 callback が取りこぼされても、ShowTalk 復帰時点で speaking 表現は必ず落とす。
            NotifyTalkTypingCompleted();

            if (effectiveCancellation.IsCancellationRequested)
            {
                await ThrowIfExternalTalkCancellationAsync(currentTalkCancellation, cancellationToken);
                return;
            }

            // complete action では HideTalk が現在の会話待機 token を止めることがあります。
            // ここで同じ token を action 実行にも渡すと、HideTalk 自身が complete action を
            // 自己キャンセルしてしまい、UI が閉じる前に action 連鎖が中断されます。
            await ExecuteInlineActionAsync(
                activeTalkOwnerActor,
                viewer,
                talkRequestData.OnCompleteTalkAction,
                talkRequestData.isWaitingActionCompleted,
                CancellationToken.None,
                "complete");

        }

        private async UniTask ThrowIfExternalTalkCancellationAsync(
            CancellationTokenSource currentTalkCancellation,
            CancellationToken externalCancellation)
        {
            if (!externalCancellation.IsCancellationRequested)
                return;

            // effectiveCancellation だけを見ると、内部 HideTalk 由来の cancel と区別できない。
            // 呼び出し元へ OperationCanceledException を返すのは、Action 側が明示的に止めた場合だけにする。
            await CleanupExternallyCanceledTalkAsync(currentTalkCancellation);
            externalCancellation.ThrowIfCancellationRequested();
        }

        private async UniTask CleanupExternallyCanceledTalkAsync(CancellationTokenSource currentTalkCancellation)
        {
            // すでに次の会話が開始されている場合、古い cancellation の cleanup で
            // 新しい会話の UI / camera focus / input lock を壊さない。
            if (!ReferenceEquals(cancellationTokenSource, currentTalkCancellation))
                return;

            currentTalkCancellation.Cancel();
            currentTalkCancellation.Dispose();
            cancellationTokenSource = null;

            // 外部 cancellation では HideTalk request data が存在しない。
            // tween を待たず即時に閉じ、Action 側の中断から1フレーム以上 UI が残る事故を避ける。
            if (talkSystemUIManagerMB != null)
                await talkSystemUIManagerMB.HideTalk(0f);

            talkChoiceUIManagerMB?.ClearChoicesImmediate();

            // TalkAdapter は「表示中の talk state」を持つため、UIを閉じるだけでは足りない。
            // HideTalk と同じ通知を送り、animation parameter / presentation state を戻す。
            HideTalkRequestData cancelHideRequest = HideTalkRequestData.Default;
            cancelHideRequest.duration = 0f;
            ResolveActivePresentationAdapter()?.HandleTalkHidden(cancelHideRequest);

            EndConversationCameraFocus();
            ClearDialogueInputLock();
            activeTalkOwnerActor = default;
            activeTalkPresentationActor = default;
            activeTalkPresentationAdapter = null;
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

            if (!TryResolveSceneKernel(out SceneKernel resolvedSceneKernel))
            {
                Debug.LogWarning($"{nameof(TalkSystemManagerMB)}: failed to resolve scene kernel for speaker lookup.", this);
                return false;
            }

            if (!talkRequestData.HasSpeakerCharacter)
                return false;

            bool resolved = characterDataBase.TryResolveTalkAdapter(
                talkRequestData.speakerCharacter,
                resolvedSceneKernel,
                out adapter,
                out speakerEntity);

            if (!resolved)
            {
                Debug.LogWarning(
                    $"{nameof(TalkSystemManagerMB)}: speaker lookup failed. speakerCharacter='{talkRequestData.speakerCharacter}'.",
                    this);
            }

            return resolved;
        }

        public async UniTask<bool> HideTalk(EntityRef actor, HideTalkRequestData requestData)
        {
            if (!activeTalkOwnerActor.IsValid && !activeTalkPresentationActor.IsValid)
            {
                Debug.LogWarning($"{nameof(TalkSystemManagerMB)}: HideTalk ignored because there is no active talk actor. requestActor={actor}", this);
                return false;
            }

            bool isOwnerActor = activeTalkOwnerActor.IsValid && activeTalkOwnerActor.Equals(actor);
            bool isPresentationActor = activeTalkPresentationActor.IsValid && activeTalkPresentationActor.Equals(actor);
            if (!isOwnerActor && !isPresentationActor)
            {
                Debug.LogWarning($"{nameof(TalkSystemManagerMB)}: ignored hide request from non-active actor {actor}.", this);
                return false;
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
            ResolveActivePresentationAdapter()?.HandleTalkHidden(requestData);

            EndConversationCameraFocus();
            ClearDialogueInputLock();
            activeTalkOwnerActor = default;
            activeTalkPresentationActor = default;
            activeTalkPresentationAdapter = null;
            return true;
        }

        public async UniTask<TalkChoiceSelectionResult> ShowChoicesAsync(
            TalkChoiceRequestData requestData,
            CancellationToken cancellationToken = default)
        {
            if (talkChoiceUIManagerMB == null || !requestData.HasOptions)
                return TalkChoiceSelectionResult.None;

            // 各オプションの表示テキストを保有Table＋Entryから順番に解決し、解決済みで再構築する。
            TalkChoiceOptionRequestData[] sourceOptions = requestData.Options;
            TalkChoiceOptionRequestData[] resolvedOptions = new TalkChoiceOptionRequestData[sourceOptions.Length];
            for (int i = 0; i < sourceOptions.Length; i++)
            {
                string resolvedText = await ResolveLocalizedTextAsync(
                    sourceOptions[i].Table,
                    sourceOptions[i].Entry,
                    sourceOptions[i].ApplySetTable,
                    sourceOptions[i].FallbackText,
                    dialogueHeldTable);
                resolvedOptions[i] = sourceOptions[i].WithResolvedDisplayText(resolvedText);
            }

            TalkChoiceRequestData resolvedRequest = new TalkChoiceRequestData(
                resolvedOptions, requestData.DefaultSelectionIndex, requestData.WrapSelection);

            cancellationTokenSource ??= new CancellationTokenSource();

            using CancellationTokenSource linkedCancellation = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, cancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token);

            return await talkChoiceUIManagerMB.ShowChoicesAsync(
                resolvedRequest,
                talkSystemUIManagerMB != null ? talkSystemUIManagerMB.NextTalkInputAction : null,
                linkedCancellation.Token);
        }

        // Typewriter 完了時に speaking 表現を解除する通知入口。
        public void NotifyTalkTypingCompleted()
        {
            TalkAdapterMB talkAdapter = ResolveActivePresentationAdapter();
            if (talkAdapter == null)
                return;

            talkAdapter.NotifyTalkTypingCompleted();
        }

        private void BeginConversationCameraFocus(EntityRef actor, EntityRef viewer)
        {
            SceneCameraFocusContext nextContext = new SceneCameraFocusContext(actor, viewer);

            // 会話表示中は初回のフォーカス中心を維持し、話者切替によるカメラの揺れを防ぐ。
            if (isConversationFocusActive && lockConversationFocusUntilHide)
                return;

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

        private EntityRef ResolveCameraFocusActor(EntityRef presentationActor, EntityRef ownerActor, EntityRef viewer)
        {
            EntityRef focusActor = presentationActor.IsValid ? presentationActor : ownerActor;

            // Talk camera の observer は実質 player を基準に解決されるため、
            // focus target も player だと重心が player 単体へ寄ってしまう。
            // 可能なら player 以外を優先して会話の中点を安定させる。
            if (focusActor.IsValid && IsPlayerEntity(focusActor) && viewer.IsValid && !IsPlayerEntity(viewer))
                return viewer;

            if (ownerActor.IsValid && IsPlayerEntity(ownerActor) && viewer.IsValid && !IsPlayerEntity(viewer))
                return viewer;

            if (viewer.IsValid && IsPlayerEntity(viewer) && focusActor.IsValid && !IsPlayerEntity(focusActor))
                return focusActor;

            if (focusActor.IsValid && viewer.IsValid && !focusActor.Equals(viewer))
                return focusActor;

            if (ownerActor.IsValid)
                return ownerActor;

            return focusActor;
        }

        private bool IsPlayerEntity(EntityRef entity)
        {
            if (!entity.IsValid)
                return false;

            if (!TryResolveSceneKernel(out SceneKernel resolvedSceneKernel) || resolvedSceneKernel.EntityComponents == null)
                return false;

            if (!resolvedSceneKernel.EntityComponents.TryResolve(entity, out EntityMB entityMB) || entityMB == null)
                return false;

            return entityMB.Tag.Equals(EntityTags.Actor.Player.Id);
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

        private sealed class HeldTableState
        {
            public TableReference Table;
            public bool HasTable;
        }

        // セリフ用に「直前に使った Table」を保有する。
        private readonly HeldTableState dialogueHeldTable = new();

        // セリフ本文を「保有Table ＋ Entry」から現在ロケールで解決する。見つからなければ fallback を返す。
        private async UniTask<string> ResolveLocalizedTextAsync(
            LocalizedStringTable tableField,
            string entry,
            bool applySetTable,
            string fallback,
            HeldTableState held)
        {
            fallback ??= string.Empty;

            // Apply Set Table が true かつ Table 指定あり → それを使用し保有更新。else → 保有Table。
            TableReference table;
            bool haveTable;
            if (applySetTable && tableField != null && tableField.TableReference.ReferenceType != TableReference.Type.Empty)
            {
                table = tableField.TableReference;
                held.Table = table;
                held.HasTable = true;
                haveTable = true;
            }
            else
            {
                table = held.Table;
                haveTable = held.HasTable;
            }

            if (!haveTable)
                return fallback;

            return await LocalizedStringResolver.ResolveAsync(table, entry, fallback);
        }

        // 話者名を SpeakerCharacter から解決する。統一 speakerNameTable の Key には descriptor.Path を使い、
        // 見つからなければ descriptor.DisplayName（[CharacterDisplayName]）をフォールバックにする。
        private async UniTask<string> ResolveSpeakerDisplayNameAsync(CharacterIdReference speakerCharacter)
        {
            if (!speakerCharacter.IsAssigned)
                return string.Empty;

            string fallbackName = TalkSpeakerDisplayNameUtility.ResolveSpeakerDisplayName(speakerCharacter);

            if (!CharacterIdRegistry.TryGetDescriptor(speakerCharacter, out CharacterIdDescriptor descriptor) ||
                string.IsNullOrWhiteSpace(descriptor.Path))
                return fallbackName;

            return await LocalizedStringResolver.ResolveAsync(speakerNameTable, descriptor.Path, fallbackName);
        }

        private static TalkRequestData ToTalkRequestData(DialogueRequestData dialogueRequestData)
        {
            dialogueRequestData.EnsureInlineActionFlagsInitialized();

            return new TalkRequestData
            {
                talkStateId = TalkStateId.None,
                speakerCharacter = dialogueRequestData.speakerCharacter,
                dialogueText = dialogueRequestData.dialogueText,
                textEffectData = dialogueRequestData.textEffectData,
                isWaitingActionCompleted = false,
                useOnStartTalkAction = false,
                onStartTalkAction = null,
                useOnCompleteTalkAction = false,
                onCompleteTalkAction = null,
            };
        }

        private void EndConversationCameraFocus()
        {
            if (TryResolveSceneKernel(out SceneKernel resolvedSceneKernel) && resolvedSceneKernel.Cameras != null)
                resolvedSceneKernel.Cameras.EndFocus();

            activeConversationFocusContext = default;
            isConversationFocusActive = false;
        }

        private EntityRef ResolveDialogueInputLockEntity(EntityRef actor, EntityRef viewer)
        {
            if (viewer.IsValid)
                return viewer;

            return actor;
        }

        private void ApplyDialogueInputLock(EntityRef targetEntity)
        {
            ClearDialogueInputLock();

            if (!targetEntity.IsValid)
                return;

            if (!TryResolveSceneKernel(out SceneKernel resolvedSceneKernel) || resolvedSceneKernel.ValueStore == null)
                return;

            resolvedSceneKernel.ValueStore.SetBoolModifier(targetEntity, ValueKeys.Move.CanMoveByInput, DialogueInputLockTag, false);
            resolvedSceneKernel.ValueStore.SetBoolModifier(targetEntity, ValueKeys.Interaction.CanInteract, DialogueInputLockTag, false);
            activeDialogueInputLockEntity = targetEntity;
            dialogueInputLockActive = true;
        }

        private void ClearDialogueInputLock()
        {
            if (!dialogueInputLockActive)
            {
                activeDialogueInputLockEntity = default;
                return;
            }

            if (activeDialogueInputLockEntity.IsValid &&
                TryResolveSceneKernel(out SceneKernel resolvedSceneKernel) &&
                resolvedSceneKernel.ValueStore != null)
            {
                resolvedSceneKernel.ValueStore.RemoveBoolModifier(activeDialogueInputLockEntity, ValueKeys.Move.CanMoveByInput, DialogueInputLockTag);
                resolvedSceneKernel.ValueStore.RemoveBoolModifier(activeDialogueInputLockEntity, ValueKeys.Interaction.CanInteract, DialogueInputLockTag);
            }

            activeDialogueInputLockEntity = default;
            dialogueInputLockActive = false;
        }

        private TalkAdapterMB ResolveActivePresentationAdapter()
        {
            if (activeTalkPresentationAdapter != null)
                return activeTalkPresentationAdapter;

            if (!activeTalkPresentationActor.IsValid)
                return null;

            if (!TryResolveSceneKernel(out SceneKernel resolvedKernel) || resolvedKernel.EntityComponents == null)
                return null;

            if (!resolvedKernel.EntityComponents.TryResolve(activeTalkPresentationActor, out TalkAdapterMB talkAdapter))
                return null;

            activeTalkPresentationAdapter = talkAdapter;
            return talkAdapter;
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

using System;
using System.Threading;
using BC.Animation;
using BC.Audio;
using BC.Base;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Sirenix.OdinInspector;

namespace BC.Managers
{
    [Serializable]
    public struct TalkAnimatorParameterWrite
    {
        public EntityAnimatorParameterWriteMode writeMode;
        public string parameterName;
        [ShowIf(nameof(SupportsAutoReset))] public bool applyAutoReset;
        [SerializeField, HideInInspector] private bool autoResetInitialized;
        [ShowIf(nameof(writeMode), EntityAnimatorParameterWriteMode.SetBool)] public bool boolValue;
        [ShowIf(nameof(writeMode), EntityAnimatorParameterWriteMode.SetFloat)] public float floatValue;
        [ShowIf(nameof(writeMode), EntityAnimatorParameterWriteMode.SetInteger)] public int intValue;
        [ShowIf(nameof(ShouldShowManualResetBool))] public bool resetBoolValue;
        [ShowIf(nameof(ShouldShowManualResetFloat))] public float resetFloatValue;
        [ShowIf(nameof(ShouldShowManualResetInt))] public int resetIntValue;

        public void ApplyEnter(EntityAnimationMB entityAnimation)
        {
            if (entityAnimation == null || string.IsNullOrWhiteSpace(parameterName))
                return;

            entityAnimation.TryApplyParameter(writeMode, parameterName, boolValue, floatValue, intValue);
        }

        public void ApplyExit(EntityAnimationMB entityAnimation)
        {
            if (entityAnimation == null || string.IsNullOrWhiteSpace(parameterName) || !SupportsAutoReset)
                return;

            switch (writeMode)
            {
                case EntityAnimatorParameterWriteMode.SetBool:
                    entityAnimation.SetBool(parameterName, applyAutoReset ? false : resetBoolValue);
                    break;

                case EntityAnimatorParameterWriteMode.SetFloat:
                    entityAnimation.SetFloat(parameterName, applyAutoReset ? 0f : resetFloatValue);
                    break;

                case EntityAnimatorParameterWriteMode.SetInteger:
                    entityAnimation.SetInteger(parameterName, applyAutoReset ? 0 : resetIntValue);
                    break;
            }
        }

        public void Apply(EntityAnimationMB entityAnimation)
        {
            ApplyEnter(entityAnimation);
        }

        public bool CanApplyExitReset =>
            SupportsAutoReset &&
            !string.IsNullOrWhiteSpace(parameterName);

        public void EnsureDefaultAutoReset()
        {
            if (autoResetInitialized)
                return;

            applyAutoReset = true;
            autoResetInitialized = true;
        }

        private bool SupportsAutoReset =>
            writeMode == EntityAnimatorParameterWriteMode.SetBool ||
            writeMode == EntityAnimatorParameterWriteMode.SetFloat ||
            writeMode == EntityAnimatorParameterWriteMode.SetInteger;

        private bool ShouldShowManualResetBool => SupportsAutoReset && !applyAutoReset && writeMode == EntityAnimatorParameterWriteMode.SetBool;
        private bool ShouldShowManualResetFloat => SupportsAutoReset && !applyAutoReset && writeMode == EntityAnimatorParameterWriteMode.SetFloat;
        private bool ShouldShowManualResetInt => SupportsAutoReset && !applyAutoReset && writeMode == EntityAnimatorParameterWriteMode.SetInteger;
    }

    [Serializable]
    public struct TalkStatePresentationEntry
    {
        public TalkStateId talkStateId;
        public bool applyFaceExpression;
        [ShowIf(nameof(applyFaceExpression))]
        public FaceExpressionId faceExpressionId;
        public bool applyShapeExpression;
        [ShowIf(nameof(applyShapeExpression))]
        public ShapeExpressionId shapeExpressionId;
        public TalkAnimatorParameterWrite[] parameterWrites;

        public void ApplyEnter(EntityAnimationMB entityAnimation, ValueStoreService valueStore, EntityRef entity)
        {
            if (applyFaceExpression && valueStore != null && entity.IsValid)
                valueStore.Set(entity, ValueKeys.Runtime.FaceExpression, faceExpressionId);

            if (applyShapeExpression && valueStore != null && entity.IsValid)
                valueStore.Set(entity, ValueKeys.Runtime.ShapeExpression, shapeExpressionId);

            if (parameterWrites == null || entityAnimation == null)
                return;

            for (int i = 0; i < parameterWrites.Length; i++)
                parameterWrites[i].ApplyEnter(entityAnimation);
        }

        public void ApplyExit(EntityAnimationMB entityAnimation)
        {
            if (parameterWrites == null || entityAnimation == null)
                return;

            for (int i = 0; i < parameterWrites.Length; i++)
                parameterWrites[i].ApplyExit(entityAnimation);
        }

        public void Apply(EntityAnimationMB entityAnimation, ValueStoreService valueStore, EntityRef entity)
        {
            ApplyEnter(entityAnimation, valueStore, entity);
        }
    }

    [Serializable]
    public struct TalkActivityPresentationSettings
    {
        public string[] talkingBoolParameterNames;
        public bool applyTalkingShapeExpression;
        [ShowIf(nameof(applyTalkingShapeExpression))]
        public ShapeExpressionId talkingShapeExpressionId;
        public bool applyNonTalkingShapeExpression;
        [ShowIf(nameof(applyNonTalkingShapeExpression))]
        public ShapeExpressionId nonTalkingShapeExpressionId;
        [ShowIf(nameof(applyTalkingShapeExpression))]
        public bool restoreStateShapeExpressionOnStop;

        public void Apply(EntityAnimationMB entityAnimation, bool isTalking)
        {
            if (entityAnimation == null || talkingBoolParameterNames == null)
                return;

            for (int i = 0; i < talkingBoolParameterNames.Length; i++)
            {
                string parameterName = talkingBoolParameterNames[i];

                if (string.IsNullOrWhiteSpace(parameterName))
                    continue;

                entityAnimation.SetBool(parameterName, isTalking);
            }
        }
    }

    // actor Entity 側で talk presentation と TalkSystemManager の橋渡しをまとめる component。
    // Action runtime は TalkSystemManager を直接叩かず、必ずこの adapter を経由して talk を開始・終了する。
    [DisallowMultipleComponent]
    public sealed class TalkAdapterMB : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private EntityMB entityMB;
        [SerializeField] private EntityAnimationMB entityAnimation;
        [SerializeField] private SceneKernelMB sceneKernelMB;

        [Header("Character")]
        [SerializeField] private CharacterIdReference characterId;

        [Header("Presentation")]
        [SerializeField] private TalkStateId defaultIdleTalkState = TalkStateId.None;
        [SerializeField] private TalkStatePresentationEntry[] statePresentations = Array.Empty<TalkStatePresentationEntry>();
        [SerializeField] private TalkActivityPresentationSettings activityPresentation;

        [Header("Sound")]
        // テキスト表示時に文字単位で再生するサウンド。キャラクターごとに設定する。
        [SerializeField] private AudioDataSO talkCharacterSound;

        private EntityRef entityRef;
        private ValueStoreService valueStore;
        private bool missingAnimationWarningLogged;
        private bool missingValueStoreWarningLogged;
        private bool hasLastStateShapeExpression;
        private ShapeExpressionId lastStateShapeExpression;
        private bool hasCurrentTalkStatePresentation;
        private TalkStateId currentTalkStateId;
        private int talkStatePresentationVersion;
        private int currentTalkStatePresentationVersion;
        private bool isTalkingActivityActive;
        private bool isCharacterRegistered;
        private bool missingEntityWarningLogged;

        public EntityRef Entity => entityMB != null && entityMB.HasEntity ? entityMB.Entity : entityRef;
        public CharacterIdReference CharacterId => characterId;
        public BC.Audio.AudioDataSO TalkCharacterSound => talkCharacterSound;

        private void OnEnable()
        {
            TryRegisterCharacterMapping();
        }

        private void OnDisable()
        {
            TalkSystemManagerMB.Instance?.UnregisterTalkAdapter(this);
            isCharacterRegistered = false;
        }

        private void LateUpdate()
        {
            if (!isCharacterRegistered)
                TryRegisterCharacterMapping();
        }

        private void Reset()
        {
            ResolveSerializedReferences();
        }

        private void OnValidate()
        {
            ResolveSerializedReferences();
            NormalizePresentationDefaults();
        }

        public async UniTask<bool> TryShowTalkAsync(
            EntityRef viewer,
            TalkRequestData requestData,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveRuntimeDependencies(out EntityRef actor, out _))
                return false;

            return await TryShowTalkAsyncInternal(actor, viewer, requestData, cancellationToken);
        }

        private async UniTask<bool> TryShowTalkAsyncInternal(
            EntityRef ownerActor,
            EntityRef viewer,
            TalkRequestData requestData,
            CancellationToken cancellationToken)
        {
            if (!TryResolveRuntimeDependencies(out EntityRef presentationActor, out TalkSystemManagerMB talkSystemManager))
                return false;

            if (requestData.HasSpeakerCharacter)
            {
                if (!talkSystemManager.TryResolveSpeakerAdapter(requestData, out TalkAdapterMB targetAdapter, out EntityRef speakerEntity))
                    return false;

                if (targetAdapter != null && !ReferenceEquals(targetAdapter, this))
                {
                    if (!targetAdapter.TryResolveRuntimeDependenciesNoWarning(out EntityRef targetActor, out _))
                    {
                        // speaker 解決先がまだ bind 完了していない場合は委譲せず、
                        // 現在の actor/viewer で会話を継続して camera focus 崩れを防ぐ。
                        targetActor = default;
                    }

                    if (targetActor.IsValid)
                    {
                        speakerEntity = targetActor;
                    }

                    EntityRef redirectedViewer = viewer;

                    // speaker を別 adapter へ委譲した結果、viewer まで speaker 自身になると
                    // talk camera の pivot が speaker 単体に寄ってしまう。
                    // この場合は元の actor を observer として引き継ぐ。
                    if (redirectedViewer.IsValid && speakerEntity.IsValid && redirectedViewer.Equals(speakerEntity) &&
                        ownerActor.IsValid && !ownerActor.Equals(speakerEntity))
                    {
                        redirectedViewer = ownerActor;
                    }

                    if (!redirectedViewer.IsValid && ownerActor.IsValid && (!speakerEntity.IsValid || !ownerActor.Equals(speakerEntity)))
                    {
                        redirectedViewer = ownerActor;
                    }

                    if (speakerEntity.IsValid)
                        return await targetAdapter.TryShowTalkAsyncInternal(ownerActor, redirectedViewer, requestData, cancellationToken);
                }
            }

            ApplyTalkState(requestData.talkStateId, logMissingState: true);
            int appliedPresentationVersion = currentTalkStatePresentationVersion;
            ApplyTalkingActivity(true);
            bool completed = false;

            try
            {
                await talkSystemManager.ShowTalk(ownerActor, presentationActor, this, viewer, requestData, cancellationToken);
                completed = true;
                return true;
            }
            finally
            {
                ClearCurrentTalkStatePresentationIfVersion(appliedPresentationVersion);

                // ShowTalk 側で中断された場合は HideTalk が呼ばれないため、
                // Talk 状態をここで確実に戻して状態リークを防ぐ。
                // completed=true の通常終了では complete action / HideTalk 側の presentation reset に任せ、
                // cancellation/例外で抜けたときだけ adapter local の activity を落とす。
                if (!completed)
                {
                    ResetAllConfiguredTalkAnimationParameters();
                    ApplyTalkingActivity(false);
                }
            }
        }

        // 文字送り完了時点で speaking 表現を止めたい場合に manager から呼ばれる。
        public void NotifyTalkTypingCompleted()
        {
            ApplyTalkingActivity(false);
        }

        public async UniTask<bool> TryHideTalkAsync(
            HideTalkRequestData requestData,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveRuntimeDependencies(out EntityRef actor, out TalkSystemManagerMB talkSystemManager))
                return false;

            return await talkSystemManager.HideTalk(actor, requestData).AttachExternalCancellation(cancellationToken);
        }

        internal void HandleTalkHidden(HideTalkRequestData requestData)
        {
            ClearCurrentTalkStatePresentation();

            if (requestData.applyTalkStateOverride)
                ApplyTalkState(requestData.talkStateId, logMissingState: true);
            else
                ApplyDefaultIdlePresentation();

            // Hide 時に idle state 適用で再セットされた talk 用 parameter も必ず落とす。
            ResetAllConfiguredTalkAnimationParameters();

            ApplyTalkingActivity(false);
        }

        private void ResolveSerializedReferences()
        {
            if (entityMB == null)
                entityMB = GetComponentInParent<EntityMB>();

            if (entityAnimation == null)
                entityAnimation = GetComponentInChildren<EntityAnimationMB>(true);

            if (sceneKernelMB == null)
                sceneKernelMB = GetComponentInParent<SceneKernelMB>();

            NormalizePresentationDefaults();
        }

        private void NormalizePresentationDefaults()
        {
            if (statePresentations == null)
                return;

            for (int i = 0; i < statePresentations.Length; i++)
            {
                TalkStatePresentationEntry presentation = statePresentations[i];
                if (presentation.parameterWrites == null)
                    continue;

                for (int j = 0; j < presentation.parameterWrites.Length; j++)
                {
                    TalkAnimatorParameterWrite write = presentation.parameterWrites[j];
                    write.EnsureDefaultAutoReset();
                    presentation.parameterWrites[j] = write;
                }

                statePresentations[i] = presentation;
            }
        }

        private void OnDestroy()
        {
            TalkSystemManagerMB.Instance?.UnregisterTalkAdapter(this);
            isCharacterRegistered = false;
        }

        private void TryRegisterCharacterMapping()
        {
            if (!characterId.IsAssigned)
                return;

            if (!TryResolveRuntimeDependenciesNoWarning(out EntityRef actor, out TalkSystemManagerMB talkSystemManager))
                return;

            talkSystemManager.RegisterTalkAdapter(characterId, actor, this);
            isCharacterRegistered = true;
        }

        private bool TryResolveRuntimeDependenciesNoWarning(out EntityRef actor, out TalkSystemManagerMB talkSystemManager)
        {
            ResolveSerializedReferences();

            talkSystemManager = TalkSystemManagerMB.Instance;
            actor = default;

            if (talkSystemManager == null)
                return false;

            if (!TryResolveActorEntity(out actor, logWarning: false))
                return false;

            entityRef = actor;
            return true;
        }

        private bool TryResolveRuntimeDependencies(out EntityRef actor, out TalkSystemManagerMB talkSystemManager)
        {
            ResolveSerializedReferences();

            talkSystemManager = TalkSystemManagerMB.Instance;
            actor = default;

            if (talkSystemManager == null)
            {
                Debug.LogWarning($"{nameof(TalkAdapterMB)}: {nameof(TalkSystemManagerMB)} is not available.", this);
                return false;
            }

            if (!TryResolveActorEntity(out actor, logWarning: true))
                return false;

            entityRef = actor;

            talkSystemManager.RegisterTalkAdapter(characterId, actor, this);
            isCharacterRegistered = true;

            if (sceneKernelMB?.Kernel?.EntityValueStore != null)
            {
                valueStore = sceneKernelMB.Kernel.EntityValueStore;
            }
            else if (!missingValueStoreWarningLogged)
            {
                Debug.LogWarning($"{nameof(TalkAdapterMB)}: Entity value store is not available, so face expression updates will be skipped.", this);
                missingValueStoreWarningLogged = true;
            }

            return true;
        }

        private bool TryResolveActorEntity(out EntityRef actor, bool logWarning)
        {
            actor = default;

            if (entityMB != null && entityMB.HasEntity)
            {
                actor = entityMB.Entity;
                missingEntityWarningLogged = false;
                return true;
            }

            EntityMB parentEntity = GetComponentInParent<EntityMB>();
            if (parentEntity != null && parentEntity.HasEntity)
            {
                entityMB = parentEntity;
                actor = parentEntity.Entity;
                missingEntityWarningLogged = false;
                return true;
            }

            EntityMB childEntity = GetComponentInChildren<EntityMB>(true);
            if (childEntity != null && childEntity.HasEntity)
            {
                entityMB = childEntity;
                actor = childEntity.Entity;
                missingEntityWarningLogged = false;
                return true;
            }

            if (entityRef.IsValid)
            {
                actor = entityRef;
                missingEntityWarningLogged = false;
                return true;
            }

            if (logWarning && !missingEntityWarningLogged)
            {
                Debug.LogWarning($"{nameof(TalkAdapterMB)}: {nameof(EntityMB)} is missing or not bound.", this);
                missingEntityWarningLogged = true;
            }

            return false;
        }

        private void ApplyDefaultIdlePresentation()
        {
            ApplyTalkState(defaultIdleTalkState, logMissingState: false);
        }

        private void ApplyTalkState(TalkStateId talkStateId, bool logMissingState)
        {
            if (!TryFindPresentation(talkStateId, out TalkStatePresentationEntry presentation))
            {
                if (logMissingState)
                    Debug.LogWarning($"{nameof(TalkAdapterMB)}: talk state '{talkStateId}' is not configured on {name}.", this);

                hasLastStateShapeExpression = false;

                return;
            }

            if (hasCurrentTalkStatePresentation && currentTalkStateId != talkStateId)
                ClearCurrentTalkStatePresentation();

            if (entityAnimation == null && HasAnimatorWrites(presentation) && !missingAnimationWarningLogged)
            {
                Debug.LogWarning($"{nameof(TalkAdapterMB)}: {nameof(EntityAnimationMB)} is missing, so talk animator parameters will be skipped.", this);
                missingAnimationWarningLogged = true;
            }

            hasLastStateShapeExpression = presentation.applyShapeExpression;
            lastStateShapeExpression = presentation.shapeExpressionId;
            presentation.ApplyEnter(entityAnimation, valueStore, Entity);
            currentTalkStateId = talkStateId;
            hasCurrentTalkStatePresentation = true;
            talkStatePresentationVersion++;
            currentTalkStatePresentationVersion = talkStatePresentationVersion;

            // talk 専用 bool は state 側の parameter write で上書きさせない。
            if (!isTalkingActivityActive && entityAnimation != null)
                activityPresentation.Apply(entityAnimation, false);
        }

        private void ClearCurrentTalkStatePresentation()
        {
            if (!hasCurrentTalkStatePresentation)
                return;

            if (TryFindPresentation(currentTalkStateId, out TalkStatePresentationEntry currentPresentation))
                currentPresentation.ApplyExit(entityAnimation);

            hasCurrentTalkStatePresentation = false;
            currentTalkStateId = default;
            currentTalkStatePresentationVersion = 0;
        }

        private void ClearCurrentTalkStatePresentationIfVersion(int presentationVersion)
        {
            if (!hasCurrentTalkStatePresentation)
                return;

            if (currentTalkStatePresentationVersion != presentationVersion)
                return;

            ClearCurrentTalkStatePresentation();
        }

        private void ResetAllConfiguredTalkAnimationParameters()
        {
            if (entityAnimation == null || statePresentations == null)
                return;

            for (int presentationIndex = 0; presentationIndex < statePresentations.Length; presentationIndex++)
            {
                TalkAnimatorParameterWrite[] parameterWrites = statePresentations[presentationIndex].parameterWrites;
                if (parameterWrites == null)
                    continue;

                for (int parameterIndex = 0; parameterIndex < parameterWrites.Length; parameterIndex++)
                {
                    TalkAnimatorParameterWrite parameterWrite = parameterWrites[parameterIndex];
                    if (!parameterWrite.CanApplyExitReset)
                        continue;

                    parameterWrite.ApplyExit(entityAnimation);
                }
            }
        }

        private void ApplyTalkingActivity(bool isTalking)
        {
            isTalkingActivityActive = isTalking;

            if (valueStore != null && Entity.IsValid)
            {
                // 他システムは animator parameter 名を知らなくても、この typed runtime key から会話状態を参照できる。
                valueStore.Set(Entity, ValueKeys.Runtime.IsTalking, isTalking);

                if (isTalking)
                {
                    if (activityPresentation.applyTalkingShapeExpression)
                        valueStore.Set(Entity, ValueKeys.Runtime.ShapeExpression, activityPresentation.talkingShapeExpressionId);
                }
                else if (activityPresentation.applyNonTalkingShapeExpression)
                {
                    valueStore.Set(Entity, ValueKeys.Runtime.ShapeExpression, activityPresentation.nonTalkingShapeExpressionId);
                }
                else if (activityPresentation.applyTalkingShapeExpression && activityPresentation.restoreStateShapeExpressionOnStop && hasLastStateShapeExpression)
                {
                    valueStore.Set(Entity, ValueKeys.Runtime.ShapeExpression, lastStateShapeExpression);
                }
            }

            if (entityAnimation == null)
            {
                if (activityPresentation.talkingBoolParameterNames != null &&
                    activityPresentation.talkingBoolParameterNames.Length > 0 &&
                    !missingAnimationWarningLogged)
                {
                    Debug.LogWarning($"{nameof(TalkAdapterMB)}: {nameof(EntityAnimationMB)} is missing, so talk activity parameters will be skipped.", this);
                    missingAnimationWarningLogged = true;
                }

                return;
            }

            activityPresentation.Apply(entityAnimation, isTalking);
        }

        private bool TryFindPresentation(TalkStateId talkStateId, out TalkStatePresentationEntry presentation)
        {
            if (statePresentations != null)
            {
                for (int i = 0; i < statePresentations.Length; i++)
                {
                    if (statePresentations[i].talkStateId == talkStateId)
                    {
                        presentation = statePresentations[i];
                        return true;
                    }
                }
            }

            presentation = default;
            return false;
        }

        private static bool HasAnimatorWrites(TalkStatePresentationEntry presentation)
        {
            return presentation.parameterWrites != null && presentation.parameterWrites.Length > 0;
        }
    }
}

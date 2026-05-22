using System;
using System.Threading;
using BC.Animation;
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
        [ShowIf(nameof(writeMode), EntityAnimatorParameterWriteMode.SetBool)] public bool boolValue;
        [ShowIf(nameof(writeMode), EntityAnimatorParameterWriteMode.SetFloat)] public float floatValue;
        [ShowIf(nameof(writeMode), EntityAnimatorParameterWriteMode.SetInteger)] public int intValue;

        public void Apply(EntityAnimationMB entityAnimation)
        {
            if (entityAnimation == null || string.IsNullOrWhiteSpace(parameterName))
                return;

            entityAnimation.TryApplyParameter(writeMode, parameterName, boolValue, floatValue, intValue);
        }
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

        public void Apply(EntityAnimationMB entityAnimation, ValueStoreService valueStore, EntityRef entity)
        {
            if (applyFaceExpression && valueStore != null && entity.IsValid)
                valueStore.Set(entity, ValueKeys.Runtime.FaceExpression, faceExpressionId);

            if (applyShapeExpression && valueStore != null && entity.IsValid)
                valueStore.Set(entity, ValueKeys.Runtime.ShapeExpression, shapeExpressionId);

            if (parameterWrites == null || entityAnimation == null)
                return;

            for (int i = 0; i < parameterWrites.Length; i++)
                parameterWrites[i].Apply(entityAnimation);
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

        [Header("Presentation")]
        [SerializeField] private TalkStateId defaultIdleTalkState = TalkStateId.None;
        [SerializeField] private TalkStatePresentationEntry[] statePresentations = Array.Empty<TalkStatePresentationEntry>();
        [SerializeField] private TalkActivityPresentationSettings activityPresentation;

        private EntityRef entityRef;
        private ValueStoreService valueStore;
        private bool missingAnimationWarningLogged;
        private bool missingValueStoreWarningLogged;
        private bool hasLastStateShapeExpression;
        private ShapeExpressionId lastStateShapeExpression;
        private bool isTalkingActivityActive;

        public EntityRef Entity => entityMB != null && entityMB.HasEntity ? entityMB.Entity : entityRef;

        private void Reset()
        {
            ResolveSerializedReferences();
        }

        private void OnValidate()
        {
            ResolveSerializedReferences();
        }

        public async UniTask<bool> TryShowTalkAsync(
            EntityRef viewer,
            TalkRequestData requestData,
            CancellationToken cancellationToken = default)
        {
            if (!TryResolveRuntimeDependencies(out EntityRef actor, out TalkSystemManagerMB talkSystemManager))
                return false;

            ApplyTalkState(requestData.talkStateId, logMissingState: true);
            ApplyTalkingActivity(true);
            bool completed = false;

            try
            {
                await talkSystemManager.ShowTalk(actor, viewer, requestData);
                completed = true;
                return true;
            }
            finally
            {
                // ShowTalk 側で中断された場合は HideTalk が呼ばれないため、
                // Talk 状態をここで確実に戻して状態リークを防ぐ。
                if (!completed)
                    ApplyTalkingActivity(false);
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

            await talkSystemManager.HideTalk(actor, requestData).AttachExternalCancellation(cancellationToken);

            if (requestData.applyTalkStateOverride)
                ApplyTalkState(requestData.talkStateId, logMissingState: true);
            else
                ApplyDefaultIdlePresentation();

            ApplyTalkingActivity(false);
            return true;
        }

        private void ResolveSerializedReferences()
        {
            if (entityMB == null)
                entityMB = GetComponentInParent<EntityMB>();

            if (entityAnimation == null)
                entityAnimation = GetComponentInChildren<EntityAnimationMB>(true);

            if (sceneKernelMB == null)
                sceneKernelMB = GetComponentInParent<SceneKernelMB>();
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

            if (entityMB == null || !entityMB.HasEntity)
            {
                Debug.LogWarning($"{nameof(TalkAdapterMB)}: {nameof(EntityMB)} is missing or not bound.", this);
                return false;
            }

            actor = entityMB.Entity;
            entityRef = actor;

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

            if (entityAnimation == null && HasAnimatorWrites(presentation) && !missingAnimationWarningLogged)
            {
                Debug.LogWarning($"{nameof(TalkAdapterMB)}: {nameof(EntityAnimationMB)} is missing, so talk animator parameters will be skipped.", this);
                missingAnimationWarningLogged = true;
            }

            hasLastStateShapeExpression = presentation.applyShapeExpression;
            lastStateShapeExpression = presentation.shapeExpressionId;
            presentation.Apply(entityAnimation, valueStore, Entity);

            // talk 専用 bool は state 側の parameter write で上書きさせない。
            if (!isTalkingActivityActive && entityAnimation != null)
                activityPresentation.Apply(entityAnimation, false);
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
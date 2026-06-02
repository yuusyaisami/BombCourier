using System;
using BC.Base;
using BC.Gimmick;
using BC.Item;
using BC.Player;
using UnityEngine;

namespace BC.Tutorial
{
    public enum TutorialNumericComparisonOperator
    {
        Equal = 0,
        GreaterOrEqual = 10,
        LessOrEqual = 20,
    }

    public enum TutorialValueStoreScope
    {
        PlayerEntity = 0,
        ActorEntity = 10,
        ExplicitEntity = 20,
        ApplicationKernel = 30,
    }

    [Serializable]
    public sealed class TutorialMoveDistanceConditionAuthoring : TutorialConditionAuthoring
    {
        [SerializeField, Min(0.01f)] private float requiredDistance = 2.0f;
        [SerializeField] private bool planarOnly = true;

        public override ITutorialConditionRuntime CreateRuntime()
        {
            return new MoveDistanceConditionRuntime(requiredDistance, planarOnly);
        }

        public override void Validate(TutorialValidationContext context, string ownerPath)
        {
            if (requiredDistance <= 0.0f)
                context.AddError($"{ownerPath}.requiredDistance must be greater than zero.");
        }
    }

    [Serializable]
    public sealed class TutorialJumpConditionAuthoring : TutorialConditionAuthoring
    {
        [SerializeField, Min(1)] private int requiredJumpCount = 1;

        public override ITutorialConditionRuntime CreateRuntime()
        {
            return new JumpConditionRuntime(requiredJumpCount);
        }

        public override void Validate(TutorialValidationContext context, string ownerPath)
        {
            if (requiredJumpCount <= 0)
                context.AddError($"{ownerPath}.requiredJumpCount must be greater than zero.");
        }
    }

    [Serializable]
    public sealed class TutorialReachLineConditionAuthoring : TutorialConditionAuthoring
    {
        [SerializeField] private Transform lineTransform;
        [SerializeField] private Vector3 localNormal = Vector3.forward;
        [SerializeField] private bool requireCrossingFromBackSide = true;
        [SerializeField, Min(0.0f)] private float distanceTolerance;

        public override ITutorialConditionRuntime CreateRuntime()
        {
            return new ReachLineConditionRuntime(lineTransform, localNormal, requireCrossingFromBackSide, distanceTolerance);
        }

        public override void Validate(TutorialValidationContext context, string ownerPath)
        {
            if (lineTransform == null)
                context.AddError($"{ownerPath}.lineTransform is required.");

            if (localNormal.sqrMagnitude <= 0.0001f)
                context.AddError($"{ownerPath}.localNormal must not be zero.");
        }
    }

    [Serializable]
    public sealed class TutorialHoldItemConditionAuthoring : TutorialConditionAuthoring
    {
        [SerializeField, EntityTagDropdown] private EntityTagReference requiredHeldItemTag;

        public override ITutorialConditionRuntime CreateRuntime()
        {
            return new HoldItemConditionRuntime(requiredHeldItemTag);
        }
    }

    [Serializable]
    public sealed class TutorialThrowItemConditionAuthoring : TutorialConditionAuthoring
    {
        [SerializeField, Min(1)] private int requiredThrowCount = 1;
        [SerializeField] private bool countDropReleaseAsSuccess;

        public override ITutorialConditionRuntime CreateRuntime()
        {
            return new ThrowItemConditionRuntime(requiredThrowCount, countDropReleaseAsSuccess);
        }

        public override void Validate(TutorialValidationContext context, string ownerPath)
        {
            if (requiredThrowCount <= 0)
                context.AddError($"{ownerPath}.requiredThrowCount must be greater than zero.");
        }
    }

    [Serializable]
    public sealed class TutorialBreakableGateBrokenConditionAuthoring : TutorialConditionAuthoring
    {
        [SerializeField] private BreakableGateObjectMB targetGate;

        public override ITutorialConditionRuntime CreateRuntime()
        {
            return new BreakableGateBrokenConditionRuntime(targetGate);
        }

        public override void Validate(TutorialValidationContext context, string ownerPath)
        {
            if (targetGate == null)
                context.AddError($"{ownerPath}.targetGate is required.");
        }
    }

    [Serializable]
    public sealed class TutorialValueStoreBoolConditionAuthoring : TutorialConditionAuthoring
    {
        [SerializeField] private TutorialValueStoreScope storeScope = TutorialValueStoreScope.PlayerEntity;
        [SerializeField] private EntityTargetReference explicitEntity = EntityTargetReference.Self();
        [ValueKeyDropdown(typeof(bool))]
        [SerializeField] private ValueKeyReference keyRef;
        [SerializeField] private bool targetValue = true;

        public override ITutorialConditionRuntime CreateRuntime()
        {
            return new ValueStoreBoolConditionRuntime(storeScope, explicitEntity, keyRef, targetValue);
        }

        public override void Validate(TutorialValidationContext context, string ownerPath)
        {
            TutorialValueStoreConditionValidation.ValidateKeyType(context, ownerPath, keyRef, typeof(bool));
        }
    }

    [Serializable]
    public sealed class TutorialValueStoreIntConditionAuthoring : TutorialConditionAuthoring
    {
        [SerializeField] private TutorialValueStoreScope storeScope = TutorialValueStoreScope.PlayerEntity;
        [SerializeField] private EntityTargetReference explicitEntity = EntityTargetReference.Self();
        [ValueKeyDropdown(typeof(int))]
        [SerializeField] private ValueKeyReference keyRef;
        [SerializeField] private TutorialNumericComparisonOperator comparison = TutorialNumericComparisonOperator.GreaterOrEqual;
        [SerializeField] private int compareValue;

        public override ITutorialConditionRuntime CreateRuntime()
        {
            return new ValueStoreIntConditionRuntime(storeScope, explicitEntity, keyRef, comparison, compareValue);
        }

        public override void Validate(TutorialValidationContext context, string ownerPath)
        {
            TutorialValueStoreConditionValidation.ValidateKeyType(context, ownerPath, keyRef, typeof(int));
        }
    }

    [Serializable]
    public sealed class TutorialValueStoreFloatConditionAuthoring : TutorialConditionAuthoring
    {
        [SerializeField] private TutorialValueStoreScope storeScope = TutorialValueStoreScope.PlayerEntity;
        [SerializeField] private EntityTargetReference explicitEntity = EntityTargetReference.Self();
        [ValueKeyDropdown(typeof(float))]
        [SerializeField] private ValueKeyReference keyRef;
        [SerializeField] private TutorialNumericComparisonOperator comparison = TutorialNumericComparisonOperator.GreaterOrEqual;
        [SerializeField] private float compareValue;
        [SerializeField, Min(0.0f)] private float equalityTolerance = 0.01f;

        public override ITutorialConditionRuntime CreateRuntime()
        {
            return new ValueStoreFloatConditionRuntime(storeScope, explicitEntity, keyRef, comparison, compareValue, equalityTolerance);
        }

        public override void Validate(TutorialValidationContext context, string ownerPath)
        {
            TutorialValueStoreConditionValidation.ValidateKeyType(context, ownerPath, keyRef, typeof(float));
        }
    }

    internal static class TutorialValueStoreConditionValidation
    {
        public static void ValidateKeyType(
            TutorialValidationContext context,
            string ownerPath,
            ValueKeyReference keyRef,
            Type expectedType)
        {
            if (!keyRef.IsAssigned)
            {
                context.AddError($"{ownerPath}.keyRef is required.");
                return;
            }

            Type valueType = keyRef.ValueType;
            if (valueType == expectedType)
                return;

            if (keyRef.TryResolve(out ValueKeyDescriptor descriptor) && descriptor.ValueType == expectedType)
                return;

            string actualTypeName = valueType != null ? valueType.Name : keyRef.ValueTypeName ?? "(Unknown)";
            context.AddError($"{ownerPath}.keyRef must reference a {expectedType.Name} value key. Actual={actualTypeName}.");
        }
    }

    internal abstract class TutorialConditionRuntimeBase : ITutorialConditionRuntime
    {
        public event Action Completed;

        public bool IsCompleted { get; private set; }

        public abstract void Start(in TutorialConditionContext context, object restoredState);
        public abstract void Tick(float deltaTime);
        public abstract object CaptureState();

        public virtual void Dispose()
        {
        }

        protected void MarkCompleted()
        {
            if (IsCompleted)
                return;

            IsCompleted = true;
            Completed?.Invoke();
        }
    }

    internal static class TutorialConditionRuntimeUtility
    {
        public static Transform ResolveRequiredPlayerTransform(in TutorialConditionContext context)
        {
            if (context.SceneKernel?.EntityComponents != null &&
                context.SceneKernel.EntityComponents.TryResolve(context.PlayerEntity, out Transform playerTransform) &&
                playerTransform != null)
            {
                return playerTransform;
            }

            throw new InvalidOperationException("Tutorial condition requires a player Transform, but it could not be resolved.");
        }

        public static T ResolveRequiredPlayerComponent<T>(in TutorialConditionContext context) where T : class
        {
            if (context.SceneKernel?.EntityComponents != null &&
                context.SceneKernel.EntityComponents.TryResolve(context.PlayerEntity, out T component) &&
                component != null)
            {
                return component;
            }

            throw new InvalidOperationException($"Tutorial condition requires player component '{typeof(T).Name}', but it could not be resolved.");
        }

        public static EntityRef ResolveEntityForScope(
            in TutorialConditionContext context,
            TutorialValueStoreScope scope,
            EntityTargetReference explicitEntity)
        {
            switch (scope)
            {
                case TutorialValueStoreScope.PlayerEntity:
                    return context.PlayerEntity;

                case TutorialValueStoreScope.ActorEntity:
                    return context.ActorEntity;

                case TutorialValueStoreScope.ExplicitEntity:
                {
                    ListPoolBuffer buffer = new();
                    int resolvedCount = ScopedEntityResolveUtility.ResolveTargets(
                        new EntityResolveContext(context.SceneKernel, context.ActorEntity, context.PlayerEntity),
                        EntityResolveScope.Entity,
                        explicitEntity,
                        buffer.Results);

                    if (resolvedCount > 0)
                        return buffer.Results[0];

                    throw new InvalidOperationException("Tutorial condition failed to resolve the explicit entity target.");
                }

                default:
                    throw new InvalidOperationException($"Entity scope is not supported: {scope}");
            }
        }

        public static KernelValueStoreService ResolveRequiredKernelStore()
        {
            ApplicationKernelMB appKernel = ApplicationKernelMB.Instance;
            KernelValueStoreService kernelStore = appKernel != null ? appKernel.Kernel?.KernelValueStore : null;
            if (kernelStore != null)
                return kernelStore;

            throw new InvalidOperationException("Tutorial condition requires ApplicationKernel.KernelValueStore, but it is not available.");
        }

        private sealed class ListPoolBuffer
        {
            public System.Collections.Generic.List<EntityRef> Results { get; } = new(2);
        }
    }

    [Serializable]
    internal struct MoveDistanceConditionState
    {
        public float AccumulatedDistance;
        public Vector3 LastPosition;
    }

    internal sealed class MoveDistanceConditionRuntime : TutorialConditionRuntimeBase
    {
        private readonly float requiredDistance;
        private readonly bool planarOnly;

        private Transform playerTransform;
        private float accumulatedDistance;
        private Vector3 lastPosition;

        public MoveDistanceConditionRuntime(float requiredDistance, bool planarOnly)
        {
            this.requiredDistance = Mathf.Max(0.0f, requiredDistance);
            this.planarOnly = planarOnly;
        }

        public override void Start(in TutorialConditionContext context, object restoredState)
        {
            playerTransform = TutorialConditionRuntimeUtility.ResolveRequiredPlayerTransform(context);

            if (restoredState is MoveDistanceConditionState state)
            {
                accumulatedDistance = Mathf.Max(0.0f, state.AccumulatedDistance);
                lastPosition = state.LastPosition;
            }
            else
            {
                accumulatedDistance = 0.0f;
                lastPosition = playerTransform.position;
            }

            if (requiredDistance <= 0.0f || accumulatedDistance >= requiredDistance)
                MarkCompleted();
        }

        public override void Tick(float deltaTime)
        {
            if (IsCompleted || playerTransform == null)
                return;

            Vector3 currentPosition = playerTransform.position;
            Vector3 delta = currentPosition - lastPosition;
            if (planarOnly)
                delta.y = 0.0f;

            accumulatedDistance += delta.magnitude;
            lastPosition = currentPosition;

            if (accumulatedDistance >= requiredDistance)
                MarkCompleted();
        }

        public override object CaptureState()
        {
            return new MoveDistanceConditionState
            {
                AccumulatedDistance = accumulatedDistance,
                LastPosition = lastPosition,
            };
        }
    }

    [Serializable]
    internal struct JumpConditionState
    {
        public int CompletedJumpCount;
    }

    internal sealed class JumpConditionRuntime : TutorialConditionRuntimeBase
    {
        private readonly int requiredJumpCount;

        private EntityMoveMotorMB moveMotor;
        private int completedJumpCount;

        public JumpConditionRuntime(int requiredJumpCount)
        {
            this.requiredJumpCount = Mathf.Max(0, requiredJumpCount);
        }

        public override void Start(in TutorialConditionContext context, object restoredState)
        {
            moveMotor = TutorialConditionRuntimeUtility.ResolveRequiredPlayerComponent<EntityMoveMotorMB>(context);
            completedJumpCount = restoredState is JumpConditionState state ? Mathf.Max(0, state.CompletedJumpCount) : 0;
            moveMotor.Jumped += HandleJumped;

            if (requiredJumpCount <= 0 || completedJumpCount >= requiredJumpCount)
                MarkCompleted();
        }

        public override void Tick(float deltaTime)
        {
        }

        public override object CaptureState()
        {
            return new JumpConditionState
            {
                CompletedJumpCount = completedJumpCount,
            };
        }

        public override void Dispose()
        {
            if (moveMotor != null)
                moveMotor.Jumped -= HandleJumped;
        }

        private void HandleJumped()
        {
            if (IsCompleted)
                return;

            completedJumpCount++;
            if (completedJumpCount >= requiredJumpCount)
                MarkCompleted();
        }
    }

    [Serializable]
    internal struct ReachLineConditionState
    {
        public float PreviousSignedDistance;
    }

    internal sealed class ReachLineConditionRuntime : TutorialConditionRuntimeBase
    {
        private readonly Transform lineTransform;
        private readonly Vector3 localNormal;
        private readonly bool requireCrossingFromBackSide;
        private readonly float distanceTolerance;

        private Transform playerTransform;
        private float previousSignedDistance;

        public ReachLineConditionRuntime(
            Transform lineTransform,
            Vector3 localNormal,
            bool requireCrossingFromBackSide,
            float distanceTolerance)
        {
            this.lineTransform = lineTransform;
            this.localNormal = localNormal;
            this.requireCrossingFromBackSide = requireCrossingFromBackSide;
            this.distanceTolerance = Mathf.Max(0.0f, distanceTolerance);
        }

        public override void Start(in TutorialConditionContext context, object restoredState)
        {
            if (lineTransform == null)
                throw new InvalidOperationException("Tutorial reach-line condition requires a lineTransform.");

            playerTransform = TutorialConditionRuntimeUtility.ResolveRequiredPlayerTransform(context);
            previousSignedDistance = restoredState is ReachLineConditionState state
                ? state.PreviousSignedDistance
                : ComputeSignedDistance();

            EvaluateCurrentPosition();
        }

        public override void Tick(float deltaTime)
        {
            if (IsCompleted || playerTransform == null || lineTransform == null)
                return;

            EvaluateCurrentPosition();
        }

        public override object CaptureState()
        {
            return new ReachLineConditionState
            {
                PreviousSignedDistance = previousSignedDistance,
            };
        }

        private void EvaluateCurrentPosition()
        {
            float currentSignedDistance = ComputeSignedDistance();
            bool onOrBeyondLine = currentSignedDistance >= -distanceTolerance;

            if (onOrBeyondLine)
            {
                if (!requireCrossingFromBackSide || previousSignedDistance < -distanceTolerance || previousSignedDistance >= -distanceTolerance)
                    MarkCompleted();
            }

            previousSignedDistance = currentSignedDistance;
        }

        private float ComputeSignedDistance()
        {
            Vector3 worldNormal = lineTransform.TransformDirection(localNormal.normalized);
            Vector3 offset = playerTransform.position - lineTransform.position;
            return Vector3.Dot(worldNormal, offset);
        }
    }

    [Serializable]
    internal struct HoldItemConditionState
    {
        public bool Completed;
    }

    internal sealed class HoldItemConditionRuntime : TutorialConditionRuntimeBase
    {
        private readonly EntityTagReference requiredHeldItemTag;

        private PlayerItemHandleStateMB handleState;

        public HoldItemConditionRuntime(EntityTagReference requiredHeldItemTag)
        {
            this.requiredHeldItemTag = requiredHeldItemTag;
        }

        public override void Start(in TutorialConditionContext context, object restoredState)
        {
            handleState = TutorialConditionRuntimeUtility.ResolveRequiredPlayerComponent<PlayerItemHandleStateMB>(context);
            handleState.CurrentHandledItemChanged += HandleCurrentHandledItemChanged;

            if (restoredState is HoldItemConditionState state && state.Completed)
            {
                MarkCompleted();
                return;
            }

            if (MatchesCurrentState())
                MarkCompleted();
        }

        public override void Tick(float deltaTime)
        {
            if (!IsCompleted && MatchesCurrentState())
                MarkCompleted();
        }

        public override object CaptureState()
        {
            return new HoldItemConditionState
            {
                Completed = IsCompleted,
            };
        }

        public override void Dispose()
        {
            if (handleState != null)
                handleState.CurrentHandledItemChanged -= HandleCurrentHandledItemChanged;
        }

        private void HandleCurrentHandledItemChanged(ICarryableItem item)
        {
            if (MatchesCurrentState())
                MarkCompleted();
        }

        private bool MatchesCurrentState()
        {
            if (handleState == null || !handleState.IsHandlingItem)
                return false;

            if (!requiredHeldItemTag.IsAssigned)
                return true;

            return handleState.TryGetHeldItemTag(out EntityTagId heldItemTag) &&
                   requiredHeldItemTag.Matches(heldItemTag);
        }
    }

    [Serializable]
    internal struct ThrowItemConditionState
    {
        public int CompletedThrowCount;
        public int LastObservedSequence;
        public bool LastObservedIsHandlingItem;
    }

    internal sealed class ThrowItemConditionRuntime : TutorialConditionRuntimeBase
    {
        private readonly int requiredThrowCount;
        private readonly bool countDropReleaseAsSuccess;

        private ValueStoreService store;
        private EntityRef playerEntity;
        private PlayerItemHandleStateMB handleState;
        private int completedThrowCount;
        private int lastObservedSequence;
        private bool lastObservedIsHandlingItem;

        public ThrowItemConditionRuntime(int requiredThrowCount, bool countDropReleaseAsSuccess)
        {
            this.requiredThrowCount = Mathf.Max(0, requiredThrowCount);
            this.countDropReleaseAsSuccess = countDropReleaseAsSuccess;
        }

        public override void Start(in TutorialConditionContext context, object restoredState)
        {
            store = context.ValueStore ?? throw new InvalidOperationException("Tutorial throw-item condition requires SceneKernel.ValueStore.");
            playerEntity = context.PlayerEntity;
            handleState = countDropReleaseAsSuccess
                ? TutorialConditionRuntimeUtility.ResolveRequiredPlayerComponent<PlayerItemHandleStateMB>(context)
                : null;

            int currentSequence = store.Get(playerEntity, ValueKeys.Runtime.ThrowSequence);
            bool currentIsHandlingItem = handleState != null && handleState.IsHandlingItem;
            if (restoredState is ThrowItemConditionState state)
            {
                completedThrowCount = Mathf.Max(0, state.CompletedThrowCount);
                lastObservedSequence = Mathf.Max(currentSequence, state.LastObservedSequence);
                lastObservedIsHandlingItem = handleState != null
                    ? currentIsHandlingItem
                    : state.LastObservedIsHandlingItem;
            }
            else
            {
                completedThrowCount = 0;
                lastObservedSequence = currentSequence;
                lastObservedIsHandlingItem = currentIsHandlingItem;
            }

            if (requiredThrowCount <= 0 || completedThrowCount >= requiredThrowCount)
                MarkCompleted();
        }

        public override void Tick(float deltaTime)
        {
            if (IsCompleted || store == null || !playerEntity.IsValid)
                return;

            int currentSequence = store.Get(playerEntity, ValueKeys.Runtime.ThrowSequence);
            bool currentIsHandlingItem = handleState != null && handleState.IsHandlingItem;
            int sequenceDelta = Mathf.Max(0, currentSequence - lastObservedSequence);

            if (sequenceDelta > 0)
            {
                completedThrowCount += sequenceDelta;
            }
            else if (countDropReleaseAsSuccess && lastObservedIsHandlingItem && !currentIsHandlingItem)
            {
                completedThrowCount += 1;
            }

            lastObservedSequence = currentSequence;
            lastObservedIsHandlingItem = currentIsHandlingItem;

            if (completedThrowCount >= requiredThrowCount)
                MarkCompleted();
        }

        public override object CaptureState()
        {
            return new ThrowItemConditionState
            {
                CompletedThrowCount = completedThrowCount,
                LastObservedSequence = lastObservedSequence,
                LastObservedIsHandlingItem = lastObservedIsHandlingItem,
            };
        }
    }

    [Serializable]
    internal struct BreakableGateBrokenConditionState
    {
        public bool Completed;
    }

    internal sealed class BreakableGateBrokenConditionRuntime : TutorialConditionRuntimeBase
    {
        private readonly BreakableGateObjectMB targetGate;

        public BreakableGateBrokenConditionRuntime(BreakableGateObjectMB targetGate)
        {
            this.targetGate = targetGate;
        }

        public override void Start(in TutorialConditionContext context, object restoredState)
        {
            if (targetGate == null)
                throw new InvalidOperationException("Tutorial breakable-gate condition requires a target gate.");

            if ((restoredState is BreakableGateBrokenConditionState state && state.Completed) || targetGate.IsBroken)
                MarkCompleted();
        }

        public override void Tick(float deltaTime)
        {
            if (!IsCompleted && targetGate != null && targetGate.IsBroken)
                MarkCompleted();
        }

        public override object CaptureState()
        {
            return new BreakableGateBrokenConditionState
            {
                Completed = IsCompleted,
            };
        }
    }

    [Serializable]
    internal struct ValueStoreConditionState
    {
        public bool Completed;
    }

    internal abstract class ValueStoreConditionRuntimeBase : TutorialConditionRuntimeBase
    {
        private readonly TutorialValueStoreScope storeScope;
        private readonly EntityTargetReference explicitEntity;

        protected ValueStoreConditionRuntimeBase(TutorialValueStoreScope storeScope, EntityTargetReference explicitEntity)
        {
            this.storeScope = storeScope;
            this.explicitEntity = explicitEntity;
        }

        protected ValueStoreService EntityStore { get; private set; }
        protected KernelValueStoreService KernelStore { get; private set; }
        protected EntityRef ObservedEntity { get; private set; }

        public override void Start(in TutorialConditionContext context, object restoredState)
        {
            if (storeScope == TutorialValueStoreScope.ApplicationKernel)
            {
                KernelStore = TutorialConditionRuntimeUtility.ResolveRequiredKernelStore();
                ObservedEntity = default;
            }
            else
            {
                EntityStore = context.ValueStore ?? throw new InvalidOperationException("Tutorial value-store condition requires SceneKernel.ValueStore.");
                ObservedEntity = TutorialConditionRuntimeUtility.ResolveEntityForScope(context, storeScope, explicitEntity);
            }

            if (restoredState is ValueStoreConditionState state && state.Completed)
            {
                MarkCompleted();
                return;
            }

            if (Evaluate())
                MarkCompleted();
        }

        public override void Tick(float deltaTime)
        {
            if (!IsCompleted && Evaluate())
                MarkCompleted();
        }

        public override object CaptureState()
        {
            return new ValueStoreConditionState
            {
                Completed = IsCompleted,
            };
        }

        protected abstract bool Evaluate();
    }

    internal sealed class ValueStoreBoolConditionRuntime : ValueStoreConditionRuntimeBase
    {
        private readonly ValueKeyReference keyRef;
        private readonly bool targetValue;

        public ValueStoreBoolConditionRuntime(
            TutorialValueStoreScope storeScope,
            EntityTargetReference explicitEntity,
            ValueKeyReference keyRef,
            bool targetValue)
            : base(storeScope, explicitEntity)
        {
            this.keyRef = keyRef;
            this.targetValue = targetValue;
        }

        protected override bool Evaluate()
        {
            return KernelStore != null
                ? KernelStore.Get<bool>(keyRef) == targetValue
                : EntityStore.Get<bool>(ObservedEntity, keyRef) == targetValue;
        }
    }

    internal sealed class ValueStoreIntConditionRuntime : ValueStoreConditionRuntimeBase
    {
        private readonly ValueKeyReference keyRef;
        private readonly TutorialNumericComparisonOperator comparison;
        private readonly int compareValue;

        public ValueStoreIntConditionRuntime(
            TutorialValueStoreScope storeScope,
            EntityTargetReference explicitEntity,
            ValueKeyReference keyRef,
            TutorialNumericComparisonOperator comparison,
            int compareValue)
            : base(storeScope, explicitEntity)
        {
            this.keyRef = keyRef;
            this.comparison = comparison;
            this.compareValue = compareValue;
        }

        protected override bool Evaluate()
        {
            int currentValue = KernelStore != null
                ? KernelStore.Get<int>(keyRef)
                : EntityStore.Get<int>(ObservedEntity, keyRef);

            return TutorialNumericComparator.Matches(currentValue, comparison, compareValue);
        }
    }

    internal sealed class ValueStoreFloatConditionRuntime : ValueStoreConditionRuntimeBase
    {
        private readonly ValueKeyReference keyRef;
        private readonly TutorialNumericComparisonOperator comparison;
        private readonly float compareValue;
        private readonly float equalityTolerance;

        public ValueStoreFloatConditionRuntime(
            TutorialValueStoreScope storeScope,
            EntityTargetReference explicitEntity,
            ValueKeyReference keyRef,
            TutorialNumericComparisonOperator comparison,
            float compareValue,
            float equalityTolerance)
            : base(storeScope, explicitEntity)
        {
            this.keyRef = keyRef;
            this.comparison = comparison;
            this.compareValue = compareValue;
            this.equalityTolerance = Mathf.Max(0.0f, equalityTolerance);
        }

        protected override bool Evaluate()
        {
            float currentValue = KernelStore != null
                ? KernelStore.Get<float>(keyRef)
                : EntityStore.Get<float>(ObservedEntity, keyRef);

            return TutorialNumericComparator.Matches(currentValue, comparison, compareValue, equalityTolerance);
        }
    }

    internal static class TutorialNumericComparator
    {
        public static bool Matches(int currentValue, TutorialNumericComparisonOperator comparison, int compareValue)
        {
            switch (comparison)
            {
                case TutorialNumericComparisonOperator.Equal:
                    return currentValue == compareValue;

                case TutorialNumericComparisonOperator.GreaterOrEqual:
                    return currentValue >= compareValue;

                case TutorialNumericComparisonOperator.LessOrEqual:
                    return currentValue <= compareValue;

                default:
                    return false;
            }
        }

        public static bool Matches(float currentValue, TutorialNumericComparisonOperator comparison, float compareValue, float equalityTolerance)
        {
            switch (comparison)
            {
                case TutorialNumericComparisonOperator.Equal:
                    return Mathf.Abs(currentValue - compareValue) <= equalityTolerance;

                case TutorialNumericComparisonOperator.GreaterOrEqual:
                    return currentValue >= compareValue;

                case TutorialNumericComparisonOperator.LessOrEqual:
                    return currentValue <= compareValue;

                default:
                    return false;
            }
        }
    }
}

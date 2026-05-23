using BC.Animation;
using UnityEngine;

namespace BC.Base
{
    public interface IPlayerAnimationController
    {
        void SetSpawnActive(bool active);
        void PlayRaiseBody();
        void SetNextStageActive(bool active);
    }

    [DisallowMultipleComponent]
    public sealed class PlayerAnimationMB : MonoBehaviour, IPlayerAnimationController
    {
        [Header("Animator")]
        [SerializeField] private EntityAnimationMB entityAnimation;

        [Header("Sources")]
        [Tooltip("IEntityMoveAnimationSource を実装した MonoBehaviour を指定する。通常は EntityMoveMotorMB。")]
        [SerializeField] private MonoBehaviour moveSourceBehaviour;

        [Tooltip("IEntityHandleItemAnimationSource を実装した MonoBehaviour を指定する。未実装なら空でよい。")]
        [SerializeField] private MonoBehaviour handleItemSourceBehaviour;

        [Header("Speed")]
        [Tooltip("trueなら CurrentSpeed に 0〜1 の正規化速度を入れる。falseなら m/s の実速度を入れる。")]
        [SerializeField] private bool useNormalizedSpeed = true;

        [SerializeField] private float speedDampTime = 0.12f;

        [Header("Parameter Names")]
        [SerializeField] private string isHandleItemParameter = "IsHandleItem";
        [SerializeField] private string isSprintParameter = "IsSprint";
        [SerializeField] private string isJumpParameter = "IsJump";
        [SerializeField] private string isFallParameter = "IsFall";
        [SerializeField] private string currentSpeedParameter = "CurrentSpeed";
        [SerializeField] private string isIdleThrowParameter = "IsIdleThrow";
        [SerializeField] private string onThrowParameter = "OnThrow";
        [SerializeField] private string isNextStageParameter = "IsNextStage";
        [SerializeField] private string isSpawnParameter = "IsSpawn";
        [SerializeField] private string onRaiseBodyParameter = "OnRaiseBody";

        [Header("Layer")]
        [SerializeField] private string upperBodyLayerName = "UpperBody";

        private IEntityMoveAnimationSource moveSource;
        private IEntityHandleItemAnimationSource handleItemSource;
        private ValueStoreService valueStoreService;
        private EntityRef entityRef;
        private bool initialized;
        private ValueWatchHandle<bool> isItemThrowAimingHandle;
        private ValueWatchHandle<int> throwSequenceHandle;
        private int lastSeenThrowSequenceVersion;

        private void Reset()
        {
            ResolveSerializedReferences();
        }

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            ResolveRuntimeReferences();
        }

        private void LateUpdate()
        {
            if (!initialized)
                return;

            ApplyMoveParameters();
            ApplyFaceStateParameters();
            ApplyHandleItemParameters();
        }

        public void SetSpawnActive(bool active)
        {
            EntityAnimation.SetBool(isSpawnParameter, active);
        }

        public void PlayRaiseBody()
        {
            EntityAnimation.SetTrigger(onRaiseBodyParameter);
        }

        public void SetNextStageActive(bool active)
        {
            EntityAnimation.SetBool(isNextStageParameter, active);
        }

        private EntityAnimationMB EntityAnimation
        {
            get
            {
                if (entityAnimation == null)
                    entityAnimation = GetComponentInChildren<EntityAnimationMB>(true);

                return entityAnimation;
            }
        }

        private void Initialize()
        {
            ResolveSerializedReferences();
            ResolveMoveSource();
            ResolveHandleItemSource();

            if (EntityAnimation == null)
            {
                Debug.LogError($"{nameof(PlayerAnimationMB)}: {nameof(EntityAnimationMB)} is missing.", this);
                enabled = false;
                return;
            }

            if (moveSource == null)
            {
                Debug.LogError($"{nameof(PlayerAnimationMB)}: {nameof(IEntityMoveAnimationSource)} was not found. Assign EntityMoveMotorMB or another movement source.", this);
                enabled = false;
                return;
            }

            initialized = true;
        }

        private void ResolveSerializedReferences()
        {
            if (entityAnimation == null)
                entityAnimation = GetComponentInChildren<EntityAnimationMB>(true);

            if (moveSourceBehaviour == null)
                moveSourceBehaviour = GetComponent<EntityMoveMotorMB>();
        }

        private void ResolveMoveSource()
        {
            if (TryAssignMoveSource(moveSourceBehaviour))
                return;

            MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (TryAssignMoveSource(behaviours[i]))
                    return;
            }

            behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (TryAssignMoveSource(behaviours[i]))
                    return;
            }
        }

        private bool TryAssignMoveSource(MonoBehaviour behaviour)
        {
            if (behaviour == null)
                return false;

            if (behaviour is PlayerMoveController playerMoveController && playerMoveController.MoveMotor != null)
            {
                moveSourceBehaviour = playerMoveController.MoveMotor;
                moveSource = playerMoveController.MoveMotor;
                return true;
            }

            if (behaviour is not IEntityMoveAnimationSource source)
                return false;

            moveSourceBehaviour = behaviour;
            moveSource = source;
            return true;
        }

        private void ResolveHandleItemSource()
        {
            if (handleItemSourceBehaviour != null)
            {
                handleItemSource = handleItemSourceBehaviour as IEntityHandleItemAnimationSource;

                if (handleItemSource == null)
                {
                    Debug.LogError($"{nameof(PlayerAnimationMB)}: Assigned handleItemSourceBehaviour does not implement {nameof(IEntityHandleItemAnimationSource)}.", handleItemSourceBehaviour);
                }

                return;
            }

            MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (TryAssignHandleItemSource(behaviours[i]))
                    return;
            }

            behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (TryAssignHandleItemSource(behaviours[i]))
                    return;
            }
        }

        private bool TryAssignHandleItemSource(MonoBehaviour behaviour)
        {
            if (behaviour is not IEntityHandleItemAnimationSource source)
                return false;

            handleItemSourceBehaviour = behaviour;
            handleItemSource = source;
            return true;
        }

        private void ApplyMoveParameters()
        {
            EntityMoveState state = moveSource.MoveState;
            bool isJump = state == EntityMoveState.Jumping;
            bool isFall = state == EntityMoveState.Falling;
            float speed = useNormalizedSpeed
                ? moveSource.NormalizedPlanarSpeed
                : moveSource.CurrentPlanarSpeed;

            EntityAnimation.SetBool(isSprintParameter, moveSource.IsSprinting);
            EntityAnimation.SetBool(isJumpParameter, isJump);
            EntityAnimation.SetBool(isFallParameter, isFall);
            EntityAnimation.SetFloat(currentSpeedParameter, speed, speedDampTime, Time.deltaTime);
        }

        private void ApplyFaceStateParameters()
        {
            ResolveRuntimeReferences();

            if (moveSource == null || valueStoreService == null || !entityRef.IsValid)
                return;

            bool isHandlingItem = handleItemSource != null && handleItemSource.IsHandlingItem;
            bool isNextStageActive = EntityAnimation.TryGetBool(isNextStageParameter, out bool isNextStage) && isNextStage;
            EntityMoveState state = moveSource.MoveState;
            FaceExpressionId expression;

            if (state == EntityMoveState.Dead)
                expression = FaceExpressionId.Dead;
            else if (isNextStageActive)
                expression = FaceExpressionId.CannotMove;
            else if (state == EntityMoveState.Jumping || state == EntityMoveState.Falling)
                expression = FaceExpressionId.Falling;
            else if (moveSource.IsSprinting)
                expression = FaceExpressionId.Running;
            else
                expression = FaceExpressionId.Neutral;

            valueStoreService.Set(entityRef, ValueKeys.Runtime.FaceExpression, expression);
        }

        private void ApplyHandleItemParameters()
        {
            bool isHandlingItem = handleItemSource != null && handleItemSource.IsHandlingItem;

            EntityAnimation.SetLayerWeight(upperBodyLayerName, isHandlingItem ? 1f : 0f);
            EntityAnimation.SetBool(isHandleItemParameter, isHandlingItem);
            ApplyThrowParameters();
        }

        private void ApplyThrowParameters()
        {
            ResolveRuntimeReferences();
            bool isItemThrowAiming = false;

            if (EnsureThrowRuntimeHandles())
            {
                isItemThrowAiming = isItemThrowAimingHandle.CurrentValue;

                if (throwSequenceHandle.TryGetChanged(ref lastSeenThrowSequenceVersion, out _))
                    EntityAnimation.SetTrigger(onThrowParameter);
            }

            EntityAnimation.SetBool(isIdleThrowParameter, isItemThrowAiming);
        }

        private bool EnsureThrowRuntimeHandles()
        {
            if (valueStoreService == null || !entityRef.IsValid)
                return false;

            if (isItemThrowAimingHandle == null)
                isItemThrowAimingHandle = valueStoreService.GetHandle(entityRef, ValueKeys.Runtime.IsItemThrowAiming);

            if (throwSequenceHandle == null)
            {
                throwSequenceHandle = valueStoreService.GetHandle(entityRef, ValueKeys.Runtime.ThrowSequence);
                lastSeenThrowSequenceVersion = throwSequenceHandle.Version;
            }

            return isItemThrowAimingHandle != null && throwSequenceHandle != null;
        }

        private void ResolveRuntimeReferences()
        {
            if (valueStoreService == null)
            {
                SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

                if (kernelMB != null && kernelMB.Kernel != null)
                    valueStoreService = kernelMB.Kernel.ValueStore;
            }

            if (entityRef.IsValid)
                return;

            EntityMB currentEntityMB = GetComponentInParent<EntityMB>();

            if (currentEntityMB != null && currentEntityMB.HasEntity)
                entityRef = currentEntityMB.Entity;
        }
    }
}
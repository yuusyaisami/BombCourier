using BC.Base;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace BC.Animation
{
    [DisallowMultipleComponent]
    public sealed class EntityAnimationMB : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;

        [Header("Sources")]
        [Tooltip("IEntityMoveAnimationSource を実装したMonoBehaviourを指定する。通常は PlayerMoveController。")]
        [SerializeField] private MonoBehaviour moveSourceBehaviour;

        [Tooltip("IEntityHandleItemAnimationSource を実装したMonoBehaviourを指定する。未実装なら空でよい。")]
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

        [Header("Layer")]
        [SerializeField] private string upperBodyLayerName = "UpperBody";

        private IEntityMoveAnimationSource moveSource;
        private IEntityHandleItemAnimationSource handleItemSource;
        private ValueStoreService valueStoreService;
        private EntityRef entityRef;

        private int isHandleItemHash;
        private int isSprintHash;
        private int isJumpHash;
        private int isFallHash;
        private int currentSpeedHash;
        private int isIdleThrowHash;
        private int onThrowHash;

        private bool initialized;
        private ValueWatchHandle<bool> isItemThrowAimingHandle;
        private ValueWatchHandle<int> throwSequenceHandle;
        private int lastSeenThrowSequenceVersion;

        private FaceExpressionId lastDebugExpression;
        private bool hasLastDebugExpression;

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            moveSourceBehaviour = GetComponentInParent<MonoBehaviour>();
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

        private void Initialize()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator == null)
            {
                Debug.LogError($"{nameof(EntityAnimationMB)}: Animator is missing.", this);
                enabled = false;
                return;
            }

            ResolveMoveSource();
            ResolveHandleItemSource();

            if (moveSource == null)
            {
                Debug.LogError($"{nameof(EntityAnimationMB)}: IEntityMoveAnimationSource was not found. Assign PlayerMoveController or another movement source.", this);
                enabled = false;
                return;
            }

            isHandleItemHash = Animator.StringToHash(isHandleItemParameter);
            isSprintHash = Animator.StringToHash(isSprintParameter);
            isJumpHash = Animator.StringToHash(isJumpParameter);
            isFallHash = Animator.StringToHash(isFallParameter);
            currentSpeedHash = Animator.StringToHash(currentSpeedParameter);
            isIdleThrowHash = Animator.StringToHash(isIdleThrowParameter);
            onThrowHash = Animator.StringToHash(onThrowParameter);

            initialized = true;
        }

        private void ResolveMoveSource()
        {
            if (moveSourceBehaviour != null)
            {
                moveSource = moveSourceBehaviour as IEntityMoveAnimationSource;

                if (moveSource == null)
                {
                    Debug.LogError($"{nameof(EntityAnimationMB)}: Assigned moveSourceBehaviour does not implement {nameof(IEntityMoveAnimationSource)}.", moveSourceBehaviour);
                }

                return;
            }

            MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IEntityMoveAnimationSource source)
                {
                    moveSourceBehaviour = behaviours[i];
                    moveSource = source;
                    return;
                }
            }

            behaviours = GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IEntityMoveAnimationSource source)
                {
                    moveSourceBehaviour = behaviours[i];
                    moveSource = source;
                    return;
                }
            }
        }

        private void ResolveHandleItemSource()
        {
            if (handleItemSourceBehaviour != null)
            {
                handleItemSource = handleItemSourceBehaviour as IEntityHandleItemAnimationSource;

                if (handleItemSource == null)
                {
                    Debug.LogError($"{nameof(EntityAnimationMB)}: Assigned handleItemSourceBehaviour does not implement {nameof(IEntityHandleItemAnimationSource)}.", handleItemSourceBehaviour);
                }

                return;
            }

            MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IEntityHandleItemAnimationSource source)
                {
                    handleItemSourceBehaviour = behaviours[i];
                    handleItemSource = source;
                    return;
                }
            }

            behaviours = GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IEntityHandleItemAnimationSource source)
                {
                    handleItemSourceBehaviour = behaviours[i];
                    handleItemSource = source;
                    return;
                }
            }
        }

        private void ApplyMoveParameters()
        {
            EntityMoveState state = moveSource.MoveState;

            bool isJump =
                state == EntityMoveState.Jumping;

            bool isFall =
                state == EntityMoveState.Falling;

            bool isSprint =
                moveSource.IsSprinting;

            float speed = useNormalizedSpeed
                ? moveSource.NormalizedPlanarSpeed
                : moveSource.CurrentPlanarSpeed;

            animator.SetBool(isSprintHash, isSprint);
            animator.SetBool(isJumpHash, isJump);
            animator.SetBool(isFallHash, isFall);

            animator.SetFloat(
                currentSpeedHash,
                speed,
                speedDampTime,
                Time.deltaTime);
        }

        public void ApplyFaceStateParameters()
        {
            ResolveRuntimeReferences();

            if (moveSource == null || valueStoreService == null || !entityRef.IsValid)
                return;

            bool isHandlingItem = handleItemSource != null && handleItemSource.IsHandlingItem;
            EntityMoveState state = moveSource.MoveState;

            FaceExpressionId expression;

            if (state == EntityMoveState.Dead)
                expression = FaceExpressionId.Dead;
            else if (isHandlingItem)
                expression = FaceExpressionId.CarryingItem;
            else if (state == EntityMoveState.Jumping || state == EntityMoveState.Falling)
                expression = FaceExpressionId.Falling;
            else if (moveSource.IsSprinting)
                expression = FaceExpressionId.Running;
            else
                expression = FaceExpressionId.Neutral;

            SetFaceExpression(expression);
        }
        private void SetFaceExpression(FaceExpressionId expression)
        {
            if (!hasLastDebugExpression || lastDebugExpression != expression)
            {
                hasLastDebugExpression = true;
                lastDebugExpression = expression;
            }

            valueStoreService.Set(entityRef, ValueKeys.Runtime.FaceExpression, expression);
        }

        private void ApplyHandleItemParameters()
        {
            bool isHandlingItem =
                handleItemSource != null &&
                handleItemSource.IsHandlingItem;

            int upperBodyLayerIndex = animator.GetLayerIndex(upperBodyLayerName);
            if (upperBodyLayerIndex >= 0)
            {
                animator.SetLayerWeight(upperBodyLayerIndex, isHandlingItem ? 1f : 0f);
            }

            animator.SetBool(isHandleItemHash, isHandlingItem);
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
                    animator.SetTrigger(onThrowHash);
            }

            animator.SetBool(isIdleThrowHash, isItemThrowAiming);
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
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

        private IEntityMoveAnimationSource moveSource;
        private IEntityHandleItemAnimationSource handleItemSource;
        private ValueStoreService valueStoreService;
        private EntityRef entityRef;

        private int isHandleItemHash;
        private int isSprintHash;
        private int isJumpHash;
        private int isFallHash;
        private int currentSpeedHash;

        private bool initialized;

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
            valueStoreService = GetComponentInParent<SceneKernelMB>().Kernel.ValueStore;
            entityRef = GetComponentInParent<EntityMB>().Entity;

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
            if (moveSource == null)
                return;

            bool isHandlingItem = handleItemSource != null && handleItemSource.IsHandlingItem;

            EntityMoveState state = moveSource.MoveState;
            if (state == EntityMoveState.Jumping || state == EntityMoveState.Falling)
                valueStoreService.Set<FaceExpressionId>(entityRef, ValueKeys.Runtime.FaceExpression, FaceExpressionId.Falling);
            else if (isHandlingItem)
                valueStoreService.Set<FaceExpressionId>(entityRef, ValueKeys.Runtime.FaceExpression, FaceExpressionId.CarryingItem);
            else if (state == EntityMoveState.Dead)
                valueStoreService.Set<FaceExpressionId>(entityRef, ValueKeys.Runtime.FaceExpression, FaceExpressionId.Dead);
        }

        private void ApplyHandleItemParameters()
        {
            bool isHandlingItem =
                handleItemSource != null &&
                handleItemSource.IsHandlingItem;

            animator.SetBool(isHandleItemHash, isHandlingItem);
        }
    }
}
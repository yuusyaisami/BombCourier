using System.Threading;
using BC.Bomb;
using BC.Camera;
using BC.Manager;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BC.Base
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EntityMoveMotorMB))]
    public sealed class PlayerMoveController : MonoBehaviour, IBombImpactReceiver
    {
        private static readonly ValueModifierTagId DeadMoveLockTag = new ValueModifierTagId(10001);

        [Header("References")]
        [SerializeField] private EntityMoveMotorMB moveMotor;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private MonoBehaviour cameraControllerSource;

        [Header("Input")]
        [SerializeField] private InputActionReference moveInputAction;
        [SerializeField] private InputActionReference jumpInputAction;
        [SerializeField] private InputActionReference sprintInputAction;

        [Header("Bomb Impact")]
        [SerializeField] private float maxRagdollImpulse = 12.0f;
        [SerializeField] private float maxBombExternalVelocity = 8.0f;

        [Header("Visual Rotation")]
        [SerializeField] private float modelTurnSharpness = 16.0f;
        [SerializeField] private float minModelTurnSpeed = 0.1f;

        private ICameraController cameraController;
        private IPlayerRagdollController ragdollController;
        private SceneKernel sceneKernel;
        private EntityRef entity;
        private ValueWatchHandle<bool> throwPoseHandle;
        private Vector3 modelInitialPosition;

        public EntityMoveMotorMB MoveMotor => ResolveMoveMotor(addIfMissing: Application.isPlaying);
        public Vector3 CurrentVelocity => moveMotor != null ? moveMotor.CurrentVelocity : Vector3.zero;
        public bool CanMoveByInput => moveMotor != null && moveMotor.CanMoveByInput;
        public bool CanMoveBySystem => moveMotor != null && moveMotor.CanMoveBySystem;
        public Transform ModelRoot => modelRoot != null ? modelRoot : transform;
        public Vector3 ModelInitialPosition
        {
            get => modelInitialPosition;
            set => modelInitialPosition = value;
        }

        private void Awake()
        {
            ResolveMoveMotor(addIfMissing: true);
            ResolveReferences();
        }

        private void Reset()
        {
            ResolveMoveMotor(addIfMissing: false);
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveMoveMotor(addIfMissing: false);
            ResolveReferences();
        }

        private void Start()
        {
            ResolveMoveMotor(addIfMissing: true);
            ResolveReferences();
        }

        private void OnEnable()
        {
            moveInputAction?.action?.Enable();
            jumpInputAction?.action?.Enable();
            sprintInputAction?.action?.Enable();
        }

        private void OnDisable()
        {
            moveInputAction?.action?.Disable();
            jumpInputAction?.action?.Disable();
            sprintInputAction?.action?.Disable();
            moveMotor?.ClearMoveIntent();
        }

        private void Update()
        {
            if (moveMotor == null)
                return;

            float dt = Time.deltaTime;

            if (dt <= 0.0f)
                return;

            ReadInput(dt);
        }

        private void FixedUpdate()
        {
            if (moveMotor == null)
                return;

            float dt = Time.fixedDeltaTime;

            if (dt <= 0.0f)
                return;

            UpdateModelRotation(dt);
        }

        private void ReadInput(float dt)
        {
            if (moveMotor.IsAutoMoveActive)
            {
                moveMotor.ClearMoveIntent();
                return;
            }

            bool canReceiveInput = moveMotor.CanProcessMoveInput;
            Vector2 moveInput = Vector2.zero;

            if (canReceiveInput && moveInputAction != null && moveInputAction.action != null)
            {
                moveInput = moveInputAction.action.ReadValue<Vector2>();
                moveInput = Vector2.ClampMagnitude(moveInput, 1.0f);
            }

            bool sprintHeld = canReceiveInput &&
                              sprintInputAction != null &&
                              sprintInputAction.action != null &&
                              sprintInputAction.action.IsPressed();

            bool jumpPressed = canReceiveInput &&
                               jumpInputAction != null &&
                               jumpInputAction.action != null &&
                               jumpInputAction.action.WasPressedThisFrame();

            moveMotor.SetMoveIntent(BuildCameraRelativeMoveDirection(moveInput), sprintHeld, jumpPressed, dt);
        }

        private Vector3 BuildCameraRelativeMoveDirection(Vector2 input)
        {
            if (input.sqrMagnitude <= 0.0001f)
                return Vector3.zero;

            Quaternion yawRotation = cameraController != null
                ? cameraController.GetYawRotation()
                : Quaternion.Euler(0.0f, transform.eulerAngles.y, 0.0f);

            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 right = yawRotation * Vector3.right;

            forward.y = 0.0f;
            right.y = 0.0f;

            forward.Normalize();
            right.Normalize();

            Vector3 direction = forward * input.y + right * input.x;

            if (direction.sqrMagnitude > 1.0f)
                direction.Normalize();

            return direction;
        }

        private void UpdateModelRotation(float dt)
        {
            if (ModelRoot == null)
                return;

            float t = 1.0f - Mathf.Exp(-modelTurnSharpness * dt);

            if (IsThrowPoseActive())
            {
                Quaternion cameraAlignedRotation = cameraController != null
                    ? cameraController.GetYawRotation()
                    : transform.rotation;

                modelRoot.rotation = Quaternion.Slerp(modelRoot.rotation, cameraAlignedRotation, t);
                return;
            }

            Vector3 horizontalVelocity = moveMotor != null
                ? moveMotor.ControlledPlanarVelocity
                : Vector3.zero;

            horizontalVelocity.y = 0.0f;

            if (horizontalVelocity.magnitude < minModelTurnSpeed)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized, Vector3.up);
            modelRoot.rotation = Quaternion.Slerp(modelRoot.rotation, targetRotation, t);
        }

        private bool IsThrowPoseActive()
        {
            ResolveRuntimeReferences(logMissing: false);

            if (sceneKernel == null || sceneKernel.EntityValueStore == null || !entity.IsValid)
                return false;

            if (throwPoseHandle == null)
                throwPoseHandle = sceneKernel.EntityValueStore.GetHandle(entity, ValueKeys.Runtime.IsThrowPoseActive);

            return throwPoseHandle.CurrentValue;
        }

        public void OnBombImpactReceived(Vector3 direction, float forceMagnitude)
        {
            EntityMoveMotorMB motor = MoveMotor;

            if (motor == null || motor.IsDead || !motor.CanMoveBySystem)
                return;

            Vector3 externalImpulse = direction * Mathf.Min(forceMagnitude, maxBombExternalVelocity);
            Vector3 ragdollImpulse = direction * Mathf.Min(forceMagnitude, maxRagdollImpulse);

            motor.AddImpulse(externalImpulse);
            motor.EnterDeadState(DeadMoveLockTag);

            if (ragdollController != null)
                ragdollController.EnterRagdoll(ragdollImpulse);
        }

        public async UniTask<bool> MoveToAsync(Vector3 targetPosition, float arriveDistance = 0.1f, CancellationToken cancellationToken = default)
        {
            EntityMoveMotorMB motor = MoveMotor;
            return motor != null && await motor.MoveToAsync(targetPosition, arriveDistance, cancellationToken);
        }

        public void CancelAutoMove()
        {
            moveMotor?.CancelAutoMove();
        }

        public void AddImpulse(Vector3 impulseVelocity)
        {
            moveMotor?.AddImpulse(impulseVelocity);
        }

        public void SetExternalVelocity(Vector3 velocity)
        {
            moveMotor?.SetExternalVelocity(velocity);
        }

        public void ClearExternalVelocity()
        {
            moveMotor?.ClearExternalVelocity();
        }

        public void SetPlanarVelocity(Vector3 velocity)
        {
            moveMotor?.SetPlanarVelocity(velocity);
        }

        public void SetVerticalVelocity(float velocity)
        {
            moveMotor?.SetVerticalVelocity(velocity);
        }

        public void EnterMotionLock(EntityMoveState lockedState)
        {
            moveMotor?.EnterMotionLock(lockedState);
        }

        public void ExitMotionLock(Vector3 releaseImpulse, float preservedVelocityRate = 0.35f)
        {
            moveMotor?.ExitMotionLock(releaseImpulse, preservedVelocityRate);
        }

        public void ReviveFromCheckpoint()
        {
            moveMotor?.ReviveFromCheckpoint();
        }

        private EntityMoveMotorMB ResolveMoveMotor(bool addIfMissing)
        {
            if (moveMotor == null)
                moveMotor = GetComponent<EntityMoveMotorMB>();

            if (moveMotor == null && addIfMissing)
                moveMotor = gameObject.AddComponent<EntityMoveMotorMB>();

            return moveMotor;
        }

        private void ResolveReferences()
        {
            if (cameraControllerSource == null)
                cameraControllerSource = GetComponentInChildren<ThirdPersonCameraController>(true);

            cameraController = cameraControllerSource as ICameraController;
            ragdollController = GetComponent<IPlayerRagdollController>();



            if (modelRoot == null)
                modelRoot = transform;
            modelInitialPosition = modelRoot.localPosition;
            ResolveRuntimeReferences(logMissing: false);

            if (Application.isPlaying && cameraController == null)
                Debug.LogWarning($"{nameof(PlayerMoveController)}: Camera controller is not assigned. Movement will use player transform forward.", this);
        }

        private void ResolveRuntimeReferences(bool logMissing)
        {
            if (sceneKernel == null)
            {
                SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

                if (kernelMB != null)
                    sceneKernel = kernelMB.Kernel;
            }

            if (!entity.IsValid)
            {
                EntityMB entityMB = GetComponentInParent<EntityMB>();

                if (entityMB != null && entityMB.HasEntity)
                {
                    entity = entityMB.Entity;
                }
                else if (logMissing)
                {
                    Debug.LogWarning($"{nameof(PlayerMoveController)}: EntityMB is not found or not bound.", this);
                }
            }
        }
    }
}

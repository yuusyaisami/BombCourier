using System;
using BC.Base;
using BC.Bomb;
using BC.Camera;
using BC.Gimmick.Cushion;
using BC.Manager;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BC.Base
{
    public interface IEntityMoveAnimationSource
    {
        EntityMoveState MoveState { get; }

        bool IsGrounded { get; }
        bool IsSprinting { get; }

        /// <summary>
        /// 足の移動アニメーションに使う速度。
        /// 移動床の速度や吹っ飛び速度は基本的に含めない。
        /// 立っているだけで床に運ばれている時に歩きアニメーションを出さないため。
        /// </summary>
        float CurrentPlanarSpeed { get; }

        /// <summary>
        /// BlendTree向けの0〜1速度。
        /// Walk/Run BlendTreeにはこちらを使う方が安定する。
        /// </summary>
        float NormalizedPlanarSpeed { get; }

        /// <summary>
        /// 必要なら向き・傾き・上半身制御に使える。
        /// </summary>
        Vector3 ControlledPlanarVelocity { get; }
    }

    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class PlayerMoveController : EntityMoveController, IEntityMoveAnimationSource, IBombImpactReceiver, ICushionImpactSource
    {
        private static readonly ValueModifierTagId DeadMoveLockTag = new ValueModifierTagId(10001);
        public static readonly ValueModifierTagId GameLogicTag = new ValueModifierTagId(10002);

        [Header("References")]
        [SerializeField] private Rigidbody playerRigidbody;
        [SerializeField] private CapsuleCollider playerCollider;

        [SerializeField] private Transform modelRoot;
        [SerializeField] private MonoBehaviour cameraControllerSource;

        [Header("Input")]
        [SerializeField] private InputActionReference moveInputAction;
        [SerializeField] private InputActionReference jumpInputAction;
        [SerializeField] private InputActionReference sprintInputAction;

        [Header("Speed")]
        [SerializeField] private float fallbackMoveSpeed = 5.0f;
        [SerializeField] private float fallbackSprintMultiplier = 1.5f;

        [Header("Acceleration")]
        [SerializeField] private float groundAcceleration = 35.0f;
        [SerializeField] private float groundDeceleration = 45.0f;
        [SerializeField] private float groundTurnAcceleration = 55.0f;
        [SerializeField] private float airAcceleration = 12.0f;
        [SerializeField] private float airDeceleration = 4.0f;
        [SerializeField] private float maxAirHorizontalSpeed = 8.0f;

        [Header("Impulse")]
        [SerializeField] private float maxRagdollImpulse = 12.0f;
        [SerializeField] private float maxBombExternalVelocity = 8.0f;

        [Header("Jump / Gravity")]
        [SerializeField] private float jumpHeight = 1.4f;
        [SerializeField] private float gravity = -28.0f;
        [SerializeField] private float groundedStickVelocity = -3.0f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Ground Probe")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float groundProbeExtraDistance = 0.18f;
        [SerializeField] private float groundProbeRadiusShrink = 0.03f;
        [SerializeField] private float maxGroundAngle = 55.0f;

        [Header("Step Assist")]
        [SerializeField] private bool enableStepAssist = true;
        [SerializeField, Min(0.05f)] private float maxStepHeight = 0.3f;
        [SerializeField, Min(0.02f)] private float stepAssistForwardDistance = 0.16f;
        [SerializeField, Min(0.01f)] private float minStepAssistSpeed = 0.1f;

        [Header("Moving Platform")]
        [SerializeField] private bool inheritMovingPlatformVelocityOnJump = true;
        [SerializeField] private float platformJumpVelocityInheritance = 1.0f;

        [Header("External Momentum")]
        [SerializeField] private float externalVelocityDamping = 6.0f;
        [SerializeField] private float minExternalVelocity = 0.03f;

        [Header("Cushion")]
        [SerializeField, Min(0.0f)] private float cushionImpactCooldown = 0.12f;

        [Header("Contact Push")]
        [SerializeField] private bool pushRigidbodiesOnContact = true;
        [SerializeField, Min(0.0f)] private float contactPushImpulse = 0.35f;
        [SerializeField, Min(0.0f)] private float contactPushSpeedMultiplier = 0.2f;
        [SerializeField, Min(0.0f)] private float maxContactPushImpulse = 2.0f;
        [SerializeField, Min(0.0f)] private float minContactPushSpeed = 0.1f;

        [Header("Visual Rotation")]
        [SerializeField] private float modelTurnSharpness = 16.0f;
        [SerializeField] private float minModelTurnSpeed = 0.1f;

        private const int MaxGroundHits = 8;
        private const int MaxStepOverlapHits = 8;
        private const float StepAssistSurfaceSkin = 0.02f;
        private const float StepAssistWallMaxUpDot = 0.25f;
        private const float StepAssistMinApproachDot = 0.2f;
        private readonly RaycastHit[] groundHits = new RaycastHit[MaxGroundHits];
        private readonly Collider[] stepOverlapHits = new Collider[MaxStepOverlapHits];

        private ICameraController cameraController;
        private IPlayerRagdollController ragdollController;
        private EntityMB entityMB;
        private CharacterController legacyCharacterController;
        private ValueWatchHandle<bool> throwPoseHandle;

        private Vector3 planarVelocity;
        private float verticalVelocity;
        private Vector3 externalVelocity;
        private Vector3 inheritedPlatformVelocity;

        private Vector2 moveInput;
        private bool sprintHeld;
        private float jumpBufferCounter;
        private float lastGroundedTime = -999.0f;

        private GroundInfo ground;
        private Transform currentPlatform;
        private Vector3 lastPlatformPosition;
        private Quaternion lastPlatformRotation;
        private bool hasPlatformPose;
        private Vector3 platformDelta;
        private Vector3 platformVelocity;
        private PhysicsMaterial lowFrictionContactMaterial;
        private bool wasGroundedLastMotorTick;

        private bool motionLocked;
        private Vector3 preservedVelocityWhenLocked;
        private float nextCushionImpactTime;
        private bool IsReceiveBombImpact;

        private CancellationTokenSource activeAutoMoveCancellationTokenSource;
        private Vector3 autoMoveTargetPosition;
        private float autoMoveArrivalDistanceSqr;
        private bool autoMoveActive;
        private bool autoMoveReachedTarget;

        private const float AutoMoveStopSpeedSqr = 0.01f;

        public Vector3 PlanarVelocity => planarVelocity;
        public float VerticalVelocity => verticalVelocity;
        public Vector3 ExternalVelocity => externalVelocity;
        public Vector3 PlatformVelocity => platformVelocity;

        public Vector3 CurrentVelocity =>
            GetBodyVelocity() + platformVelocity;

        public Transform CushionImpactRoot => transform;
        public EntityTagId CushionImpactTag => ResolveCushionImpactTag();

        private void Awake()
        {
            EnsureMovementBody();
        }

        protected override void Start()
        {
            base.Start();

            EnsureMovementBody();

            if (playerRigidbody == null || playerCollider == null)
            {
                Debug.LogError($"{nameof(PlayerMoveController)}: Rigidbody or CapsuleCollider is missing.", this);
                enabled = false;
                return;
            }

            if (cameraControllerSource == null)
                cameraControllerSource = GetComponentInChildren<ThirdPersonCameraController>(true);

            cameraController = cameraControllerSource as ICameraController;
            ragdollController = GetComponent<IPlayerRagdollController>();
            entityMB = GetComponentInParent<EntityMB>();

            if (cameraController == null)
            {
                Debug.LogWarning($"{nameof(PlayerMoveController)}: Camera controller is not assigned. Movement will use player transform forward.", this);
            }

            if (modelRoot == null)
                modelRoot = transform;
        }

        private void OnEnable()
        {
            EnsureMovementBody();
            moveInputAction?.action?.Enable();
            jumpInputAction?.action?.Enable();
            sprintInputAction?.action?.Enable();
        }

        private void OnDisable()
        {
            moveInputAction?.action?.Disable();
            jumpInputAction?.action?.Disable();
            sprintInputAction?.action?.Disable();
            CancelAutoMove();
        }

        private void Update()
        {
            if (!IsRuntimeReady || playerRigidbody == null || playerCollider == null)
                return;

            float dt = Time.deltaTime;

            if (dt <= 0.0f)
                return;

            ReadInput(dt);
        }

        private void FixedUpdate()
        {
            if (!IsRuntimeReady || playerRigidbody == null || playerCollider == null)
                return;

            float dt = Time.fixedDeltaTime;

            if (dt <= 0.0f)
                return;

            if (motionLocked)
            {
                TickLockedMotion(dt);
                PublishRuntimeValues();
                return;
            }

            TickMotor(dt);
            PublishRuntimeValues();
        }

        private void ReadInput(float dt)
        {
            if (autoMoveActive)
            {
                moveInput = Vector2.zero;
                sprintHeld = false;
                jumpBufferCounter = 0.0f;
                return;
            }

            bool canReceiveInput = CanReceiveMoveInput();

            if (canReceiveInput && moveInputAction != null && moveInputAction.action != null)
            {
                moveInput = moveInputAction.action.ReadValue<Vector2>();
                moveInput = Vector2.ClampMagnitude(moveInput, 1.0f);
            }
            else
            {
                moveInput = Vector2.zero;
            }

            sprintHeld = canReceiveInput &&
                         sprintInputAction != null &&
                         sprintInputAction.action != null &&
                         sprintInputAction.action.IsPressed();

            if (canReceiveInput &&
                jumpInputAction != null &&
                jumpInputAction.action != null &&
                jumpInputAction.action.WasPressedThisFrame())
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= dt;
            }
        }

        private void TickMotor(float dt)
        {
            ProbeGround();

            bool isGrounded = ground.IsValid;

            if (!isGrounded && wasGroundedLastMotorTick)
                CapturePlatformInertiaOnLeaveGround();

            if (isGrounded)
                lastGroundedTime = Time.time;

            UpdatePlatformMotion(dt, isGrounded);

            if (isGrounded && !wasGroundedLastMotorTick)
                RebaseInheritedPlatformMomentumOnLanding();

            ApplyPlatformCarry();
            UpdatePlanarVelocity(dt, isGrounded);
            UpdateVerticalVelocity(dt, isGrounded);
            UpdateExternalVelocity(dt);

            Vector3 bodyVelocity = GetBodyVelocity();
            bool steppedUp = TryStepUp(dt, bodyVelocity, isGrounded);

            if (steppedUp && !isGrounded)
                RebaseInheritedPlatformMomentumOnLanding();

            bodyVelocity = GetBodyVelocity();

            ApplyCurrentVelocityToBody(bodyVelocity);

            StorePlatformPose();
            UpdateMoveState();
            UpdateModelRotation(dt);
            wasGroundedLastMotorTick = ground.IsValid;
        }

        private void UpdatePlanarVelocity(float dt, bool isGrounded)
        {
            Vector3 desiredDirection = GetDesiredMoveDirection();
            bool hasMoveInput = desiredDirection.sqrMagnitude > 0.0001f;

            float moveSpeed = GetMoveBaseSpeed(fallbackMoveSpeed);

            if (sprintHeld)
                moveSpeed *= GetSprintMultiplier(fallbackSprintMultiplier);

            Vector3 desiredVelocity = hasMoveInput
                ? desiredDirection * moveSpeed
                : Vector3.zero;

            float acceleration;

            if (isGrounded)
            {
                if (!hasMoveInput)
                {
                    acceleration = groundDeceleration;
                }
                else
                {
                    float dot = planarVelocity.sqrMagnitude > 0.001f
                        ? Vector3.Dot(planarVelocity.normalized, desiredDirection)
                        : 1.0f;

                    acceleration = dot < 0.0f
                        ? groundTurnAcceleration
                        : groundAcceleration;
                }
            }
            else
            {
                acceleration = hasMoveInput ? airAcceleration : airDeceleration;
            }

            planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, acceleration * dt);

            if (autoMoveActive && autoMoveReachedTarget && planarVelocity.sqrMagnitude <= AutoMoveStopSpeedSqr)
                CompleteAutoMoveTarget();

            if (!isGrounded)
            {
                Vector3 horizontal = new Vector3(planarVelocity.x, 0.0f, planarVelocity.z);

                if (horizontal.magnitude > maxAirHorizontalSpeed)
                {
                    horizontal = horizontal.normalized * maxAirHorizontalSpeed;
                    planarVelocity = new Vector3(horizontal.x, planarVelocity.y, horizontal.z);
                }
            }
        }

        private void UpdateVerticalVelocity(float dt, bool isGrounded)
        {
            bool canUseCoyote = Time.time - lastGroundedTime <= coyoteTime;
            bool wantsJump = jumpBufferCounter > 0.0f;

            if (wantsJump && canUseCoyote)
            {
                float jumpHeightMultiplier = Mathf.Max(0.0f, GetJumpHeightMultiplier(1.0f));
                float effectiveJumpHeight = Mathf.Max(0.0f, jumpHeight * jumpHeightMultiplier);
                float jumpSpeed = Mathf.Sqrt(effectiveJumpHeight * -2.0f * gravity);
                verticalVelocity = jumpSpeed;

                InheritCurrentPlatformVelocity();

                jumpBufferCounter = 0.0f;
                lastGroundedTime = -999.0f;
                StateMachine.ChangeState(EntityMoveState.Jumping);
                return;
            }

            if (isGrounded && verticalVelocity < 0.0f)
            {
                verticalVelocity = groundedStickVelocity;
                return;
            }

            verticalVelocity += gravity * dt;
        }

        private void UpdateExternalVelocity(float dt)
        {
            if (externalVelocity.sqrMagnitude <= minExternalVelocity * minExternalVelocity)
            {
                externalVelocity = Vector3.zero;
                return;
            }

            externalVelocity = Vector3.Lerp(
                externalVelocity,
                Vector3.zero,
                1.0f - Mathf.Exp(-externalVelocityDamping * dt));
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

        private void ProbeGround()
        {
            ground = default;

            if (playerCollider == null)
                return;

            GetGroundProbeParameters(out Vector3 center, out float radius, out float distance);

            int hitCount = Physics.SphereCastNonAlloc(
                center,
                radius,
                Vector3.down,
                groundHits,
                distance,
                groundMask,
                QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = groundHits[i];

                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                    continue;

                float angle = Vector3.Angle(hit.normal, Vector3.up);

                if (angle > maxGroundAngle || hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                ground = new GroundInfo(true, hit.normal, hit.point, hit.collider.transform);
            }
        }

        private void GetGroundProbeParameters(out Vector3 center, out float radius, out float distance)
        {
            Vector3 lossyScale = transform.lossyScale;
            float scaleX = Mathf.Abs(lossyScale.x);
            float scaleY = Mathf.Abs(lossyScale.y);
            float scaleZ = Mathf.Abs(lossyScale.z);

            float colliderRadius = playerCollider.radius * Mathf.Max(scaleX, scaleZ);
            float colliderHalfHeight = playerCollider.height * 0.5f * scaleY;

            center = transform.TransformPoint(playerCollider.center);
            radius = Mathf.Max(0.01f, colliderRadius - groundProbeRadiusShrink);
            distance = Mathf.Max(0.01f, colliderHalfHeight - colliderRadius + groundProbeExtraDistance);
        }

        private void UpdatePlatformMotion(float dt, bool isGrounded)
        {
            platformDelta = Vector3.zero;
            platformVelocity = Vector3.zero;

            if (!isGrounded || !ground.IsValid || ground.Transform == null)
            {
                currentPlatform = null;
                hasPlatformPose = false;
                return;
            }

            Transform platform = ground.Transform;
            IMovingPlatformMotionSource motionSource = platform.GetComponentInParent<IMovingPlatformMotionSource>();

            if (motionSource != null &&
                motionSource.TryGetPassengerMotion(transform.position, dt, out MovingPlatformPassengerMotion motion))
            {
                platformDelta = motion.Delta;
                platformVelocity = motion.Velocity;
                currentPlatform = platform;
                return;
            }

            if (currentPlatform == platform && hasPlatformPose)
            {
                Vector3 positionDelta = platform.position - lastPlatformPosition;
                Quaternion rotationDelta = platform.rotation * Quaternion.Inverse(lastPlatformRotation);

                Vector3 playerOffsetFromPlatform = transform.position - platform.position;
                Vector3 rotatedOffset = rotationDelta * playerOffsetFromPlatform;
                Vector3 rotationMovementDelta = rotatedOffset - playerOffsetFromPlatform;

                platformDelta = positionDelta + rotationMovementDelta;

                if (dt > 0.0f)
                    platformVelocity = platformDelta / dt;
            }

            currentPlatform = platform;
        }

        private void ApplyPlatformCarry()
        {
            if (playerRigidbody == null || platformDelta.sqrMagnitude <= 0.0f)
                return;

            playerRigidbody.position += platformDelta;
        }

        private void CapturePlatformInertiaOnLeaveGround()
        {
            if (!inheritMovingPlatformVelocityOnJump ||
                inheritedPlatformVelocity.sqrMagnitude > 0.0001f ||
                platformVelocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            inheritedPlatformVelocity = platformVelocity * platformJumpVelocityInheritance;
        }

        private void InheritCurrentPlatformVelocity()
        {
            if (!inheritMovingPlatformVelocityOnJump || inheritedPlatformVelocity.sqrMagnitude > 0.0001f)
                return;

            inheritedPlatformVelocity = platformVelocity * platformJumpVelocityInheritance;
        }

        private void RebaseInheritedPlatformMomentumOnLanding()
        {
            if (inheritedPlatformVelocity.sqrMagnitude <= 0.0001f)
                return;

            Vector3 inheritedPlanarVelocity = Vector3.ProjectOnPlane(inheritedPlatformVelocity, Vector3.up);
            Vector3 landingSupportVelocity = Vector3.ProjectOnPlane(platformVelocity, Vector3.up);
            planarVelocity += inheritedPlanarVelocity - landingSupportVelocity;
            inheritedPlatformVelocity = Vector3.zero;
        }

        private void StorePlatformPose()
        {
            if (currentPlatform == null)
            {
                hasPlatformPose = false;
                return;
            }

            lastPlatformPosition = currentPlatform.position;
            lastPlatformRotation = currentPlatform.rotation;
            hasPlatformPose = true;
        }

        public void OnBombImpactReceived(Vector3 direction, float forceMagnitude)
        {
            if (IsReceiveBombImpact)
                return;

            Vector3 externalImpulse = direction * Mathf.Min(forceMagnitude, maxBombExternalVelocity);
            Vector3 ragdollImpulse = direction * Mathf.Min(forceMagnitude, maxRagdollImpulse);

            AddImpulse(externalImpulse);

            IsReceiveBombImpact = true;
            StateMachine.ChangeState(EntityMoveState.Dead);

            if (IsRuntimeReady && SceneKernel.ValueStore != null)
            {
                if (ragdollController != null)
                    ragdollController.EnterRagdoll(ragdollImpulse);

                SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsDead, true);
                SceneKernel.ValueStore.SetBoolModifier(Entity, ValueKeys.Move.CanMove, DeadMoveLockTag, false);
            }
        }

        private void UpdateMoveState()
        {
            if (motionLocked)
            {
                if (MoveState != EntityMoveState.Disabled && MoveState != EntityMoveState.Dead)
                    StateMachine.ChangeState(EntityMoveState.Disabled);
                return;
            }

            if (ground.IsValid)
            {
                Vector3 horizontal = new Vector3(planarVelocity.x, 0.0f, planarVelocity.z);

                if (horizontal.magnitude > 0.1f)
                    StateMachine.ChangeState(EntityMoveState.Moving);
                else
                    StateMachine.ChangeState(EntityMoveState.Idle);

                return;
            }

            if (verticalVelocity > 0.0f)
                StateMachine.ChangeState(EntityMoveState.Jumping);
            else
                StateMachine.ChangeState(EntityMoveState.Falling);
        }

        private void UpdateModelRotation(float dt)
        {
            if (modelRoot == null)
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

            Vector3 horizontalVelocity = new Vector3(planarVelocity.x, 0.0f, planarVelocity.z);

            if (horizontalVelocity.magnitude < minModelTurnSpeed)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized, Vector3.up);
            modelRoot.rotation = Quaternion.Slerp(modelRoot.rotation, targetRotation, t);
        }

        private bool IsThrowPoseActive()
        {
            if (!IsRuntimeReady || SceneKernel == null || SceneKernel.EntityValueStore == null)
                return false;

            if (throwPoseHandle == null)
                throwPoseHandle = SceneKernel.EntityValueStore.GetHandle(Entity, ValueKeys.Runtime.IsThrowPoseActive);

            return throwPoseHandle.CurrentValue;
        }

        private void TickLockedMotion(float dt)
        {
            ProbeGround();
            bool isGrounded = ground.IsValid;

            if (!isGrounded && wasGroundedLastMotorTick)
                CapturePlatformInertiaOnLeaveGround();

            UpdatePlatformMotion(dt, isGrounded);

            if (isGrounded && !wasGroundedLastMotorTick)
                RebaseInheritedPlatformMomentumOnLanding();

            ApplyPlatformCarry();
            UpdateExternalVelocity(dt);

            ApplyCurrentVelocityToBody(externalVelocity + inheritedPlatformVelocity);
            StorePlatformPose();
            wasGroundedLastMotorTick = ground.IsValid;
        }

        private void ApplyCurrentVelocityToBody(Vector3 velocity)
        {
            if (playerRigidbody == null)
                return;

            playerRigidbody.linearVelocity = velocity;
        }

        private Vector3 GetBodyVelocity()
        {
            return planarVelocity + Vector3.up * verticalVelocity + externalVelocity + inheritedPlatformVelocity;
        }

        public void AddImpulse(Vector3 impulseVelocity)
        {
            externalVelocity += impulseVelocity;
        }

        public void SetExternalVelocity(Vector3 velocity)
        {
            externalVelocity = velocity;
        }

        public void ClearExternalVelocity()
        {
            externalVelocity = Vector3.zero;
        }

        public bool HandleCushionImpact(CushionImpactData impactData, CushionImpactResult impactResult)
        {
            if (!impactResult.IsHandled || IsReceiveBombImpact)
                return false;

            switch (impactResult.ResponseKind)
            {
                case CushionResponseKind.Bounce:
                    ApplyCushionBounce(impactResult.BounceVelocity);
                    return true;

                case CushionResponseKind.Stop:
                case CushionResponseKind.StopAndAttach:
                    ApplyCushionStop();
                    return true;

                default:
                    return false;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            ProcessCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            ProcessCollision(collision);
        }

        private void ProcessCollision(Collision collision)
        {
            if (collision == null || collision.collider == null)
                return;

            ResolveCollisionVelocityConstraints(collision);
            TryApplyContactPush(collision);

            if (Time.time < nextCushionImpactTime)
                return;

            CushionSurfaceMB surface = collision.collider.GetComponentInParent<CushionSurfaceMB>();

            if (surface == null)
                return;

            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;

            Vector3 incomingVelocity = playerRigidbody != null
                ? playerRigidbody.linearVelocity
                : CurrentVelocity;

            CushionImpactData impactData = new CushionImpactData(
                gameObject,
                transform,
                entityMB,
                CushionImpactTag,
                playerRigidbody,
                playerCollider,
                collision.contactCount > 0 ? contact.point : transform.position,
                collision.contactCount > 0 ? contact.normal : Vector3.up,
                incomingVelocity,
                collision.relativeVelocity.magnitude);

            if (!surface.TryEvaluate(impactData, out CushionImpactResult result))
                return;

            if (HandleCushionImpact(impactData, result))
                nextCushionImpactTime = Time.time + cushionImpactCooldown;
        }

        private void ResolveCollisionVelocityConstraints(Collision collision)
        {
            if (collision == null)
                return;

            float minGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);

            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                float upDot = Vector3.Dot(contact.normal, Vector3.up);
                float downDot = Vector3.Dot(contact.normal, Vector3.down);

                if (upDot >= minGroundDot)
                {
                    lastGroundedTime = Time.time;

                    if (verticalVelocity < groundedStickVelocity)
                        verticalVelocity = groundedStickVelocity;
                }

                if (Mathf.Abs(upDot) < minGroundDot)
                    RemoveIntoWallVelocity(contact.normal);

                if (downDot > 0.35f && verticalVelocity > 0.0f)
                    verticalVelocity = 0.0f;
            }
        }

        private void RemoveIntoWallVelocity(Vector3 surfaceNormal)
        {
            Vector3 wallNormal = Vector3.ProjectOnPlane(surfaceNormal, Vector3.up);

            if (wallNormal.sqrMagnitude <= 0.0001f)
                return;

            wallNormal.Normalize();
            planarVelocity = RemoveIntoSurfaceComponent(planarVelocity, wallNormal);
            externalVelocity = RemoveIntoSurfaceComponent(externalVelocity, wallNormal);
            inheritedPlatformVelocity = RemoveIntoSurfaceComponent(inheritedPlatformVelocity, wallNormal);
        }

        private static Vector3 RemoveIntoSurfaceComponent(Vector3 velocity, Vector3 surfaceNormal)
        {
            float intoSurfaceSpeed = Vector3.Dot(velocity, -surfaceNormal);

            if (intoSurfaceSpeed <= 0.0f)
                return velocity;

            return velocity + surfaceNormal * intoSurfaceSpeed;
        }

        private bool TryStepUp(float dt, Vector3 bodyVelocity, bool isGrounded)
        {
            if (!enableStepAssist ||
                playerRigidbody == null ||
                playerCollider == null ||
                motionLocked ||
                IsReceiveBombImpact ||
                verticalVelocity > 0.1f)
            {
                return false;
            }

            bool canStep = isGrounded || Time.time - lastGroundedTime <= coyoteTime;

            if (!canStep)
                return false;

            Vector3 moveDirection = GetDesiredMoveDirection();
            Vector3 horizontalVelocity = bodyVelocity;
            horizontalVelocity.y = 0.0f;

            if (moveDirection.sqrMagnitude <= 0.0001f)
            {
                if (horizontalVelocity.sqrMagnitude <= minStepAssistSpeed * minStepAssistSpeed)
                    return false;

                moveDirection = horizontalVelocity.normalized;
            }

            float horizontalSpeed = horizontalVelocity.magnitude;
            float forwardDistance = Mathf.Clamp(horizontalSpeed * Time.fixedDeltaTime + StepAssistSurfaceSkin, StepAssistSurfaceSkin, stepAssistForwardDistance);

            if (forwardDistance <= 0.0f)
                return false;

            GetCapsuleGeometry(playerRigidbody.position, out Vector3 capsuleBottom, out Vector3 capsuleTop, out float capsuleRadius);
            Vector3 capsuleCenter = playerRigidbody.position + transform.rotation * playerCollider.center;
            float castRadius = Mathf.Max(0.01f, capsuleRadius - StepAssistSurfaceSkin);
            float feetY = capsuleBottom.y - capsuleRadius;
            Vector3 lowerOrigin = new Vector3(capsuleCenter.x, feetY + castRadius + 0.05f, capsuleCenter.z);
            Vector3 upperOrigin = lowerOrigin + Vector3.up * maxStepHeight;

            if (!Physics.SphereCast(
                    lowerOrigin,
                    castRadius,
                    moveDirection,
                    out RaycastHit lowerHit,
                    forwardDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (lowerHit.collider == null || lowerHit.transform.IsChildOf(transform))
                return false;

            float hitUpDot = Mathf.Abs(Vector3.Dot(lowerHit.normal, Vector3.up));

            if (hitUpDot > StepAssistWallMaxUpDot)
                return false;

            Vector3 wallNormal = Vector3.ProjectOnPlane(lowerHit.normal, Vector3.up);

            if (wallNormal.sqrMagnitude <= 0.0001f)
                return false;

            wallNormal.Normalize();

            if (Vector3.Dot(moveDirection, -wallNormal) < StepAssistMinApproachDot)
                return false;

            if (Physics.SphereCast(
                    upperOrigin,
                    castRadius,
                    moveDirection,
                    out RaycastHit upperHit,
                    forwardDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore) &&
                upperHit.collider != null &&
                !upperHit.transform.IsChildOf(transform))
            {
                return false;
            }

            Vector3 candidatePosition = playerRigidbody.position + Vector3.up * (maxStepHeight + StepAssistSurfaceSkin) + moveDirection * Mathf.Max(lowerHit.distance + StepAssistSurfaceSkin, forwardDistance * 0.5f);

            if (!TryFindStepGround(candidatePosition, out Vector3 snappedPosition, out GroundInfo stepGround))
                return false;

            GetCapsuleGeometry(snappedPosition, out Vector3 candidateCapsuleBottom, out _, out float candidateCapsuleRadius);
            float candidateFeetY = candidateCapsuleBottom.y - candidateCapsuleRadius;

            if (!CanOccupyCapsule(snappedPosition, candidateFeetY))
                return false;

            if (snappedPosition.y <= playerRigidbody.position.y + 0.0001f)
                return false;

            if (snappedPosition.y - playerRigidbody.position.y > maxStepHeight + StepAssistSurfaceSkin)
                return false;

            playerRigidbody.position = snappedPosition;
            ground = stepGround;
            lastGroundedTime = Time.time;

            if (verticalVelocity < groundedStickVelocity)
                verticalVelocity = groundedStickVelocity;

            return true;
        }

        public override async UniTask<bool> MoveToAsync(Vector3 targetPosition, float arriveDistance = 0.1f, CancellationToken cancellationToken = default)
        {
            if (IsAlreadyAtAutoMoveTarget(targetPosition, arriveDistance))
                return true;

            if (!CanStartAutoMove())
                return false;

            CancellationTokenSource autoMoveCancellationTokenSource = BeginNewAutoMove();
            CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                autoMoveCancellationTokenSource.Token);
            bool reachedTarget = false;

            try
            {
                autoMoveTargetPosition = targetPosition;
                autoMoveArrivalDistanceSqr = Mathf.Max(0.0001f, arriveDistance * arriveDistance);
                autoMoveReachedTarget = false;
                autoMoveActive = true;

                await UniTask.WaitUntil(() => !autoMoveActive, PlayerLoopTiming.Update, linkedCancellationTokenSource.Token);
                reachedTarget = autoMoveReachedTarget;
            }
            catch (OperationCanceledException) when (linkedCancellationTokenSource.IsCancellationRequested)
            {
                return false;
            }
            finally
            {
                if (ReferenceEquals(activeAutoMoveCancellationTokenSource, autoMoveCancellationTokenSource))
                {
                    activeAutoMoveCancellationTokenSource = null;
                    autoMoveActive = false;
                    autoMoveReachedTarget = false;
                }

                CompleteAutoMove(autoMoveCancellationTokenSource);
                linkedCancellationTokenSource.Dispose();
            }

            return reachedTarget;
        }

        public void CancelAutoMove()
        {
            if (activeAutoMoveCancellationTokenSource != null)
                activeAutoMoveCancellationTokenSource.Cancel();

            autoMoveActive = false;
            autoMoveReachedTarget = false;
        }

        private bool CanStartAutoMove()
        {
            return IsRuntimeReady &&
                   playerRigidbody != null &&
                   playerCollider != null &&
                   !motionLocked &&
                   !IsReceiveBombImpact &&
                   CanReceiveMoveInput();
        }

        private bool IsAlreadyAtAutoMoveTarget(Vector3 targetPosition, float arriveDistance)
        {
            if (playerRigidbody == null)
                return false;

            Vector3 toTarget = targetPosition - playerRigidbody.position;
            toTarget.y = 0.0f;

            return toTarget.sqrMagnitude <= Mathf.Max(0.0001f, arriveDistance * arriveDistance);
        }

        private Vector3 GetDesiredMoveDirection()
        {
            if (autoMoveActive)
                return BuildAutoMoveDirection();

            return BuildCameraRelativeMoveDirection(moveInput);
        }

        private Vector3 BuildAutoMoveDirection()
        {
            if (playerRigidbody == null)
                return Vector3.zero;

            Vector3 toTarget = autoMoveTargetPosition - playerRigidbody.position;
            toTarget.y = 0.0f;

            autoMoveReachedTarget = toTarget.sqrMagnitude <= autoMoveArrivalDistanceSqr;

            if (autoMoveReachedTarget)
            {
                return Vector3.zero;
            }

            return toTarget.normalized;
        }

        private CancellationTokenSource BeginNewAutoMove()
        {
            if (activeAutoMoveCancellationTokenSource != null)
                activeAutoMoveCancellationTokenSource.Cancel();

            activeAutoMoveCancellationTokenSource = new CancellationTokenSource();
            return activeAutoMoveCancellationTokenSource;
        }

        private void CompleteAutoMove(CancellationTokenSource autoMoveCancellationTokenSource)
        {
            if (ReferenceEquals(activeAutoMoveCancellationTokenSource, autoMoveCancellationTokenSource))
                activeAutoMoveCancellationTokenSource = null;

            autoMoveCancellationTokenSource.Dispose();
        }

        private void CompleteAutoMoveTarget()
        {
            autoMoveActive = false;
        }

        private bool CanOccupyCapsule(Vector3 bodyPosition, float candidateFeetY)
        {
            GetCapsuleGeometry(bodyPosition, out Vector3 capsuleBottom, out Vector3 capsuleTop, out float capsuleRadius);
            Vector3 capsuleCenter = bodyPosition + transform.rotation * playerCollider.center;
            int hitCount = Physics.OverlapCapsuleNonAlloc(
                capsuleBottom,
                capsuleTop,
                Mathf.Max(0.01f, capsuleRadius - StepAssistSurfaceSkin),
                stepOverlapHits,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = stepOverlapHits[i];

                if (hit == null || hit.transform.IsChildOf(transform))
                    continue;

                Vector3 closestPoint = hit.ClosestPoint(capsuleCenter);

                if (closestPoint.y <= candidateFeetY + StepAssistSurfaceSkin)
                    continue;

                return false;
            }

            return true;
        }

        private bool TryFindStepGround(Vector3 candidatePosition, out Vector3 snappedPosition, out GroundInfo stepGround)
        {
            snappedPosition = default;
            stepGround = default;

            GetCapsuleGeometry(candidatePosition, out Vector3 capsuleBottom, out _, out float capsuleRadius);

            float probeStartOffset = maxStepHeight + groundProbeExtraDistance + StepAssistSurfaceSkin;
            Vector3 probeOrigin = capsuleBottom + Vector3.up * probeStartOffset;
            float probeDistance = probeStartOffset + maxStepHeight + groundProbeExtraDistance;

            if (!Physics.SphereCast(
                    probeOrigin,
                    Mathf.Max(0.01f, capsuleRadius - StepAssistSurfaceSkin),
                    Vector3.down,
                    out RaycastHit hit,
                    probeDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                return false;

            float angle = Vector3.Angle(hit.normal, Vector3.up);

            if (angle > maxGroundAngle)
                return false;

            Vector3 targetCapsuleBottom = probeOrigin + Vector3.down * hit.distance;
            Vector3 bodyOffset = candidatePosition - capsuleBottom;
            snappedPosition = targetCapsuleBottom + bodyOffset;

            if (snappedPosition.y - playerRigidbody.position.y > maxStepHeight + StepAssistSurfaceSkin)
                return false;

            stepGround = new GroundInfo(true, hit.normal, hit.point, hit.collider.transform);
            return true;
        }

        private void GetCapsuleGeometry(Vector3 bodyPosition, out Vector3 capsuleBottom, out Vector3 capsuleTop, out float capsuleRadius)
        {
            Vector3 lossyScale = transform.lossyScale;
            float scaleX = Mathf.Abs(lossyScale.x);
            float scaleY = Mathf.Abs(lossyScale.y);
            float scaleZ = Mathf.Abs(lossyScale.z);

            capsuleRadius = playerCollider.radius * Mathf.Max(scaleX, scaleZ);
            float capsuleHalfHeight = Mathf.Max(capsuleRadius, playerCollider.height * 0.5f * scaleY);
            Vector3 capsuleCenter = bodyPosition + transform.rotation * playerCollider.center;
            float cylinderHalfHeight = Mathf.Max(0.0f, capsuleHalfHeight - capsuleRadius);

            capsuleBottom = capsuleCenter + Vector3.down * cylinderHalfHeight;
            capsuleTop = capsuleCenter + Vector3.up * cylinderHalfHeight;
        }

        private void TryApplyContactPush(Collision collision)
        {
            if (!pushRigidbodiesOnContact || IsReceiveBombImpact || motionLocked)
                return;

            if (collision == null || collision.collider == null || collision.collider.transform.IsChildOf(transform))
                return;

            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            Vector3 pushDirection = ResolveContactPushDirection(collision, contact.normal);

            if (pushDirection.sqrMagnitude <= 0.0001f)
                return;

            float pushSpeed = new Vector3(CurrentVelocity.x, 0.0f, CurrentVelocity.z).magnitude;

            if (pushSpeed < minContactPushSpeed)
                return;

            float pushImpulse = Mathf.Min(
                maxContactPushImpulse,
                contactPushImpulse + pushSpeed * contactPushSpeedMultiplier);

            if (pushImpulse <= 0.0f)
                return;

            EntityImpactData impactData = new EntityImpactData(
                EntityImpactKind.Contact,
                gameObject,
                transform,
                collision.collider,
                collision.contactCount > 0 ? contact.point : transform.position,
                pushDirection,
                pushImpulse);

            EntityImpactResponseMB impactResponse = collision.collider.GetComponentInParent<EntityImpactResponseMB>();

            if (impactResponse != null)
                impactResponse.TryApplyImpact(impactData);
        }

        private Vector3 ResolveContactPushDirection(Collision collision, Vector3 contactNormal)
        {
            Vector3 direction = CurrentVelocity;
            direction.y = 0.0f;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = -Vector3.ProjectOnPlane(contactNormal, Vector3.up);

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = collision.collider.transform.position - transform.position;
                direction.y = 0.0f;
            }

            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        }

        private void ApplyCushionStop()
        {
            planarVelocity = Vector3.zero;
            externalVelocity = Vector3.zero;
            inheritedPlatformVelocity = Vector3.zero;
            verticalVelocity = groundedStickVelocity;
            ApplyCurrentVelocityToBody(GetBodyVelocity());
        }

        private void ApplyCushionBounce(Vector3 bounceVelocity)
        {
            planarVelocity = Vector3.zero;
            verticalVelocity = groundedStickVelocity;
            externalVelocity = bounceVelocity;
            inheritedPlatformVelocity = Vector3.zero;
            ApplyCurrentVelocityToBody(GetBodyVelocity());
        }

        private EntityTagId ResolveCushionImpactTag()
        {
            if (entityMB != null && entityMB.Tag.IsValid)
                return entityMB.Tag;

            return EntityTags.Actor.Player.Id;
        }

        public void SetPlanarVelocity(Vector3 velocity)
        {
            velocity.y = 0.0f;
            planarVelocity = velocity;
        }

        public void SetVerticalVelocity(float velocity)
        {
            verticalVelocity = velocity;
        }

        public void EnterMotionLock(EntityMoveState lockedState)
        {
            if (motionLocked)
                return;

            preservedVelocityWhenLocked = CurrentVelocity;
            motionLocked = true;
            StateMachine.ChangeState(lockedState);
        }

        public void ExitMotionLock(Vector3 releaseImpulse, float preservedVelocityRate = 0.35f)
        {
            if (!motionLocked)
                return;

            motionLocked = false;

            externalVelocity += preservedVelocityWhenLocked * preservedVelocityRate;
            externalVelocity += releaseImpulse;

            preservedVelocityWhenLocked = Vector3.zero;
        }

        public void ReviveFromCheckpoint()
        {
            IsReceiveBombImpact = false;
            motionLocked = false;
            preservedVelocityWhenLocked = Vector3.zero;

            planarVelocity = Vector3.zero;
            verticalVelocity = groundedStickVelocity;
            externalVelocity = Vector3.zero;
            inheritedPlatformVelocity = Vector3.zero;

            ApplyCurrentVelocityToBody(Vector3.up * verticalVelocity);
            StateMachine.ChangeState(EntityMoveState.Idle);

            if (IsRuntimeReady && SceneKernel.ValueStore != null)
            {
                SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsDead, false);
                SceneKernel.ValueStore.RemoveBoolModifier(Entity, ValueKeys.Move.CanMove, DeadMoveLockTag);
            }

            PublishRuntimeValues();
        }

        public bool IsGrounded => ground.IsValid;

        public bool IsSprinting =>
            sprintHeld &&
            MoveState == EntityMoveState.Moving &&
            CurrentPlanarSpeed > 0.15f;

        public bool IsDead => IsReceiveBombImpact;

        public Vector3 ControlledPlanarVelocity
        {
            get
            {
                Vector3 velocity = planarVelocity;
                velocity.y = 0.0f;
                return velocity;
            }
        }

        public float CurrentPlanarSpeed => ControlledPlanarVelocity.magnitude;

        public float NormalizedPlanarSpeed
        {
            get
            {
                float baseSpeed = GetMoveBaseSpeed(fallbackMoveSpeed);
                float sprintMul = GetSprintMultiplier(fallbackSprintMultiplier);
                float maxSpeed = Mathf.Max(0.01f, baseSpeed * sprintMul);
                return Mathf.Clamp01(CurrentPlanarSpeed / maxSpeed);
            }
        }

        private void PublishRuntimeValues()
        {
            if (!IsRuntimeReady || SceneKernel.ValueStore == null)
                return;

            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.MoveState, MoveState);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.CurrentPlanarSpeed, CurrentPlanarSpeed);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.VerticalVelocity, verticalVelocity);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsGrounded, IsGrounded);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsSprinting, IsSprinting);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsDead, IsDead);
        }

        private void OnDestroy()
        {
            if (lowFrictionContactMaterial == null)
                return;

            if (Application.isPlaying)
                Destroy(lowFrictionContactMaterial);
            else
                DestroyImmediate(lowFrictionContactMaterial);

            lowFrictionContactMaterial = null;
        }

        private void EnsureMovementBody()
        {
            if (playerRigidbody == null)
                playerRigidbody = GetComponent<Rigidbody>();

            if (playerCollider == null)
                playerCollider = GetComponent<CapsuleCollider>();

            legacyCharacterController = GetComponent<CharacterController>();

            if (playerCollider == null)
            {
                playerCollider = gameObject.AddComponent<CapsuleCollider>();

                if (legacyCharacterController != null)
                {
                    playerCollider.center = legacyCharacterController.center;
                    playerCollider.radius = legacyCharacterController.radius;
                    playerCollider.height = legacyCharacterController.height;
                }
            }

            if (playerRigidbody == null)
                playerRigidbody = gameObject.AddComponent<Rigidbody>();

            playerRigidbody.useGravity = false;
            playerRigidbody.isKinematic = false;
            playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            playerRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            playerRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            EnsureLowFrictionColliderMaterial();

            if (legacyCharacterController != null)
                legacyCharacterController.enabled = false;
        }

        private void EnsureLowFrictionColliderMaterial()
        {
            if (playerCollider == null)
                return;

            if (lowFrictionContactMaterial == null)
            {
                lowFrictionContactMaterial = new PhysicsMaterial($"{nameof(PlayerMoveController)}_LowFriction")
                {
                    dynamicFriction = 0.0f,
                    staticFriction = 0.0f,
                    bounciness = 0.0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum,
                };
                lowFrictionContactMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            if (playerCollider.sharedMaterial != lowFrictionContactMaterial)
                playerCollider.sharedMaterial = lowFrictionContactMaterial;
        }

        private readonly struct GroundInfo
        {
            public readonly bool IsValid;
            public readonly Vector3 Normal;
            public readonly Vector3 Point;
            public readonly Transform Transform;

            public GroundInfo(bool isValid, Vector3 normal, Vector3 point, Transform transform)
            {
                IsValid = isValid;
                Normal = normal;
                Point = point;
                Transform = transform;
            }
        }
    }
}

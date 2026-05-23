using System;
using System.Threading;
using BC.Gimmick.Cushion;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.Base
{
    public readonly struct CushionHighJumpEventData
    {
        public CushionHighJumpEventData(Vector3 bounceVelocity)
        {
            BounceVelocity = bounceVelocity;
        }

        public Vector3 BounceVelocity { get; }
    }

    [DefaultExecutionOrder(90)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class EntityMoveMotorMB : EntityMoveController, IEntityMoveAnimationSource, IEntityVelocitySource, ICushionImpactSource
    {
        public static readonly ValueModifierTagId GameLogicTag = new ValueModifierTagId(10002);

        [Header("References")]
        [SerializeField] private Rigidbody bodyRigidbody;
        [SerializeField] private CapsuleCollider bodyCollider;

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
        [SerializeField, Min(0.0f)] private float platformSupportSampleSmoothing = 0.0f;
        [SerializeField, Min(0.0f)] private float platformCarryRotationDeadZoneDegrees = 0.05f;

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

        [Header("Runtime Debug")]
        [SerializeField] private bool currentCanMoveByInput;
        [SerializeField] private bool currentCanMoveBySystem;

        private const int MaxGroundHits = 8;
        private const int MaxStepOverlapHits = 8;
        private const float StepAssistSurfaceSkin = 0.02f;
        private const float StepAssistWallMaxUpDot = 0.25f;
        private const float StepAssistMinApproachDot = 0.2f;
        private const float AutoMoveStopSpeedSqr = 0.01f;

        private readonly RaycastHit[] groundHits = new RaycastHit[MaxGroundHits];
        private readonly Collider[] stepOverlapHits = new Collider[MaxStepOverlapHits];

        private EntityMB entityMB;
        private CharacterController legacyCharacterController;
        private PhysicsMaterial lowFrictionContactMaterial;

        private Vector3 planarVelocity;
        private float verticalVelocity;
        private Vector3 externalVelocity;
        private Vector3 inheritedPlatformVelocity;

        private Vector3 moveDirection;
        private bool sprintHeld;
        private bool jumpHeld;
        private float jumpBufferCounter;
        private float lastGroundedTime = -999.0f;

        private GroundInfo ground;
        private Transform currentPlatform;
        private Vector3 lastPlatformPosition;
        private Quaternion lastPlatformRotation;
        private bool hasPlatformPose;
        private Vector3 platformDelta;
        private Vector3 platformVelocity;
        private Transform supportSampleSource;
        private Vector3 supportSampleLocalPoint;
        private bool hasSupportSamplePoint;
        private bool suppressPlatformVelocityInjectionThisTick;
        private bool wasGroundedLastMotorTick;

        private bool motionLocked;
        private Vector3 preservedVelocityWhenLocked;
        private float nextCushionImpactTime;
        private float pendingCushionHighJumpExpireTime = -999.0f;
        private Vector3 pendingCushionBounceDirection;
        private float pendingCushionBounceSpeed;
        private float pendingCushionBounceSpeedLimit;
        private float pendingCushionHighJumpMultiplier = 1.0f;
        private bool isDead;
        private ValueModifierTagId? activeDeadMoveLockTag;

        private CancellationTokenSource activeAutoMoveCancellationTokenSource;
        private Vector3 autoMoveTargetPosition;
        private float autoMoveArrivalDistanceSqr;
        private bool autoMoveActive;
        private bool autoMoveReachedTarget;

        public Vector3 PlanarVelocity => planarVelocity;
        public float VerticalVelocity => verticalVelocity;
        public Vector3 ExternalVelocity => externalVelocity;
        public Vector3 PlatformVelocity => platformVelocity;

        public Vector3 CurrentVelocity => GetBodyVelocity() + platformVelocity;
        public bool CanMoveByInput => currentCanMoveByInput;
        public bool CanMoveBySystem => currentCanMoveBySystem;
        public bool CanProcessMoveInput => CanReceiveMoveInput() && CanApplySystemMovement();
        public bool IsAutoMoveActive => autoMoveActive;
        public bool IsGrounded => ground.IsValid;
        public bool IsDead => isDead;
        public event Action<CushionHighJumpEventData> CushionHighJumped;
        public Vector3 GroundNormal => ground.IsValid ? ground.Normal : Vector3.up;
        public Vector3 GroundPoint => ground.IsValid ? ground.Point : transform.position;
        public Transform GroundTransform => ground.IsValid ? ground.Transform : null;

        public Transform CushionImpactRoot => transform;
        public EntityTagId CushionImpactTag => ResolveCushionImpactTag();

        public bool IsSprinting =>
            sprintHeld &&
            MoveState == EntityMoveState.Moving &&
            CurrentPlanarSpeed > 0.15f;

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

        private void Awake()
        {
            EnsureMovementBody();
        }

        protected override void Start()
        {
            base.Start();

            EnsureMovementBody();

            if (bodyRigidbody == null || bodyCollider == null)
            {
                Debug.LogError($"{nameof(EntityMoveMotorMB)}: Rigidbody or CapsuleCollider is missing.", this);
                enabled = false;
                return;
            }

            entityMB = GetComponentInParent<EntityMB>();
        }

        private void OnEnable()
        {
            EnsureMovementBody();
        }

        private void OnDisable()
        {
            CancelAutoMove();
            ClearMoveIntent();
        }

        private void FixedUpdate()
        {
            if (!IsRuntimeReady || bodyRigidbody == null || bodyCollider == null)
                return;

            float dt = Time.fixedDeltaTime;

            if (dt <= 0.0f)
                return;

            RefreshMoveGateDebugValues();

            if (!currentCanMoveBySystem)
            {
                TickSystemMovementLocked(dt);
                PublishRuntimeValues();
                return;
            }

            if (motionLocked)
            {
                TickLockedMotion(dt);
                PublishRuntimeValues();
                return;
            }

            TickMotor(dt);
            PublishRuntimeValues();
        }

        public void SetMoveIntent(Vector3 worldMoveDirection, bool wantsSprint, bool jumpPressedThisFrame, bool jumpHeldInput, float deltaTime)
        {
            if (autoMoveActive)
            {
                ClearMoveIntent();
                return;
            }

            bool canReceiveInput = CanProcessMoveInput;

            if (canReceiveInput)
            {
                worldMoveDirection.y = 0.0f;
                moveDirection = Vector3.ClampMagnitude(worldMoveDirection, 1.0f);
                sprintHeld = wantsSprint;
                jumpHeld = jumpHeldInput;

                if (jumpPressedThisFrame)
                {
                    jumpBufferCounter = jumpBufferTime;
                    return;
                }
            }
            else
            {
                moveDirection = Vector3.zero;
                sprintHeld = false;
                jumpHeld = false;
            }

            jumpBufferCounter -= deltaTime;
        }

        public void ClearMoveIntent()
        {
            moveDirection = Vector3.zero;
            sprintHeld = false;
            jumpHeld = false;
            jumpBufferCounter = 0.0f;
            ClearPendingCushionHighJump();
        }

        private void TickMotor(float dt)
        {
            suppressPlatformVelocityInjectionThisTick = false;
            ProbeGround();

            bool isGrounded = ground.IsValid;

            if (!isGrounded && wasGroundedLastMotorTick)
                CapturePlatformInertiaOnLeaveGround();

            if (isGrounded)
                lastGroundedTime = Time.time;

            UpdatePlatformMotion(dt, isGrounded);

            if (isGrounded && !wasGroundedLastMotorTick)
                RebaseInheritedPlatformMomentumOnLanding();

            UpdatePlanarVelocity(dt, isGrounded);
            UpdateVerticalVelocity(dt, isGrounded);
            UpdateExternalVelocity(dt);

            Vector3 bodyVelocity = GetBodyVelocity();
            bool steppedUp = TryStepUp(dt, bodyVelocity, isGrounded);

            if (steppedUp && !isGrounded)
                RebaseInheritedPlatformMomentumOnLanding();

            bodyVelocity = GetBodyVelocity();

            ApplyCurrentVelocityToBody(bodyVelocity, isGrounded);

            StorePlatformPose();
            UpdateMoveState();
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
            if (TryConsumePendingCushionHighJump())
                return;

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

        private void ProbeGround()
        {
            ground = default;

            if (bodyCollider == null)
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
                ground = new GroundInfo(true, hit.normal, hit.point, hit.collider.transform, hit.collider);
            }
        }

        private void GetGroundProbeParameters(out Vector3 center, out float radius, out float distance)
        {
            Vector3 lossyScale = transform.lossyScale;
            float scaleX = Mathf.Abs(lossyScale.x);
            float scaleY = Mathf.Abs(lossyScale.y);
            float scaleZ = Mathf.Abs(lossyScale.z);

            float colliderRadius = bodyCollider.radius * Mathf.Max(scaleX, scaleZ);
            float colliderHalfHeight = bodyCollider.height * 0.5f * scaleY;

            center = transform.TransformPoint(bodyCollider.center);
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
                supportSampleSource = null;
                hasSupportSamplePoint = false;
                return;
            }

            Transform platform = ground.Transform;
            Vector3 supportSamplePoint = ResolveSupportSamplePoint(platform, dt);

            if (SupportMotionUtility.TryGetSupportMotion(
                    ground.Collider,
                    supportSamplePoint,
                    dt,
                    transform,
                    out SupportMotionSnapshot supportMotion))
            {
                Transform supportSource = supportMotion.SourceTransform != null ? supportMotion.SourceTransform : platform;
                UpdateSupportSamplePoint(supportSource, supportSamplePoint, dt);

                Vector3 stabilizedPoint = hasSupportSamplePoint && supportSource != null
                    ? supportSource.TransformPoint(supportSampleLocalPoint)
                    : supportSamplePoint;

                Vector3 sourceOffset = stabilizedPoint - supportMotion.SourceOrigin;
                Vector3 stabilizedDelta = supportMotion.SourcePositionDelta +
                                         supportMotion.SourceRotationDelta * sourceOffset -
                                         sourceOffset;

                if (Quaternion.Angle(supportMotion.SourceRotationDelta, Quaternion.identity) <= platformCarryRotationDeadZoneDegrees)
                    stabilizedDelta = supportMotion.SourcePositionDelta;

                platformDelta = stabilizedDelta;
                if (dt > 0.0f)
                    platformVelocity = stabilizedDelta / dt;

                currentPlatform = supportSource;
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

        private Vector3 ResolveSupportSamplePoint(Transform platform, float dt)
        {
            Vector3 desiredWorldPoint = bodyRigidbody != null
                ? bodyRigidbody.worldCenterOfMass
                : transform.position;

            if (!float.IsFinite(desiredWorldPoint.x) ||
                !float.IsFinite(desiredWorldPoint.y) ||
                !float.IsFinite(desiredWorldPoint.z))
            {
                desiredWorldPoint = transform.position;
            }

            if (platform == null)
                return desiredWorldPoint;

            UpdateSupportSamplePoint(platform, desiredWorldPoint, dt);
            return platform.TransformPoint(supportSampleLocalPoint);
        }

        private void UpdateSupportSamplePoint(Transform sourceTransform, Vector3 desiredWorldPoint, float dt)
        {
            if (sourceTransform == null)
            {
                supportSampleSource = null;
                hasSupportSamplePoint = false;
                return;
            }

            Vector3 desiredLocalPoint = sourceTransform.InverseTransformPoint(desiredWorldPoint);

            if (!hasSupportSamplePoint || supportSampleSource != sourceTransform)
            {
                supportSampleSource = sourceTransform;
                supportSampleLocalPoint = desiredLocalPoint;
                hasSupportSamplePoint = true;
                return;
            }

            float smoothing = Mathf.Max(0.0f, platformSupportSampleSmoothing);
            if (smoothing <= 0.0f)
            {
                supportSampleLocalPoint = desiredLocalPoint;
                return;
            }

            float blend = 1.0f - Mathf.Exp(-smoothing * Mathf.Max(0.0001f, dt));
            supportSampleLocalPoint = Vector3.Lerp(supportSampleLocalPoint, desiredLocalPoint, Mathf.Clamp01(blend));
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

            // このtickは継承済みなので、追加の足場速度注入を抑止して二重加算を防ぐ。
            suppressPlatformVelocityInjectionThisTick = true;
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

        private void TickLockedMotion(float dt)
        {
            suppressPlatformVelocityInjectionThisTick = false;
            ProbeGround();
            bool isGrounded = ground.IsValid;

            if (!isGrounded && wasGroundedLastMotorTick)
                CapturePlatformInertiaOnLeaveGround();

            UpdatePlatformMotion(dt, isGrounded);

            if (isGrounded && !wasGroundedLastMotorTick)
                RebaseInheritedPlatformMomentumOnLanding();

            UpdateExternalVelocity(dt);

            ApplyCurrentVelocityToBody(externalVelocity + inheritedPlatformVelocity, isGrounded);
            StorePlatformPose();
            wasGroundedLastMotorTick = ground.IsValid;
        }

        private void TickSystemMovementLocked(float dt)
        {
            suppressPlatformVelocityInjectionThisTick = false;
            CancelAutoMove();
            ClearMoveIntent();

            planarVelocity = Vector3.zero;
            verticalVelocity = 0.0f;
            externalVelocity = Vector3.zero;
            inheritedPlatformVelocity = Vector3.zero;

            ProbeGround();
            bool isGrounded = ground.IsValid;

            UpdatePlatformMotion(dt, isGrounded);
            ApplyCurrentVelocityToBody(Vector3.zero, isGrounded);
            StorePlatformPose();

            if (MoveState != EntityMoveState.Disabled && MoveState != EntityMoveState.Dead)
                StateMachine.ChangeState(EntityMoveState.Disabled);

            wasGroundedLastMotorTick = ground.IsValid;
        }

        private void ApplyCurrentVelocityToBody(Vector3 velocity, bool injectPlatformVelocity = false)
        {
            if (bodyRigidbody == null)
                return;

            Vector3 appliedVelocity = velocity;
            if (injectPlatformVelocity && !suppressPlatformVelocityInjectionThisTick)
                appliedVelocity += platformVelocity;

            bodyRigidbody.linearVelocity = appliedVelocity;
        }

        private Vector3 GetBodyVelocity()
        {
            return planarVelocity + Vector3.up * verticalVelocity + externalVelocity + inheritedPlatformVelocity;
        }

        public void AddImpulse(Vector3 impulseVelocity)
        {
            if (!CanApplySystemMovement())
                return;

            externalVelocity += impulseVelocity;
        }

        public void SetExternalVelocity(Vector3 velocity)
        {
            if (!CanApplySystemMovement())
                return;

            externalVelocity = velocity;
        }

        public void ClearExternalVelocity()
        {
            externalVelocity = Vector3.zero;
        }

        public bool HandleCushionImpact(CushionImpactData impactData, CushionImpactResult impactResult)
        {
            if (!impactResult.IsHandled || isDead || !CanApplySystemMovement())
                return false;

            switch (impactResult.ResponseKind)
            {
                case CushionResponseKind.Bounce:
                    ApplyCushionBounce(impactResult);
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

            if (!CanApplySystemMovement())
                return;

            ResolveCollisionVelocityConstraints(collision);
            TryApplyContactPush(collision);

            if (Time.time < nextCushionImpactTime)
                return;

            CushionSurfaceMB surface = collision.collider.GetComponentInParent<CushionSurfaceMB>();

            if (surface == null)
                return;

            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;

            Vector3 incomingVelocity = bodyRigidbody != null
                ? bodyRigidbody.linearVelocity
                : CurrentVelocity;

            CushionImpactData impactData = new CushionImpactData(
                gameObject,
                transform,
                entityMB,
                CushionImpactTag,
                bodyRigidbody,
                bodyCollider,
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
                bodyRigidbody == null ||
                bodyCollider == null ||
                motionLocked ||
                isDead ||
                verticalVelocity > 0.1f)
            {
                return false;
            }

            bool canStep = isGrounded || Time.time - lastGroundedTime <= coyoteTime;

            if (!canStep)
                return false;

            Vector3 desiredDirection = GetDesiredMoveDirection();
            Vector3 horizontalVelocity = bodyVelocity;
            horizontalVelocity.y = 0.0f;

            if (desiredDirection.sqrMagnitude <= 0.0001f)
            {
                if (horizontalVelocity.sqrMagnitude <= minStepAssistSpeed * minStepAssistSpeed)
                    return false;

                desiredDirection = horizontalVelocity.normalized;
            }

            float horizontalSpeed = horizontalVelocity.magnitude;
            float forwardDistance = Mathf.Clamp(horizontalSpeed * Time.fixedDeltaTime + StepAssistSurfaceSkin, StepAssistSurfaceSkin, stepAssistForwardDistance);

            if (forwardDistance <= 0.0f)
                return false;

            GetCapsuleGeometry(bodyRigidbody.position, out Vector3 capsuleBottom, out _, out float capsuleRadius);
            Vector3 capsuleCenter = bodyRigidbody.position + transform.rotation * bodyCollider.center;
            float castRadius = Mathf.Max(0.01f, capsuleRadius - StepAssistSurfaceSkin);
            float feetY = capsuleBottom.y - capsuleRadius;
            Vector3 lowerOrigin = new Vector3(capsuleCenter.x, feetY + castRadius + 0.05f, capsuleCenter.z);
            Vector3 upperOrigin = lowerOrigin + Vector3.up * maxStepHeight;

            if (!Physics.SphereCast(
                    lowerOrigin,
                    castRadius,
                    desiredDirection,
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

            if (Vector3.Dot(desiredDirection, -wallNormal) < StepAssistMinApproachDot)
                return false;

            if (Physics.SphereCast(
                    upperOrigin,
                    castRadius,
                    desiredDirection,
                    out RaycastHit upperHit,
                    forwardDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore) &&
                upperHit.collider != null &&
                !upperHit.transform.IsChildOf(transform))
            {
                return false;
            }

            Vector3 candidatePosition = bodyRigidbody.position + Vector3.up * (maxStepHeight + StepAssistSurfaceSkin) + desiredDirection * Mathf.Max(lowerHit.distance + StepAssistSurfaceSkin, forwardDistance * 0.5f);

            if (!TryFindStepGround(candidatePosition, out Vector3 snappedPosition, out GroundInfo stepGround))
                return false;

            GetCapsuleGeometry(snappedPosition, out Vector3 candidateCapsuleBottom, out _, out float candidateCapsuleRadius);
            float candidateFeetY = candidateCapsuleBottom.y - candidateCapsuleRadius;

            if (!CanOccupyCapsule(snappedPosition, candidateFeetY))
                return false;

            if (snappedPosition.y <= bodyRigidbody.position.y + 0.0001f)
                return false;

            if (snappedPosition.y - bodyRigidbody.position.y > maxStepHeight + StepAssistSurfaceSkin)
                return false;

            bodyRigidbody.position = snappedPosition;
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
                   bodyRigidbody != null &&
                   bodyCollider != null &&
                   !motionLocked &&
                   !isDead &&
                   CanApplySystemMovement();
        }

        private bool IsAlreadyAtAutoMoveTarget(Vector3 targetPosition, float arriveDistance)
        {
            if (bodyRigidbody == null)
                return false;

            Vector3 toTarget = targetPosition - bodyRigidbody.position;
            toTarget.y = 0.0f;

            return toTarget.sqrMagnitude <= Mathf.Max(0.0001f, arriveDistance * arriveDistance);
        }

        private Vector3 GetDesiredMoveDirection()
        {
            if (autoMoveActive)
                return BuildAutoMoveDirection();

            return moveDirection;
        }

        private Vector3 BuildAutoMoveDirection()
        {
            if (bodyRigidbody == null)
                return Vector3.zero;

            Vector3 toTarget = autoMoveTargetPosition - bodyRigidbody.position;
            toTarget.y = 0.0f;

            autoMoveReachedTarget = toTarget.sqrMagnitude <= autoMoveArrivalDistanceSqr;

            if (autoMoveReachedTarget)
                return Vector3.zero;

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
            Vector3 capsuleCenter = bodyPosition + transform.rotation * bodyCollider.center;
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

            if (snappedPosition.y - bodyRigidbody.position.y > maxStepHeight + StepAssistSurfaceSkin)
                return false;

            stepGround = new GroundInfo(true, hit.normal, hit.point, hit.collider.transform, hit.collider);
            return true;
        }

        private void GetCapsuleGeometry(Vector3 bodyPosition, out Vector3 capsuleBottom, out Vector3 capsuleTop, out float capsuleRadius)
        {
            Vector3 lossyScale = transform.lossyScale;
            float scaleX = Mathf.Abs(lossyScale.x);
            float scaleY = Mathf.Abs(lossyScale.y);
            float scaleZ = Mathf.Abs(lossyScale.z);

            capsuleRadius = bodyCollider.radius * Mathf.Max(scaleX, scaleZ);
            float capsuleHalfHeight = Mathf.Max(capsuleRadius, bodyCollider.height * 0.5f * scaleY);
            Vector3 capsuleCenter = bodyPosition + transform.rotation * bodyCollider.center;
            float cylinderHalfHeight = Mathf.Max(0.0f, capsuleHalfHeight - capsuleRadius);

            capsuleBottom = capsuleCenter + Vector3.down * cylinderHalfHeight;
            capsuleTop = capsuleCenter + Vector3.up * cylinderHalfHeight;
        }

        private void TryApplyContactPush(Collision collision)
        {
            if (!pushRigidbodiesOnContact || isDead || motionLocked)
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
            ClearPendingCushionHighJump();
            planarVelocity = Vector3.zero;
            externalVelocity = Vector3.zero;
            inheritedPlatformVelocity = Vector3.zero;
            verticalVelocity = groundedStickVelocity;
            ApplyCurrentVelocityToBody(GetBodyVelocity());
        }

        private void ApplyCushionBounce(CushionImpactResult impactResult)
        {
            ClearPendingCushionHighJump();

            Vector3 bounceVelocity = impactResult.BounceVelocity;
            if (TryBuildHighJumpBounceVelocity(impactResult.BounceVelocity, impactResult.BounceSpeedLimit, impactResult.HighJumpSpeedMultiplier, out Vector3 highJumpBounceVelocity))
            {
                bounceVelocity = highJumpBounceVelocity;
                CushionHighJumped?.Invoke(new CushionHighJumpEventData(bounceVelocity));
            }
            else
            {
                ArmPendingCushionHighJump(impactResult);
            }

            planarVelocity = Vector3.zero;
            verticalVelocity = groundedStickVelocity;
            externalVelocity = bounceVelocity;
            inheritedPlatformVelocity = Vector3.zero;
            ApplyCurrentVelocityToBody(GetBodyVelocity());
        }

        private bool TryConsumePendingCushionHighJump()
        {
            if (Time.time > pendingCushionHighJumpExpireTime)
            {
                ClearPendingCushionHighJump();
                return false;
            }

            if (!HasBufferedHighJumpInput())
                return false;

            Vector3 normalBounceVelocity = pendingCushionBounceDirection * pendingCushionBounceSpeed;
            if (!TryBuildHighJumpBounceVelocity(normalBounceVelocity, pendingCushionBounceSpeedLimit, pendingCushionHighJumpMultiplier, out Vector3 highJumpBounceVelocity))
                return false;

            planarVelocity = Vector3.zero;
            verticalVelocity = groundedStickVelocity;
            externalVelocity = highJumpBounceVelocity;
            inheritedPlatformVelocity = Vector3.zero;
            ApplyCurrentVelocityToBody(GetBodyVelocity());
            CushionHighJumped?.Invoke(new CushionHighJumpEventData(highJumpBounceVelocity));
            return true;
        }

        private bool TryBuildHighJumpBounceVelocity(
            Vector3 normalBounceVelocity,
            float bounceSpeedLimit,
            float highJumpSpeedMultiplier,
            out Vector3 highJumpBounceVelocity)
        {
            highJumpBounceVelocity = normalBounceVelocity;

            if (highJumpSpeedMultiplier <= 1.0001f || !HasBufferedHighJumpInput())
                return false;

            float normalBounceSpeed = normalBounceVelocity.magnitude;
            if (normalBounceSpeed <= 0.0001f)
                return false;

            Vector3 bounceDirection = normalBounceVelocity / normalBounceSpeed;
            float cappedBoostedSpeed = normalBounceSpeed * highJumpSpeedMultiplier;
            if (bounceSpeedLimit > 0f)
                cappedBoostedSpeed = Mathf.Min(cappedBoostedSpeed, bounceSpeedLimit * highJumpSpeedMultiplier);

            if (cappedBoostedSpeed <= normalBounceSpeed + 0.0001f)
                return false;

            ConsumeHighJumpInput();
            highJumpBounceVelocity = bounceDirection * cappedBoostedSpeed;
            return true;
        }

        private void ArmPendingCushionHighJump(CushionImpactResult impactResult)
        {
            if (impactResult.ResponseKind != CushionResponseKind.Bounce ||
                impactResult.HighJumpSpeedMultiplier <= 1.0001f ||
                impactResult.BounceVelocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float bounceSpeed = impactResult.BounceVelocity.magnitude;
            pendingCushionHighJumpExpireTime = Time.time + coyoteTime;
            pendingCushionBounceDirection = impactResult.BounceVelocity / bounceSpeed;
            pendingCushionBounceSpeed = bounceSpeed;
            pendingCushionBounceSpeedLimit = impactResult.BounceSpeedLimit;
            pendingCushionHighJumpMultiplier = impactResult.HighJumpSpeedMultiplier;
        }

        private void ClearPendingCushionHighJump()
        {
            pendingCushionHighJumpExpireTime = -999.0f;
            pendingCushionBounceDirection = Vector3.zero;
            pendingCushionBounceSpeed = 0f;
            pendingCushionBounceSpeedLimit = 0f;
            pendingCushionHighJumpMultiplier = 1.0f;
        }

        private bool HasBufferedHighJumpInput()
        {
            return jumpHeld || jumpBufferCounter > 0.0f;
        }

        private void ConsumeHighJumpInput()
        {
            jumpBufferCounter = 0.0f;
            lastGroundedTime = -999.0f;
            ClearPendingCushionHighJump();
            StateMachine.ChangeState(EntityMoveState.Jumping);
        }

        private EntityTagId ResolveCushionImpactTag()
        {
            if (entityMB != null && entityMB.Tag.IsValid)
                return entityMB.Tag;

            return default;
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

        public void EnterDeadState(ValueModifierTagId moveLockTag)
        {
            isDead = true;
            activeDeadMoveLockTag = moveLockTag;
            StateMachine.ChangeState(EntityMoveState.Dead);

            if (IsRuntimeReady && SceneKernel.ValueStore != null)
            {
                SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsDead, true);
                SetSystemMovementModifier(moveLockTag, false);
            }
        }

        public void ReviveFromCheckpoint()
        {
            isDead = false;
            motionLocked = false;
            preservedVelocityWhenLocked = Vector3.zero;

            planarVelocity = Vector3.zero;
            verticalVelocity = groundedStickVelocity;
            externalVelocity = Vector3.zero;
            inheritedPlatformVelocity = Vector3.zero;

            ApplyCurrentVelocityToBody(Vector3.up * verticalVelocity);
            StateMachine.ChangeState(EntityMoveState.Idle);

            if (activeDeadMoveLockTag.HasValue)
            {
                RemoveSystemMovementModifier(activeDeadMoveLockTag.Value);
                activeDeadMoveLockTag = null;
            }

            if (IsRuntimeReady && SceneKernel.ValueStore != null)
                SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsDead, false);

            PublishRuntimeValues();
        }

        public void SetSystemMovementModifier(ValueModifierTagId tag, bool canMove)
        {
            if (IsRuntimeReady && SceneKernel.ValueStore != null)
                SceneKernel.ValueStore.SetBoolModifier(Entity, ValueKeys.Move.CanMoveBySystem, tag, canMove);
        }

        public void RemoveSystemMovementModifier(ValueModifierTagId tag)
        {
            if (IsRuntimeReady && SceneKernel.ValueStore != null)
                SceneKernel.ValueStore.RemoveBoolModifier(Entity, ValueKeys.Move.CanMoveBySystem, tag);
        }

        private void PublishRuntimeValues()
        {
            RefreshMoveGateDebugValues();

            if (!IsRuntimeReady || SceneKernel.ValueStore == null)
                return;

            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.MoveState, MoveState);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.CurrentPlanarSpeed, CurrentPlanarSpeed);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.VerticalVelocity, verticalVelocity);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsGrounded, IsGrounded);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsSprinting, IsSprinting);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsDead, IsDead);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.CanMoveByInput, currentCanMoveByInput);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.CanMoveBySystem, currentCanMoveBySystem);
        }

        private void RefreshMoveGateDebugValues()
        {
            currentCanMoveByInput = CanReceiveMoveInput();
            currentCanMoveBySystem = CanApplySystemMovement();
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
            if (bodyRigidbody == null)
                bodyRigidbody = GetComponent<Rigidbody>();

            if (bodyCollider == null)
                bodyCollider = GetComponent<CapsuleCollider>();

            legacyCharacterController = GetComponent<CharacterController>();

            if (bodyCollider == null)
            {
                bodyCollider = gameObject.AddComponent<CapsuleCollider>();

                if (legacyCharacterController != null)
                {
                    bodyCollider.center = legacyCharacterController.center;
                    bodyCollider.radius = legacyCharacterController.radius;
                    bodyCollider.height = legacyCharacterController.height;
                }
            }

            if (bodyRigidbody == null)
                bodyRigidbody = gameObject.AddComponent<Rigidbody>();

            bodyRigidbody.useGravity = false;
            bodyRigidbody.isKinematic = false;
            bodyRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            bodyRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            bodyRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            EnsureLowFrictionColliderMaterial();

            if (legacyCharacterController != null)
                legacyCharacterController.enabled = false;
        }

        private void EnsureLowFrictionColliderMaterial()
        {
            if (bodyCollider == null)
                return;

            if (lowFrictionContactMaterial == null)
            {
                lowFrictionContactMaterial = new PhysicsMaterial($"{nameof(EntityMoveMotorMB)}_LowFriction")
                {
                    dynamicFriction = 0.0f,
                    staticFriction = 0.0f,
                    bounciness = 0.0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum,
                };
                lowFrictionContactMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            if (bodyCollider.sharedMaterial != lowFrictionContactMaterial)
                bodyCollider.sharedMaterial = lowFrictionContactMaterial;
        }

        private readonly struct GroundInfo
        {
            public readonly bool IsValid;
            public readonly Vector3 Normal;
            public readonly Vector3 Point;
            public readonly Transform Transform;
            public readonly Collider Collider;

            public GroundInfo(bool isValid, Vector3 normal, Vector3 point, Transform transform, Collider collider)
            {
                IsValid = isValid;
                Normal = normal;
                Point = point;
                Transform = transform;
                Collider = collider;
            }
        }
    }
}

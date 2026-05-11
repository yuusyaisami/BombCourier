using BC.Base;
using BC.Bomb;
using BC.Camera;
using BC.Gimmick.Cushion;
using BC.Manager;
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

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMoveController : EntityMoveController, IEntityMoveAnimationSource, IBombImpactReceiver, ICushionImpactSource // Rigidbodyがないので
    {
        private static readonly ValueModifierTagId DeadMoveLockTag = new ValueModifierTagId(10001);
        public static readonly ValueModifierTagId GameLogicTag = new ValueModifierTagId(10002);
        [Header("References")]
        [SerializeField] private CharacterController characterController;
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
        [Header("Impulse")] // ラグドールが吹き飛びすぎないようにするための制限
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

        private ICameraController cameraController;
        private IPlayerRagdollController ragdollController;
        private EntityMB entityMB;

        private Vector3 planarVelocity;
        private float verticalVelocity;
        private Vector3 externalVelocity;

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

        private bool motionLocked;
        private Vector3 preservedVelocityWhenLocked;
        private float nextCushionImpactTime;

        public Vector3 PlanarVelocity => planarVelocity;
        public float VerticalVelocity => verticalVelocity;
        public Vector3 ExternalVelocity => externalVelocity;
        public Vector3 PlatformVelocity => platformVelocity;

        public Vector3 CurrentVelocity =>
            planarVelocity + Vector3.up * verticalVelocity + externalVelocity + platformVelocity;
        public Transform CushionImpactRoot => transform;
        public EntityTagId CushionImpactTag => ResolveCushionImpactTag();
        private bool IsReceiveBombImpact; // 今は常に受け取るようにするが、将来的には状態によって受け取らないこともあるかもしれない

        protected override void Start()
        {
            base.Start();

            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (characterController == null)
            {
                Debug.LogError($"{nameof(PlayerMoveController)}: CharacterController is missing.", this);
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
            {
                modelRoot = transform;
            }
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
        }

        private void Update()
        {
            if (!IsRuntimeReady || characterController == null)
                return;

            float dt = Time.deltaTime;

            if (dt <= 0.0f)
                return;

            ReadInput(dt);

            if (motionLocked)
            {
                TickLockedMotion(dt);
                return;
            }

            TickMotor(dt);
            PublishRuntimeValues();
        }

        private void ReadInput(float dt)
        {
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

            bool isGrounded = ground.IsValid || characterController.isGrounded;

            if (isGrounded)
                lastGroundedTime = Time.time;

            UpdatePlatformMotion(dt, isGrounded);
            UpdatePlanarVelocity(dt, isGrounded);
            UpdateVerticalVelocity(dt, isGrounded);
            UpdateExternalVelocity(dt);

            Vector3 displacement =
                platformDelta +
                (planarVelocity + Vector3.up * verticalVelocity + externalVelocity) * dt;

            CollisionFlags flags = characterController.Move(displacement);

            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0.0f)
                verticalVelocity = 0.0f;

            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < groundedStickVelocity)
                verticalVelocity = groundedStickVelocity;

            ProbeGround();
            StorePlatformPose();
            UpdateMoveState();
            UpdateModelRotation(dt);
        }

        private void UpdatePlanarVelocity(float dt, bool isGrounded)
        {
            Vector3 desiredDirection = BuildCameraRelativeMoveDirection(moveInput);
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

            planarVelocity = Vector3.MoveTowards(
                planarVelocity,
                desiredVelocity,
                acceleration * dt);

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

                if (inheritMovingPlatformVelocityOnJump)
                    externalVelocity += platformVelocity * platformJumpVelocityInheritance;

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

            Vector3 center = transform.position + characterController.center;

            float radius = Mathf.Max(
                0.01f,
                characterController.radius - groundProbeRadiusShrink);

            float distance =
                characterController.height * 0.5f -
                characterController.radius +
                groundProbeExtraDistance;

            if (!Physics.SphereCast(
                    center,
                    radius,
                    Vector3.down,
                    out RaycastHit hit,
                    distance,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            float angle = Vector3.Angle(hit.normal, Vector3.up);

            if (angle > maxGroundAngle)
                return;

            ground = new GroundInfo(
                true,
                hit.normal,
                hit.point,
                hit.collider.transform);
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

        // 爆弾
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
                {
                    ragdollController.EnterRagdoll(ragdollImpulse);
                }

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

            if (ground.IsValid || characterController.isGrounded)
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

            Vector3 horizontalVelocity = new Vector3(planarVelocity.x, 0.0f, planarVelocity.z);

            if (horizontalVelocity.magnitude < minModelTurnSpeed)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized, Vector3.up);

            float t = 1.0f - Mathf.Exp(-modelTurnSharpness * dt);
            modelRoot.rotation = Quaternion.Slerp(modelRoot.rotation, targetRotation, t);
        }

        private void TickLockedMotion(float dt)
        {
            ProbeGround();
            bool isGrounded = ground.IsValid || characterController.isGrounded;
            UpdatePlatformMotion(dt, isGrounded);
            UpdateExternalVelocity(dt);

            Vector3 displacement =
                platformDelta +
                externalVelocity * dt;

            characterController.Move(displacement);

            StorePlatformPose();
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

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit == null || hit.collider == null)
                return;

            TryApplyContactPush(hit);

            if (Time.time < nextCushionImpactTime)
                return;

            CushionSurfaceMB surface = hit.collider.GetComponentInParent<CushionSurfaceMB>();

            if (surface == null)
                return;

            CushionImpactData impactData = new CushionImpactData(
                gameObject,
                transform,
                entityMB,
                CushionImpactTag,
                null,
                characterController,
                hit.point,
                hit.normal,
                CurrentVelocity,
                CurrentVelocity.magnitude);

            if (!surface.TryEvaluate(impactData, out CushionImpactResult result))
                return;

            if (HandleCushionImpact(impactData, result))
                nextCushionImpactTime = Time.time + cushionImpactCooldown;
        }

        private void TryApplyContactPush(ControllerColliderHit hit)
        {
            if (!pushRigidbodiesOnContact || IsReceiveBombImpact || motionLocked)
                return;

            if (hit.moveDirection.y < -0.3f || hit.collider.transform.IsChildOf(transform))
                return;

            Vector3 pushDirection = ResolveContactPushDirection(hit);

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
                hit.collider,
                hit.point,
                pushDirection,
                pushImpulse);

            EntityImpactResponseMB impactResponse = hit.collider.GetComponentInParent<EntityImpactResponseMB>();

            if (impactResponse != null)
            {
                impactResponse.TryApplyImpact(impactData);
                return;
            }

            Rigidbody hitRigidbody = hit.collider.attachedRigidbody;

            if (hitRigidbody == null || hitRigidbody.isKinematic)
                return;

            hitRigidbody.AddForceAtPosition(pushDirection * pushImpulse, hit.point, ForceMode.Impulse);
        }

        private Vector3 ResolveContactPushDirection(ControllerColliderHit hit)
        {
            Vector3 direction = hit.moveDirection;
            direction.y = 0.0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = CurrentVelocity;
                direction.y = 0.0f;
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = hit.collider.transform.position - transform.position;
                direction.y = 0.0f;
            }

            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        }

        private void ApplyCushionStop()
        {
            // CharacterController は Rigidbody ではないため、速度層を明示的に消して停止させる。
            planarVelocity = Vector3.zero;
            externalVelocity = Vector3.zero;
            verticalVelocity = groundedStickVelocity;
        }

        private void ApplyCushionBounce(Vector3 bounceVelocity)
        {
            planarVelocity = Vector3.zero;
            verticalVelocity = groundedStickVelocity;
            externalVelocity = bounceVelocity;
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

            StateMachine.ChangeState(EntityMoveState.Idle);

            if (IsRuntimeReady && SceneKernel.ValueStore != null)
            {
                SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsDead, false);
                SceneKernel.ValueStore.RemoveBoolModifier(Entity, ValueKeys.Move.CanMove, DeadMoveLockTag);
            }

            PublishRuntimeValues();
        }
        public bool IsGrounded => ground.IsValid || characterController != null && characterController.isGrounded;

        public bool IsSprinting =>
            sprintHeld &&
            MoveState == EntityMoveState.Moving &&
            CurrentPlanarSpeed > 0.15f;

        public bool IsDead => IsReceiveBombImpact; // 今は爆弾の衝撃を受けたら死ぬことにする

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

        private readonly struct GroundInfo
        {
            public readonly bool IsValid;
            public readonly Vector3 Normal;
            public readonly Vector3 Point;
            public readonly Transform Transform;

            public GroundInfo(
                bool isValid,
                Vector3 normal,
                Vector3 point,
                Transform transform)
            {
                IsValid = isValid;
                Normal = normal;
                Point = point;
                Transform = transform;
            }
        }

    }
}

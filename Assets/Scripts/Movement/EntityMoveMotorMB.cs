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
        [Tooltip("この移動モーターが直接制御する Rigidbody です。")]
        [SerializeField] private Rigidbody bodyRigidbody;
        [Tooltip("地面判定とカプセル形状の基準にする CapsuleCollider です。")]
        [SerializeField] private CapsuleCollider bodyCollider;

        [Header("Speed")]
        [Tooltip("移動速度のデフォルト値。ValueStore に値が無いときの fallback です。")]
        [SerializeField] private float fallbackMoveSpeed = 5.0f;
        [Tooltip("ダッシュ倍率のデフォルト値。ValueStore に値が無いときの fallback です。")]
        [SerializeField] private float fallbackSprintMultiplier = 1.5f;

        [Header("Acceleration")]
        [Tooltip("接地時の前進加速です。")]
        [SerializeField] private float groundAcceleration = 35.0f;
        [Tooltip("接地時の減速です。")]
        [SerializeField] private float groundDeceleration = 45.0f;
        [Tooltip("接地時に逆方向へ向きを変えるときの加速です。")]
        [SerializeField] private float groundTurnAcceleration = 55.0f;
        [Tooltip("空中での移動加速です。")]
        [SerializeField] private float airAcceleration = 12.0f;
        [Tooltip("空中で入力が無いときの減速です。")]
        [SerializeField] private float airDeceleration = 4.0f;
        [Tooltip("空中での水平速度上限です。")]
        [SerializeField] private float maxAirHorizontalSpeed = 8.0f;

        [Header("Jump / Gravity")]
        [Tooltip("基本ジャンプの高さです。")]
        [SerializeField] private float jumpHeight = 1.4f;
        [Tooltip("重力加速度です。負値で下方向に働きます。")]
        [SerializeField] private float gravity = -28.0f;
        [Tooltip("接地時に少し下方向へ押し付ける速度です。")]
        [SerializeField] private float groundedStickVelocity = -3.0f;
        [Tooltip("接地判定が切れてから猶予としてジャンプや段差処理を許す時間です。")]
        [SerializeField] private float coyoteTime = 0.12f;
        [Tooltip("ジャンプ入力を少し早押ししても拾うためのバッファ時間です。")]
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Ground Probe")]
        [Tooltip("地面判定に使うレイヤーマスクです。")]
        [SerializeField] private LayerMask groundMask = ~0;
        [Tooltip("地面判定を少しだけ深く取るための追加距離です。")]
        [SerializeField] private float groundProbeExtraDistance = 0.18f;
        [Tooltip("地面判定カプセルの半径から差し引く縮小量です。")]
        [SerializeField] private float groundProbeRadiusShrink = 0.03f;
        [Tooltip("地面として認識する最大角度です。")]
        [SerializeField] private float maxGroundAngle = 55.0f;

        [Header("Step Assist")]
        [Tooltip("段差を登る補助処理を有効にするかを指定します。")]
        [SerializeField] private bool enableStepAssist = true;
        [Tooltip("登れる最大の段差高さです。")]
        [SerializeField, Min(0.05f)] private float maxStepHeight = 0.3f;
        [Tooltip("段差判定で前方に見る距離です。")]
        [SerializeField, Min(0.02f)] private float stepAssistForwardDistance = 0.16f;
        [Tooltip("段差補助を始める最小水平速度です。")]
        [SerializeField, Min(0.01f)] private float minStepAssistSpeed = 0.1f;

        [Header("Moving Platform")]
        [Tooltip("移動足場の速度をジャンプ後に継承するかを指定します。")]
        [SerializeField] private bool inheritMovingPlatformVelocityOnJump = true;
        [Tooltip("足場速度をジャンプにどの程度反映するかの倍率です。")]
        [SerializeField] private float platformJumpVelocityInheritance = 1.0f;
        [Tooltip("足場上のサンプル点を平滑化する強さです。0 で無効になります。")]
        [SerializeField, Min(0.0f)] private float platformSupportSampleSmoothing = 0.0f;
        [Tooltip("足場回転由来の微小な追従を無視する角度のしきい値です。")]
        [SerializeField, Min(0.0f)] private float platformCarryRotationDeadZoneDegrees = 0.05f;

        [Header("External Momentum")]
        [Tooltip("外力や一時速度の減衰速度です。")]
        [SerializeField] private float externalVelocityDamping = 6.0f;
        [Tooltip("この値未満の外部速度は 0 扱いにします。")]
        [SerializeField] private float minExternalVelocity = 0.03f;

        [Header("Cushion")]
        [Tooltip("クッション接触の連続反応を抑えるクールダウンです。")]
        [SerializeField, Min(0.0f)] private float cushionImpactCooldown = 0.12f;

        [Header("Contact Push")]
        [Tooltip("接触した相手を押し返す処理を有効にするかを指定します。")]
        [SerializeField] private bool pushRigidbodiesOnContact = true;
        [Tooltip("接触押し返しの固定インパルスです。")]
        [SerializeField, Min(0.0f)] private float contactPushImpulse = 0.35f;
        [Tooltip("接触時の移動速度に応じて追加される押し返し係数です。")]
        [SerializeField, Min(0.0f)] private float contactPushSpeedMultiplier = 0.2f;
        [Tooltip("接触押し返しの最大インパルスです。")]
        [SerializeField, Min(0.0f)] private float maxContactPushImpulse = 2.0f;
        [Tooltip("この速度未満では押し返しを行いません。")]
        [SerializeField, Min(0.0f)] private float minContactPushSpeed = 0.1f;

        [Header("Runtime Debug")]
        [Tooltip("現在の Move.CanMoveByInput をデバッグ表示する値です。")]
        [SerializeField] private bool currentCanMoveByInput;
        [Tooltip("現在の Move.CanMoveBySystem をデバッグ表示する値です。")]
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
        /// <summary>接地している足場の移動速度を返します。</summary>
        public Vector3 PlatformVelocity => platformVelocity;

        /// <summary>このモーターが最終的に身体へ与えている速度を返します。</summary>
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

        // 依存コンポーネントの存在を保証する。
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

        // 再有効化時に Rigidbody / Collider を再取得する。
        private void OnEnable()
        {
            EnsureMovementBody();
        }

        // 無効化時に自動移動と入力状態を解除する。
        private void OnDisable()
        {
            CancelAutoMove();
            ClearMoveIntent();
        }

        // 毎物理 tick の中心処理。ここで入力、接地、足場、速度をまとめて更新する。
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

        // 外部入力から移動意図を受け取り、ジャンプバッファもここで保持する。
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

        // 入力の残りを消して、次の tick に影響しないようにする。
        public void ClearMoveIntent()
        {
            moveDirection = Vector3.zero;
            sprintHeld = false;
            jumpHeld = false;
            jumpBufferCounter = 0.0f;
            ClearPendingCushionHighJump();
        }

        // 通常時の移動更新。接地、足場、水平移動、垂直移動を順に処理する。
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

        // 水平方向の速度を、入力と接地状態に応じて滑らかに更新する。
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

        // ジャンプ、重力、接地時の貼り付き速度を更新する。
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

        // 受けた外力を徐々に減衰させる。
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

        // キャラクター下方へ SphereCast を飛ばして接地候補を探す。
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

        // カプセル Collider を地面判定用の検索形状へ変換する。
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

        // 足場の移動量を算出し、キャラへ渡すべきプラットフォーム速度を更新する。
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

        // 足場のどの位置を追従基準として使うかを決める。
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

        // 足場ローカル座標で追従点を保存し、必要なら平滑化する。
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

        // 足場から離れた瞬間の運動量を、ジャンプ用の継承速度として保存する。
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

        // ジャンプ開始時に、その場の足場速度をキャラ速度へ足し込む。
        private void InheritCurrentPlatformVelocity()
        {
            if (!inheritMovingPlatformVelocityOnJump || inheritedPlatformVelocity.sqrMagnitude > 0.0001f)
                return;

            inheritedPlatformVelocity = platformVelocity * platformJumpVelocityInheritance;

            // このtickは継承済みなので、追加の足場速度注入を抑止して二重加算を防ぐ。
            suppressPlatformVelocityInjectionThisTick = true;
        }

        // 着地時に、継承済みの足場速度を現在の足場速度へ合わせ直す。
        private void RebaseInheritedPlatformMomentumOnLanding()
        {
            if (inheritedPlatformVelocity.sqrMagnitude <= 0.0001f)
                return;

            Vector3 inheritedPlanarVelocity = Vector3.ProjectOnPlane(inheritedPlatformVelocity, Vector3.up);
            Vector3 landingSupportVelocity = Vector3.ProjectOnPlane(platformVelocity, Vector3.up);
            planarVelocity += inheritedPlanarVelocity - landingSupportVelocity;
            inheritedPlatformVelocity = Vector3.zero;
        }

        // 次回 tick の差分計算に使うため、足場の現在姿勢を保存する。
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

        // 現在速度と接地状態から、見た目上の移動状態を更新する。
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

        // motion lock 中でも、足場追従や外力の反映だけは維持する。
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

        // system movement lock 中は入力と通常移動を止め、状態だけ整える。
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

        // 計算済みの速度を Rigidbody に反映する。必要なら足場速度を重ねる。
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
                case CushionResponseKind.Dampen:
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

        // 衝突面から、壁滑り・接地貼り付き・天井頭打ちを処理する。
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

        // 壁へ入り込む速度成分を取り除く。
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

        // 小さな段差を越えられるか試す。地形の引っかかりを減らすための補助処理。
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

        // 自動移動の完了を await できるようにする。到達判定は水平距離で行う。
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

        // 外部から開始された自動移動を中断する。
        public void CancelAutoMove()
        {
            if (activeAutoMoveCancellationTokenSource != null)
                activeAutoMoveCancellationTokenSource.Cancel();

            autoMoveActive = false;
            autoMoveReachedTarget = false;
        }

        // 自動移動を始めても安全かを確認する。
        private bool CanStartAutoMove()
        {
            return IsRuntimeReady &&
                   bodyRigidbody != null &&
                   bodyCollider != null &&
                   !motionLocked &&
                   !isDead &&
                   CanApplySystemMovement();
        }

        // すでに目標地点へ十分近いなら、移動処理を始めない。
        private bool IsAlreadyAtAutoMoveTarget(Vector3 targetPosition, float arriveDistance)
        {
            if (bodyRigidbody == null)
                return false;

            Vector3 toTarget = targetPosition - bodyRigidbody.position;
            toTarget.y = 0.0f;

            return toTarget.sqrMagnitude <= Mathf.Max(0.0001f, arriveDistance * arriveDistance);
        }

        // 通常入力と自動移動のどちらを使うかをまとめて返す。
        private Vector3 GetDesiredMoveDirection()
        {
            if (autoMoveActive)
                return BuildAutoMoveDirection();

            return moveDirection;
        }

        // 自動移動先へ向かう正規化済みベクトルを作る。
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

        // 既存の自動移動を止めて、新しいキャンセル元を作る。
        private CancellationTokenSource BeginNewAutoMove()
        {
            if (activeAutoMoveCancellationTokenSource != null)
                activeAutoMoveCancellationTokenSource.Cancel();

            activeAutoMoveCancellationTokenSource = new CancellationTokenSource();
            return activeAutoMoveCancellationTokenSource;
        }

        // 自動移動用のキャンセルソースを安全に破棄する。
        private void CompleteAutoMove(CancellationTokenSource autoMoveCancellationTokenSource)
        {
            if (ReferenceEquals(activeAutoMoveCancellationTokenSource, autoMoveCancellationTokenSource))
                activeAutoMoveCancellationTokenSource = null;

            autoMoveCancellationTokenSource.Dispose();
        }

        // 目標到達時に自動移動を終了する。
        private void CompleteAutoMoveTarget()
        {
            autoMoveActive = false;
        }

        // 移動先に他のコライダーが重ならないかを確認する。
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

        // 段差を登った後に着地できる地面を再探索する。
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

        // Body の位置から、各種カプセル判定に使う幾何形状を作る。
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

        // 接触した相手へ、進行方向ベースの押し返しを与える。
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

        // 押し返しに使う方向を、現在速度や接触法線から決める。
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

        // クッションで完全停止するときの速度リセットを行う。
        private void ApplyCushionStop()
        {
            ClearPendingCushionHighJump();
            planarVelocity = Vector3.zero;
            externalVelocity = Vector3.zero;
            inheritedPlatformVelocity = Vector3.zero;
            verticalVelocity = groundedStickVelocity;
            ApplyCurrentVelocityToBody(GetBodyVelocity());
        }

        // クッションで跳ねるときの反応を適用する。
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

        // バッファされた高跳び入力があれば、着地反応を強化して消費する。
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

        // 通常のバウンス速度を、高跳び入力に応じて増幅できるか判定する。
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

        // 高跳び入力の猶予時間を設定し、次の入力を待てるようにする。
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

        // 高跳び待機状態を初期化する。
        private void ClearPendingCushionHighJump()
        {
            pendingCushionHighJumpExpireTime = -999.0f;
            pendingCushionBounceDirection = Vector3.zero;
            pendingCushionBounceSpeed = 0f;
            pendingCushionBounceSpeedLimit = 0f;
            pendingCushionHighJumpMultiplier = 1.0f;
        }

        // 高跳びに使えるジャンプ入力が残っているか確認する。
        private bool HasBufferedHighJumpInput()
        {
            return jumpHeld || jumpBufferCounter > 0.0f;
        }

        // 高跳び用の入力を消費し、ジャンプ遷移へ進める。
        private void ConsumeHighJumpInput()
        {
            jumpBufferCounter = 0.0f;
            lastGroundedTime = -999.0f;
            ClearPendingCushionHighJump();
            StateMachine.ChangeState(EntityMoveState.Jumping);
        }

        // クッション衝撃判定に使うタグを解決する。
        private EntityTagId ResolveCushionImpactTag()
        {
            if (entityMB != null && entityMB.Tag.IsValid)
                return entityMB.Tag;

            return default;
        }

        // 平面速度だけを直接設定する。
        public void SetPlanarVelocity(Vector3 velocity)
        {
            velocity.y = 0.0f;
            planarVelocity = velocity;
        }

        // 垂直速度だけを直接設定する。
        public void SetVerticalVelocity(float velocity)
        {
            verticalVelocity = velocity;
        }

        // 移動の主処理を止めて、演出や拘束状態へ切り替える。
        public void EnterMotionLock(EntityMoveState lockedState)
        {
            if (motionLocked)
                return;

            preservedVelocityWhenLocked = CurrentVelocity;
            motionLocked = true;
            StateMachine.ChangeState(lockedState);
        }

        // motion lock を解除し、保持していた速度を一部戻す。
        public void ExitMotionLock(Vector3 releaseImpulse, float preservedVelocityRate = 0.35f)
        {
            if (!motionLocked)
                return;

            motionLocked = false;

            externalVelocity += preservedVelocityWhenLocked * preservedVelocityRate;
            externalVelocity += releaseImpulse;

            preservedVelocityWhenLocked = Vector3.zero;
        }

        // 死亡状態へ入り、入力とシステム移動の両方を止める。
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

        // リトライ後に死亡状態や拘束を解除して復帰する。
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

        // ValueStore 側の System 移動可否へタグ付きの上書きを掛ける。
        public void SetSystemMovementModifier(ValueModifierTagId tag, bool canMove)
        {
            if (IsRuntimeReady && SceneKernel.ValueStore != null)
                SceneKernel.ValueStore.SetBoolModifier(Entity, ValueKeys.Move.CanMoveBySystem, tag, canMove);
        }

        // System 移動可否のタグ上書きを外す。
        public void RemoveSystemMovementModifier(ValueModifierTagId tag)
        {
            if (IsRuntimeReady && SceneKernel.ValueStore != null)
                SceneKernel.ValueStore.RemoveBoolModifier(Entity, ValueKeys.Move.CanMoveBySystem, tag);
        }

        // Inspector デバッグ用の値を現在状態へ同期する。
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

        // ValueStore のゲート状態をデバッグ表示へ反映する。
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

        // Rigidbody / Collider / 古い CharacterController を一度だけ解決する。
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

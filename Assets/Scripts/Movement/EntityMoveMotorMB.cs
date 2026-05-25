using System;
using System.Collections.Generic;
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
        [Tooltip("任意の足元補助コライダーです。設定する場合は trigger を必須にします。")]
        [SerializeField] private Collider footCollider;

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

        [Header("Ground Snap")]
        [Tooltip("地面吸着の設定です。")]
        [SerializeField] private GroundSnapSettings groundSnapSettings = new GroundSnapSettings();

        [Header("M3 Step Assist")]
        [Tooltip("段差補助ソルバの設定です。")]
        [SerializeField] private StepAssistSettings stepAssistSettings = new StepAssistSettings();

        [Header("Support Inertia")]
        [Tooltip("移動足場の慣性・launch 設定です。")]
        [SerializeField] private SupportInertiaSettings supportInertiaSettings = new SupportInertiaSettings();

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

        [Header("M4 Contact Classification")]
        [Tooltip("頭上接触を ceiling と判定する高さ帯です。")]
        [SerializeField, Min(0.0f)] private float contactCeilingBand = 0.08f;

        [Header("Runtime Debug")]
        [Tooltip("現在の Move.CanMoveByInput をデバッグ表示する値です。")]
        [SerializeField] private bool currentCanMoveByInput;
        [Tooltip("現在の Move.CanMoveBySystem をデバッグ表示する値です。")]
        [SerializeField] private bool currentCanMoveBySystem;

        private const int MaxGroundHits = 8;
        private const float AutoMoveStopSpeedSqr = 0.01f;

        private readonly RaycastHit[] groundHits = new RaycastHit[MaxGroundHits];
        private readonly Collider[] stepOverlapHits = new Collider[8];
        private readonly MoveContactBuffer moveContactBuffer = new MoveContactBuffer();
        private readonly HashSet<Collider> contactPushProcessedColliders = new HashSet<Collider>();
        private readonly HashSet<Collider> cushionProcessedColliders = new HashSet<Collider>();
        private readonly EntityMoveRuntimeState runtimeState = new EntityMoveRuntimeState();
        private readonly SupportMotionTracker supportMotionTracker = new SupportMotionTracker();
        private readonly CushionHighJumpBuffer cushionHighJumpBuffer = new CushionHighJumpBuffer();
        private EntityMoveIntent currentIntent = new EntityMoveIntent();
        private readonly AutoMoveState autoMoveState = new AutoMoveState();
        private readonly AutoMoveDriver autoMoveDriver = new AutoMoveDriver();

        private EntityMB entityMB;
        private PhysicsMaterial lowFrictionContactMaterial;
        private string lastColliderPolicyErrorMessage;

        private GroundInfo ground;
        private bool suppressPlatformVelocityInjectionThisTick;

        private Vector3 preservedVelocityWhenLocked;
        private float nextCushionImpactTime;
        private ValueModifierTagId? activeDeadMoveLockTag;

        public Vector3 PlanarVelocity => GetVelocityChannels().InputPlanar;
        public float VerticalVelocity => GetVelocityChannels().Vertical;
        public Vector3 ExternalVelocity => GetVelocityChannels().External;
        /// <summary>接地している足場の移動速度を返します。</summary>
        public Vector3 PlatformVelocity => GetVelocityChannels().SupportCarry;

        /// <summary>このモーターが最終的に身体へ与えている速度を返します。</summary>
        public Vector3 CurrentVelocity => VelocityComposer.ComposeFinalVelocity(GetVelocityChannels());
        public bool CanMoveByInput => currentCanMoveByInput;
        public bool CanMoveBySystem => currentCanMoveBySystem;
        public bool CanProcessMoveInput => CanReceiveMoveInput() && CanApplySystemMovement();
        public bool IsAutoMoveActive => autoMoveState.IsActive;
        public bool IsGrounded => ground.IsValid;
        public bool IsDead => runtimeState.IsDead;
        public event Action<CushionHighJumpEventData> CushionHighJumped;
        public Vector3 GroundNormal => ground.IsValid ? ground.Normal : Vector3.up;
        public Vector3 GroundPoint => ground.IsValid ? ground.Point : transform.position;
        public Transform GroundTransform => ground.IsValid ? ground.Transform : null;

        public Transform CushionImpactRoot => transform;
        public EntityTagId CushionImpactTag => ResolveCushionImpactTag();

        public bool IsSprinting =>
            currentIntent.SprintHeld &&
            MoveState == EntityMoveState.Moving &&
            CurrentPlanarSpeed > 0.15f;

        public Vector3 ControlledPlanarVelocity
        {
            get
            {
                return VelocityComposer.ControlledPlanarVelocity(GetVelocityChannels());
            }
        }

        public float CurrentPlanarSpeed => VelocityComposer.CurrentPlanarSpeed(GetVelocityChannels());

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
            SyncM3SettingsFromLegacyStepFields();
            EnsureMovementBody();
        }

        private void OnValidate()
        {
            SyncM3SettingsFromLegacyStepFields();
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
            moveContactBuffer.ClearForNextPhysicsTick();
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

            if (runtimeState.MotionLocked)
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
            if (autoMoveState.IsActive)
            {
                ClearMoveIntent();
                return;
            }

            bool canReceiveInput = CanProcessMoveInput;
            currentIntent.JumpPressed = jumpPressedThisFrame;

            if (canReceiveInput)
            {
                worldMoveDirection.y = 0.0f;
                currentIntent.WorldMoveDirection = Vector3.ClampMagnitude(worldMoveDirection, 1.0f);
                currentIntent.HasMoveInput = currentIntent.WorldMoveDirection.sqrMagnitude > 0.0001f;
                currentIntent.SprintHeld = wantsSprint;
                currentIntent.JumpHeld = jumpHeldInput;
                currentIntent.IsAutoMove = false;

                if (jumpPressedThisFrame)
                {
                    runtimeState.JumpBufferCounter = jumpBufferTime;
                    return;
                }
            }
            else
            {
                currentIntent.Clear();
            }

            runtimeState.JumpBufferCounter -= deltaTime;
        }

        // 入力の残りを消して、次の tick に影響しないようにする。
        public void ClearMoveIntent()
        {
            currentIntent.Clear();
            runtimeState.JumpBufferCounter = 0.0f;
            cushionHighJumpBuffer.Clear();
        }

        // 通常時の移動更新。接地、足場、水平移動、垂直移動を順に処理する。
        private void TickMotor(float dt)
        {
            suppressPlatformVelocityInjectionThisTick = false;
            SyncLegacyVelocityFieldsFromChannels();
            ProbeGround();
            SyncRuntimeGroundState();

            bool isGrounded = ground.IsValid;

            if (!isGrounded && runtimeState.WasGrounded)
                CapturePlatformInertiaOnLeaveGround();

            if (isGrounded)
                runtimeState.LastGroundedTime = Time.time;

            UpdatePlatformMotion(dt, isGrounded);
            SyncVelocityChannelsFromLegacyFields();

            // Support launch must be resolved before GroundSnap so launch tick can suppress snap.
            ResolveSupportInertiaLaunch();

            if (isGrounded && !runtimeState.WasGrounded)
                RebaseInheritedPlatformMomentumOnLanding();

            UpdatePlanarVelocity(dt, isGrounded);
            UpdateVerticalVelocity(dt, isGrounded);
            UpdateExternalVelocity(dt);

            ProcessBufferedContacts();
            SyncVelocityChannelsFromLegacyFields();

            Vector3 bodyVelocity = GetBodyVelocity();
            PositionCorrection totalCorrection = PositionCorrection.None;
            bool steppedUp = TryStepUp(dt, bodyVelocity, isGrounded, out PositionCorrection stepCorrection);

            if (stepCorrection.HasCorrection)
                totalCorrection = totalCorrection.Combine(stepCorrection);

            if (steppedUp && !isGrounded)
                RebaseInheritedPlatformMomentumOnLanding();

            // Step直後に同tickで下方向snapを重ねると過補正しやすいため、上方向step成立時はsnapを1tick抑止する。
            bool allowGroundSnap = !stepCorrection.HasCorrection || stepCorrection.Delta.y <= 0.0001f;
            PositionCorrection snapCorrection = allowGroundSnap
                ? GroundSnapSolver.Resolve(
                    groundSnapSettings,
                    runtimeState,
                    runtimeState.Ground,
                    ground.IsValid,
                    VerticalVelocity,
                    Time.time,
                    dt)
                : PositionCorrection.None;

            if (snapCorrection.HasCorrection)
                totalCorrection = totalCorrection.Combine(snapCorrection);

            ApplyPositionCorrection(totalCorrection);

            if (totalCorrection.HasCorrection)
            {
                ProbeGround();
                SyncRuntimeGroundState();
            }

            SyncRuntimeGroundState();
            bodyVelocity = GetBodyVelocity();

            ApplyCurrentVelocityToBody(bodyVelocity, isGrounded);

            StorePlatformPose();
            UpdateMoveState();
            runtimeState.WasGrounded = ground.IsValid;
            moveContactBuffer.ClearForNextPhysicsTick();
        }

        // 水平方向の速度を、入力と接地状態に応じて滑らかに更新する。
        private void UpdatePlanarVelocity(float dt, bool isGrounded)
        {
            VelocityChannels channels = runtimeState.Velocity;
            Vector3 desiredDirection = GetDesiredMoveDirection();
            bool hasMoveInput = currentIntent.HasMoveInput && desiredDirection.sqrMagnitude > 0.0001f;

            float moveSpeed = GetMoveBaseSpeed(fallbackMoveSpeed);

            if (currentIntent.SprintHeld)
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
                    float dot = channels.InputPlanar.sqrMagnitude > 0.001f
                        ? Vector3.Dot(channels.InputPlanar.normalized, desiredDirection)
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

            channels.InputPlanar = Vector3.MoveTowards(channels.InputPlanar, desiredVelocity, acceleration * dt);

            if (autoMoveState.IsActive && autoMoveState.ReachedTarget && channels.InputPlanar.sqrMagnitude <= AutoMoveStopSpeedSqr)
                CompleteAutoMoveTarget();

            if (!isGrounded)
            {
                Vector3 horizontal = new Vector3(channels.InputPlanar.x, 0.0f, channels.InputPlanar.z);

                if (horizontal.magnitude > maxAirHorizontalSpeed)
                {
                    horizontal = horizontal.normalized * maxAirHorizontalSpeed;
                    channels.InputPlanar = new Vector3(horizontal.x, channels.InputPlanar.y, horizontal.z);
                }
            }

            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();
        }

        // ジャンプ、重力、接地時の貼り付き速度を更新する。
        private void UpdateVerticalVelocity(float dt, bool isGrounded)
        {
            VelocityChannels channels = runtimeState.Velocity;
            CushionApplyResult pendingHighJumpResult = CushionImpactHandler.TryConsumeBufferedHighJump(
                runtimeState,
                cushionHighJumpBuffer,
                HasBufferedHighJumpInput(),
                groundedStickVelocity,
                Time.time);

            if (CommitCushionApplyResult(pendingHighJumpResult))
                return;

            bool canUseCoyote = Time.time - runtimeState.LastGroundedTime <= coyoteTime;
            bool wantsJump = currentIntent.JumpPressed || runtimeState.JumpBufferCounter > 0.0f;

            if (wantsJump && canUseCoyote)
            {
                float jumpHeightMultiplier = Mathf.Max(0.0f, GetJumpHeightMultiplier(1.0f));
                float effectiveJumpHeight = Mathf.Max(0.0f, jumpHeight * jumpHeightMultiplier);
                float jumpSpeed = Mathf.Sqrt(effectiveJumpHeight * -2.0f * gravity);
                channels.Vertical = jumpSpeed;
                runtimeState.Velocity = channels;
                SyncLegacyVelocityFieldsFromChannels();
                runtimeState.LastJumpTime = Time.time;

                InheritCurrentPlatformVelocity();

                currentIntent.JumpPressed = false;
                runtimeState.JumpBufferCounter = 0.0f;
                runtimeState.LastGroundedTime = -999.0f;
                StateMachine.ChangeState(EntityMoveState.Jumping);
                return;
            }

            currentIntent.JumpPressed = false;

            if (isGrounded && channels.Vertical < 0.0f)
            {
                channels.Vertical = groundedStickVelocity;
                runtimeState.Velocity = channels;
                SyncLegacyVelocityFieldsFromChannels();
                return;
            }

            channels.Vertical += gravity * dt;
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();
        }

        // 受けた外力を徐々に減衰させる。
        private void UpdateExternalVelocity(float dt)
        {
            VelocityChannels channels = runtimeState.Velocity;

            if (channels.External.sqrMagnitude <= minExternalVelocity * minExternalVelocity)
            {
                channels.External = Vector3.zero;
                runtimeState.Velocity = channels;
                SyncLegacyVelocityFieldsFromChannels();
                return;
            }

            channels.External = Vector3.Lerp(
                channels.External,
                Vector3.zero,
                1.0f - Mathf.Exp(-externalVelocityDamping * dt));
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();
        }

        // キャラクター下方へ SphereCast を飛ばして接地候補を探す。
        private void ProbeGround()
        {
            GroundHitInfo hit = GroundProbeSolver.Probe(
                transform,
                bodyCollider,
                groundMask,
                groundProbeExtraDistance,
                groundProbeRadiusShrink,
                maxGroundAngle,
                groundHits);

            ground = hit.IsValid
                ? new GroundInfo(true, hit.Normal, hit.Point, hit.Transform, hit.Collider, hit.Distance, hit.Angle, hit.SurfaceKind)
                : default;
        }

        // 足場の移動量を算出し、キャラへ渡すべきプラットフォーム速度を更新する。
        private void UpdatePlatformMotion(float dt, bool isGrounded)
        {
            supportMotionTracker.Update(
                transform,
                bodyRigidbody,
                runtimeState.Ground,
                isGrounded,
                runtimeState,
                dt,
                Time.time,
                runtimeState.SupportReattachDisabledUntilTime,
                platformSupportSampleSmoothing,
                platformCarryRotationDeadZoneDegrees);

            SyncVelocityChannelsFromLegacyFields();
        }

        // 足場から離れた瞬間の運動量を、ジャンプ用の継承速度として保存する。
        private void CapturePlatformInertiaOnLeaveGround()
        {
            VelocityChannels channels = runtimeState.Velocity;
            if (!inheritMovingPlatformVelocityOnJump ||
                channels.InheritedSupport.sqrMagnitude > 0.0001f ||
                channels.SupportCarry.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            channels.InheritedSupport = channels.SupportCarry * platformJumpVelocityInheritance;
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();
            runtimeState.LastSupportLaunchTime = Time.time;
        }

        // ジャンプ開始時に、その場の足場速度をキャラ速度へ足し込む。
        private void InheritCurrentPlatformVelocity()
        {
            VelocityChannels channels = runtimeState.Velocity;
            if (!inheritMovingPlatformVelocityOnJump || channels.InheritedSupport.sqrMagnitude > 0.0001f)
                return;

            channels.InheritedSupport = channels.SupportCarry * platformJumpVelocityInheritance;
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();
            runtimeState.LastSupportLaunchTime = Time.time;

            // このtickは継承済みなので、追加の足場速度注入を抑止して二重加算を防ぐ。
            suppressPlatformVelocityInjectionThisTick = true;
        }

        // 着地時に、継承済みの足場速度を現在の足場速度へ合わせ直す。
        private void RebaseInheritedPlatformMomentumOnLanding()
        {
            VelocityChannels channels = runtimeState.Velocity;
            if (channels.InheritedSupport.sqrMagnitude <= 0.0001f)
                return;

            Vector3 inheritedPlanarVelocity = Vector3.ProjectOnPlane(channels.InheritedSupport, Vector3.up);
            Vector3 landingSupportVelocity = Vector3.ProjectOnPlane(channels.SupportCarry, Vector3.up);
            channels.InputPlanar += inheritedPlanarVelocity - landingSupportVelocity;
            channels.InheritedSupport = Vector3.zero;
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();
        }

        // 次回 tick の差分計算に使うため、足場の現在姿勢を保存する。
        private void StorePlatformPose()
        {
            supportMotionTracker.StorePlatformPose();
        }

        // 現在速度と接地状態から、見た目上の移動状態を更新する。
        private void UpdateMoveState()
        {
            if (runtimeState.MotionLocked)
            {
                if (MoveState != EntityMoveState.Disabled && MoveState != EntityMoveState.Dead)
                    StateMachine.ChangeState(EntityMoveState.Disabled);
                return;
            }

            if (ground.IsValid)
            {
                Vector3 horizontal = ControlledPlanarVelocity;

                if (horizontal.magnitude > 0.1f)
                    StateMachine.ChangeState(EntityMoveState.Moving);
                else
                    StateMachine.ChangeState(EntityMoveState.Idle);

                return;
            }

            if (VerticalVelocity > 0.0f)
                StateMachine.ChangeState(EntityMoveState.Jumping);
            else
                StateMachine.ChangeState(EntityMoveState.Falling);
        }

        // motion lock 中でも、足場追従や外力の反映だけは維持する。
        private void TickLockedMotion(float dt)
        {
            suppressPlatformVelocityInjectionThisTick = false;
            SyncLegacyVelocityFieldsFromChannels();
            ProbeGround();
            SyncRuntimeGroundState();
            bool isGrounded = ground.IsValid;

            if (!isGrounded && runtimeState.WasGrounded)
                CapturePlatformInertiaOnLeaveGround();

            UpdatePlatformMotion(dt, isGrounded);
            ResolveSupportInertiaLaunch();

            if (isGrounded && !runtimeState.WasGrounded)
                RebaseInheritedPlatformMomentumOnLanding();

            UpdateExternalVelocity(dt);

            VelocityChannels channels = runtimeState.Velocity;
            ApplyCurrentVelocityToBody(channels.External + channels.InheritedSupport, isGrounded);
            StorePlatformPose();
            runtimeState.WasGrounded = ground.IsValid;
            runtimeState.IsGrounded = ground.IsValid;
            moveContactBuffer.ClearForNextPhysicsTick();
        }

        // system movement lock 中は入力と通常移動を止め、状態だけ整える。
        private void TickSystemMovementLocked(float dt)
        {
            suppressPlatformVelocityInjectionThisTick = false;
            CancelAutoMove();
            ClearMoveIntent();

            VelocityChannels channels = runtimeState.Velocity;
            channels.InputPlanar = Vector3.zero;
            channels.Vertical = 0.0f;
            channels.External = Vector3.zero;
            channels.InheritedSupport = Vector3.zero;
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();

            ProbeGround();
            SyncRuntimeGroundState();
            bool isGrounded = ground.IsValid;

            UpdatePlatformMotion(dt, isGrounded);
            ApplyCurrentVelocityToBody(Vector3.zero, isGrounded);
            StorePlatformPose();

            if (MoveState != EntityMoveState.Disabled && MoveState != EntityMoveState.Dead)
                StateMachine.ChangeState(EntityMoveState.Disabled);

            runtimeState.WasGrounded = ground.IsValid;
            runtimeState.IsGrounded = ground.IsValid;
            moveContactBuffer.ClearForNextPhysicsTick();
        }

        // 計算済みの速度を Rigidbody に反映する。必要なら足場速度を重ねる。
        private void ApplyCurrentVelocityToBody(Vector3 velocity, bool injectPlatformVelocity = false)
        {
            if (bodyRigidbody == null)
                return;

            Vector3 appliedVelocity = velocity;
            if (injectPlatformVelocity && !suppressPlatformVelocityInjectionThisTick)
                appliedVelocity += runtimeState.Velocity.SupportCarry;

            bodyRigidbody.linearVelocity = appliedVelocity;
        }

        private void ApplyPositionCorrection(in PositionCorrection correction)
        {
            if (!correction.HasCorrection || bodyRigidbody == null)
                return;

            bodyRigidbody.position += correction.Delta;
        }

        private Vector3 GetBodyVelocity()
        {
            return VelocityComposer.ComposeBodyVelocity(runtimeState.Velocity);
        }

        private VelocityChannels GetVelocityChannels()
        {
            return runtimeState.Velocity;
        }

        private void SyncVelocityChannelsFromLegacyFields()
        {
            VelocityChannels channels = runtimeState.Velocity;
            channels.InputPlanar = runtimeState.PlanarVelocity;
            channels.Vertical = runtimeState.VerticalVelocity;
            channels.External = runtimeState.ExternalVelocity;
            channels.SupportCarry = runtimeState.PlatformVelocity;
            channels.InheritedSupport = runtimeState.InheritedSupportVelocity;
            runtimeState.Velocity = channels;
        }

        private void SyncLegacyVelocityFieldsFromChannels()
        {
            VelocityChannels channels = runtimeState.Velocity;
            runtimeState.PlanarVelocity = channels.InputPlanar;
            runtimeState.VerticalVelocity = channels.Vertical;
            runtimeState.ExternalVelocity = channels.External;
            runtimeState.PlatformVelocity = channels.SupportCarry;
            runtimeState.InheritedSupportVelocity = channels.InheritedSupport;
        }

        public void AddImpulse(Vector3 impulseVelocity)
        {
            if (!CanApplySystemMovement())
                return;

            VelocityChannels channels = runtimeState.Velocity;
            channels.External += impulseVelocity;
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();
        }

        public void SetExternalVelocity(Vector3 velocity)
        {
            if (!CanApplySystemMovement())
                return;

            VelocityChannels channels = runtimeState.Velocity;
            channels.External = velocity;
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();
        }

        public void ClearExternalVelocity()
        {
            VelocityChannels channels = runtimeState.Velocity;
            channels.External = Vector3.zero;
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();
        }

        public bool HandleCushionImpact(CushionImpactData impactData, CushionImpactResult impactResult)
        {
            CushionApplyResult applyResult = CushionImpactHandler.ApplyImpact(
                runtimeState,
                impactResult,
                CanApplySystemMovement(),
                HasBufferedHighJumpInput(),
                cushionHighJumpBuffer,
                groundedStickVelocity,
                Time.time,
                coyoteTime);

            return CommitCushionApplyResult(applyResult);
        }

        private void OnCollisionEnter(Collision collision)
        {
            BufferCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            BufferCollision(collision);
        }

        // Callbackでは処理せず、接触情報だけを蓄積して FixedUpdate 側で順序処理する。
        private void BufferCollision(Collision collision)
        {
            if (collision == null || collision.collider == null)
                return;

            if (!CanApplySystemMovement())
                return;

            moveContactBuffer.Add(collision, BuildCurrentBodyGeometry());
        }

        // M4: buffered contacts を分類し、制約・押し返し・クッション反応を固定順で適用する。
        private void ProcessBufferedContacts()
        {
            if (moveContactBuffer.Count <= 0)
                return;

            MovementBodyGeometry bodyGeometry = BuildCurrentBodyGeometry();
            ContactClassifier.Classify(moveContactBuffer, in bodyGeometry, in runtimeState.Ground, maxGroundAngle, contactCeilingBand);

            CollisionConstraintSolver.Resolve(
                moveContactBuffer,
                runtimeState,
                groundedStickVelocity,
                Time.time,
                RemoveIntoWallVelocity);

            contactPushProcessedColliders.Clear();
            cushionProcessedColliders.Clear();

            for (int i = 0; i < moveContactBuffer.Count; i++)
            {
                MoveContactInfo contact = moveContactBuffer.Get(i);

                if (contact.Collider == null)
                    continue;

                if (contactPushProcessedColliders.Add(contact.Collider))
                {
                    ContactPushEmitter.TryApply(
                        contact,
                        transform,
                        CurrentVelocity,
                        pushRigidbodiesOnContact,
                        runtimeState.IsDead,
                        runtimeState.MotionLocked,
                        minContactPushSpeed,
                        contactPushImpulse,
                        contactPushSpeedMultiplier,
                        maxContactPushImpulse);
                }

                if (cushionProcessedColliders.Add(contact.Collider))
                {
                    Vector3 incomingVelocity = bodyRigidbody != null
                        ? bodyRigidbody.linearVelocity
                        : CurrentVelocity;

                    if (!CushionImpactHandler.TryProcessContact(
                            contact,
                            Time.time,
                            cushionImpactCooldown,
                            ref nextCushionImpactTime,
                            gameObject,
                            transform,
                            entityMB,
                            CushionImpactTag,
                            bodyRigidbody,
                            bodyCollider,
                            incomingVelocity,
                            runtimeState,
                            CanApplySystemMovement(),
                            HasBufferedHighJumpInput(),
                            cushionHighJumpBuffer,
                            groundedStickVelocity,
                            coyoteTime,
                            out CushionApplyResult applyResult))
                    {
                        continue;
                    }

                    CommitCushionApplyResult(applyResult);
                }
            }
        }

        // 壁へ入り込む速度成分を取り除く。
        private void RemoveIntoWallVelocity(Vector3 surfaceNormal)
        {
            Vector3 wallNormal = Vector3.ProjectOnPlane(surfaceNormal, Vector3.up);

            if (wallNormal.sqrMagnitude <= 0.0001f)
                return;

            wallNormal.Normalize();
            VelocityChannels channels = runtimeState.Velocity;
            channels.InputPlanar = RemoveIntoSurfaceComponent(channels.InputPlanar, wallNormal);
            channels.External = RemoveIntoSurfaceComponent(channels.External, wallNormal);
            channels.InheritedSupport = RemoveIntoSurfaceComponent(channels.InheritedSupport, wallNormal);
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();
        }

        private static Vector3 RemoveIntoSurfaceComponent(Vector3 velocity, Vector3 surfaceNormal)
        {
            float intoSurfaceSpeed = Vector3.Dot(velocity, -surfaceNormal);

            if (intoSurfaceSpeed <= 0.0f)
                return velocity;

            return velocity + surfaceNormal * intoSurfaceSpeed;
        }

        // 小さな段差を越えられるか試す。地形の引っかかりを減らすための補助処理。
        private bool TryStepUp(float dt, Vector3 bodyVelocity, bool isGrounded, out PositionCorrection correction)
        {
            correction = PositionCorrection.None;
            SyncLegacyVelocityFieldsFromChannels();

            bool stepped = StepAssistSolver.TryResolve(
                stepAssistSettings,
                transform,
                bodyRigidbody,
                bodyCollider,
                groundMask,
                maxGroundAngle,
                groundProbeExtraDistance,
                isGrounded,
                runtimeState.LastGroundedTime,
                coyoteTime,
                VerticalVelocity,
                runtimeState.MotionLocked,
                runtimeState.IsDead,
                GetDesiredMoveDirection(),
                bodyVelocity,
                dt,
                stepOverlapHits,
                out correction,
                out GroundHitInfo stepGround);

            if (!stepped)
                return false;

            ground = new GroundInfo(
                stepGround.IsValid,
                stepGround.Normal,
                stepGround.Point,
                stepGround.Transform,
                stepGround.Collider,
                stepGround.Distance,
                stepGround.Angle,
                stepGround.SurfaceKind);
            runtimeState.LastGroundedTime = Time.time;

            if (VerticalVelocity < groundedStickVelocity)
            {
                VelocityChannels channels = runtimeState.Velocity;
                channels.Vertical = groundedStickVelocity;
                runtimeState.Velocity = channels;
                SyncLegacyVelocityFieldsFromChannels();
            }

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
                autoMoveDriver.BeginMove(autoMoveState, targetPosition, arriveDistance);

                await UniTask.WaitUntil(() => !autoMoveState.IsActive, PlayerLoopTiming.Update, linkedCancellationTokenSource.Token);
                reachedTarget = autoMoveState.ReachedTarget;
            }
            catch (OperationCanceledException) when (linkedCancellationTokenSource.IsCancellationRequested)
            {
                return false;
            }
            finally
            {
                if (ReferenceEquals(autoMoveState.ActiveCancellationTokenSource, autoMoveCancellationTokenSource))
                    autoMoveDriver.Cancel(autoMoveState);

                autoMoveDriver.CompleteAndDispose(autoMoveState, autoMoveCancellationTokenSource);
                linkedCancellationTokenSource.Dispose();
            }

            return reachedTarget;
        }

        // 外部から開始された自動移動を中断する。
        public void CancelAutoMove()
        {
            autoMoveDriver.Cancel(autoMoveState);
        }

        // 自動移動を始めても安全かを確認する。
        private bool CanStartAutoMove()
        {
            return autoMoveDriver.CanStart(
                IsRuntimeReady,
                bodyRigidbody,
                bodyCollider,
                runtimeState.MotionLocked,
                runtimeState.IsDead,
                CanApplySystemMovement());
        }

        // すでに目標地点へ十分近いなら、移動処理を始めない。
        private bool IsAlreadyAtAutoMoveTarget(Vector3 targetPosition, float arriveDistance)
        {
            return autoMoveDriver.IsAlreadyAtTarget(bodyRigidbody, targetPosition, arriveDistance);
        }

        // 通常入力と自動移動のどちらを使うかをまとめて返す。
        private Vector3 GetDesiredMoveDirection()
        {
            if (autoMoveState.IsActive)
                return BuildAutoMoveDirection();

            currentIntent.IsAutoMove = false;
            return currentIntent.HasMoveInput ? currentIntent.WorldMoveDirection : Vector3.zero;
        }

        // 自動移動先へ向かう正規化済みベクトルを作る。
        private Vector3 BuildAutoMoveDirection()
        {
            currentIntent.IsAutoMove = true;
            Vector3 direction = autoMoveDriver.BuildDirection(autoMoveState, bodyRigidbody);
            currentIntent.WorldMoveDirection = direction;
            currentIntent.HasMoveInput = direction.sqrMagnitude > 0.0001f;
            return direction;
        }

        // 既存の自動移動を止めて、新しいキャンセル元を作る。
        private CancellationTokenSource BeginNewAutoMove()
        {
            return autoMoveDriver.BeginNew(autoMoveState);
        }

        // 目標到達時に自動移動を終了する。
        private void CompleteAutoMoveTarget()
        {
            autoMoveDriver.CompleteTarget(autoMoveState);
        }

        private MovementBodyGeometry BuildCurrentBodyGeometry()
        {
            Vector3 bodyPosition = bodyRigidbody != null ? bodyRigidbody.position : transform.position;
            return MovementBodyGeometryUtility.Build(transform, bodyCollider, bodyPosition);
        }

        private bool CommitCushionApplyResult(in CushionApplyResult applyResult)
        {
            if (!applyResult.Handled)
                return false;

            if (applyResult.ShouldConsumeHighJumpInput)
                ConsumeHighJumpInput();

            SyncLegacyVelocityFieldsFromChannels();
            ApplyCurrentVelocityToBody(GetBodyVelocity());

            if (applyResult.ShouldRaiseHighJumpEvent)
                CushionHighJumped?.Invoke(new CushionHighJumpEventData(applyResult.HighJumpEventVelocity));

            return true;
        }

        // 高跳びに使えるジャンプ入力が残っているか確認する。
        private bool HasBufferedHighJumpInput()
        {
            return currentIntent.JumpHeld || runtimeState.JumpBufferCounter > 0.0f;
        }

        // 高跳び用の入力を消費し、ジャンプ遷移へ進める。
        private void ConsumeHighJumpInput()
        {
            currentIntent.JumpPressed = false;
            runtimeState.JumpBufferCounter = 0.0f;
            runtimeState.LastGroundedTime = -999.0f;
            cushionHighJumpBuffer.Clear();
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
            VelocityChannels channels = runtimeState.Velocity;
            channels.InputPlanar = velocity;
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();
        }

        // 垂直速度だけを直接設定する。
        public void SetVerticalVelocity(float velocity)
        {
            VelocityChannels channels = runtimeState.Velocity;
            channels.Vertical = velocity;
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();
        }

        // 移動の主処理を止めて、演出や拘束状態へ切り替える。
        public void EnterMotionLock(EntityMoveState lockedState)
        {
            if (runtimeState.MotionLocked)
                return;

            preservedVelocityWhenLocked = CurrentVelocity;
            runtimeState.MotionLocked = true;
            StateMachine.ChangeState(lockedState);
        }

        // motion lock を解除し、保持していた速度を一部戻す。
        public void ExitMotionLock(Vector3 releaseImpulse, float preservedVelocityRate = 0.35f)
        {
            if (!runtimeState.MotionLocked)
                return;

            runtimeState.MotionLocked = false;

            VelocityChannels channels = runtimeState.Velocity;
            channels.External += preservedVelocityWhenLocked * preservedVelocityRate;
            channels.External += releaseImpulse;
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();

            preservedVelocityWhenLocked = Vector3.zero;
        }

        // 死亡状態へ入り、入力とシステム移動の両方を止める。
        public void EnterDeadState(ValueModifierTagId moveLockTag)
        {
            runtimeState.IsDead = true;
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
            runtimeState.IsDead = false;
            runtimeState.MotionLocked = false;
            preservedVelocityWhenLocked = Vector3.zero;

            VelocityChannels channels = runtimeState.Velocity;
            channels.InputPlanar = Vector3.zero;
            channels.Vertical = groundedStickVelocity;
            channels.External = Vector3.zero;
            channels.InheritedSupport = Vector3.zero;
            runtimeState.Velocity = channels;
            SyncLegacyVelocityFieldsFromChannels();

            ApplyCurrentVelocityToBody(Vector3.up * VerticalVelocity);
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
            runtimeState.MoveState = MoveState;

            if (!IsRuntimeReady || SceneKernel.ValueStore == null)
                return;

            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.MoveState, MoveState);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.CurrentPlanarSpeed, CurrentPlanarSpeed);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.VerticalVelocity, runtimeState.VerticalVelocity);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsGrounded, IsGrounded);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsSprinting, IsSprinting);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.IsDead, IsDead);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.CanMoveByInput, currentCanMoveByInput);
            SceneKernel.ValueStore.Set(Entity, ValueKeys.Runtime.CanMoveBySystem, currentCanMoveBySystem);
        }

        private void SyncRuntimeGroundState()
        {
            runtimeState.IsGrounded = ground.IsValid;
            runtimeState.Ground = ground.IsValid
                ? new GroundHitInfo(
                    true,
                    ground.Collider,
                    ground.Transform,
                    ground.Point,
                    ground.Normal,
                    ground.Distance,
                    ground.Angle,
                    ground.SurfaceKind,
                    ground.SurfaceKind == GroundSurfaceKind.Walkable)
                : default;
        }

        private void ResolveSupportInertiaLaunch()
        {
            SyncLegacyVelocityFieldsFromChannels();
            if (!SupportInertiaSolver.TryResolveLaunch(
                    supportInertiaSettings,
                    runtimeState,
                    HasBufferedHighJumpInput(),
                    Time.time,
                    out float _))
            {
                return;
            }

            SyncVelocityChannelsFromLegacyFields();

            StateMachine.ChangeState(EntityMoveState.Jumping);
        }

        private void SyncM3SettingsFromLegacyStepFields()
        {
            if (stepAssistSettings == null)
                stepAssistSettings = new StepAssistSettings();

            if (groundSnapSettings == null)
                groundSnapSettings = new GroundSnapSettings();

            if (supportInertiaSettings == null)
                supportInertiaSettings = new SupportInertiaSettings();

            stepAssistSettings.Enabled = enableStepAssist;
            stepAssistSettings.MaxStepHeight = maxStepHeight;
            stepAssistSettings.ForwardProbeDistance = stepAssistForwardDistance;
            stepAssistSettings.MinIntentMagnitude = minStepAssistSpeed;
            stepAssistSettings.StepDownProbeDistance = Mathf.Max(stepAssistSettings.StepDownProbeDistance, maxStepHeight + groundProbeExtraDistance);
        }

        // ValueStore のゲート状態をデバッグ表示へ反映する。
        private void RefreshMoveGateDebugValues()
        {
            currentCanMoveByInput = CanReceiveMoveInput();
            currentCanMoveBySystem = CanApplySystemMovement();
        }

        private void OnDestroy()
        {
            MovementPhysicsMaterialFactory.Release(ref lowFrictionContactMaterial);
        }

        // Rigidbody / Collider / 古い CharacterController を一度だけ解決する。
        private void EnsureMovementBody()
        {
            MovementBodyResolver.ResolveAndConfigure(
                gameObject,
                ref bodyRigidbody,
                ref bodyCollider,
                out _);

            MovementPhysicsMaterialFactory.EnsureLowFrictionMaterial(bodyCollider, ref lowFrictionContactMaterial);
            ValidateMovementColliderPolicy();
        }

        private void ValidateMovementColliderPolicy()
        {
            if (bodyCollider == null)
                return;

            if (MovementColliderPolicyValidator.TryValidate(transform, bodyCollider, footCollider, out string errorMessage))
            {
                lastColliderPolicyErrorMessage = null;
                return;
            }

            if (string.Equals(lastColliderPolicyErrorMessage, errorMessage, StringComparison.Ordinal))
                return;

            lastColliderPolicyErrorMessage = errorMessage;
            Debug.LogError($"{nameof(EntityMoveMotorMB)} collider policy violation:\n{errorMessage}", this);
        }

        private readonly struct GroundInfo
        {
            public readonly bool IsValid;
            public readonly Vector3 Normal;
            public readonly Vector3 Point;
            public readonly Transform Transform;
            public readonly Collider Collider;
            public readonly float Distance;
            public readonly float Angle;
            public readonly GroundSurfaceKind SurfaceKind;

            public GroundInfo(bool isValid, Vector3 normal, Vector3 point, Transform transform, Collider collider, float distance, float angle, GroundSurfaceKind surfaceKind)
            {
                IsValid = isValid;
                Normal = normal;
                Point = point;
                Transform = transform;
                Collider = collider;
                Distance = distance;
                Angle = angle;
                SurfaceKind = surfaceKind;
            }
        }
    }
}

using System;
using System.Threading;
using BC.Animation;
using BC.Gimmick.Cushion;
using BC.Manager;
using BC.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.Base
{
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EntityMoveMotorMB))]
    public sealed class EntityLandingImpactMB : MonoBehaviour
    {
        private static readonly ValueModifierTagId LandingMovePenaltyTag = new ValueModifierTagId(10004);
        private static readonly ValueModifierTagId LandingJumpPenaltyTag = new ValueModifierTagId(10005);

        [Header("References")]
        [Tooltip("接地/落下情報を参照する移動モーターです。")]
        [SerializeField] private EntityMoveMotorMB moveMotor;
        [Tooltip("ValueStore の EntityRef 解決に使う EntityMB です。")]
        [SerializeField] private EntityMB entityMB;
        [Tooltip("必要時に参照する Rigidbody です。")]
        [SerializeField] private Rigidbody bodyRigidbody;
        [Tooltip("衝突系情報の補助参照に使う Collider です。")]
        [SerializeField] private Collider bodyCollider;
        [Tooltip("IEntityRagdollController 実装元。未設定時は自動探索します。")]
        [SerializeField] private MonoBehaviour ragdollControllerSource;
        [Tooltip("IAnimatorParameterController 実装元。未設定時は自動探索します。")]
        [SerializeField] private MonoBehaviour animatorControllerSource;
        [Tooltip("高所落下で所持アイテムを手放す処理の参照先です。")]
        [SerializeField] private PlayerItemHandleStateMB playerItemHandleState;
        [Tooltip("手放し方向を決める向き情報の参照先です。")]
        [SerializeField] private EntityFacingControllerMB facingController;

        [Header("Hard Landing Detection")]
        [Tooltip("この速度以上で落下して着地したときに hard landing 扱いにします。")]
        [SerializeField, Min(0.0f)] private float minimumDownwardImpactSpeed = 12.0f;
        [Tooltip("この距離以上の落下でないと hard landing を発火しません。")]
        [SerializeField, Min(0.0f)] private float minimumFallDistance = 3.0f;
        [Tooltip("hard landing リアクションを連続発火させないための最短間隔です。")]
        [SerializeField, Min(0.0f)] private float reactionCooldown = 0.2f;
        [Tooltip("クッション面が抑制対象なら hard landing リアクションを無効化します。")]
        [SerializeField] private bool suppressReactionOnCushion = true;

        [Header("Ragdoll Reaction")]
        [Tooltip("hard landing 時にラグドール化リアクションを使うかを指定します。")]
        [SerializeField] private bool enableRagdollReaction = true;
        [Tooltip("着地水平速度に掛けるラグドール初速係数です。")]
        [SerializeField, Min(0.0f)] private float ragdollHorizontalImpulseMultiplier = 0.15f;
        [Tooltip("ラグドール復帰までの待機時間です。")]
        [SerializeField, Min(0.0f)] private float ragdollRecoveryDelay = 2.0f;
        [Tooltip("ラグドール復帰時に発火するアニメーショントリガー名です。")]
        [SerializeField] private string getUpTriggerParameter = "";

        [Header("Animation Reaction")]
        [Tooltip("hard landing 時に着地アニメーションを再生するかを指定します。")]
        [SerializeField] private bool triggerLandingAnimation;
        [Tooltip("着地時に発火するアニメーショントリガー名です。")]
        [SerializeField] private string landingTriggerParameter = "";

        [Header("Penalty Reaction")]
        [Tooltip("hard landing 後に移動速度ペナルティを適用するかを指定します。")]
        [SerializeField] private bool applyMoveSpeedPenalty;
        [Tooltip("移動速度ペナルティ倍率です。1 に近いほど軽減されます。")]
        [SerializeField, Range(0.0f, 1.0f)] private float moveSpeedMultiplier = 0.45f;
        [Tooltip("hard landing 後にジャンプ力ペナルティを適用するかを指定します。")]
        [SerializeField] private bool applyJumpPenalty;
        [Tooltip("ジャンプ力ペナルティ倍率です。1 に近いほど軽減されます。")]
        [SerializeField, Range(0.0f, 1.0f)] private float jumpHeightMultiplier = 0.65f;
        [Tooltip("ペナルティを保持する時間です。")]
        [SerializeField, Min(0.0f)] private float penaltyDuration = 1.0f;

        [Header("Item Drop Reaction")]
        [Tooltip("hard landing 時に所持アイテムを強制ドロップするかを指定します。")]
        [SerializeField] private bool dropHeldItemOnHardLanding;
        [Tooltip("hard landing ドロップへ追加するローカル速度オフセットです。")]
        [SerializeField] private Vector3 additionalDropVelocityLocal = Vector3.zero;
        [Tooltip("hard landing ドロップの前方速度です。")]
        [SerializeField, Min(0.0f)] private float hardLandingDropForwardSpeed = 1.2f;
        [Tooltip("hard landing ドロップの上向き速度です。")]
        [SerializeField, Min(0.0f)] private float hardLandingDropUpwardSpeed = 1.0f;
        [Tooltip("キャリアの水平速度をドロップへ引き継ぐ割合です。")]
        [SerializeField, Range(0.0f, 1.0f)] private float hardLandingCarrierPlanarVelocityFactor = 0.25f;

        [Header("Runtime Debug")]
        [SerializeField] private float lastLandingDownwardSpeed;
        [SerializeField] private float lastLandingFallDistance;
    #pragma warning disable CS0414
        [SerializeField] private bool lastLandingSuppressedByCushion;
    #pragma warning restore CS0414

        private IEntityRagdollController ragdollController;
        private IAnimatorParameterController animatorParameterController;
        private SceneKernel sceneKernel;
        private EntityRef entityRef;
        private bool wasGroundedLastTick;
        private float previousVerticalVelocity;
        private float airbornePeakHeight;
        private float nextReactionTime;
        private CancellationTokenSource activePenaltyCancellationTokenSource;
        private CancellationTokenSource activeRagdollRecoveryCancellationTokenSource;

        // Inspector 再設定時に依存参照を補完する。
        private void Reset()
        {
            ResolveReferences();
        }

        // 起動時に参照解決と落下追跡の初期化を行う。
        private void Awake()
        {
            ResolveReferences();
            ResetLandingTracking();
        }

        // 有効化時に状態を揃える。
        private void OnEnable()
        {
            ResolveReferences();
            ResetLandingTracking();
        }

        // 無効化時は進行中の非同期処理とペナルティを確実に片付ける。
        private void OnDisable()
        {
            CancelActiveOperations();
            RemoveLandingPenalties();
            ResetLandingTracking();
        }

        private void OnDestroy()
        {
            CancelActiveOperations();
        }

        private void OnValidate()
        {
            ResolveReferences();
            minimumDownwardImpactSpeed = Mathf.Max(0.0f, minimumDownwardImpactSpeed);
            minimumFallDistance = Mathf.Max(0.0f, minimumFallDistance);
            reactionCooldown = Mathf.Max(0.0f, reactionCooldown);
            ragdollHorizontalImpulseMultiplier = Mathf.Max(0.0f, ragdollHorizontalImpulseMultiplier);
            ragdollRecoveryDelay = Mathf.Max(0.0f, ragdollRecoveryDelay);
            moveSpeedMultiplier = Mathf.Clamp01(moveSpeedMultiplier);
            jumpHeightMultiplier = Mathf.Clamp01(jumpHeightMultiplier);
            penaltyDuration = Mathf.Max(0.0f, penaltyDuration);
            hardLandingDropForwardSpeed = Mathf.Max(0.0f, hardLandingDropForwardSpeed);
            hardLandingDropUpwardSpeed = Mathf.Max(0.0f, hardLandingDropUpwardSpeed);
            hardLandingCarrierPlanarVelocityFactor = Mathf.Clamp01(hardLandingCarrierPlanarVelocityFactor);
        }

        // 接地遷移を監視し、着地フレームで hard landing 判定を行う。
        private void FixedUpdate()
        {
            if (moveMotor == null || !moveMotor.enabled)
                return;

            if (ragdollController != null && ragdollController.IsRagdollActive)
            {
                ResetLandingTracking();
                return;
            }

            bool isGrounded = moveMotor.IsGrounded;
            float currentHeight = transform.position.y;

            if (!isGrounded)
            {
                if (wasGroundedLastTick)
                    airbornePeakHeight = currentHeight;
                else if (currentHeight > airbornePeakHeight)
                    airbornePeakHeight = currentHeight;
            }
            else if (!wasGroundedLastTick)
            {
                HandleLanding();
                airbornePeakHeight = currentHeight;
            }
            else
            {
                airbornePeakHeight = currentHeight;
            }

            wasGroundedLastTick = isGrounded;
            previousVerticalVelocity = moveMotor.VerticalVelocity;
        }

        // 速度と落下距離から hard landing を判定してリアクションを起動する。
        private void HandleLanding()
        {
            if (moveMotor == null || moveMotor.IsDead || Time.time < nextReactionTime)
                return;

            // 着地フレームでは Motor 側が縦速度を groundedStickVelocity へ補正済みなので、
            // 直前 tick の縦速度を保持しておき、そこから衝撃量を計算する。
            float downwardImpactSpeed = Mathf.Max(0.0f, -previousVerticalVelocity);
            float landingHeight = moveMotor.GroundPoint.y;
            float fallDistance = Mathf.Max(0.0f, airbornePeakHeight - landingHeight);

            lastLandingDownwardSpeed = downwardImpactSpeed;
            lastLandingFallDistance = fallDistance;
            lastLandingSuppressedByCushion = false;

            if (downwardImpactSpeed < minimumDownwardImpactSpeed)
                return;

            if (minimumFallDistance > 0.0f && fallDistance < minimumFallDistance)
                return;

            if (suppressReactionOnCushion && IsHardLandingSuppressedByCushion())
            {
                lastLandingSuppressedByCushion = true;
                return;
            }

            nextReactionTime = Time.time + reactionCooldown;

            Vector3 landingVelocity = BuildLandingVelocity();
            ApplyLandingReaction(landingVelocity);
        }

        // hard landing 時の反応を統合実行する。
        private void ApplyLandingReaction(Vector3 landingVelocity)
        {
            if (dropHeldItemOnHardLanding)
                DropHeldItem(landingVelocity);

            StartPenaltyRoutine();

            if (enableRagdollReaction && ragdollController != null)
            {
                ragdollController.EnterRagdoll(BuildRagdollImpulse(landingVelocity));
                StartRagdollRecoveryRoutine();
                return;
            }

            if (triggerLandingAnimation)
                TriggerAnimatorParameter(landingTriggerParameter);
        }

        // 着地判定に使う速度を組み立てる。
        private Vector3 BuildLandingVelocity()
        {
            Vector3 currentVelocity = moveMotor != null ? moveMotor.CurrentVelocity : Vector3.zero;
            currentVelocity.y = previousVerticalVelocity;
            return currentVelocity;
        }

        // ラグドールへ渡す水平インパルスを算出する。
        private Vector3 BuildRagdollImpulse(Vector3 landingVelocity)
        {
            Vector3 horizontalVelocity = landingVelocity;
            horizontalVelocity.y = 0.0f;
            return horizontalVelocity * ragdollHorizontalImpulseMultiplier;
        }

        // 接地中のクッション面が hard landing 抑制対象かを判定する。
        private bool IsHardLandingSuppressedByCushion()
        {
            if (moveMotor == null || moveMotor.GroundTransform == null)
                return false;

            CushionSurfaceMB surface = moveMotor.GroundTransform.GetComponentInParent<CushionSurfaceMB>();

            if (surface == null)
                return false;

            return surface.TryEvaluateHardLandingSuppression(moveMotor.CushionImpactTag, transform);
        }

        // hard landing 専用の速度で所持アイテムを強制リリースする。
        private void DropHeldItem(Vector3 landingVelocity)
        {
            if (playerItemHandleState == null)
                return;

            Vector3 facingDirection = ResolveFacingForwardDirection();
            Vector3 carrierPlanarVelocity = landingVelocity;
            carrierPlanarVelocity.y = 0.0f;
            carrierPlanarVelocity *= hardLandingCarrierPlanarVelocityFactor;

            Vector3 releaseVelocity =
                (facingDirection * hardLandingDropForwardSpeed) +
                (Vector3.up * hardLandingDropUpwardSpeed) +
                carrierPlanarVelocity +
                transform.TransformDirection(additionalDropVelocityLocal);

            playerItemHandleState.ForceReleaseCurrentItem(releaseVelocity);
        }

        // ドロップ基準に使う前方ベクトルを解決する。
        private Vector3 ResolveFacingForwardDirection()
        {
            if (facingController != null && facingController.TryGetWorldFrontDirection(out Vector3 worldFront))
                return worldFront;

            Vector3 forward = transform.forward;
            forward.y = 0.0f;

            if (forward.sqrMagnitude <= 0.0001f)
                return Vector3.forward;

            return forward.normalized;
        }

        // ペナルティ有効時のみ非同期ルーチンを起動する。
        private void StartPenaltyRoutine()
        {
            if ((!applyMoveSpeedPenalty && !applyJumpPenalty) || penaltyDuration <= 0.0f)
                return;

            RunPenaltyAsync().Forget();
        }

        // 速度/ジャンプペナルティを一定時間だけ適用する。
        private async UniTaskVoid RunPenaltyAsync()
        {
            ResolveRuntimeReferences();

            if (sceneKernel == null || sceneKernel.EntityValueStore == null || !entityRef.IsValid)
                return;

            CancellationTokenSource cancellationTokenSource = BeginNewPenaltyCancellation();
            ApplyLandingPenalties();

            try
            {
                await UniTask.Delay(
                    Mathf.CeilToInt(penaltyDuration * 1000.0f),
                    cancellationToken: cancellationTokenSource.Token);
            }
            catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
            {
            }
            finally
            {
                if (ReferenceEquals(activePenaltyCancellationTokenSource, cancellationTokenSource))
                {
                    RemoveLandingPenalties();
                    activePenaltyCancellationTokenSource = null;
                }

                cancellationTokenSource.Dispose();
            }
        }

        // ValueStore へ hard landing ペナルティを設定する。
        private void ApplyLandingPenalties()
        {
            if (sceneKernel == null || sceneKernel.EntityValueStore == null || !entityRef.IsValid)
                return;

            if (applyMoveSpeedPenalty)
            {
                sceneKernel.EntityValueStore.SetMul(
                    entityRef,
                    ValueKeys.Move.BaseSpeed,
                    LandingMovePenaltyTag,
                    moveSpeedMultiplier);
            }

            if (applyJumpPenalty)
            {
                sceneKernel.EntityValueStore.SetMul(
                    entityRef,
                    ValueKeys.Move.JumpHeightMultiplier,
                    LandingJumpPenaltyTag,
                    jumpHeightMultiplier);
            }
        }

        // ValueStore から hard landing ペナルティを解除する。
        private void RemoveLandingPenalties()
        {
            ResolveRuntimeReferences();

            if (sceneKernel == null || sceneKernel.EntityValueStore == null || !entityRef.IsValid)
                return;

            if (applyMoveSpeedPenalty)
            {
                sceneKernel.EntityValueStore.RemoveMul(
                    entityRef,
                    ValueKeys.Move.BaseSpeed,
                    LandingMovePenaltyTag);
            }

            if (applyJumpPenalty)
            {
                sceneKernel.EntityValueStore.RemoveMul(
                    entityRef,
                    ValueKeys.Move.JumpHeightMultiplier,
                    LandingJumpPenaltyTag);
            }
        }

        // ラグドール復帰処理を開始する。
        private void StartRagdollRecoveryRoutine()
        {
            if (ragdollRecoveryDelay <= 0.0f)
            {
                CompleteRagdollRecovery();
                return;
            }

            RunRagdollRecoveryAsync().Forget();
        }

        // 一定時間後にラグドール復帰を実行する。
        private async UniTaskVoid RunRagdollRecoveryAsync()
        {
            CancellationTokenSource cancellationTokenSource = BeginNewRagdollRecoveryCancellation();

            try
            {
                await UniTask.Delay(
                    Mathf.CeilToInt(ragdollRecoveryDelay * 1000.0f),
                    cancellationToken: cancellationTokenSource.Token);
            }
            catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
            {
            }
            finally
            {
                if (ReferenceEquals(activeRagdollRecoveryCancellationTokenSource, cancellationTokenSource))
                {
                    activeRagdollRecoveryCancellationTokenSource = null;
                    CompleteRagdollRecovery();
                }

                cancellationTokenSource.Dispose();
            }
        }

        // ラグドール解除と起き上がりトリガー発火を行う。
        private void CompleteRagdollRecovery()
        {
            if (ragdollController == null || !ragdollController.IsRagdollActive)
                return;

            ragdollController.ExitRagdoll();
            TriggerAnimatorParameter(getUpTriggerParameter);
            ResetLandingTracking();
        }

        // 指定トリガーを一度リセットしてから再発火する。
        private void TriggerAnimatorParameter(string parameterName)
        {
            if (animatorParameterController == null || string.IsNullOrWhiteSpace(parameterName))
                return;

            animatorParameterController.ResetTrigger(parameterName);
            animatorParameterController.SetTrigger(parameterName);
        }

        // Inspector 未設定の参照を安全に補完する。
        private void ResolveReferences()
        {
            if (moveMotor == null)
                moveMotor = GetComponent<EntityMoveMotorMB>();

            if (entityMB == null)
                entityMB = GetComponentInParent<EntityMB>();

            if (bodyRigidbody == null)
                bodyRigidbody = GetComponent<Rigidbody>();

            if (bodyCollider == null)
                bodyCollider = GetComponent<CapsuleCollider>();

            if (bodyCollider == null)
                bodyCollider = GetComponent<Collider>();

            if (playerItemHandleState == null)
                playerItemHandleState = GetComponent<PlayerItemHandleStateMB>();

            ragdollController = ResolveInterfaceReference<IEntityRagdollController>(ragdollControllerSource, ref ragdollControllerSource);
            animatorParameterController = ResolveInterfaceReference<IAnimatorParameterController>(animatorControllerSource, ref animatorControllerSource);
        }

        // 実行時に必要な kernel と entity を解決する。
        private void ResolveRuntimeReferences()
        {
            if (sceneKernel == null)
            {
                SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

                if (kernelMB != null)
                    sceneKernel = kernelMB.Kernel;
            }

            if (!entityRef.IsValid && entityMB != null && entityMB.HasEntity)
                entityRef = entityMB.Entity;
        }

        // 着地追跡の内部状態を初期値へ戻す。
        private void ResetLandingTracking()
        {
            wasGroundedLastTick = moveMotor != null && moveMotor.IsGrounded;
            previousVerticalVelocity = moveMotor != null ? moveMotor.VerticalVelocity : 0.0f;
            airbornePeakHeight = transform.position.y;
        }

        // 進行中のペナルティ/復帰タスクをキャンセルする。
        private void CancelActiveOperations()
        {
            activePenaltyCancellationTokenSource?.Cancel();
            activePenaltyCancellationTokenSource = null;

            activeRagdollRecoveryCancellationTokenSource?.Cancel();
            activeRagdollRecoveryCancellationTokenSource = null;
        }

        private CancellationTokenSource BeginNewPenaltyCancellation()
        {
            if (activePenaltyCancellationTokenSource != null)
                activePenaltyCancellationTokenSource.Cancel();

            activePenaltyCancellationTokenSource = new CancellationTokenSource();
            return activePenaltyCancellationTokenSource;
        }

        private CancellationTokenSource BeginNewRagdollRecoveryCancellation()
        {
            if (activeRagdollRecoveryCancellationTokenSource != null)
                activeRagdollRecoveryCancellationTokenSource.Cancel();

            activeRagdollRecoveryCancellationTokenSource = new CancellationTokenSource();
            return activeRagdollRecoveryCancellationTokenSource;
        }

        private T ResolveInterfaceReference<T>(MonoBehaviour assignedSource, ref MonoBehaviour cachedSource) where T : class
        {
            if (assignedSource is T assigned)
                return assigned;

            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not T found)
                    continue;

                cachedSource = behaviours[i];
                return found;
            }

            behaviours = GetComponentsInParent<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not T found)
                    continue;

                cachedSource = behaviours[i];
                return found;
            }

            cachedSource = null;
            return null;
        }
    }
}
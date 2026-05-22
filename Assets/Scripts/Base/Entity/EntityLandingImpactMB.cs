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
        [SerializeField] private EntityMoveMotorMB moveMotor;
        [SerializeField] private EntityMB entityMB;
        [SerializeField] private Rigidbody bodyRigidbody;
        [SerializeField] private Collider bodyCollider;
        [SerializeField] private MonoBehaviour ragdollControllerSource;
        [SerializeField] private MonoBehaviour animatorControllerSource;
        [SerializeField] private PlayerItemHandleStateMB playerItemHandleState;
        [SerializeField] private EntityFacingControllerMB facingController;

        [Header("Hard Landing Detection")]
        [SerializeField, Min(0.0f)] private float minimumDownwardImpactSpeed = 12.0f;
        [SerializeField, Min(0.0f)] private float minimumFallDistance = 3.0f;
        [SerializeField, Min(0.0f)] private float reactionCooldown = 0.2f;
        [SerializeField] private bool suppressReactionOnCushion = true;

        [Header("Ragdoll Reaction")]
        [SerializeField] private bool enableRagdollReaction = true;
        [SerializeField, Min(0.0f)] private float ragdollHorizontalImpulseMultiplier = 0.15f;
        [SerializeField, Min(0.0f)] private float ragdollRecoveryDelay = 2.0f;
        [SerializeField] private string getUpTriggerParameter = "";

        [Header("Animation Reaction")]
        [SerializeField] private bool triggerLandingAnimation;
        [SerializeField] private string landingTriggerParameter = "";

        [Header("Penalty Reaction")]
        [SerializeField] private bool applyMoveSpeedPenalty;
        [SerializeField, Range(0.0f, 1.0f)] private float moveSpeedMultiplier = 0.45f;
        [SerializeField] private bool applyJumpPenalty;
        [SerializeField, Range(0.0f, 1.0f)] private float jumpHeightMultiplier = 0.65f;
        [SerializeField, Min(0.0f)] private float penaltyDuration = 1.0f;

        [Header("Item Drop Reaction")]
        [SerializeField] private bool dropHeldItemOnHardLanding;
        [SerializeField] private Vector3 additionalDropVelocityLocal = Vector3.zero;
        [SerializeField, Min(0.0f)] private float hardLandingDropForwardSpeed = 1.2f;
        [SerializeField, Min(0.0f)] private float hardLandingDropUpwardSpeed = 1.0f;
        [SerializeField, Range(0.0f, 1.0f)] private float hardLandingCarrierPlanarVelocityFactor = 0.25f;

        [Header("Runtime Debug")]
        [SerializeField] private float lastLandingDownwardSpeed;
        [SerializeField] private float lastLandingFallDistance;
        [SerializeField] private bool lastLandingSuppressedByCushion;

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

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            ResetLandingTracking();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResetLandingTracking();
        }

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

        private Vector3 BuildLandingVelocity()
        {
            Vector3 currentVelocity = moveMotor != null ? moveMotor.CurrentVelocity : Vector3.zero;
            currentVelocity.y = previousVerticalVelocity;
            return currentVelocity;
        }

        private Vector3 BuildRagdollImpulse(Vector3 landingVelocity)
        {
            Vector3 horizontalVelocity = landingVelocity;
            horizontalVelocity.y = 0.0f;
            return horizontalVelocity * ragdollHorizontalImpulseMultiplier;
        }

        private bool IsHardLandingSuppressedByCushion()
        {
            if (moveMotor == null || moveMotor.GroundTransform == null)
                return false;

            CushionSurfaceMB surface = moveMotor.GroundTransform.GetComponentInParent<CushionSurfaceMB>();

            if (surface == null)
                return false;

            return surface.TryEvaluateHardLandingSuppression(moveMotor.CushionImpactTag, transform);
        }

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

        private void StartPenaltyRoutine()
        {
            if ((!applyMoveSpeedPenalty && !applyJumpPenalty) || penaltyDuration <= 0.0f)
                return;

            RunPenaltyAsync().Forget();
        }

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

        private void StartRagdollRecoveryRoutine()
        {
            if (ragdollRecoveryDelay <= 0.0f)
            {
                CompleteRagdollRecovery();
                return;
            }

            RunRagdollRecoveryAsync().Forget();
        }

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

        private void CompleteRagdollRecovery()
        {
            if (ragdollController == null || !ragdollController.IsRagdollActive)
                return;

            ragdollController.ExitRagdoll();
            TriggerAnimatorParameter(getUpTriggerParameter);
            ResetLandingTracking();
        }

        private void TriggerAnimatorParameter(string parameterName)
        {
            if (animatorParameterController == null || string.IsNullOrWhiteSpace(parameterName))
                return;

            animatorParameterController.ResetTrigger(parameterName);
            animatorParameterController.SetTrigger(parameterName);
        }

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

        private void ResetLandingTracking()
        {
            wasGroundedLastTick = moveMotor != null && moveMotor.IsGrounded;
            previousVerticalVelocity = moveMotor != null ? moveMotor.VerticalVelocity : 0.0f;
            airbornePeakHeight = transform.position.y;
        }

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
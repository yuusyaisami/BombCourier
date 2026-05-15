using System;
using System.Collections.Generic;
using BC.Base;
using BC.Item;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BC.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerItemHandleStateMB : MonoBehaviour, IEntityHandleItemAnimationSource, IPlayerInteractionSource
    {
        [Header("Detection")]
        [SerializeField] private float handleItemDistance = 1.5f;
        [SerializeField] private float handleItemAngleThreshold = 45f;
        [SerializeField] private LayerMask itemLayerMask = ~0;
        [SerializeField] private Transform handleItemPoint;
        [SerializeField] private GameObject playerModel;

        [Header("Input")]
        [SerializeField] private InputActionReference handleItemAction;

        [Header("Throw")]
        [SerializeField] private float maxThrowForce = 12f;
        [SerializeField] private float minThrowForce = 4f;
        [SerializeField] private float throwForceChargeTime = 1.5f;
        [SerializeField, Min(0f)] private float emptyHandThrowPreviewHoldTime = 0.2f;

        [Header("Trajectory")]
        [SerializeField] private LineRenderer trajectoryLineRenderer;
        [SerializeField] private LayerMask trajectoryCollisionMask = ~0;
        [SerializeField, Min(2)] private int trajectoryPointCount = 32;
        [SerializeField, Min(0.01f)] private float trajectoryTimeStep = 0.08f;
        [SerializeField, Min(0.01f)] private float trajectoryProbeRadius = 0.45f;
        [SerializeField, Min(0.001f)] private float trajectoryLineWidth = 0.04f;
        [SerializeField] private Color trajectoryColor = new Color(1.0f, 0.62f, 0.08f, 0.9f);

        [Header("Runtime Debug")]
        [SerializeField] private bool isHandlingItem;


        private static readonly ValueModifierTagId CarryItemJumpPenaltyTag = new ValueModifierTagId(10003);
        private const int MaxTrajectoryHits = 16;
        // アイテムを拾った後、プレイヤーが入力を離すまで次のアイテムを拾えないようにするためのフラグ。これがないと、例えば爆弾を投げた瞬間にもう一度掴んでしまう。
        private bool waitForPickupInputRelease;
        private bool isThrowCharging;
        private bool carryJumpPenaltyApplied;

        private readonly RaycastHit[] trajectoryHits = new RaycastHit[MaxTrajectoryHits];

        private ValueStoreService valueStore;
        private EntityRef entityRef;
        private ValueWatchHandle<bool> fatigueInteractHandle;
        private ICarryableItem currentlyHandledItem;
        private PlayerInteractionController interactionController;
        private float emptyHandThrowPreviewTimer;
        private float throwForceChargeTimer;
        private Vector3[] trajectoryPoints = new Vector3[32];
        private Material ownedTrajectoryMaterial;
        private bool isEmptyHandThrowPreviewActive;
        private int throwSequence;
        private int lastConsumedInputPressSequence;

        public bool IsHandlingItem => isHandlingItem;
        public bool IsInputPressed => interactionController != null && interactionController.IsInputPressed;
        public float InputHoldDuration => interactionController != null ? interactionController.InputHoldDuration : 0f;
        public int InputPressSequence => interactionController != null ? interactionController.InputPressSequence : 0;
        public int InputReleaseSequence => interactionController != null ? interactionController.InputReleaseSequence : 0;
        public bool HasCandidate => interactionController != null && interactionController.HasCandidate;
        public IPlayerInteractable CurrentBestInteractable => interactionController != null ? interactionController.CurrentBestInteractable : null;
        public IPlayerInteractable ActiveInteractable => interactionController != null ? interactionController.ActiveInteractable : null;
        public float ActiveHoldProgress => interactionController != null ? interactionController.ActiveHoldProgress : 0f;
        public IReadOnlyList<PlayerInteractionCandidate> Candidates =>
            interactionController != null ? interactionController.Candidates : Array.Empty<PlayerInteractionCandidate>();

        // throw系の状態
        public bool IsThrowCharging => isThrowCharging || isEmptyHandThrowPreviewActive;
        public float CurrentThrowForce => CalculateThrowForce();
        public float CurrentThrowChargeRatio => Mathf.Clamp01(throwForceChargeTimer / Mathf.Max(0.01f, throwForceChargeTime));
        public Action OnThrowChargeStart { get; set; }
        public Action OnThrowChargeEnd { get; set; }
        public event Action<PlayerInteractionEventData> InteractionEvent
        {
            add
            {
                if (interactionController != null)
                    interactionController.InteractionEvent += value;
            }
            remove
            {
                if (interactionController != null)
                    interactionController.InteractionEvent -= value;
            }
        }

        private void Awake()
        {
            if (handleItemPoint == null)
                Debug.LogError($"{nameof(PlayerItemHandleStateMB)}: handleItemPoint is not assigned.", this);

            if (playerModel == null)
                Debug.LogError($"{nameof(PlayerItemHandleStateMB)}: playerModel is not assigned.", this);

            if (handleItemAction == null || handleItemAction.action == null)
                Debug.LogError($"{nameof(PlayerItemHandleStateMB)}: handleItemAction is not assigned.", this);

            interactionController = new PlayerInteractionController(
                handleItemAction != null ? handleItemAction.action : null,
                handleItemPoint,
                playerModel != null ? playerModel.transform : transform,
                handleItemDistance,
                handleItemAngleThreshold,
                itemLayerMask);
            interactionController.InteractionEvent += HandleInteractionEvent;

            ResolveRuntimeReferences(logMissingEntity: true);
        }

        private void OnEnable()
        {
            handleItemAction?.action?.Enable();
            interactionController?.Bind();
        }

        private void OnDisable()
        {
            interactionController?.ResetRuntimeState();
            interactionController?.Unbind();
            handleItemAction?.action?.Disable();
            HideTrajectory();
            RemoveCarryJumpPenalty();
            ResetEmptyHandThrowPreview();
        }

        private void OnDestroy()
        {
            if (interactionController != null)
            {
                interactionController.InteractionEvent -= HandleInteractionEvent;
                interactionController.Dispose();
                interactionController = null;
            }

            if (ownedTrajectoryMaterial != null)
            {
                Destroy(ownedTrajectoryMaterial);
                ownedTrajectoryMaterial = null;
            }
        }

        private void Update()
        {
            if (handleItemAction == null || handleItemAction.action == null)
                return;

            if (handleItemPoint == null || playerModel == null)
                return;

            interactionController?.Tick(Time.deltaTime, !isHandlingItem);

            if (!isHandlingItem)
            {
                HideTrajectory();

                if (!isHandlingItem)
                    TickEmptyHandThrowPreview(Time.deltaTime);
                else
                    ResetEmptyHandThrowPreview();
            }
            else
            {
                ResetEmptyHandThrowPreview();
                TickThrow();
            }

            PublishRuntimeValues();
        }

        private void TickEmptyHandThrowPreview(float dt)
        {
            if (GetCurrentBestCarryableItem() != null || IsFatigueInteracting())
            {
                ResetEmptyHandThrowPreview();
                return;
            }

            if (!IsInputPressed)
            {
                ResetEmptyHandThrowPreview();
                return;
            }

            emptyHandThrowPreviewTimer += dt;

            if (emptyHandThrowPreviewTimer >= emptyHandThrowPreviewHoldTime)
                isEmptyHandThrowPreviewActive = true;
        }

        private void TickThrow()
        {
            if (currentlyHandledItem == null)
            {
                ClearHeldState();
                return;
            }

            // 拾った時の入力がまだ押されている間は、投げ処理を一切しない。
            if (waitForPickupInputRelease)
            {
                HideTrajectory();

                if (IsInputPressed)
                {
                    return;
                }

                waitForPickupInputRelease = false;
                return;
            }

            // まだ投げチャージしていない。
            // 2回目の押下で初めてチャージ開始。
            if (!isThrowCharging)
            {
                HideTrajectory();

                if (ConsumeInputPress())
                {
                    isThrowCharging = true;
                    OnThrowChargeStart?.Invoke();
                    throwForceChargeTimer = 0f;
                    UpdateThrowTrajectory();
                }

                return;
            }

            // チャージ中。
            if (IsInputPressed)
            {
                throwForceChargeTimer += Time.deltaTime;
                UpdateThrowTrajectory();
                return;
            }

            // チャージ後に離したら投げる。
            if (isThrowCharging)
            {
                isThrowCharging = false;
                OnThrowChargeEnd?.Invoke();
            }

            ReleaseCurrentItem();
        }
        private void HandleItem(ICarryableItem item)
        {
            if (item == null || item.ItemTransform == null)
                return;

            interactionController?.ClearCandidateState();
            ResetEmptyHandThrowPreview();

            currentlyHandledItem = item;

            isHandlingItem = true;
            throwForceChargeTimer = 0f;

            // 拾った時の押下シーケンスをここで消費済みにしないと、
            // ボタンを離した直後に「2回目の押下」と誤判定して即投げへ進んでしまう。
            lastConsumedInputPressSequence = InputPressSequence;

            // 拾った時の押下を、投げチャージに流用させない。
            waitForPickupInputRelease = true;
            isThrowCharging = false;

            item.OnHandle(handleItemPoint);
            ApplyCarryJumpPenaltyIfNeeded(item);

            PublishRuntimeValues();
        }

        private void ReleaseCurrentItem()
        {
            if (currentlyHandledItem == null)
            {
                ClearHeldState();
                return;
            }

            currentlyHandledItem.OnRelease(BuildThrowVelocity());
            throwSequence++;

            ClearHeldState();
        }

        private void ClearHeldState()
        {
            currentlyHandledItem = null;

            isHandlingItem = false;
            throwForceChargeTimer = 0f;
            waitForPickupInputRelease = false;
            isThrowCharging = false;

            ResetEmptyHandThrowPreview();
            HideTrajectory();
            RemoveCarryJumpPenalty();
            PublishRuntimeValues();
        }

        private float CalculateThrowForce()
        {
            float chargeRatio = Mathf.Clamp01(throwForceChargeTimer / Mathf.Max(0.01f, throwForceChargeTime));
            return Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);
        }

        private Vector3 BuildThrowVelocity()
        {
            return BuildThrowDirection() * CalculateThrowForce();
        }

        private Vector3 BuildThrowDirection()
        {
            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;

            if (mainCamera != null && mainCamera.transform.forward.sqrMagnitude > 0.0001f)
                return mainCamera.transform.forward.normalized;

            Vector3 fallbackDirection = playerModel != null
                ? playerModel.transform.forward
                : transform.forward;

            fallbackDirection.y = 0.0f;

            if (fallbackDirection.sqrMagnitude <= 0.0001f)
                fallbackDirection = transform.forward;

            return fallbackDirection.normalized;
        }

        private void ApplyCarryJumpPenaltyIfNeeded(ICarryableItem item)
        {
            if (item is not ICarryMoveModifier moveModifier)
                return;

            if (!moveModifier.TryGetJumpHeightMultiplier(out float jumpHeightMultiplier))
                return;

            if (Mathf.Approximately(jumpHeightMultiplier, 1.0f))
                return;

            ResolveRuntimeReferences(logMissingEntity: false);

            if (valueStore == null || !entityRef.IsValid)
                return;

            // 持ち物ごとの移動補正は、拾う側ではなくアイテム側の任意インターフェースから受け取る。
            valueStore.SetMul(
                entityRef,
                ValueKeys.Move.JumpHeightMultiplier,
                CarryItemJumpPenaltyTag,
                Mathf.Max(0.0f, jumpHeightMultiplier));

            carryJumpPenaltyApplied = true;
        }

        private void RemoveCarryJumpPenalty()
        {
            if (!carryJumpPenaltyApplied)
                return;

            ResolveRuntimeReferences(logMissingEntity: false);

            if (valueStore != null && entityRef.IsValid)
            {
                valueStore.RemoveMul(
                    entityRef,
                    ValueKeys.Move.JumpHeightMultiplier,
                    CarryItemJumpPenaltyTag);
            }

            carryJumpPenaltyApplied = false;
        }

        private void UpdateThrowTrajectory()
        {
            if (currentlyHandledItem == null || currentlyHandledItem.ItemTransform == null)
            {
                HideTrajectory();
                return;
            }

            EnsureTrajectoryRenderer();

            if (trajectoryLineRenderer == null)
                return;

            int pointCount = Mathf.Clamp(trajectoryPointCount, 2, 64);

            if (trajectoryPoints == null || trajectoryPoints.Length < pointCount)
                trajectoryPoints = new Vector3[pointCount];

            Vector3 origin = handleItemPoint != null
                ? handleItemPoint.position
                : currentlyHandledItem.ItemTransform.position;
            Vector3 velocity = BuildThrowVelocity();
            Vector3 previous = origin;

            trajectoryPoints[0] = origin;
            int usedPointCount = 1;

            for (int i = 1; i < pointCount; i++)
            {
                float time = trajectoryTimeStep * i;
                Vector3 next = origin + velocity * time + 0.5f * Physics.gravity * time * time;

                if (TryFindTrajectoryHit(previous, next, out Vector3 hitPoint))
                {
                    trajectoryPoints[usedPointCount++] = hitPoint;
                    break;
                }

                trajectoryPoints[usedPointCount++] = next;
                previous = next;
            }

            trajectoryLineRenderer.positionCount = usedPointCount;

            for (int i = 0; i < usedPointCount; i++)
            {
                trajectoryLineRenderer.SetPosition(i, trajectoryPoints[i]);
            }

            trajectoryLineRenderer.enabled = true;
        }

        private bool TryFindTrajectoryHit(Vector3 from, Vector3 to, out Vector3 hitPoint)
        {
            hitPoint = to;

            Vector3 segment = to - from;
            float distance = segment.magnitude;

            if (distance <= 0.0001f)
                return false;

            Vector3 direction = segment / distance;
            float radius = Mathf.Max(0.01f, trajectoryProbeRadius);

            int hitCount = Physics.SphereCastNonAlloc(
                from,
                radius,
                direction,
                trajectoryHits,
                distance,
                trajectoryCollisionMask,
                QueryTriggerInteraction.Ignore);

            bool found = false;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = trajectoryHits[i];

                if (hit.collider == null || IsIgnoredTrajectoryCollider(hit.collider))
                    continue;

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    found = true;
                }
            }

            if (!found)
                return false;

            hitPoint = from + direction * bestDistance;
            return true;
        }

        private bool IsIgnoredTrajectoryCollider(Collider hit)
        {
            if (hit == null)
                return true;

            Transform holderRoot = ResolveTrajectoryHolderRoot();

            if (holderRoot != null && hit.transform.IsChildOf(holderRoot))
                return true;

            Transform itemTransform = currentlyHandledItem?.ItemTransform;
            return itemTransform != null && hit.transform.IsChildOf(itemTransform);
        }

        private Transform ResolveTrajectoryHolderRoot()
        {
            if (handleItemPoint == null)
                return transform;

            CharacterController ownerController = handleItemPoint.GetComponentInParent<CharacterController>();

            if (ownerController != null)
                return ownerController.transform;

            return handleItemPoint.root != null ? handleItemPoint.root : transform;
        }

        private void EnsureTrajectoryRenderer()
        {
            if (trajectoryLineRenderer == null)
            {
                GameObject lineObject = new GameObject("Bomb Throw Trajectory");
                lineObject.transform.SetParent(transform, false);
                trajectoryLineRenderer = lineObject.AddComponent<LineRenderer>();
            }

            trajectoryLineRenderer.useWorldSpace = true;
            trajectoryLineRenderer.startWidth = trajectoryLineWidth;
            trajectoryLineRenderer.endWidth = trajectoryLineWidth;
            trajectoryLineRenderer.startColor = trajectoryColor;
            trajectoryLineRenderer.endColor = trajectoryColor;
            trajectoryLineRenderer.numCapVertices = 4;
            trajectoryLineRenderer.numCornerVertices = 4;

            if (trajectoryLineRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");

                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Unlit");

                if (shader != null)
                {
                    ownedTrajectoryMaterial = new Material(shader);
                    trajectoryLineRenderer.sharedMaterial = ownedTrajectoryMaterial;
                }
            }
        }

        private void HideTrajectory()
        {
            if (trajectoryLineRenderer == null)
                return;

            trajectoryLineRenderer.enabled = false;
            trajectoryLineRenderer.positionCount = 0;
        }

        private void HandleInteractionEvent(PlayerInteractionEventData eventData)
        {
            if (eventData.EventType != PlayerInteractionEventType.Completed)
                return;

            if (eventData.Interactable is not CarryableItemInteractableAdapter adapter)
                return;

            HandleItem(adapter.CarryableItem);
        }

        private ICarryableItem GetCurrentBestCarryableItem()
        {
            return CurrentBestInteractable is CarryableItemInteractableAdapter adapter
                ? adapter.CarryableItem
                : null;
        }

        private void PublishRuntimeValues()
        {
            ResolveRuntimeReferences(logMissingEntity: false);

            if (valueStore == null || !entityRef.IsValid)
                return;

            valueStore.Set(entityRef, ValueKeys.Runtime.IsHandlingItem, isHandlingItem);
            valueStore.Set(entityRef, ValueKeys.Runtime.IsThrowPoseActive, IsThrowPoseActive());
            valueStore.Set(entityRef, ValueKeys.Runtime.IsItemThrowAiming, IsItemThrowAiming());
            valueStore.Set(entityRef, ValueKeys.Runtime.ThrowSequence, throwSequence);
        }

        private void ResolveRuntimeReferences(bool logMissingEntity)
        {
            if (valueStore == null)
            {
                SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

                if (kernelMB != null && kernelMB.Kernel != null)
                    valueStore = kernelMB.Kernel.ValueStore;
            }

            if (!entityRef.IsValid)
            {
                EntityMB entityMB = GetComponentInParent<EntityMB>();

                if (entityMB != null && entityMB.HasEntity)
                {
                    entityRef = entityMB.Entity;
                }
                else if (logMissingEntity)
                {
                    Debug.LogWarning($"{nameof(PlayerItemHandleStateMB)}: EntityMB is not found or not bound.", this);
                }
            }

            if (fatigueInteractHandle == null && valueStore != null && entityRef.IsValid)
                fatigueInteractHandle = valueStore.GetHandle(entityRef, ValueKeys.Runtime.IsFatigueInteracting);
        }

        private void OnDrawGizmosSelected()
        {
            if (handleItemPoint == null)
                return;

            Gizmos.DrawWireSphere(handleItemPoint.position, handleItemDistance);
        }

        private bool IsFatigueInteracting()
        {
            ResolveRuntimeReferences(logMissingEntity: false);
            return fatigueInteractHandle != null && fatigueInteractHandle.CurrentValue;
        }

        private bool IsThrowPoseActive()
        {
            return isThrowCharging || isEmptyHandThrowPreviewActive;
        }

        private bool IsItemThrowAiming()
        {
            return isHandlingItem && currentlyHandledItem != null && isThrowCharging;
        }

        private void ResetEmptyHandThrowPreview()
        {
            emptyHandThrowPreviewTimer = 0f;
            isEmptyHandThrowPreviewActive = false;
        }

        private bool ConsumeInputPress()
        {
            int currentSequence = InputPressSequence;

            if (currentSequence == 0 || currentSequence == lastConsumedInputPressSequence)
                return false;

            lastConsumedInputPressSequence = currentSequence;
            return true;
        }
    }
}

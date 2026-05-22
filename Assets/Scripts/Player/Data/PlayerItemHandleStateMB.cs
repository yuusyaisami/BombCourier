using System;
using System.Collections.Generic;
using BC.Base;
using BC.Bomb;
using BC.Item;
using BC.Manager;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BC.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerItemHandleStateMB : MonoBehaviour, IEntityHandleItemAnimationSource, IInteractionSource
    {
        [Header("Detection")]
        [SerializeField] private float handleItemDistance = 1.5f;
        [SerializeField] private float handleItemAngleThreshold = 45f;
        [SerializeField] private LayerMask itemLayerMask = ~0;
        [SerializeField] private Transform handleItemPoint;
        [SerializeField] private GameObject playerModel;

        [Header("Input")]
        [SerializeField] private InputActionReference handleItemAction;

        [Header("Interaction Facing")]
        [SerializeField] private bool faceInteractionTargetOnStart = true;
        [SerializeField] private EntityFacingControllerMB facingController;

        [Header("Throw")]
        [SerializeField] private float maxThrowForce = 12f;
        [SerializeField] private float minThrowForce = 4f;
        [SerializeField, Min(0.01f)] private float throwChargeActivationHoldTime = 0.1f;
        [SerializeField] private float throwForceChargeTime = 1.5f;
        [SerializeField, Min(0f)] private float dropForwardVelocity = 0.75f;
        [SerializeField, Range(0f, 1f)] private float dropCarrierPlanarVelocityFactor = 0.35f;
        [SerializeField, Min(0f)] private float emptyHandThrowPreviewHoldTime = 0.2f;
        [SerializeField, Range(0f, 30f)] private float throwUpwardCompensationAngle = 8f;

        [Header("Trajectory")]
        [SerializeField] private LineRenderer trajectoryLineRenderer;
        [SerializeField] private LayerMask trajectoryCollisionMask = ~0;
        [SerializeField, Min(2)] private int trajectoryPointCount = 32;
        [SerializeField, Min(0.01f)] private float trajectoryTimeStep = 0.08f;
        [SerializeField, Min(0.01f)] private float trajectoryProbeRadius = 0.45f;
        [SerializeField, Min(0.001f)] private float trajectoryLineWidth = 0.04f;
        [SerializeField] private Color trajectoryColor = new Color(1.0f, 0.62f, 0.08f, 0.9f);
        [SerializeField] private bool includeTriggerCollidersInTrajectory = true;
        [SerializeField, Min(0f)] private float trajectoryHitMarkerMinDistance = 1.0f;
        [SerializeField, Min(0.01f)] private float trajectoryHitMarkerDiameter = 0.24f;
        [SerializeField] private Color trajectoryHitMarkerColor = new Color(1.0f, 0.82f, 0.24f, 0.95f);

        [Header("Runtime Debug")]
        [SerializeField] private bool currentCanInteract = true;
        [SerializeField] private bool isHandlingItem;


        private static readonly ValueModifierTagId CarryItemJumpPenaltyTag = new ValueModifierTagId(10003);
        private const int MaxTrajectoryHits = 16;
        // アイテムを拾った後、プレイヤーが入力を離すまで次のアイテムを拾えないようにするためのフラグ。これがないと、例えば爆弾を投げた瞬間にもう一度掴んでしまう。
        private bool waitForPickupInputRelease;
        private bool isThrowChargePending;
        private bool isThrowCharging;
        private bool carryJumpPenaltyApplied;
        private bool activeInteractionFacingOwnsChannel;

        private readonly RaycastHit[] trajectoryHits = new RaycastHit[MaxTrajectoryHits];
        private readonly Collider[] trajectoryOverlapHits = new Collider[MaxTrajectoryHits];

        private ValueStoreService valueStore;
        private EntityRef entityRef;
        private ValueWatchHandle<bool> canInteractHandle;
        private ValueWatchHandle<bool> fatigueInteractHandle;
        private ICarryableItem currentlyHandledItem;
        private IEntityVelocitySource velocitySource;
        private PlayerInteractionController interactionController;
        private float emptyHandThrowPreviewTimer;
        private float throwChargePendingTimer;
        private float throwForceChargeTimer;
        private Vector3[] trajectoryPoints = new Vector3[32];
        private Material ownedTrajectoryMaterial;
        private Material ownedTrajectoryHitMarkerMaterial;
        private Transform trajectoryHitMarkerTransform;
        private Renderer trajectoryHitMarkerRenderer;
        private bool isEmptyHandThrowPreviewActive;
        private int throwSequence;
        private int lastConsumedInputPressSequence;

        public ICarryableItem CurrentHandledItem => currentlyHandledItem;
        public bool IsHandlingItem => isHandlingItem;
        public bool CanInteract => currentCanInteract;
        public ICarryableItem CurrentlyHandledItem => currentlyHandledItem;
        public bool IsInputPressed => interactionController != null && interactionController.IsInputPressed;
        public float InputHoldDuration => interactionController != null ? interactionController.InputHoldDuration : 0f;
        public int InputPressSequence => interactionController != null ? interactionController.InputPressSequence : 0;
        public int InputReleaseSequence => interactionController != null ? interactionController.InputReleaseSequence : 0;
        public bool HasCandidate => interactionController != null && interactionController.HasCandidate;
        public IInteractionTarget CurrentBestInteractable => interactionController != null ? interactionController.CurrentBestInteractable : null;
        public IInteractionTarget ActiveInteractable => interactionController != null ? interactionController.ActiveInteractable : null;
        public float ActiveHoldProgress => interactionController != null ? interactionController.ActiveHoldProgress : 0f;
        public Transform HandleItemPoint => handleItemPoint;
        public float HandleItemDistance => handleItemDistance;
        public IReadOnlyList<InteractionCandidate> Candidates =>
            interactionController != null ? interactionController.Candidates : Array.Empty<InteractionCandidate>();

        // throw系の状態
        public bool IsThrowCharging => isThrowCharging || isEmptyHandThrowPreviewActive;
        public float CurrentThrowForce => CalculateThrowForce();
        public float CurrentThrowChargeRatio => Mathf.Clamp01(throwForceChargeTimer / Mathf.Max(0.01f, throwForceChargeTime));
        public Action OnThrowChargeStart { get; set; }
        public Action OnThrowChargeEnd { get; set; }
        public event Action<ICarryableItem> CurrentHandledItemChanged;
        public event Action<InteractionEventData> InteractionEvent
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
            ResolveFacingController();

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
                () => entityRef,
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
            ClearInteractionFacing(force: true);
            interactionController?.ResetRuntimeState();
            interactionController?.Unbind();
            handleItemAction?.action?.Disable();
            HideTrajectory();
            RemoveCarryJumpPenalty();
            ResetEmptyHandThrowPreview();
        }

        private void Reset()
        {
            ResolveFacingController();
        }

        private void OnValidate()
        {
            ResolveFacingController();
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

            if (ownedTrajectoryHitMarkerMaterial != null)
            {
                Destroy(ownedTrajectoryHitMarkerMaterial);
                ownedTrajectoryHitMarkerMaterial = null;
            }
        }

        private void Update()
        {
            if (handleItemAction == null || handleItemAction.action == null)
                return;

            if (handleItemPoint == null || playerModel == null)
                return;

            bool canInteract = RefreshInteractionGateDebugValue();
            interactionController?.Tick(Time.deltaTime, canInteract && !isHandlingItem);

            if (!canInteract)
            {
                CancelInputDrivenItemAction();
                PublishRuntimeValues();
                return;
            }

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
            if (currentlyHandledItem == null ||
                currentlyHandledItem.ItemTransform == null ||
                !currentlyHandledItem.ItemTransform.gameObject.activeInHierarchy)
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

            // 2回目の押下直後はまだ「置く」か「投げる」か確定していないので、
            // 短い保持猶予を経てから初めて投げチャージへ遷移する。
            if (isThrowChargePending)
            {
                HideTrajectory();

                if (!IsInputPressed)
                {
                    ReleaseCurrentItem(HeldItemReleaseKind.Drop);
                    return;
                }

                throwChargePendingTimer += Time.deltaTime;
                float activationHoldTime = Mathf.Max(0.01f, throwChargeActivationHoldTime);
                if (throwChargePendingTimer < activationHoldTime)
                    return;

                StartThrowCharge(throwChargePendingTimer - activationHoldTime);
                return;
            }

            // まだ投げチャージに入っていない。
            // 2回目の押下でまず pending に入り、短押しならドロップ、長押しで投げに切り替わる。
            if (!isThrowCharging)
            {
                HideTrajectory();

                if (ConsumeInputPress())
                {
                    BeginThrowChargePending();
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
            EndThrowCharge();
            ReleaseCurrentItem(HeldItemReleaseKind.Throw);
        }

        private void BeginThrowChargePending()
        {
            isThrowChargePending = true;
            throwChargePendingTimer = 0f;
            throwForceChargeTimer = 0f;
        }

        private void StartThrowCharge(float initialChargeTime)
        {
            isThrowChargePending = false;
            throwChargePendingTimer = 0f;

            if (isThrowCharging)
                return;

            isThrowCharging = true;
            throwForceChargeTimer = Mathf.Max(0f, initialChargeTime);
            OnThrowChargeStart?.Invoke();
            UpdateThrowTrajectory();
        }

        private void EndThrowCharge()
        {
            if (!isThrowCharging)
                return;

            isThrowCharging = false;
            OnThrowChargeEnd?.Invoke();
        }

        private void HandleItem(ICarryableItem item)
        {
            if (item == null || item.ItemTransform == null)
                return;

            if (item is BombMB bomb && GameLogicManagerMB.Instance != null)
            {
                // current bomb の追従だけ行い、retry checkpoint は起爆開始時に確定する。
                GameLogicManagerMB.Instance.SetCurrentBomb(bomb);
            }

            interactionController?.ClearCandidateState();
            ResetEmptyHandThrowPreview();

            SetCurrentHandledItem(item);

            isHandlingItem = true;
            throwForceChargeTimer = 0f;

            // 拾った時の押下シーケンスをここで消費済みにしないと、
            // ボタンを離した直後に「2回目の押下」と誤判定して即投げへ進んでしまう。
            lastConsumedInputPressSequence = InputPressSequence;

            // 拾った時の押下を、投げチャージに流用させない。
            waitForPickupInputRelease = true;
            isThrowChargePending = false;
            isThrowCharging = false;
            throwChargePendingTimer = 0f;

            item.OnHandle(handleItemPoint);
            ApplyCarryJumpPenaltyIfNeeded(item);

            PublishRuntimeValues();
        }

        private void ReleaseCurrentItem(HeldItemReleaseKind releaseKind)
        {
            if (currentlyHandledItem == null)
            {
                ClearHeldState();
                return;
            }

            Vector3 releaseVelocity = releaseKind == HeldItemReleaseKind.Throw
                ? BuildThrowVelocity()
                : BuildDropVelocity();

            currentlyHandledItem.OnRelease(releaseVelocity);

            if (releaseKind == HeldItemReleaseKind.Throw)
                throwSequence++;

            ClearHeldState();
        }

        public bool ForceReleaseCurrentItem(Vector3 releaseVelocity)
        {
            if (currentlyHandledItem == null)
                return false;

            if (isThrowCharging)
            {
                isThrowCharging = false;
                OnThrowChargeEnd?.Invoke();
            }

            // 着地ショックでの手放しは「投げた」扱いにしない。
            // 投擲トリガーを増やすと着地アニメーションと競合するため、throwSequence は進めない。
            currentlyHandledItem.OnRelease(releaseVelocity);
            ClearHeldState();
            return true;
        }

        public bool TryGetHeldItemTag(out EntityTagId heldItemTag)
        {
            heldItemTag = default;

            if (currentlyHandledItem == null || currentlyHandledItem.ItemTransform == null)
                return false;

            EntityMB heldItemEntity = currentlyHandledItem.ItemTransform.GetComponentInParent<EntityMB>();

            if (heldItemEntity == null || !heldItemEntity.Tag.IsValid)
                return false;

            heldItemTag = heldItemEntity.Tag;
            return true;
        }

        public bool IsHoldingItemWithTag(EntityTagId tagId)
        {
            return tagId.IsValid &&
                   TryGetHeldItemTag(out EntityTagId heldItemTag) &&
                   heldItemTag.Equals(tagId);
        }

        private void ClearHeldState()
        {
            SetCurrentHandledItem(null);

            isHandlingItem = false;
            isThrowChargePending = false;
            throwForceChargeTimer = 0f;
            throwChargePendingTimer = 0f;
            waitForPickupInputRelease = false;
            isThrowCharging = false;

            ResetEmptyHandThrowPreview();
            HideTrajectory();
            RemoveCarryJumpPenalty();
            PublishRuntimeValues();
        }

        public void RestoreRetryCheckpointState()
        {
            // Retry 復帰では item 側の transform / rigidbody を checkpoint restore に任せる。
            // Player 側は所持状態と throw 入力状態だけを安全に初期化する。
            ClearHeldState();
        }

        private void SetCurrentHandledItem(ICarryableItem item)
        {
            if (EqualityComparer<ICarryableItem>.Default.Equals(currentlyHandledItem, item))
                return;

            currentlyHandledItem = item;
            CurrentHandledItemChanged?.Invoke(currentlyHandledItem);
        }

        private float CalculateThrowForce()
        {
            float chargeRatio = Mathf.Clamp01(throwForceChargeTimer / Mathf.Max(0.01f, throwForceChargeTime));
            return Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);
        }

        private Vector3 BuildThrowVelocity()
        {
            return BuildThrowDirection() * CalculateThrowForce() + GetCarrierVelocity();
        }

        private Vector3 BuildDropVelocity()
        {
            Vector3 carrierVelocity = GetCarrierVelocity();
            carrierVelocity.y = 0f;
            carrierVelocity *= Mathf.Clamp01(dropCarrierPlanarVelocityFactor);

            return BuildDropDirection() * Mathf.Max(0f, dropForwardVelocity) + carrierVelocity;
        }

        private Vector3 GetCarrierVelocity()
        {
            if (velocitySource == null)
                velocitySource = ResolveVelocitySource();

            return velocitySource != null ? velocitySource.CurrentVelocity : Vector3.zero;
        }

        private IEntityVelocitySource ResolveVelocitySource()
        {
            MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IEntityVelocitySource source)
                    return source;
            }

            return null;
        }

        private Vector3 BuildThrowDirection()
        {
            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;

            if (mainCamera != null && mainCamera.transform.forward.sqrMagnitude > 0.0001f)
            {
                // カメラ forward をそのまま使うと低めに落ちやすいので、少しだけ上方向へ補正する。
                return ApplyThrowUpwardCompensation(
                    mainCamera.transform.forward,
                    mainCamera.transform.right,
                    throwUpwardCompensationAngle);
            }

            Vector3 fallbackDirection = playerModel != null
                ? playerModel.transform.forward
                : transform.forward;

            fallbackDirection.y = 0.0f;

            if (fallbackDirection.sqrMagnitude <= 0.0001f)
                fallbackDirection = transform.forward;

            return ApplyThrowUpwardCompensation(
                fallbackDirection,
                transform.right,
                throwUpwardCompensationAngle);
        }

        private Vector3 BuildDropDirection()
        {
            Vector3 forward = playerModel != null
                ? playerModel.transform.forward
                : transform.forward;

            forward.y = 0f;

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = transform.forward;
                forward.y = 0f;
            }

            if (forward.sqrMagnitude <= 0.0001f)
                return Vector3.forward;

            return forward.normalized;
        }

        private static Vector3 ApplyThrowUpwardCompensation(Vector3 forward, Vector3 rightAxis, float upwardCompensationAngle)
        {
            Vector3 normalizedForward = forward.normalized;
            if (normalizedForward.sqrMagnitude <= 0.0001f)
                return Vector3.forward;

            Vector3 normalizedRight = rightAxis.normalized;
            if (normalizedRight.sqrMagnitude <= 0.0001f)
                normalizedRight = Vector3.right;

            // Unityの右手軸回転では負方向がカメラforwardを上へ起こす向きになる。
            Quaternion compensationRotation = Quaternion.AngleAxis(-upwardCompensationAngle, normalizedRight);
            return (compensationRotation * normalizedForward).normalized;
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
            bool hasTrajectoryHit = false;
            Vector3 trajectoryHitPoint = Vector3.zero;

            // 物理位置を点列に落とし込み、途中でColliderに当たったらそこでLineを止める。
            trajectoryPoints[0] = origin;
            int usedPointCount = 1;

            for (int i = 1; i < pointCount; i++)
            {
                float time = trajectoryTimeStep * i;
                Vector3 next = origin + velocity * time + 0.5f * Physics.gravity * time * time;

                if (TryFindTrajectoryHit(previous, next, out Vector3 hitPoint))
                {
                    hasTrajectoryHit = true;
                    trajectoryHitPoint = hitPoint;
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

            if (hasTrajectoryHit && Vector3.Distance(origin, trajectoryHitPoint) >= trajectoryHitMarkerMinDistance)
            {
                ShowTrajectoryHitMarker(trajectoryHitPoint);
            }
            else
            {
                HideTrajectoryHitMarker();
            }
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
            QueryTriggerInteraction triggerInteraction = includeTriggerCollidersInTrajectory
                ? QueryTriggerInteraction.Collide
                : QueryTriggerInteraction.Ignore;

            // 始点がすでに collider に重なっている場合、SphereCast だけでは取りこぼすことがある。
            int overlapCount = Physics.OverlapSphereNonAlloc(
                from,
                radius,
                trajectoryOverlapHits,
                trajectoryCollisionMask,
                triggerInteraction);

            for (int i = 0; i < overlapCount; i++)
            {
                Collider overlapCollider = trajectoryOverlapHits[i];

                if (overlapCollider == null || IsIgnoredTrajectoryCollider(overlapCollider))
                    continue;

                // MarkerはLineの中心ではなく、Collider表面に近い位置へ置く。
                hitPoint = overlapCollider.ClosestPoint(from);
                return true;
            }

            int hitCount = Physics.SphereCastNonAlloc(
                from,
                radius,
                direction,
                trajectoryHits,
                distance,
                trajectoryCollisionMask,
                triggerInteraction);

            bool found = false;
            float bestDistance = float.MaxValue;
            Vector3 bestPoint = to;

            // NonAllocの結果は距離順とは限らないため、明示的に一番手前のHitを選ぶ。
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = trajectoryHits[i];

                if (hit.collider == null || IsIgnoredTrajectoryCollider(hit.collider))
                    continue;

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestPoint = hit.point;
                    found = true;
                }
            }

            if (!found)
                return false;

            hitPoint = bestPoint;
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

            // 予測線がプレイヤー自身のCharacterControllerに当たって即終了しないよう、所持者Rootを除外する。
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

        private void EnsureTrajectoryHitMarker()
        {
            if (trajectoryHitMarkerTransform == null)
            {
                GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                markerObject.name = "Bomb Throw Hit Marker";
                markerObject.transform.SetParent(transform, false);

                Collider markerCollider = markerObject.GetComponent<Collider>();
                if (markerCollider != null)
                    Destroy(markerCollider);

                trajectoryHitMarkerTransform = markerObject.transform;
                trajectoryHitMarkerRenderer = markerObject.GetComponent<Renderer>();
            }

            trajectoryHitMarkerTransform.localScale = Vector3.one * trajectoryHitMarkerDiameter;

            if (trajectoryHitMarkerRenderer == null)
                return;

            if (ownedTrajectoryHitMarkerMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

                if (shader == null)
                    shader = Shader.Find("Standard");

                if (shader != null)
                {
                    ownedTrajectoryHitMarkerMaterial = new Material(shader);
                }
            }

            if (ownedTrajectoryHitMarkerMaterial != null)
            {
                ownedTrajectoryHitMarkerMaterial.color = trajectoryHitMarkerColor;
                trajectoryHitMarkerRenderer.sharedMaterial = ownedTrajectoryHitMarkerMaterial;
            }
        }

        private void ShowTrajectoryHitMarker(Vector3 hitPoint)
        {
            EnsureTrajectoryHitMarker();

            if (trajectoryHitMarkerTransform == null)
                return;

            // 予想着地点を見失わないよう、ヒット地点にだけ小さな球を表示する。
            trajectoryHitMarkerTransform.position = hitPoint;
            trajectoryHitMarkerTransform.gameObject.SetActive(true);
        }

        private void HideTrajectoryHitMarker()
        {
            if (trajectoryHitMarkerTransform == null)
                return;

            trajectoryHitMarkerTransform.gameObject.SetActive(false);
        }

        private void HideTrajectory()
        {
            if (trajectoryLineRenderer == null)
            {
                HideTrajectoryHitMarker();
                return;
            }

            trajectoryLineRenderer.enabled = false;
            trajectoryLineRenderer.positionCount = 0;
            HideTrajectoryHitMarker();
        }

        private void HandleInteractionEvent(InteractionEventData eventData)
        {
            switch (eventData.EventType)
            {
                case InteractionEventType.Started:
                    TryApplyInteractionFacing(eventData);
                    break;

                case InteractionEventType.Canceled:
                    ClearInteractionFacing(force: false);
                    break;

                case InteractionEventType.Completed:
                    ClearInteractionFacing(force: true);

                    if (eventData.Interactable is CarryableItemInteractableAdapter adapter)
                        HandleItem(adapter.CarryableItem);
                    break;
            }
        }

        private void TryApplyInteractionFacing(InteractionEventData eventData)
        {
            if (!faceInteractionTargetOnStart)
                return;

            if (eventData.Interactable is not IInteractionFacingTarget facingTarget || !facingTarget.AllowInteractionSourceFacing)
                return;

            Transform targetTransform = facingTarget.InteractionFacingTransform;
            EntityFacingControllerMB resolvedFacingController = ResolveFacingController();

            if (resolvedFacingController == null || targetTransform == null)
                return;

            resolvedFacingController.SetFacingTargetTransform(
                EntityFacingChannels.Interaction,
                targetTransform,
                EntityFacingPriorities.Interaction);
            activeInteractionFacingOwnsChannel = true;
        }

        private void ClearInteractionFacing(bool force)
        {
            if (!force && !activeInteractionFacingOwnsChannel)
                return;

            activeInteractionFacingOwnsChannel = false;
            ResolveFacingController()?.ClearFacing(EntityFacingChannels.Interaction);
        }

        private EntityFacingControllerMB ResolveFacingController()
        {
            if (facingController == null)
                facingController = GetComponentInParent<EntityFacingControllerMB>();

            return facingController;
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
            valueStore.Set(entityRef, ValueKeys.Runtime.CanInteract, currentCanInteract);
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

            if (canInteractHandle == null && valueStore != null && entityRef.IsValid)
                canInteractHandle = valueStore.GetHandle(entityRef, ValueKeys.Interaction.CanInteract);

            if (fatigueInteractHandle == null && valueStore != null && entityRef.IsValid)
                fatigueInteractHandle = valueStore.GetHandle(entityRef, ValueKeys.Runtime.IsFatigueInteracting);
        }

        private bool IsFatigueInteracting()
        {
            ResolveRuntimeReferences(logMissingEntity: false);
            return fatigueInteractHandle != null && fatigueInteractHandle.CurrentValue;
        }

        private bool RefreshInteractionGateDebugValue()
        {
            ResolveRuntimeReferences(logMissingEntity: false);
            currentCanInteract = canInteractHandle == null || canInteractHandle.CurrentValue;
            return currentCanInteract;
        }

        private void CancelInputDrivenItemAction()
        {
            isThrowChargePending = false;
            throwChargePendingTimer = 0f;
            EndThrowCharge();

            throwForceChargeTimer = 0f;
            ResetEmptyHandThrowPreview();
            HideTrajectory();
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

        private enum HeldItemReleaseKind
        {
            Drop,
            Throw
        }
    }
}

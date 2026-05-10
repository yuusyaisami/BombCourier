using BC.Base;
using BC.Bomb;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using BC.Rendering;

namespace BC.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerItemHandleStateMB : MonoBehaviour, IEntityHandleItemAnimationSource
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
        [SerializeField] private float maxThrowForce = 5f;
        [SerializeField] private float minThrowForce = 2f;
        [SerializeField] private float throwForceChargeTime = 2f;

        [Header("Runtime Debug")]
        [SerializeField] private bool isHandlingItem;


        private const int MaxItemHits = 32;
        // アイテムを拾った後、プレイヤーが入力を離すまで次のアイテムを拾えないようにするためのフラグ。これがないと、例えば爆弾を投げた瞬間にもう一度掴んでしまう。
        private bool waitForPickupInputRelease;
        private bool isThrowCharging;

        private readonly Collider[] itemHits = new Collider[MaxItemHits];
        private readonly List<IItemObject> pickupCandidates = new(16);
        private readonly HashSet<PickupOutlineTargetMB> outlinedTargets = new();

        private ValueStoreService valueStore;
        private EntityRef entityRef;
        private IItemObject currentlyHandledItem;
        private IItemObject currentBestItem;
        private float throwForceChargeTimer;

        public bool IsHandlingItem => isHandlingItem;

        private void Awake()
        {
            if (handleItemPoint == null)
                Debug.LogError($"{nameof(PlayerItemHandleStateMB)}: handleItemPoint is not assigned.", this);

            if (playerModel == null)
                Debug.LogError($"{nameof(PlayerItemHandleStateMB)}: playerModel is not assigned.", this);

            if (handleItemAction == null || handleItemAction.action == null)
                Debug.LogError($"{nameof(PlayerItemHandleStateMB)}: handleItemAction is not assigned.", this);

            SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();
            if (kernelMB != null && kernelMB.Kernel != null)
                valueStore = kernelMB.Kernel.ValueStore;

            EntityMB entityMB = GetComponentInParent<EntityMB>();
            if (entityMB != null && entityMB.HasEntity)
            {
                entityRef = entityMB.Entity;
            }
            else
            {
                Debug.LogWarning($"{nameof(PlayerItemHandleStateMB)}: EntityMB is not found or not bound.", this);
            }
        }

        private void OnEnable()
        {
            handleItemAction?.action?.Enable();
        }

        private void OnDisable()
        {
            handleItemAction?.action?.Disable();
            ClearPickupOutlines();
        }

        private void Update()
        {
            if (handleItemAction == null || handleItemAction.action == null)
                return;

            if (handleItemPoint == null || playerModel == null)
                return;

            if (!isHandlingItem)
            {
                RefreshPickupCandidates();
                TickPickup();
            }
            else
            {
                ClearPickupOutlines();
                TickThrow();
            }

            PublishRuntimeValues();
        }

        private void RefreshPickupCandidates()
        {
            pickupCandidates.Clear();
            currentBestItem = null;

            int hitCount = Physics.OverlapSphereNonAlloc(
                handleItemPoint.position,
                handleItemDistance,
                itemHits,
                itemLayerMask,
                QueryTriggerInteraction.Collide
            );

            float bestScore = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = itemHits[i];

                if (hit == null)
                    continue;

                IItemObject item = hit.GetComponentInParent<IItemObject>();

                if (item == null)
                    continue;

                if (item.IsHandled)
                    continue;

                Transform itemTransform = item.ItemTransform;

                if (itemTransform == null)
                    continue;

                Vector3 toItem = itemTransform.position - playerModel.transform.position;
                toItem.y = 0f;

                float sqrDistance = toItem.sqrMagnitude;

                if (sqrDistance <= 0.0001f)
                    continue;

                Vector3 playerForward = playerModel.transform.forward;
                playerForward.y = 0f;

                if (playerForward.sqrMagnitude <= 0.0001f)
                    continue;

                playerForward.Normalize();

                Vector3 directionToItem = toItem.normalized;
                float angle = Vector3.Angle(playerForward, directionToItem);

                if (angle > handleItemAngleThreshold)
                    continue;

                pickupCandidates.Add(item);

                float score = sqrDistance + angle * 0.05f;

                if (score < bestScore)
                {
                    bestScore = score;
                    currentBestItem = item;
                }
            }

            UpdatePickupOutlines(pickupCandidates, currentBestItem);
        }

        private void TickPickup()
        {
            if (!handleItemAction.action.WasPressedThisFrame())
                return;

            if (currentBestItem == null)
                return;

            HandleItem(currentBestItem);
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
                if (handleItemAction.action.IsPressed())
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
                if (handleItemAction.action.WasPressedThisFrame())
                {
                    isThrowCharging = true;
                    throwForceChargeTimer = 0f;
                }

                return;
            }

            // チャージ中。
            if (handleItemAction.action.IsPressed())
            {
                throwForceChargeTimer += Time.deltaTime;
                return;
            }

            // チャージ後に離したら投げる。
            ReleaseCurrentItem();
        }
        private void HandleItem(IItemObject item)
        {
            if (item == null || item.ItemTransform == null)
                return;

            ClearPickupOutlines();

            currentlyHandledItem = item;
            currentBestItem = null;
            pickupCandidates.Clear();

            isHandlingItem = true;
            throwForceChargeTimer = 0f;

            // 拾った時の押下を、投げチャージに流用させない。
            waitForPickupInputRelease = true;
            isThrowCharging = false;

            item.OnHandle(handleItemPoint);

            PublishRuntimeValues();
        }

        private void ReleaseCurrentItem()
        {
            if (currentlyHandledItem == null)
            {
                ClearHeldState();
                return;
            }

            float chargeRatio = Mathf.Clamp01(throwForceChargeTimer / Mathf.Max(0.01f, throwForceChargeTime));
            float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);

            Vector3 throwDirection = playerModel.transform.forward;
            throwDirection.y = 0f;

            if (throwDirection.sqrMagnitude <= 0.0001f)
            {
                throwDirection = transform.forward;
                throwDirection.y = 0f;
            }

            throwDirection.Normalize();

            currentlyHandledItem.OnRelease(throwDirection * throwForce);

            ClearHeldState();
        }

        private void ClearHeldState()
        {
            currentlyHandledItem = null;
            currentBestItem = null;
            pickupCandidates.Clear();

            isHandlingItem = false;
            throwForceChargeTimer = 0f;
            waitForPickupInputRelease = false;
            isThrowCharging = false;

            PublishRuntimeValues();
        }

        private void UpdatePickupOutlines(IReadOnlyList<IItemObject> candidates, IItemObject bestItem)
        {
            foreach (PickupOutlineTargetMB target in outlinedTargets)
            {
                if (target != null)
                    target.ClearOutline();
            }

            outlinedTargets.Clear();

            if (candidates == null)
                return;

            for (int i = 0; i < candidates.Count; i++)
            {
                IItemObject item = candidates[i];

                if (item is not MonoBehaviour itemMB)
                    continue;

                PickupOutlineTargetMB target = itemMB.GetComponentInParent<PickupOutlineTargetMB>();

                if (target == null)
                    continue;

                PickupOutlineKind kind = ReferenceEquals(item, bestItem)
                    ? PickupOutlineKind.Best
                    : PickupOutlineKind.Candidate;

                target.SetOutline(kind);
                outlinedTargets.Add(target);
            }
        }

        private void ClearPickupOutlines()
        {
            foreach (PickupOutlineTargetMB target in outlinedTargets)
            {
                if (target != null)
                    target.ClearOutline();
            }

            outlinedTargets.Clear();
        }

        private void PublishRuntimeValues()
        {
            if (valueStore == null || !entityRef.IsValid)
                return;

            valueStore.Set(entityRef, ValueKeys.Runtime.IsHandlingItem, isHandlingItem);
        }

        private void OnDrawGizmosSelected()
        {
            if (handleItemPoint == null)
                return;

            Gizmos.DrawWireSphere(handleItemPoint.position, handleItemDistance);
        }
    }
}
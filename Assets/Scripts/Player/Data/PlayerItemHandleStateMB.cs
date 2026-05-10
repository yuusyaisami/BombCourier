using BC.Base;
using BC.Bomb;
using BC.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

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
        private readonly Collider[] itemHits = new Collider[MaxItemHits];

        private ValueStoreService valueStore;
        private EntityRef entityRef;
        private IItemObject currentlyHandledItem;
        private float throwForceChargeTimer;

        public bool IsHandlingItem => isHandlingItem;

        private void Awake()
        {
            if (handleItemPoint == null)
            {
                Debug.LogError($"{nameof(PlayerItemHandleStateMB)}: handleItemPoint is not assigned.", this);
            }

            if (playerModel == null)
            {
                Debug.LogError($"{nameof(PlayerItemHandleStateMB)}: playerModel is not assigned.", this);
            }

            if (handleItemAction == null || handleItemAction.action == null)
            {
                Debug.LogError($"{nameof(PlayerItemHandleStateMB)}: handleItemAction is not assigned.", this);
            }

            SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();
            if (kernelMB != null && kernelMB.Kernel != null)
            {
                valueStore = kernelMB.Kernel.ValueStore;
            }

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
        }

        private void Update()
        {
            if (handleItemAction == null || handleItemAction.action == null)
                return;

            if (handleItemPoint == null || playerModel == null)
                return;

            if (!isHandlingItem)
            {
                TickPickup();
            }
            else
            {
                TickThrow();
            }

            PublishRuntimeValues();
        }

        private void TickPickup()
        {
            if (!handleItemAction.action.WasPressedThisFrame())
                return;

            if (!TryFindBestItem(out IItemObject item))
                return;

            HandleItem(item);
        }

        private void TickThrow()
        {
            if (currentlyHandledItem == null)
            {
                ClearHeldState();
                return;
            }

            if (handleItemAction.action.IsPressed())
            {
                throwForceChargeTimer += Time.deltaTime;
                return;
            }

            if (throwForceChargeTimer <= 0f)
                return;

            ReleaseCurrentItem();
        }

        private void HandleItem(IItemObject item)
        {
            if (item == null)
                return;

            currentlyHandledItem = item;
            isHandlingItem = true;
            throwForceChargeTimer = 0f;

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
            isHandlingItem = false;
            throwForceChargeTimer = 0f;

            PublishRuntimeValues();
        }

        private bool TryFindBestItem(out IItemObject bestItem)
        {
            bestItem = null;

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

                Vector3 directionToItem = toItem.normalized;
                Vector3 playerForward = playerModel.transform.forward;
                playerForward.y = 0f;

                if (playerForward.sqrMagnitude <= 0.0001f)
                    continue;

                playerForward.Normalize();

                float angle = Vector3.Angle(playerForward, directionToItem);
                if (angle > handleItemAngleThreshold)
                    continue;

                // 正面に近く、距離が近いものを優先する。
                float score = sqrDistance + angle * 0.05f;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestItem = item;
                }
            }

            return bestItem != null;
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
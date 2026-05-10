using System.Collections.Generic;
using BC.Base;
using BC.Bomb;
using BC.Utility;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BC.Player
{
    public sealed class PlayerItemHandleStateMB : MonoBehaviour, IEntityHandleItemAnimationSource
    {
        [SerializeField] private float handleItemDistance = 1.5f; // アイテムを扱える距離
        [SerializeField] private float handleItemAngleThreshold = 45f; // アイテムを扱える角度の閾値
        [SerializeField] private LayerMask itemLayerMask; // アイテムを検出するためのレイヤーマスク
        [SerializeField] private float handleItemPressDuration = 0.5f; // アイテムを扱うために必要なボタン押下時間
        [SerializeField] private float maxThrowForce = 5f; // アイテムを放るときの力
        [SerializeField] private float minThrowForce = 2f; // アイテムを放るときの最小の力  
        [SerializeField] private float throwForceChargeTime = 2f; // アイテムを放るときの力が最大になるまでの時間
        [SerializeField] private InputActionReference handleItemAction; // アイテムを扱うためのInputActionReference
        [SerializeField] private bool isHandlingItem;
        [SerializeField] private Transform handleItemPoint; // アイテムを扱う位置のTransform
        [SerializeField] private GameObject playerModel; // プレイヤーモデルのGameObject (これが向きのなる)

        public bool IsHandlingItem => isHandlingItem;
        private float handleItemPressTimer;
        private float throwForceChargeTimer;
        private ValueStoreService valueStore;
        private EntityRef entityRef;
        private IItemObject currentlyHandledItem;
        private void OnEnable()
        {
            if (handleItemAction != null && handleItemAction.action != null)
            {
                handleItemAction.action.Enable();
            }
        }
        private void OnDisable()
        {
            if (handleItemAction != null && handleItemAction.action != null)
            {
                handleItemAction.action.Disable();
            }
        }
        private void Start()
        {
            // 初期状態の設定などがあればここで行う
            isHandlingItem = false;
            handleItemPressTimer = 0f;

            valueStore = GetComponent<SceneKernelMB>().Kernel.ValueStore;
            EntityMB entityMB = GetComponentInParent<EntityMB>();
            if (entityMB != null && entityMB.HasEntity)
            {
                entityRef = entityMB.Entity;
            }
            else
            {
                Debug.LogError($"{nameof(PlayerItemHandleStateMB)}: EntityMB is not found or not bound.", this);
            }
        }

        private void Update()
        {
            // 持っているアイテムが有効かどうか
            if (isHandlingItem)
            {
                if (currentlyHandledItem == null)
                {
                    isHandlingItem = false;
                    PublishRuntimeValues();
                    return;
                }
            }
            // ここでアイテムを扱う入力をチェックして、isHandlingItemを更新する
            if (handleItemAction != null && handleItemAction.action != null)
            {
                if (!isHandlingItem)
                {
                    // アイテムを扱っている間の処理があればここに書く
                    List<IItemObject> canHandleItems = UpdateCanHandleItemDetect();

                    if (handleItemAction.action.IsPressed() && canHandleItems.Count > 0)
                    {
                        handleItemPressTimer += Time.deltaTime;

                        if (handleItemPressTimer >= handleItemPressDuration)
                        {
                            isHandlingItem = true;
                        }
                    }
                    else
                    {
                        handleItemPressTimer = 0f;
                        isHandlingItem = false;
                    }
                }
                else
                {
                    if (handleItemAction.action.IsPressed())
                    {
                        throwForceChargeTimer += Time.deltaTime;
                    }
                    else if (throwForceChargeTimer > 0f)
                    {
                        // アイテムを放る処理
                        float chargeRatio = Mathf.Clamp01(throwForceChargeTimer / throwForceChargeTime);
                        float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);
                        ReleaseItem(currentlyHandledItem);
                        isHandlingItem = false;
                        handleItemPressTimer = 0f;
                        throwForceChargeTimer = 0f;
                    }
                }
            }
            PublishRuntimeValues();
        }
        private void PublishRuntimeValues()
        {
            if (valueStore != null)
            {
                valueStore.Set(entityRef, ValueKeys.Runtime.IsHandlingItem, isHandlingItem);
            }
        }
        // アイテム取得
        public void HandleItem(IItemObject item)
        {
            // アイテムのSetParent
            if (item is MonoBehaviour itemMB)
            {
                itemMB.transform.SetParent(handleItemPoint);
                itemMB.transform.localPosition = Vector3.zero;
                itemMB.transform.localRotation = Quaternion.identity;
            }
            currentlyHandledItem = item;

            item.OnHandle();
        }
        public void ReleaseItem(IItemObject item)
        {

            // アイテムのSetParentをnullにしてワールドに戻す
            if (item is MonoBehaviour itemMB)
            {
                itemMB.transform.SetParent(null);
                // modelが向いている方向にアイテムを放る
                Vector3 throwDirection = playerModel.transform.forward;
                if (itemMB.TryGetComponent<Rigidbody>(out Rigidbody itemRb))
                {
                    float chargeRatio = Mathf.Clamp01(throwForceChargeTimer / throwForceChargeTime);
                    float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);
                    itemRb.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);
                }
            }
            currentlyHandledItem = null;
            isHandlingItem = false;
            handleItemPressTimer = 0f;
            throwForceChargeTimer = 0f;
            PublishRuntimeValues();
        }

        // 特定の範囲内を取得
        public List<IItemObject> UpdateCanHandleItemDetect()
        {
            var result = new List<IItemObject>();

            Collider[] hitColliders = Physics.OverlapSphere(handleItemPoint.position, handleItemDistance);
            foreach (var hitCollider in hitColliders)
            {
                IItemObject item = hitCollider.GetComponent<IItemObject>();
                if (item != null)
                {
                    Vector3 directionToItem = (hitCollider.transform.position - playerModel.transform.position).normalized;
                    float angleToItem = Vector3.Angle(playerModel.transform.forward, directionToItem);

                    if (angleToItem <= handleItemAngleThreshold)
                    {
                        result.Add(item);
                    }
                }
            }
            return result;
        }
    }
}
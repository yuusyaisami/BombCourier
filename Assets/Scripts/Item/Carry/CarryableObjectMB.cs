using System.Collections.Generic;
using BC.Base;
using BC.Player;
using UnityEngine;

namespace BC.Item
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public sealed class CarryableObjectMB : MonoBehaviour, ICarryableItem, ICarryReleaseOwnerCollisionGuard, IInteractionPromptDetailTextProvider
    {
        [Header("Carry")]
        [Tooltip("このオブジェクトを持ち運び可能にするかを指定します。")]
        [SerializeField] private bool canBeCarried = true;

        [Header("Prompt")]
        [Tooltip("プロンプト詳細表示に使う任意テキストです。空なら詳細表示は出しません。")]
        [SerializeField, TextArea] private string promptDetailText = string.Empty;

        private Rigidbody rb;
        private Collider objectCollider;
        private bool isHandled;
        private float ignoreOwnerCollisionUntilTime;
        private readonly List<Collider> ignoredHolderColliders = new(16);

        public Transform ItemTransform => transform;
        public bool IsHandled => isHandled;
        public bool CanBeCarried => canBeCarried && enabled && gameObject.activeInHierarchy;
        public string PromptDetailText => promptDetailText ?? string.Empty;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            objectCollider = GetComponent<Collider>();
        }

        private void OnDisable()
        {
            ignoreOwnerCollisionUntilTime = 0f;
            ClearIgnoredHolderCollisions();
        }

        private void Update()
        {
            if (ignoreOwnerCollisionUntilTime <= 0f)
                return;

            if (Time.time < ignoreOwnerCollisionUntilTime)
                return;

            ignoreOwnerCollisionUntilTime = 0f;
            ClearIgnoredHolderCollisions();
        }

        public void OnHandle(Transform handlePoint)
        {
            if (!CanBeCarried || handlePoint == null)
                return;

            isHandled = true;

            // 掴んでいる間は物理挙動を止め、ハンドル位置に追従させる。
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.detectCollisions = true;
            rb.useGravity = false;

            ConfigureHeldHolderCollisionIgnore(handlePoint);

            transform.SetParent(handlePoint, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void OnRelease(Vector3 throwVelocity)
        {
            if (!isHandled)
                return;

            isHandled = false;
            ignoreOwnerCollisionUntilTime = 0f;
            ClearIgnoredHolderCollisions();
            transform.SetParent(null, true);

            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(throwVelocity, ForceMode.VelocityChange);
        }

        public void IgnoreOwnerCollisionAfterRelease(Transform ownerRoot, float durationSeconds)
        {
            if (ownerRoot == null || objectCollider == null || durationSeconds <= 0f)
                return;

            ConfigureHeldHolderCollisionIgnore(ownerRoot);
            ignoreOwnerCollisionUntilTime = Mathf.Max(ignoreOwnerCollisionUntilTime, Time.time + durationSeconds);
        }

        private void ConfigureHeldHolderCollisionIgnore(Transform handlePoint)
        {
            ClearIgnoredHolderCollisions();

            if (handlePoint == null || objectCollider == null)
                return;

            EntityMB ownerEntity = handlePoint.GetComponentInParent<EntityMB>();
            Transform ownerRoot = ownerEntity != null ? ownerEntity.transform : handlePoint.root;

            if (ownerRoot == null)
                return;

            Collider[] ownerColliders = ownerRoot.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < ownerColliders.Length; i++)
            {
                Collider ownerCollider = ownerColliders[i];

                if (!CanIgnoreHolderCollider(ownerCollider))
                    continue;

                Physics.IgnoreCollision(objectCollider, ownerCollider, true);
                ignoredHolderColliders.Add(ownerCollider);
            }
        }

        private bool CanIgnoreHolderCollider(Collider ownerCollider)
        {
            if (ownerCollider == null ||
                ownerCollider == objectCollider ||
                !ownerCollider.enabled ||
                !ownerCollider.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (ownerCollider.transform.IsChildOf(transform))
                return false;

            return !ignoredHolderColliders.Contains(ownerCollider);
        }

        private void ClearIgnoredHolderCollisions()
        {
            if (objectCollider != null)
            {
                for (int i = 0; i < ignoredHolderColliders.Count; i++)
                {
                    Collider ignored = ignoredHolderColliders[i];

                    if (ignored != null)
                        Physics.IgnoreCollision(objectCollider, ignored, false);
                }
            }

            ignoredHolderColliders.Clear();
        }
    }
}
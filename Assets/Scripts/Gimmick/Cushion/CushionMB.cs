using System.Collections.Generic;
using BC.Base;
using BC.Item;
using UnityEngine;

namespace BC.Gimmick.Cushion
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public sealed class CushionMB : MonoBehaviour, ICarryableItem, ICushionImpactSource
    {
        [Header("Carry")]
        [Tooltip("このクッションを持ち運び可能にするかを指定します。")]
        [SerializeField] private bool canBeCarried = true;

        private Rigidbody rb;
        private Collider cushionCollider;
        private EntityMB entityMB;
        private bool isHandled;
        private readonly List<Collider> ignoredHolderColliders = new(16);

        public Transform ItemTransform => transform;
        public bool IsHandled => isHandled;
        public bool CanBeCarried => canBeCarried && enabled && gameObject.activeInHierarchy;
        public Transform CushionImpactRoot => transform;
        public EntityTagId CushionImpactTag => ResolveImpactTag();

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            cushionCollider = GetComponent<Collider>();
            entityMB = GetComponentInParent<EntityMB>();
        }

        private void OnDisable()
        {
            ClearIgnoredHolderCollisions();
        }

        public void OnHandle(Transform handlePoint)
        {
            if (!CanBeCarried || handlePoint == null)
                return;

            isHandled = true;

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
            ClearIgnoredHolderCollisions();
            transform.SetParent(null, true);

            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(throwVelocity, ForceMode.VelocityChange);
        }

        public bool HandleCushionImpact(CushionImpactData impactData, CushionImpactResult impactResult)
        {
            if (!impactResult.IsHandled || rb == null)
                return false;

            isHandled = false;
            ClearIgnoredHolderCollisions();
            return CushionRigidbodyImpactApplier.Apply(transform, rb, impactResult);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isHandled || rb == null || cushionCollider == null)
                return;

            CushionSurfaceMB surface = collision.collider.GetComponentInParent<CushionSurfaceMB>();

            if (surface == null)
                return;

            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            float impactForce = collision.impulse.magnitude / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            CushionImpactData impactData = new CushionImpactData(
                gameObject,
                transform,
                entityMB,
                CushionImpactTag,
                rb,
                cushionCollider,
                collision.contactCount > 0 ? contact.point : transform.position,
                collision.contactCount > 0 ? contact.normal : -collision.relativeVelocity.normalized,
                rb.linearVelocity,
                impactForce);

            if (surface.TryEvaluate(impactData, out CushionImpactResult result))
            {
                HandleCushionImpact(impactData, result);
            }
        }

        private EntityTagId ResolveImpactTag()
        {
            if (entityMB != null && entityMB.Tag.IsValid)
                return entityMB.Tag;

            return EntityTags.Gimmick.Cushion.Id;
        }

        private void ConfigureHeldHolderCollisionIgnore(Transform handlePoint)
        {
            ClearIgnoredHolderCollisions();

            if (handlePoint == null || cushionCollider == null)
                return;

            CharacterController ownerController = handlePoint.GetComponentInParent<CharacterController>();
            Transform ownerRoot = ownerController != null
                ? ownerController.transform
                : handlePoint.root;

            if (ownerRoot == null)
                return;

            Collider[] ownerColliders = ownerRoot.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < ownerColliders.Length; i++)
            {
                Collider ownerCollider = ownerColliders[i];

                if (!CanIgnoreHolderCollider(ownerCollider))
                    continue;

                Physics.IgnoreCollision(cushionCollider, ownerCollider, true);
                ignoredHolderColliders.Add(ownerCollider);
            }
        }

        private bool CanIgnoreHolderCollider(Collider ownerCollider)
        {
            if (ownerCollider == null ||
                ownerCollider == cushionCollider ||
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
            if (cushionCollider != null)
            {
                for (int i = 0; i < ignoredHolderColliders.Count; i++)
                {
                    Collider ignored = ignoredHolderColliders[i];

                    if (ignored != null)
                        Physics.IgnoreCollision(cushionCollider, ignored, false);
                }
            }

            ignoredHolderColliders.Clear();
        }
    }
}

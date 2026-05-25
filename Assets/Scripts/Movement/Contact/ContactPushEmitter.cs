using UnityEngine;

namespace BC.Base
{
    public static class ContactPushEmitter
    {
        public static void TryApply(
            in MoveContactInfo contact,
            Transform actorTransform,
            Vector3 currentVelocity,
            bool pushRigidbodiesOnContact,
            bool isDead,
            bool motionLocked,
            float minContactPushSpeed,
            float contactPushImpulse,
            float contactPushSpeedMultiplier,
            float maxContactPushImpulse)
        {
            if (!pushRigidbodiesOnContact || isDead || motionLocked)
                return;

            if (contact.Collider == null || contact.Collider.transform.IsChildOf(actorTransform))
                return;

            Vector3 pushDirection = ResolveDirection(contact.Collider, actorTransform, currentVelocity, contact.Normal);
            if (pushDirection.sqrMagnitude <= 0.0001f)
                return;

            float pushSpeed = new Vector3(currentVelocity.x, 0.0f, currentVelocity.z).magnitude;
            if (pushSpeed < minContactPushSpeed)
                return;

            float pushImpulse = Mathf.Min(maxContactPushImpulse, contactPushImpulse + pushSpeed * contactPushSpeedMultiplier);
            if (pushImpulse <= 0.0f)
                return;

            EntityImpactData impactData = new EntityImpactData(
                EntityImpactKind.Contact,
                actorTransform.gameObject,
                actorTransform,
                contact.Collider,
                contact.Point,
                pushDirection,
                pushImpulse);

            EntityImpactResponseMB impactResponse = contact.Collider.GetComponentInParent<EntityImpactResponseMB>();
            if (impactResponse == null)
                return;

            impactResponse.TryApplyImpact(impactData);
        }

        private static Vector3 ResolveDirection(Collider collisionCollider, Transform actorTransform, Vector3 currentVelocity, Vector3 contactNormal)
        {
            Vector3 direction = currentVelocity;
            direction.y = 0.0f;

            if (direction.sqrMagnitude <= 0.0001f)
                direction = -Vector3.ProjectOnPlane(contactNormal, Vector3.up);

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = collisionCollider != null
                    ? collisionCollider.transform.position - actorTransform.position
                    : Vector3.zero;
                direction.y = 0.0f;
            }

            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        }
    }
}

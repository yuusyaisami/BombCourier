using UnityEngine;

namespace BC.Base
{
    public static class MovementBodyResolver
    {
        public static void ResolveAndConfigure(
            GameObject owner,
            ref Rigidbody bodyRigidbody,
            ref CapsuleCollider bodyCollider,
            out CharacterController legacyCharacterController)
        {
            legacyCharacterController = null;

            if (owner == null)
                return;

            if (bodyRigidbody == null)
                bodyRigidbody = owner.GetComponent<Rigidbody>();

            if (bodyCollider == null)
                bodyCollider = owner.GetComponent<CapsuleCollider>();

            legacyCharacterController = owner.GetComponent<CharacterController>();

            if (bodyCollider == null)
            {
                bodyCollider = owner.AddComponent<CapsuleCollider>();

                if (legacyCharacterController != null)
                {
                    bodyCollider.center = legacyCharacterController.center;
                    bodyCollider.radius = legacyCharacterController.radius;
                    bodyCollider.height = legacyCharacterController.height;
                }
            }

            if (bodyRigidbody == null)
                bodyRigidbody = owner.AddComponent<Rigidbody>();

            ConfigureRigidbody(bodyRigidbody);

            if (legacyCharacterController != null)
                legacyCharacterController.enabled = false;
        }

        private static void ConfigureRigidbody(Rigidbody bodyRigidbody)
        {
            if (bodyRigidbody == null)
                return;

            bodyRigidbody.useGravity = false;
            bodyRigidbody.isKinematic = false;
            bodyRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            bodyRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            bodyRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
}
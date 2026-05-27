using UnityEngine;
using BC.Base;

namespace BC.Gimmick.Cushion
{
    public static class CushionRigidbodyImpactApplier
    {
        public static void ApplyBounceToVelocityChannels(ref VelocityChannels channels, Vector3 bounceVelocity)
        {
            channels.InputPlanar = Vector3.zero;
            // Keep vertical bounce in Vertical so gravity and grounded logic treat it like a jump arc.
            channels.Vertical = bounceVelocity.y;
            channels.External = new Vector3(bounceVelocity.x, 0.0f, bounceVelocity.z);
            channels.InheritedSupport = Vector3.zero;
        }

        public static bool Apply(Transform targetTransform, Rigidbody rb, CushionImpactResult impactResult)
        {
            if (targetTransform == null || rb == null || !impactResult.IsHandled)
                return false;

            switch (impactResult.ResponseKind)
            {
                case CushionResponseKind.Bounce:
                    ApplyBounce(targetTransform, rb, impactResult.BounceVelocity);
                    return true;

                case CushionResponseKind.StopAndAttach:
                    ApplyStopAndAttach(targetTransform, rb, impactResult);
                    return true;

                case CushionResponseKind.Stop:
                    ApplyStop(rb);
                    return true;

                case CushionResponseKind.Dampen:
                    ApplyDampen(targetTransform, rb, impactResult.RetainedLinearVelocityRate);
                    return true;

                default:
                    return false;
            }
        }

        public static void ApplyStop(Rigidbody rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.detectCollisions = true;
            rb.useGravity = false;
        }

        public static void ApplyStopAndAttach(
            Transform targetTransform,
            Rigidbody rb,
            CushionImpactResult impactResult)
        {
            ApplyStop(rb);

            if (impactResult.AttachParent != null)
                targetTransform.SetParent(impactResult.AttachParent, true);

            if (impactResult.UseAttachPose)
                targetTransform.SetPositionAndRotation(impactResult.AttachPosition, impactResult.AttachRotation);
        }

        public static void ApplyBounce(Transform targetTransform, Rigidbody rb, Vector3 bounceVelocity)
        {
            targetTransform.SetParent(null, true);
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = true;
            rb.angularVelocity = Vector3.zero;
            rb.linearVelocity = bounceVelocity;
        }

        public static void ApplyDampen(Transform targetTransform, Rigidbody rb, float retainedLinearVelocityRate)
        {
            targetTransform.SetParent(null, true);
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = true;
            rb.linearVelocity *= Mathf.Clamp01(retainedLinearVelocityRate);
            rb.angularVelocity = Vector3.zero;
        }
    }
}

using UnityEngine;

namespace BC.Base
{
    public static class MovementBodyGeometryUtility
    {
        public static void GetCapsuleGeometry(
            Transform ownerTransform,
            CapsuleCollider bodyCollider,
            Vector3 bodyPosition,
            out Vector3 capsuleBottom,
            out Vector3 capsuleTop,
            out float capsuleRadius)
        {
            if (ownerTransform == null || bodyCollider == null)
            {
                capsuleBottom = bodyPosition;
                capsuleTop = bodyPosition;
                capsuleRadius = 0.0f;
                return;
            }

            Vector3 lossyScale = ownerTransform.lossyScale;
            float scaleX = Mathf.Abs(lossyScale.x);
            float scaleY = Mathf.Abs(lossyScale.y);
            float scaleZ = Mathf.Abs(lossyScale.z);

            capsuleRadius = bodyCollider.radius * Mathf.Max(scaleX, scaleZ);
            float capsuleHalfHeight = Mathf.Max(capsuleRadius, bodyCollider.height * 0.5f * scaleY);
            Vector3 capsuleCenter = bodyPosition + ownerTransform.rotation * bodyCollider.center;
            float cylinderHalfHeight = Mathf.Max(0.0f, capsuleHalfHeight - capsuleRadius);

            capsuleBottom = capsuleCenter + Vector3.down * cylinderHalfHeight;
            capsuleTop = capsuleCenter + Vector3.up * cylinderHalfHeight;
        }

        public static MovementBodyGeometry Build(
            Transform ownerTransform,
            CapsuleCollider bodyCollider,
            Vector3 bodyPosition,
            float footBandHeight = 0.2f)
        {
            GetCapsuleGeometry(ownerTransform, bodyCollider, bodyPosition, out Vector3 capsuleBottom, out Vector3 capsuleTop, out float capsuleRadius);

            Vector3 capsuleCenter = ownerTransform != null && bodyCollider != null
                ? bodyPosition + ownerTransform.rotation * bodyCollider.center
                : bodyPosition;

            float feetY = capsuleBottom.y - capsuleRadius;
            float headY = capsuleTop.y + capsuleRadius;
            float resolvedFootBandHeight = Mathf.Max(0.0f, footBandHeight);
            float footBandTopY = feetY + resolvedFootBandHeight;

            return new MovementBodyGeometry(
                bodyPosition,
                capsuleCenter,
                capsuleBottom,
                capsuleTop,
                capsuleRadius,
                feetY,
                headY,
                footBandTopY);
        }
    }
}
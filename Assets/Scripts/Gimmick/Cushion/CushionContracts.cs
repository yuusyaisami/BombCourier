using BC.Base;
using UnityEngine;

namespace BC.Gimmick.Cushion
{
    public enum CushionBounceDirectionMode
    {
        LocalUp = 0,
        LocalForward = 1,
        CollisionNormal = 2,
        CustomLocalDirection = 3,
    }

    public enum CushionResponseKind
    {
        None = 0,
        Stop = 1,
        StopAndAttach = 2,
        Bounce = 3,
    }

    public readonly struct CushionImpactData
    {
        public CushionImpactData(
            GameObject sourceGameObject,
            Transform sourceRoot,
            EntityMB sourceEntity,
            EntityTagId sourceTag,
            Rigidbody sourceRigidbody,
            Collider sourceCollider,
            Vector3 point,
            Vector3 normal,
            Vector3 incomingVelocity,
            float impactForce)
        {
            SourceGameObject = sourceGameObject;
            SourceRoot = sourceRoot;
            SourceEntity = sourceEntity;
            SourceTag = sourceTag;
            SourceRigidbody = sourceRigidbody;
            SourceCollider = sourceCollider;
            Point = point;
            Normal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            IncomingVelocity = incomingVelocity;
            ImpactForce = impactForce;
        }

        public GameObject SourceGameObject { get; }
        public Transform SourceRoot { get; }
        public EntityMB SourceEntity { get; }
        public EntityTagId SourceTag { get; }
        public Rigidbody SourceRigidbody { get; }
        public Collider SourceCollider { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public Vector3 IncomingVelocity { get; }
        public float ImpactForce { get; }
    }

    public readonly struct CushionImpactResult
    {
        private CushionImpactResult(
            bool isHandled,
            CushionResponseKind responseKind,
            Vector3 bounceVelocity,
            float bounceSpeedLimit,
            float highJumpSpeedMultiplier,
            Transform attachParent,
            bool useAttachPose,
            Vector3 attachPosition,
            Quaternion attachRotation,
            bool suppressExplosion)
        {
            IsHandled = isHandled;
            ResponseKind = responseKind;
            BounceVelocity = bounceVelocity;
            BounceSpeedLimit = bounceSpeedLimit;
            HighJumpSpeedMultiplier = highJumpSpeedMultiplier;
            AttachParent = attachParent;
            UseAttachPose = useAttachPose;
            AttachPosition = attachPosition;
            AttachRotation = attachRotation;
            SuppressExplosion = suppressExplosion;
        }

        public bool IsHandled { get; }
        public CushionResponseKind ResponseKind { get; }
        public Vector3 BounceVelocity { get; }
        public float BounceSpeedLimit { get; }
        public float HighJumpSpeedMultiplier { get; }
        public Transform AttachParent { get; }
        public bool UseAttachPose { get; }
        public Vector3 AttachPosition { get; }
        public Quaternion AttachRotation { get; }
        public bool SuppressExplosion { get; }

        public static CushionImpactResult NotHandled =>
            new CushionImpactResult(false, CushionResponseKind.None, Vector3.zero, 0f, 1f, null, false, Vector3.zero, Quaternion.identity, false);

        public static CushionImpactResult Stop(bool suppressExplosion = true)
        {
            return new CushionImpactResult(true, CushionResponseKind.Stop, Vector3.zero, 0f, 1f, null, false, Vector3.zero, Quaternion.identity, suppressExplosion);
        }

        public static CushionImpactResult StopAndAttach(
            Transform attachParent,
            bool useAttachPose,
            Vector3 attachPosition,
            Quaternion attachRotation,
            bool suppressExplosion = true)
        {
            return new CushionImpactResult(
                true,
                CushionResponseKind.StopAndAttach,
                Vector3.zero,
                0f,
                1f,
                attachParent,
                useAttachPose,
                attachPosition,
                attachRotation,
                suppressExplosion);
        }

        public static CushionImpactResult Bounce(
            Vector3 bounceVelocity,
            float bounceSpeedLimit,
            float highJumpSpeedMultiplier = 1f,
            bool suppressExplosion = true)
        {
            return new CushionImpactResult(
                true,
                CushionResponseKind.Bounce,
                bounceVelocity,
                Mathf.Max(0f, bounceSpeedLimit),
                Mathf.Max(1f, highJumpSpeedMultiplier),
                null,
                false,
                Vector3.zero,
                Quaternion.identity,
                suppressExplosion);
        }
    }

    public interface ICushionImpactSource
    {
        Transform CushionImpactRoot { get; }
        EntityTagId CushionImpactTag { get; }

        bool HandleCushionImpact(CushionImpactData impactData, CushionImpactResult impactResult);
    }
}

using BC.Base;
using UnityEngine;

namespace BC.Gimmick.Cushion
{
    [DisallowMultipleComponent]
    public sealed class CushionSurfaceMB : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private bool acceptAnyTag;
        [SerializeField, EntityTagDropdown] private EntityTagReference[] targetTags =
        {
            EntityTagReference.From(EntityTags.Item.Bomb)
        };

        [Header("Response")]
        [SerializeField, Range(0.0f, 1.0f)] private float bounceRate;
        [SerializeField, Min(0.0f)] private float bounceSpeed = 8.0f;
        [SerializeField] private CushionBounceDirectionMode bounceDirectionMode = CushionBounceDirectionMode.LocalUp;
        [SerializeField] private Vector3 customLocalDirection = Vector3.up;
        [SerializeField] private bool attachWhenStopped = true;
        [SerializeField] private Transform attachPoint;

        public bool TryEvaluate(CushionImpactData impactData, out CushionImpactResult result)
        {
            result = CushionImpactResult.NotHandled;

            if (ShouldIgnoreSelfImpact(impactData.SourceRoot))
                return false;

            if (!MatchesTargetTag(impactData.SourceTag))
                return false;

            // Bounce率0は「受け止める」。0より大きい時だけ指定方向へ跳ね返す。
            if (bounceRate <= 0.0001f)
            {
                result = BuildStopResult(impactData);
                return true;
            }

            Vector3 direction = ResolveBounceDirection(impactData);
            result = CushionImpactResult.Bounce(direction * bounceSpeed * bounceRate);
            return true;
        }

        private CushionImpactResult BuildStopResult(CushionImpactData impactData)
        {
            if (!attachWhenStopped)
                return CushionImpactResult.Stop();

            Transform parent = attachPoint != null ? attachPoint : transform;
            bool useAttachPose = attachPoint != null;
            Vector3 attachPosition = attachPoint != null ? attachPoint.position : impactData.SourceRoot.position;
            Quaternion attachRotation = attachPoint != null ? attachPoint.rotation : impactData.SourceRoot.rotation;

            return CushionImpactResult.StopAndAttach(parent, useAttachPose, attachPosition, attachRotation);
        }

        private Vector3 ResolveBounceDirection(CushionImpactData impactData)
        {
            Vector3 direction = bounceDirectionMode switch
            {
                CushionBounceDirectionMode.LocalForward => transform.forward,
                CushionBounceDirectionMode.CollisionNormal => impactData.Normal,
                CushionBounceDirectionMode.CustomLocalDirection => transform.TransformDirection(customLocalDirection),
                _ => transform.up,
            };

            if (direction.sqrMagnitude <= 0.0001f)
                direction = transform.up;

            return direction.normalized;
        }

        private bool MatchesTargetTag(EntityTagId sourceTag)
        {
            if (acceptAnyTag)
                return true;

            if (!sourceTag.IsValid || targetTags == null || targetTags.Length == 0)
                return false;

            for (int i = 0; i < targetTags.Length; i++)
            {
                if (targetTags[i].Matches(sourceTag))
                    return true;
            }

            return false;
        }

        private bool ShouldIgnoreSelfImpact(Transform sourceRoot)
        {
            if (sourceRoot == null)
                return false;

            return transform == sourceRoot || transform.IsChildOf(sourceRoot) || sourceRoot.IsChildOf(transform);
        }
    }
}

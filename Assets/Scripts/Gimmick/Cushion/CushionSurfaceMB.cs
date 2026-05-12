using BC.Base;
using UnityEngine;

namespace BC.Gimmick.Cushion
{
    [DisallowMultipleComponent]
    public sealed class CushionSurfaceMB : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("タグ判定を無視して、すべての対象を受け付けるかを指定します。")]
        [SerializeField] private bool acceptAnyTag;
        [Tooltip("このクッションが反応する EntityTag の一覧です。")]
        [SerializeField, EntityTagDropdown]
        private EntityTagReference[] targetTags =
        {
            EntityTagReference.From(EntityTags.Item.Bomb)
        };

        [Header("Response")]
        [Tooltip("跳ね返し強度の割合です。0 の場合は跳ね返さず受け止めます。")]
        [SerializeField, Range(0.0f, 1.0f)] private float bounceRate;
        [Tooltip("跳ね返す時の基準速度です。")]
        [SerializeField, Min(0.0f)] private float bounceSpeed = 8.0f;
        [Tooltip("跳ね返す方向の決め方です。")]
        [SerializeField] private CushionBounceDirectionMode bounceDirectionMode = CushionBounceDirectionMode.LocalUp;
        [Tooltip("BounceDirectionMode が CustomLocalDirection の時に使うローカル方向です。")]
        [SerializeField] private Vector3 customLocalDirection = Vector3.up;
        [Tooltip("停止時に対象をこのクッションへ貼り付けるかを指定します。爆弾は有効でも貼り付けず吸収します。")]
        [SerializeField] private bool attachWhenStopped;
        [Tooltip("停止時の貼り付け先に使う Transform です。未指定ならこのオブジェクト自身を使います。")]
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
            if (impactData.SourceTag.Equals(EntityTags.Item.Bomb.Id))
                return CushionImpactResult.Stop();

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

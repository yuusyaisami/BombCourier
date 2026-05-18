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

        [Header("Landing")]
        [Tooltip("true の時、対象タグの高所落下リアクションをこのクッション上で無効化します。")]
        [SerializeField] private bool suppressHardLanding = true;
        [Tooltip("高所落下リアクションの無効化でタグ判定を無視し、すべての対象を受け付けるかを指定します。")]
        [SerializeField] private bool acceptAnyHardLandingTag = true;
        [Tooltip("高所落下リアクションを無効化する EntityTag の一覧です。acceptAnyHardLandingTag が true の時は参照しません。")]
        [SerializeField, EntityTagDropdown]
        private EntityTagReference[] hardLandingTargetTags =
        {
            EntityTagReference.From(EntityTags.Actor.Player)
        };

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

        public bool TryEvaluateHardLandingSuppression(EntityTagId sourceTag, Transform sourceRoot)
        {
            if (!suppressHardLanding)
                return false;

            if (ShouldIgnoreSelfImpact(sourceRoot))
                return false;

            return MatchesTag(sourceTag, acceptAnyHardLandingTag, hardLandingTargetTags);
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
            return MatchesTag(sourceTag, acceptAnyTag, targetTags);
        }

        private static bool MatchesTag(EntityTagId sourceTag, bool acceptAny, EntityTagReference[] tags)
        {
            if (acceptAny)
                return true;

            if (!sourceTag.IsValid || tags == null || tags.Length == 0)
                return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i].Matches(sourceTag))
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

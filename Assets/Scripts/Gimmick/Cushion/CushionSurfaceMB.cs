using System;
using BC.Base;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace BC.Gimmick.Cushion
{
    [Serializable]
    public struct CushionStopDampenRule
    {
        [Tooltip("この減速率を適用する対象タグです。")]
        [SerializeField, EntityTagDropdown]
        private EntityTagReference tag;

        [Tooltip("衝突後に残す線形速度の割合です。0 で完全停止、1 で無変化です。")]
        [SerializeField, Range(0.0f, 1.0f)]
        private float retainedVelocityRate;

        public bool Matches(EntityTagId sourceTag)
        {
            return tag.Matches(sourceTag);
        }

        public float RetainedVelocityRate => Mathf.Clamp01(retainedVelocityRate);
    }

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
        [SerializeField, Min(0.0f)] private float bounceRate = 1.0f;
        [ShowIf(nameof(ShowBounceSettings))]
        [Tooltip("通常 bounce の最低速度です。0 なら下限を設けません。")]
        [SerializeField, Min(0.0f)] private float minBounceSpeed;
        [ShowIf(nameof(ShowBounceSettings))]
        [Tooltip("通常 bounce の最高速度です。0 なら上限を設けません。")]
        [SerializeField, Min(0.0f)] private float maxBounceSpeed = 12.0f;
        [ShowIf(nameof(ShowBounceSettings))]
        [Tooltip("min/max への収束速度です。大きいほど 1 回あたりの変化が大きくなります。")]
        [SerializeField, Min(0.001f), FormerlySerializedAs("bounceSpeed")] private float convergenceBounceSpeed = 4.0f;
        [ShowIf(nameof(ShowBounceSettings))]
        [Tooltip("計算後の bounce 出力がこの値未満なら反発せず停止系応答へ切り替えます。")]
        [SerializeField, Min(0.0f)] private float minBounceOutputSpeedToApply = 0.1f;
        [ShowIf(nameof(ShowBounceSettings))]
        [Tooltip("Player が high jump を成立させた時に許可する追加倍率です。")]
        [SerializeField, Min(1.0f)] private float highJumpSpeedMultiplier = 1.5f;
        [ShowIf(nameof(ShowBounceSettings))]
        [Tooltip("跳ね返す方向の決め方です。")]
        [SerializeField] private CushionBounceDirectionMode bounceDirectionMode = CushionBounceDirectionMode.LocalUp;
        [ShowIf(nameof(ShowCustomBounceDirection))]
        [Tooltip("BounceDirectionMode が CustomLocalDirection の時に使うローカル方向です。")]
        [SerializeField] private Vector3 customLocalDirection = Vector3.up;
        [Tooltip("停止時に対象をこのクッションへ貼り付けるかを指定します。爆弾は有効でも貼り付けず吸収します。")]
        [SerializeField] private bool attachWhenStopped;
        [ShowIf(nameof(ShowStopDampenSettings))]
        [Tooltip("タグ個別設定に一致しない時に使う、衝突後に残す線形速度の割合です。0 で完全停止、1 で無変化です。")]
        [SerializeField, Range(0.0f, 1.0f)] private float defaultRetainedVelocityRate;
        [ShowIf(nameof(ShowStopDampenSettings))]
        [Tooltip("タグごとに停止時の減速率を上書きします。")]
        [SerializeField] private CushionStopDampenRule[] stopDampenRules = Array.Empty<CushionStopDampenRule>();
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
            float bounceVelocityMagnitude = ResolveBounceSpeed(impactData, direction);

            if (bounceVelocityMagnitude < Mathf.Max(0.0f, minBounceOutputSpeedToApply))
            {
                result = BuildStopResult(impactData);
                return true;
            }

            result = CushionImpactResult.Bounce(
                direction * bounceVelocityMagnitude,
                ResolveBounceSpeedLimit(bounceVelocityMagnitude),
                highJumpSpeedMultiplier);
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
            if (!attachWhenStopped || impactData.SourceTag.Equals(EntityTags.Item.Bomb.Id))
                return CushionImpactResult.Dampen(ResolveRetainedVelocityRate(impactData.SourceTag));

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
                CushionBounceDirectionMode.WorldUp => Vector3.up,
                CushionBounceDirectionMode.LocalForward => transform.forward,
                CushionBounceDirectionMode.CollisionNormal => impactData.Normal,
                CushionBounceDirectionMode.CustomLocalDirection => transform.TransformDirection(customLocalDirection),
                _ => transform.up,
            };

            if (direction.sqrMagnitude <= 0.0001f)
                direction = transform.up;

            return direction.normalized;
        }

        private float ResolveBounceSpeed(CushionImpactData impactData, Vector3 bounceDirection)
        {
            // 入力速度のうち、bounce 方向へ向かっていた成分を優先して拾う。
            float directionalIncomingSpeed = Mathf.Max(0f, Vector3.Dot(-impactData.IncomingVelocity, bounceDirection));

            float resolvedMinSpeed;
            float resolvedMaxSpeed;
            bool hasMinSpeed;
            bool hasMaxSpeed;
            ResolveBounceBounds(out resolvedMinSpeed, out resolvedMaxSpeed, out hasMinSpeed, out hasMaxSpeed);

            float scaledSpeed = directionalIncomingSpeed * Mathf.Max(0.0f, bounceRate);
            if (bounceRate < 0.9999f)
            {
                if (hasMaxSpeed)
                    scaledSpeed = Mathf.Min(scaledSpeed, resolvedMaxSpeed);

                return Mathf.Max(0.0f, scaledSpeed);
            }

            float convergedSpeed = scaledSpeed;
            float convergenceStep = Mathf.Max(0.001f, convergenceBounceSpeed);

            if (hasMaxSpeed && convergedSpeed > resolvedMaxSpeed)
                convergedSpeed = Mathf.MoveTowards(convergedSpeed, resolvedMaxSpeed, convergenceStep);

            if (hasMinSpeed && convergedSpeed < resolvedMinSpeed)
                convergedSpeed = Mathf.MoveTowards(convergedSpeed, resolvedMinSpeed, convergenceStep);

            return Mathf.Max(0f, convergedSpeed);
        }

        private void ResolveBounceBounds(out float resolvedMinSpeed, out float resolvedMaxSpeed, out bool hasMinSpeed, out bool hasMaxSpeed)
        {
            resolvedMinSpeed = Mathf.Max(0.0f, minBounceSpeed);
            resolvedMaxSpeed = Mathf.Max(0.0f, maxBounceSpeed);
            hasMinSpeed = resolvedMinSpeed > 0.0f;
            hasMaxSpeed = resolvedMaxSpeed > 0.0f;

            if (hasMinSpeed && hasMaxSpeed && resolvedMaxSpeed < resolvedMinSpeed)
                resolvedMaxSpeed = resolvedMinSpeed;
        }

        private float ResolveBounceSpeedLimit(float resolvedBounceSpeed)
        {
            ResolveBounceBounds(out _, out float resolvedMaxSpeed, out _, out bool hasMaxSpeed);

            if (hasMaxSpeed)
                return resolvedMaxSpeed;

            return Mathf.Max(0f, resolvedBounceSpeed);
        }

        private bool MatchesTargetTag(EntityTagId sourceTag)
        {
            return MatchesTag(sourceTag, acceptAnyTag, targetTags);
        }

        private bool ShowBounceSettings => bounceRate > 0.0001f;
        private bool ShowCustomBounceDirection => ShowBounceSettings && bounceDirectionMode == CushionBounceDirectionMode.CustomLocalDirection;

        private bool ShowStopDampenSettings => !attachWhenStopped;

        private float ResolveRetainedVelocityRate(EntityTagId sourceTag)
        {
            if (stopDampenRules != null)
            {
                for (int i = 0; i < stopDampenRules.Length; i++)
                {
                    if (stopDampenRules[i].Matches(sourceTag))
                        return stopDampenRules[i].RetainedVelocityRate;
                }
            }

            return Mathf.Clamp01(defaultRetainedVelocityRate);
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

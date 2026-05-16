using UnityEngine;

namespace BC.Base
{
    public interface IEntityVelocitySource
    {
        Vector3 CurrentVelocity { get; }
        Vector3 PlanarVelocity { get; }
        float VerticalVelocity { get; }
        Vector3 ExternalVelocity { get; }
        Vector3 PlatformVelocity { get; }
    }

    public interface IEntityMoveAnimationSource
    {
        EntityMoveState MoveState { get; }

        bool IsGrounded { get; }
        bool IsSprinting { get; }

        /// <summary>
        /// 足の移動アニメーションに使う速度。
        /// 移動床の速度や吹っ飛び速度は基本的に含めない。
        /// 立っているだけで床に運ばれている時に歩きアニメーションを出さないため。
        /// </summary>
        float CurrentPlanarSpeed { get; }

        /// <summary>
        /// BlendTree向けの0〜1速度。
        /// Walk/Run BlendTreeにはこちらを使う方が安定する。
        /// </summary>
        float NormalizedPlanarSpeed { get; }

        /// <summary>
        /// 必要なら向き・傾き・上半身制御に使える。
        /// </summary>
        Vector3 ControlledPlanarVelocity { get; }
    }
}

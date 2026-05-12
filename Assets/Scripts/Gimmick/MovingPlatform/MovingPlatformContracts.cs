using UnityEngine;

namespace BC.Gimmick.MovingPlatform
{
    public enum MovingPlatformPathMode
    {
        LocalOffset = 0,
        Vector3Points = 1,
        CubicBezier = 2,
        TransformPoints = 3,
    }

    public enum MovingPlatformPlaybackMode
    {
        Once = 0,
        Loop = 1,
        PingPong = 2,
    }

    public enum MovingPlatformEasingMode
    {
        Linear = 0,
        SmoothStep = 1,
        EaseInOutSine = 2,
    }

    public enum MovingPlatformStepPoseBasis
    {
        LayerBase = 0,
        PreviousStepEnd = 1,
        World = 2,
    }

    public readonly struct MovingPlatformBasePose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 LocalScale;

        public MovingPlatformBasePose(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            Position = position;
            Rotation = rotation;
            LocalScale = localScale;
        }

        public MovingPlatformPose ToPose()
        {
            return new MovingPlatformPose(Position, Rotation, LocalScale);
        }
    }

    public readonly struct MovingPlatformPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 LocalScale;

        public MovingPlatformPose(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            Position = position;
            Rotation = rotation;
            LocalScale = localScale;
        }
    }
}
using Unity.Cinemachine;
using UnityEngine;

namespace BC.Camera
{
    public readonly struct CameraPathPlaybackPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly bool HasFieldOfView;
        public readonly float FieldOfView;

        public CameraPathPlaybackPose(Vector3 position, Quaternion rotation, bool hasFieldOfView, float fieldOfView)
        {
            Position = position;
            Rotation = rotation;
            HasFieldOfView = hasFieldOfView;
            FieldOfView = fieldOfView;
        }
    }

    public static class CameraPathPlaybackUtility
    {
        private const float DefaultFieldOfView = 60.0f;

        public static CameraPathPlaybackPose BuildPose(in CameraPathResolvedPoint point)
        {
            return new CameraPathPlaybackPose(
                point.Position,
                point.Rotation,
                point.Lens.OverrideFieldOfView,
                point.Lens.FieldOfView);
        }

        public static CameraPathPlaybackPose BuildInterpolatedPose(
            in CameraPathResolvedPoint from,
            in CameraPathResolvedPoint to,
            float t,
            float currentFieldOfView)
        {
            t = Mathf.Clamp01(t);

            bool hasFieldOfView = from.Lens.OverrideFieldOfView || to.Lens.OverrideFieldOfView;
            float fieldOfView = currentFieldOfView;

            if (hasFieldOfView)
            {
                float fromFieldOfView = from.Lens.OverrideFieldOfView ? from.Lens.FieldOfView : currentFieldOfView;
                float toFieldOfView = to.Lens.OverrideFieldOfView ? to.Lens.FieldOfView : fromFieldOfView;
                fieldOfView = Mathf.Lerp(fromFieldOfView, toFieldOfView, t);
            }

            return new CameraPathPlaybackPose(
                Vector3.Lerp(from.Position, to.Position, t),
                Quaternion.Slerp(from.Rotation, to.Rotation, t),
                hasFieldOfView,
                fieldOfView);
        }

        public static void ApplyPose(Transform cameraTransform, CinemachineCamera camera, in CameraPathPlaybackPose pose)
        {
            if (cameraTransform == null)
                return;

            cameraTransform.SetPositionAndRotation(pose.Position, pose.Rotation);

            if (camera == null || !pose.HasFieldOfView)
                return;

            LensSettings lens = camera.Lens;
            lens.FieldOfView = pose.FieldOfView;
            camera.Lens = lens;
        }

        public static void ApplyPose(Transform cameraTransform, UnityEngine.Camera camera, in CameraPathPlaybackPose pose)
        {
            if (cameraTransform == null)
                return;

            cameraTransform.SetPositionAndRotation(pose.Position, pose.Rotation);

            if (camera == null || !pose.HasFieldOfView)
                return;

            camera.fieldOfView = pose.FieldOfView;
        }

        public static float GetFieldOfView(CinemachineCamera camera)
        {
            return camera != null ? camera.Lens.FieldOfView : DefaultFieldOfView;
        }

        public static float GetFieldOfView(UnityEngine.Camera camera)
        {
            return camera != null ? camera.fieldOfView : DefaultFieldOfView;
        }
    }
}
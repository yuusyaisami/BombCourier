using System;
using System.Collections.Generic;
using BC.ActionSystem;
using BC.Base;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

namespace BC.Camera
{
    public sealed class CameraPathPlayRequest
    {
        public readonly CinemachineCamera PathCamera;
        public readonly CinemachineCamera ReturnCamera;
        public readonly CameraPathSequenceDefinition Sequence;
        public readonly EntityRef Actor;
        public readonly int PathPriority;
        public readonly int ReturnPriority;
        public readonly int InactivePriority;

        public CameraPathPlayRequest(
            CinemachineCamera pathCamera,
            CinemachineCamera returnCamera,
            CameraPathSequenceDefinition sequence,
            EntityRef actor,
            int pathPriority,
            int returnPriority,
            int inactivePriority)
        {
            PathCamera = pathCamera;
            ReturnCamera = returnCamera;
            Sequence = sequence;
            Actor = actor;
            PathPriority = pathPriority;
            ReturnPriority = returnPriority;
            InactivePriority = inactivePriority;
        }
    }

    public sealed class CameraPathPlayerService
    {
        private readonly SceneKernel sceneKernel;
        private int playVersion;

        public CameraPathPlayerService(SceneKernel sceneKernel)
        {
            this.sceneKernel = sceneKernel ?? throw new ArgumentNullException(nameof(sceneKernel));
        }

        public async UniTask PlayAsync(CameraPathPlayRequest request)
        {
            if (!ValidateRequest(request))
                return;

            int version = ++playVersion;
            IReadOnlyList<CameraPathResolvedPoint> points = request.Sequence.Points;

            SetPriority(request.ReturnCamera, request.InactivePriority);
            SetPriority(request.PathCamera, request.PathPriority);
            request.PathCamera.Prioritize();

            Transform cameraTransform = request.PathCamera.transform;
            ApplyPoint(request.PathCamera, cameraTransform, points[0]);
            await ExecutePointActionAsync(points[0], request.Actor);
            await HoldAsync(points[0].HoldSeconds, version);

            for (int index = 1; index < points.Count; index++)
            {
                if (version != playVersion || request.PathCamera == null)
                    return;

                CameraPathResolvedPoint previous = points[index - 1];
                CameraPathResolvedPoint current = points[index];

                await MoveToPointAsync(request.PathCamera, previous, current, version);

                if (version != playVersion)
                    return;

                await ExecutePointActionAsync(current, request.Actor);
                await HoldAsync(current.HoldSeconds, version);
            }

            if (version != playVersion)
                return;

            SetPriority(request.PathCamera, request.InactivePriority);
            SetPriority(request.ReturnCamera, request.ReturnPriority);
            request.ReturnCamera?.Prioritize();
        }

        public void Cancel()
        {
            playVersion++;
        }

        private bool ValidateRequest(CameraPathPlayRequest request)
        {
            if (request == null)
            {
                Debug.LogError($"{nameof(CameraPathPlayerService)}: play request is null.");
                return false;
            }

            if (request.PathCamera == null)
            {
                Debug.LogError($"{nameof(CameraPathPlayerService)}: path camera is null.");
                return false;
            }

            if (request.ReturnCamera == null)
            {
                Debug.LogError($"{nameof(CameraPathPlayerService)}: return camera is null.");
                return false;
            }

            if (request.Sequence == null || request.Sequence.Count == 0)
            {
                Debug.LogError($"{nameof(CameraPathPlayerService)}: camera path sequence is empty.");
                return false;
            }

            return true;
        }

        private async UniTask MoveToPointAsync(
            CinemachineCamera camera,
            CameraPathResolvedPoint from,
            CameraPathResolvedPoint to,
            int version)
        {
            CameraPathTransitionSettings transition = to.TransitionFromPrevious;

            if (transition.Kind == CameraPathTransitionKind.Cut || transition.Duration <= 0.0f)
            {
                if (camera != null)
                    ApplyPoint(camera, camera.transform, to);

                await UniTask.Yield(PlayerLoopTiming.Update);
                return;
            }

            float elapsed = 0.0f;

            while (elapsed < transition.Duration)
            {
                if (version != playVersion || camera == null)
                    return;

                float t = transition.Evaluate(elapsed / transition.Duration);
                ApplyInterpolatedPoint(camera, camera.transform, from, to, t);
                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (version == playVersion && camera != null)
                ApplyPoint(camera, camera.transform, to);
        }

        private async UniTask HoldAsync(float seconds, int version)
        {
            float remaining = Mathf.Max(0.0f, seconds);

            while (remaining > 0.0f)
            {
                if (version != playVersion)
                    return;

                remaining -= Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }

        private async UniTask ExecutePointActionAsync(CameraPathResolvedPoint point, EntityRef actor)
        {
            InlineAction inlineAction = point.OnArriveAction;

            if (inlineAction == null)
                return;

            if (!actor.IsValid)
            {
                Debug.LogWarning($"{nameof(CameraPathPlayerService)}: camera path point action was skipped because actor is invalid.");
                return;
            }

            ActionExecutionResult result = await sceneKernel.Actions.ExecuteAsync(actor, inlineAction.Compile());

            if (result.IsFailed)
                Debug.LogWarning($"{nameof(CameraPathPlayerService)}: camera path point action failed. {result.Message}");
        }

        private static void ApplyPoint(CinemachineCamera camera, Transform cameraTransform, CameraPathResolvedPoint point)
        {
            cameraTransform.SetPositionAndRotation(point.Position, point.Rotation);
            ApplyLens(camera, point.Lens);
        }

        private static void ApplyInterpolatedPoint(
            CinemachineCamera camera,
            Transform cameraTransform,
            CameraPathResolvedPoint from,
            CameraPathResolvedPoint to,
            float t)
        {
            cameraTransform.SetPositionAndRotation(
                Vector3.Lerp(from.Position, to.Position, t),
                Quaternion.Slerp(from.Rotation, to.Rotation, t));

            ApplyInterpolatedLens(camera, from.Lens, to.Lens, t);
        }

        private static void ApplyLens(CinemachineCamera camera, CameraPathLensSettings lensSettings)
        {
            if (camera == null || !lensSettings.OverrideFieldOfView)
                return;

            LensSettings lens = camera.Lens;
            lens.FieldOfView = lensSettings.FieldOfView;
            camera.Lens = lens;
        }

        private static void ApplyInterpolatedLens(
            CinemachineCamera camera,
            CameraPathLensSettings from,
            CameraPathLensSettings to,
            float t)
        {
            if (camera == null)
                return;

            if (!from.OverrideFieldOfView && !to.OverrideFieldOfView)
                return;

            LensSettings lens = camera.Lens;
            float fromFieldOfView = from.OverrideFieldOfView ? from.FieldOfView : lens.FieldOfView;
            float toFieldOfView = to.OverrideFieldOfView ? to.FieldOfView : fromFieldOfView;
            lens.FieldOfView = Mathf.Lerp(fromFieldOfView, toFieldOfView, t);
            camera.Lens = lens;
        }

        private static void SetPriority(CinemachineCamera camera, int priority)
        {
            if (camera == null)
                return;

            PrioritySettings settings = camera.Priority;
            settings.Enabled = true;
            settings.Value = priority;
            camera.Priority = settings;
        }
    }
}
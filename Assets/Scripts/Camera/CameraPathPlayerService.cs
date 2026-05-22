using System;
using System.Collections.Generic;
using BC.ActionSystem;
using BC.Base;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

namespace BC.Camera
{
    // カメラパス再生に必要な入力をまとめたリクエスト。
    // 再生対象カメラ、戻り先カメラ、sequence、本体 actor などを 1 つに束ねる。
    public sealed class CameraPathPlayRequest
    {
        public readonly CinemachineCamera PathCamera;
        public readonly CinemachineCamera ReturnCamera;
        public readonly CameraPathSequenceDefinition Sequence;
        public readonly EntityRef Actor;
        public readonly int PathPriority;
        public readonly int ReturnPriority;
        public readonly int InactivePriority;
        public readonly Func<UniTask> OnCompletedBeforeCameraReset;

        public CameraPathPlayRequest(
            CinemachineCamera pathCamera,
            CinemachineCamera returnCamera,
            CameraPathSequenceDefinition sequence,
            EntityRef actor,
            int pathPriority,
            int returnPriority,
            int inactivePriority,
            Func<UniTask> onCompletedBeforeCameraReset = null)
        {
            PathCamera = pathCamera;
            ReturnCamera = returnCamera;
            Sequence = sequence;
            Actor = actor;
            PathPriority = pathPriority;
            ReturnPriority = returnPriority;
            InactivePriority = inactivePriority;
            OnCompletedBeforeCameraReset = onCompletedBeforeCameraReset;
        }
    }

    // CameraPathSequenceDefinition を時間進行に沿って再生する純粋サービス。
    // 実際のカメラ選択は SceneCameraService が担い、この class は再生進行だけに集中する。
    public sealed class CameraPathPlayerService
    {
        private readonly SceneKernel sceneKernel;
        // 新しい再生を始めるたびに version を進め、古い async ループを自然終了させる。
        private int playVersion;

        public CameraPathPlayerService(SceneKernel sceneKernel)
        {
            this.sceneKernel = sceneKernel ?? throw new ArgumentNullException(nameof(sceneKernel));
        }

        // sequence の各 point を順番に適用し、必要なら point action も実行する。
        public async UniTask PlayAsync(CameraPathPlayRequest request)
        {
            if (!ValidateRequest(request))
                return;

            int version = ++playVersion;
            IReadOnlyList<CameraPathResolvedPoint> points = request.Sequence.Points;
            // 再生中だけ SceneCameraService 側で path camera を最優先にする。
            sceneKernel.Cameras?.BeginPathCameraOverride(request, version);

            try
            {
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

                await AwaitBeforeCameraResetAsync(request);
            }
            finally
            {
                // 途中 cancel や exception でも override を必ず解除する。
                sceneKernel.Cameras?.EndPathCameraOverride(version);
            }
        }

        // 現在進行中の再生を止めたいときは version を進めるだけでよい。
        public void Cancel()
        {
            playVersion++;
        }

        // 実行前に最低限必要な構成だけチェックする。
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

        // 2 点間を transition 設定に従って補間する。
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

            // move 中に新しい再生が始まったら version 不一致で中断する。
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

        private static async UniTask AwaitBeforeCameraResetAsync(CameraPathPlayRequest request)
        {
            if (request?.OnCompletedBeforeCameraReset == null)
                return;

            try
            {
                await request.OnCompletedBeforeCameraReset.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void ApplyPoint(CinemachineCamera camera, Transform cameraTransform, CameraPathResolvedPoint point)
        {
            CameraPathPlaybackUtility.ApplyPose(
                cameraTransform,
                camera,
                CameraPathPlaybackUtility.BuildPose(point));
        }

        private static void ApplyInterpolatedPoint(
            CinemachineCamera camera,
            Transform cameraTransform,
            CameraPathResolvedPoint from,
            CameraPathResolvedPoint to,
            float t)
        {
            CameraPathPlaybackUtility.ApplyPose(
                cameraTransform,
                camera,
                CameraPathPlaybackUtility.BuildInterpolatedPose(
                    from,
                    to,
                    t,
                    CameraPathPlaybackUtility.GetFieldOfView(camera)));
        }
    }
}
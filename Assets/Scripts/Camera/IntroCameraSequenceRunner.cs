using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

namespace BombCourier.CameraIntro
{
    /// <summary>
    /// ゲーム開始時のイントロカメラ演出を再生する。
    /// 
    /// 方針:
    /// - Intro用CinemachineCameraのTransformを直接動かす
    /// - CinemachineのPriorityでGameplayCameraと切り替える
    /// - 点列はCatmull-Romで滑らかに補間する
    /// </summary>
    public sealed class IntroCameraSequenceRunner : MonoBehaviour
    {
        [Header("Cinemachine")]
        [SerializeField] private CinemachineCamera introCamera;
        [SerializeField] private CinemachineCamera gameplayCamera;

        [SerializeField] private int introPriority = 100;
        [SerializeField] private int gameplayPriority = 10;
        [SerializeField] private int inactivePriority = 0;

        [Header("Motion")]
        [SerializeField]
        private AnimationCurve defaultEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField]
        private bool useUnscaledTime = false;

        [Header("Events")]
        [SerializeField]
        private UnityEvent<bool> inputLockChanged;

        [SerializeField]
        private UnityEvent introCompleted;

        private CancellationTokenSource activePlayCancellationTokenSource;
        private bool skipRequested;

        public async UniTask Play(IntroCameraPathAuthoring path)
        {
            CancellationTokenSource playCancellationTokenSource = BeginNewPlay();

            try
            {
                await PlayRoutine(path, playCancellationTokenSource.Token);
            }
            catch (OperationCanceledException) when (playCancellationTokenSource.IsCancellationRequested)
            {
            }
            finally
            {
                CompletePlay(playCancellationTokenSource);
            }
        }

        public void Skip()
        {
            skipRequested = true;
        }

        private CancellationTokenSource BeginNewPlay()
        {
            skipRequested = false;

            if (activePlayCancellationTokenSource != null)
            {
                activePlayCancellationTokenSource.Cancel();
            }

            activePlayCancellationTokenSource = new CancellationTokenSource();
            return activePlayCancellationTokenSource;
        }

        private void CompletePlay(CancellationTokenSource playCancellationTokenSource)
        {
            if (ReferenceEquals(activePlayCancellationTokenSource, playCancellationTokenSource))
            {
                activePlayCancellationTokenSource = null;
            }

            playCancellationTokenSource.Dispose();
        }

        private async UniTask PlayRoutine(IntroCameraPathAuthoring path, CancellationToken cancellationToken)
        {
            if (path == null)
            {
                Debug.LogError($"{nameof(IntroCameraSequenceRunner)}: path is null.", this);
                CompleteImmediately();
                return;
            }

            if (introCamera == null)
            {
                Debug.LogError($"{nameof(IntroCameraSequenceRunner)}: introCamera is null.", this);
                CompleteImmediately();
                return;
            }

            if (gameplayCamera == null)
            {
                Debug.LogError($"{nameof(IntroCameraSequenceRunner)}: gameplayCamera is null.", this);
                CompleteImmediately();
                return;
            }

            List<IntroCameraPoint> points = path.BuildOrderedPoints();

            if (points.Count < 2)
            {
                Debug.LogWarning($"{nameof(IntroCameraSequenceRunner)}: Intro path requires at least 2 points. Skipping intro.", this);
                CompleteImmediately();
                return;
            }

            inputLockChanged?.Invoke(true);

            SetPriority(gameplayCamera, inactivePriority);
            SetPriority(introCamera, introPriority);
            introCamera.Prioritize();

            Transform cameraTransform = introCamera.transform;

            IntroCameraPoint first = points[0];
            cameraTransform.SetPositionAndRotation(
                first.transform.position,
                EvaluateRotation(points, 0, 0f, first.transform.position, first.transform.rotation)
            );

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            for (int segmentIndex = 0; segmentIndex < points.Count - 1; segmentIndex++)
            {
                if (skipRequested)
                {
                    break;
                }

                IntroCameraPoint to = points[segmentIndex + 1];

                float duration = Mathf.Max(0.01f, to.SecondsFromPrevious);
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    if (skipRequested)
                    {
                        break;
                    }

                    float rawT = Mathf.Clamp01(elapsed / duration);
                    float easedT = defaultEase != null ? defaultEase.Evaluate(rawT) : SmoothStep(rawT);

                    Vector3 position = EvaluatePosition(points, segmentIndex, easedT);
                    Quaternion rotation = EvaluateRotation(
                        points,
                        segmentIndex,
                        easedT,
                        position,
                        cameraTransform.rotation
                    );

                    cameraTransform.SetPositionAndRotation(position, rotation);

                    elapsed += DeltaTime();
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                cameraTransform.SetPositionAndRotation(
                    to.transform.position,
                    EvaluateRotation(points, segmentIndex, 1f, to.transform.position, cameraTransform.rotation)
                );

                if (to.HoldSeconds > 0f)
                {
                    float hold = 0f;

                    while (hold < to.HoldSeconds)
                    {
                        if (skipRequested)
                        {
                            break;
                        }

                        hold += DeltaTime();
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    }
                }
            }

            SetPriority(introCamera, inactivePriority);
            SetPriority(gameplayCamera, gameplayPriority);
            gameplayCamera.Prioritize();

            inputLockChanged?.Invoke(false);
            introCompleted?.Invoke();

            return;
        }

        private void CompleteImmediately()
        {
            if (introCamera != null)
            {
                SetPriority(introCamera, inactivePriority);
            }

            if (gameplayCamera != null)
            {
                SetPriority(gameplayCamera, gameplayPriority);
                gameplayCamera.Prioritize();
            }

            inputLockChanged?.Invoke(false);
            introCompleted?.Invoke();
        }

        private float DeltaTime()
        {
            return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private static void SetPriority(CinemachineCamera camera, int priority)
        {
            if (camera == null)
            {
                return;
            }

            // Cinemachine 3系では Priority は PrioritySettings。
            // 直接 int 代入できる環境もあるが、明示的にEnabled/Valueを触る方が安全。
            PrioritySettings settings = camera.Priority;
            settings.Enabled = true;
            settings.Value = priority;
            camera.Priority = settings;
        }

        private static Vector3 EvaluatePosition(
            IReadOnlyList<IntroCameraPoint> points,
            int segmentIndex,
            float t
        )
        {
            Vector3 p0 = GetPointPosition(points, segmentIndex - 1);
            Vector3 p1 = GetPointPosition(points, segmentIndex);
            Vector3 p2 = GetPointPosition(points, segmentIndex + 1);
            Vector3 p3 = GetPointPosition(points, segmentIndex + 2);

            return CatmullRom(p0, p1, p2, p3, t);
        }

        private static Quaternion EvaluateRotation(
            IReadOnlyList<IntroCameraPoint> points,
            int segmentIndex,
            float t,
            Vector3 cameraPosition,
            Quaternion fallbackRotation
        )
        {
            IntroCameraPoint from = points[Mathf.Clamp(segmentIndex, 0, points.Count - 1)];
            IntroCameraPoint to = points[Mathf.Clamp(segmentIndex + 1, 0, points.Count - 1)];

            bool useLookAt = from.HasLookAtTarget || to.HasLookAtTarget;

            if (useLookAt)
            {
                Vector3 fromTarget = from.GetLookAtPosition();
                Vector3 toTarget = to.GetLookAtPosition();
                Vector3 target = Vector3.Lerp(fromTarget, toTarget, t);

                Vector3 direction = target - cameraPosition;

                if (direction.sqrMagnitude < 0.0001f)
                {
                    return fallbackRotation;
                }

                return Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            return Quaternion.Slerp(from.transform.rotation, to.transform.rotation, t);
        }

        private static Vector3 GetPointPosition(IReadOnlyList<IntroCameraPoint> points, int index)
        {
            index = Mathf.Clamp(index, 0, points.Count - 1);
            return points[index].transform.position;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
using System;
using System.Collections.Generic;
using BC.Base;
using BC.Camera;
using UnityEditor;
using UnityEngine;
using UnityCamera = UnityEngine.Camera;

namespace BC.Editor.Camera
{
    [Flags]
    internal enum CameraPathPreviewTargetMode
    {
        None = 0,
        SceneView = 1,
        PreviewCamera = 2,
        Both = SceneView | PreviewCamera,
    }

    internal readonly struct CameraPathPreviewResolveResult
    {
        public readonly bool CanPreview;
        public readonly CameraPathSequenceDefinition Sequence;
        public readonly string ResolveModeLabel;
        public readonly string Message;

        public CameraPathPreviewResolveResult(
            bool canPreview,
            CameraPathSequenceDefinition sequence,
            string resolveModeLabel,
            string message)
        {
            CanPreview = canPreview;
            Sequence = sequence;
            ResolveModeLabel = resolveModeLabel ?? string.Empty;
            Message = message ?? string.Empty;
        }
    }

    internal static class CameraPathSequencePreviewResolver
    {
        public static CameraPathPreviewResolveResult Resolve(CameraPathSequenceAuthoringMB sequenceSource)
        {
            if (sequenceSource == null)
                return Fail("Camera path source is missing.");

            IReadOnlyList<CameraPathPointDefinition> definitions = sequenceSource.BuildSequence();

            if (definitions == null || definitions.Count == 0)
                return Fail("Camera path に point がありません。");

            if (Application.isPlaying && TryResolveRuntimeSequence(definitions, out CameraPathSequenceDefinition runtimeSequence, out string runtimeMessage))
            {
                return new CameraPathPreviewResolveResult(
                    true,
                    runtimeSequence,
                    "Play Mode runtime resolve",
                    runtimeMessage);
            }

            if (TryResolveEditorSequence(definitions, out CameraPathSequenceDefinition editorSequence, out bool usedFallback, out string editorMessage))
            {
                return new CameraPathPreviewResolveResult(
                    true,
                    editorSequence,
                    Application.isPlaying
                        ? (usedFallback ? "Play Mode fallback resolve" : "Play Mode literal resolve")
                        : (usedFallback ? "Edit Mode literal/fallback resolve" : "Edit Mode literal resolve"),
                    editorMessage);
            }

            return Fail(editorMessage);
        }

        private static bool TryResolveRuntimeSequence(
            IReadOnlyList<CameraPathPointDefinition> definitions,
            out CameraPathSequenceDefinition sequence,
            out string message)
        {
            sequence = null;

            SceneKernelMB kernelMB = UnityEngine.Object.FindAnyObjectByType<SceneKernelMB>();

            if (kernelMB?.Kernel?.ReactiveValues == null)
            {
                message = "Play Mode ですが SceneKernel が見つからないため runtime resolve は使えません。";
                return false;
            }

            EntityRef actor = ResolveRuntimeActor();

            if (!actor.IsValid)
            {
                message = "Play Mode ですが actor を解決できないため runtime resolve は使えません。";
                return false;
            }

            if (!TryResolveWithRuntimeContext(definitions, kernelMB.Kernel, actor, out sequence, out message))
                return false;

            message = "Play Mode では SceneKernel と tracked player を使って runtime と同じ解決経路で preview します。";
            return true;
        }

        private static bool TryResolveWithRuntimeContext(
            IReadOnlyList<CameraPathPointDefinition> definitions,
            SceneKernel sceneKernel,
            EntityRef actor,
            out CameraPathSequenceDefinition sequence,
            out string message)
        {
            sequence = null;

            if (sceneKernel?.ReactiveValues == null)
            {
                message = "Reactive value resolver is not available.";
                return false;
            }

            List<CameraPathResolvedPoint> resolvedPoints = new(definitions.Count);
            ReactiveEvalContext context = new(sceneKernel, actor, default);

            for (int i = 0; i < definitions.Count; i++)
            {
                CameraPathPointDefinition definition = definitions[i];

                if (!definition.TryResolve(context, sceneKernel.ReactiveValues, out CameraPathResolvedPoint point, out ReactiveError error))
                {
                    message = $"{ResolvePointLabel(definition, i)} を runtime resolve できませんでした。{error.Message}";
                    return false;
                }

                resolvedPoints.Add(StripAction(point));
            }

            sequence = new CameraPathSequenceDefinition(resolvedPoints);
            message = string.Empty;
            return true;
        }

        private static bool TryResolveEditorSequence(
            IReadOnlyList<CameraPathPointDefinition> definitions,
            out CameraPathSequenceDefinition sequence,
            out bool usedFallback,
            out string message)
        {
            sequence = null;
            usedFallback = false;
            List<CameraPathResolvedPoint> resolvedPoints = new(definitions.Count);

            for (int i = 0; i < definitions.Count; i++)
            {
                CameraPathPointDefinition definition = definitions[i];

                if (!TryResolvePreviewVector3(definition.Position, out Vector3 position, out bool usedPositionFallback, out string positionMessage))
                {
                    message = $"{ResolvePointLabel(definition, i)} の Position を preview 用に解決できません。{positionMessage}";
                    return false;
                }

                if (!TryResolvePreviewVector3(definition.EulerAngles, out Vector3 eulerAngles, out bool usedRotationFallback, out string rotationMessage))
                {
                    message = $"{ResolvePointLabel(definition, i)} の Euler Angles を preview 用に解決できません。{rotationMessage}";
                    return false;
                }

                usedFallback |= usedPositionFallback || usedRotationFallback;
                resolvedPoints.Add(new CameraPathResolvedPoint(
                    definition.Label,
                    position,
                    Quaternion.Euler(eulerAngles),
                    definition.HoldSeconds,
                    definition.TransitionFromPrevious,
                    definition.Lens,
                    null));
            }

            sequence = new CameraPathSequenceDefinition(resolvedPoints);
            message = usedFallback
                ? "Edit Mode では literal を優先し、runtime context が要る値は fallback で preview します。"
                : "Edit Mode では literal 値だけで preview します。";
            return true;
        }

        private static bool TryResolvePreviewVector3(
            ReactiveVector3 source,
            out Vector3 value,
            out bool usedFallback,
            out string message)
        {
            if (source.SourceKind == ReactiveVector3SourceKind.Literal)
            {
                value = source.Literal;
                usedFallback = false;
                message = string.Empty;
                return true;
            }

            if (source.FailurePolicy == ReactiveFailurePolicy.UseFallback)
            {
                value = source.FallbackValue;
                usedFallback = true;
                message = $"{source.SourceKind} は fallback を使用します。";
                return true;
            }

            value = default;
            usedFallback = false;
            message = $"{source.SourceKind} は runtime context が必要で、failure policy が UseFallback ではありません。";
            return false;
        }

        private static EntityRef ResolveRuntimeActor()
        {
            CameraManager cameraManager = CameraManager.Instance;

            if (cameraManager?.CurrentThirdPersonTarget != null)
            {
                EntityMB trackedEntity = cameraManager.CurrentThirdPersonTarget.GetComponentInParent<EntityMB>();
                if (trackedEntity != null && trackedEntity.HasEntity)
                    return trackedEntity.Entity;
            }

            PlayerMB player = UnityEngine.Object.FindAnyObjectByType<PlayerMB>();
            if (player != null)
            {
                EntityMB playerEntity = player.GetComponent<EntityMB>();
                if (playerEntity != null && playerEntity.HasEntity)
                    return playerEntity.Entity;
            }

            return default;
        }

        private static CameraPathResolvedPoint StripAction(in CameraPathResolvedPoint point)
        {
            return new CameraPathResolvedPoint(
                point.Label,
                point.Position,
                point.Rotation,
                point.HoldSeconds,
                point.TransitionFromPrevious,
                point.Lens,
                null);
        }

        private static string ResolvePointLabel(CameraPathPointDefinition definition, int index)
        {
            return string.IsNullOrWhiteSpace(definition.Label)
                ? $"Point {index + 1}"
                : $"Point {index + 1}: {definition.Label}";
        }

        private static CameraPathPreviewResolveResult Fail(string message)
        {
            return new CameraPathPreviewResolveResult(false, null, string.Empty, message);
        }
    }

    internal sealed class CameraPathSequencePreviewSession : IDisposable
    {
        private enum PreviewPhase
        {
            Hold = 0,
            Transition = 1,
            Completed = 2,
        }

        private const string PreviewCameraName = "[CameraPathPreview]";

        private readonly CameraPathSequenceAuthoringMB owner;
        private readonly CameraPathSequenceDefinition sequence;
        private readonly CameraPathPreviewTargetMode targetMode;
        private readonly SceneView sceneView;
        private readonly SavedSceneViewState savedSceneViewState;
        private readonly UnityCamera previewCamera;
        private readonly float totalDurationSeconds;
        private readonly string resolveModeLabel;
        private readonly string resolveMessage;

        private double lastUpdateTime;
        private int settledPointIndex;
        private int transitionTargetIndex = -1;
        private float phaseElapsedSeconds;
        private float elapsedSeconds;
        private PreviewPhase phase;

        public static CameraPathSequencePreviewSession ActiveSession { get; private set; }

        public UnityCamera PreviewCamera => previewCamera;
        public float ElapsedSeconds => elapsedSeconds;
        public float TotalDurationSeconds => totalDurationSeconds;
        public int PointCount => sequence?.Count ?? 0;
        public int CurrentPointIndex => transitionTargetIndex >= 0 ? transitionTargetIndex : settledPointIndex;
        public string PhaseLabel => phase.ToString();
        public string ResolveModeLabel => resolveModeLabel;
        public string ResolveMessage => resolveMessage;
        public CameraPathPreviewTargetMode TargetMode => targetMode;

        static CameraPathSequencePreviewSession()
        {
            AssemblyReloadEvents.beforeAssemblyReload += StopActiveSession;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private CameraPathSequencePreviewSession(
            CameraPathSequenceAuthoringMB owner,
            CameraPathSequenceDefinition sequence,
            CameraPathPreviewTargetMode targetMode,
            SceneView sceneView,
            string resolveModeLabel,
            string resolveMessage)
        {
            this.owner = owner;
            this.sequence = sequence;
            this.targetMode = targetMode;
            this.sceneView = sceneView;
            this.resolveModeLabel = resolveModeLabel ?? string.Empty;
            this.resolveMessage = resolveMessage ?? string.Empty;
            savedSceneViewState = new SavedSceneViewState(sceneView);
            previewCamera = CreatePreviewCamera(targetMode);
            totalDurationSeconds = ComputeTotalDuration(sequence);
            lastUpdateTime = EditorApplication.timeSinceStartup;
            settledPointIndex = 0;
            phase = PreviewPhase.Hold;

            ApplyPoint(sequence.Points[0]);
            EditorApplication.update += Update;
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        public bool IsOwnedBy(CameraPathSequenceAuthoringMB candidate)
        {
            return owner == candidate;
        }

        public static bool TryStart(
            CameraPathSequenceAuthoringMB owner,
            CameraPathSequenceDefinition sequence,
            CameraPathPreviewTargetMode requestedTargetMode,
            string resolveModeLabel,
            string resolveMessage,
            out string message)
        {
            StopActiveSession();

            if (owner == null)
            {
                message = "Camera path source is missing.";
                return false;
            }

            if (sequence == null || sequence.Count == 0)
            {
                message = "Preview 対象の camera path sequence が空です。";
                return false;
            }

            if (requestedTargetMode == CameraPathPreviewTargetMode.None)
            {
                message = "Preview target を 1 つ以上選択してください。";
                return false;
            }

            CameraPathPreviewTargetMode effectiveTargetMode = requestedTargetMode;
            SceneView activeSceneView = null;
            string downgradedMessage = string.Empty;

            if ((effectiveTargetMode & CameraPathPreviewTargetMode.SceneView) != 0)
            {
                activeSceneView = SceneView.lastActiveSceneView;

                if (activeSceneView == null || activeSceneView.camera == null)
                {
                    effectiveTargetMode &= ~CameraPathPreviewTargetMode.SceneView;
                    downgradedMessage = "Scene View が取得できなかったため Preview Camera のみを使います。";
                }
            }

            if (effectiveTargetMode == CameraPathPreviewTargetMode.None)
            {
                message = "Scene View を取得できず、Preview Camera も無効です。";
                return false;
            }

            ActiveSession = new CameraPathSequencePreviewSession(
                owner,
                sequence,
                effectiveTargetMode,
                activeSceneView,
                resolveModeLabel,
                resolveMessage);

            message = downgradedMessage;
            return true;
        }

        public static void StopActiveSession()
        {
            if (ActiveSession == null)
                return;

            CameraPathSequencePreviewSession activeSession = ActiveSession;
            ActiveSession = null;
            activeSession.Dispose();
        }

        public static void StopIfOwner(CameraPathSequenceAuthoringMB owner)
        {
            if (ActiveSession != null && ActiveSession.owner == owner)
                StopActiveSession();
        }

        public void Dispose()
        {
            EditorApplication.update -= Update;

            if (sceneView != null)
                savedSceneViewState.Restore(sceneView);

            if (previewCamera != null)
            {
                UnityEngine.Object.DestroyImmediate(previewCamera.gameObject);
            }

            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange stateChange)
        {
            StopActiveSession();
        }

        private void Update()
        {
            if (owner == null || sequence == null || sequence.Count == 0)
            {
                StopActiveSession();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float deltaTime = Mathf.Max(0.0f, (float)(now - lastUpdateTime));
            lastUpdateTime = now;

            if (phase != PreviewPhase.Completed)
                Tick(deltaTime);

            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private void Tick(float deltaTime)
        {
            IReadOnlyList<CameraPathResolvedPoint> points = sequence.Points;

            while (deltaTime > 0.0f && phase != PreviewPhase.Completed)
            {
                if (phase == PreviewPhase.Hold)
                {
                    CameraPathResolvedPoint current = points[settledPointIndex];
                    float holdSeconds = current.HoldSeconds;

                    if (holdSeconds <= 0.0f)
                    {
                        if (settledPointIndex >= points.Count - 1)
                        {
                            phase = PreviewPhase.Completed;
                            break;
                        }

                        transitionTargetIndex = settledPointIndex + 1;
                        phase = PreviewPhase.Transition;
                        phaseElapsedSeconds = 0.0f;
                        continue;
                    }

                    float consumed = Mathf.Min(deltaTime, holdSeconds - phaseElapsedSeconds);
                    phaseElapsedSeconds += consumed;
                    elapsedSeconds += consumed;
                    deltaTime -= consumed;

                    if (phaseElapsedSeconds >= holdSeconds)
                    {
                        if (settledPointIndex >= points.Count - 1)
                        {
                            phase = PreviewPhase.Completed;
                            break;
                        }

                        transitionTargetIndex = settledPointIndex + 1;
                        phase = PreviewPhase.Transition;
                        phaseElapsedSeconds = 0.0f;
                    }

                    continue;
                }

                CameraPathResolvedPoint from = points[settledPointIndex];
                CameraPathResolvedPoint to = points[transitionTargetIndex];
                CameraPathTransitionSettings transition = to.TransitionFromPrevious;

                if (transition.Kind == CameraPathTransitionKind.Cut || transition.Duration <= 0.0f)
                {
                    ApplyPoint(to);
                    settledPointIndex = transitionTargetIndex;
                    transitionTargetIndex = -1;
                    phase = PreviewPhase.Hold;
                    phaseElapsedSeconds = 0.0f;
                    continue;
                }

                ApplyInterpolatedPoint(from, to, transition.Evaluate(phaseElapsedSeconds / transition.Duration));

                float transitionConsumed = Mathf.Min(deltaTime, transition.Duration - phaseElapsedSeconds);
                phaseElapsedSeconds += transitionConsumed;
                elapsedSeconds += transitionConsumed;
                deltaTime -= transitionConsumed;

                if (phaseElapsedSeconds >= transition.Duration)
                {
                    ApplyPoint(to);
                    settledPointIndex = transitionTargetIndex;
                    transitionTargetIndex = -1;
                    phase = PreviewPhase.Hold;
                    phaseElapsedSeconds = 0.0f;
                }
            }

            if (phase == PreviewPhase.Completed)
                ApplyPoint(points[points.Count - 1]);
        }

        private void ApplyPoint(in CameraPathResolvedPoint point)
        {
            CameraPathPlaybackPose pose = CameraPathPlaybackUtility.BuildPose(point);

            if ((targetMode & CameraPathPreviewTargetMode.SceneView) != 0 && sceneView?.camera != null)
            {
                if (sceneView.orthographic)
                    sceneView.orthographic = false;

                CameraPathPlaybackUtility.ApplyPose(sceneView.camera.transform, sceneView.camera, pose);
                sceneView.Repaint();
            }

            if ((targetMode & CameraPathPreviewTargetMode.PreviewCamera) != 0 && previewCamera != null)
            {
                CameraPathPlaybackUtility.ApplyPose(previewCamera.transform, previewCamera, pose);
            }
        }

        private void ApplyInterpolatedPoint(in CameraPathResolvedPoint from, in CameraPathResolvedPoint to, float t)
        {
            if ((targetMode & CameraPathPreviewTargetMode.SceneView) != 0 && sceneView?.camera != null)
            {
                if (sceneView.orthographic)
                    sceneView.orthographic = false;

                CameraPathPlaybackPose scenePose = CameraPathPlaybackUtility.BuildInterpolatedPose(
                    from,
                    to,
                    t,
                    CameraPathPlaybackUtility.GetFieldOfView(sceneView.camera));

                CameraPathPlaybackUtility.ApplyPose(sceneView.camera.transform, sceneView.camera, scenePose);
                sceneView.Repaint();
            }

            if ((targetMode & CameraPathPreviewTargetMode.PreviewCamera) != 0 && previewCamera != null)
            {
                CameraPathPlaybackPose previewPose = CameraPathPlaybackUtility.BuildInterpolatedPose(
                    from,
                    to,
                    t,
                    CameraPathPlaybackUtility.GetFieldOfView(previewCamera));

                CameraPathPlaybackUtility.ApplyPose(previewCamera.transform, previewCamera, previewPose);
            }
        }

        private static UnityCamera CreatePreviewCamera(CameraPathPreviewTargetMode targetMode)
        {
            if ((targetMode & CameraPathPreviewTargetMode.PreviewCamera) == 0)
                return null;

            GameObject cameraObject = EditorUtility.CreateGameObjectWithHideFlags(
                PreviewCameraName,
                HideFlags.DontSave,
                typeof(UnityCamera));

            UnityCamera camera = cameraObject.GetComponent<UnityCamera>();
            camera.enabled = true;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000.0f;
            camera.fieldOfView = 60.0f;
            return camera;
        }

        private static float ComputeTotalDuration(CameraPathSequenceDefinition sequence)
        {
            if (sequence == null || sequence.Count == 0)
                return 0.0f;

            float total = 0.0f;
            IReadOnlyList<CameraPathResolvedPoint> points = sequence.Points;

            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0)
                    total += points[i].TransitionFromPrevious.Duration;

                total += points[i].HoldSeconds;
            }

            return total;
        }

        private readonly struct SavedSceneViewState
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly float fieldOfView;
            private readonly bool orthographic;

            public SavedSceneViewState(SceneView sceneView)
            {
                if (sceneView?.camera == null)
                {
                    position = Vector3.zero;
                    rotation = Quaternion.identity;
                    fieldOfView = 60.0f;
                    orthographic = false;
                    return;
                }

                position = sceneView.camera.transform.position;
                rotation = sceneView.camera.transform.rotation;
                fieldOfView = sceneView.camera.fieldOfView;
                orthographic = sceneView.orthographic;
            }

            public void Restore(SceneView sceneView)
            {
                if (sceneView?.camera == null)
                    return;

                sceneView.orthographic = orthographic;
                sceneView.camera.transform.SetPositionAndRotation(position, rotation);
                sceneView.camera.fieldOfView = fieldOfView;
                sceneView.Repaint();
            }
        }
    }
}
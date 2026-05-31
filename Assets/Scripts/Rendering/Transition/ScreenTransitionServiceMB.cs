using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BC.Rendering.Transition
{
    [DisallowMultipleComponent]
    public sealed class ScreenTransitionServiceMB : MonoBehaviour
    {
        public static ScreenTransitionServiceMB Instance { get; private set; }

        [Header("Defaults")]
        [SerializeField] private ScreenTransitionProfileSO defaultProfile;

        [Header("Debug")]
        [SerializeField] private bool dontDestroyOnLoad;
        [SerializeField] private bool useUnscaledTime = true;

        private RenderTexture fromTexture;
        private RTHandle fromTextureHandle;
        private bool captureRequested;
        private bool hasCapturedFromTexture;
        private bool missingFromLogged;
        private bool isTransitioning;
        private float progress;
        private ScreenTransitionMode mode = ScreenTransitionMode.LinearCrossFade;
        private int sourceWidth;
        private int sourceHeight;
        private GraphicsFormat sourceFormat;
        private int transitionVersion;
        private CancellationTokenSource activeTransitionCts;
        private bool toReady;
        private bool waitForToReady;
        private ScreenTransitionProfileSO activeProfile;

        public bool IsTransitioning => isTransitioning;
        public float Progress => progress;
        public int LastCaptureFrame { get; private set; } = -1;
        public ScreenTransitionState State { get; private set; } = ScreenTransitionState.Idle;
        public bool IsToReady => toReady;

        internal bool CaptureRequested => captureRequested;
        internal bool HasCapturedFromTexture => hasCapturedFromTexture;
        internal ScreenTransitionMode ActiveMode => mode;
        internal RTHandle FromTextureHandle => fromTextureHandle;
        internal float ActiveFeather => activeProfile != null ? activeProfile.Feather : 0.04f;
        internal Vector2 ActiveDirection => activeProfile != null ? activeProfile.Direction : Vector2.right;
        internal Vector2 ActiveCenter => activeProfile != null ? activeProfile.Center : new Vector2(0.5f, 0.5f);
        internal Texture2D ActiveNoiseTexture => activeProfile != null ? activeProfile.NoiseTexture : null;
        internal float ActiveNoiseScale => activeProfile != null ? activeProfile.NoiseScale : 8f;
        internal float ActiveNoiseStrength => activeProfile != null ? activeProfile.NoiseStrength : 1f;
        internal float ActiveSeed => activeProfile != null ? activeProfile.Seed : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            ReleaseFromTexture();
        }

        private void OnDisable()
        {
            CancelActiveTransitionTask();
            ReleaseFromTexture();
            hasCapturedFromTexture = false;
            captureRequested = false;
            isTransitioning = false;
            progress = 0f;
            missingFromLogged = false;
            waitForToReady = false;
            toReady = false;
            State = ScreenTransitionState.Idle;
        }

        public void SetDefaultProfile(ScreenTransitionProfileSO profile)
        {
            defaultProfile = profile;
        }

        public async UniTask PlayAsync(ScreenTransitionRequest request, CancellationToken ct = default)
        {
            ScreenTransitionProfileSO profile = request.Profile != null
                ? request.Profile
                : defaultProfile;

            if (profile == null)
            {
                Debug.LogError($"[{nameof(ScreenTransitionServiceMB)}] Missing transition profile.", this);
                return;
            }

            if (request.CaptureFromCurrentFrame || !hasCapturedFromTexture)
                await CaptureCurrentFrameAsync(ct);

            float duration = request.OverrideDuration ?? profile.Duration;
            duration = Mathf.Max(0f, duration);
            activeProfile = profile;

            BeginTransition(profile.Mode);
            waitForToReady = request.WaitUntilToReady;

            if (waitForToReady)
            {
                SetState(ScreenTransitionState.LoadOrPrepareTo);
                while (isTransitioning && !toReady)
                {
                    ct.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }

            if (!isTransitioning)
                return;

            SetState(ScreenTransitionState.Transitioning);

            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            ReplaceActiveTransitionTokenSource(linkedCts);

            int expectedVersion = transitionVersion;
            CancellationToken linkedToken = linkedCts.Token;

            try
            {
                if (duration <= 0f)
                {
                    SkipToEnd();
                    return;
                }

                float elapsed = 0f;
                while (isTransitioning && expectedVersion == transitionVersion)
                {
                    linkedToken.ThrowIfCancellationRequested();

                    elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float eased = EvaluateEasing(profile.Easing, t);
                    progress = eased;

                    if (t >= 1f)
                    {
                        CompleteTransition();
                        break;
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, linkedToken);
                }
            }
            catch (OperationCanceledException)
            {
                if (ct.IsCancellationRequested)
                {
                    CancelCurrentTransition(ScreenTransitionCancelMode.CompleteImmediately);
                    throw;
                }
            }
            finally
            {
                if (ReferenceEquals(activeTransitionCts, linkedCts))
                    CancelActiveTransitionTask();
            }
        }

        public async UniTask CaptureCurrentFrameAsync(CancellationToken ct = default)
        {
            RequestCaptureFromCurrentFrame();

            while (captureRequested || !hasCapturedFromTexture)
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, ct);
            }
        }

        public void StartTransition(bool captureFromCurrentFrame = true)
        {
            ScreenTransitionMode targetMode = defaultProfile != null
                ? defaultProfile.Mode
                : ScreenTransitionMode.LinearCrossFade;

            StartTransition(targetMode, captureFromCurrentFrame);
        }

        public void StartTransition(ScreenTransitionMode transitionMode, bool captureFromCurrentFrame = true)
        {
            activeProfile = defaultProfile;
            BeginTransition(transitionMode);

            if (captureFromCurrentFrame)
                RequestCaptureFromCurrentFrame();
        }

        public void RequestCaptureFromCurrentFrame()
        {
            captureRequested = true;
            hasCapturedFromTexture = false;
            missingFromLogged = false;
            SetState(ScreenTransitionState.CaptureFrom);
        }

        public void SetToReady(bool ready = true)
        {
            toReady = ready;
            if (ready && waitForToReady && isTransitioning && State == ScreenTransitionState.LoadOrPrepareTo)
                SetState(ScreenTransitionState.Transitioning);
        }

        public void ResetToReady()
        {
            toReady = false;
        }

        public void BeginHoldFromDuringLoad(ScreenTransitionMode transitionMode)
        {
            activeProfile = defaultProfile;
            BeginTransition(transitionMode);
            waitForToReady = true;
            SetState(ScreenTransitionState.LoadOrPrepareTo);
        }

        public void SetProgress(float normalizedProgress)
        {
            progress = Mathf.Clamp01(normalizedProgress);
        }

        public void SkipToEnd()
        {
            CompleteTransition();
        }

        public void CompleteTransition()
        {
            progress = 1f;
            isTransitioning = false;
            captureRequested = false;
            missingFromLogged = false;
            waitForToReady = false;
            toReady = false;
            transitionVersion++;
            SetState(ScreenTransitionState.Complete);
            CancelActiveTransitionTask();
            SetState(ScreenTransitionState.Idle);
        }

        public void CancelTransition()
        {
            CancelCurrentTransition(ScreenTransitionCancelMode.ReturnToFrom);
        }

        public void CancelCurrentTransition(ScreenTransitionCancelMode mode)
        {
            switch (mode)
            {
                case ScreenTransitionCancelMode.CompleteImmediately:
                    progress = 1f;
                    break;
                case ScreenTransitionCancelMode.HoldCurrentVisual:
                    progress = Mathf.Clamp01(progress);
                    break;
                case ScreenTransitionCancelMode.ReturnToFrom:
                    progress = 0f;
                    break;
                case ScreenTransitionCancelMode.HardStop:
                    progress = 0f;
                    hasCapturedFromTexture = false;
                    captureRequested = false;
                    ReleaseFromTexture();
                    break;
            }

            isTransitioning = false;
            missingFromLogged = false;
            waitForToReady = false;
            toReady = false;
            transitionVersion++;
            SetState(ScreenTransitionState.Idle);
            CancelActiveTransitionTask();
        }

        internal bool EnsureFromTexture(int width, int height, GraphicsFormat format)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);

            bool needsRecreate =
                fromTexture == null ||
                fromTextureHandle == null ||
                sourceWidth != width ||
                sourceHeight != height ||
                sourceFormat != format;

            if (!needsRecreate)
                return true;

            ReleaseFromTexture();

            var descriptor = new RenderTextureDescriptor(width, height)
            {
                graphicsFormat = format,
                depthBufferBits = 0,
                msaaSamples = 1,
                mipCount = 1,
                useMipMap = false,
                autoGenerateMips = false,
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear,
            };

            fromTexture = new RenderTexture(descriptor)
            {
                name = "BC.ScreenTransition.FromTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            if (!fromTexture.Create())
            {
                Debug.LogError($"[{nameof(ScreenTransitionServiceMB)}] Failed to create from texture ({width}x{height}, {format}).", this);
                ReleaseFromTexture();
                return false;
            }

            fromTextureHandle = RTHandles.Alloc(fromTexture);
            sourceWidth = width;
            sourceHeight = height;
            sourceFormat = format;
            hasCapturedFromTexture = false;
            return true;
        }

        internal void NotifyFromCaptureCompleted()
        {
            captureRequested = false;
            hasCapturedFromTexture = true;
            missingFromLogged = false;
            LastCaptureFrame = Time.frameCount;

            if (!isTransitioning)
                SetState(ScreenTransitionState.HoldFrom);
        }

        internal void ReportMissingFromTextureIfNeeded()
        {
            if (missingFromLogged)
                return;

            missingFromLogged = true;
            Debug.LogError($"[{nameof(ScreenTransitionServiceMB)}] Transition requested without valid from texture. Request capture before progressing.", this);
        }

        private void ReleaseFromTexture()
        {
            if (fromTextureHandle != null)
            {
                fromTextureHandle.Release();
                fromTextureHandle = null;
            }

            if (fromTexture != null)
            {
                if (fromTexture.IsCreated())
                    fromTexture.Release();

                Destroy(fromTexture);
                fromTexture = null;
            }

            sourceWidth = 0;
            sourceHeight = 0;
            sourceFormat = GraphicsFormat.None;
        }

        private void BeginTransition(ScreenTransitionMode transitionMode)
        {
            mode = transitionMode;
            isTransitioning = true;
            progress = 0f;
            missingFromLogged = false;
            waitForToReady = false;
            toReady = false;
            transitionVersion++;
            SetState(ScreenTransitionState.HoldFrom);
        }

        private static float EvaluateEasing(AnimationCurve curve, float t)
        {
            if (curve == null || curve.length == 0)
                return t;

            return Mathf.Clamp01(curve.Evaluate(t));
        }

        private void ReplaceActiveTransitionTokenSource(CancellationTokenSource next)
        {
            CancelActiveTransitionTask();
            activeTransitionCts = next;
        }

        private void CancelActiveTransitionTask()
        {
            if (activeTransitionCts == null)
                return;

            activeTransitionCts.Cancel();
            activeTransitionCts.Dispose();
            activeTransitionCts = null;
        }

        private void SetState(ScreenTransitionState state)
        {
            State = state;
        }
    }
}

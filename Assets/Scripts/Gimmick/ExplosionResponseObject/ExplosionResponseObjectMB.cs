using System;
using System.Collections;
using BC.ActionSystem;
using BC.Audio;
using BC.Base;
using BC.Bomb;
using BC.Stage;
using BC.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Gimmick.ExplosionResponseObject
{
    public enum ExplosionResponseMode
    {
        Timer = 0,
        Once = 1,
        Toggle = 2,
    }

    [DisallowMultipleComponent]
    public sealed class ExplosionResponseObjectMB : MonoBehaviour, IExplosionImpactReceiver, IBombImpactReceiver, BC.Stage.Snapshot.IStageStateRestorable
    {
        [Header("Impact")]
        [Tooltip("この値以上の爆風を受けた時だけ反応します。")]
        [SerializeField, Min(0f)] private float minimumImpactForce;
        [Tooltip("爆風を受けた後の状態遷移モードです。")]
        [SerializeField] private ExplosionResponseMode mode = ExplosionResponseMode.Timer;
        [ShowIf(nameof(UsesTimerMode))]
        [Tooltip("Timer モード時に On を維持する秒数です。再爆風を受けるとこの時間で延長されます。")]
        [SerializeField, Min(0.01f)] private float activeDuration = 1.0f;

        [Header("Visual")]
        [Tooltip("状態表示に使う Renderer 群です。未指定時は子階層から自動取得します。")]
        [SerializeField] private Renderer[] targetRenderers = Array.Empty<Renderer>();
        [Tooltip("Renderer に EnvironmentStylizedLit の SimpleBoost プロパティも書き込みます。")]
        [SerializeField] private bool syncEnvironmentSimpleBoost = true;
        [Tooltip("Off 状態の見た目です。")]
        [SerializeField] private RendererVisualState offVisual = new(Color.white, false, Color.white, 0f, 0f);
        [Tooltip("On 状態の見た目です。")]
        [SerializeField] private RendererVisualState onVisual = new(Color.white, true, Color.white, 1.0f, 0.0f);

        [Header("InlineAction")]
        [Tooltip("状態が On に変わった直後に実行する InlineAction です。")]
        [SerializeField] private InlineAction onActivatedInlineAction;
        [Tooltip("状態が Off に変わった直後に実行する InlineAction です。")]
        [SerializeField] private InlineAction onDeactivatedInlineAction;

        [Header("Sound")]
        [Tooltip("爆風を検出したときに再生するサウンドです。")]
        [SerializeField] private AudioDataSO explosionDetectedSound;

        [Header("Debug")]
        [Tooltip("実行中の現在状態です。")]
        [SerializeField, ReadOnly] private bool isActive;
        [Tooltip("最後に受けた爆風の強さです。")]
        [SerializeField, ReadOnly] private float lastImpactForce;

        private MaterialPropertyBlock propertyBlock;
        private EntityMB selfEntityMB;
        private Coroutine timerCoroutine;
        private float timerEndTime;
        private float pendingRestoreTimerRemaining;
        private bool suppressNextEnableReset;

        public bool IsActive => isActive;
        public float LastImpactForce => lastImpactForce;

        private bool UsesTimerMode => mode == ExplosionResponseMode.Timer;

        private void Reset()
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
            EnsurePropertyBlock();
            ApplyVisual();
        }

        private void Awake()
        {
            EnsurePropertyBlock();
            ResolveReferences();
            ResetRuntimeState();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            ResolveReferences();

            if (suppressNextEnableReset)
            {
                suppressNextEnableReset = false;
                ApplyVisual();

                if (pendingRestoreTimerRemaining > 0f && UsesTimerMode && isActive)
                    StartTimerWithDuration(pendingRestoreTimerRemaining);

                pendingRestoreTimerRemaining = 0f;
                return;
            }

            ResetRuntimeState();
        }

        private void OnDisable()
        {
            StopTimer();
        }

        private void OnValidate()
        {
            minimumImpactForce = Mathf.Max(0f, minimumImpactForce);
            activeDuration = Mathf.Max(0.01f, activeDuration);

            if (!Application.isPlaying && (targetRenderers == null || targetRenderers.Length == 0))
                targetRenderers = GetComponentsInChildren<Renderer>(true);

            if (!Application.isPlaying)
            {
                EnsurePropertyBlock();
                isActive = false;
                ApplyVisual();
            }
        }

        public void OnExplosionImpactReceived(Vector3 direction, float impactForce)
        {
            if (!isActiveAndEnabled)
                return;

            lastImpactForce = Mathf.Max(0f, impactForce);
            if (lastImpactForce < minimumImpactForce)
                return;

            TryPlayExplosionDetectedSound();

            HandleImpact();
        }

        public void OnBombImpactReceived(Vector3 direction, float impactForce)
        {
            OnExplosionImpactReceived(direction, impactForce);
        }

        private void HandleImpact()
        {
            switch (mode)
            {
                case ExplosionResponseMode.Once:
                    if (!isActive)
                        SetActiveState(true, invokeActions: true);
                    break;

                case ExplosionResponseMode.Toggle:
                    SetActiveState(!isActive, invokeActions: true);
                    break;

                case ExplosionResponseMode.Timer:
                default:
                    if (!isActive)
                        SetActiveState(true, invokeActions: true);

                    // Timer モードは再度 On を実行せず、期限だけ延長します。
                    RestartTimer();
                    break;
            }
        }

        private void ResetRuntimeState()
        {
            StopTimer();
            isActive = false;
            lastImpactForce = 0f;
            pendingRestoreTimerRemaining = 0f;
            suppressNextEnableReset = false;
            ApplyVisual();
        }

        private void SetActiveState(bool nextActive, bool invokeActions)
        {
            if (isActive == nextActive)
                return;

            InlineAction inlineAction = nextActive ? onActivatedInlineAction : onDeactivatedInlineAction;
            if (invokeActions && !CanExecuteInlineAction(inlineAction, nextActive, out string failureReason))
            {
                Debug.LogError($"{nameof(ExplosionResponseObjectMB)}: {failureReason}", this);
                return;
            }

            isActive = nextActive;
            ApplyVisual();

            if (!invokeActions)
                return;

            ExecuteInlineAction(inlineAction, nextActive);
        }

        private void RestartTimer()
        {
            StartTimerWithDuration(activeDuration);
        }

        private void StopTimer()
        {
            if (timerCoroutine == null)
            {
                timerEndTime = 0f;
                return;
            }

            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
            timerEndTime = 0f;
        }

        private IEnumerator DeactivateAfterDelayCoroutine()
        {
            while (Time.time < timerEndTime)
                yield return null;

            timerCoroutine = null;
            timerEndTime = 0f;
            SetActiveState(false, invokeActions: true);
        }

        private void ResolveReferences()
        {
            if (selfEntityMB == null)
                selfEntityMB = GetComponentInParent<EntityMB>();
        }

        private void ApplyVisual()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                return;

            EnsurePropertyBlock();
            RendererVisualState currentVisual = isActive ? onVisual : offVisual;

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer targetRenderer = targetRenderers[i];
                if (targetRenderer == null)
                    continue;

                RendererVisualStateUtility.Apply(targetRenderer, currentVisual, syncEnvironmentSimpleBoost, propertyBlock);
            }
        }

        private bool CanExecuteInlineAction(InlineAction inlineAction, bool nextActive, out string failureReason)
        {
            failureReason = null;
            if (inlineAction == null)
                return true;

            ResolveReferences();

            if (selfEntityMB == null || !selfEntityMB.HasEntity)
            {
                failureReason = $"State transition to {(nextActive ? "On" : "Off")} requires a valid self Entity because an InlineAction is configured.";
                return false;
            }

            if (!InlineActionExecutionUtility.TryResolveSceneKernel(this, out SceneKernel resolvedKernel) || resolvedKernel.Actions == null)
            {
                failureReason = $"State transition to {(nextActive ? "On" : "Off")} requires SceneKernel.Actions, but it was not available.";
                return false;
            }

            return true;
        }

        private void ExecuteInlineAction(InlineAction inlineAction, bool nextActive)
        {
            if (inlineAction == null)
                return;

            InlineActionExecutionUtility.ExecuteAndForget(
                this,
                selfEntityMB.Entity,
                inlineAction,
                default,
                $"{nameof(ExplosionResponseObjectMB)}.{(nextActive ? "On" : "Off")}");
        }

        private void TryPlayExplosionDetectedSound()
        {
            if (explosionDetectedSound == null || explosionDetectedSound.Clip == null)
                return;

            if (AudioSystemMB.Instance == null)
            {
                Debug.LogWarning($"{nameof(ExplosionResponseObjectMB)}: Explosion detected sound is configured but {nameof(AudioSystemMB)} is unavailable.", this);
                return;
            }

            if (!AudioSystemMB.Instance.TryPlaySE(explosionDetectedSound))
            {
                Debug.LogWarning($"{nameof(ExplosionResponseObjectMB)}: Failed to play configured explosion detected sound '{explosionDetectedSound.Clip.name}'.", this);
            }
        }

        private void EnsurePropertyBlock()
        {
            propertyBlock ??= new MaterialPropertyBlock();
        }

        public object CaptureStageState()
        {
            return new ExplosionResponseCheckpointState(
                isActive,
                lastImpactForce,
                UsesTimerMode ? GetRemainingTimerSeconds() : 0f);
        }

        public void RestoreStageState(object state)
        {
            if (state is not ExplosionResponseCheckpointState checkpoint)
                return;

            StopTimer();

            bool restoredActive = checkpoint.IsActive;
            float restoredTimerRemaining = 0f;

            if (UsesTimerMode)
            {
                restoredTimerRemaining = Mathf.Max(0f, checkpoint.TimerRemainingSeconds);
                restoredActive = restoredActive && restoredTimerRemaining > 0f;
            }

            isActive = restoredActive;
            lastImpactForce = Mathf.Max(0f, checkpoint.LastImpactForce);
            ApplyVisual();

            if (UsesTimerMode && restoredActive)
            {
                if (isActiveAndEnabled)
                {
                    StartTimerWithDuration(restoredTimerRemaining);
                }
                else
                {
                    suppressNextEnableReset = true;
                    pendingRestoreTimerRemaining = restoredTimerRemaining;
                }
            }
            else if (!isActiveAndEnabled)
            {
                suppressNextEnableReset = true;
                pendingRestoreTimerRemaining = 0f;
            }
        }

        private void StartTimerWithDuration(float duration)
        {
            StopTimer();

            float clampedDuration = Mathf.Max(0.01f, duration);
            timerEndTime = Time.time + clampedDuration;
            timerCoroutine = StartCoroutine(DeactivateAfterDelayCoroutine());
        }

        private float GetRemainingTimerSeconds()
        {
            if (timerCoroutine == null || timerEndTime <= 0f)
                return 0f;

            return Mathf.Max(0f, timerEndTime - Time.time);
        }

        [Serializable]
        private sealed class ExplosionResponseCheckpointState
        {
            public ExplosionResponseCheckpointState(bool isActive, float lastImpactForce, float timerRemainingSeconds)
            {
                IsActive = isActive;
                LastImpactForce = lastImpactForce;
                TimerRemainingSeconds = timerRemainingSeconds;
            }

            public bool IsActive { get; }
            public float LastImpactForce { get; }
            public float TimerRemainingSeconds { get; }
        }
    }
}

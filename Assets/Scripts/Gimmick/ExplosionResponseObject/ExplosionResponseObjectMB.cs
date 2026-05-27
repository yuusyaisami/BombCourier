using System;
using System.Collections;
using BC.ActionSystem;
using BC.Base;
using BC.Bomb;
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
    public sealed class ExplosionResponseObjectMB : MonoBehaviour, IExplosionImpactReceiver, IBombImpactReceiver
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

        [Header("Debug")]
        [Tooltip("実行中の現在状態です。")]
        [SerializeField, ReadOnly] private bool isActive;
        [Tooltip("最後に受けた爆風の強さです。")]
        [SerializeField, ReadOnly] private float lastImpactForce;

        private MaterialPropertyBlock propertyBlock;
        private EntityMB selfEntityMB;
        private Coroutine timerCoroutine;

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
            ApplyVisual();
        }

        private void SetActiveState(bool nextActive, bool invokeActions)
        {
            if (isActive == nextActive)
                return;

            isActive = nextActive;
            ApplyVisual();

            if (!invokeActions)
                return;

            ExecuteInlineAction(isActive ? onActivatedInlineAction : onDeactivatedInlineAction);
        }

        private void RestartTimer()
        {
            StopTimer();
            timerCoroutine = StartCoroutine(DeactivateAfterDelayCoroutine());
        }

        private void StopTimer()
        {
            if (timerCoroutine == null)
                return;

            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        private IEnumerator DeactivateAfterDelayCoroutine()
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, activeDuration));
            timerCoroutine = null;
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

        private void ExecuteInlineAction(InlineAction inlineAction)
        {
            if (inlineAction == null)
                return;

            ResolveReferences();

            if (selfEntityMB == null || !selfEntityMB.HasEntity)
            {
                Debug.LogWarning($"{nameof(ExplosionResponseObjectMB)}: InlineAction was skipped because self Entity is not available.", this);
                return;
            }

            InlineActionExecutionUtility.ExecuteAndForget(
                this,
                selfEntityMB.Entity,
                inlineAction,
                default,
                $"{nameof(ExplosionResponseObjectMB)}.{(isActive ? "On" : "Off")}");
        }

        private void EnsurePropertyBlock()
        {
            propertyBlock ??= new MaterialPropertyBlock();
        }
    }
}
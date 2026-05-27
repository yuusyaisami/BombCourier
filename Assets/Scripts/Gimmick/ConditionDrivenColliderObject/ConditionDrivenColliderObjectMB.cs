using System;
using BC.Base;
using BC.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Gimmick.ConditionDrivenColliderObject
{
    [DisallowMultipleComponent]
    public sealed class ConditionDrivenColliderObjectMB : MonoBehaviour
    {
        [Header("Condition")]
        [Tooltip("Collider 有効状態の判定に使う ReactiveBool です。")]
        [SerializeField] private ReactiveBool condition = ReactiveBool.LiteralValue(false);
        [Tooltip("condition が true の時に Collider を有効化するかを指定します。false にすると反転します。")]
        [SerializeField] private bool enableColliderWhenConditionTrue = true;

        [Header("Targets")]
        [Tooltip("制御対象の Collider 群です。Reset 時に候補を自動設定します。")]
        [SerializeField] private Collider[] targetColliders = Array.Empty<Collider>();
        [Tooltip("半透明表示を適用する Renderer 群です。未指定時は子階層から自動取得します。")]
        [SerializeField] private Renderer[] targetRenderers = Array.Empty<Renderer>();

        [Header("Visual")]
        [Tooltip("Collider が有効な時の alpha 値です。")]
        [SerializeField, Range(0f, 1f)] private float enabledAlpha = 1.0f;
        [Tooltip("Collider が無効な時の alpha 値です。")]
        [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.45f;

        [Header("Debug")]
        [SerializeField, ReadOnly] private bool conditionReadSucceeded;
        [SerializeField, ReadOnly] private bool currentConditionValue;
        [SerializeField, ReadOnly] private bool currentColliderEnabled;

        private MaterialPropertyBlock propertyBlock;
        private ReactiveBoolBinding conditionBinding;
        private ReactiveValueResolverService fallbackReactiveResolver;
        private SceneKernel sceneKernel;
        private EntityMB selfEntityMB;
        private Color[] rendererBaseColors = Array.Empty<Color>();
        private bool hasAppliedState;

        public bool ConditionReadSucceeded => conditionReadSucceeded;
        public bool CurrentConditionValue => currentConditionValue;
        public bool CurrentColliderEnabled => currentColliderEnabled;

        private void Reset()
        {
            targetColliders = GetComponentsInChildren<Collider>(true);
            targetRenderers = GetComponentsInChildren<Renderer>(true);
            EnsurePropertyBlock();
            CacheRendererBaseColors();
        }

        private void Awake()
        {
            EnsurePropertyBlock();
            ResolveReferences();
            EnsureTargetRenderers();
            CacheRendererBaseColors();
            RebuildConditionBinding();
        }

        private void OnEnable()
        {
            EnsurePropertyBlock();
            ResolveReferences();
            EnsureTargetRenderers();
            CacheRendererBaseColors();
            RebuildConditionBinding();
            RefreshConditionState(force: true);
        }

        private void Update()
        {
            if (conditionBinding == null)
                return;

            if (!conditionBinding.IsDirty && hasAppliedState)
                return;

            RefreshConditionState(force: false);
        }

        private void OnDisable()
        {
            DisposeConditionBinding();
            hasAppliedState = false;
        }

        private void OnValidate()
        {
            enabledAlpha = Mathf.Clamp01(enabledAlpha);
            disabledAlpha = Mathf.Clamp01(disabledAlpha);

            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>(true);

            if (!Application.isPlaying)
            {
                EnsurePropertyBlock();
                CacheRendererBaseColors();
                ApplyVisualState(ResolveAuthoringColliderState());
                return;
            }

            EnsurePropertyBlock();
            ResolveReferences();
            CacheRendererBaseColors();
            RebuildConditionBinding();
            RefreshConditionState(force: true);
        }

        private void ResolveReferences()
        {
            if (sceneKernel == null)
            {
                SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();
                if (kernelMB != null)
                    sceneKernel = kernelMB.Kernel;
            }

            if (selfEntityMB == null)
                selfEntityMB = GetComponentInParent<EntityMB>();

            fallbackReactiveResolver ??= new ReactiveValueResolverService(null);
        }

        private void RebuildConditionBinding()
        {
            DisposeConditionBinding();

            ReactiveValueResolverService resolver = sceneKernel != null && sceneKernel.ReactiveValues != null
                ? sceneKernel.ReactiveValues
                : fallbackReactiveResolver;

            conditionBinding = new ReactiveBoolBinding(
                resolver,
                BuildReactiveEvalContext(),
                condition,
                ResolveEvaluationMode(),
                condition.FailurePolicy);
        }

        private void RefreshConditionState(bool force)
        {
            if (conditionBinding == null)
            {
                conditionReadSucceeded = false;
                return;
            }

            ReactiveResult<bool> result = conditionBinding.Read();
            conditionReadSucceeded = result.Success;

            if (!result.Success)
                return;

            currentConditionValue = result.Value;
            bool nextColliderEnabled = enableColliderWhenConditionTrue
                ? currentConditionValue
                : !currentConditionValue;

            if (!force && hasAppliedState && currentColliderEnabled == nextColliderEnabled)
                return;

            currentColliderEnabled = nextColliderEnabled;
            ApplyColliderState(currentColliderEnabled);
            ApplyVisualState(currentColliderEnabled);
            hasAppliedState = true;
        }

        private void ApplyColliderState(bool isEnabled)
        {
            if (targetColliders == null)
                return;

            for (int i = 0; i < targetColliders.Length; i++)
            {
                Collider targetCollider = targetColliders[i];
                if (targetCollider == null)
                    continue;

                targetCollider.enabled = isEnabled;
            }
        }

        private void ApplyVisualState(bool isColliderEnabled)
        {
            EnsureTargetRenderers();
            if (targetRenderers == null || targetRenderers.Length == 0)
                return;

            EnsurePropertyBlock();
            CacheRendererBaseColors();

            float alpha = isColliderEnabled ? enabledAlpha : disabledAlpha;
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer targetRenderer = targetRenderers[i];
                if (targetRenderer == null)
                    continue;

                Color baseColor = rendererBaseColors.Length > i
                    ? rendererBaseColors[i]
                    : RendererVisualStateUtility.ResolveBaseColor(targetRenderer);

                RendererVisualState visualState = RendererVisualState.FromBaseColor(baseColor).WithAlpha(alpha);
                RendererVisualStateUtility.Apply(targetRenderer, visualState, false, propertyBlock);
            }
        }

        private void EnsureTargetRenderers()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void CacheRendererBaseColors()
        {
            EnsureTargetRenderers();

            if (targetRenderers == null)
            {
                rendererBaseColors = Array.Empty<Color>();
                return;
            }

            if (rendererBaseColors.Length != targetRenderers.Length)
                rendererBaseColors = new Color[targetRenderers.Length];

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer targetRenderer = targetRenderers[i];
                rendererBaseColors[i] = targetRenderer != null
                    ? RendererVisualStateUtility.ResolveBaseColor(targetRenderer)
                    : Color.white;
            }
        }

        private bool ResolveAuthoringColliderState()
        {
            if (targetColliders == null || targetColliders.Length == 0)
                return true;

            bool hasAnyCollider = false;
            for (int i = 0; i < targetColliders.Length; i++)
            {
                Collider targetCollider = targetColliders[i];
                if (targetCollider == null)
                    continue;

                hasAnyCollider = true;
                if (!targetCollider.enabled)
                    return false;
            }

            return hasAnyCollider;
        }

        private ReactiveEvalContext BuildReactiveEvalContext()
        {
            EntityRef selfEntity = selfEntityMB != null && selfEntityMB.HasEntity
                ? selfEntityMB.Entity
                : default;

            return new ReactiveEvalContext(sceneKernel, selfEntity, default);
        }

        private ReactiveEvaluationMode ResolveEvaluationMode()
        {
            return condition.SourceKind switch
            {
                ReactiveBoolSourceKind.Literal => ReactiveEvaluationMode.Snapshot,
                ReactiveBoolSourceKind.EntityValueStore => ReactiveEvaluationMode.Watched,
                ReactiveBoolSourceKind.KernelValueStore => ReactiveEvaluationMode.Watched,
                _ => ReactiveEvaluationMode.Continuous,
            };
        }

        private void DisposeConditionBinding()
        {
            if (conditionBinding == null)
                return;

            conditionBinding.Dispose();
            conditionBinding = null;
        }

        private void EnsurePropertyBlock()
        {
            propertyBlock ??= new MaterialPropertyBlock();
        }
    }
}
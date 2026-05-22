using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.Character
{
    public enum ShapeExpressionPlayMode
    {
        Once = 0,
        Loop = 1,
        PingPong = 2,
    }

    [Serializable]
    public struct ShapeExpressionTransitionSettings
    {
        [SerializeField, Min(0f)] private float duration;
        [SerializeField] private AnimationCurve ease;

        public float Duration => Mathf.Max(0f, duration);

        public float Evaluate(float t)
        {
            t = Mathf.Clamp01(t);
            return ease != null ? Mathf.Clamp01(ease.Evaluate(t)) : t;
        }

        public static ShapeExpressionTransitionSettings Immediate()
        {
            return new ShapeExpressionTransitionSettings
            {
                duration = 0f,
                ease = null,
            };
        }
    }

    [DisallowMultipleComponent]
    public sealed class ShapeExpressionControllerMB : MonoBehaviour
    {
#pragma warning disable CS0649
        [Serializable]
        private struct ShapeWeightPoint
        {
            public string alias;
            [Range(0f, 100f)]
            public float weight;
        }

        [Serializable]
        private struct ShapeExpressionStep
        {
            [Min(0f)] public float duration;
            public AnimationCurve ease;
            public ShapeWeightPoint[] points;
        }

        [Serializable]
        private struct ShapeExpressionEntry
        {
            public ShapeExpressionId expressionId;
            public ShapeExpressionPlayMode playMode;
            public bool resetUnspecifiedToDefaultWeights;
            public ShapeExpressionTransitionSettings enterTransition;
            public ShapeExpressionStep[] steps;
        }

        private struct CompiledPoint
        {
            public int AliasIndex;
            public float Weight;
        }

        private struct CompiledStep
        {
            public float Duration;
            public AnimationCurve Ease;
            public CompiledPoint[] Points;
        }

        private struct CompiledEntry
        {
            public ShapeExpressionId ExpressionId;
            public ShapeExpressionPlayMode PlayMode;
            public bool ResetUnspecifiedToDefaultWeights;
            public ShapeExpressionTransitionSettings EnterTransition;
            public CompiledStep[] Steps;
        }
#pragma warning restore CS0649

        [Header("Dependencies")]
        [SerializeField] private ShapeBlendMappingMB blendMapping;

        [Header("Runtime Binding")]
        [SerializeField] private bool bindToRuntimeShapeExpression = true;

        [Header("Authored Expressions")]
        [SerializeField] private ShapeExpressionEntry[] expressions = Array.Empty<ShapeExpressionEntry>();
        [Header("Debug")]
        [SerializeField] private ShapeExpressionId debugExpressionId; // 現在の表情を表示するためだけのフィールド。実行中に変更して動作確認できる。

        private ValueStoreService valueStore;
        private EntityRef entityRef;
        private ValueWatchHandle<ShapeExpressionId> expressionHandle;
        private EventSubscription expressionSubscription;
        private CompiledEntry[] compiledEntries = Array.Empty<CompiledEntry>();

        private float[] defaultWeights = Array.Empty<float>();
        private float[] sampledWeights = Array.Empty<float>();
        private float[] segmentFrom = Array.Empty<float>();
        private float[] segmentTo = Array.Empty<float>();

        private ShapeExpressionId currentExpression = ShapeExpressionId.Neutral;
        private int activeEntryIndex = -1;
        private int activeStepIndex;
        private int activeStepDirection = 1;
        private float segmentDuration;
        private float segmentElapsed;
        private AnimationCurve segmentEase;
        private bool segmentActive;
        private bool enteringExpression;
        private bool started;

        public ShapeExpressionId CurrentExpression => currentExpression;

        private void Reset()
        {
            ResolveSerializedReferences();
        }

        private void OnValidate()
        {
            ResolveSerializedReferences();
        }

        private void Start()
        {
            started = true;
            ResolveSerializedReferences();

            if (!ValidateDependencies(logErrors: true) || !BuildRuntimeCache(logWarnings: true))
                return;

            if (bindToRuntimeShapeExpression)
            {
                EnsureRuntimeBinding();
            }
            else
            {
                SetExpression(currentExpression);
            }
        }

        private void OnEnable()
        {
            if (!started)
                return;

            ResolveSerializedReferences();

            if (!ValidateDependencies(logErrors: false) || !BuildRuntimeCache(logWarnings: false))
                return;

            if (bindToRuntimeShapeExpression)
            {
                EnsureRuntimeBinding();
            }
            else
            {
                SetExpression(currentExpression);
            }
        }

        private void LateUpdate()
        {
            // デバッグ用
            debugExpressionId = currentExpression;
            if (!started || activeEntryIndex < 0 || sampledWeights.Length == 0)
                return;

            if (segmentActive)
            {
                TickSegment(Time.deltaTime);
            }
            else
            {
                TryQueueNextSegment();
            }

            ApplyCurrentSample();
        }

        public void SetExpression(ShapeExpressionId expressionId)
        {
            if (!BuildRuntimeCache(logWarnings: false))
                return;

            currentExpression = expressionId;

            if (!TryFindCompiledEntry(expressionId, out int entryIndex))
            {
                activeEntryIndex = -1;
                segmentActive = false;
                blendMapping.ResetMappedWeights();

                // Keep sampled values in sync after a full reset, otherwise later transitions jump.
                for (int i = 0; i < sampledWeights.Length; i++)
                    sampledWeights[i] = defaultWeights[i];

                return;
            }

            activeEntryIndex = entryIndex;
            activeStepIndex = 0;
            activeStepDirection = 1;
            enteringExpression = true;

            CompiledEntry entry = compiledEntries[activeEntryIndex];

            // Expression switch always starts from the currently sampled pose,
            // so even abrupt key changes blend without popping.
            CopyArray(sampledWeights, segmentFrom);
            BuildStepTarget(entry, 0, sampledWeights, segmentTo);

            segmentDuration = entry.EnterTransition.Duration;
            segmentElapsed = 0f;
            segmentEase = null;
            segmentActive = segmentDuration > 0f;

            if (!segmentActive)
            {
                CopyArray(segmentTo, sampledWeights);
                enteringExpression = false;
            }
        }

        private void ResolveSerializedReferences()
        {
            if (blendMapping == null)
                blendMapping = GetComponent<ShapeBlendMappingMB>();
        }

        private bool ValidateDependencies(bool logErrors)
        {
            if (blendMapping != null)
                return true;

            if (logErrors)
                Debug.LogError($"{nameof(ShapeExpressionControllerMB)}: {nameof(ShapeBlendMappingMB)} is missing.", this);

            enabled = false;
            return false;
        }

        private bool BuildRuntimeCache(bool logWarnings)
        {
            if (blendMapping == null)
                return false;

            if (defaultWeights.Length > 0 && compiledEntries.Length > 0)
                return true;

            int aliasCount = blendMapping.AliasCount;

            if (aliasCount <= 0)
            {
                defaultWeights = Array.Empty<float>();
                sampledWeights = Array.Empty<float>();
                segmentFrom = Array.Empty<float>();
                segmentTo = Array.Empty<float>();
                compiledEntries = Array.Empty<CompiledEntry>();

                if (logWarnings)
                    Debug.LogWarning($"{nameof(ShapeExpressionControllerMB)}: no aliases were resolved from {nameof(ShapeBlendMappingMB)}.", this);

                return false;
            }

            defaultWeights = new float[aliasCount];
            sampledWeights = new float[aliasCount];
            segmentFrom = new float[aliasCount];
            segmentTo = new float[aliasCount];

            for (int i = 0; i < aliasCount; i++)
            {
                defaultWeights[i] = blendMapping.TryGetDefaultWeight(i, out float defaultWeight) ? defaultWeight : 0f;
                sampledWeights[i] = defaultWeights[i];
            }

            List<CompiledEntry> built = new(expressions != null ? expressions.Length : 0);

            if (expressions != null)
            {
                for (int i = 0; i < expressions.Length; i++)
                {
                    ShapeExpressionEntry source = expressions[i];
                    CompiledStep[] compiledSteps = CompileSteps(source.steps, i, logWarnings);

                    if (compiledSteps.Length == 0)
                        continue;

                    built.Add(new CompiledEntry
                    {
                        ExpressionId = source.expressionId,
                        PlayMode = source.playMode,
                        ResetUnspecifiedToDefaultWeights = source.resetUnspecifiedToDefaultWeights,
                        EnterTransition = source.enterTransition,
                        Steps = compiledSteps,
                    });
                }
            }

            compiledEntries = built.ToArray();
            return compiledEntries.Length > 0;
        }

        private CompiledStep[] CompileSteps(ShapeExpressionStep[] steps, int entryIndex, bool logWarnings)
        {
            if (steps == null || steps.Length == 0)
                return Array.Empty<CompiledStep>();

            CompiledStep[] compiled = new CompiledStep[steps.Length];

            for (int stepIndex = 0; stepIndex < steps.Length; stepIndex++)
            {
                ShapeExpressionStep sourceStep = steps[stepIndex];
                List<CompiledPoint> points = new(sourceStep.points != null ? sourceStep.points.Length : 0);

                if (sourceStep.points != null)
                {
                    for (int pointIndex = 0; pointIndex < sourceStep.points.Length; pointIndex++)
                    {
                        ShapeWeightPoint point = sourceStep.points[pointIndex];
                        if (string.IsNullOrWhiteSpace(point.alias))
                            continue;

                        if (!blendMapping.TryGetAliasIndex(point.alias.Trim(), out int aliasIndex))
                        {
                            if (logWarnings)
                            {
                                Debug.LogWarning(
                                    $"{nameof(ShapeExpressionControllerMB)}: expression[{entryIndex}] step[{stepIndex}] alias '{point.alias}' was not found in {nameof(ShapeBlendMappingMB)} and was skipped.",
                                    this);
                            }

                            continue;
                        }

                        points.Add(new CompiledPoint
                        {
                            AliasIndex = aliasIndex,
                            Weight = Mathf.Clamp(point.weight, 0f, 100f),
                        });
                    }
                }

                compiled[stepIndex] = new CompiledStep
                {
                    Duration = Mathf.Max(0f, sourceStep.duration),
                    Ease = sourceStep.ease,
                    Points = points.ToArray(),
                };
            }

            return compiled;
        }

        private void EnsureRuntimeBinding()
        {
            if (expressionSubscription != null)
                return;

            if (!ResolveRuntimeBinding())
                return;

            expressionHandle = valueStore.GetHandle(entityRef, ValueKeys.Runtime.ShapeExpression);
            expressionSubscription = expressionHandle.Subscribe(SetExpression);
            SetExpression(expressionHandle.CurrentValue);
        }

        private bool ResolveRuntimeBinding()
        {
            if (valueStore != null && entityRef.IsValid)
                return true;

            SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

            if (kernelMB == null || kernelMB.Kernel == null || kernelMB.Kernel.EntityValueStore == null)
            {
                Debug.LogError($"{nameof(ShapeExpressionControllerMB)}: ValueStore is not found.", this);
                enabled = false;
                return false;
            }

            valueStore = kernelMB.Kernel.EntityValueStore;

            EntityMB entityMB = GetComponentInParent<EntityMB>();

            if (entityMB == null || !entityMB.HasEntity)
            {
                Debug.LogError($"{nameof(ShapeExpressionControllerMB)}: EntityMB is not found or not bound.", this);
                enabled = false;
                return false;
            }

            entityRef = entityMB.Entity;
            return true;
        }

        private void TickSegment(float deltaTime)
        {
            if (segmentDuration <= 0f)
            {
                CopyArray(segmentTo, sampledWeights);
                segmentActive = false;
                enteringExpression = false;
                return;
            }

            segmentElapsed = Mathf.Min(segmentElapsed + Mathf.Max(deltaTime, 0f), segmentDuration);
            float t = Mathf.Clamp01(segmentElapsed / segmentDuration);

            float eased = segmentEase != null ? Mathf.Clamp01(segmentEase.Evaluate(t)) : t;
            for (int i = 0; i < sampledWeights.Length; i++)
                sampledWeights[i] = Mathf.LerpUnclamped(segmentFrom[i], segmentTo[i], eased);

            if (segmentElapsed < segmentDuration)
                return;

            CopyArray(segmentTo, sampledWeights);
            segmentActive = false;

            if (enteringExpression)
                enteringExpression = false;
        }

        private void TryQueueNextSegment()
        {
            if (activeEntryIndex < 0)
                return;

            CompiledEntry entry = compiledEntries[activeEntryIndex];

            if (!TryGetNextStepIndex(entry, ref activeStepIndex, ref activeStepDirection, out int nextStepIndex))
                return;

            CopyArray(sampledWeights, segmentFrom);
            BuildStepTarget(entry, nextStepIndex, sampledWeights, segmentTo);

            // Duration/ease belong to the step we are leaving,
            // which makes authoring point-to-point transitions explicit.
            CompiledStep fromStep = entry.Steps[activeStepIndex];
            segmentDuration = fromStep.Duration;
            segmentElapsed = 0f;
            segmentEase = fromStep.Ease;
            segmentActive = segmentDuration > 0f;

            activeStepIndex = nextStepIndex;

            if (!segmentActive)
                CopyArray(segmentTo, sampledWeights);
        }

        private void BuildStepTarget(CompiledEntry entry, int stepIndex, float[] currentWeights, float[] output)
        {
            if (entry.ResetUnspecifiedToDefaultWeights)
                CopyArray(defaultWeights, output);
            else
                CopyArray(currentWeights, output);

            CompiledPoint[] points = entry.Steps[stepIndex].Points;
            for (int i = 0; i < points.Length; i++)
            {
                CompiledPoint point = points[i];
                output[point.AliasIndex] = point.Weight;
            }
        }

        private void ApplyCurrentSample()
        {
            int frameCount = Time.frameCount;

            // Controller writes use a frame token so direct external writes can be
            // suppressed only for aliases touched in the current frame.
            for (int i = 0; i < sampledWeights.Length; i++)
                blendMapping.TrySetWeightFromController(i, sampledWeights[i], frameCount);
        }

        private bool TryFindCompiledEntry(ShapeExpressionId expressionId, out int entryIndex)
        {
            for (int i = 0; i < compiledEntries.Length; i++)
            {
                if (compiledEntries[i].ExpressionId == expressionId)
                {
                    entryIndex = i;
                    return true;
                }
            }

            entryIndex = -1;
            return false;
        }

        private static bool TryGetNextStepIndex(
            in CompiledEntry entry,
            ref int currentStepIndex,
            ref int direction,
            out int nextStepIndex)
        {
            int stepCount = entry.Steps.Length;
            nextStepIndex = currentStepIndex;

            if (stepCount <= 1)
                return false;

            switch (entry.PlayMode)
            {
                case ShapeExpressionPlayMode.Once:
                    if (currentStepIndex >= stepCount - 1)
                        return false;

                    nextStepIndex = currentStepIndex + 1;
                    return true;

                case ShapeExpressionPlayMode.Loop:
                    nextStepIndex = (currentStepIndex + 1) % stepCount;
                    return true;

                case ShapeExpressionPlayMode.PingPong:
                    nextStepIndex = currentStepIndex + direction;

                    if (nextStepIndex >= stepCount)
                    {
                        direction = -1;
                        nextStepIndex = stepCount - 2;
                    }
                    else if (nextStepIndex < 0)
                    {
                        direction = 1;
                        nextStepIndex = 1;
                    }

                    return true;

                default:
                    return false;
            }
        }

        private static void CopyArray(float[] source, float[] destination)
        {
            int count = Mathf.Min(source.Length, destination.Length);
            Array.Copy(source, destination, count);
        }

        private void OnDisable()
        {
            expressionSubscription?.Dispose();
            expressionSubscription = null;
            expressionHandle = null;
        }

        private void OnDestroy()
        {
            expressionSubscription?.Dispose();
        }
    }
}

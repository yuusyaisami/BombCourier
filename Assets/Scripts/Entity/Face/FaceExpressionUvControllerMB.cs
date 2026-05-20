using BC.Base;
using UnityEngine;

namespace BC.Character
{
    [DisallowMultipleComponent]
    public sealed class FaceExpressionUvControllerMB : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private MeshUvRectRemapperMB remapper;
        [SerializeField] private FaceExpressionUvSet uvSet;

        [Header("Runtime Binding")]
        [SerializeField] private bool bindToRuntimeFaceExpression = true;

        [Header("Blink")]
        [SerializeField] private bool enableBlink = true;
        [SerializeField, Min(0.0f)] private float minBlinkInterval = 2.5f;
        [SerializeField, Min(0.0f)] private float maxBlinkInterval = 5.0f;
        [SerializeField, Min(0.01f)] private float blinkDuration = 0.08f;

        private ValueStoreService valueStore;
        private EntityRef entityRef;
        private ValueWatchHandle<FaceExpressionId> expressionHandle;
        private EventSubscription expressionSubscription;
        private FaceExpressionId baseExpression = FaceExpressionId.Neutral;
        private FaceExpressionId displayedExpression = FaceExpressionId.Neutral;
        private float nextBlinkTime = float.PositiveInfinity;
        private float blinkEndTime = float.PositiveInfinity;
        private bool isBlinkActive;
        private bool started;

        public FaceExpressionId CurrentBaseExpression => baseExpression;
        public FaceExpressionId CurrentDisplayedExpression => displayedExpression;

        private void Reset()
        {
            ResolveSerializedReferences();
        }

        private void OnValidate()
        {
            ResolveSerializedReferences();
            NormalizeBlinkSettings();
        }

        private void Start()
        {
            started = true;
            ResolveSerializedReferences();
            NormalizeBlinkSettings();

            if (!ValidateDependencies(logErrors: true))
                return;

            if (bindToRuntimeFaceExpression)
            {
                EnsureRuntimeBinding();
            }
            else
            {
                SetExpression(baseExpression);
            }
        }

        private void OnEnable()
        {
            if (!started)
                return;

            ResolveSerializedReferences();
            NormalizeBlinkSettings();

            if (!ValidateDependencies(logErrors: false))
                return;

            if (bindToRuntimeFaceExpression)
            {
                EnsureRuntimeBinding();
            }
            else
            {
                SetExpression(baseExpression);
            }
        }

        private void Update()
        {
            if (!enableBlink)
                return;

            if (!uvSet.TryGetBlinkExpression(baseExpression, out FaceExpressionId blinkExpression))
                return;

            float now = Time.time;

            if (isBlinkActive)
            {
                if (now < blinkEndTime)
                    return;

                isBlinkActive = false;
                ApplyExpression(baseExpression);
                ScheduleNextBlink(now);
                return;
            }

            if (now < nextBlinkTime)
                return;

            isBlinkActive = true;
            blinkEndTime = now + blinkDuration;
            ApplyExpression(blinkExpression);
        }

        public void SetExpression(FaceExpressionId expression)
        {
            baseExpression = expression;
            isBlinkActive = false;
            blinkEndTime = float.PositiveInfinity;
            ApplyExpression(expression);
            ScheduleNextBlink(Time.time);
        }

        private void ResolveSerializedReferences()
        {
            if (remapper == null)
                remapper = GetComponent<MeshUvRectRemapperMB>();
        }

        private void NormalizeBlinkSettings()
        {
            if (maxBlinkInterval < minBlinkInterval)
                maxBlinkInterval = minBlinkInterval;
        }

        private bool ValidateDependencies(bool logErrors)
        {
            if (remapper == null)
            {
                if (logErrors)
                    Debug.LogError($"{nameof(FaceExpressionUvControllerMB)}: {nameof(MeshUvRectRemapperMB)} is missing.", this);

                enabled = false;
                return false;
            }

            if (uvSet == null)
            {
                if (logErrors)
                    Debug.LogError($"{nameof(FaceExpressionUvControllerMB)}: {nameof(FaceExpressionUvSet)} is not assigned.", this);

                enabled = false;
                return false;
            }

            return true;
        }

        private void EnsureRuntimeBinding()
        {
            if (expressionSubscription != null)
                return;

            if (!ResolveRuntimeBinding())
                return;

            expressionHandle = valueStore.GetHandle(entityRef, ValueKeys.Runtime.FaceExpression);
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
                Debug.LogError($"{nameof(FaceExpressionUvControllerMB)}: ValueStore is not found.", this);
                enabled = false;
                return false;
            }

            valueStore = kernelMB.Kernel.EntityValueStore;

            EntityMB entityMB = GetComponentInParent<EntityMB>();

            if (entityMB == null || !entityMB.HasEntity)
            {
                Debug.LogError($"{nameof(FaceExpressionUvControllerMB)}: EntityMB is not found or not bound.", this);
                enabled = false;
                return false;
            }

            entityRef = entityMB.Entity;
            return true;
        }

        private void ApplyExpression(FaceExpressionId expression)
        {
            if (!TryResolveUvRect(expression, out Rect targetUvRect))
                return;

            displayedExpression = expression;

            if (!remapper.TryApplyUvRect(targetUvRect))
                enabled = false;
        }

        private bool TryResolveUvRect(FaceExpressionId expression, out Rect targetUvRect)
        {
            if (uvSet.TryGetExpressionUvRect(expression, out targetUvRect))
                return true;

            if (uvSet.TryGetExpressionUvRect(FaceExpressionId.Neutral, out targetUvRect))
                return true;

            Debug.LogError($"{nameof(FaceExpressionUvControllerMB)}: Neutral UV rect is not registered.", this);
            enabled = false;
            return false;
        }

        private void ScheduleNextBlink(float now)
        {
            if (!enableBlink || !uvSet.TryGetBlinkExpression(baseExpression, out _))
            {
                nextBlinkTime = float.PositiveInfinity;
                return;
            }

            nextBlinkTime = now + Random.Range(minBlinkInterval, maxBlinkInterval);
        }

        private void OnDisable()
        {
            expressionSubscription?.Dispose();
            expressionSubscription = null;
            expressionHandle = null;
            isBlinkActive = false;
            blinkEndTime = float.PositiveInfinity;
            nextBlinkTime = float.PositiveInfinity;
        }

        private void OnDestroy()
        {
            expressionSubscription?.Dispose();
        }
    }
}
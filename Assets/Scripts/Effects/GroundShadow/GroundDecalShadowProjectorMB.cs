using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BC.Effects.GroundShadow
{
    public enum GroundShadowCastMode
    {
        Raycast = 0,
        SphereCast = 1
    }

    public enum GroundShadowUpdatePhase
    {
        Update = 0,
        FixedUpdate = 1,
        LateUpdate = 2,
        Manual = 3
    }

    /// <summary>
    /// Projects a stylized gameplay shadow straight down from a target onto the nearest ground surface.
    /// This is not a physically correct light shadow. It is a landing/contact indicator for Player, bombs,
    /// and other gameplay-critical entities.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DecalProjector))]
    [AddComponentMenu("BC/Effects/Ground Shadow/Ground Decal Shadow Projector")]
    public sealed class GroundDecalShadowProjectorMB : MonoBehaviour
    {
        private const float MinimumPositiveValue = 0.0001f;
        private const int MaxProbeHitCount = 16;

        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private DecalProjector decalProjector;
        [SerializeField] private Transform orientationReference;

        [Header("Update")]
        [SerializeField] private GroundShadowUpdatePhase updatePhase = GroundShadowUpdatePhase.LateUpdate;
        [SerializeField] private bool useUnscaledTime;

        [Header("Ground Probe")]
        [SerializeField] private GroundShadowCastMode castMode = GroundShadowCastMode.SphereCast;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField] private bool ignoreTargetHierarchy = true;
        [SerializeField, Min(0.0f)] private float castOriginUpOffset = 0.5f;
        [SerializeField, Min(0.01f)] private float maxGroundDistance = 20.0f;
        [SerializeField, Min(0.0f)] private float sphereCastRadius = 0.18f;
        [SerializeField, Range(0.0f, 89.0f)] private float maxReceivableSurfaceAngle = 68.0f;
        [SerializeField, Min(0.0f)] private float surfaceOffset = 0.025f;
        [SerializeField, Min(0.0f)] private float lostGroundGraceTime = 0.08f;

        [Header("Shape")]
        [SerializeField, Min(0.01f)] private float baseWidth = 0.9f;
        [SerializeField, Min(0.01f)] private float baseHeight = 0.58f;
        [SerializeField, Min(0.01f)] private float maxWidthMultiplier = 1.45f;
        [SerializeField, Min(0.01f)] private float maxHeightMultiplier = 1.25f;
        [SerializeField, Min(0.01f)] private float projectionDepth = 0.85f;
        [SerializeField, Min(0.01f)] private float fadeHeight = 6.0f;
        [SerializeField, Range(0.0f, 1.0f)] private float minFadeFactor = 0.22f;
        [SerializeField, Range(0.0f, 1.0f)] private float maxFadeFactor = 0.68f;
        [SerializeField] private AnimationCurve heightToSize = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
        [SerializeField] private AnimationCurve heightToFade = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.0f);

        [Header("Smoothing")]
        [SerializeField, Min(0.0f)] private float positionSharpness = 0.0f;
        [SerializeField, Min(0.0f)] private float rotationSharpness = 0.0f;
        [SerializeField, Min(0.0f)] private float sizeSharpness = 18.0f;
        [SerializeField, Min(0.0f)] private float fadeSharpness = 18.0f;

        [Header("Projector")]
        [SerializeField] private bool forceScaleInvariant = true;
        [SerializeField] private bool applyAngleFade = true;
        [SerializeField, Range(0.0f, 180.0f)] private float startAngleFade = 70.0f;
        [SerializeField, Range(0.0f, 180.0f)] private float endAngleFade = 88.0f;
        [SerializeField, Min(0.0f)] private float drawDistance = 40.0f;
        [SerializeField, Range(0.0f, 1.0f)] private float cameraFadeScale = 0.85f;
        [SerializeField] private bool hideWhenMaterialMissing = true;

        private readonly RaycastHit[] probeHits = new RaycastHit[MaxProbeHitCount];
        private RaycastHit lastGroundHit;
        private bool hasLastGroundHit;
        private float missingGroundTimer;
        private float externalFadeMultiplier = 1.0f;
        private float externalSizeMultiplier = 1.0f;
        private Vector3 currentSize;
        private float currentFadeFactor;
        private bool isProjectorVisible;

        public Transform Target => target;
        public DecalProjector Projector => decalProjector;
        public bool HasGround => hasLastGroundHit;
        public bool IsProjectorVisible => isProjectorVisible;
        public RaycastHit LastGroundHit => lastGroundHit;

        private void Awake()
        {
            ResolveReferences();
            ApplyStaticProjectorSettings();
            HideImmediate();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ApplyStaticProjectorSettings();
            ForceRefresh();
        }

        private void Reset()
        {
            ResolveReferences();
            orientationReference = target;
            groundMask = ~0;
            updatePhase = GroundShadowUpdatePhase.LateUpdate;
            castMode = GroundShadowCastMode.SphereCast;
            ignoreTargetHierarchy = true;
            ApplyStaticProjectorSettings();
        }

        private void OnValidate()
        {
            ResolveReferences();
            NormalizeSerializedValues();
            ApplyStaticProjectorSettings();
        }

        private void Update()
        {
            if (updatePhase == GroundShadowUpdatePhase.Update)
            {
                Tick(GetDeltaTime());
            }
        }

        private void FixedUpdate()
        {
            if (updatePhase == GroundShadowUpdatePhase.FixedUpdate)
            {
                Tick(Time.fixedDeltaTime);
            }
        }

        private void LateUpdate()
        {
            if (updatePhase == GroundShadowUpdatePhase.LateUpdate)
            {
                Tick(GetDeltaTime());
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (orientationReference == null)
            {
                orientationReference = newTarget;
            }

            ForceRefresh();
        }

        public void SetExternalFadeMultiplier(float multiplier)
        {
            externalFadeMultiplier = Mathf.Max(0.0f, multiplier);
        }

        public void SetExternalSizeMultiplier(float multiplier)
        {
            externalSizeMultiplier = Mathf.Max(MinimumPositiveValue, multiplier);
        }

        public void SetManualVisible(bool visible)
        {
            if (decalProjector == null)
            {
                return;
            }

            SetProjectorVisible(visible);
        }

        public void ForceRefresh()
        {
            Tick(0.0f, true);
        }

        public bool TryGetGroundHit(out RaycastHit hit)
        {
            hit = lastGroundHit;
            return hasLastGroundHit;
        }

        private void Tick(float deltaTime, bool immediate = false)
        {
            if (decalProjector == null)
            {
                ResolveReferences();
            }

            if (!CanProject())
            {
                hasLastGroundHit = false;
                missingGroundTimer = 0.0f;
                HideImmediate();
                return;
            }

            bool foundGround = TryProbeGround(out RaycastHit hit);
            if (foundGround)
            {
                lastGroundHit = hit;
                hasLastGroundHit = true;
                missingGroundTimer = 0.0f;
            }
            else
            {
                missingGroundTimer += Mathf.Max(0.0f, deltaTime);
                if (!hasLastGroundHit || missingGroundTimer > lostGroundGraceTime)
                {
                    hasLastGroundHit = false;
                    HideImmediate();
                    return;
                }
            }

            UpdateProjectorFromGround(deltaTime, immediate || !isProjectorVisible);
        }

        private bool CanProject()
        {
            if (target == null || decalProjector == null)
            {
                return false;
            }

            if (!target.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (hideWhenMaterialMissing && decalProjector.material == null)
            {
                return false;
            }

            return true;
        }

        private bool TryProbeGround(out RaycastHit hit)
        {
            Vector3 origin = target.position + Vector3.up * castOriginUpOffset;
            float distance = castOriginUpOffset + maxGroundDistance;
            int hitCount;

            if (castMode == GroundShadowCastMode.SphereCast && sphereCastRadius > MinimumPositiveValue)
            {
                hitCount = Physics.SphereCastNonAlloc(origin, sphereCastRadius, Vector3.down, probeHits, distance, groundMask, triggerInteraction);
            }
            else
            {
                hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, probeHits, distance, groundMask, triggerInteraction);
            }

            hit = default;
            bool hasCandidate = false;
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = probeHits[i];
                if (candidate.collider == null)
                {
                    continue;
                }

                if (IsIgnoredHit(candidate))
                {
                    continue;
                }

                if (!IsReceivableSurface(candidate.normal))
                {
                    continue;
                }

                if (candidate.distance >= bestDistance)
                {
                    continue;
                }

                hit = candidate;
                bestDistance = candidate.distance;
                hasCandidate = true;
            }

            return hasCandidate;
        }

        private bool IsIgnoredHit(RaycastHit hit)
        {
            if (!ignoreTargetHierarchy || target == null || hit.collider == null)
            {
                return false;
            }

            Transform hitTransform = hit.collider.transform;
            return hitTransform == target || hitTransform.IsChildOf(target);
        }

        private bool IsReceivableSurface(Vector3 normal)
        {
            float minUpDot = Mathf.Cos(maxReceivableSurfaceAngle * Mathf.Deg2Rad);
            return Vector3.Dot(normal.normalized, Vector3.up) >= minUpDot;
        }

        private void UpdateProjectorFromGround(float deltaTime, bool immediate)
        {
            float targetToGroundHeight = Mathf.Max(0.0f, target.position.y - lastGroundHit.point.y);
            float height01 = fadeHeight <= MinimumPositiveValue ? 0.0f : Mathf.Clamp01(targetToGroundHeight / fadeHeight);
            float size01 = EvaluateSafe(heightToSize, height01, height01);
            float fade01 = EvaluateSafe(heightToFade, height01, 1.0f - height01);

            float width = Mathf.Lerp(baseWidth, baseWidth * maxWidthMultiplier, size01) * externalSizeMultiplier;
            float height = Mathf.Lerp(baseHeight, baseHeight * maxHeightMultiplier, size01) * externalSizeMultiplier;
            Vector3 targetSize = new Vector3(width, height, Mathf.Max(MinimumPositiveValue, projectionDepth));

            float targetFade = Mathf.Lerp(minFadeFactor, maxFadeFactor, fade01) * externalFadeMultiplier;
            targetFade = Mathf.Clamp01(targetFade);

            Vector3 targetPosition = lastGroundHit.point + lastGroundHit.normal * surfaceOffset;
            Quaternion targetRotation = BuildProjectorRotation(lastGroundHit.normal);

            if (immediate)
            {
                transform.SetPositionAndRotation(targetPosition, targetRotation);
                currentSize = targetSize;
                currentFadeFactor = targetFade;
            }
            else
            {
                transform.position = DampVector3(transform.position, targetPosition, positionSharpness, deltaTime);
                transform.rotation = DampQuaternion(transform.rotation, targetRotation, rotationSharpness, deltaTime);
                currentSize = DampVector3(currentSize, targetSize, sizeSharpness, deltaTime);
                currentFadeFactor = DampFloat(currentFadeFactor, targetFade, fadeSharpness, deltaTime);
            }

            decalProjector.size = currentSize;
            decalProjector.fadeFactor = currentFadeFactor;
            SetProjectorVisible(currentFadeFactor > 0.001f);
        }

        private Quaternion BuildProjectorRotation(Vector3 surfaceNormal)
        {
            Vector3 projectionDirection = -surfaceNormal.normalized;
            Vector3 referenceForward = orientationReference != null ? orientationReference.forward : target.forward;
            Vector3 projectedUp = Vector3.ProjectOnPlane(referenceForward, surfaceNormal);

            if (projectedUp.sqrMagnitude < 0.0001f)
            {
                projectedUp = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal);
            }

            if (projectedUp.sqrMagnitude < 0.0001f)
            {
                projectedUp = Vector3.ProjectOnPlane(Vector3.right, surfaceNormal);
            }

            return Quaternion.LookRotation(projectionDirection, projectedUp.normalized);
        }

        private void SetProjectorVisible(bool visible)
        {
            if (decalProjector == null)
            {
                return;
            }

            isProjectorVisible = visible;
            decalProjector.enabled = visible;
        }

        private void HideImmediate()
        {
            currentFadeFactor = 0.0f;
            if (decalProjector != null)
            {
                decalProjector.fadeFactor = 0.0f;
                SetProjectorVisible(false);
            }
            else
            {
                isProjectorVisible = false;
            }
        }

        private void ResolveReferences()
        {
            if (decalProjector == null)
            {
                decalProjector = GetComponent<DecalProjector>();
            }

            if (target == null && transform.parent != null)
            {
                target = transform.parent;
            }
        }

        private void ApplyStaticProjectorSettings()
        {
            if (decalProjector == null)
            {
                return;
            }

            if (forceScaleInvariant)
            {
                decalProjector.scaleMode = DecalScaleMode.ScaleInvariant;
            }

            decalProjector.drawDistance = Mathf.Max(0.0f, drawDistance);
            decalProjector.fadeScale = Mathf.Clamp01(cameraFadeScale);

            if (applyAngleFade)
            {
                decalProjector.startAngleFade = Mathf.Min(startAngleFade, endAngleFade);
                decalProjector.endAngleFade = Mathf.Max(startAngleFade, endAngleFade);
            }
            else
            {
                decalProjector.startAngleFade = 180.0f;
                decalProjector.endAngleFade = 180.0f;
            }
        }

        private void NormalizeSerializedValues()
        {
            maxGroundDistance = Mathf.Max(0.01f, maxGroundDistance);
            projectionDepth = Mathf.Max(0.01f, projectionDepth);
            fadeHeight = Mathf.Max(0.01f, fadeHeight);
            baseWidth = Mathf.Max(0.01f, baseWidth);
            baseHeight = Mathf.Max(0.01f, baseHeight);
            maxWidthMultiplier = Mathf.Max(0.01f, maxWidthMultiplier);
            maxHeightMultiplier = Mathf.Max(0.01f, maxHeightMultiplier);
            sphereCastRadius = Mathf.Max(0.0f, sphereCastRadius);

            if (minFadeFactor > maxFadeFactor)
            {
                (minFadeFactor, maxFadeFactor) = (maxFadeFactor, minFadeFactor);
            }

            if (heightToSize == null || heightToSize.length == 0)
            {
                heightToSize = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
            }

            if (heightToFade == null || heightToFade.length == 0)
            {
                heightToFade = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.0f);
            }
        }

        private float GetDeltaTime()
        {
            return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private static float EvaluateSafe(AnimationCurve curve, float time, float fallback)
        {
            if (curve == null || curve.length == 0)
            {
                return fallback;
            }

            return Mathf.Clamp01(curve.Evaluate(time));
        }

        private static float DampFloat(float current, float target, float sharpness, float deltaTime)
        {
            if (sharpness <= 0.0f || deltaTime <= 0.0f)
            {
                return target;
            }

            float t = 1.0f - Mathf.Exp(-sharpness * deltaTime);
            return Mathf.Lerp(current, target, t);
        }

        private static Vector3 DampVector3(Vector3 current, Vector3 target, float sharpness, float deltaTime)
        {
            if (sharpness <= 0.0f || deltaTime <= 0.0f)
            {
                return target;
            }

            float t = 1.0f - Mathf.Exp(-sharpness * deltaTime);
            return Vector3.Lerp(current, target, t);
        }

        private static Quaternion DampQuaternion(Quaternion current, Quaternion target, float sharpness, float deltaTime)
        {
            if (sharpness <= 0.0f || deltaTime <= 0.0f)
            {
                return target;
            }

            float t = 1.0f - Mathf.Exp(-sharpness * deltaTime);
            return Quaternion.Slerp(current, target, t);
        }
    }
}

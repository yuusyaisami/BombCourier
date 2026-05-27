using System.Collections.Generic;
using BC.Base;
using BC.Manager;
using BC.Player;
using BC.Utility;
using System;
using UnityEngine;

namespace BC.Gimmick.FilterObject
{
    public enum FilterConditionSourceMode
    {
        HeldItemTag = 0,
        Literal = 1,
    }

    public enum FilterSurfaceApproachMode
    {
        BothSides = 0,
        ForwardSideOnly = 1,
        BackwardSideOnly = 2,
    }

    [DisallowMultipleComponent]
    public sealed class FilterObjectMB : MonoBehaviour
    {
        [System.Serializable]
        private struct FilterFaceDefinition
        {
            [Tooltip("この面の向き判定に使う Transform です。未指定なら sensor collider の Transform を使います。")]
            [SerializeField] private Transform referenceTransform;
            [Tooltip("この面の侵入検出に使う collider です。trigger を推奨します。")]
            [SerializeField] private Collider sensorCollider;
            [Tooltip("通過を許可した時に一時的に IgnoreCollision する blocker 群です。")]
            [SerializeField] private Collider[] blockingColliders;

            [Header("Target")]
            [Tooltip("タグ判定を無視して、すべての Entity を対象にするかを指定します。")]
            [SerializeField] private bool acceptAnyTag;
            [Tooltip("この面が通過判定の対象にする EntityTag 一覧です。")]
            [SerializeField, EntityTagDropdown] private EntityTagReference[] targetTags;
            [Tooltip("この FilterObject 自身の階層に属する collider を無視するかを指定します。")]
            [SerializeField] private bool ignoreSelfEntity;

            [Header("Direction")]
            [Tooltip("local forward を基準に、どちら側から近づいた時だけ通過判定を行うかを指定します。")]
            [SerializeField] private FilterSurfaceApproachMode approachMode;

            [Header("Pass State")]
            [Tooltip("Filter 条件が true の時にこの面を通れるかを指定します。")]
            [SerializeField] private bool passWhenConditionTrue;
            [Tooltip("Filter 条件が false の時にこの面を通れるかを指定します。")]
            [SerializeField] private bool passWhenConditionFalse;

            public Transform ReferenceTransform => referenceTransform != null
                ? referenceTransform
                : sensorCollider != null ? sensorCollider.transform : null;

            public Collider SensorCollider => sensorCollider;
            public Collider[] BlockingColliders => blockingColliders;
            public bool AcceptAnyTag => acceptAnyTag;
            public EntityTagReference[] TargetTags => targetTags;
            public bool IgnoreSelfEntity => ignoreSelfEntity;
            public FilterSurfaceApproachMode ApproachMode => approachMode;
            public bool PassWhenConditionTrue => passWhenConditionTrue;
            public bool PassWhenConditionFalse => passWhenConditionFalse;
        }

        private struct TrackedColliderState
        {
            public bool IsEntryFromForwardSide;
            public bool IsPassing;
        }

        private sealed class FilterFaceRuntime
        {
            public readonly Dictionary<Collider, TrackedColliderState> TrackedColliderStates = new();
            public readonly HashSet<Collider> SeenColliders = new();
            public readonly List<Collider> ReleaseBuffer = new();
        }

        [Header("Condition")]
        [Tooltip("現在の条件ソースです。初期実装では held item tag と literal を切り替えます。")]
        [SerializeField] private FilterConditionSourceMode conditionSourceMode = FilterConditionSourceMode.HeldItemTag;
        [Tooltip("条件ソースが未設定または評価不能の時に使う既定値です。")]
        [SerializeField] private bool fallbackConditionState;
        [Tooltip("Literal モードの時にそのまま使う条件値です。")]
        [SerializeField] private bool literalConditionState;

        [Header("Held Item Tag Condition")]
        [Tooltip("所持アイテムを参照する PlayerItemHandleState です。未指定なら GameLogicManager から自動解決します。")]
        [SerializeField] private PlayerItemHandleStateMB playerItemHandleState;
        [Tooltip("タグ判定を無視して、何か1つでも持っていれば条件 true にするかを指定します。")]
        [SerializeField] private bool acceptAnyHeldItemTag;
        [Tooltip("条件を満たす held item の EntityTag 一覧です。")]
        [SerializeField, EntityTagDropdown] private EntityTagReference[] targetHeldItemTags;

        [Header("Faces")]
        [Tooltip("front/back などの各面設定です。1 FilterObject に対して 1 コンポーネントでまとめて管理します。")]
        [SerializeField] private FilterFaceDefinition[] faces;
        [SerializeField, Min(8)] private int maxOverlapColliders = 32;

        [Header("Visual")]
        [Tooltip("条件 true の時に半透明化する対象です。未指定なら子階層から自動検索します。")]
        [SerializeField] private MeshMaterialControllerMB[] materialControllers;
        [SerializeField, Range(0.0f, 1.0f)] private float inactiveAlpha = 1.0f;
        [SerializeField, Range(0.0f, 1.0f)] private float activeAlpha = 0.45f;

        [Header("Runtime Debug")]
        [SerializeField] private bool isConditionActive;
        [SerializeField] private bool hasHeldItem;
        [SerializeField] private EntityTagId currentHeldItemTag;

        private readonly Dictionary<FilterIgnoreKey, FilterIgnoreState> ignoredCollisionStates = new();
        private FilterFaceRuntime[] faceRuntimes = System.Array.Empty<FilterFaceRuntime>();
        private Collider[] overlapResults = System.Array.Empty<Collider>();

        public bool IsConditionActive => isConditionActive;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureRuntimeBuffers();
            RefreshConditionState(forceNotify: true);
        }

        private void OnEnable()
        {
            ResolveReferences();
            EnsureRuntimeBuffers();
            RefreshConditionState(forceNotify: true);
        }

        private void OnDisable()
        {
            ClearFaceTrackedColliders();
            ClearIgnoredCollisions();
        }

        private void FixedUpdate()
        {
            RefreshConditionState(forceNotify: false);
            EvaluateFaces();
        }

        private void OnValidate()
        {
            inactiveAlpha = Mathf.Clamp01(inactiveAlpha);
            activeAlpha = Mathf.Clamp01(activeAlpha);
            maxOverlapColliders = Mathf.Max(8, maxOverlapColliders);

            if (!Application.isPlaying)
            {
                ResolveReferences();
                EnsureRuntimeBuffers();
            }
        }

        public bool EvaluatePassState(bool passWhenConditionTrue, bool passWhenConditionFalse)
        {
            return isConditionActive ? passWhenConditionTrue : passWhenConditionFalse;
        }

        internal void SetCollisionIgnored(Collider blockingCollider, Collider otherCollider, bool ignored)
        {
            if (blockingCollider == null || otherCollider == null || blockingCollider == otherCollider)
                return;

            if (ignored)
                AcquireIgnoredCollision(blockingCollider, otherCollider);
            else
                ReleaseIgnoredCollision(blockingCollider, otherCollider);
        }

        private void RefreshConditionState(bool forceNotify)
        {
            bool nextState = fallbackConditionState;

            if (TryEvaluateCondition(out bool evaluatedState))
            {
                nextState = evaluatedState;
            }

            if (!forceNotify && isConditionActive == nextState)
                return;

            isConditionActive = nextState;
            ApplyVisualState(isConditionActive);
        }

        private void ApplyVisualState(bool isActive)
        {
            if (materialControllers == null)
                return;

            float alpha = isActive ? activeAlpha : inactiveAlpha;

            for (int i = 0; i < materialControllers.Length; i++)
            {
                MeshMaterialControllerMB controller = materialControllers[i];

                if (controller == null)
                    continue;

                controller.SetAlpha(alpha);
            }
        }

        private void ResolveReferences()
        {
            if (materialControllers == null || materialControllers.Length == 0)
                materialControllers = GetComponentsInChildren<MeshMaterialControllerMB>(true);
        }

        private bool TryEvaluateCondition(out bool isActive)
        {
            switch (conditionSourceMode)
            {
                case FilterConditionSourceMode.Literal:
                    isActive = literalConditionState;
                    return true;

                case FilterConditionSourceMode.HeldItemTag:
                    return TryEvaluateHeldItemTagCondition(out isActive);

                default:
                    isActive = fallbackConditionState;
                    return false;
            }
        }

        private bool TryEvaluateHeldItemTagCondition(out bool isActive)
        {
            ResolvePlayerItemHandleState();

            hasHeldItem = false;
            currentHeldItemTag = default;

            if (playerItemHandleState == null)
            {
                isActive = false;
                return false;
            }

            hasHeldItem = playerItemHandleState.TryGetHeldItemTag(out currentHeldItemTag);

            if (!hasHeldItem)
            {
                isActive = false;
                return true;
            }

            isActive = MatchesHeldItemTargetTag(currentHeldItemTag);
            return true;
        }

        private bool MatchesHeldItemTargetTag(EntityTagId heldItemTag)
        {
            if (!heldItemTag.IsValid)
                return false;

            if (acceptAnyHeldItemTag)
                return true;

            if (targetHeldItemTags == null || targetHeldItemTags.Length == 0)
                return false;

            for (int i = 0; i < targetHeldItemTags.Length; i++)
            {
                if (targetHeldItemTags[i].Matches(heldItemTag))
                    return true;
            }

            return false;
        }

        private void ResolvePlayerItemHandleState()
        {
            if (playerItemHandleState != null)
                return;

            GameLogicManagerMB gameLogicManager = GameLogicManagerMB.Instance;

            if (gameLogicManager != null && gameLogicManager.PlayerInstance != null)
                playerItemHandleState = gameLogicManager.PlayerInstance.GetComponent<PlayerItemHandleStateMB>();

            if (playerItemHandleState == null)
                playerItemHandleState = FindAnyObjectByType<PlayerItemHandleStateMB>();
        }

        private void EnsureRuntimeBuffers()
        {
            if (faces == null)
                faces = System.Array.Empty<FilterFaceDefinition>();

            if (faceRuntimes.Length != faces.Length)
            {
                FilterFaceRuntime[] nextRuntimes = new FilterFaceRuntime[faces.Length];

                for (int i = 0; i < nextRuntimes.Length; i++)
                    nextRuntimes[i] = i < faceRuntimes.Length ? faceRuntimes[i] ?? new FilterFaceRuntime() : new FilterFaceRuntime();

                faceRuntimes = nextRuntimes;
            }

            if (overlapResults == null || overlapResults.Length != maxOverlapColliders)
                overlapResults = new Collider[maxOverlapColliders];
        }

        private void EvaluateFaces()
        {
            EnsureRuntimeBuffers();

            for (int i = 0; i < faces.Length; i++)
                EvaluateFace(faces[i], faceRuntimes[i]);
        }

        private void EvaluateFace(in FilterFaceDefinition face, FilterFaceRuntime runtime)
        {
            runtime.SeenColliders.Clear();

            Collider sensorCollider = face.SensorCollider;

            if (sensorCollider == null || !sensorCollider.enabled)
            {
                ReleaseStaleColliders(face, runtime);
                return;
            }

            Bounds sensorBounds = sensorCollider.bounds;
            Vector3 extents = new Vector3(
                Mathf.Max(0.01f, sensorBounds.extents.x),
                Mathf.Max(0.01f, sensorBounds.extents.y),
                Mathf.Max(0.01f, sensorBounds.extents.z));

            int hitCount = Physics.OverlapBoxNonAlloc(
                sensorBounds.center,
                extents,
                overlapResults,
                Quaternion.identity,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider otherCollider = overlapResults[i];

                if (otherCollider == null || otherCollider.isTrigger || otherCollider == sensorCollider)
                    continue;

                if (otherCollider.transform.IsChildOf(transform))
                    continue;

                if (!IsSensorOverlapping(sensorCollider, otherCollider))
                    continue;

                runtime.SeenColliders.Add(otherCollider);
                RefreshFaceColliderState(face, runtime, otherCollider);
            }

            ReleaseStaleColliders(face, runtime);
        }

        private void RefreshFaceColliderState(in FilterFaceDefinition face, FilterFaceRuntime runtime, Collider otherCollider)
        {
            TrackedColliderState trackedState = GetOrCreateTrackedColliderState(runtime, face, otherCollider);

            // 一度通過を許可した collider は exit まで維持する。
            // 通過中に条件や中心点の位置が変わっても、途中で引っかからないようにするため。
            if (trackedState.IsPassing)
                return;

            bool canPass = CanColliderPass(face, trackedState, otherCollider);
            ApplyFaceColliderPassState(face, runtime, otherCollider, trackedState, canPass);
        }

        private bool CanColliderPass(in FilterFaceDefinition face, in TrackedColliderState trackedState, Collider otherCollider)
        {
            if (!TryResolveSourceTag(face, otherCollider, out EntityTagId sourceTag, out Transform sourceRoot))
                return false;

            if (face.IgnoreSelfEntity && ShouldIgnoreSelfContact(face.ReferenceTransform, sourceRoot))
                return false;

            if (!MatchesFaceTargetTag(face, sourceTag))
                return false;

            if (!MatchesApproachSide(face.ApproachMode, trackedState.IsEntryFromForwardSide))
                return false;

            return EvaluatePassState(face.PassWhenConditionTrue, face.PassWhenConditionFalse);
        }

        private bool TryResolveSourceTag(in FilterFaceDefinition face, Collider otherCollider, out EntityTagId sourceTag, out Transform sourceRoot)
        {
            sourceTag = default;
            sourceRoot = otherCollider != null ? otherCollider.transform.root : null;

            if (otherCollider == null)
                return false;

            EntityMB entityMB = otherCollider.GetComponentInParent<EntityMB>();

            if (entityMB != null)
            {
                sourceRoot = entityMB.transform;
                sourceTag = entityMB.Tag;
            }

            if (face.AcceptAnyTag)
                return true;

            return sourceTag.IsValid;
        }

        private static bool ShouldIgnoreSelfContact(Transform referenceTransform, Transform sourceRoot)
        {
            if (referenceTransform == null || sourceRoot == null)
                return false;

            return referenceTransform == sourceRoot || referenceTransform.IsChildOf(sourceRoot) || sourceRoot.IsChildOf(referenceTransform);
        }

        private static bool MatchesApproachSide(FilterSurfaceApproachMode approachMode, bool isEntryFromForwardSide)
        {
            return approachMode switch
            {
                FilterSurfaceApproachMode.ForwardSideOnly => isEntryFromForwardSide,
                FilterSurfaceApproachMode.BackwardSideOnly => !isEntryFromForwardSide,
                _ => true,
            };
        }

        private static bool IsSensorOverlapping(Collider sensorCollider, Collider otherCollider)
        {
            return Physics.ComputePenetration(
                sensorCollider,
                sensorCollider.transform.position,
                sensorCollider.transform.rotation,
                otherCollider,
                otherCollider.transform.position,
                otherCollider.transform.rotation,
                out _,
                out _);
        }

        private static bool MatchesFaceTargetTag(in FilterFaceDefinition face, EntityTagId sourceTag)
        {
            if (face.AcceptAnyTag)
                return true;

            EntityTagReference[] targetTags = face.TargetTags;

            if (!sourceTag.IsValid || targetTags == null || targetTags.Length == 0)
                return false;

            for (int i = 0; i < targetTags.Length; i++)
            {
                if (targetTags[i].Matches(sourceTag))
                    return true;
            }

            return false;
        }

        private TrackedColliderState GetOrCreateTrackedColliderState(FilterFaceRuntime runtime, in FilterFaceDefinition face, Collider otherCollider)
        {
            if (runtime.TrackedColliderStates.TryGetValue(otherCollider, out TrackedColliderState trackedState))
                return trackedState;

            trackedState = new TrackedColliderState
            {
                IsEntryFromForwardSide = IsOnForwardSide(face.ReferenceTransform, otherCollider),
                IsPassing = false,
            };

            runtime.TrackedColliderStates.Add(otherCollider, trackedState);
            return trackedState;
        }

        private static bool IsOnForwardSide(Transform referenceTransform, Collider otherCollider)
        {
            if (referenceTransform == null || otherCollider == null)
                return true;

            Vector3 toOther = otherCollider.bounds.center - referenceTransform.position;
            return Vector3.Dot(referenceTransform.forward, toOther) >= 0.0f;
        }

        private void ApplyFaceColliderPassState(
            in FilterFaceDefinition face,
            FilterFaceRuntime runtime,
            Collider otherCollider,
            TrackedColliderState trackedState,
            bool canPass)
        {
            if (trackedState.IsPassing == canPass)
                return;

            if (trackedState.IsPassing)
                SetBlockingCollisionIgnored(face, otherCollider, false);

            trackedState.IsPassing = canPass;
            runtime.TrackedColliderStates[otherCollider] = trackedState;

            if (canPass)
                SetBlockingCollisionIgnored(face, otherCollider, true);
        }

        private void SetBlockingCollisionIgnored(in FilterFaceDefinition face, Collider otherCollider, bool ignored)
        {
            Collider[] blockingColliders = face.BlockingColliders;

            if (blockingColliders == null)
                return;

            for (int i = 0; i < blockingColliders.Length; i++)
            {
                Collider blockingCollider = blockingColliders[i];

                if (blockingCollider == null || blockingCollider == otherCollider)
                    continue;

                SetCollisionIgnored(blockingCollider, otherCollider, ignored);
            }
        }

        private void ReleaseStaleColliders(in FilterFaceDefinition face, FilterFaceRuntime runtime)
        {
            runtime.ReleaseBuffer.Clear();

            foreach (Collider trackedCollider in runtime.TrackedColliderStates.Keys)
            {
                if (trackedCollider != null && runtime.SeenColliders.Contains(trackedCollider))
                    continue;

                runtime.ReleaseBuffer.Add(trackedCollider);
            }

            for (int i = 0; i < runtime.ReleaseBuffer.Count; i++)
                ReleaseTrackedCollider(face, runtime, runtime.ReleaseBuffer[i]);

            runtime.ReleaseBuffer.Clear();
        }

        private void ReleaseTrackedCollider(in FilterFaceDefinition face, FilterFaceRuntime runtime, Collider otherCollider)
        {
            if (otherCollider == null)
            {
                runtime.TrackedColliderStates.Remove(otherCollider);
                return;
            }

            if (!runtime.TrackedColliderStates.TryGetValue(otherCollider, out TrackedColliderState trackedState))
                return;

            if (trackedState.IsPassing)
                SetBlockingCollisionIgnored(face, otherCollider, false);

            runtime.TrackedColliderStates.Remove(otherCollider);
        }

        private void ClearFaceTrackedColliders()
        {
            EnsureRuntimeBuffers();

            for (int i = 0; i < faces.Length; i++)
            {
                ReleaseStaleColliders(faces[i], faceRuntimes[i]);
                faceRuntimes[i].TrackedColliderStates.Clear();
                faceRuntimes[i].SeenColliders.Clear();
                faceRuntimes[i].ReleaseBuffer.Clear();
            }
        }

        private void AcquireIgnoredCollision(Collider blockingCollider, Collider otherCollider)
        {
            FilterIgnoreKey key = new FilterIgnoreKey(blockingCollider, otherCollider);

            if (ignoredCollisionStates.TryGetValue(key, out FilterIgnoreState state))
            {
                state.ReferenceCount++;
                ignoredCollisionStates[key] = state;
                return;
            }

            // 同じ blocker を複数面で共有しても、参照カウントで IgnoreCollision を安全に管理する。
            Physics.IgnoreCollision(blockingCollider, otherCollider, true);
            ignoredCollisionStates.Add(key, new FilterIgnoreState(blockingCollider, otherCollider));
        }

        private void ReleaseIgnoredCollision(Collider blockingCollider, Collider otherCollider)
        {
            FilterIgnoreKey key = new FilterIgnoreKey(blockingCollider, otherCollider);

            if (!ignoredCollisionStates.TryGetValue(key, out FilterIgnoreState state))
                return;

            state.ReferenceCount--;

            if (state.ReferenceCount > 0)
            {
                ignoredCollisionStates[key] = state;
                return;
            }

            if (blockingCollider != null && otherCollider != null)
                Physics.IgnoreCollision(blockingCollider, otherCollider, false);

            ignoredCollisionStates.Remove(key);
        }

        private void ClearIgnoredCollisions()
        {
            foreach (FilterIgnoreState state in ignoredCollisionStates.Values)
            {
                if (state.BlockingCollider == null || state.OtherCollider == null)
                    continue;

                Physics.IgnoreCollision(state.BlockingCollider, state.OtherCollider, false);
            }

            ignoredCollisionStates.Clear();
        }

        private readonly struct FilterIgnoreKey : IEquatable<FilterIgnoreKey>
        {
            private readonly int blockingColliderInstanceId;
            private readonly int otherColliderInstanceId;

            public FilterIgnoreKey(Collider blockingCollider, Collider otherCollider)
            {
#pragma warning disable CS0618
                blockingColliderInstanceId = blockingCollider != null ? blockingCollider.GetInstanceID() : 0;
                otherColliderInstanceId = otherCollider != null ? otherCollider.GetInstanceID() : 0;
#pragma warning restore CS0618
            }

            public bool Equals(FilterIgnoreKey other)
            {
                return blockingColliderInstanceId == other.blockingColliderInstanceId &&
                       otherColliderInstanceId == other.otherColliderInstanceId;
            }

            public override bool Equals(object obj)
            {
                return obj is FilterIgnoreKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (blockingColliderInstanceId * 397) ^ otherColliderInstanceId;
                }
            }
        }

        private struct FilterIgnoreState
        {
            public FilterIgnoreState(Collider blockingCollider, Collider otherCollider)
            {
                BlockingCollider = blockingCollider;
                OtherCollider = otherCollider;
                ReferenceCount = 1;
            }

            public Collider BlockingCollider { get; }
            public Collider OtherCollider { get; }
            public int ReferenceCount { get; set; }
        }
    }
}
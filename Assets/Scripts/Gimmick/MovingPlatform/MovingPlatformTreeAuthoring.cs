using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.Gimmick.MovingPlatform
{
    [Serializable]
    public abstract class MovingPlatformTreeNodeAuthoring
    {
        [SerializeField] private string stableId = string.Empty;
        [SerializeField] private string label = string.Empty;

        public string StableId => NormalizeId(stableId);
        public string Label => string.IsNullOrWhiteSpace(label) ? GetType().Name : label.Trim();

        internal void SetStableId(string value)
        {
            stableId = NormalizeId(value);
        }

        internal void SetLabel(string value)
        {
            label = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        internal static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public sealed class MovingPlatformRailNodeAuthoring : MovingPlatformTreeNodeAuthoring
    {
        [SerializeField] private string parentRailNodeId = string.Empty;
        [SerializeField] private ReactiveVector3 localPosition = default;
        [SerializeField] private bool overrideIncomingTiming;
        [SerializeField] private MovingPlatformTimingControl incomingTimingControl = MovingPlatformTimingControl.Duration;
        [SerializeField, Min(0.01f)] private float incomingDuration = 1.0f;
        [SerializeField, Min(0.01f)] private float incomingSpeed = 1.0f;
        [SerializeField] private MovingPlatformEasingMode incomingEasingMode = MovingPlatformEasingMode.SmoothStep;

        public string ParentRailNodeId => NormalizeId(parentRailNodeId);
        public ReactiveVector3 LocalPosition => localPosition;
        public bool OverrideIncomingTiming => overrideIncomingTiming;
        public MovingPlatformTimingControl IncomingTimingControl => incomingTimingControl;
        public float IncomingDuration => Mathf.Max(0.01f, incomingDuration);
        public float IncomingSpeed => Mathf.Max(0.01f, incomingSpeed);
        public MovingPlatformEasingMode IncomingEasingMode => incomingEasingMode;

        internal void SetParentRailNodeId(string value)
        {
            parentRailNodeId = NormalizeId(value);
        }

        internal void SetLocalPosition(ReactiveVector3 value)
        {
            localPosition = value;
        }

        internal void SetIncomingTiming(
            bool overrideTiming,
            MovingPlatformTimingControl timingControl,
            float duration,
            float speed,
            MovingPlatformEasingMode easingMode)
        {
            overrideIncomingTiming = overrideTiming;
            incomingTimingControl = timingControl;
            incomingDuration = Mathf.Max(0.01f, duration);
            incomingSpeed = Mathf.Max(0.01f, speed);
            incomingEasingMode = easingMode;
        }
    }

    [Serializable]
    public sealed class MovingPlatformSelectorRuleAuthoring
    {
        [SerializeField] private int priority;
        [SerializeField] private bool activeOnStart = true;
        [SerializeField] private bool resetWhenSelected = true;
        [SerializeField] private Color visualizationColor = Color.clear;

        [SerializeField] private bool overrideRuntimePathEmission;
        [SerializeField] private bool useVisualizationColorForRuntimePathEmission = true;
        [SerializeField] private Color runtimePathEmissionColor = Color.white;
        [SerializeField, Min(0.0f)] private float runtimePathActiveEmissionStrength = 2.0f;
        [SerializeField, Min(0.0f)] private float runtimePathInactiveEmissionStrength;
        [SerializeField] private bool syncRuntimePathSimpleBoost = true;
        [SerializeField, Min(0.0f)] private float runtimePathActiveSimpleBoostIntensity = 4.0f;
        [SerializeField, Min(0.0f)] private float runtimePathInactiveSimpleBoostIntensity;
        [SerializeField] private bool dimRuntimePathWhenInactive = true;
        [SerializeField, Range(0.0f, 1.0f)] private float runtimePathInactiveAlphaMultiplier = 0.35f;

        [SerializeField] private bool useReactiveCondition;
        [SerializeField] private ReactiveWatchedBool activeCondition = default;

        [SerializeField] private bool useSignalGate;
        [SerializeField] private KernelSignalReference activateSignal;
        [SerializeField] private KernelSignalReference deactivateSignal;

        [SerializeField] private MovingPlatformPlaybackMode playbackMode = MovingPlatformPlaybackMode.PingPong;
        [SerializeField] private MovingPlatformTimingControl defaultTimingControl = MovingPlatformTimingControl.Duration;
        [SerializeField, Min(0.01f)] private float defaultDuration = 1.0f;
        [SerializeField, Min(0.01f)] private float defaultSpeed = 1.0f;
        [SerializeField] private MovingPlatformEasingMode defaultEasingMode = MovingPlatformEasingMode.SmoothStep;

        public int Priority => priority;
        public bool ActiveOnStart => activeOnStart;
        public bool ResetWhenSelected => resetWhenSelected;
        public Color VisualizationColor => visualizationColor;
        public bool OverrideRuntimePathEmission => overrideRuntimePathEmission;
        public bool UseVisualizationColorForRuntimePathEmission => useVisualizationColorForRuntimePathEmission;
        public Color RuntimePathEmissionColor => runtimePathEmissionColor;
        public float RuntimePathActiveEmissionStrength => Mathf.Max(0.0f, runtimePathActiveEmissionStrength);
        public float RuntimePathInactiveEmissionStrength => Mathf.Max(0.0f, runtimePathInactiveEmissionStrength);
        public bool SyncRuntimePathSimpleBoost => syncRuntimePathSimpleBoost;
        public float RuntimePathActiveSimpleBoostIntensity => Mathf.Max(0.0f, runtimePathActiveSimpleBoostIntensity);
        public float RuntimePathInactiveSimpleBoostIntensity => Mathf.Max(0.0f, runtimePathInactiveSimpleBoostIntensity);
        public bool DimRuntimePathWhenInactive => dimRuntimePathWhenInactive;
        public float RuntimePathInactiveAlphaMultiplier => Mathf.Clamp01(runtimePathInactiveAlphaMultiplier);
        public bool UseReactiveCondition => useReactiveCondition;
        public ReactiveWatchedBool ActiveCondition => activeCondition;
        public bool UseSignalGate => useSignalGate;
        public KernelSignalReference ActivateSignal => activateSignal;
        public KernelSignalReference DeactivateSignal => deactivateSignal;
        public MovingPlatformPlaybackMode PlaybackMode => playbackMode;
        public MovingPlatformTimingControl DefaultTimingControl => defaultTimingControl;
        public float DefaultDuration => Mathf.Max(0.01f, defaultDuration);
        public float DefaultSpeed => Mathf.Max(0.01f, defaultSpeed);
        public MovingPlatformEasingMode DefaultEasingMode => defaultEasingMode;

        internal void CopyFrom(MovingPlatformLayer source)
        {
            if (source == null)
                return;

            priority = source.Priority;
            activeOnStart = source.ActiveOnStart;
            resetWhenSelected = source.ResetWhenSelected;
            visualizationColor = source.VisualizationColor;
            overrideRuntimePathEmission = source.OverrideRuntimePathEmission;
            useVisualizationColorForRuntimePathEmission = source.UseVisualizationColorForRuntimePathEmission;
            runtimePathEmissionColor = source.RuntimePathEmissionColor;
            runtimePathActiveEmissionStrength = source.RuntimePathActiveEmissionStrength;
            runtimePathInactiveEmissionStrength = source.RuntimePathInactiveEmissionStrength;
            syncRuntimePathSimpleBoost = source.SyncRuntimePathSimpleBoost;
            runtimePathActiveSimpleBoostIntensity = source.RuntimePathActiveSimpleBoostIntensity;
            runtimePathInactiveSimpleBoostIntensity = source.RuntimePathInactiveSimpleBoostIntensity;
            dimRuntimePathWhenInactive = source.DimRuntimePathWhenInactive;
            runtimePathInactiveAlphaMultiplier = source.RuntimePathInactiveAlphaMultiplier;
            useReactiveCondition = source.UseReactiveCondition;
            activeCondition = source.ActiveCondition;
            useSignalGate = source.UseSignalGate;
            activateSignal = source.ActivateSignal;
            deactivateSignal = source.DeactivateSignal;
            playbackMode = source.PlaybackMode;
            defaultTimingControl = source.DefaultTimingControl;
            defaultDuration = source.DefaultDuration;
            defaultSpeed = source.DefaultSpeed;
            defaultEasingMode = source.DefaultEasingMode;
        }
    }

    [Serializable]
    public abstract class MovingPlatformControlNodeAuthoring : MovingPlatformTreeNodeAuthoring
    {
    }

    [Serializable]
    public sealed class MovingPlatformMoveNodeAuthoring : MovingPlatformControlNodeAuthoring
    {
        [SerializeField] private string targetRailNodeId = string.Empty;
        [SerializeField] private bool overrideTiming;
        [SerializeField] private MovingPlatformTimingControl timingControl = MovingPlatformTimingControl.Duration;
        [SerializeField, Min(0.01f)] private float duration = 1.0f;
        [SerializeField, Min(0.01f)] private float speed = 1.0f;
        [SerializeField] private MovingPlatformEasingMode easingMode = MovingPlatformEasingMode.SmoothStep;

        public string TargetRailNodeId => NormalizeId(targetRailNodeId);
        public bool OverrideTiming => overrideTiming;
        public MovingPlatformTimingControl TimingControl => timingControl;
        public float Duration => Mathf.Max(0.01f, duration);
        public float Speed => Mathf.Max(0.01f, speed);
        public MovingPlatformEasingMode EasingMode => easingMode;

        internal void SetTargetRailNodeId(string value)
        {
            targetRailNodeId = NormalizeId(value);
        }

        internal void SetTimingOverride(
            bool enabled,
            MovingPlatformTimingControl timingMode,
            float timingDuration,
            float timingSpeed,
            MovingPlatformEasingMode timingEasingMode)
        {
            overrideTiming = enabled;
            timingControl = timingMode;
            duration = Mathf.Max(0.01f, timingDuration);
            speed = Mathf.Max(0.01f, timingSpeed);
            easingMode = timingEasingMode;
        }
    }

    [Serializable]
    public sealed class MovingPlatformWaitNodeAuthoring : MovingPlatformControlNodeAuthoring
    {
        [SerializeField] private MovingPlatformWaitMode waitMode = MovingPlatformWaitMode.Duration;
        [SerializeField, Min(0.01f)] private float duration = 1.0f;
        [SerializeField] private KernelSignalReference signal;

        public MovingPlatformWaitMode WaitMode => waitMode;
        public float Duration => Mathf.Max(0.01f, duration);
        public KernelSignalReference Signal => signal;

        internal void CopyFrom(MovingPlatformWaitSegment source)
        {
            if (source == null)
                return;

            waitMode = source.WaitMode;
            duration = source.Duration;
            signal = source.Signal;
        }
    }

    [Serializable]
    public sealed class MovingPlatformInlineActionNodeAuthoring : MovingPlatformControlNodeAuthoring
    {
        [SerializeField] private WiringAction[] actions = Array.Empty<WiringAction>();

        public IReadOnlyList<WiringAction> Actions => actions ?? Array.Empty<WiringAction>();

        public int Execute(in WiringActionContext context)
        {
            return WiringActionRunner.ExecuteAll(actions, context);
        }

        internal void SetActions(IReadOnlyList<WiringAction> sourceActions)
        {
            if (sourceActions == null || sourceActions.Count == 0)
            {
                actions = Array.Empty<WiringAction>();
                return;
            }

            actions = new WiringAction[sourceActions.Count];
            for (int i = 0; i < sourceActions.Count; i++)
                actions[i] = sourceActions[i];
        }
    }

    [Serializable]
    public sealed class MovingPlatformRotationNodeAuthoring : MovingPlatformControlNodeAuthoring
    {
        [SerializeField, Min(0.01f)] private float duration = 1.0f;
        [SerializeField] private MovingPlatformEasingMode easingMode = MovingPlatformEasingMode.SmoothStep;
        [SerializeField] private Vector3 eulerDelta = new(0.0f, 90.0f, 0.0f);
        [SerializeField] private bool usePivotOffset;
        [SerializeField] private ReactiveVector3 pivotLocalOffset = default;

        public float Duration => Mathf.Max(0.01f, duration);
        public MovingPlatformEasingMode EasingMode => easingMode;
        public Vector3 EulerDelta => eulerDelta;
        public bool UsePivotOffset => usePivotOffset;
        public ReactiveVector3 PivotLocalOffset => pivotLocalOffset;

        internal void CopyFrom(MovingPlatformRotationSegment source)
        {
            if (source == null)
                return;

            duration = source.Duration;
            easingMode = source.EasingMode;
            eulerDelta = source.EulerDelta;
            usePivotOffset = source.UsePivotOffset;
            pivotLocalOffset = source.PivotLocalOffset;
        }
    }

    [Serializable]
    public sealed class MovingPlatformScaleNodeAuthoring : MovingPlatformControlNodeAuthoring
    {
        [SerializeField, Min(0.01f)] private float duration = 1.0f;
        [SerializeField] private MovingPlatformEasingMode easingMode = MovingPlatformEasingMode.SmoothStep;
        [SerializeField] private MovingPlatformScaleMode scaleMode = MovingPlatformScaleMode.Absolute;
        [SerializeField] private Vector3 targetScale = Vector3.one;
        [SerializeField] private bool usePivotOffset;
        [SerializeField] private ReactiveVector3 pivotLocalOffset = default;

        public float Duration => Mathf.Max(0.01f, duration);
        public MovingPlatformEasingMode EasingMode => easingMode;
        public MovingPlatformScaleMode ScaleMode => scaleMode;
        public Vector3 TargetScale => targetScale;
        public bool UsePivotOffset => usePivotOffset;
        public ReactiveVector3 PivotLocalOffset => pivotLocalOffset;

        internal void CopyFrom(MovingPlatformScaleSegment source)
        {
            if (source == null)
                return;

            duration = source.Duration;
            easingMode = source.EasingMode;
            scaleMode = source.ScaleMode;
            targetScale = source.TargetScale;
            usePivotOffset = source.UsePivotOffset;
            pivotLocalOffset = source.PivotLocalOffset;
        }
    }

    [Serializable]
    public sealed class MovingPlatformSelectorNodeAuthoring : MovingPlatformTreeNodeAuthoring
    {
        [SerializeField] private string anchorRailNodeId = string.Empty;
        [SerializeField] private MovingPlatformSelectorRuleAuthoring rule = new();
        [SerializeReference] private List<MovingPlatformControlNodeAuthoring> orderedChildren = new();

        public string AnchorRailNodeId => NormalizeId(anchorRailNodeId);
        public MovingPlatformSelectorRuleAuthoring Rule => rule ??= new MovingPlatformSelectorRuleAuthoring();
        public IReadOnlyList<MovingPlatformControlNodeAuthoring> OrderedChildren => orderedChildren ?? (IReadOnlyList<MovingPlatformControlNodeAuthoring>)Array.Empty<MovingPlatformControlNodeAuthoring>();

        internal List<MovingPlatformControlNodeAuthoring> MutableOrderedChildren
        {
            get
            {
                orderedChildren ??= new List<MovingPlatformControlNodeAuthoring>();
                return orderedChildren;
            }
        }

        internal void SetAnchorRailNodeId(string value)
        {
            anchorRailNodeId = NormalizeId(value);
        }
    }

    [Serializable]
    public sealed class MovingPlatformTreeAuthoring
    {
        [SerializeField] private List<MovingPlatformRailNodeAuthoring> railNodes = new();
        [SerializeField] private string rootRailNodeId = string.Empty;
        [SerializeReference] private List<MovingPlatformSelectorNodeAuthoring> selectors = new();

        public IReadOnlyList<MovingPlatformRailNodeAuthoring> RailNodes => railNodes ?? (IReadOnlyList<MovingPlatformRailNodeAuthoring>)Array.Empty<MovingPlatformRailNodeAuthoring>();
        public string RootRailNodeId => MovingPlatformTreeNodeAuthoring.NormalizeId(rootRailNodeId);
        public IReadOnlyList<MovingPlatformSelectorNodeAuthoring> Selectors => selectors ?? (IReadOnlyList<MovingPlatformSelectorNodeAuthoring>)Array.Empty<MovingPlatformSelectorNodeAuthoring>();

        internal List<MovingPlatformRailNodeAuthoring> MutableRailNodes
        {
            get
            {
                railNodes ??= new List<MovingPlatformRailNodeAuthoring>();
                return railNodes;
            }
        }

        internal List<MovingPlatformSelectorNodeAuthoring> MutableSelectors
        {
            get
            {
                selectors ??= new List<MovingPlatformSelectorNodeAuthoring>();
                return selectors;
            }
        }

        public MovingPlatformRailNodeAuthoring AddRailNode(
            string label = null,
            string parentRailNodeId = null,
            ReactiveVector3 localPosition = default)
        {
            List<MovingPlatformRailNodeAuthoring> rails = MutableRailNodes;
            var usedIds = BuildUsedRailNodeIds();

            var railNode = new MovingPlatformRailNodeAuthoring();
            string stableId = GenerateUniqueId("rail", usedIds, rails.Count + 1);
            railNode.SetStableId(stableId);
            railNode.SetLabel(string.IsNullOrWhiteSpace(label) ? stableId : label);
            railNode.SetParentRailNodeId(MovingPlatformTreeNodeAuthoring.NormalizeId(parentRailNodeId));
            railNode.SetLocalPosition(localPosition);
            rails.Add(railNode);

            if (rails.Count == 1 || string.IsNullOrWhiteSpace(rootRailNodeId))
            {
                rootRailNodeId = stableId;
                railNode.SetParentRailNodeId(string.Empty);
            }

            return railNode;
        }

        public bool RemoveRailNode(string stableId)
        {
            stableId = MovingPlatformTreeNodeAuthoring.NormalizeId(stableId);
            if (string.IsNullOrWhiteSpace(stableId))
                return false;

            List<MovingPlatformRailNodeAuthoring> rails = MutableRailNodes;
            int removeIndex = FindRailNodeIndex(stableId);
            if (removeIndex < 0)
                return false;

            MovingPlatformRailNodeAuthoring removedNode = rails[removeIndex];
            string removedParentId = removedNode.ParentRailNodeId;
            bool wasRoot = string.Equals(rootRailNodeId, stableId, StringComparison.Ordinal);

            string replacementRootId = removedParentId;
            if (wasRoot)
            {
                replacementRootId = FindPreferredRootReplacement(stableId);
                if (string.IsNullOrWhiteSpace(replacementRootId))
                {
                    if (rails.Count <= 1)
                    {
                        rails.RemoveAt(removeIndex);
                        rootRailNodeId = string.Empty;
                        return true;
                    }

                    return false;
                }
            }

            RetargetReferencesForRailRemoval(stableId, replacementRootId);

            List<MovingPlatformRailNodeAuthoring> children = CollectRailChildren(stableId);
            for (int i = 0; i < children.Count; i++)
            {
                MovingPlatformRailNodeAuthoring child = children[i];
                if (child == null)
                    continue;

                if (wasRoot && string.Equals(child.StableId, replacementRootId, StringComparison.Ordinal))
                {
                    child.SetParentRailNodeId(string.Empty);
                    continue;
                }

                child.SetParentRailNodeId(replacementRootId);
            }

            if (wasRoot)
                rootRailNodeId = replacementRootId;

            rails.RemoveAt(removeIndex);

            if (!string.IsNullOrWhiteSpace(rootRailNodeId) && FindRailNodeIndex(rootRailNodeId) < 0)
                rootRailNodeId = FindPreferredRootReplacement(string.Empty);

            return true;
        }

        public bool ReparentRailNode(string stableId, string newParentRailNodeId)
        {
            stableId = MovingPlatformTreeNodeAuthoring.NormalizeId(stableId);
            if (string.IsNullOrWhiteSpace(stableId))
                return false;

            List<MovingPlatformRailNodeAuthoring> rails = MutableRailNodes;
            MovingPlatformRailNodeAuthoring railNode = FindRailNode(stableId);
            if (railNode == null)
                return false;

            string parentId = MovingPlatformTreeNodeAuthoring.NormalizeId(newParentRailNodeId);
            if (string.Equals(stableId, parentId, StringComparison.Ordinal))
                return false;

            if (!string.IsNullOrWhiteSpace(parentId) && FindRailNode(parentId) == null)
                return false;

            if (WouldCreateRailCycle(stableId, parentId))
                return false;

            string previousRootId = rootRailNodeId;
            railNode.SetParentRailNodeId(parentId);
            if (string.IsNullOrWhiteSpace(parentId))
            {
                if (!string.IsNullOrWhiteSpace(previousRootId) && !string.Equals(previousRootId, stableId, StringComparison.Ordinal))
                {
                    MovingPlatformRailNodeAuthoring previousRoot = FindRailNode(previousRootId);
                    if (previousRoot != null)
                        previousRoot.SetParentRailNodeId(stableId);
                }

                rootRailNodeId = stableId;
            }
            else if (string.Equals(previousRootId, stableId, StringComparison.Ordinal))
            {
                string replacementRootId = FindPreferredRootReplacement(stableId);
                if (string.IsNullOrWhiteSpace(replacementRootId))
                {
                    railNode.SetParentRailNodeId(string.Empty);
                    rootRailNodeId = stableId;
                    return true;
                }

                rootRailNodeId = replacementRootId;
            }

            if (!string.IsNullOrWhiteSpace(rootRailNodeId) && FindRailNode(rootRailNodeId) == null)
                rootRailNodeId = stableId;

            return true;
        }

        public MovingPlatformSelectorNodeAuthoring AddSelectorNode(
            string label = null,
            string anchorRailNodeId = null)
        {
            List<MovingPlatformSelectorNodeAuthoring> selectorNodes = MutableSelectors;
            var usedIds = BuildUsedSelectorIds();

            var selector = new MovingPlatformSelectorNodeAuthoring();
            string stableId = GenerateUniqueId("selector", usedIds, selectorNodes.Count + 1);
            selector.SetStableId(stableId);
            selector.SetLabel(string.IsNullOrWhiteSpace(label) ? stableId : label);
            selector.SetAnchorRailNodeId(ResolveAnchorRailNodeId(anchorRailNodeId));
            selectorNodes.Add(selector);
            return selector;
        }

        public bool RemoveSelectorNode(string stableId)
        {
            stableId = MovingPlatformTreeNodeAuthoring.NormalizeId(stableId);
            if (string.IsNullOrWhiteSpace(stableId))
                return false;

            List<MovingPlatformSelectorNodeAuthoring> selectorNodes = MutableSelectors;
            int index = FindSelectorNodeIndex(stableId);
            if (index < 0)
                return false;

            selectorNodes.RemoveAt(index);
            return true;
        }

        public bool SetSelectorAnchorRailNodeId(string selectorStableId, string anchorRailNodeId)
        {
            MovingPlatformSelectorNodeAuthoring selector = FindSelectorNode(selectorStableId);
            if (selector == null)
                return false;

            string resolvedAnchorRailNodeId = ResolveAnchorRailNodeId(anchorRailNodeId);
            if (string.IsNullOrWhiteSpace(resolvedAnchorRailNodeId))
                return false;

            selector.SetAnchorRailNodeId(resolvedAnchorRailNodeId);
            return true;
        }

        public MovingPlatformControlNodeAuthoring AddSelectorStep(string selectorStableId, Type stepType = null, string label = null)
        {
            MovingPlatformSelectorNodeAuthoring selector = FindSelectorNode(selectorStableId);
            if (selector == null)
                return null;

            stepType ??= typeof(MovingPlatformMoveNodeAuthoring);
            if (!typeof(MovingPlatformControlNodeAuthoring).IsAssignableFrom(stepType) || stepType.IsAbstract)
                return null;

            var step = (MovingPlatformControlNodeAuthoring)Activator.CreateInstance(stepType);
            string stableId = GenerateUniqueId(BuildStepIdPrefix(stepType), BuildUsedStepIds(selector), selector.MutableOrderedChildren.Count + 1);
            step.SetStableId(stableId);
            step.SetLabel(string.IsNullOrWhiteSpace(label) ? BuildDefaultStepLabel(stepType) : label);

            if (step is MovingPlatformMoveNodeAuthoring moveNode)
                moveNode.SetTargetRailNodeId(ResolveAnchorRailNodeId(null));

            selector.MutableOrderedChildren.Add(step);
            return step;
        }

        public bool RemoveSelectorStep(string selectorStableId, string stepStableId)
        {
            MovingPlatformSelectorNodeAuthoring selector = FindSelectorNode(selectorStableId);
            if (selector == null)
                return false;

            stepStableId = MovingPlatformTreeNodeAuthoring.NormalizeId(stepStableId);
            if (string.IsNullOrWhiteSpace(stepStableId))
                return false;

            List<MovingPlatformControlNodeAuthoring> steps = selector.MutableOrderedChildren;
            for (int i = 0; i < steps.Count; i++)
            {
                MovingPlatformControlNodeAuthoring step = steps[i];
                if (step == null || !string.Equals(step.StableId, stepStableId, StringComparison.Ordinal))
                    continue;

                steps.RemoveAt(i);
                return true;
            }

            return false;
        }

        public bool MoveSelectorStep(string selectorStableId, string stepStableId, int newIndex)
        {
            MovingPlatformSelectorNodeAuthoring selector = FindSelectorNode(selectorStableId);
            if (selector == null)
                return false;

            List<MovingPlatformControlNodeAuthoring> steps = selector.MutableOrderedChildren;
            if (steps.Count == 0)
                return false;

            stepStableId = MovingPlatformTreeNodeAuthoring.NormalizeId(stepStableId);
            if (string.IsNullOrWhiteSpace(stepStableId))
                return false;

            int currentIndex = -1;
            for (int i = 0; i < steps.Count; i++)
            {
                MovingPlatformControlNodeAuthoring step = steps[i];
                if (step != null && string.Equals(step.StableId, stepStableId, StringComparison.Ordinal))
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
                return false;

            newIndex = Mathf.Clamp(newIndex, 0, steps.Count - 1);
            if (currentIndex == newIndex)
                return true;

            MovingPlatformControlNodeAuthoring movedStep = steps[currentIndex];
            steps.RemoveAt(currentIndex);
            if (newIndex >= steps.Count)
                steps.Add(movedStep);
            else
                steps.Insert(newIndex, movedStep);

            return true;
        }

        public bool MoveSelectorStep(string sourceSelectorStableId, string stepStableId, string targetSelectorStableId, int targetInsertIndex)
        {
            sourceSelectorStableId = MovingPlatformTreeNodeAuthoring.NormalizeId(sourceSelectorStableId);
            targetSelectorStableId = MovingPlatformTreeNodeAuthoring.NormalizeId(targetSelectorStableId);
            if (string.IsNullOrWhiteSpace(sourceSelectorStableId) || string.IsNullOrWhiteSpace(targetSelectorStableId))
                return false;

            if (string.Equals(sourceSelectorStableId, targetSelectorStableId, StringComparison.Ordinal))
                return MoveSelectorStep(sourceSelectorStableId, stepStableId, targetInsertIndex);

            MovingPlatformSelectorNodeAuthoring sourceSelector = FindSelectorNode(sourceSelectorStableId);
            MovingPlatformSelectorNodeAuthoring targetSelector = FindSelectorNode(targetSelectorStableId);
            if (sourceSelector == null || targetSelector == null)
                return false;

            stepStableId = MovingPlatformTreeNodeAuthoring.NormalizeId(stepStableId);
            if (string.IsNullOrWhiteSpace(stepStableId))
                return false;

            List<MovingPlatformControlNodeAuthoring> sourceSteps = sourceSelector.MutableOrderedChildren;
            int sourceIndex = -1;
            for (int i = 0; i < sourceSteps.Count; i++)
            {
                MovingPlatformControlNodeAuthoring step = sourceSteps[i];
                if (step != null && string.Equals(step.StableId, stepStableId, StringComparison.Ordinal))
                {
                    sourceIndex = i;
                    break;
                }
            }

            if (sourceIndex < 0)
                return false;

            MovingPlatformControlNodeAuthoring movedStep = sourceSteps[sourceIndex];
            sourceSteps.RemoveAt(sourceIndex);

            List<MovingPlatformControlNodeAuthoring> targetSteps = targetSelector.MutableOrderedChildren;
            targetInsertIndex = Mathf.Clamp(targetInsertIndex, 0, targetSteps.Count);
            if (targetInsertIndex >= targetSteps.Count)
                targetSteps.Add(movedStep);
            else
                targetSteps.Insert(targetInsertIndex, movedStep);

            return true;
        }

        internal void SetRootRailNodeId(string value)
        {
            rootRailNodeId = MovingPlatformTreeNodeAuthoring.NormalizeId(value);
        }

        private static string BuildDefaultStepLabel(Type stepType)
        {
            if (stepType == typeof(MovingPlatformMoveNodeAuthoring))
                return "Move";

            if (stepType == typeof(MovingPlatformWaitNodeAuthoring))
                return "Wait";

            if (stepType == typeof(MovingPlatformInlineActionNodeAuthoring))
                return "Action";

            if (stepType == typeof(MovingPlatformRotationNodeAuthoring))
                return "Rotate";

            if (stepType == typeof(MovingPlatformScaleNodeAuthoring))
                return "Scale";

            return stepType != null ? stepType.Name : "Step";
        }

        private string ResolveAnchorRailNodeId(string anchorRailNodeId)
        {
            anchorRailNodeId = MovingPlatformTreeNodeAuthoring.NormalizeId(anchorRailNodeId);
            if (!string.IsNullOrWhiteSpace(anchorRailNodeId) && FindRailNodeIndex(anchorRailNodeId) >= 0)
                return anchorRailNodeId;

            if (!string.IsNullOrWhiteSpace(rootRailNodeId) && FindRailNodeIndex(rootRailNodeId) >= 0)
                return rootRailNodeId;

            if (railNodes != null && railNodes.Count > 0)
            {
                for (int i = 0; i < railNodes.Count; i++)
                {
                    MovingPlatformRailNodeAuthoring railNode = railNodes[i];
                    if (railNode == null)
                        continue;

                    string stableId = railNode.StableId;
                    if (!string.IsNullOrWhiteSpace(stableId))
                        return stableId;
                }
            }

            return string.Empty;
        }

        private MovingPlatformRailNodeAuthoring FindRailNode(string stableId)
        {
            int index = FindRailNodeIndex(stableId);
            return index >= 0 ? MutableRailNodes[index] : null;
        }

        private int FindRailNodeIndex(string stableId)
        {
            stableId = MovingPlatformTreeNodeAuthoring.NormalizeId(stableId);
            if (string.IsNullOrWhiteSpace(stableId) || railNodes == null)
                return -1;

            for (int i = 0; i < railNodes.Count; i++)
            {
                MovingPlatformRailNodeAuthoring railNode = railNodes[i];
                if (railNode != null && string.Equals(railNode.StableId, stableId, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private MovingPlatformSelectorNodeAuthoring FindSelectorNode(string stableId)
        {
            int index = FindSelectorNodeIndex(stableId);
            return index >= 0 ? MutableSelectors[index] : null;
        }

        private int FindSelectorNodeIndex(string stableId)
        {
            stableId = MovingPlatformTreeNodeAuthoring.NormalizeId(stableId);
            if (string.IsNullOrWhiteSpace(stableId) || selectors == null)
                return -1;

            for (int i = 0; i < selectors.Count; i++)
            {
                MovingPlatformSelectorNodeAuthoring selector = selectors[i];
                if (selector != null && string.Equals(selector.StableId, stableId, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private List<MovingPlatformRailNodeAuthoring> CollectRailChildren(string parentStableId)
        {
            var children = new List<MovingPlatformRailNodeAuthoring>();
            if (railNodes == null || railNodes.Count == 0)
                return children;

            parentStableId = MovingPlatformTreeNodeAuthoring.NormalizeId(parentStableId);
            for (int i = 0; i < railNodes.Count; i++)
            {
                MovingPlatformRailNodeAuthoring railNode = railNodes[i];
                if (railNode == null)
                    continue;

                if (string.Equals(railNode.ParentRailNodeId, parentStableId, StringComparison.Ordinal))
                    children.Add(railNode);
            }

            return children;
        }

        private string FindPreferredRootReplacement(string removedStableId)
        {
            removedStableId = MovingPlatformTreeNodeAuthoring.NormalizeId(removedStableId);
            if (railNodes == null || railNodes.Count == 0)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(removedStableId))
            {
                List<MovingPlatformRailNodeAuthoring> children = CollectRailChildren(removedStableId);
                for (int i = 0; i < children.Count; i++)
                {
                    MovingPlatformRailNodeAuthoring child = children[i];
                    if (child == null)
                        continue;

                    string childStableId = child.StableId;
                    if (!string.IsNullOrWhiteSpace(childStableId))
                        return childStableId;
                }
            }

            for (int i = 0; i < railNodes.Count; i++)
            {
                MovingPlatformRailNodeAuthoring railNode = railNodes[i];
                if (railNode == null)
                    continue;

                string stableId = railNode.StableId;
                if (string.IsNullOrWhiteSpace(stableId))
                    continue;

                if (string.Equals(stableId, removedStableId, StringComparison.Ordinal))
                    continue;

                return stableId;
            }

            return string.Empty;
        }

        private void RetargetReferencesForRailRemoval(string removedStableId, string replacementStableId)
        {
            removedStableId = MovingPlatformTreeNodeAuthoring.NormalizeId(removedStableId);
            replacementStableId = MovingPlatformTreeNodeAuthoring.NormalizeId(replacementStableId);

            if (selectors == null || selectors.Count == 0)
                return;

            HashSet<string> removedIds = CollectRailSubtreeIds(removedStableId);
            string replacementTarget = string.IsNullOrWhiteSpace(replacementStableId) ? rootRailNodeId : replacementStableId;

            for (int i = 0; i < selectors.Count; i++)
            {
                MovingPlatformSelectorNodeAuthoring selector = selectors[i];
                if (selector == null)
                    continue;

                if (removedIds.Contains(selector.AnchorRailNodeId))
                    selector.SetAnchorRailNodeId(replacementTarget);

                List<MovingPlatformControlNodeAuthoring> steps = selector.MutableOrderedChildren;
                for (int stepIndex = 0; stepIndex < steps.Count; stepIndex++)
                {
                    if (steps[stepIndex] is not MovingPlatformMoveNodeAuthoring moveStep)
                        continue;

                    if (!removedIds.Contains(moveStep.TargetRailNodeId))
                        continue;

                    moveStep.SetTargetRailNodeId(replacementTarget);
                }
            }
        }

        private HashSet<string> CollectRailSubtreeIds(string rootStableId)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(rootStableId) || railNodes == null || railNodes.Count == 0)
                return ids;

            var pending = new Queue<string>();
            pending.Enqueue(rootStableId);
            ids.Add(rootStableId);

            while (pending.Count > 0)
            {
                string currentId = pending.Dequeue();
                List<MovingPlatformRailNodeAuthoring> children = CollectRailChildren(currentId);
                for (int i = 0; i < children.Count; i++)
                {
                    MovingPlatformRailNodeAuthoring child = children[i];
                    if (child == null)
                        continue;

                    string childStableId = child.StableId;
                    if (string.IsNullOrWhiteSpace(childStableId) || !ids.Add(childStableId))
                        continue;

                    pending.Enqueue(childStableId);
                }
            }

            return ids;
        }

        private static string BuildStepIdPrefix(Type stepType)
        {
            if (stepType == typeof(MovingPlatformMoveNodeAuthoring))
                return "step.move";

            if (stepType == typeof(MovingPlatformWaitNodeAuthoring))
                return "step.wait";

            if (stepType == typeof(MovingPlatformInlineActionNodeAuthoring))
                return "step.action";

            if (stepType == typeof(MovingPlatformRotationNodeAuthoring))
                return "step.rotate";

            if (stepType == typeof(MovingPlatformScaleNodeAuthoring))
                return "step.scale";

            return "step";
        }

        private HashSet<string> BuildUsedRailNodeIds()
        {
            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            if (railNodes == null)
                return usedIds;

            for (int i = 0; i < railNodes.Count; i++)
            {
                MovingPlatformRailNodeAuthoring railNode = railNodes[i];
                if (railNode == null)
                    continue;

                string stableId = railNode.StableId;
                if (!string.IsNullOrWhiteSpace(stableId))
                    usedIds.Add(stableId);
            }

            return usedIds;
        }

        private HashSet<string> BuildUsedSelectorIds()
        {
            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            if (selectors == null)
                return usedIds;

            for (int i = 0; i < selectors.Count; i++)
            {
                MovingPlatformSelectorNodeAuthoring selector = selectors[i];
                if (selector == null)
                    continue;

                string stableId = selector.StableId;
                if (!string.IsNullOrWhiteSpace(stableId))
                    usedIds.Add(stableId);
            }

            return usedIds;
        }

        private HashSet<string> BuildUsedStepIds(MovingPlatformSelectorNodeAuthoring selector)
        {
            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            if (selector == null)
                return usedIds;

            IReadOnlyList<MovingPlatformControlNodeAuthoring> steps = selector.OrderedChildren;
            for (int i = 0; i < steps.Count; i++)
            {
                MovingPlatformControlNodeAuthoring step = steps[i];
                if (step == null)
                    continue;

                string stableId = step.StableId;
                if (!string.IsNullOrWhiteSpace(stableId))
                    usedIds.Add(stableId);
            }

            return usedIds;
        }

        private static string GenerateUniqueId(string prefix, HashSet<string> usedIds, int seed)
        {
            prefix = string.IsNullOrWhiteSpace(prefix) ? "node" : prefix.Trim();
            int counter = Mathf.Max(1, seed);
            while (true)
            {
                string candidate = $"{prefix}.{counter}";
                if (usedIds.Add(candidate))
                    return candidate;

                counter++;
            }
        }

        private bool WouldCreateRailCycle(string stableId, string newParentRailNodeId)
        {
            stableId = MovingPlatformTreeNodeAuthoring.NormalizeId(stableId);
            newParentRailNodeId = MovingPlatformTreeNodeAuthoring.NormalizeId(newParentRailNodeId);
            if (string.IsNullOrWhiteSpace(stableId) || string.IsNullOrWhiteSpace(newParentRailNodeId))
                return false;

            string cursor = newParentRailNodeId;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (!string.IsNullOrWhiteSpace(cursor) && visited.Add(cursor))
            {
                if (string.Equals(cursor, stableId, StringComparison.Ordinal))
                    return true;

                MovingPlatformRailNodeAuthoring node = FindRailNode(cursor);
                if (node == null)
                    break;

                cursor = node.ParentRailNodeId;
            }

            return false;
        }

        public bool HasAuthoringData
        {
            get
            {
                return railNodes != null && railNodes.Count > 0 &&
                       selectors != null && selectors.Count > 0;
            }
        }
    }

    public enum MovingPlatformTreeValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    public readonly struct MovingPlatformTreeValidationIssue
    {
        public readonly MovingPlatformTreeValidationSeverity Severity;
        public readonly string Code;
        public readonly string Message;

        public MovingPlatformTreeValidationIssue(MovingPlatformTreeValidationSeverity severity, string code, string message)
        {
            Severity = severity;
            Code = string.IsNullOrWhiteSpace(code) ? "Issue" : code.Trim();
            Message = string.IsNullOrWhiteSpace(message) ? "Issue." : message.Trim();
        }
    }
}

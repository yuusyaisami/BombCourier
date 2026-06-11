using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.Gimmick.MovingPlatform
{
    internal readonly struct MovingPlatformTreeEdgeTiming
    {
        public readonly float Duration;
        public readonly MovingPlatformEasingMode EasingMode;

        public MovingPlatformTreeEdgeTiming(float duration, MovingPlatformEasingMode easingMode)
        {
            Duration = Mathf.Max(0.01f, duration);
            EasingMode = easingMode;
        }
    }

    internal sealed class MovingPlatformSelectorRoute
    {
        internal enum ResolvedStepKind
        {
            Move = 0,
            WaitDuration = 1,
            WaitSignal = 2,
            InlineAction = 3,
            Rotate = 4,
            Scale = 5,
        }

        internal readonly struct ResolvedStep
        {
            private readonly MovingPlatformControlNodeAuthoring sourceNode;

            public readonly int AuthoringStepIndex;
            public readonly ResolvedStepKind Kind;
            public readonly int FromRailIndex;
            public readonly int ToRailIndex;
            public readonly float Duration;
            public readonly MovingPlatformEasingMode EasingMode;
            public readonly SignalId WaitSignalId;
            public readonly bool HasWaitSignal;
            public readonly Vector3 RotationEulerDelta;
            public readonly Vector3 ScaleTarget;
            public readonly MovingPlatformScaleMode ScaleMode;
            public readonly bool UsePivotOffset;
            public readonly ReactiveVector3 PivotLocalOffset;

            public ResolvedStep(
                int authoringStepIndex,
                ResolvedStepKind kind,
                int fromRailIndex,
                int toRailIndex,
                float duration,
                MovingPlatformEasingMode easingMode,
                MovingPlatformControlNodeAuthoring sourceNode,
                SignalId waitSignalId = default,
                bool hasWaitSignal = false,
                Vector3 rotationEulerDelta = default,
                Vector3 scaleTarget = default,
                MovingPlatformScaleMode scaleMode = MovingPlatformScaleMode.Absolute,
                bool usePivotOffset = false,
                ReactiveVector3 pivotLocalOffset = default)
            {
                AuthoringStepIndex = authoringStepIndex;
                Kind = kind;
                FromRailIndex = fromRailIndex;
                ToRailIndex = toRailIndex;
                Duration = Mathf.Max(0.01f, duration);
                EasingMode = easingMode;
                this.sourceNode = sourceNode;
                WaitSignalId = waitSignalId;
                HasWaitSignal = hasWaitSignal;
                RotationEulerDelta = rotationEulerDelta;
                ScaleTarget = scaleTarget;
                ScaleMode = scaleMode;
                UsePivotOffset = usePivotOffset;
                PivotLocalOffset = pivotLocalOffset;
            }

            public void ExecuteEnter(in WiringActionContext context)
            {
                if (sourceNode is MovingPlatformInlineActionNodeAuthoring inlineActionNode)
                    inlineActionNode.Execute(context);
            }

            public void ExecuteExit(in WiringActionContext context)
            {
                _ = context;
            }
        }

        private readonly string stableId;
        private readonly string label;
        private readonly MovingPlatformSelectorRuleAuthoring rule;
        private readonly int anchorRailIndex;
        private readonly ResolvedStep[] resolvedSteps;
        private readonly int[] routeRailIndices;
        private readonly int[] referencedRailIndices;

        public MovingPlatformSelectorRoute(
            string stableId,
            string label,
            MovingPlatformSelectorRuleAuthoring rule,
            int anchorRailIndex,
            ResolvedStep[] resolvedSteps,
            int[] routeRailIndices,
            int[] referencedRailIndices)
        {
            this.stableId = string.IsNullOrWhiteSpace(stableId) ? string.Empty : stableId.Trim();
            this.label = string.IsNullOrWhiteSpace(label) ? "Selector" : label.Trim();
            this.rule = rule ?? new MovingPlatformSelectorRuleAuthoring();
            this.anchorRailIndex = anchorRailIndex;
            this.resolvedSteps = resolvedSteps ?? Array.Empty<ResolvedStep>();
            this.routeRailIndices = routeRailIndices ?? Array.Empty<int>();
            this.referencedRailIndices = referencedRailIndices ?? Array.Empty<int>();
        }

        public string StableId => stableId;
        public string Label => label;
        public MovingPlatformSelectorRuleAuthoring Rule => rule;
        public int AnchorRailIndex => anchorRailIndex;
        public bool IsValid => anchorRailIndex >= 0 && resolvedSteps.Length > 0;
        public int StepCount => resolvedSteps.Length;
        public int RouteRailCount => routeRailIndices.Length;

        public bool ContainsRailIndex(int railIndex)
        {
            for (int i = 0; i < referencedRailIndices.Length; i++)
            {
                if (referencedRailIndices[i] == railIndex)
                    return true;
            }

            return false;
        }

        public int GetRouteRailIndexAt(int index)
        {
            return routeRailIndices[index];
        }

        public bool TryGetStep(int stepIndex, out ResolvedStep step)
        {
            if (stepIndex < 0 || stepIndex >= resolvedSteps.Length)
            {
                step = default;
                return false;
            }

            step = resolvedSteps[stepIndex];
            return true;
        }

        public void ExecuteStepEnter(int stepIndex, in WiringActionContext context)
        {
            if (TryGetStep(stepIndex, out ResolvedStep step))
                step.ExecuteEnter(context);
        }

        public void ExecuteStepExit(int stepIndex, in WiringActionContext context)
        {
            if (TryGetStep(stepIndex, out ResolvedStep step))
                step.ExecuteExit(context);
        }

        public bool TryGetNextStepCursor(int currentCursor, int direction, out int nextCursor, out int nextDirection, out bool completedCycle)
        {
            nextCursor = -1;
            nextDirection = NormalizeDirection(direction);
            completedCycle = false;

            if (!IsValid || StepCount <= 0)
                return false;

            if (currentCursor < 0)
            {
                nextCursor = nextDirection >= 0 ? 0 : StepCount - 1;
                return true;
            }

            switch (rule.PlaybackMode)
            {
                case MovingPlatformPlaybackMode.Loop:
                    nextCursor = currentCursor + nextDirection;
                    if (nextCursor >= StepCount)
                    {
                        nextCursor = 0;
                        completedCycle = true;
                    }
                    else if (nextCursor < 0)
                    {
                        nextCursor = StepCount - 1;
                        completedCycle = true;
                    }

                    return true;

                case MovingPlatformPlaybackMode.PingPong:
                    // 端に到達したら進行方向を反転し、1つ内側のステップへ折り返す。
                    // 末尾(StepCount-1)で +1 が範囲外になったら direction=-1 にして currentCursor-1 へ、
                    // 先頭(0)で -1 が範囲外になったら direction=+1 にして currentCursor+1 へ戻す。
                    // こうして同じ端ステップを2回連続で再生せず、A→B→C→B→A→… と往復させる。
                    nextCursor = currentCursor + nextDirection;
                    if (nextCursor >= StepCount)
                    {
                        nextDirection = -1;
                        nextCursor = currentCursor - 1;
                        completedCycle = true;
                    }
                    else if (nextCursor < 0)
                    {
                        nextDirection = 1;
                        nextCursor = currentCursor + 1;
                        completedCycle = true;
                    }

                    // StepCount==1 のときは折り返し先が範囲外になり、ここで false=「次は無い」を返す。
                    return nextCursor >= 0 && nextCursor < StepCount;

                default:
                    nextCursor = currentCursor + nextDirection;
                    return nextCursor >= 0 && nextCursor < StepCount;
            }
        }

        public bool TryResolveResumeCursor(int arrivalRailIndex, out int currentCursor, out int currentDirection)
        {
            currentDirection = 1;
            currentCursor = -1;

            if (!IsValid)
                return false;

            if (arrivalRailIndex == anchorRailIndex)
                return true;

            for (int i = resolvedSteps.Length - 1; i >= 0; i--)
            {
                if (resolvedSteps[i].ToRailIndex != arrivalRailIndex)
                    continue;

                currentCursor = i;
                return true;
            }

            return false;
        }

        private static int NormalizeDirection(int direction)
        {
            return direction >= 0 ? 1 : -1;
        }
    }

    internal sealed class MovingPlatformTreeRuntime
    {
        internal readonly struct RailNodeRuntime
        {
            public readonly string StableId;
            public readonly string Label;
            public readonly int ParentIndex;
            public readonly int[] ChildIndices;
            public readonly ReactiveVector3 LocalPosition;
            public readonly bool OverrideIncomingTiming;
            public readonly MovingPlatformTimingControl IncomingTimingControl;
            public readonly float IncomingDuration;
            public readonly float IncomingSpeed;
            public readonly MovingPlatformEasingMode IncomingEasingMode;

            public RailNodeRuntime(
                string stableId,
                string label,
                int parentIndex,
                int[] childIndices,
                ReactiveVector3 localPosition,
                bool overrideIncomingTiming,
                MovingPlatformTimingControl incomingTimingControl,
                float incomingDuration,
                float incomingSpeed,
                MovingPlatformEasingMode incomingEasingMode)
            {
                StableId = stableId;
                Label = label;
                ParentIndex = parentIndex;
                ChildIndices = childIndices ?? Array.Empty<int>();
                LocalPosition = localPosition;
                OverrideIncomingTiming = overrideIncomingTiming;
                IncomingTimingControl = incomingTimingControl;
                IncomingDuration = Mathf.Max(0.01f, incomingDuration);
                IncomingSpeed = Mathf.Max(0.01f, incomingSpeed);
                IncomingEasingMode = incomingEasingMode;
            }
        }

        private readonly RailNodeRuntime[] railNodes;
        private readonly Dictionary<string, int> railIndicesById;
        private readonly MovingPlatformSelectorRoute[] selectorRoutes;
        private readonly ReactiveValueResolverService resolver;
        private readonly ReactiveEvalContext evalContext;
        private readonly MovingPlatformTreeValidationIssue[] issues;
        private readonly int rootRailIndex;

        private MovingPlatformTreeRuntime(
            RailNodeRuntime[] railNodes,
            Dictionary<string, int> railIndicesById,
            MovingPlatformSelectorRoute[] selectorRoutes,
            ReactiveValueResolverService resolver,
            ReactiveEvalContext evalContext,
            int rootRailIndex,
            MovingPlatformTreeValidationIssue[] issues)
        {
            this.railNodes = railNodes ?? Array.Empty<RailNodeRuntime>();
            this.railIndicesById = railIndicesById ?? new Dictionary<string, int>(StringComparer.Ordinal);
            this.selectorRoutes = selectorRoutes ?? Array.Empty<MovingPlatformSelectorRoute>();
            this.resolver = resolver;
            this.evalContext = evalContext;
            this.rootRailIndex = rootRailIndex;
            this.issues = issues ?? Array.Empty<MovingPlatformTreeValidationIssue>();
        }

        public bool IsValid
        {
            get
            {
                if (railNodes.Length <= 0 || selectorRoutes.Length <= 0 || rootRailIndex < 0)
                    return false;

                for (int i = 0; i < issues.Length; i++)
                {
                    if (issues[i].Severity == MovingPlatformTreeValidationSeverity.Error)
                        return false;
                }

                return true;
            }
        }

        public int RootRailIndex => rootRailIndex;
        public int RailNodeCount => railNodes.Length;
        public int SelectorCount => selectorRoutes.Length;
        public IReadOnlyList<MovingPlatformTreeValidationIssue> Issues => issues;

        public static MovingPlatformTreeRuntime Build(
            MovingPlatformTreeAuthoring treeAuthoring,
            ReactiveValueResolverService resolver,
            ReactiveEvalContext evalContext)
        {
            var issues = new List<MovingPlatformTreeValidationIssue>();

            if (treeAuthoring == null || treeAuthoring.RailNodes == null || treeAuthoring.RailNodes.Count == 0)
            {
                issues.Add(new MovingPlatformTreeValidationIssue(
                    MovingPlatformTreeValidationSeverity.Error,
                    "Tree.Empty",
                    "MovingPlatform tree has no rail nodes."));
                return new MovingPlatformTreeRuntime(
                    Array.Empty<RailNodeRuntime>(),
                    new Dictionary<string, int>(StringComparer.Ordinal),
                    Array.Empty<MovingPlatformSelectorRoute>(),
                    resolver,
                    evalContext,
                    -1,
                    issues.ToArray());
            }

            var railIndicesById = new Dictionary<string, int>(StringComparer.Ordinal);
            var railAuthoring = new List<MovingPlatformRailNodeAuthoring>(treeAuthoring.RailNodes.Count);
            for (int i = 0; i < treeAuthoring.RailNodes.Count; i++)
            {
                MovingPlatformRailNodeAuthoring node = treeAuthoring.RailNodes[i];
                if (node == null)
                {
                    issues.Add(new MovingPlatformTreeValidationIssue(
                        MovingPlatformTreeValidationSeverity.Error,
                        "Tree.RailNodeNull",
                        $"Rail node[{i}] is null."));
                    continue;
                }

                string stableId = node.StableId;
                if (string.IsNullOrWhiteSpace(stableId))
                {
                    issues.Add(new MovingPlatformTreeValidationIssue(
                        MovingPlatformTreeValidationSeverity.Error,
                        "Tree.RailNodeIdEmpty",
                        $"Rail node[{i}] is missing a stable id."));
                    continue;
                }

                if (railIndicesById.ContainsKey(stableId))
                {
                    issues.Add(new MovingPlatformTreeValidationIssue(
                        MovingPlatformTreeValidationSeverity.Error,
                        "Tree.RailNodeIdDuplicate",
                        $"Rail node id '{stableId}' is duplicated."));
                    continue;
                }

                railIndicesById.Add(stableId, railAuthoring.Count);
                railAuthoring.Add(node);
            }

            if (railAuthoring.Count == 0)
            {
                issues.Add(new MovingPlatformTreeValidationIssue(
                    MovingPlatformTreeValidationSeverity.Error,
                    "Tree.RailNodesUnavailable",
                    "No valid rail nodes were available after validation."));
                return new MovingPlatformTreeRuntime(
                    Array.Empty<RailNodeRuntime>(),
                    railIndicesById,
                    Array.Empty<MovingPlatformSelectorRoute>(),
                    resolver,
                    evalContext,
                    -1,
                    issues.ToArray());
            }

            int[] parentIndices = new int[railAuthoring.Count];
            List<int>[] childBuffers = new List<int>[railAuthoring.Count];
            for (int i = 0; i < railAuthoring.Count; i++)
            {
                parentIndices[i] = -2;
                childBuffers[i] = new List<int>(4);
            }

            int rootIndex = -1;
            string configuredRootId = treeAuthoring.RootRailNodeId;

            for (int i = 0; i < railAuthoring.Count; i++)
            {
                MovingPlatformRailNodeAuthoring node = railAuthoring[i];
                string parentId = node.ParentRailNodeId;
                if (string.IsNullOrWhiteSpace(parentId))
                {
                    if (!string.IsNullOrWhiteSpace(configuredRootId) &&
                        !string.Equals(configuredRootId, node.StableId, StringComparison.Ordinal))
                    {
                        issues.Add(new MovingPlatformTreeValidationIssue(
                            MovingPlatformTreeValidationSeverity.Error,
                            "Tree.RootMismatch",
                            $"Configured root '{configuredRootId}' does not match root rail '{node.StableId}'."));
                    }

                    if (rootIndex >= 0)
                    {
                        issues.Add(new MovingPlatformTreeValidationIssue(
                            MovingPlatformTreeValidationSeverity.Error,
                            "Tree.MultipleRoots",
                            $"Multiple root rail nodes found: '{railAuthoring[rootIndex].StableId}' and '{node.StableId}'."));
                    }

                    rootIndex = i;
                    parentIndices[i] = -1;
                    continue;
                }

                if (!railIndicesById.TryGetValue(parentId, out int parentIndex))
                {
                    issues.Add(new MovingPlatformTreeValidationIssue(
                        MovingPlatformTreeValidationSeverity.Error,
                        "Tree.ParentMissing",
                        $"Rail node '{node.StableId}' references missing parent '{parentId}'."));
                    parentIndices[i] = -1;
                    continue;
                }

                parentIndices[i] = parentIndex;
                childBuffers[parentIndex].Add(i);
            }

            if (!string.IsNullOrWhiteSpace(configuredRootId) &&
                railIndicesById.TryGetValue(configuredRootId, out int configuredRootIndex))
            {
                rootIndex = configuredRootIndex;
            }

            if (rootIndex < 0)
            {
                issues.Add(new MovingPlatformTreeValidationIssue(
                    MovingPlatformTreeValidationSeverity.Error,
                    "Tree.RootMissing",
                    "MovingPlatform tree must have exactly one root rail node."));
            }

            int[] visitState = new int[railAuthoring.Count];
            for (int i = 0; i < railAuthoring.Count; i++)
            {
                if (HasCycle(i))
                {
                    issues.Add(new MovingPlatformTreeValidationIssue(
                        MovingPlatformTreeValidationSeverity.Error,
                        "Tree.Cycle",
                        $"Rail node '{railAuthoring[i].StableId}' participates in a cycle."));
                    break;
                }
            }

            int reachableCount = 0;
            if (rootIndex >= 0)
            {
                var stack = new Stack<int>();
                var visited = new bool[railAuthoring.Count];
                stack.Push(rootIndex);

                while (stack.Count > 0)
                {
                    int current = stack.Pop();
                    if (current < 0 || current >= railAuthoring.Count || visited[current])
                        continue;

                    visited[current] = true;
                    reachableCount++;

                    for (int childIndex = 0; childIndex < childBuffers[current].Count; childIndex++)
                        stack.Push(childBuffers[current][childIndex]);
                }
            }

            if (reachableCount != railAuthoring.Count)
            {
                issues.Add(new MovingPlatformTreeValidationIssue(
                    MovingPlatformTreeValidationSeverity.Error,
                    "Tree.Disconnected",
                    "Rail tree is disconnected. Every rail node must be reachable from the root."));
            }

            var runtimeRailNodes = new RailNodeRuntime[railAuthoring.Count];
            for (int i = 0; i < railAuthoring.Count; i++)
            {
                MovingPlatformRailNodeAuthoring node = railAuthoring[i];
                runtimeRailNodes[i] = new RailNodeRuntime(
                    node.StableId,
                    node.Label,
                    parentIndices[i] < 0 ? -1 : parentIndices[i],
                    childBuffers[i].ToArray(),
                    node.LocalPosition,
                    node.OverrideIncomingTiming,
                    node.IncomingTimingControl,
                    node.IncomingDuration,
                    node.IncomingSpeed,
                    node.IncomingEasingMode);
            }

            var selectorRoutes = new List<MovingPlatformSelectorRoute>();
            var selectorIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<MovingPlatformSelectorNodeAuthoring> selectors = treeAuthoring.Selectors ?? Array.Empty<MovingPlatformSelectorNodeAuthoring>();
            for (int selectorIndex = 0; selectorIndex < selectors.Count; selectorIndex++)
            {
                MovingPlatformSelectorNodeAuthoring selector = selectors[selectorIndex];
                if (selector == null)
                {
                    issues.Add(new MovingPlatformTreeValidationIssue(
                        MovingPlatformTreeValidationSeverity.Error,
                        "Tree.SelectorNull",
                        $"Selector[{selectorIndex}] is null."));
                    continue;
                }

                string selectorId = selector.StableId;
                if (string.IsNullOrWhiteSpace(selectorId))
                {
                    issues.Add(new MovingPlatformTreeValidationIssue(
                        MovingPlatformTreeValidationSeverity.Error,
                        "Tree.SelectorIdEmpty",
                        $"Selector[{selectorIndex}] is missing a stable id."));
                    continue;
                }

                if (!selectorIds.Add(selectorId))
                {
                    issues.Add(new MovingPlatformTreeValidationIssue(
                        MovingPlatformTreeValidationSeverity.Error,
                        "Tree.SelectorIdDuplicate",
                        $"Selector id '{selectorId}' is duplicated."));
                    continue;
                }

                if (!railIndicesById.TryGetValue(selector.AnchorRailNodeId, out int anchorRailIndex))
                {
                    issues.Add(new MovingPlatformTreeValidationIssue(
                        MovingPlatformTreeValidationSeverity.Error,
                        "Tree.SelectorAnchorMissing",
                        $"Selector '{selector.Label}' references missing anchor rail '{selector.AnchorRailNodeId}'."));
                    continue;
                }

                MovingPlatformSelectorRoute route = BuildRoute(selector, anchorRailIndex, runtimeRailNodes, railIndicesById, issues);
                selectorRoutes.Add(route);
            }

            return new MovingPlatformTreeRuntime(
                runtimeRailNodes,
                railIndicesById,
                selectorRoutes.ToArray(),
                resolver,
                evalContext,
                rootIndex,
                issues.ToArray());

            bool HasCycle(int nodeIndex)
            {
                if (nodeIndex < 0 || nodeIndex >= parentIndices.Length)
                    return false;

                if (visitState[nodeIndex] == 1)
                    return true;

                if (visitState[nodeIndex] == 2)
                    return false;

                visitState[nodeIndex] = 1;
                int parentIndex = parentIndices[nodeIndex];
                if (parentIndex >= 0 && HasCycle(parentIndex))
                    return true;

                visitState[nodeIndex] = 2;
                return false;
            }
        }

        public bool TryGetSelectorRoute(int selectorIndex, out MovingPlatformSelectorRoute route)
        {
            if (selectorIndex < 0 || selectorIndex >= selectorRoutes.Length)
            {
                route = null;
                return false;
            }

            route = selectorRoutes[selectorIndex];
            return route != null && route.IsValid;
        }

        public bool TryGetRailNodeIndex(string stableId, out int railIndex)
        {
            return railIndicesById.TryGetValue(MovingPlatformTreeNodeAuthoring.NormalizeId(stableId), out railIndex);
        }

        public string GetRailNodeId(int railIndex)
        {
            return railIndex >= 0 && railIndex < railNodes.Length
                ? railNodes[railIndex].StableId
                : string.Empty;
        }

        public int GetRailNodeParentIndex(int railIndex)
        {
            return railIndex >= 0 && railIndex < railNodes.Length
                ? railNodes[railIndex].ParentIndex
                : -1;
        }

        public int[] GetRailNodeChildren(int railIndex)
        {
            return railIndex >= 0 && railIndex < railNodes.Length
                ? railNodes[railIndex].ChildIndices
                : Array.Empty<int>();
        }

        public Vector3 GetWorldPosition(in MovingPlatformBasePose basePose, int railIndex)
        {
            Vector3 localPosition = ResolveLocalPosition(railIndex);
            Vector3 scaledLocal = Vector3.Scale(localPosition, basePose.LocalScale);
            return basePose.Position + basePose.Rotation * scaledLocal;
        }

        public bool TryResolveLiteralLocalPosition(int railIndex, out Vector3 localPosition)
        {
            localPosition = default;
            if (railIndex < 0 || railIndex >= railNodes.Length)
                return false;

            ReactiveVector3 reactiveLocalPosition = railNodes[railIndex].LocalPosition;
            if (reactiveLocalPosition.SourceKind != ReactiveVector3SourceKind.Literal)
                return false;

            localPosition = reactiveLocalPosition.Literal;
            return true;
        }

        public bool TryFindNearestRailNode(in MovingPlatformBasePose basePose, Vector3 worldPosition, out int railIndex)
        {
            railIndex = -1;
            if (railNodes.Length <= 0)
                return false;

            float bestDistanceSqr = float.MaxValue;
            for (int i = 0; i < railNodes.Length; i++)
            {
                Vector3 nodeWorld = GetWorldPosition(basePose, i);
                float distanceSqr = (worldPosition - nodeWorld).sqrMagnitude;
                if (distanceSqr >= bestDistanceSqr)
                    continue;

                bestDistanceSqr = distanceSqr;
                railIndex = i;
            }

            return railIndex >= 0;
        }

        public bool TryBuildRailPath(int fromRailIndex, int toRailIndex, List<int> pathBuffer)
        {
            if (pathBuffer == null)
                return false;

            pathBuffer.Clear();
            if (fromRailIndex < 0 || fromRailIndex >= railNodes.Length || toRailIndex < 0 || toRailIndex >= railNodes.Length)
                return false;

            if (fromRailIndex == toRailIndex)
            {
                pathBuffer.Add(fromRailIndex);
                return true;
            }

            var fromAncestors = new Dictionary<int, int>();
            int depth = 0;
            for (int cursor = fromRailIndex; cursor >= 0; cursor = railNodes[cursor].ParentIndex)
                fromAncestors[cursor] = depth++;

            int lca = -1;
            for (int cursor = toRailIndex; cursor >= 0; cursor = railNodes[cursor].ParentIndex)
            {
                if (fromAncestors.ContainsKey(cursor))
                {
                    lca = cursor;
                    break;
                }
            }

            if (lca < 0)
                return false;

            int walker = fromRailIndex;
            while (walker != lca)
            {
                pathBuffer.Add(walker);
                walker = railNodes[walker].ParentIndex;
            }

            pathBuffer.Add(lca);

            var down = new List<int>();
            walker = toRailIndex;
            while (walker != lca)
            {
                down.Add(walker);
                walker = railNodes[walker].ParentIndex;
            }

            for (int i = down.Count - 1; i >= 0; i--)
                pathBuffer.Add(down[i]);

            return pathBuffer.Count > 0;
        }

        public MovingPlatformTreeEdgeTiming ResolveEdgeTiming(int fromRailIndex, int toRailIndex, MovingPlatformSelectorRuleAuthoring rule)
        {
            rule ??= new MovingPlatformSelectorRuleAuthoring();
            if (fromRailIndex == toRailIndex)
                return new MovingPlatformTreeEdgeTiming(0.01f, rule.DefaultEasingMode);

            int childIndex;
            if (railNodes[toRailIndex].ParentIndex == fromRailIndex)
            {
                childIndex = toRailIndex;
            }
            else if (railNodes[fromRailIndex].ParentIndex == toRailIndex)
            {
                childIndex = fromRailIndex;
            }
            else
            {
                float distance = Vector3.Distance(ResolveLocalPosition(fromRailIndex), ResolveLocalPosition(toRailIndex));
                float duration = rule.DefaultTimingControl == MovingPlatformTimingControl.Speed
                    ? (distance > 0.0001f ? distance / rule.DefaultSpeed : 0.01f)
                    : rule.DefaultDuration;
                return new MovingPlatformTreeEdgeTiming(duration, rule.DefaultEasingMode);
            }

            RailNodeRuntime child = railNodes[childIndex];
            if (!child.OverrideIncomingTiming)
            {
                float distance = Vector3.Distance(ResolveLocalPosition(fromRailIndex), ResolveLocalPosition(toRailIndex));
                float duration = rule.DefaultTimingControl == MovingPlatformTimingControl.Speed
                    ? (distance > 0.0001f ? distance / rule.DefaultSpeed : 0.01f)
                    : rule.DefaultDuration;
                return new MovingPlatformTreeEdgeTiming(duration, rule.DefaultEasingMode);
            }

            float edgeDistance = Vector3.Distance(ResolveLocalPosition(fromRailIndex), ResolveLocalPosition(toRailIndex));
            float edgeDuration = child.IncomingTimingControl == MovingPlatformTimingControl.Speed
                ? (edgeDistance > 0.0001f ? edgeDistance / child.IncomingSpeed : 0.01f)
                : child.IncomingDuration;
            return new MovingPlatformTreeEdgeTiming(edgeDuration, child.IncomingEasingMode);
        }

        private Vector3 ResolveLocalPosition(int railIndex)
        {
            RailNodeRuntime node = railNodes[railIndex];
            if (node.LocalPosition.SourceKind == ReactiveVector3SourceKind.Literal)
                return node.LocalPosition.Literal;

            if (resolver != null)
            {
                ReactiveResult<Vector3> result = resolver.ResolveVector3(evalContext, node.LocalPosition);
                if (result.Success)
                    return result.Value;
            }

            return node.LocalPosition.FallbackValue;
        }

        private static MovingPlatformSelectorRoute BuildRoute(
            MovingPlatformSelectorNodeAuthoring selector,
            int anchorRailIndex,
            RailNodeRuntime[] railNodes,
            Dictionary<string, int> railIndicesById,
            List<MovingPlatformTreeValidationIssue> issues)
        {
            var resolvedSteps = new List<MovingPlatformSelectorRoute.ResolvedStep>();
            var routeRailIndices = new List<int> { anchorRailIndex };
            var referencedRailIndices = new HashSet<int> { anchorRailIndex };
            var pathBuffer = new List<int>();

            int currentRailIndex = anchorRailIndex;
            IReadOnlyList<MovingPlatformControlNodeAuthoring> steps = selector.OrderedChildren;
            for (int authoringStepIndex = 0; authoringStepIndex < steps.Count; authoringStepIndex++)
            {
                MovingPlatformControlNodeAuthoring step = steps[authoringStepIndex];
                if (step == null)
                {
                    issues.Add(new MovingPlatformTreeValidationIssue(
                        MovingPlatformTreeValidationSeverity.Error,
                        "Tree.SelectorStepNull",
                        $"Selector '{selector.Label}' contains a null step at index {authoringStepIndex}."));
                    return BuildInvalidRoute(selector, anchorRailIndex);
                }

                if (step is MovingPlatformMoveNodeAuthoring moveStep)
                {
                    if (!railIndicesById.TryGetValue(moveStep.TargetRailNodeId, out int targetRailIndex))
                    {
                        issues.Add(new MovingPlatformTreeValidationIssue(
                            MovingPlatformTreeValidationSeverity.Error,
                            "Tree.MoveTargetMissing",
                            $"Selector '{selector.Label}' step '{step.Label}' references missing rail '{moveStep.TargetRailNodeId}'."));
                        return BuildInvalidRoute(selector, anchorRailIndex);
                    }

                    if (!TryBuildRailPath(railNodes, currentRailIndex, targetRailIndex, pathBuffer))
                    {
                        issues.Add(new MovingPlatformTreeValidationIssue(
                            MovingPlatformTreeValidationSeverity.Error,
                            "Tree.MovePathMissing",
                            $"Selector '{selector.Label}' cannot build a rail path from '{railNodes[currentRailIndex].StableId}' to '{moveStep.TargetRailNodeId}'."));
                        return BuildInvalidRoute(selector, anchorRailIndex);
                    }

                    if (pathBuffer.Count <= 1)
                    {
                        resolvedSteps.Add(new MovingPlatformSelectorRoute.ResolvedStep(
                            authoringStepIndex,
                            MovingPlatformSelectorRoute.ResolvedStepKind.Move,
                            currentRailIndex,
                            targetRailIndex,
                            0.01f,
                            moveStep.OverrideTiming ? moveStep.EasingMode : selector.Rule.DefaultEasingMode,
                            moveStep));
                    }
                    else
                    {
                        float totalDistance = 0f;
                        for (int pathIndex = 0; pathIndex < pathBuffer.Count - 1; pathIndex++)
                            totalDistance += Vector3.Distance(ResolveLocalPosition(railNodes, pathBuffer[pathIndex]), ResolveLocalPosition(railNodes, pathBuffer[pathIndex + 1]));

                        float configuredTotalDuration = 0f;
                        if (moveStep.OverrideTiming)
                        {
                            configuredTotalDuration = moveStep.TimingControl == MovingPlatformTimingControl.Speed
                                ? (totalDistance > 0.0001f ? totalDistance / moveStep.Speed : 0.01f)
                                : moveStep.Duration;
                        }

                        for (int pathIndex = 0; pathIndex < pathBuffer.Count - 1; pathIndex++)
                        {
                            int fromRailIndex = pathBuffer[pathIndex];
                            int toRailIndex = pathBuffer[pathIndex + 1];
                            float edgeDistance = Vector3.Distance(ResolveLocalPosition(railNodes, fromRailIndex), ResolveLocalPosition(railNodes, toRailIndex));

                            MovingPlatformTreeEdgeTiming timing;
                            if (moveStep.OverrideTiming)
                            {
                                float normalizedDistance = totalDistance > 0.0001f ? edgeDistance / totalDistance : 1.0f / Mathf.Max(1, pathBuffer.Count - 1);
                                timing = new MovingPlatformTreeEdgeTiming(configuredTotalDuration * normalizedDistance, moveStep.EasingMode);
                            }
                            else
                            {
                                timing = ResolveEdgeTiming(railNodes, fromRailIndex, toRailIndex, selector.Rule);
                            }

                            resolvedSteps.Add(new MovingPlatformSelectorRoute.ResolvedStep(
                                authoringStepIndex,
                                MovingPlatformSelectorRoute.ResolvedStepKind.Move,
                                fromRailIndex,
                                toRailIndex,
                                timing.Duration,
                                timing.EasingMode,
                                moveStep));
                        }
                    }

                    currentRailIndex = targetRailIndex;
                    routeRailIndices.Add(targetRailIndex);
                    referencedRailIndices.Add(targetRailIndex);
                    continue;
                }

                if (step is MovingPlatformWaitNodeAuthoring waitNode)
                {
                    if (waitNode.WaitMode == MovingPlatformWaitMode.Signal)
                    {
                        if (!waitNode.Signal.TryResolve(out Signal signal))
                        {
                            issues.Add(new MovingPlatformTreeValidationIssue(
                                MovingPlatformTreeValidationSeverity.Error,
                                "Tree.WaitSignalMissing",
                                $"Selector '{selector.Label}' wait step '{step.Label}' references an unresolved signal."));
                            return BuildInvalidRoute(selector, anchorRailIndex);
                        }

                        resolvedSteps.Add(new MovingPlatformSelectorRoute.ResolvedStep(
                            authoringStepIndex,
                            MovingPlatformSelectorRoute.ResolvedStepKind.WaitSignal,
                            currentRailIndex,
                            currentRailIndex,
                            0.01f,
                            MovingPlatformEasingMode.Linear,
                            waitNode,
                            signal.Id,
                            true));
                    }
                    else
                    {
                        resolvedSteps.Add(new MovingPlatformSelectorRoute.ResolvedStep(
                            authoringStepIndex,
                            MovingPlatformSelectorRoute.ResolvedStepKind.WaitDuration,
                            currentRailIndex,
                            currentRailIndex,
                            waitNode.Duration,
                            MovingPlatformEasingMode.Linear,
                            waitNode));
                    }

                    continue;
                }

                if (step is MovingPlatformInlineActionNodeAuthoring inlineActionNode)
                {
                    resolvedSteps.Add(new MovingPlatformSelectorRoute.ResolvedStep(
                        authoringStepIndex,
                        MovingPlatformSelectorRoute.ResolvedStepKind.InlineAction,
                        currentRailIndex,
                        currentRailIndex,
                        0.01f,
                        MovingPlatformEasingMode.Linear,
                        inlineActionNode));
                    continue;
                }

                if (step is MovingPlatformRotationNodeAuthoring rotationNode)
                {
                    resolvedSteps.Add(new MovingPlatformSelectorRoute.ResolvedStep(
                        authoringStepIndex,
                        MovingPlatformSelectorRoute.ResolvedStepKind.Rotate,
                        currentRailIndex,
                        currentRailIndex,
                        rotationNode.Duration,
                        rotationNode.EasingMode,
                        rotationNode,
                        default,
                        false,
                        rotationEulerDelta: rotationNode.EulerDelta,
                        usePivotOffset: rotationNode.UsePivotOffset,
                        pivotLocalOffset: rotationNode.PivotLocalOffset));
                    continue;
                }

                if (step is MovingPlatformScaleNodeAuthoring scaleNode)
                {
                    resolvedSteps.Add(new MovingPlatformSelectorRoute.ResolvedStep(
                        authoringStepIndex,
                        MovingPlatformSelectorRoute.ResolvedStepKind.Scale,
                        currentRailIndex,
                        currentRailIndex,
                        scaleNode.Duration,
                        scaleNode.EasingMode,
                        scaleNode,
                        default,
                        false,
                        scaleTarget: scaleNode.TargetScale,
                        scaleMode: scaleNode.ScaleMode,
                        usePivotOffset: scaleNode.UsePivotOffset,
                        pivotLocalOffset: scaleNode.PivotLocalOffset));
                    continue;
                }

                issues.Add(new MovingPlatformTreeValidationIssue(
                    MovingPlatformTreeValidationSeverity.Error,
                    "Tree.StepUnsupported",
                    $"Selector '{selector.Label}' contains an unsupported step type '{step.GetType().Name}'."));
                return BuildInvalidRoute(selector, anchorRailIndex);
            }

            // Loop の場合、末尾が anchor に戻らない「開いた経路」(例 A→B→C) だと、巻き戻し時に
            // step0 の終点(=2番目の点 B)へ向かうため、最後の2点(B↔C)で往復してしまう。
            // 末尾 → anchor への戻りステップを補完して閉ループ化し、A→B→C→A→… と正しく巡回させる。
            if (selector.Rule.PlaybackMode == MovingPlatformPlaybackMode.Loop &&
                currentRailIndex != anchorRailIndex &&
                TryBuildRailPath(railNodes, currentRailIndex, anchorRailIndex, pathBuffer) &&
                pathBuffer.Count > 1)
            {
                for (int pathIndex = 0; pathIndex < pathBuffer.Count - 1; pathIndex++)
                {
                    int fromRailIndex = pathBuffer[pathIndex];
                    int toRailIndex = pathBuffer[pathIndex + 1];
                    MovingPlatformTreeEdgeTiming timing = ResolveEdgeTiming(railNodes, fromRailIndex, toRailIndex, selector.Rule);

                    resolvedSteps.Add(new MovingPlatformSelectorRoute.ResolvedStep(
                        -1, // 合成した戻りステップ（オーサリング由来ではない）。
                        MovingPlatformSelectorRoute.ResolvedStepKind.Move,
                        fromRailIndex,
                        toRailIndex,
                        timing.Duration,
                        timing.EasingMode,
                        null));

                    referencedRailIndices.Add(toRailIndex);
                }

                routeRailIndices.Add(anchorRailIndex);
                currentRailIndex = anchorRailIndex;
            }

            if (resolvedSteps.Count == 0)
            {
                issues.Add(new MovingPlatformTreeValidationIssue(
                    MovingPlatformTreeValidationSeverity.Error,
                    "Tree.SelectorEmpty",
                    $"Selector '{selector.Label}' has no playable steps."));
                return BuildInvalidRoute(selector, anchorRailIndex);
            }

            int[] referencedRails = new int[referencedRailIndices.Count];
            referencedRailIndices.CopyTo(referencedRails);
            Array.Sort(referencedRails);

            return new MovingPlatformSelectorRoute(
                selector.StableId,
                selector.Label,
                selector.Rule,
                anchorRailIndex,
                resolvedSteps.ToArray(),
                routeRailIndices.ToArray(),
                referencedRails);
        }

        private static MovingPlatformSelectorRoute BuildInvalidRoute(MovingPlatformSelectorNodeAuthoring selector, int anchorRailIndex)
        {
            return new MovingPlatformSelectorRoute(
                selector != null ? selector.StableId : string.Empty,
                selector != null ? selector.Label : "Selector",
                selector != null ? selector.Rule : new MovingPlatformSelectorRuleAuthoring(),
                anchorRailIndex,
                Array.Empty<MovingPlatformSelectorRoute.ResolvedStep>(),
                new[] { anchorRailIndex },
                new[] { anchorRailIndex });
        }

        private static bool TryBuildRailPath(RailNodeRuntime[] railNodes, int fromRailIndex, int toRailIndex, List<int> pathBuffer)
        {
            pathBuffer.Clear();
            if (fromRailIndex < 0 || toRailIndex < 0 || fromRailIndex >= railNodes.Length || toRailIndex >= railNodes.Length)
                return false;

            if (fromRailIndex == toRailIndex)
            {
                pathBuffer.Add(fromRailIndex);
                return true;
            }

            var fromAncestors = new Dictionary<int, int>();
            for (int cursor = fromRailIndex, depth = 0; cursor >= 0; cursor = railNodes[cursor].ParentIndex, depth++)
                fromAncestors[cursor] = depth;

            int lca = -1;
            for (int cursor = toRailIndex; cursor >= 0; cursor = railNodes[cursor].ParentIndex)
            {
                if (fromAncestors.ContainsKey(cursor))
                {
                    lca = cursor;
                    break;
                }
            }

            if (lca < 0)
                return false;

            int walker = fromRailIndex;
            while (walker != lca)
            {
                pathBuffer.Add(walker);
                walker = railNodes[walker].ParentIndex;
            }

            pathBuffer.Add(lca);

            var downward = new List<int>();
            walker = toRailIndex;
            while (walker != lca)
            {
                downward.Add(walker);
                walker = railNodes[walker].ParentIndex;
            }

            for (int i = downward.Count - 1; i >= 0; i--)
                pathBuffer.Add(downward[i]);

            return pathBuffer.Count > 0;
        }

        private static Vector3 ResolveLocalPosition(RailNodeRuntime[] railNodes, int railIndex)
        {
            ReactiveVector3 localPosition = railNodes[railIndex].LocalPosition;
            return localPosition.SourceKind == ReactiveVector3SourceKind.Literal
                ? localPosition.Literal
                : localPosition.FallbackValue;
        }

        private static MovingPlatformTreeEdgeTiming ResolveEdgeTiming(
            RailNodeRuntime[] railNodes,
            int fromRailIndex,
            int toRailIndex,
            MovingPlatformSelectorRuleAuthoring rule)
        {
            rule ??= new MovingPlatformSelectorRuleAuthoring();
            int childIndex;
            if (railNodes[toRailIndex].ParentIndex == fromRailIndex)
                childIndex = toRailIndex;
            else if (railNodes[fromRailIndex].ParentIndex == toRailIndex)
                childIndex = fromRailIndex;
            else
                childIndex = -1;

            if (childIndex < 0)
            {
                float distance = Vector3.Distance(ResolveLocalPosition(railNodes, fromRailIndex), ResolveLocalPosition(railNodes, toRailIndex));
                float duration = rule.DefaultTimingControl == MovingPlatformTimingControl.Speed
                    ? (distance > 0.0001f ? distance / rule.DefaultSpeed : 0.01f)
                    : rule.DefaultDuration;
                return new MovingPlatformTreeEdgeTiming(duration, rule.DefaultEasingMode);
            }

            RailNodeRuntime childNode = railNodes[childIndex];
            if (!childNode.OverrideIncomingTiming)
            {
                float distance = Vector3.Distance(ResolveLocalPosition(railNodes, fromRailIndex), ResolveLocalPosition(railNodes, toRailIndex));
                float duration = rule.DefaultTimingControl == MovingPlatformTimingControl.Speed
                    ? (distance > 0.0001f ? distance / rule.DefaultSpeed : 0.01f)
                    : rule.DefaultDuration;
                return new MovingPlatformTreeEdgeTiming(duration, rule.DefaultEasingMode);
            }

            float edgeDistance = Vector3.Distance(ResolveLocalPosition(railNodes, fromRailIndex), ResolveLocalPosition(railNodes, toRailIndex));
            float edgeDuration = childNode.IncomingTimingControl == MovingPlatformTimingControl.Speed
                ? (edgeDistance > 0.0001f ? edgeDistance / childNode.IncomingSpeed : 0.01f)
                : childNode.IncomingDuration;
            return new MovingPlatformTreeEdgeTiming(edgeDuration, childNode.IncomingEasingMode);
        }
    }

    internal readonly struct MovingPlatformTraversalEvent
    {
        public readonly int SelectorIndex;
        public readonly int StepIndex;
        public readonly bool Enter;

        public MovingPlatformTraversalEvent(int selectorIndex, int stepIndex, bool enter)
        {
            SelectorIndex = selectorIndex;
            StepIndex = stepIndex;
            Enter = enter;
        }
    }

    internal sealed class MovingPlatformTraversalController
    {
        private struct ActiveSegmentState
        {
            public bool IsActive;
            public bool IsTransfer;
            public MovingPlatformSelectorRoute.ResolvedStepKind Kind;
            public MovingPlatformPose StartPose;
            public MovingPlatformPose EndPose;
            public float Duration;
            public float Elapsed;
            public MovingPlatformEasingMode EasingMode;
            public SignalId WaitSignalId;
            public bool WaitSignalTriggered;
            public int SelectorIndex;
            public int StepCursor;
            public int FromRailIndex;
            public int ToRailIndex;
        }

        private readonly MovingPlatformTreeRuntime treeRuntime;
        private readonly float maxRouteLinearSpeed;
        private readonly float maxTransferLinearSpeed;
        private readonly List<MovingPlatformTraversalEvent> pendingEvents = new();
        private readonly List<int> transferPath = new();

        private ActiveSegmentState activeSegment;
        private MovingPlatformBasePose railReferencePose;
        private bool hasRailReferencePose;
        private int currentSelectorIndex = -1;
        private int currentStepCursor = -1;
        private int currentDirection = 1;
        private int currentRailIndex = -1;
        private int transferCursor = -1;
        private int transferTargetSelectorIndex = -1;
        private bool transferResetWhenSelected;
        private bool segmentPaused;

        public MovingPlatformTraversalController(
            MovingPlatformTreeRuntime treeRuntime,
            float maxRouteLinearSpeed,
            float maxTransferLinearSpeed)
        {
            this.treeRuntime = treeRuntime;
            this.maxRouteLinearSpeed = Mathf.Max(0.1f, maxRouteLinearSpeed);
            this.maxTransferLinearSpeed = Mathf.Max(0.1f, maxTransferLinearSpeed);
        }

        public bool IsValid => treeRuntime != null && treeRuntime.IsValid;

        public void Reset(in MovingPlatformBasePose basePose, Vector3 currentWorldPosition)
        {
            AbortActiveSegment(false);
            ClearTransferState();
            railReferencePose = basePose;
            hasRailReferencePose = true;
            currentSelectorIndex = -1;
            currentStepCursor = -1;
            currentDirection = 1;
            segmentPaused = false;
            pendingEvents.Clear();
            currentRailIndex = treeRuntime != null && treeRuntime.TryFindNearestRailNode(basePose, currentWorldPosition, out int nearestRailIndex)
                ? nearestRailIndex
                : -1;
        }

        public bool BeginSelectorTransition(
            int selectorIndex,
            in MovingPlatformBasePose basePose,
            Vector3 currentWorldPosition,
            bool resetWhenSelected)
        {
            if (!IsValid || !treeRuntime.TryGetSelectorRoute(selectorIndex, out MovingPlatformSelectorRoute route))
                return false;

            railReferencePose = basePose;
            hasRailReferencePose = true;

            int startRailIndex = currentRailIndex;
            if (startRailIndex < 0)
            {
                if (!treeRuntime.TryFindNearestRailNode(basePose, currentWorldPosition, out startRailIndex))
                    startRailIndex = route.AnchorRailIndex;
            }

            if (currentSelectorIndex < 0 || currentSelectorIndex == selectorIndex)
            {
                ClearTransferState();
                currentSelectorIndex = selectorIndex;
                currentDirection = 1;
                currentStepCursor = resetWhenSelected ? -1 : currentStepCursor;
                currentRailIndex = startRailIndex;

                int targetRailIndex = resetWhenSelected ? route.AnchorRailIndex : ResolveTransferTargetRail(route, startRailIndex);
                if (targetRailIndex != startRailIndex &&
                    treeRuntime.TryBuildRailPath(startRailIndex, targetRailIndex, transferPath) &&
                    transferPath.Count >= 2)
                {
                    transferCursor = 0;
                    transferTargetSelectorIndex = selectorIndex;
                    transferResetWhenSelected = resetWhenSelected;
                }

                return true;
            }

            AbortActiveSegment(true);

            int selectorTargetRailIndex = resetWhenSelected ? route.AnchorRailIndex : ResolveTransferTargetRail(route, startRailIndex);
            if (!treeRuntime.TryBuildRailPath(startRailIndex, selectorTargetRailIndex, transferPath) || transferPath.Count < 2)
            {
                ClearTransferState();
                currentSelectorIndex = selectorIndex;
                currentStepCursor = resetWhenSelected ? -1 : currentStepCursor;
                currentDirection = 1;
                currentRailIndex = selectorTargetRailIndex;
                return true;
            }

            transferCursor = 0;
            transferTargetSelectorIndex = selectorIndex;
            transferResetWhenSelected = resetWhenSelected;
            currentSelectorIndex = selectorIndex;
            currentStepCursor = -1;
            currentDirection = 1;
            return true;
        }

        public MovingPlatformPose Tick(
            float deltaTime,
            int requestedSelectorIndex,
            in MovingPlatformBasePose basePose,
            Vector3 currentWorldPosition,
            Quaternion currentWorldRotation,
            Vector3 currentLocalScale,
            out bool sequenceCompleted)
        {
            sequenceCompleted = false;
            MovingPlatformPose currentPose = new(currentWorldPosition, currentWorldRotation, currentLocalScale);
            if (!IsValid)
                return currentPose;

            bool hasRequestedRoute = treeRuntime.TryGetSelectorRoute(requestedSelectorIndex, out MovingPlatformSelectorRoute requestedRoute);
            if (!hasRequestedRoute)
            {
                if (activeSegment.IsActive)
                    segmentPaused = true;

                return EvaluateCurrentPose(currentPose);
            }

            if (currentSelectorIndex != requestedSelectorIndex && !activeSegment.IsActive)
            {
                railReferencePose = basePose;
                hasRailReferencePose = true;
                currentSelectorIndex = requestedSelectorIndex;
                currentStepCursor = -1;
                currentDirection = 1;
                currentRailIndex = requestedRoute.AnchorRailIndex;
                ClearTransferState();
            }

            if (segmentPaused && activeSegment.IsActive)
            {
                if (currentSelectorIndex == requestedSelectorIndex)
                {
                    segmentPaused = false;
                }
                else
                {
                    AbortActiveSegment(true);
                }
            }

            float remainingTime = Mathf.Max(0f, deltaTime);
            // 1フレーム分の remainingTime を、長さ 0 に近い極短セグメント
            // (Wait / InlineAction / 同一座標 Move 等) を跨いで複数消費するためのループ。
            // セグメントが万一前進しない異常時に無限ループへ陥らないよう、
            // 1フレームあたりの処理セグメント数を 128 で安全打ち切りする
            // (通常の足場経路でこの上限に達することはない)。
            int guard = 0;
            while (remainingTime > 0f && guard++ < 128)
            {
                if (!activeSegment.IsActive)
                {
                    if (HasPendingTransfer(requestedSelectorIndex))
                    {
                        if (!TryBeginTransferStep(currentPose))
                            continue;
                    }
                    else
                    {
                        if (!TryBeginStep(requestedSelectorIndex, currentPose, ref sequenceCompleted))
                            break;
                    }
                }

                if (!activeSegment.IsActive)
                    break;

                float remainingSegmentTime = Mathf.Max(0f, activeSegment.Duration - activeSegment.Elapsed);
                if (remainingTime < remainingSegmentTime)
                {
                    activeSegment.Elapsed += remainingTime;
                    currentPose = EvaluateCurrentPose(currentPose);
                    remainingTime = 0f;
                    break;
                }

                activeSegment.Elapsed = activeSegment.Duration;
                remainingTime -= remainingSegmentTime;
                currentPose = EvaluateCurrentPose(currentPose);
                CompleteActiveSegment();
            }

            return EvaluateCurrentPose(currentPose);
        }

        public void NotifySignal(SignalId signalId)
        {
            if (!activeSegment.IsActive || activeSegment.Kind != MovingPlatformSelectorRoute.ResolvedStepKind.WaitSignal)
                return;

            if (activeSegment.WaitSignalId.Equals(signalId))
                activeSegment.WaitSignalTriggered = true;
        }

        public void DrainEvents(List<MovingPlatformTraversalEvent> buffer)
        {
            if (buffer == null)
                return;

            buffer.Clear();
            for (int i = 0; i < pendingEvents.Count; i++)
                buffer.Add(pendingEvents[i]);

            pendingEvents.Clear();
        }

        private bool TryBeginStep(int requestedSelectorIndex, in MovingPlatformPose currentPose, ref bool sequenceCompleted)
        {
            if (!treeRuntime.TryGetSelectorRoute(requestedSelectorIndex, out MovingPlatformSelectorRoute route))
                return false;

            if (!route.TryGetNextStepCursor(currentStepCursor, currentDirection, out int nextCursor, out int nextDirection, out bool completedCycle))
            {
                sequenceCompleted = true;
                return false;
            }

            if (!route.TryGetStep(nextCursor, out MovingPlatformSelectorRoute.ResolvedStep step))
                return false;

            currentStepCursor = nextCursor;
            currentDirection = nextDirection;
            sequenceCompleted |= completedCycle;

            StartStep(requestedSelectorIndex, nextCursor, step, currentPose);
            return true;
        }

        private void StartStep(
            int selectorIndex,
            int stepCursor,
            in MovingPlatformSelectorRoute.ResolvedStep step,
            in MovingPlatformPose currentPose)
        {
            MovingPlatformPose endPose = ResolveEndPose(step, currentPose);
            bool waitSignal = step.Kind == MovingPlatformSelectorRoute.ResolvedStepKind.WaitSignal;
            float duration = waitSignal ? 0.01f : Mathf.Max(0.0001f, step.Duration);

            if (!waitSignal && step.Kind == MovingPlatformSelectorRoute.ResolvedStepKind.Move)
            {
                float distance = Vector3.Distance(currentPose.Position, endPose.Position);
                float minDuration = distance / maxRouteLinearSpeed;
                duration = Mathf.Max(duration, minDuration);
            }

            activeSegment = new ActiveSegmentState
            {
                IsActive = true,
                IsTransfer = false,
                Kind = step.Kind,
                StartPose = currentPose,
                EndPose = endPose,
                Duration = duration,
                Elapsed = 0f,
                EasingMode = step.EasingMode,
                WaitSignalId = step.WaitSignalId,
                WaitSignalTriggered = !waitSignal,
                SelectorIndex = selectorIndex,
                StepCursor = stepCursor,
                FromRailIndex = step.FromRailIndex,
                ToRailIndex = step.ToRailIndex,
            };

            pendingEvents.Add(new MovingPlatformTraversalEvent(selectorIndex, stepCursor, true));
            segmentPaused = false;
        }

        private bool TryBeginTransferStep(in MovingPlatformPose currentPose)
        {
            if (transferCursor < 0 || transferPath.Count < 2 || transferCursor >= transferPath.Count - 1)
            {
                FinalizeTransferState();
                return false;
            }

            int fromRailIndex = transferPath[transferCursor];
            int toRailIndex = transferPath[transferCursor + 1];

            MovingPlatformSelectorRuleAuthoring rule = treeRuntime.TryGetSelectorRoute(transferTargetSelectorIndex, out MovingPlatformSelectorRoute route)
                ? route.Rule
                : new MovingPlatformSelectorRuleAuthoring();

            MovingPlatformTreeEdgeTiming edgeTiming = treeRuntime.ResolveEdgeTiming(fromRailIndex, toRailIndex, rule);
            Vector3 targetPosition = treeRuntime.GetWorldPosition(railReferencePose, toRailIndex);
            float transferDistance = Vector3.Distance(currentPose.Position, targetPosition);
            float duration = Mathf.Max(edgeTiming.Duration, transferDistance / maxTransferLinearSpeed);

            activeSegment = new ActiveSegmentState
            {
                IsActive = true,
                IsTransfer = true,
                Kind = MovingPlatformSelectorRoute.ResolvedStepKind.Move,
                StartPose = currentPose,
                EndPose = new MovingPlatformPose(targetPosition, currentPose.Rotation, currentPose.LocalScale),
                Duration = Mathf.Max(0.0001f, duration),
                Elapsed = 0f,
                EasingMode = edgeTiming.EasingMode,
                WaitSignalTriggered = true,
                SelectorIndex = -1,
                StepCursor = -1,
                FromRailIndex = fromRailIndex,
                ToRailIndex = toRailIndex,
            };

            segmentPaused = false;
            return true;
        }

        private MovingPlatformPose ResolveEndPose(in MovingPlatformSelectorRoute.ResolvedStep step, in MovingPlatformPose currentPose)
        {
            switch (step.Kind)
            {
                case MovingPlatformSelectorRoute.ResolvedStepKind.Move:
                    {
                        MovingPlatformBasePose poseBase = hasRailReferencePose
                            ? railReferencePose
                            : new MovingPlatformBasePose(currentPose.Position, currentPose.Rotation, currentPose.LocalScale);
                        Vector3 targetPosition = treeRuntime.GetWorldPosition(poseBase, step.ToRailIndex);
                        return new MovingPlatformPose(targetPosition, currentPose.Rotation, currentPose.LocalScale);
                    }

                case MovingPlatformSelectorRoute.ResolvedStepKind.Rotate:
                    {
                        Quaternion targetRotation = currentPose.Rotation * Quaternion.Euler(step.RotationEulerDelta);
                        if (!step.UsePivotOffset)
                            return new MovingPlatformPose(currentPose.Position, targetRotation, currentPose.LocalScale);

                        Vector3 pivotOffsetLocal = ResolveReactiveVector3(step.PivotLocalOffset);
                        Vector3 pivotWorld = currentPose.Position + currentPose.Rotation * Vector3.Scale(pivotOffsetLocal, currentPose.LocalScale);
                        Vector3 rotatedPivotVector = targetRotation * Vector3.Scale(pivotOffsetLocal, currentPose.LocalScale);
                        Vector3 targetPosition = pivotWorld - rotatedPivotVector;
                        return new MovingPlatformPose(targetPosition, targetRotation, currentPose.LocalScale);
                    }

                case MovingPlatformSelectorRoute.ResolvedStepKind.Scale:
                    {
                        Vector3 targetScale = step.ScaleMode == MovingPlatformScaleMode.Multiply
                            ? Vector3.Scale(currentPose.LocalScale, step.ScaleTarget)
                            : step.ScaleTarget;

                        if (!step.UsePivotOffset)
                            return new MovingPlatformPose(currentPose.Position, currentPose.Rotation, targetScale);

                        Vector3 pivotOffsetLocal = ResolveReactiveVector3(step.PivotLocalOffset);
                        Vector3 pivotWorld = currentPose.Position + currentPose.Rotation * Vector3.Scale(pivotOffsetLocal, currentPose.LocalScale);
                        Vector3 scaledPivotVector = currentPose.Rotation * Vector3.Scale(pivotOffsetLocal, targetScale);
                        Vector3 targetPosition = pivotWorld - scaledPivotVector;
                        return new MovingPlatformPose(targetPosition, currentPose.Rotation, targetScale);
                    }

                default:
                    return currentPose;
            }
        }

        private static Vector3 ResolveReactiveVector3(ReactiveVector3 reactiveVector)
        {
            return reactiveVector.SourceKind == ReactiveVector3SourceKind.Literal
                ? reactiveVector.Literal
                : reactiveVector.FallbackValue;
        }

        private MovingPlatformPose EvaluateCurrentPose(in MovingPlatformPose fallbackPose)
        {
            if (!activeSegment.IsActive)
                return fallbackPose;

            float normalizedTime = activeSegment.Duration > 0.0f
                ? Mathf.Clamp01(activeSegment.Elapsed / activeSegment.Duration)
                : 1.0f;
            float eased = Ease(activeSegment.EasingMode, normalizedTime);

            if (activeSegment.Kind == MovingPlatformSelectorRoute.ResolvedStepKind.WaitSignal && !activeSegment.WaitSignalTriggered)
                eased = 0.0f;

            Vector3 position = Vector3.Lerp(activeSegment.StartPose.Position, activeSegment.EndPose.Position, eased);
            Quaternion rotation = Quaternion.Slerp(activeSegment.StartPose.Rotation, activeSegment.EndPose.Rotation, eased);
            Vector3 scale = Vector3.Lerp(activeSegment.StartPose.LocalScale, activeSegment.EndPose.LocalScale, eased);
            return new MovingPlatformPose(position, rotation, scale);
        }

        private void CompleteActiveSegment()
        {
            if (!activeSegment.IsActive)
                return;

            bool wasTransfer = activeSegment.IsTransfer;
            int selectorIndex = activeSegment.SelectorIndex;
            int stepCursor = activeSegment.StepCursor;
            int arrivedRailIndex = activeSegment.ToRailIndex;
            activeSegment = default;

            currentRailIndex = arrivedRailIndex;

            if (wasTransfer)
            {
                transferCursor++;
                if (transferCursor >= transferPath.Count - 1)
                    FinalizeTransferState();
            }
            else if (selectorIndex >= 0 && stepCursor >= 0)
            {
                pendingEvents.Add(new MovingPlatformTraversalEvent(selectorIndex, stepCursor, false));
            }
        }

        private void AbortActiveSegment(bool emitExit)
        {
            if (!activeSegment.IsActive)
                return;

            if (emitExit && !activeSegment.IsTransfer && activeSegment.SelectorIndex >= 0 && activeSegment.StepCursor >= 0)
                pendingEvents.Add(new MovingPlatformTraversalEvent(activeSegment.SelectorIndex, activeSegment.StepCursor, false));

            activeSegment = default;
            segmentPaused = false;
        }

        private bool HasPendingTransfer(int requestedSelectorIndex)
        {
            return transferCursor >= 0 &&
                   transferPath.Count >= 2 &&
                   transferTargetSelectorIndex == requestedSelectorIndex;
        }

        private int ResolveTransferTargetRail(MovingPlatformSelectorRoute route, int startRailIndex)
        {
            if (route == null || !route.IsValid)
                return startRailIndex;

            if (route.ContainsRailIndex(startRailIndex))
                return startRailIndex;

            int bestRailIndex = route.AnchorRailIndex;
            float bestCost = float.MaxValue;

            for (int i = 0; i < route.RouteRailCount; i++)
            {
                int candidate = route.GetRouteRailIndexAt(i);
                var path = new List<int>();
                if (!treeRuntime.TryBuildRailPath(startRailIndex, candidate, path))
                    continue;

                float cost = path.Count;
                if (cost >= bestCost)
                    continue;

                bestCost = cost;
                bestRailIndex = candidate;
            }

            return bestRailIndex;
        }

        private void FinalizeTransferState()
        {
            int targetSelectorIndex = transferTargetSelectorIndex;
            int arrivalRailIndex = transferPath.Count > 0 ? transferPath[transferPath.Count - 1] : currentRailIndex;
            bool resetWhenSelected = transferResetWhenSelected;

            ClearTransferState();

            if (!treeRuntime.TryGetSelectorRoute(targetSelectorIndex, out MovingPlatformSelectorRoute route))
                return;

            currentSelectorIndex = targetSelectorIndex;
            currentRailIndex = arrivalRailIndex;

            if (resetWhenSelected)
            {
                currentStepCursor = -1;
                currentDirection = 1;
                return;
            }

            if (route.TryResolveResumeCursor(arrivalRailIndex, out int resumeCursor, out int resumeDirection))
            {
                currentStepCursor = resumeCursor;
                currentDirection = resumeDirection;
            }
            else
            {
                currentStepCursor = -1;
                currentDirection = 1;
            }
        }

        private void ClearTransferState()
        {
            transferPath.Clear();
            transferCursor = -1;
            transferTargetSelectorIndex = -1;
            transferResetWhenSelected = false;
        }

        private static float Ease(MovingPlatformEasingMode easingMode, float normalizedTime)
        {
            normalizedTime = Mathf.Clamp01(normalizedTime);
            return easingMode switch
            {
                MovingPlatformEasingMode.EaseInOutSine => 0.5f - 0.5f * Mathf.Cos(normalizedTime * Mathf.PI),
                MovingPlatformEasingMode.SmoothStep => normalizedTime * normalizedTime * (3.0f - 2.0f * normalizedTime),
                _ => normalizedTime,
            };
        }
    }
}

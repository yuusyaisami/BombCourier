using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.Gimmick.MovingPlatform
{
    public enum MovingPlatformPlaybackMode
    {
        Once = 0,
        Loop = 1,
        PingPong = 2,
    }

    public enum MovingPlatformEasingMode
    {
        Linear = 0,
        SmoothStep = 1,
        EaseInOutSine = 2,
    }

    public enum MovingPlatformTimingControl
    {
        Duration = 0,
        Speed = 1,
    }

    public readonly struct MovingPlatformBasePose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 LocalScale;

        public MovingPlatformBasePose(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            Position = position;
            Rotation = rotation;
            LocalScale = localScale;
        }

        public MovingPlatformPose ToPose()
        {
            return new MovingPlatformPose(Position, Rotation, LocalScale);
        }
    }

    public readonly struct MovingPlatformPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 LocalScale;

        public MovingPlatformPose(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            Position = position;
            Rotation = rotation;
            LocalScale = localScale;
        }
    }

    internal static class MovingPlatformRailIdUtility
    {
        public static string Normalize(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim();
        }
    }

    [Serializable]
    public sealed class MovingPlatformRailNode
    {
        internal const string DefaultNodeLabelPrefix = "Node";

        [SerializeField] private string nodePath = "Node";
        [SerializeField] private ReactiveVector3 localPosition = default;

        public string NodePath => MovingPlatformRailIdUtility.Normalize(nodePath);
        public ReactiveVector3 LocalPosition => localPosition;

        internal string RawNodePath => nodePath;

        internal bool NeedsGeneratedNodePath()
        {
            return string.IsNullOrWhiteSpace(NodePath) || string.Equals(NodePath, DefaultNodeLabelPrefix, StringComparison.Ordinal);
        }

        internal void SetNodePath(string value)
        {
            nodePath = MovingPlatformRailIdUtility.Normalize(value);
        }
    }

    [Serializable]
    public sealed class MovingPlatformRailConnection
    {
        [SerializeField] private string fromNodePath = string.Empty;
        [SerializeField] private string toNodePath = string.Empty;
        [SerializeField] private bool bidirectional = true;
        [SerializeField, Min(0.01f)] private float duration = 1.0f;
        [SerializeField] private MovingPlatformEasingMode easingMode = MovingPlatformEasingMode.SmoothStep;

        public string FromNodePath => MovingPlatformRailIdUtility.Normalize(fromNodePath);
        public string ToNodePath => MovingPlatformRailIdUtility.Normalize(toNodePath);
        public bool Bidirectional => bidirectional;
        public float Duration => Mathf.Max(0.01f, duration);
        public MovingPlatformEasingMode EasingMode => easingMode;
    }

    internal enum MovingPlatformRailTransitionEventKind
    {
        SegmentEnter = 0,
        SegmentExit = 1,
    }

    internal readonly struct MovingPlatformRailTransitionEvent
    {
        public readonly int LayerIndex;
        public readonly int SegmentIndex;
        public readonly MovingPlatformRailTransitionEventKind Kind;

        public MovingPlatformRailTransitionEvent(int layerIndex, int segmentIndex, MovingPlatformRailTransitionEventKind kind)
        {
            LayerIndex = layerIndex;
            SegmentIndex = segmentIndex;
            Kind = kind;
        }
    }

    internal readonly struct MovingPlatformRailLocation
    {
        public readonly bool IsValid;
        public readonly bool IsOnSegment;
        public readonly int FromNodeIndex;
        public readonly int ToNodeIndex;
        public readonly float Normalized;
        public readonly Vector3 WorldPosition;

        public bool IsOnNode => IsValid && !IsOnSegment;
        public int NodeIndex => FromNodeIndex;

        public MovingPlatformRailLocation(
            bool isValid,
            bool isOnSegment,
            int fromNodeIndex,
            int toNodeIndex,
            float normalized,
            Vector3 worldPosition)
        {
            IsValid = isValid;
            IsOnSegment = isOnSegment;
            FromNodeIndex = fromNodeIndex;
            ToNodeIndex = toNodeIndex;
            Normalized = Mathf.Clamp01(normalized);
            WorldPosition = worldPosition;
        }

        public static MovingPlatformRailLocation AtNode(int nodeIndex, Vector3 worldPosition)
        {
            return new MovingPlatformRailLocation(true, false, nodeIndex, nodeIndex, 0f, worldPosition);
        }
    }

    internal sealed class MovingPlatformRailGraph
    {
        internal readonly struct NodeData
        {
            public readonly string Path;
            public readonly ReactiveVector3 LocalPosition;

            public NodeData(string path, ReactiveVector3 localPosition)
            {
                Path = path;
                LocalPosition = localPosition;
            }
        }

        internal readonly struct EdgeData
        {
            public readonly int FromIndex;
            public readonly int ToIndex;
            public readonly float Duration;
            public readonly MovingPlatformEasingMode EasingMode;

            public EdgeData(int fromIndex, int toIndex, float duration, MovingPlatformEasingMode easingMode)
            {
                FromIndex = fromIndex;
                ToIndex = toIndex;
                Duration = Mathf.Max(0.01f, duration);
                EasingMode = easingMode;
            }
        }

        private readonly NodeData[] nodes;
        private readonly Dictionary<string, int> nodeIndicesByPath;
        private readonly Dictionary<ulong, EdgeData> edgesByPair;
        private readonly List<EdgeData>[] outgoingEdges;
        private readonly EdgeData[] allEdges;
        private readonly ReactiveValueResolverService resolver;
        private readonly ReactiveEvalContext evalContext;

        private MovingPlatformRailGraph(
            NodeData[] nodes,
            Dictionary<string, int> nodeIndicesByPath,
            Dictionary<ulong, EdgeData> edgesByPair,
            List<EdgeData>[] outgoingEdges,
            EdgeData[] allEdges,
            ReactiveValueResolverService resolver,
            ReactiveEvalContext evalContext)
        {
            this.nodes = nodes;
            this.nodeIndicesByPath = nodeIndicesByPath;
            this.edgesByPair = edgesByPair;
            this.outgoingEdges = outgoingEdges;
            this.allEdges = allEdges;
            this.resolver = resolver;
            this.evalContext = evalContext;
        }

        public bool IsValid => nodes != null && nodes.Length > 0 && edgesByPair != null && edgesByPair.Count > 0;
        public int NodeCount => nodes != null ? nodes.Length : 0;

        public static MovingPlatformRailGraph Build(
            Transform platformTransform,
            IReadOnlyList<MovingPlatformRailNode> railNodes,
            IReadOnlyList<MovingPlatformRailConnection> railConnections)
        {
            return Build(platformTransform, railNodes, railConnections, null, default);
        }

        public static MovingPlatformRailGraph Build(
            Transform platformTransform,
            IReadOnlyList<MovingPlatformRailNode> railNodes,
            IReadOnlyList<MovingPlatformRailConnection> railConnections,
            ReactiveValueResolverService resolver,
            ReactiveEvalContext evalContext)
        {
            _ = platformTransform;

            if (railNodes == null || railNodes.Count == 0)
                return null;

            var nodeIndicesByPath = new Dictionary<string, int>(StringComparer.Ordinal);
            var nodeBuffer = new List<NodeData>(railNodes.Count);

            for (int i = 0; i < railNodes.Count; i++)
            {
                MovingPlatformRailNode railNode = railNodes[i];
                if (railNode == null)
                    continue;

                string nodePath = railNode.NodePath;
                if (string.IsNullOrWhiteSpace(nodePath) || nodeIndicesByPath.ContainsKey(nodePath))
                    continue;

                int nodeIndex = nodeBuffer.Count;
                nodeBuffer.Add(new NodeData(nodePath, railNode.LocalPosition));
                nodeIndicesByPath.Add(nodePath, nodeIndex);
            }

            if (nodeBuffer.Count == 0)
                return null;

            var edgesByPair = new Dictionary<ulong, EdgeData>();
            var outgoingEdges = new List<EdgeData>[nodeBuffer.Count];
            var edgeBuffer = new List<EdgeData>(railConnections != null ? railConnections.Count * 2 : 0);

            for (int i = 0; i < outgoingEdges.Length; i++)
                outgoingEdges[i] = new List<EdgeData>(4);

            if (railConnections != null)
            {
                for (int i = 0; i < railConnections.Count; i++)
                {
                    MovingPlatformRailConnection connection = railConnections[i];
                    if (connection == null ||
                        string.IsNullOrWhiteSpace(connection.FromNodePath) ||
                        string.IsNullOrWhiteSpace(connection.ToNodePath) ||
                        !nodeIndicesByPath.TryGetValue(connection.FromNodePath, out int fromIndex) ||
                        !nodeIndicesByPath.TryGetValue(connection.ToNodePath, out int toIndex) ||
                        fromIndex == toIndex)
                    {
                        continue;
                    }

                    AddEdge(fromIndex, toIndex, connection.Duration, connection.EasingMode);
                    if (connection.Bidirectional)
                        AddEdge(toIndex, fromIndex, connection.Duration, connection.EasingMode);
                }
            }

            return new MovingPlatformRailGraph(
                nodeBuffer.ToArray(),
                nodeIndicesByPath,
                edgesByPair,
                outgoingEdges,
                edgeBuffer.ToArray(),
                resolver,
                evalContext);

            void AddEdge(int fromIndex, int toIndex, float duration, MovingPlatformEasingMode easingMode)
            {
                ulong key = ComposeEdgeKey(fromIndex, toIndex);
                EdgeData edge = new EdgeData(fromIndex, toIndex, duration, easingMode);
                edgesByPair[key] = edge;
                outgoingEdges[fromIndex].Add(edge);
                edgeBuffer.Add(edge);
            }
        }

        public bool TryGetNodeIndex(string nodePath, out int nodeIndex)
        {
            if (!string.IsNullOrWhiteSpace(nodePath) &&
                nodeIndicesByPath != null &&
                nodeIndicesByPath.TryGetValue(MovingPlatformRailIdUtility.Normalize(nodePath), out nodeIndex))
            {
                return true;
            }

            nodeIndex = -1;
            return false;
        }

        public Vector3 GetWorldPosition(in MovingPlatformBasePose basePose, int nodeIndex)
        {
            Vector3 localPosition = ResolveLocalPosition(nodeIndex);
            Vector3 scaledLocalPosition = Vector3.Scale(localPosition, basePose.LocalScale);
            return basePose.Position + basePose.Rotation * scaledLocalPosition;
        }

        public bool TryGetEdge(int fromIndex, int toIndex, out EdgeData edge)
        {
            return edgesByPair.TryGetValue(ComposeEdgeKey(fromIndex, toIndex), out edge);
        }

        public bool TryGetAnyEdge(int fromIndex, int toIndex, out EdgeData edge)
        {
            if (TryGetEdge(fromIndex, toIndex, out edge))
                return true;

            return TryGetEdge(toIndex, fromIndex, out edge);
        }

        public float GetApproxDistance(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= NodeCount || toIndex < 0 || toIndex >= NodeCount)
                return 0f;

            return Vector3.Distance(ResolveLocalPosition(fromIndex), ResolveLocalPosition(toIndex));
        }

        public bool TryFindNearestLocation(in MovingPlatformBasePose basePose, Vector3 worldPosition, out MovingPlatformRailLocation location)
        {
            location = default;
            if (nodes == null || nodes.Length == 0)
                return false;

            float bestDistanceSqr = float.MaxValue;

            for (int i = 0; i < NodeCount; i++)
            {
                Vector3 nodeWorld = GetWorldPosition(basePose, i);
                float nodeDistanceSqr = (worldPosition - nodeWorld).sqrMagnitude;
                if (nodeDistanceSqr >= bestDistanceSqr)
                    continue;

                bestDistanceSqr = nodeDistanceSqr;
                location = MovingPlatformRailLocation.AtNode(i, nodeWorld);
            }

            for (int i = 0; i < allEdges.Length; i++)
            {
                EdgeData edge = allEdges[i];
                Vector3 fromWorld = GetWorldPosition(basePose, edge.FromIndex);
                Vector3 toWorld = GetWorldPosition(basePose, edge.ToIndex);
                Vector3 segment = toWorld - fromWorld;
                float segmentLengthSqr = segment.sqrMagnitude;
                if (segmentLengthSqr <= 0.000001f)
                    continue;

                float normalized = Mathf.Clamp01(Vector3.Dot(worldPosition - fromWorld, segment) / segmentLengthSqr);
                Vector3 closestPoint = Vector3.Lerp(fromWorld, toWorld, normalized);
                float distanceSqr = (worldPosition - closestPoint).sqrMagnitude;
                if (distanceSqr >= bestDistanceSqr)
                    continue;

                bestDistanceSqr = distanceSqr;
                if (normalized <= 0.0001f)
                {
                    location = MovingPlatformRailLocation.AtNode(edge.FromIndex, fromWorld);
                }
                else if (normalized >= 0.9999f)
                {
                    location = MovingPlatformRailLocation.AtNode(edge.ToIndex, toWorld);
                }
                else
                {
                    location = new MovingPlatformRailLocation(true, true, edge.FromIndex, edge.ToIndex, normalized, closestPoint);
                }
            }

            return location.IsValid;
        }

        public bool TryFindShortestPathToAny(
            in MovingPlatformBasePose basePose,
            int startNodeIndex,
            IReadOnlyList<int> targetNodeIndices,
            out List<int> nodePath,
            out float pathCost)
        {
            nodePath = null;
            pathCost = float.MaxValue;

            if (nodes == null || nodes.Length == 0 || startNodeIndex < 0 || startNodeIndex >= NodeCount || targetNodeIndices == null || targetNodeIndices.Count == 0)
                return false;

            var targetSet = new HashSet<int>(targetNodeIndices);
            if (targetSet.Contains(startNodeIndex))
            {
                nodePath = new List<int>(1) { startNodeIndex };
                pathCost = 0f;
                return true;
            }

            float[] distances = new float[NodeCount];
            int[] previous = new int[NodeCount];
            bool[] visited = new bool[NodeCount];

            for (int i = 0; i < NodeCount; i++)
            {
                distances[i] = float.MaxValue;
                previous[i] = -1;
            }

            distances[startNodeIndex] = 0f;
            int targetNodeIndex = -1;

            for (int visitCount = 0; visitCount < NodeCount; visitCount++)
            {
                int currentIndex = -1;
                float currentDistance = float.MaxValue;

                for (int i = 0; i < NodeCount; i++)
                {
                    if (visited[i] || distances[i] >= currentDistance)
                        continue;

                    currentIndex = i;
                    currentDistance = distances[i];
                }

                if (currentIndex < 0)
                    break;

                visited[currentIndex] = true;
                if (targetSet.Contains(currentIndex))
                {
                    targetNodeIndex = currentIndex;
                    pathCost = currentDistance;
                    break;
                }

                List<EdgeData> edges = outgoingEdges[currentIndex];
                for (int i = 0; i < edges.Count; i++)
                {
                    EdgeData edge = edges[i];
                    if (visited[edge.ToIndex])
                        continue;

                    float edgeCost = Vector3.Distance(
                        GetWorldPosition(basePose, edge.FromIndex),
                        GetWorldPosition(basePose, edge.ToIndex));
                    float nextDistance = currentDistance + edgeCost;
                    if (nextDistance >= distances[edge.ToIndex])
                        continue;

                    distances[edge.ToIndex] = nextDistance;
                    previous[edge.ToIndex] = currentIndex;
                }
            }

            if (targetNodeIndex < 0)
                return false;

            nodePath = new List<int>(8);
            int walkIndex = targetNodeIndex;
            while (walkIndex >= 0)
            {
                nodePath.Add(walkIndex);
                walkIndex = previous[walkIndex];
            }

            nodePath.Reverse();
            return nodePath.Count > 0;
        }

        private Vector3 ResolveLocalPosition(int nodeIndex)
        {
            NodeData node = nodes[nodeIndex];
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

        private static ulong ComposeEdgeKey(int fromIndex, int toIndex)
        {
            return ((ulong)(uint)fromIndex << 32) | (uint)toIndex;
        }
    }

    internal sealed class MovingPlatformRailLayerRoute
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

        internal readonly struct ResolvedSegment
        {
            private readonly MovingPlatformLayerSegment sourceSegment;

            public readonly int SegmentIndex;
            public readonly ResolvedStepKind Kind;
            public readonly int FromNodeIndex;
            public readonly int ToNodeIndex;
            public readonly float Duration;
            public readonly MovingPlatformEasingMode EasingMode;
            public readonly SignalId WaitSignalId;
            public readonly bool HasWaitSignal;
            public readonly Vector3 RotationEulerDelta;
            public readonly Vector3 ScaleTarget;
            public readonly MovingPlatformScaleMode ScaleMode;
            public readonly bool UsePivotOffset;
            public readonly ReactiveVector3 PivotLocalOffset;

            public ResolvedSegment(
                int segmentIndex,
                ResolvedStepKind kind,
                int fromNodeIndex,
                int toNodeIndex,
                float duration,
                MovingPlatformEasingMode easingMode,
                MovingPlatformLayerSegment sourceSegment,
                SignalId waitSignalId = default,
                bool hasWaitSignal = false,
                Vector3 rotationEulerDelta = default,
                Vector3 scaleTarget = default,
                MovingPlatformScaleMode scaleMode = MovingPlatformScaleMode.Absolute,
                bool usePivotOffset = false,
                ReactiveVector3 pivotLocalOffset = default)
            {
                SegmentIndex = segmentIndex;
                Kind = kind;
                FromNodeIndex = fromNodeIndex;
                ToNodeIndex = toNodeIndex;
                Duration = Mathf.Max(0.01f, duration);
                EasingMode = easingMode;
                this.sourceSegment = sourceSegment;
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
                if (sourceSegment is MovingPlatformInlineActionSegment inlineActionSegment)
                    inlineActionSegment.Execute(context);
            }

            public void ExecuteExit(in WiringActionContext context)
            {
                _ = context;
            }
        }

        private readonly int[] nodeIndices;
        private readonly ResolvedSegment[] segments;

        public MovingPlatformRailLayerRoute(MovingPlatformLayer layer, MovingPlatformRailGraph graph)
        {
            PlaybackMode = layer != null ? layer.PlaybackMode : MovingPlatformPlaybackMode.Once;

            if (layer == null || graph == null || !layer.UsesRailRoute || !graph.TryGetNodeIndex(layer.StartNodePath, out int currentNodeIndex))
            {
                nodeIndices = Array.Empty<int>();
                segments = Array.Empty<ResolvedSegment>();
                return;
            }

            int segmentCount = layer.Segments != null ? layer.Segments.Count : 0;
            var resolvedNodeIndices = new List<int>(Mathf.Max(2, segmentCount + 1)) { currentNodeIndex };
            var resolvedSegments = new List<ResolvedSegment>(segmentCount);

            for (int i = 0; i < segmentCount; i++)
            {
                if (!layer.TryGetSegment(i, out MovingPlatformLayerSegment segment) || segment == null)
                {
                    nodeIndices = Array.Empty<int>();
                    segments = Array.Empty<ResolvedSegment>();
                    return;
                }

                if (segment is MovingPlatformRailRouteSegment moveSegment)
                {
                    if (string.IsNullOrWhiteSpace(moveSegment.TargetNodePath) ||
                        !graph.TryGetNodeIndex(moveSegment.TargetNodePath, out int targetNodeIndex))
                    {
                        nodeIndices = Array.Empty<int>();
                        segments = Array.Empty<ResolvedSegment>();
                        return;
                    }

                    float duration;
                    MovingPlatformEasingMode easingMode;
                    if (moveSegment.OverrideConnectionTiming)
                    {
                        if (moveSegment.TimingControl == MovingPlatformTimingControl.Speed)
                        {
                            float distance = graph.GetApproxDistance(currentNodeIndex, targetNodeIndex);
                            duration = distance > 0.0001f
                                ? distance / moveSegment.Speed
                                : 0.01f;
                        }
                        else
                        {
                            duration = moveSegment.Duration;
                        }

                        easingMode = moveSegment.EasingMode;
                    }
                    else
                    {
                        if (layer.DefaultTimingControl == MovingPlatformTimingControl.Speed)
                        {
                            float distance = graph.GetApproxDistance(currentNodeIndex, targetNodeIndex);
                            duration = distance > 0.0001f
                                ? distance / layer.DefaultSpeed
                                : 0.01f;
                        }
                        else
                        {
                            duration = layer.DefaultDuration;
                        }

                        easingMode = layer.DefaultEasingMode;
                    }

                    resolvedSegments.Add(new ResolvedSegment(
                        i,
                        ResolvedStepKind.Move,
                        currentNodeIndex,
                        targetNodeIndex,
                        duration,
                        easingMode,
                        moveSegment));
                    resolvedNodeIndices.Add(targetNodeIndex);
                    currentNodeIndex = targetNodeIndex;
                    continue;
                }

                if (segment is MovingPlatformWaitSegment waitSegment)
                {
                    if (waitSegment.WaitMode == MovingPlatformWaitMode.Signal)
                    {
                        if (!waitSegment.Signal.TryResolve(out Signal signal))
                        {
                            nodeIndices = Array.Empty<int>();
                            segments = Array.Empty<ResolvedSegment>();
                            return;
                        }

                        resolvedSegments.Add(new ResolvedSegment(
                            i,
                            ResolvedStepKind.WaitSignal,
                            currentNodeIndex,
                            currentNodeIndex,
                            0.01f,
                            MovingPlatformEasingMode.Linear,
                            waitSegment,
                            signal.Id,
                            true));
                    }
                    else
                    {
                        resolvedSegments.Add(new ResolvedSegment(
                            i,
                            ResolvedStepKind.WaitDuration,
                            currentNodeIndex,
                            currentNodeIndex,
                            waitSegment.Duration,
                            MovingPlatformEasingMode.Linear,
                            waitSegment));
                    }

                    continue;
                }

                if (segment is MovingPlatformInlineActionSegment inlineActionSegment)
                {
                    resolvedSegments.Add(new ResolvedSegment(
                        i,
                        ResolvedStepKind.InlineAction,
                        currentNodeIndex,
                        currentNodeIndex,
                        0.01f,
                        MovingPlatformEasingMode.Linear,
                        inlineActionSegment));
                    continue;
                }

                if (segment is MovingPlatformRotationSegment rotationSegment)
                {
                    resolvedSegments.Add(new ResolvedSegment(
                        i,
                        ResolvedStepKind.Rotate,
                        currentNodeIndex,
                        currentNodeIndex,
                        rotationSegment.Duration,
                        rotationSegment.EasingMode,
                        rotationSegment,
                        default,
                        false,
                        rotationEulerDelta: rotationSegment.EulerDelta,
                        usePivotOffset: rotationSegment.UsePivotOffset,
                        pivotLocalOffset: rotationSegment.PivotLocalOffset));
                    continue;
                }

                if (segment is MovingPlatformScaleSegment scaleSegment)
                {
                    resolvedSegments.Add(new ResolvedSegment(
                        i,
                        ResolvedStepKind.Scale,
                        currentNodeIndex,
                        currentNodeIndex,
                        scaleSegment.Duration,
                        scaleSegment.EasingMode,
                        scaleSegment,
                        default,
                        false,
                        scaleTarget: scaleSegment.TargetScale,
                        scaleMode: scaleSegment.ScaleMode,
                        usePivotOffset: scaleSegment.UsePivotOffset,
                        pivotLocalOffset: scaleSegment.PivotLocalOffset));
                }
            }

            if (resolvedSegments.Count == 0)
            {
                nodeIndices = Array.Empty<int>();
                segments = Array.Empty<ResolvedSegment>();
                return;
            }

            nodeIndices = resolvedNodeIndices.ToArray();
            segments = resolvedSegments.ToArray();
        }

        public MovingPlatformPlaybackMode PlaybackMode { get; }
        public bool IsValid => nodeIndices != null && nodeIndices.Length >= 1 && segments != null && segments.Length > 0;
        public int NodeCount => nodeIndices != null ? nodeIndices.Length : 0;
        public int SegmentCount => segments != null ? segments.Length : 0;
        public IReadOnlyList<int> NodeIndices => nodeIndices;

        public bool ContainsNode(int nodeIndex)
        {
            if (!IsValid)
                return false;

            for (int i = 0; i < nodeIndices.Length; i++)
            {
                if (nodeIndices[i] == nodeIndex)
                    return true;
            }

            return false;
        }

        public int GetNodeIndexAt(int cursor)
        {
            return nodeIndices[cursor];
        }

        public bool TryGetSegment(int segmentIndex, out ResolvedSegment segment)
        {
            if (segments == null || segmentIndex < 0 || segmentIndex >= segments.Length)
            {
                segment = default;
                return false;
            }

            segment = segments[segmentIndex];
            return true;
        }

        public void ExecuteSegmentEnter(int segmentIndex, in WiringActionContext context)
        {
            if (TryGetSegment(segmentIndex, out ResolvedSegment segment))
                segment.ExecuteEnter(context);
        }

        public void ExecuteSegmentExit(int segmentIndex, in WiringActionContext context)
        {
            if (TryGetSegment(segmentIndex, out ResolvedSegment segment))
                segment.ExecuteExit(context);
        }

        public bool TryGetSegmentForTransition(int currentCursor, int nextCursor, out ResolvedSegment segment)
        {
            _ = currentCursor;
            return TryGetSegment(nextCursor, out segment);
        }

        public bool TryFindNodeCursor(int nodeIndex, int preferredDirection, out int cursor, out int resolvedDirection)
        {
            if (TryFindNodeCursorWithDirection(nodeIndex, preferredDirection, out cursor))
            {
                resolvedDirection = NormalizeDirection(preferredDirection);
                return true;
            }

            int fallbackDirection = NormalizeDirection(preferredDirection) >= 0 ? -1 : 1;
            if (TryFindNodeCursorWithDirection(nodeIndex, fallbackDirection, out cursor))
            {
                resolvedDirection = fallbackDirection;
                return true;
            }

            for (int i = 0; i < NodeCount; i++)
            {
                if (nodeIndices[i] == nodeIndex)
                {
                    cursor = i;
                    resolvedDirection = 1;
                    return true;
                }
            }

            cursor = -1;
            resolvedDirection = 1;
            return false;
        }

        public bool TryGetNextCursor(int currentCursor, int direction, out int nextCursor, out int nextDirection, out bool completedCycle)
        {
            nextCursor = -1;
            nextDirection = NormalizeDirection(direction);
            completedCycle = false;

            if (!IsValid || currentCursor < 0 || currentCursor >= NodeCount)
                return false;

            switch (PlaybackMode)
            {
                case MovingPlatformPlaybackMode.Loop:
                    nextCursor = currentCursor + nextDirection;
                    if (nextCursor >= NodeCount)
                    {
                        nextCursor = 0;
                        completedCycle = true;
                    }
                    else if (nextCursor < 0)
                    {
                        nextCursor = NodeCount - 1;
                        completedCycle = true;
                    }

                    return nextCursor != currentCursor;

                case MovingPlatformPlaybackMode.PingPong:
                    nextCursor = currentCursor + nextDirection;
                    if (nextCursor >= NodeCount)
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

                    return nextCursor >= 0 && nextCursor < NodeCount && nextCursor != currentCursor;

                default:
                    nextCursor = currentCursor + nextDirection;
                    return nextCursor >= 0 && nextCursor < NodeCount;
            }
        }

        private bool TryFindNodeCursorWithDirection(int nodeIndex, int direction, out int cursor)
        {
            direction = NormalizeDirection(direction);

            for (int i = 0; i < NodeCount; i++)
            {
                if (nodeIndices[i] != nodeIndex)
                    continue;

                if (TryGetNextCursor(i, direction, out _, out _, out _))
                {
                    cursor = i;
                    return true;
                }
            }

            cursor = -1;
            return false;
        }

        private static int NormalizeDirection(int direction)
        {
            return direction >= 0 ? 1 : -1;
        }

        public bool TryGetNextSegmentCursor(int currentCursor, int direction, out int nextCursor, out int nextDirection, out bool completedCycle)
        {
            nextCursor = -1;
            nextDirection = NormalizeDirection(direction);
            completedCycle = false;

            if (!IsValid || SegmentCount <= 0)
                return false;

            if (currentCursor < 0)
            {
                nextCursor = nextDirection >= 0 ? 0 : SegmentCount - 1;
                return true;
            }

            switch (PlaybackMode)
            {
                case MovingPlatformPlaybackMode.Loop:
                    nextCursor = currentCursor + nextDirection;
                    if (nextCursor >= SegmentCount)
                    {
                        nextCursor = 0;
                        completedCycle = true;
                    }
                    else if (nextCursor < 0)
                    {
                        nextCursor = SegmentCount - 1;
                        completedCycle = true;
                    }

                    return true;

                case MovingPlatformPlaybackMode.PingPong:
                    nextCursor = currentCursor + nextDirection;
                    if (nextCursor >= SegmentCount)
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

                    return nextCursor >= 0 && nextCursor < SegmentCount;

                default:
                    nextCursor = currentCursor + nextDirection;
                    return nextCursor >= 0 && nextCursor < SegmentCount;
            }
        }
    }

    internal sealed class MovingPlatformRailController
    {
        private struct RailSegmentState
        {
            public bool IsActive;
            public MovingPlatformRailLayerRoute.ResolvedStepKind Kind;
            public MovingPlatformPose StartPose;
            public MovingPlatformPose EndPose;
            public float Duration;
            public float Elapsed;
            public MovingPlatformEasingMode EasingMode;
            public SignalId WaitSignalId;
            public bool WaitSignalTriggered;
            public int RouteLayerIndex;
            public int RouteSegmentIndex;
        }

        private readonly MovingPlatformRailGraph graph;
        private readonly MovingPlatformRailLayerRoute[] routes;
        private readonly List<MovingPlatformRailTransitionEvent> pendingTransitionEvents = new();

        private RailSegmentState activeSegment;
        private MovingPlatformBasePose railReferencePose;
        private bool hasRailReferencePose;
        private int currentRouteLayerIndex = -1;
        private int currentRouteSegmentCursor = -1;
        private int currentRouteDirection = 1;
        private bool segmentPaused;

        public MovingPlatformRailController(MovingPlatformRailGraph graph, IReadOnlyList<MovingPlatformRailLayerRoute> routes)
        {
            this.graph = graph;

            if (routes == null)
            {
                this.routes = Array.Empty<MovingPlatformRailLayerRoute>();
                return;
            }

            this.routes = new MovingPlatformRailLayerRoute[routes.Count];
            for (int i = 0; i < routes.Count; i++)
                this.routes[i] = routes[i];
        }

        public bool IsValid
        {
            get
            {
                if (graph == null || routes == null)
                    return false;

                for (int i = 0; i < routes.Length; i++)
                {
                    if (routes[i] != null && routes[i].IsValid)
                        return true;
                }

                return false;
            }
        }

        public void Reset(in MovingPlatformBasePose basePose, Vector3 currentWorldPosition)
        {
            AbortActiveSegment(false);
            railReferencePose = basePose;
            hasRailReferencePose = true;
            currentRouteLayerIndex = -1;
            currentRouteSegmentCursor = -1;
            currentRouteDirection = 1;
            segmentPaused = false;
            _ = currentWorldPosition;
            pendingTransitionEvents.Clear();
        }

        public bool TryGetRoute(int layerIndex, out MovingPlatformRailLayerRoute route)
        {
            if (layerIndex >= 0 && layerIndex < routes.Length)
            {
                route = routes[layerIndex];
                return route != null;
            }

            route = null;
            return false;
        }

        public bool SnapToRouteStart(int layerIndex, in MovingPlatformBasePose basePose)
        {
            if (!IsValidRouteIndex(layerIndex))
                return false;

            AbortActiveSegment(false);
            railReferencePose = basePose;
            hasRailReferencePose = true;
            currentRouteLayerIndex = layerIndex;
            currentRouteSegmentCursor = -1;
            currentRouteDirection = 1;
            segmentPaused = false;
            pendingTransitionEvents.Clear();
            return true;
        }

        public void DrainTransitionEvents(List<MovingPlatformRailTransitionEvent> buffer)
        {
            if (buffer == null)
                return;

            buffer.Clear();
            for (int i = 0; i < pendingTransitionEvents.Count; i++)
                buffer.Add(pendingTransitionEvents[i]);

            pendingTransitionEvents.Clear();
        }

        public MovingPlatformPose Tick(
            float deltaTime,
            int requestedLayerIndex,
            in MovingPlatformBasePose basePose,
            Vector3 currentWorldPosition,
            Quaternion currentWorldRotation,
            Vector3 currentLocalScale,
            out bool sequenceCompleted)
        {
            sequenceCompleted = false;
            _ = basePose;
            MovingPlatformPose currentPose = new(currentWorldPosition, currentWorldRotation, currentLocalScale);

            bool hasRequestedRoute = IsValidRouteIndex(requestedLayerIndex);
            if (!hasRequestedRoute)
            {
                if (activeSegment.IsActive)
                    segmentPaused = true;

                return EvaluateCurrentPose(currentPose);
            }

            if (currentRouteLayerIndex != requestedLayerIndex && !activeSegment.IsActive)
            {
                railReferencePose = basePose;
                hasRailReferencePose = true;
                currentRouteLayerIndex = requestedLayerIndex;
                currentRouteSegmentCursor = -1;
                currentRouteDirection = 1;
            }

            if (segmentPaused && activeSegment.IsActive)
            {
                if (currentRouteLayerIndex == requestedLayerIndex)
                {
                    segmentPaused = false;
                }
                else
                {
                    AbortActiveSegment(true);
                }
            }

            float remainingTime = Mathf.Max(0f, deltaTime);
            int guard = 0;

            while (remainingTime > 0f && guard++ < 128)
            {
                if (!activeSegment.IsActive)
                {
                    if (!TryBeginStep(requestedLayerIndex, currentPose, ref sequenceCompleted))
                        break;
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
            if (!activeSegment.IsActive || activeSegment.Kind != MovingPlatformRailLayerRoute.ResolvedStepKind.WaitSignal)
                return;

            if (activeSegment.WaitSignalId.Equals(signalId))
                activeSegment.WaitSignalTriggered = true;
        }

        private bool TryBeginStep(
            int requestedLayerIndex,
            in MovingPlatformPose currentPose,
            ref bool sequenceCompleted)
        {
            if (!IsValidRouteIndex(requestedLayerIndex))
                return false;

            MovingPlatformRailLayerRoute route = routes[requestedLayerIndex];
            if (!route.TryGetNextSegmentCursor(currentRouteSegmentCursor, currentRouteDirection, out int nextCursor, out int nextDirection, out bool completedCycle))
            {
                sequenceCompleted = true;
                return false;
            }

            if (!route.TryGetSegment(nextCursor, out MovingPlatformRailLayerRoute.ResolvedSegment segment))
                return false;

            currentRouteSegmentCursor = nextCursor;
            currentRouteDirection = nextDirection;
            sequenceCompleted |= completedCycle;

            StartSegment(segment, currentPose, requestedLayerIndex);
            return true;
        }

        private MovingPlatformPose EvaluateCurrentPose(in MovingPlatformPose fallbackPose)
        {
            if (!activeSegment.IsActive)
                return fallbackPose;

            float normalizedTime = activeSegment.Duration > 0.0f
                ? Mathf.Clamp01(activeSegment.Elapsed / activeSegment.Duration)
                : 1.0f;
            float eased = Ease(activeSegment.EasingMode, normalizedTime);

            if (activeSegment.Kind == MovingPlatformRailLayerRoute.ResolvedStepKind.WaitSignal && !activeSegment.WaitSignalTriggered)
                eased = 0.0f;

            Vector3 position = Vector3.Lerp(activeSegment.StartPose.Position, activeSegment.EndPose.Position, eased);
            Quaternion rotation = Quaternion.Slerp(activeSegment.StartPose.Rotation, activeSegment.EndPose.Rotation, eased);
            Vector3 scale = Vector3.Lerp(activeSegment.StartPose.LocalScale, activeSegment.EndPose.LocalScale, eased);

            return new MovingPlatformPose(position, rotation, scale);
        }

        private void StartSegment(
            in MovingPlatformRailLayerRoute.ResolvedSegment segment,
            in MovingPlatformPose currentPose,
            int routeLayerIndex)
        {
            MovingPlatformPose endPose = ResolveEndPose(segment, currentPose);
            bool waitSignal = segment.Kind == MovingPlatformRailLayerRoute.ResolvedStepKind.WaitSignal;

            activeSegment = new RailSegmentState
            {
                IsActive = true,
                Kind = segment.Kind,
                StartPose = currentPose,
                EndPose = endPose,
                Duration = waitSignal ? 0.01f : Mathf.Max(0.0001f, segment.Duration),
                Elapsed = 0f,
                EasingMode = segment.EasingMode,
                WaitSignalId = segment.WaitSignalId,
                WaitSignalTriggered = !waitSignal,
                RouteLayerIndex = routeLayerIndex,
                RouteSegmentIndex = segment.SegmentIndex,
            };

            if (routeLayerIndex >= 0 && segment.SegmentIndex >= 0)
            {
                pendingTransitionEvents.Add(new MovingPlatformRailTransitionEvent(
                    routeLayerIndex,
                    segment.SegmentIndex,
                    MovingPlatformRailTransitionEventKind.SegmentEnter));
            }

            segmentPaused = false;
        }

        private MovingPlatformPose ResolveEndPose(in MovingPlatformRailLayerRoute.ResolvedSegment segment, in MovingPlatformPose currentPose)
        {
            switch (segment.Kind)
            {
                case MovingPlatformRailLayerRoute.ResolvedStepKind.Move:
                {
                    MovingPlatformBasePose poseBase = hasRailReferencePose
                        ? railReferencePose
                        : new MovingPlatformBasePose(currentPose.Position, currentPose.Rotation, currentPose.LocalScale);
                    Vector3 targetPosition = graph.GetWorldPosition(poseBase, segment.ToNodeIndex);
                    return new MovingPlatformPose(targetPosition, currentPose.Rotation, currentPose.LocalScale);
                }

                case MovingPlatformRailLayerRoute.ResolvedStepKind.Rotate:
                {
                    Quaternion targetRotation = currentPose.Rotation * Quaternion.Euler(segment.RotationEulerDelta);
                    if (!segment.UsePivotOffset)
                        return new MovingPlatformPose(currentPose.Position, targetRotation, currentPose.LocalScale);

                    Vector3 pivotOffsetLocal = ResolveReactiveVector3(segment.PivotLocalOffset);
                    Vector3 pivotWorld = currentPose.Position + currentPose.Rotation * Vector3.Scale(pivotOffsetLocal, currentPose.LocalScale);
                    Vector3 rotatedPivotVector = targetRotation * Vector3.Scale(pivotOffsetLocal, currentPose.LocalScale);
                    Vector3 targetPosition = pivotWorld - rotatedPivotVector;
                    return new MovingPlatformPose(targetPosition, targetRotation, currentPose.LocalScale);
                }

                case MovingPlatformRailLayerRoute.ResolvedStepKind.Scale:
                {
                    Vector3 targetScale = segment.ScaleMode == MovingPlatformScaleMode.Multiply
                        ? Vector3.Scale(currentPose.LocalScale, segment.ScaleTarget)
                        : segment.ScaleTarget;

                    if (!segment.UsePivotOffset)
                        return new MovingPlatformPose(currentPose.Position, currentPose.Rotation, targetScale);

                    Vector3 pivotOffsetLocal = ResolveReactiveVector3(segment.PivotLocalOffset);
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
            if (reactiveVector.SourceKind == ReactiveVector3SourceKind.Literal)
                return reactiveVector.Literal;

            return reactiveVector.FallbackValue;
        }

        private void CompleteActiveSegment()
        {
            if (!activeSegment.IsActive)
                return;

            bool shouldEmitExit = activeSegment.RouteLayerIndex >= 0 && activeSegment.RouteSegmentIndex >= 0;
            int routeLayerIndex = activeSegment.RouteLayerIndex;
            int routeSegmentIndex = activeSegment.RouteSegmentIndex;
            activeSegment = default;

            if (shouldEmitExit)
            {
                pendingTransitionEvents.Add(new MovingPlatformRailTransitionEvent(
                    routeLayerIndex,
                    routeSegmentIndex,
                    MovingPlatformRailTransitionEventKind.SegmentExit));
            }
        }

        private void AbortActiveSegment(bool emitExit)
        {
            if (!activeSegment.IsActive)
                return;

            if (emitExit && activeSegment.RouteLayerIndex >= 0 && activeSegment.RouteSegmentIndex >= 0)
            {
                pendingTransitionEvents.Add(new MovingPlatformRailTransitionEvent(
                    activeSegment.RouteLayerIndex,
                    activeSegment.RouteSegmentIndex,
                    MovingPlatformRailTransitionEventKind.SegmentExit));
            }

            activeSegment = default;
            segmentPaused = false;
        }

        private bool IsValidRouteIndex(int routeLayerIndex)
        {
            return routeLayerIndex >= 0 && routeLayerIndex < routes.Length && routes[routeLayerIndex] != null && routes[routeLayerIndex].IsValid;
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

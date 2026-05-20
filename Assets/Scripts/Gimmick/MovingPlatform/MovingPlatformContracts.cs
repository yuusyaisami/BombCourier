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
        [SerializeField] private string nodeName = "Node";
        [SerializeField] private ReactiveVector3 localPosition = default;

        public string NodePath => MovingPlatformRailIdUtility.Normalize(nodePath);
        public string NodeName => !string.IsNullOrWhiteSpace(nodeName)
            ? nodeName
            : !string.IsNullOrWhiteSpace(NodePath)
                ? NodePath
                : "Node";
        public ReactiveVector3 LocalPosition => localPosition;

        internal string RawNodePath => nodePath;
        internal string RawNodeName => nodeName;

        internal bool NeedsGeneratedNodePath()
        {
            return string.IsNullOrWhiteSpace(NodePath) || string.Equals(NodePath, DefaultNodeLabelPrefix, StringComparison.Ordinal);
        }

        internal bool NeedsGeneratedNodeName()
        {
            return string.IsNullOrWhiteSpace(nodeName) || string.Equals(nodeName, DefaultNodeLabelPrefix, StringComparison.Ordinal);
        }

        internal void SetNodePath(string value)
        {
            nodePath = MovingPlatformRailIdUtility.Normalize(value);
        }

        internal void SetNodeName(string value)
        {
            nodeName = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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
            public readonly string Name;
            public readonly ReactiveVector3 LocalPosition;

            public NodeData(string path, string name, ReactiveVector3 localPosition)
            {
                Path = path;
                Name = name;
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
                nodeBuffer.Add(new NodeData(nodePath, railNode.NodeName, railNode.LocalPosition));
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
        internal readonly struct ResolvedSegment
        {
            private readonly MovingPlatformRailRouteSegment sourceSegment;

            public readonly int SegmentIndex;
            public readonly int FromNodeIndex;
            public readonly int ToNodeIndex;
            public readonly float Duration;
            public readonly MovingPlatformEasingMode EasingMode;

            public ResolvedSegment(
                int segmentIndex,
                int fromNodeIndex,
                int toNodeIndex,
                float duration,
                MovingPlatformEasingMode easingMode,
                MovingPlatformRailRouteSegment sourceSegment)
            {
                SegmentIndex = segmentIndex;
                FromNodeIndex = fromNodeIndex;
                ToNodeIndex = toNodeIndex;
                Duration = Mathf.Max(0.01f, duration);
                EasingMode = easingMode;
                this.sourceSegment = sourceSegment;
            }

            public void ExecuteEnter(in WiringActionContext context)
            {
                sourceSegment?.ExecuteEnter(context);
            }

            public void ExecuteExit(in WiringActionContext context)
            {
                sourceSegment?.ExecuteExit(context);
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

            var resolvedNodeIndices = new List<int>(layer.RouteSegmentCount + 1) { currentNodeIndex };
            var resolvedSegments = new List<ResolvedSegment>(layer.RouteSegmentCount);

            for (int i = 0; i < layer.RouteSegmentCount; i++)
            {
                if (!layer.TryGetRouteSegment(i, out MovingPlatformRailRouteSegment segment) ||
                    string.IsNullOrWhiteSpace(segment.TargetNodePath) ||
                    !graph.TryGetNodeIndex(segment.TargetNodePath, out int targetNodeIndex) ||
                    !graph.TryGetEdge(currentNodeIndex, targetNodeIndex, out MovingPlatformRailGraph.EdgeData edge))
                {
                    nodeIndices = Array.Empty<int>();
                    segments = Array.Empty<ResolvedSegment>();
                    return;
                }

                float duration = segment.OverrideConnectionTiming ? segment.Duration : edge.Duration;
                MovingPlatformEasingMode easingMode = segment.OverrideConnectionTiming ? segment.EasingMode : edge.EasingMode;
                resolvedSegments.Add(new ResolvedSegment(i, currentNodeIndex, targetNodeIndex, duration, easingMode, segment));
                resolvedNodeIndices.Add(targetNodeIndex);
                currentNodeIndex = targetNodeIndex;
            }

            nodeIndices = resolvedNodeIndices.ToArray();
            segments = resolvedSegments.ToArray();
        }

        public MovingPlatformPlaybackMode PlaybackMode { get; }
        public bool IsValid => nodeIndices != null && nodeIndices.Length >= 2 && segments != null && segments.Length == nodeIndices.Length - 1;
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
            if (!IsValid || Mathf.Abs(nextCursor - currentCursor) != 1)
            {
                segment = default;
                return false;
            }

            return TryGetSegment(Math.Min(currentCursor, nextCursor), out segment);
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
    }

    internal sealed class MovingPlatformRailController
    {
        private struct RailSegmentState
        {
            public bool IsActive;
            public bool IsRouteSegment;
            public Vector3 StartWorldPosition;
            public Vector3 EndWorldPosition;
            public float Duration;
            public float Elapsed;
            public MovingPlatformEasingMode EasingMode;
            public int EndNodeIndex;
            public int RouteLayerIndex;
            public int RouteSegmentIndex;
        }

        private readonly MovingPlatformRailGraph graph;
        private readonly MovingPlatformRailLayerRoute[] routes;
        private readonly List<int> transferPathNodes = new();
        private readonly List<MovingPlatformRailTransitionEvent> pendingTransitionEvents = new();

        private MovingPlatformRailLocation currentLocation;
        private RailSegmentState activeSegment;
        private int currentRouteLayerIndex = -1;
        private int currentRouteNodeCursor = -1;
        private int currentRouteDirection = 1;
        private int transferTargetLayerIndex = -1;
        private int transferPathCursor;
        private bool hasCurrentLocation;
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
            currentRouteLayerIndex = -1;
            currentRouteNodeCursor = -1;
            currentRouteDirection = 1;
            segmentPaused = false;
            hasCurrentLocation = graph != null && graph.TryFindNearestLocation(basePose, currentWorldPosition, out currentLocation);
            pendingTransitionEvents.Clear();
            ClearTransfer();
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
            out bool sequenceCompleted)
        {
            sequenceCompleted = false;
            EnsureCurrentLocation(basePose, currentWorldPosition);

            bool hasRequestedRoute = IsValidRouteIndex(requestedLayerIndex);
            if (!hasRequestedRoute)
            {
                if (activeSegment.IsActive)
                    segmentPaused = true;

                return EvaluateCurrentPose(basePose, currentWorldPosition);
            }

            if (segmentPaused && activeSegment.IsActive)
            {
                if (currentRouteLayerIndex == requestedLayerIndex || transferTargetLayerIndex == requestedLayerIndex)
                {
                    segmentPaused = false;
                }
                else
                {
                    currentLocation = GetCurrentLocationFromState(basePose, currentWorldPosition);
                    hasCurrentLocation = currentLocation.IsValid;
                    AbortActiveSegment(true);
                    ClearTransfer();
                }
            }

            float remainingTime = Mathf.Max(0f, deltaTime);
            int guard = 0;

            while (remainingTime > 0f && guard++ < 128)
            {
                if (!activeSegment.IsActive)
                {
                    if (!TryBeginMovement(requestedLayerIndex, basePose, currentWorldPosition, ref sequenceCompleted))
                        break;
                }

                if (!activeSegment.IsActive)
                    break;

                float remainingSegmentTime = Mathf.Max(0f, activeSegment.Duration - activeSegment.Elapsed);
                if (remainingTime < remainingSegmentTime)
                {
                    activeSegment.Elapsed += remainingTime;
                    remainingTime = 0f;
                    break;
                }

                activeSegment.Elapsed = activeSegment.Duration;
                remainingTime -= remainingSegmentTime;
                CompleteActiveSegment(basePose);
            }

            return EvaluateCurrentPose(basePose, currentWorldPosition);
        }

        private bool TryBeginMovement(
            int requestedLayerIndex,
            in MovingPlatformBasePose basePose,
            Vector3 currentWorldPosition,
            ref bool sequenceCompleted)
        {
            if (!IsValidRouteIndex(requestedLayerIndex))
                return false;

            currentLocation = GetCurrentLocationFromState(basePose, currentWorldPosition);
            hasCurrentLocation = currentLocation.IsValid;
            if (!currentLocation.IsValid)
                return false;

            if (transferTargetLayerIndex >= 0 && transferTargetLayerIndex != requestedLayerIndex)
                ClearTransfer();

            if (transferPathNodes.Count > 0 && TryStartNextTransferSegment(requestedLayerIndex, basePose, ref sequenceCompleted))
                return true;

            if (currentLocation.IsOnNode)
            {
                if (routes[requestedLayerIndex].ContainsNode(currentLocation.NodeIndex))
                {
                    if (TryAttachRouteAtCurrentNode(requestedLayerIndex))
                        return TryStartNextCurrentRouteSegment(basePose, ref sequenceCompleted);
                }

                if (currentRouteLayerIndex >= 0 && currentRouteLayerIndex != requestedLayerIndex)
                {
                    if (TryStartNextCurrentRouteSegment(basePose, ref sequenceCompleted))
                        return true;
                }

                if (currentRouteLayerIndex == requestedLayerIndex)
                    return TryStartNextCurrentRouteSegment(basePose, ref sequenceCompleted);
            }

            return TryPlanTransferToRoute(requestedLayerIndex, basePose);
        }

        private bool TryStartNextCurrentRouteSegment(in MovingPlatformBasePose basePose, ref bool sequenceCompleted)
        {
            if (!currentLocation.IsOnNode || !IsValidRouteIndex(currentRouteLayerIndex))
                return false;

            MovingPlatformRailLayerRoute route = routes[currentRouteLayerIndex];
            if (currentRouteNodeCursor < 0 || currentRouteNodeCursor >= route.NodeCount)
            {
                if (!route.TryFindNodeCursor(currentLocation.NodeIndex, currentRouteDirection, out currentRouteNodeCursor, out currentRouteDirection))
                    return false;
            }

            if (!route.TryGetNextCursor(currentRouteNodeCursor, currentRouteDirection, out int nextCursor, out int nextDirection, out bool completedCycle))
            {
                sequenceCompleted = true;
                return false;
            }

            int nextNodeIndex = route.GetNodeIndexAt(nextCursor);
            if (!route.TryGetSegmentForTransition(currentRouteNodeCursor, nextCursor, out MovingPlatformRailLayerRoute.ResolvedSegment segment))
                return false;

            currentRouteNodeCursor = nextCursor;
            currentRouteDirection = nextDirection;
            sequenceCompleted |= completedCycle;
            StartSegment(
                currentLocation.WorldPosition,
                graph.GetWorldPosition(basePose, nextNodeIndex),
                segment.Duration,
                segment.EasingMode,
                nextNodeIndex,
                true,
                currentRouteLayerIndex,
                segment.SegmentIndex);
            return true;
        }

        private bool TryPlanTransferToRoute(int requestedLayerIndex, in MovingPlatformBasePose basePose)
        {
            if (!IsValidRouteIndex(requestedLayerIndex))
                return false;

            ClearTransfer();

            if (currentLocation.IsOnNode)
            {
                if (!graph.TryFindShortestPathToAny(basePose, currentLocation.NodeIndex, routes[requestedLayerIndex].NodeIndices, out List<int> nodePath, out _))
                    return false;

                if (nodePath.Count <= 1)
                {
                    if (!TryAttachRouteAtCurrentNode(requestedLayerIndex))
                        return false;

                    bool unused = false;
                    return TryStartNextCurrentRouteSegment(basePose, ref unused);
                }

                transferTargetLayerIndex = requestedLayerIndex;
                transferPathNodes.AddRange(nodePath);
                transferPathCursor = 0;
                bool unusedSequenceCompleted = false;
                return TryStartNextTransferSegment(requestedLayerIndex, basePose, ref unusedSequenceCompleted);
            }

            if (!graph.TryGetAnyEdge(currentLocation.FromNodeIndex, currentLocation.ToNodeIndex, out MovingPlatformRailGraph.EdgeData edgeInfo))
                return false;

            bool hasFromPath = graph.TryFindShortestPathToAny(
                basePose,
                currentLocation.FromNodeIndex,
                routes[requestedLayerIndex].NodeIndices,
                out List<int> fromPath,
                out float fromPathCost);
            bool hasToPath = graph.TryFindShortestPathToAny(
                basePose,
                currentLocation.ToNodeIndex,
                routes[requestedLayerIndex].NodeIndices,
                out List<int> toPath,
                out float toPathCost);
            if (!hasFromPath && !hasToPath)
                return false;

            float fromTotalCost = hasFromPath
                ? Vector3.Distance(currentLocation.WorldPosition, graph.GetWorldPosition(basePose, currentLocation.FromNodeIndex)) + fromPathCost
                : float.MaxValue;
            float toTotalCost = hasToPath
                ? Vector3.Distance(currentLocation.WorldPosition, graph.GetWorldPosition(basePose, currentLocation.ToNodeIndex)) + toPathCost
                : float.MaxValue;

            int chosenStartNodeIndex;
            List<int> chosenPath;
            float partialNormalized;
            if (fromTotalCost <= toTotalCost)
            {
                chosenStartNodeIndex = currentLocation.FromNodeIndex;
                chosenPath = fromPath;
                partialNormalized = currentLocation.Normalized;
            }
            else
            {
                chosenStartNodeIndex = currentLocation.ToNodeIndex;
                chosenPath = toPath;
                partialNormalized = 1f - currentLocation.Normalized;
            }

            if (chosenPath == null || chosenPath.Count == 0)
                return false;

            transferTargetLayerIndex = requestedLayerIndex;
            transferPathNodes.AddRange(chosenPath);
            transferPathCursor = 0;

            float partialDuration = Mathf.Max(0f, edgeInfo.Duration * Mathf.Clamp01(partialNormalized));
            Vector3 endWorldPosition = graph.GetWorldPosition(basePose, chosenStartNodeIndex);
            if (partialDuration > 0.0001f && (currentLocation.WorldPosition - endWorldPosition).sqrMagnitude > 0.000001f)
            {
                currentRouteLayerIndex = -1;
                currentRouteNodeCursor = -1;
                StartSegment(currentLocation.WorldPosition, endWorldPosition, partialDuration, edgeInfo.EasingMode, chosenStartNodeIndex);
                return true;
            }

            currentLocation = MovingPlatformRailLocation.AtNode(chosenStartNodeIndex, endWorldPosition);
            hasCurrentLocation = true;
            bool unusedTransferCompleted = false;
            return TryStartNextTransferSegment(requestedLayerIndex, basePose, ref unusedTransferCompleted);
        }

        private bool TryStartNextTransferSegment(int requestedLayerIndex, in MovingPlatformBasePose basePose, ref bool sequenceCompleted)
        {
            if (transferPathNodes.Count == 0 || !currentLocation.IsOnNode || transferTargetLayerIndex != requestedLayerIndex)
                return false;

            if (transferPathCursor >= transferPathNodes.Count - 1)
            {
                int targetLayerIndex = transferTargetLayerIndex;
                ClearTransfer();
                if (!TryAttachRouteAtCurrentNode(targetLayerIndex))
                    return false;

                return TryStartNextCurrentRouteSegment(basePose, ref sequenceCompleted);
            }

            int currentNodeIndex = transferPathNodes[transferPathCursor];
            int nextNodeIndex = transferPathNodes[transferPathCursor + 1];
            if (currentNodeIndex != currentLocation.NodeIndex || !graph.TryGetEdge(currentNodeIndex, nextNodeIndex, out MovingPlatformRailGraph.EdgeData edge))
            {
                ClearTransfer();
                return false;
            }

            transferPathCursor++;
            currentRouteLayerIndex = -1;
            currentRouteNodeCursor = -1;
            StartSegment(currentLocation.WorldPosition, graph.GetWorldPosition(basePose, nextNodeIndex), edge.Duration, edge.EasingMode, nextNodeIndex);
            return true;
        }

        private bool TryAttachRouteAtCurrentNode(int routeLayerIndex)
        {
            if (!currentLocation.IsOnNode || !IsValidRouteIndex(routeLayerIndex))
                return false;

            if (!routes[routeLayerIndex].TryFindNodeCursor(currentLocation.NodeIndex, currentRouteDirection, out int routeNodeCursor, out int routeDirection))
                return false;

            currentRouteLayerIndex = routeLayerIndex;
            currentRouteNodeCursor = routeNodeCursor;
            currentRouteDirection = routeDirection;
            return true;
        }

        private MovingPlatformPose EvaluateCurrentPose(in MovingPlatformBasePose basePose, Vector3 fallbackWorldPosition)
        {
            Vector3 worldPosition;
            if (activeSegment.IsActive)
            {
                float normalizedTime = activeSegment.Duration > 0.0f
                    ? Mathf.Clamp01(activeSegment.Elapsed / activeSegment.Duration)
                    : 1.0f;
                worldPosition = Vector3.Lerp(
                    activeSegment.StartWorldPosition,
                    activeSegment.EndWorldPosition,
                    Ease(activeSegment.EasingMode, normalizedTime));
            }
            else if (hasCurrentLocation && currentLocation.IsValid)
            {
                worldPosition = currentLocation.WorldPosition;
            }
            else
            {
                worldPosition = fallbackWorldPosition;
            }

            return new MovingPlatformPose(worldPosition, basePose.Rotation, basePose.LocalScale);
        }

        private MovingPlatformRailLocation GetCurrentLocationFromState(in MovingPlatformBasePose basePose, Vector3 fallbackWorldPosition)
        {
            if (activeSegment.IsActive)
            {
                float normalizedTime = activeSegment.Duration > 0.0f
                    ? Mathf.Clamp01(activeSegment.Elapsed / activeSegment.Duration)
                    : 1.0f;
                Vector3 worldPosition = Vector3.Lerp(
                    activeSegment.StartWorldPosition,
                    activeSegment.EndWorldPosition,
                    Ease(activeSegment.EasingMode, normalizedTime));

                if (!graph.TryFindNearestLocation(basePose, worldPosition, out MovingPlatformRailLocation segmentLocation))
                    return default;

                return segmentLocation;
            }

            if (hasCurrentLocation && currentLocation.IsValid)
                return currentLocation;

            return graph != null && graph.TryFindNearestLocation(basePose, fallbackWorldPosition, out MovingPlatformRailLocation nearestLocation)
                ? nearestLocation
                : default;
        }

        private void EnsureCurrentLocation(in MovingPlatformBasePose basePose, Vector3 currentWorldPosition)
        {
            if (activeSegment.IsActive || (hasCurrentLocation && currentLocation.IsValid) || graph == null)
                return;

            hasCurrentLocation = graph.TryFindNearestLocation(basePose, currentWorldPosition, out currentLocation);
        }

        private void StartSegment(
            Vector3 startWorldPosition,
            Vector3 endWorldPosition,
            float duration,
            MovingPlatformEasingMode easingMode,
            int endNodeIndex,
            bool isRouteSegment = false,
            int routeLayerIndex = -1,
            int routeSegmentIndex = -1)
        {
            activeSegment = new RailSegmentState
            {
                IsActive = true,
                IsRouteSegment = isRouteSegment,
                StartWorldPosition = startWorldPosition,
                EndWorldPosition = endWorldPosition,
                Duration = Mathf.Max(0.0001f, duration),
                Elapsed = 0f,
                EasingMode = easingMode,
                EndNodeIndex = endNodeIndex,
                RouteLayerIndex = routeLayerIndex,
                RouteSegmentIndex = routeSegmentIndex,
            };

            if (isRouteSegment && routeLayerIndex >= 0 && routeSegmentIndex >= 0)
            {
                pendingTransitionEvents.Add(new MovingPlatformRailTransitionEvent(
                    routeLayerIndex,
                    routeSegmentIndex,
                    MovingPlatformRailTransitionEventKind.SegmentEnter));
            }

            segmentPaused = false;
        }

        private void CompleteActiveSegment(in MovingPlatformBasePose basePose)
        {
            if (!activeSegment.IsActive)
                return;

            int endNodeIndex = activeSegment.EndNodeIndex;
            bool shouldEmitExit = activeSegment.IsRouteSegment && activeSegment.RouteLayerIndex >= 0 && activeSegment.RouteSegmentIndex >= 0;
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

            currentLocation = MovingPlatformRailLocation.AtNode(endNodeIndex, graph.GetWorldPosition(basePose, endNodeIndex));
            hasCurrentLocation = true;
        }

        private void AbortActiveSegment(bool emitExit)
        {
            if (!activeSegment.IsActive)
                return;

            if (emitExit && activeSegment.IsRouteSegment && activeSegment.RouteLayerIndex >= 0 && activeSegment.RouteSegmentIndex >= 0)
            {
                pendingTransitionEvents.Add(new MovingPlatformRailTransitionEvent(
                    activeSegment.RouteLayerIndex,
                    activeSegment.RouteSegmentIndex,
                    MovingPlatformRailTransitionEventKind.SegmentExit));
            }

            activeSegment = default;
            segmentPaused = false;
        }

        private void ClearTransfer()
        {
            transferPathNodes.Clear();
            transferTargetLayerIndex = -1;
            transferPathCursor = 0;
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

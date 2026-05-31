using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.Gimmick.MovingPlatform
{
    internal readonly struct MovingPlatformTreeMigrationResult
    {
        public readonly bool Success;
        public readonly MovingPlatformTreeAuthoring TreeAuthoring;
        public readonly string FailureReason;

        public MovingPlatformTreeMigrationResult(bool success, MovingPlatformTreeAuthoring treeAuthoring, string failureReason)
        {
            Success = success;
            TreeAuthoring = treeAuthoring;
            FailureReason = string.IsNullOrWhiteSpace(failureReason) ? string.Empty : failureReason.Trim();
        }
    }

    internal static class MovingPlatformTreeMigration
    {
        public static MovingPlatformTreeMigrationResult TryMigrate(
            IReadOnlyList<MovingPlatformRailNode> legacyRailNodes,
            IReadOnlyList<MovingPlatformRailConnection> legacyRailConnections,
            IReadOnlyList<MovingPlatformLayer> legacyLayers)
        {
            if (legacyRailNodes == null || legacyRailNodes.Count == 0)
                return new MovingPlatformTreeMigrationResult(false, null, "Legacy railNodes are empty.");

            if (legacyLayers == null || legacyLayers.Count == 0)
                return new MovingPlatformTreeMigrationResult(false, null, "Legacy layers are empty.");

            var tree = new MovingPlatformTreeAuthoring();
            var railById = new Dictionary<string, MovingPlatformRailNodeAuthoring>(StringComparer.Ordinal);

            for (int i = 0; i < legacyRailNodes.Count; i++)
            {
                MovingPlatformRailNode legacyNode = legacyRailNodes[i];
                if (legacyNode == null || string.IsNullOrWhiteSpace(legacyNode.NodePath))
                    return new MovingPlatformTreeMigrationResult(false, null, $"Legacy rail node[{i}] is missing a nodePath.");

                if (railById.ContainsKey(legacyNode.NodePath))
                    return new MovingPlatformTreeMigrationResult(false, null, $"Legacy rail node '{legacyNode.NodePath}' is duplicated.");

                var authoringNode = new MovingPlatformRailNodeAuthoring();
                authoringNode.SetStableId(legacyNode.NodePath);
                authoringNode.SetLabel(legacyNode.NodePath);
                authoringNode.SetLocalPosition(legacyNode.LocalPosition);
                railById.Add(legacyNode.NodePath, authoringNode);
                tree.MutableRailNodes.Add(authoringNode);
            }

            string rootRailId;
            if (legacyRailConnections != null && legacyRailConnections.Count > 0)
            {
                string connectionFailure = BuildRailTreeFromConnections(legacyRailConnections, legacyLayers, railById, out rootRailId);
                if (!string.IsNullOrWhiteSpace(connectionFailure))
                    return new MovingPlatformTreeMigrationResult(false, null, connectionFailure);
            }
            else
            {
                string inferenceFailure = BuildRailTreeFromRoutes(legacyLayers, railById, out rootRailId);
                if (!string.IsNullOrWhiteSpace(inferenceFailure))
                    return new MovingPlatformTreeMigrationResult(false, null, inferenceFailure);
            }

            tree.SetRootRailNodeId(rootRailId);

            for (int i = 0; i < legacyLayers.Count; i++)
            {
                MovingPlatformLayer legacyLayer = legacyLayers[i];
                if (legacyLayer == null)
                    return new MovingPlatformTreeMigrationResult(false, null, $"Legacy layer[{i}] is null.");

                if (string.IsNullOrWhiteSpace(legacyLayer.StartNodePath))
                    return new MovingPlatformTreeMigrationResult(false, null, $"Legacy layer '{legacyLayer.LayerName}' is missing a start node.");

                if (!railById.ContainsKey(legacyLayer.StartNodePath))
                    return new MovingPlatformTreeMigrationResult(false, null, $"Legacy layer '{legacyLayer.LayerName}' references missing start node '{legacyLayer.StartNodePath}'.");

                var selector = new MovingPlatformSelectorNodeAuthoring();
                selector.SetStableId($"selector.{i + 1}");
                selector.SetLabel(string.IsNullOrWhiteSpace(legacyLayer.LayerName) ? $"Selector {i + 1}" : legacyLayer.LayerName);
                selector.SetAnchorRailNodeId(legacyLayer.StartNodePath);
                selector.Rule.CopyFrom(legacyLayer);

                IReadOnlyList<MovingPlatformLayerSegment> segments = legacyLayer.Segments;
                for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
                {
                    MovingPlatformLayerSegment legacySegment = segments[segmentIndex];
                    if (legacySegment == null)
                        return new MovingPlatformTreeMigrationResult(false, null, $"Legacy layer '{legacyLayer.LayerName}' contains a null segment at index {segmentIndex}.");

                    MovingPlatformControlNodeAuthoring migratedStep = MigrateStep(legacyLayer, legacySegment, segmentIndex);
                    if (migratedStep == null)
                        return new MovingPlatformTreeMigrationResult(false, null, $"Legacy layer '{legacyLayer.LayerName}' contains unsupported segment '{legacySegment.GetType().Name}'.");

                    selector.MutableOrderedChildren.Add(migratedStep);
                }

                tree.MutableSelectors.Add(selector);
            }

            return new MovingPlatformTreeMigrationResult(true, tree, string.Empty);
        }

        private static MovingPlatformControlNodeAuthoring MigrateStep(
            MovingPlatformLayer legacyLayer,
            MovingPlatformLayerSegment legacySegment,
            int segmentIndex)
        {
            if (legacySegment is MovingPlatformRailRouteSegment moveSegment)
            {
                var step = new MovingPlatformMoveNodeAuthoring();
                step.SetStableId($"step.move.{segmentIndex + 1}");
                step.SetLabel(string.IsNullOrWhiteSpace(moveSegment.TargetNodePath) ? "Move" : moveSegment.TargetNodePath);
                step.SetTargetRailNodeId(moveSegment.TargetNodePath);
                step.SetTimingOverride(
                    moveSegment.OverrideConnectionTiming,
                    moveSegment.TimingControl,
                    moveSegment.Duration,
                    moveSegment.Speed,
                    moveSegment.EasingMode);
                return step;
            }

            if (legacySegment is MovingPlatformWaitSegment waitSegment)
            {
                var step = new MovingPlatformWaitNodeAuthoring();
                step.SetStableId($"step.wait.{segmentIndex + 1}");
                step.SetLabel(waitSegment.SegmentName);
                step.CopyFrom(waitSegment);
                return step;
            }

            if (legacySegment is MovingPlatformInlineActionSegment inlineActionSegment)
            {
                var step = new MovingPlatformInlineActionNodeAuthoring();
                step.SetStableId($"step.inline.{segmentIndex + 1}");
                step.SetLabel(inlineActionSegment.SegmentName);
                step.SetActions(inlineActionSegment.Actions);
                return step;
            }

            if (legacySegment is MovingPlatformRotationSegment rotationSegment)
            {
                var step = new MovingPlatformRotationNodeAuthoring();
                step.SetStableId($"step.rotate.{segmentIndex + 1}");
                step.SetLabel(rotationSegment.SegmentName);
                step.CopyFrom(rotationSegment);
                return step;
            }

            if (legacySegment is MovingPlatformScaleSegment scaleSegment)
            {
                var step = new MovingPlatformScaleNodeAuthoring();
                step.SetStableId($"step.scale.{segmentIndex + 1}");
                step.SetLabel(scaleSegment.SegmentName);
                step.CopyFrom(scaleSegment);
                return step;
            }

            _ = legacyLayer;
            return null;
        }

        private static string BuildRailTreeFromConnections(
            IReadOnlyList<MovingPlatformRailConnection> legacyRailConnections,
            IReadOnlyList<MovingPlatformLayer> legacyLayers,
            Dictionary<string, MovingPlatformRailNodeAuthoring> railById,
            out string rootRailId)
        {
            rootRailId = string.Empty;

            var adjacency = new Dictionary<string, List<(string NeighborId, MovingPlatformRailConnection Connection)>>(StringComparer.Ordinal);
            var undirectedEdges = new HashSet<string>(StringComparer.Ordinal);
            foreach (string railId in railById.Keys)
                adjacency[railId] = new List<(string, MovingPlatformRailConnection)>();

            for (int i = 0; i < legacyRailConnections.Count; i++)
            {
                MovingPlatformRailConnection connection = legacyRailConnections[i];
                if (connection == null || string.IsNullOrWhiteSpace(connection.FromNodePath) || string.IsNullOrWhiteSpace(connection.ToNodePath))
                    return $"Legacy rail connection[{i}] is incomplete.";

                if (!railById.ContainsKey(connection.FromNodePath) || !railById.ContainsKey(connection.ToNodePath))
                    return $"Legacy rail connection '{connection.FromNodePath}->{connection.ToNodePath}' references a missing node.";

                string edgeKey = BuildUndirectedEdgeKey(connection.FromNodePath, connection.ToNodePath);
                if (!undirectedEdges.Add(edgeKey))
                    return $"Legacy rail connection '{connection.FromNodePath}<->{connection.ToNodePath}' is duplicated or cyclic.";

                adjacency[connection.FromNodePath].Add((connection.ToNodePath, connection));
                adjacency[connection.ToNodePath].Add((connection.FromNodePath, connection));
            }

            if (undirectedEdges.Count != railById.Count - 1)
                return "Legacy railConnections do not form a strict tree. Expected nodeCount - 1 unique edges.";

            rootRailId = SelectRootRailId(adjacency, legacyLayers);
            if (string.IsNullOrWhiteSpace(rootRailId))
                return "Failed to determine a root rail node from legacy connections.";

            var visited = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>();
            queue.Enqueue(rootRailId);
            visited.Add(rootRailId);
            railById[rootRailId].SetParentRailNodeId(string.Empty);

            while (queue.Count > 0)
            {
                string currentId = queue.Dequeue();
                List<(string NeighborId, MovingPlatformRailConnection Connection)> neighbors = adjacency[currentId];
                for (int i = 0; i < neighbors.Count; i++)
                {
                    string nextId = neighbors[i].NeighborId;
                    if (!visited.Add(nextId))
                        continue;

                    MovingPlatformRailConnection connection = neighbors[i].Connection;
                    railById[nextId].SetParentRailNodeId(currentId);
                    railById[nextId].SetIncomingTiming(
                        overrideTiming: true,
                        timingControl: MovingPlatformTimingControl.Duration,
                        duration: connection.Duration,
                        speed: 1.0f,
                        easingMode: connection.EasingMode);
                    queue.Enqueue(nextId);
                }
            }

            if (visited.Count != railById.Count)
                return "Legacy railConnections are disconnected. Every rail node must be reachable from the inferred root.";

            return string.Empty;
        }

        private static string SelectRootRailId(
            Dictionary<string, List<(string NeighborId, MovingPlatformRailConnection Connection)>> adjacency,
            IReadOnlyList<MovingPlatformLayer> legacyLayers)
        {
            var startUsage = new Dictionary<string, int>(StringComparer.Ordinal);
            if (legacyLayers != null)
            {
                for (int i = 0; i < legacyLayers.Count; i++)
                {
                    MovingPlatformLayer layer = legacyLayers[i];
                    if (layer == null || string.IsNullOrWhiteSpace(layer.StartNodePath))
                        continue;

                    startUsage.TryGetValue(layer.StartNodePath, out int count);
                    startUsage[layer.StartNodePath] = count + 1;
                }
            }

            string bestId = string.Empty;
            int bestDegree = int.MinValue;
            int bestStartHits = int.MinValue;
            foreach (KeyValuePair<string, List<(string NeighborId, MovingPlatformRailConnection Connection)>> pair in adjacency)
            {
                int degree = pair.Value.Count;
                startUsage.TryGetValue(pair.Key, out int startHits);
                if (degree > bestDegree ||
                    (degree == bestDegree && startHits > bestStartHits) ||
                    (degree == bestDegree && startHits == bestStartHits && string.CompareOrdinal(pair.Key, bestId) < 0))
                {
                    bestId = pair.Key;
                    bestDegree = degree;
                    bestStartHits = startHits;
                }
            }

            return bestId;
        }

        private static string BuildRailTreeFromRoutes(
            IReadOnlyList<MovingPlatformLayer> legacyLayers,
            Dictionary<string, MovingPlatformRailNodeAuthoring> railById,
            out string rootRailId)
        {
            rootRailId = string.Empty;
            var parentById = new Dictionary<string, string>(StringComparer.Ordinal);

            for (int layerIndex = 0; layerIndex < legacyLayers.Count; layerIndex++)
            {
                MovingPlatformLayer layer = legacyLayers[layerIndex];
                if (layer == null || string.IsNullOrWhiteSpace(layer.StartNodePath))
                    return $"Legacy layer[{layerIndex}] is missing a start node.";

                if (!railById.ContainsKey(layer.StartNodePath))
                    return $"Legacy layer '{layer.LayerName}' references missing rail '{layer.StartNodePath}'.";

                if (string.IsNullOrWhiteSpace(rootRailId))
                    rootRailId = layer.StartNodePath;

                if (!parentById.TryGetValue(rootRailId, out string rootParentId) || string.IsNullOrWhiteSpace(rootParentId))
                    railById[rootRailId].SetParentRailNodeId(string.Empty);

                if (!parentById.ContainsKey(layer.StartNodePath))
                {
                    if (!string.Equals(layer.StartNodePath, rootRailId, StringComparison.Ordinal))
                        return $"Legacy routes are disconnected. Start node '{layer.StartNodePath}' cannot be inferred without explicit railConnections.";

                    parentById[layer.StartNodePath] = string.Empty;
                }

                string currentId = layer.StartNodePath;
                IReadOnlyList<MovingPlatformLayerSegment> segments = layer.Segments;
                for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
                {
                    if (segments[segmentIndex] is not MovingPlatformRailRouteSegment moveSegment)
                        continue;

                    string targetId = moveSegment.TargetNodePath;
                    if (string.IsNullOrWhiteSpace(targetId) || !railById.ContainsKey(targetId))
                        return $"Legacy layer '{layer.LayerName}' references missing move target '{targetId}'.";

                    if (!parentById.ContainsKey(targetId))
                    {
                        parentById[targetId] = currentId;
                        railById[targetId].SetParentRailNodeId(currentId);
                        railById[targetId].SetIncomingTiming(
                            moveSegment.OverrideConnectionTiming,
                            moveSegment.OverrideConnectionTiming ? moveSegment.TimingControl : layer.DefaultTimingControl,
                            moveSegment.OverrideConnectionTiming ? moveSegment.Duration : layer.DefaultDuration,
                            moveSegment.OverrideConnectionTiming ? moveSegment.Speed : layer.DefaultSpeed,
                            moveSegment.OverrideConnectionTiming ? moveSegment.EasingMode : layer.DefaultEasingMode);
                        currentId = targetId;
                        continue;
                    }

                    if (IsAncestor(targetId, currentId, parentById) ||
                        IsAncestor(currentId, targetId, parentById) ||
                        string.Equals(parentById[targetId], currentId, StringComparison.Ordinal) ||
                        string.Equals(parentById[currentId], targetId, StringComparison.Ordinal))
                    {
                        currentId = targetId;
                        continue;
                    }

                    return $"Legacy route '{layer.LayerName}' contains non-tree jump '{currentId}->{targetId}' without railConnections.";
                }
            }

            if (string.IsNullOrWhiteSpace(rootRailId))
                return "Failed to infer a root rail node from legacy routes.";

            foreach (KeyValuePair<string, string> pair in parentById)
            {
                if (string.IsNullOrWhiteSpace(pair.Value))
                    railById[pair.Key].SetParentRailNodeId(string.Empty);
            }

            return string.Empty;
        }

        private static bool IsAncestor(string ancestorId, string nodeId, Dictionary<string, string> parentById)
        {
            string cursor = nodeId;
            while (!string.IsNullOrWhiteSpace(cursor) && parentById.TryGetValue(cursor, out string parentId))
            {
                if (string.Equals(parentId, ancestorId, StringComparison.Ordinal))
                    return true;

                cursor = parentId;
            }

            return false;
        }

        private static string BuildUndirectedEdgeKey(string a, string b)
        {
            return string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";
        }
    }
}

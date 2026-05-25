using System.Collections.Generic;
using BC.Gimmick.MovingPlatform;
using UnityEngine;

namespace BC.Editor.Gimmick.MovingPlatformTools
{
    internal sealed class MovingPlatformGizmoPresenter
    {
        private static readonly Color ConnectionShadowColor = new(0.16f, 0.19f, 0.24f, 0.55f);

        private readonly List<MovingPlatformEditorLayerPathData> layerPaths = new();
        private readonly List<MovingPlatformEditorRailConnectionData> railConnections = new();
        private readonly List<MovingPlatformEditorRailNodeData> railNodes = new();

        public void Draw(MovingPlatformMB target)
        {
            if (target == null)
                return;

            if (!target.TryCollectEditorGizmoData(layerPaths, railConnections, railNodes))
                return;

            Color previousColor = Gizmos.color;

            DrawRailConnections();
            DrawRailNodes();
            DrawLayerPaths();

            Gizmos.color = previousColor;
        }

        private void DrawRailConnections()
        {
            for (int i = 0; i < railConnections.Count; i++)
            {
                MovingPlatformEditorRailConnectionData connection = railConnections[i];

                Gizmos.color = ConnectionShadowColor;
                Gizmos.DrawLine(connection.From, connection.To);

                Gizmos.color = connection.Color;
                Gizmos.DrawLine(connection.From, connection.To);
            }
        }

        private void DrawRailNodes()
        {
            for (int i = 0; i < railNodes.Count; i++)
            {
                MovingPlatformEditorRailNodeData railNode = railNodes[i];

                Gizmos.color = railNode.FillColor;
                Gizmos.DrawSphere(railNode.Position, railNode.Radius);

                Gizmos.color = railNode.WireColor;
                Gizmos.DrawWireSphere(railNode.Position, railNode.Radius * 1.15f);
            }
        }

        private void DrawLayerPaths()
        {
            for (int i = 0; i < layerPaths.Count; i++)
            {
                MovingPlatformEditorLayerPathData layerPath = layerPaths[i];
                if (layerPath.Points == null || layerPath.Points.Length == 0)
                    continue;

                Gizmos.color = layerPath.Color;
                for (int pointIndex = 0; pointIndex < layerPath.Points.Length - 1; pointIndex++)
                    Gizmos.DrawLine(layerPath.Points[pointIndex], layerPath.Points[pointIndex + 1]);

                float pointRadius = Mathf.Max(0.01f, layerPath.PointRadius);
                Gizmos.DrawSphere(layerPath.Points[0], pointRadius);

                Gizmos.color = Color.Lerp(layerPath.Color, Color.white, 0.35f);
                for (int pointIndex = 0; pointIndex < layerPath.Points.Length; pointIndex++)
                    Gizmos.DrawWireSphere(layerPath.Points[pointIndex], pointRadius * 0.55f);

                Gizmos.color = layerPath.Color;
                Gizmos.DrawSphere(layerPath.Points[layerPath.Points.Length - 1], pointRadius * 0.75f);
            }
        }
    }
}

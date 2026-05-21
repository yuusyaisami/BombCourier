using System.Collections.Generic;
using BC.Camera;
using BC.Editor.Foundation.Scene;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Camera
{
    internal sealed class CameraPathSequenceGizmoAdapter : GizmoAdapterBase<CameraPathSequenceAuthoringMB>
    {
        private const float PointRadius = 0.18f;
        private const float DirectionLength = 1.0f;

        protected override void DrawGizmos(CameraPathSequenceAuthoringMB target, bool selected)
        {
            IReadOnlyList<CameraPathPointDefinition> points = target.BuildSequence();

            if (points == null || points.Count == 0)
                return;

            Color previousColor = Gizmos.color;
            Gizmos.color = selected ? SceneHandleStyleTokens.SelectedColor : SceneHandleStyleTokens.LineColor;

            Vector3 previousPosition = Vector3.zero;
            bool hasPrevious = false;

            for (int i = 0; i < points.Count; i++)
            {
                CameraPathPointDefinition point = points[i];

                if (!point.TryGetLiteralPosition(out Vector3 position))
                {
                    hasPrevious = false;
                    continue;
                }

                Gizmos.DrawSphere(position, PointRadius);

                if (point.TryGetLiteralRotation(out Quaternion rotation))
                    Gizmos.DrawLine(position, position + (rotation * Vector3.forward * DirectionLength));

                if (hasPrevious)
                    Gizmos.DrawLine(previousPosition, position);

                previousPosition = position;
                hasPrevious = true;
            }

            Gizmos.color = previousColor;
        }
    }

    internal sealed class CameraPathPointGizmoAdapter : GizmoAdapterBase<CameraPathPointAuthoringMB>
    {
        private const float PointRadius = 0.22f;
        private const float DirectionLength = 1.25f;

        protected override void DrawGizmos(CameraPathPointAuthoringMB target, bool selected)
        {
            Color previousColor = Gizmos.color;
            Gizmos.color = selected ? SceneHandleStyleTokens.SelectedColor : SceneHandleStyleTokens.LineColor;
            Gizmos.DrawSphere(target.transform.position, PointRadius);
            Gizmos.DrawLine(target.transform.position, target.transform.position + (target.transform.forward * DirectionLength));
            Gizmos.color = previousColor;
        }
    }

    internal static class CameraPathGizmoDrawers
    {
        private static readonly CameraPathSequenceGizmoAdapter SequenceAdapter = new();
        private static readonly CameraPathPointGizmoAdapter PointAdapter = new();

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Active)]
        private static void DrawSequenceGizmo(CameraPathSequenceAuthoringMB target, GizmoType gizmoType)
        {
            SceneGizmoBridgeUtility.Draw(target, gizmoType, SequenceAdapter);
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Active)]
        private static void DrawPointGizmo(CameraPathPointAuthoringMB target, GizmoType gizmoType)
        {
            SceneGizmoBridgeUtility.Draw(target, gizmoType, PointAdapter);
        }
    }
}
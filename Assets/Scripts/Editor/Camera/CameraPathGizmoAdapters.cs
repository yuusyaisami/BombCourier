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
        private const float CutIndicatorAlpha = 0.38f;
        private const float CutDashLength = 0.75f;
        private const float CutDashGap = 0.45f;

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
                {
                    if (point.TransitionFromPrevious.Kind == CameraPathTransitionKind.Cut)
                        DrawCutDashedIndicator(previousPosition, position);
                    else
                        Gizmos.DrawLine(previousPosition, position);
                }

                previousPosition = position;
                hasPrevious = true;
            }

            Gizmos.color = previousColor;
        }

        private static void DrawCutDashedIndicator(Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;

            if (distance <= 0.0001f)
                return;

            Color original = Gizmos.color;
            Gizmos.color = new Color(original.r, original.g, original.b, original.a * CutIndicatorAlpha);

            float step = Mathf.Max(0.01f, CutDashLength + CutDashGap);
            Vector3 unit = direction / distance;

            for (float d = 0.0f; d < distance; d += step)
            {
                float from = d;
                float to = Mathf.Min(d + CutDashLength, distance);
                Vector3 segmentStart = start + unit * from;
                Vector3 segmentEnd = start + unit * to;
                Gizmos.DrawLine(segmentStart, segmentEnd);
            }

            Gizmos.color = original;
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
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.Camera
{
    public class CameraPathSequenceAuthoringMB : MonoBehaviour, ICameraPathSequenceSource
    {
        [SerializeField] private List<CameraPathPointDefinition> points = new();
        [SerializeField, HideInInspector] private bool positionsStoredAsLocal = true;

        [SerializeField, HideInInspector] private int selectedPointIndex = -1;

        public IReadOnlyList<CameraPathPointDefinition> BuildSequence()
        {
            if (points == null || points.Count == 0)
                return Array.Empty<CameraPathPointDefinition>();

            List<CameraPathPointDefinition> resolvedPoints = new(points.Count);

            for (int i = 0; i < points.Count; i++)
            {
                CameraPathPointDefinition point = points[i];

                if (point.TryGetLiteralPosition(out Vector3 localPosition))
                    resolvedPoints.Add(point.WithLiteralPosition(transform.TransformPoint(localPosition)));
                else
                    resolvedPoints.Add(point);
            }

            return resolvedPoints;
        }

        private void Reset()
        {
            EnsureDefaultPoint();
        }

        private void OnValidate()
        {
            points ??= new List<CameraPathPointDefinition>();
            MigrateLegacyWorldPositionsToLocal();

            if (points.Count == 0)
            {
                selectedPointIndex = -1;
                return;
            }

            selectedPointIndex = Mathf.Clamp(selectedPointIndex, 0, points.Count - 1);
        }

        private void EnsureDefaultPoint()
        {
            points ??= new List<CameraPathPointDefinition>();

            if (points.Count > 0)
                return;

            // 内部保存は local 座標。再生時に world 座標へ変換する。
            points.Add(new CameraPathPointDefinition(
                "Point 1",
                Vector3.zero,
                transform.rotation,
                0.0f,
                CameraPathTransitionSettings.Cut(),
                default,
                null));

            positionsStoredAsLocal = true;
            selectedPointIndex = 0;
        }

        private void MigrateLegacyWorldPositionsToLocal()
        {
            if (positionsStoredAsLocal || points == null || points.Count == 0)
                return;

            for (int i = 0; i < points.Count; i++)
            {
                CameraPathPointDefinition point = points[i];
                if (!point.TryGetLiteralPosition(out Vector3 worldPosition))
                    continue;

                points[i] = point.WithLiteralPosition(transform.InverseTransformPoint(worldPosition));
            }

            positionsStoredAsLocal = true;
        }
    }
}
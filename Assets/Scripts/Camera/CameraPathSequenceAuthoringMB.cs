using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.Camera
{
    public class CameraPathSequenceAuthoringMB : MonoBehaviour, ICameraPathSequenceSource
    {
        [SerializeField] private List<CameraPathPointDefinition> points = new();

        [SerializeField, HideInInspector] private int selectedPointIndex = -1;

        public IReadOnlyList<CameraPathPointDefinition> BuildSequence()
        {
            if (points == null || points.Count == 0)
                return Array.Empty<CameraPathPointDefinition>();

            return points;
        }

        private void Reset()
        {
            EnsureDefaultPoint();
        }

        private void OnValidate()
        {
            points ??= new List<CameraPathPointDefinition>();

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

            // 新規作成時はコンポーネントのTransform位置から最初のカメラポイントを作る。
            points.Add(new CameraPathPointDefinition(
                "Point 1",
                transform.position,
                transform.rotation,
                0.0f,
                CameraPathTransitionSettings.Cut(),
                default,
                null));

            selectedPointIndex = 0;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (points == null || points.Count == 0)
                return;

            Gizmos.color = Color.green;

            Vector3 previousPosition = Vector3.zero;
            bool hasPrevious = false;

            for (int i = 0; i < points.Count; i++)
            {
                CameraPathPointDefinition point = points[i];

                // Position が動的なポイントは空間上に置けないため、可視セグメントは次のリテラル位置から再開されます。
                if (!point.TryGetLiteralPosition(out Vector3 position))
                {
                    hasPrevious = false;
                    continue;
                }

                Gizmos.DrawSphere(position, 0.18f);

                if (point.TryGetLiteralRotation(out Quaternion rotation))
                    Gizmos.DrawLine(position, position + rotation * Vector3.forward * 1.0f);

                if (hasPrevious)
                    Gizmos.DrawLine(previousPosition, position);

                previousPosition = position;
                hasPrevious = true;
            }
        }
#endif
    }
}
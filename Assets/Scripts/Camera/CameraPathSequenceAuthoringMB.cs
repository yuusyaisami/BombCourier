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
    }
}
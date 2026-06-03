using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BC.Tutorial
{
    public enum TutorialReachLineNormalAxis
    {
        Forward = 0,
        Right = 10,
        Up = 20,
    }

    public enum TutorialReachLineTriggerMode
    {
        CrossFromBackToFront = 0,
        CrossEitherDirection = 10,
    }

    [DisallowMultipleComponent]
    public sealed class TutorialReachLineMB : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField, Tooltip("このオブジェクトのどのローカル軸を『正側』とみなすかを指定します。正側へ越えると達成方向として扱われます。")]
        private TutorialReachLineNormalAxis normalAxis = TutorialReachLineNormalAxis.Forward;

        [SerializeField, Tooltip("判定方法を指定します。BackToFront は負側から正側へ越えたときだけ成功します。EitherDirection はどちら側から越えても成功します。")]
        private TutorialReachLineTriggerMode triggerMode = TutorialReachLineTriggerMode.CrossFromBackToFront;

        [SerializeField, Min(0.0f), Tooltip("判定平面の半厚さです。0 より大きい場合、この帯に入った時点で到達扱いになります。")]
        private float distanceTolerance;

        [Header("Gizmo")]
        [SerializeField, Min(0.25f), Tooltip("SceneView で表示する判定面の横幅です。")]
        private float gizmoWidth = 4.0f;

        [SerializeField, Min(0.25f), Tooltip("SceneView で表示する判定面の高さです。")]
        private float gizmoHeight = 2.0f;

        [SerializeField, Min(0.1f), Tooltip("SceneView で表示する正側方向の矢印長です。")]
        private float gizmoNormalLength = 1.25f;

        public TutorialReachLineNormalAxis NormalAxis => normalAxis;
        public TutorialReachLineTriggerMode TriggerMode => triggerMode;
        public float DistanceTolerance => Mathf.Max(0.0f, distanceTolerance);
        public float GizmoWidth => Mathf.Max(0.25f, gizmoWidth);
        public float GizmoHeight => Mathf.Max(0.25f, gizmoHeight);
        public float GizmoNormalLength => Mathf.Max(0.1f, gizmoNormalLength);
        public bool RequireCrossingFromBackSide => triggerMode == TutorialReachLineTriggerMode.CrossFromBackToFront;

        public Vector3 GetWorldNormal()
        {
            return transform.TransformDirection(GetLocalNormalVector(normalAxis)).normalized;
        }

        public float ComputeSignedDistance(Vector3 worldPosition)
        {
            Vector3 offset = worldPosition - transform.position;
            return Vector3.Dot(GetWorldNormal(), offset);
        }

        public bool ShouldComplete(float previousDistance, float currentDistance)
        {
            float tolerance = DistanceTolerance;
            bool wasBehindPlane = previousDistance < -tolerance;
            bool wasInFrontOfPlane = previousDistance > tolerance;
            bool isWithinToleranceBand = Mathf.Abs(currentDistance) <= tolerance;
            bool reachedFrontSide = currentDistance >= -tolerance;
            bool reachedBackSide = currentDistance <= tolerance;

            if (RequireCrossingFromBackSide)
                return wasBehindPlane && reachedFrontSide;

            return isWithinToleranceBand ||
                   (wasBehindPlane && reachedFrontSide) ||
                   (wasInFrontOfPlane && reachedBackSide);
        }

        public void GetVisualizationBasis(out Vector3 worldNormal, out Vector3 planeRight, out Vector3 planeUp)
        {
            switch (normalAxis)
            {
                case TutorialReachLineNormalAxis.Right:
                    worldNormal = transform.right.normalized;
                    planeRight = transform.forward.normalized;
                    planeUp = transform.up.normalized;
                    break;

                case TutorialReachLineNormalAxis.Up:
                    worldNormal = transform.up.normalized;
                    planeRight = transform.right.normalized;
                    planeUp = transform.forward.normalized;
                    break;

                default:
                    worldNormal = transform.forward.normalized;
                    planeRight = transform.right.normalized;
                    planeUp = transform.up.normalized;
                    break;
            }
        }

        public string GetDebugDescription()
        {
            string crossingText = RequireCrossingFromBackSide
                ? "Back -> Front"
                : "Either Direction";
            return $"{crossingText} | Tolerance {DistanceTolerance:0.##}";
        }

        private static Vector3 GetLocalNormalVector(TutorialReachLineNormalAxis axis)
        {
            switch (axis)
            {
                case TutorialReachLineNormalAxis.Right:
                    return Vector3.right;

                case TutorialReachLineNormalAxis.Up:
                    return Vector3.up;

                default:
                    return Vector3.forward;
            }
        }

#if UNITY_EDITOR
        private static readonly Color PlaneColor = new(0.55f, 0.78f, 1.0f, 0.9f);
        private static readonly Color SelectedPlaneColor = new(1.0f, 0.82f, 0.36f, 1.0f);
        private static readonly Color FrontDirectionColor = new(0.26f, 0.9f, 0.42f, 0.95f);
        private static readonly Color BackDirectionColor = new(0.95f, 0.35f, 0.35f, 0.85f);
        private const float ArrowHeadLength = 0.22f;
        private const float ArrowHeadRadius = 0.08f;
        private const float LabelOffset = 0.35f;

        private void OnDrawGizmos()
        {
            if (Selection.Contains(gameObject))
                return;

            DrawReachGizmos(selected: false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawReachGizmos(selected: true);
        }

        private void DrawReachGizmos(bool selected)
        {
            GetVisualizationBasis(out Vector3 normal, out Vector3 planeRight, out Vector3 planeUp);

            Vector3 origin = transform.position;
            float halfWidth = GizmoWidth * 0.5f;
            float halfHeight = GizmoHeight * 0.5f;
            float tolerance = DistanceTolerance;

            Color previousColor = Gizmos.color;
            DrawPlaneRectangle(origin, planeRight, planeUp, halfWidth, halfHeight, selected ? SelectedPlaneColor : PlaneColor);

            if (tolerance > 0.0f)
            {
                Color toleranceColor = new(PlaneColor.r, PlaneColor.g, PlaneColor.b, 0.45f);
                DrawPlaneRectangle(origin + (normal * tolerance), planeRight, planeUp, halfWidth, halfHeight, toleranceColor);
                DrawPlaneRectangle(origin - (normal * tolerance), planeRight, planeUp, halfWidth, halfHeight, toleranceColor);

                Gizmos.color = toleranceColor;
                Gizmos.DrawLine(origin + (planeRight * halfWidth) + (normal * tolerance), origin + (planeRight * halfWidth) - (normal * tolerance));
                Gizmos.DrawLine(origin - (planeRight * halfWidth) + (normal * tolerance), origin - (planeRight * halfWidth) - (normal * tolerance));
                Gizmos.DrawLine(origin + (planeUp * halfHeight) + (normal * tolerance), origin + (planeUp * halfHeight) - (normal * tolerance));
                Gizmos.DrawLine(origin - (planeUp * halfHeight) + (normal * tolerance), origin - (planeUp * halfHeight) - (normal * tolerance));
            }

            DrawArrow(origin, normal, GizmoNormalLength, FrontDirectionColor);
            DrawArrow(origin, -normal, Mathf.Max(0.35f, GizmoNormalLength * 0.45f), BackDirectionColor);

            Gizmos.color = previousColor;

            if (selected)
            {
                Handles.color = SelectedPlaneColor;
                Handles.Label(
                    origin + (planeUp * (halfHeight + LabelOffset)),
                    $"ReachLine\n{GetDebugDescription()}");
            }
        }

        private static void DrawPlaneRectangle(
            Vector3 center,
            Vector3 planeRight,
            Vector3 planeUp,
            float halfWidth,
            float halfHeight,
            Color color)
        {
            Vector3 cornerA = center + (planeRight * halfWidth) + (planeUp * halfHeight);
            Vector3 cornerB = center - (planeRight * halfWidth) + (planeUp * halfHeight);
            Vector3 cornerC = center - (planeRight * halfWidth) - (planeUp * halfHeight);
            Vector3 cornerD = center + (planeRight * halfWidth) - (planeUp * halfHeight);

            Gizmos.color = color;
            Gizmos.DrawLine(cornerA, cornerB);
            Gizmos.DrawLine(cornerB, cornerC);
            Gizmos.DrawLine(cornerC, cornerD);
            Gizmos.DrawLine(cornerD, cornerA);
        }

        private static void DrawArrow(Vector3 origin, Vector3 direction, float length, Color color)
        {
            Vector3 normalizedDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            Vector3 tip = origin + (normalizedDirection * length);
            Vector3 arrowBase = tip - (normalizedDirection * ArrowHeadLength);

            Vector3 right = Vector3.Cross(normalizedDirection, Vector3.up);
            if (right.sqrMagnitude <= 0.0001f)
                right = Vector3.Cross(normalizedDirection, Vector3.forward);

            right.Normalize();
            Vector3 up = Vector3.Cross(right, normalizedDirection).normalized;

            Gizmos.color = color;
            Gizmos.DrawLine(origin, tip);
            Gizmos.DrawLine(arrowBase + (right * ArrowHeadRadius), tip);
            Gizmos.DrawLine(arrowBase - (right * ArrowHeadRadius), tip);
            Gizmos.DrawLine(arrowBase + (up * ArrowHeadRadius), tip);
            Gizmos.DrawLine(arrowBase - (up * ArrowHeadRadius), tip);
        }
#endif
    }
}

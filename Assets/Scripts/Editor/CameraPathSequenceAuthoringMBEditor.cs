using BC.Camera;
using BC.Base;
using UnityEditor;
using UnityEngine;

namespace BC.CameraEditor
{
    [CustomEditor(typeof(CameraPathSequenceAuthoringMB), true)]
    public sealed class CameraPathSequenceAuthoringMBEditor : UnityEditor.Editor
    {
        private const string PointsPropertyName = "points";
        private const string SelectedPointIndexPropertyName = "selectedPointIndex";
        private const string LabelFieldName = "label";
        private const string PositionFieldName = "position";
        private const string EulerAnglesFieldName = "eulerAngles";
        private const string ReactiveSourceKindFieldName = "sourceKind";
        private const string ReactiveEvaluationModeFieldName = "evaluationMode";
        private const string ReactiveFailurePolicyFieldName = "failurePolicy";
        private const string ReactiveLiteralFieldName = "literal";
        private const string ReactiveFallbackValueFieldName = "fallbackValue";
        private const string HoldSecondsFieldName = "holdSeconds";
        private const string TransitionFieldName = "transitionFromPrevious";
        private const string LensFieldName = "lens";
        private const string OnArriveActionFieldName = "onArriveAction";
        private const string TransitionKindFieldName = "kind";
        private const string TransitionDurationFieldName = "duration";
        private const string TransitionEaseFieldName = "ease";
        private const string LensOverrideFieldName = "overrideFieldOfView";
        private const string LensFieldOfViewFieldName = "fieldOfView";

        private static readonly Color LineColor = new(0.1f, 0.95f, 0.55f, 1.0f);
        private static readonly Color PointColor = new(0.25f, 0.85f, 1.0f, 1.0f);
        private static readonly Color SelectedPointColor = new(1.0f, 0.85f, 0.2f, 1.0f);

        private SerializedProperty pointsProperty;
        private SerializedProperty selectedPointIndexProperty;

        private void OnEnable()
        {
            pointsProperty = serializedObject.FindProperty(PointsPropertyName);
            selectedPointIndexProperty = serializedObject.FindProperty(SelectedPointIndexPropertyName);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EnsureSelectionInRange();

            EditorGUILayout.LabelField("Camera Path", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Position と Euler Angles は ReactiveVector3 です。Literal な要素だけ Scene View で編集します。Position が Literal なら移動、Euler Angles が Literal なら回転を扱えます。", MessageType.Info);

            int removeIndex = -1;
            int insertAfterIndex = -1;

            for (int i = 0; i < pointsProperty.arraySize; i++)
            {
                DrawPointInspector(i, ref removeIndex, ref insertAfterIndex);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Point"))
                {
                    InsertPointAfter(pointsProperty.arraySize - 1);
                }

                GUI.enabled = pointsProperty.arraySize > 0;
                if (GUILayout.Button("Select Last"))
                {
                    selectedPointIndexProperty.intValue = pointsProperty.arraySize - 1;
                    SceneView.RepaintAll();
                }
                GUI.enabled = true;
            }

            if (insertAfterIndex >= 0)
            {
                InsertPointAfter(insertAfterIndex);
            }

            if (removeIndex >= 0)
            {
                RemovePoint(removeIndex);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            serializedObject.Update();
            EnsureSelectionInRange();

            DrawPathSceneHandles();
            DrawSelectedTransformHandle();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPointInspector(int index, ref int removeIndex, ref int insertAfterIndex)
        {
            SerializedProperty pointProperty = pointsProperty.GetArrayElementAtIndex(index);
            SerializedProperty labelProperty = pointProperty.FindPropertyRelative(LabelFieldName);
            string pointName = string.IsNullOrWhiteSpace(labelProperty.stringValue) ? $"Point {index + 1}" : $"{index + 1}: {labelProperty.stringValue}";
            bool selected = selectedPointIndexProperty.intValue == index;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(selected, pointName, EditorStyles.miniButtonLeft))
                    {
                        selectedPointIndexProperty.intValue = index;
                        SceneView.RepaintAll();
                    }

                    if (GUILayout.Button("+", EditorStyles.miniButtonMid, GUILayout.Width(28.0f)))
                    {
                        insertAfterIndex = index;
                    }

                    if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(28.0f)))
                    {
                        removeIndex = index;
                    }
                }

                if (!selected)
                    return;

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(labelProperty);
                EditorGUILayout.PropertyField(pointProperty.FindPropertyRelative(PositionFieldName), true);
                EditorGUILayout.PropertyField(pointProperty.FindPropertyRelative(EulerAnglesFieldName), true);
                EditorGUILayout.PropertyField(pointProperty.FindPropertyRelative(HoldSecondsFieldName));
                EditorGUILayout.PropertyField(pointProperty.FindPropertyRelative(TransitionFieldName), true);
                EditorGUILayout.PropertyField(pointProperty.FindPropertyRelative(LensFieldName), true);
                EditorGUILayout.PropertyField(pointProperty.FindPropertyRelative(OnArriveActionFieldName), true);

                if (!HasLiteralPosition(pointProperty) && !HasLiteralRotation(pointProperty))
                    EditorGUILayout.HelpBox("このポイントは Position/Euler Angles ともに Literal ではないため、Scene View のハンドル表示から除外されます。", MessageType.None);
                else if (!HasLiteralPosition(pointProperty) || !HasLiteralRotation(pointProperty))
                    EditorGUILayout.HelpBox("Literal な要素だけ Scene View で編集します。Position 非 Literal では回転のみ、Euler Angles 非 Literal では移動のみを扱います。", MessageType.None);

                EditorGUI.indentLevel--;
            }
        }

        private void DrawPathSceneHandles()
        {
            Vector3 previousPosition = Vector3.zero;
            bool hasPrevious = false;

            for (int i = 0; i < pointsProperty.arraySize; i++)
            {
                SerializedProperty pointProperty = pointsProperty.GetArrayElementAtIndex(i);
                if (!TryGetLiteralPosition(pointProperty, out _, out Vector3 position))
                {
                    hasPrevious = false;
                    continue;
                }

                bool hasLiteralRotation = TryGetLiteralRotation(pointProperty, out _, out Quaternion rotation);
                Quaternion handleRotation = hasLiteralRotation ? rotation : Quaternion.identity;

                float handleSize = HandleUtility.GetHandleSize(position);
                bool selected = selectedPointIndexProperty.intValue == i;

                if (hasPrevious)
                {
                    Handles.color = LineColor;
                    Handles.DrawAAPolyLine(4.0f, previousPosition, position);
                }

                Handles.color = selected ? SelectedPointColor : PointColor;
                if (Handles.Button(position, handleRotation, handleSize * 0.12f, handleSize * 0.18f, Handles.SphereHandleCap))
                {
                    selectedPointIndexProperty.intValue = i;
                    SceneView.RepaintAll();
                }

                if (hasLiteralRotation)
                    Handles.ArrowHandleCap(0, position, rotation, handleSize * 0.55f, EventType.Repaint);

                Handles.Label(position + Vector3.up * handleSize * 0.18f, GetPointLabel(i, pointProperty));

                previousPosition = position;
                hasPrevious = true;
            }
        }

        private void DrawSelectedTransformHandle()
        {
            int selectedIndex = selectedPointIndexProperty.intValue;

            if (selectedIndex < 0 || selectedIndex >= pointsProperty.arraySize)
                return;

            CameraPathSequenceAuthoringMB sequence = (CameraPathSequenceAuthoringMB)target;
            SerializedProperty pointProperty = pointsProperty.GetArrayElementAtIndex(selectedIndex);

            bool hasLiteralPosition = TryGetLiteralPosition(pointProperty, out SerializedProperty positionProperty, out Vector3 position);
            bool hasLiteralRotation = TryGetLiteralRotation(pointProperty, out SerializedProperty eulerAnglesProperty, out Quaternion rotation);

            if (!hasLiteralPosition && !hasLiteralRotation)
                return;

            if (hasLiteralPosition)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 movedPosition = Handles.PositionHandle(position, hasLiteralRotation ? rotation : Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Move Camera Path Point");
                    positionProperty.vector3Value = movedPosition;
                    position = movedPosition;
                    EditorUtility.SetDirty(target);
                }
            }

            if (hasLiteralRotation)
            {
                Vector3 rotationPivot = hasLiteralPosition ? position : sequence.transform.position;

                EditorGUI.BeginChangeCheck();
                Quaternion movedRotation = Handles.RotationHandle(rotation, rotationPivot);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Rotate Camera Path Point");
                    eulerAnglesProperty.vector3Value = movedRotation.eulerAngles;
                    EditorUtility.SetDirty(target);
                }
            }
        }

        private void InsertPointAfter(int sourceIndex)
        {
            BuildNewPointPose(sourceIndex, out Vector3 position, out Quaternion rotation);

            int newIndex = Mathf.Clamp(sourceIndex + 1, 0, pointsProperty.arraySize);

            if (pointsProperty.arraySize == 0)
            {
                pointsProperty.arraySize = 1;
                newIndex = 0;
            }
            else
            {
                pointsProperty.InsertArrayElementAtIndex(newIndex);
            }

            WriteDefaultPoint(pointsProperty.GetArrayElementAtIndex(newIndex), newIndex, position, rotation);
            selectedPointIndexProperty.intValue = newIndex;
            SceneView.RepaintAll();
        }

        private void RemovePoint(int index)
        {
            if (index < 0 || index >= pointsProperty.arraySize)
                return;

            pointsProperty.DeleteArrayElementAtIndex(index);
            selectedPointIndexProperty.intValue = pointsProperty.arraySize == 0 ? -1 : Mathf.Clamp(index, 0, pointsProperty.arraySize - 1);
            SceneView.RepaintAll();
        }

        private void BuildNewPointPose(int sourceIndex, out Vector3 position, out Quaternion rotation)
        {
            CameraPathSequenceAuthoringMB sequence = (CameraPathSequenceAuthoringMB)target;
            position = sequence.transform.position;
            rotation = sequence.transform.rotation;

            if (sourceIndex < 0 || sourceIndex >= pointsProperty.arraySize)
                return;

            SerializedProperty sourcePoint = pointsProperty.GetArrayElementAtIndex(sourceIndex);

            bool hasLiteralPosition = TryGetLiteralPosition(sourcePoint, out _, out Vector3 sourcePosition);
            bool hasLiteralRotation = TryGetLiteralRotation(sourcePoint, out _, out Quaternion sourceRotation);

            if (hasLiteralPosition)
                position = sourcePosition;

            if (hasLiteralRotation)
                rotation = sourceRotation;

            if (hasLiteralPosition)
                position += rotation * Vector3.forward * 2.0f;
        }

        private static void WriteDefaultPoint(SerializedProperty pointProperty, int index, Vector3 position, Quaternion rotation)
        {
            pointProperty.FindPropertyRelative(LabelFieldName).stringValue = $"Point {index + 1}";
            WriteLiteralReactiveVector3(pointProperty.FindPropertyRelative(PositionFieldName), position);
            WriteLiteralReactiveVector3(pointProperty.FindPropertyRelative(EulerAnglesFieldName), rotation.eulerAngles);
            pointProperty.FindPropertyRelative(HoldSecondsFieldName).floatValue = 0.0f;

            SerializedProperty transitionProperty = pointProperty.FindPropertyRelative(TransitionFieldName);
            transitionProperty.FindPropertyRelative(TransitionKindFieldName).enumValueIndex = index == 0 ? (int)CameraPathTransitionKind.Cut : (int)CameraPathTransitionKind.Ease;
            transitionProperty.FindPropertyRelative(TransitionDurationFieldName).floatValue = index == 0 ? 0.0f : 1.0f;
            transitionProperty.FindPropertyRelative(TransitionEaseFieldName).animationCurveValue = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

            SerializedProperty lensProperty = pointProperty.FindPropertyRelative(LensFieldName);
            lensProperty.FindPropertyRelative(LensOverrideFieldName).boolValue = false;
            lensProperty.FindPropertyRelative(LensFieldOfViewFieldName).floatValue = 60.0f;
        }

        private void EnsureSelectionInRange()
        {
            if (pointsProperty.arraySize == 0)
            {
                selectedPointIndexProperty.intValue = -1;
                return;
            }

            if (selectedPointIndexProperty.intValue < 0 || selectedPointIndexProperty.intValue >= pointsProperty.arraySize)
            {
                selectedPointIndexProperty.intValue = 0;
            }
        }

        private static string GetPointLabel(int index, SerializedProperty pointProperty)
        {
            string label = pointProperty.FindPropertyRelative(LabelFieldName).stringValue;
            return string.IsNullOrWhiteSpace(label) ? $"Point {index + 1}" : $"{index + 1}: {label}";
        }

        private static bool HasLiteralPosition(SerializedProperty pointProperty)
        {
            return IsLiteralReactiveVector3(pointProperty.FindPropertyRelative(PositionFieldName));
        }

        private static bool HasLiteralRotation(SerializedProperty pointProperty)
        {
            return IsLiteralReactiveVector3(pointProperty.FindPropertyRelative(EulerAnglesFieldName));
        }

        private static bool TryGetLiteralPosition(
            SerializedProperty pointProperty,
            out SerializedProperty positionLiteralProperty,
            out Vector3 position)
        {
            positionLiteralProperty = null;
            position = Vector3.zero;

            SerializedProperty positionProperty = pointProperty.FindPropertyRelative(PositionFieldName);

            if (!IsLiteralReactiveVector3(positionProperty))
                return false;

            positionLiteralProperty = positionProperty.FindPropertyRelative(ReactiveLiteralFieldName);
            position = positionLiteralProperty.vector3Value;
            return true;
        }

        private static bool TryGetLiteralRotation(
            SerializedProperty pointProperty,
            out SerializedProperty eulerAnglesLiteralProperty,
            out Quaternion rotation)
        {
            eulerAnglesLiteralProperty = null;
            rotation = Quaternion.identity;

            SerializedProperty eulerAnglesProperty = pointProperty.FindPropertyRelative(EulerAnglesFieldName);

            if (!IsLiteralReactiveVector3(eulerAnglesProperty))
                return false;

            eulerAnglesLiteralProperty = eulerAnglesProperty.FindPropertyRelative(ReactiveLiteralFieldName);
            rotation = Quaternion.Euler(eulerAnglesLiteralProperty.vector3Value);
            return true;
        }

        private static bool IsLiteralReactiveVector3(SerializedProperty property)
        {
            SerializedProperty sourceKindProperty = property?.FindPropertyRelative(ReactiveSourceKindFieldName);
            return sourceKindProperty != null && sourceKindProperty.enumValueIndex == (int)ReactiveVector3SourceKind.Literal;
        }

        private static void WriteLiteralReactiveVector3(SerializedProperty property, Vector3 value)
        {
            property.FindPropertyRelative(ReactiveSourceKindFieldName).enumValueIndex = (int)ReactiveVector3SourceKind.Literal;
            property.FindPropertyRelative(ReactiveEvaluationModeFieldName).enumValueIndex = (int)ReactiveEvaluationMode.Snapshot;
            property.FindPropertyRelative(ReactiveFailurePolicyFieldName).enumValueIndex = (int)ReactiveFailurePolicy.FailAction;
            property.FindPropertyRelative(ReactiveLiteralFieldName).vector3Value = value;
            property.FindPropertyRelative(ReactiveFallbackValueFieldName).vector3Value = value;
        }
    }
}
using BC.Editor.Foundation;
using BC.Editor.Foundation.Scene;
using BC.Base;
using BC.Gimmick.MovingPlatform;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Gimmick.MovingPlatformTools
{
    [CustomEditor(typeof(MovingPlatformMB), true)]
    public sealed class MovingPlatformAuthoringEditor : OdinEditor
    {
        private static int selectedRailNodeIndex = -1;

        private readonly MovingPlatformSceneHandleController sceneHandleController = new();

        private MovingPlatformMB TypedTarget => target as MovingPlatformMB;

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            // CameraPath と同様に、Inspector から選択した要素を Scene ハンドル編集へ直結する。
            MovingPlatformSceneHandleController.DrawInspectorNodeSelector(TypedTarget, ref selectedRailNodeIndex, RepaintSceneView);

            UndoApplyUtility.ApplyModifiedProperties(serializedObject);
        }

        private void OnSceneGUI()
        {
            MovingPlatformMB movingPlatform = TypedTarget;
            if (movingPlatform == null)
                return;

            serializedObject.UpdateIfRequiredOrScript();
            sceneHandleController.DrawSceneHandles(movingPlatform, serializedObject, ref selectedRailNodeIndex, RepaintSceneView);
            UndoApplyUtility.ApplyModifiedProperties(serializedObject);
        }

        private void RepaintSceneView()
        {
            SceneView.RepaintAll();
            Repaint();
        }
    }

    public sealed class MovingPlatformSceneHandleController
    {
        private const string RailNodesFieldName = "railNodes";
        private const string LocalPositionFieldName = "localPosition";
        private const string SourceKindFieldName = "sourceKind";
        private const string EvaluationModeFieldName = "evaluationMode";
        private const string FailurePolicyFieldName = "failurePolicy";
        private const string LiteralFieldName = "literal";
        private const string FallbackValueFieldName = "fallbackValue";

        private readonly List<MovingPlatformEditorRailNodeHandleData> handleNodes = new();

        public void DrawSceneHandles(MovingPlatformMB target, SerializedObject serializedObject, ref int selectedRailNodeIndex, Action repaintScene)
        {
            if (target == null || serializedObject == null)
                return;

            if (!target.TryCollectEditorRailNodeHandleData(handleNodes))
                return;

            DrawNodeSelectionButtons(ref selectedRailNodeIndex, repaintScene);
            DrawSelectedNodePositionHandle(target, serializedObject, selectedRailNodeIndex, repaintScene);
        }

        private static void DrawSelectedNodePositionHandle(
            MovingPlatformMB target,
            SerializedObject serializedObject,
            int selectedRailNodeIndex,
            Action repaintScene)
        {
            if (!TryFindHandleNode(selectedRailNodeIndex, target, out MovingPlatformEditorRailNodeHandleData selectedNode))
                return;

            if (!selectedNode.IsLiteralPosition)
                return;

            using SceneUndoScope moveScope = new(target, "Move MovingPlatform Rail Node", recordPrefabOverrides: false, markDirty: false);
            Vector3 movedWorldPosition = Handles.PositionHandle(selectedNode.WorldPosition, Quaternion.identity);
            if (!moveScope.TryRecordChanges())
                return;

            Transform motionTransform = target.TryGetPrimaryMotionTransform(out Transform resolvedMotionTransform)
                ? resolvedMotionTransform
                : target.transform;
            Vector3 movedLocalPosition = motionTransform.InverseTransformPoint(movedWorldPosition);
            if (!TrySetNodeLiteralPosition(serializedObject, selectedRailNodeIndex, movedLocalPosition))
                return;

            repaintScene?.Invoke();
        }

        private void DrawNodeSelectionButtons(ref int selectedRailNodeIndex, Action repaintScene)
        {
            Vector3 previousWorld = Vector3.zero;
            bool hasPrevious = false;

            for (int i = 0; i < handleNodes.Count; i++)
            {
                MovingPlatformEditorRailNodeHandleData node = handleNodes[i];
                float handleSize = HandleUtility.GetHandleSize(node.WorldPosition);
                bool selected = selectedRailNodeIndex == node.RailNodeIndex;

                if (hasPrevious)
                {
                    Handles.color = SceneHandleStyleTokens.LineColor;
                    Handles.DrawAAPolyLine(3.0f, previousWorld, node.WorldPosition);
                }

                Handles.color = selected ? SceneHandleStyleTokens.SelectedColor : SceneHandleStyleTokens.LineColor;
                if (Handles.Button(
                        node.WorldPosition,
                        Quaternion.identity,
                        ResolveNodeHandleRadius(handleSize),
                        ResolveNodePickRadius(handleSize),
                        Handles.SphereHandleCap))
                {
                    selectedRailNodeIndex = node.RailNodeIndex;
                    repaintScene?.Invoke();
                }

                Handles.Label(
                    node.WorldPosition + Vector3.up * handleSize * SceneHandleStyleTokens.LabelOffset,
                    BuildNodeLabel(node));

                previousWorld = node.WorldPosition;
                hasPrevious = true;
            }
        }

        public static void DrawInspectorNodeSelector(MovingPlatformMB target, ref int selectedRailNodeIndex, Action repaintScene)
        {
            if (target == null)
                return;

            var tempNodes = new List<MovingPlatformEditorRailNodeHandleData>();
            if (!target.TryCollectEditorRailNodeHandleData(tempNodes))
                return;

            EditorGUILayout.LabelField("Rail Node Selection", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int i = 0; i < tempNodes.Count; i++)
                {
                    MovingPlatformEditorRailNodeHandleData node = tempNodes[i];
                    bool selected = selectedRailNodeIndex == node.RailNodeIndex;
                    string label = BuildNodeLabel(node);
                    if (!node.IsLiteralPosition)
                        label += " (ReadOnly)";

                    if (GUILayout.Toggle(selected, label, EditorStyles.miniButton))
                    {
                        if (!selected)
                        {
                            selectedRailNodeIndex = node.RailNodeIndex;
                            repaintScene?.Invoke();
                        }
                    }
                }
            }
        }

        private static bool TrySetNodeLiteralPosition(SerializedObject serializedObject, int railNodeIndex, Vector3 localPosition)
        {
            SerializedProperty railNodesProperty = serializedObject.FindProperty(RailNodesFieldName);
            if (railNodesProperty == null || !railNodesProperty.isArray)
                return false;

            if (railNodeIndex < 0 || railNodeIndex >= railNodesProperty.arraySize)
                return false;

            SerializedProperty nodeProperty = railNodesProperty.GetArrayElementAtIndex(railNodeIndex);
            if (nodeProperty == null)
                return false;

            SerializedProperty localPositionProperty = nodeProperty.FindPropertyRelative(LocalPositionFieldName);
            if (localPositionProperty == null)
                return false;

            SerializedProperty sourceKindProperty = localPositionProperty.FindPropertyRelative(SourceKindFieldName);
            SerializedProperty evaluationModeProperty = localPositionProperty.FindPropertyRelative(EvaluationModeFieldName);
            SerializedProperty failurePolicyProperty = localPositionProperty.FindPropertyRelative(FailurePolicyFieldName);
            SerializedProperty literalProperty = localPositionProperty.FindPropertyRelative(LiteralFieldName);
            SerializedProperty fallbackValueProperty = localPositionProperty.FindPropertyRelative(FallbackValueFieldName);

            if (sourceKindProperty == null ||
                evaluationModeProperty == null ||
                failurePolicyProperty == null ||
                literalProperty == null ||
                fallbackValueProperty == null)
            {
                return false;
            }

            sourceKindProperty.intValue = (int)ReactiveVector3SourceKind.Literal;
            evaluationModeProperty.intValue = (int)ReactiveEvaluationMode.Snapshot;
            failurePolicyProperty.intValue = (int)ReactiveFailurePolicy.FailAction;
            literalProperty.vector3Value = localPosition;
            fallbackValueProperty.vector3Value = localPosition;
            return true;
        }

        private static bool TryFindHandleNode(int selectedRailNodeIndex, MovingPlatformMB target, out MovingPlatformEditorRailNodeHandleData handleNode)
        {
            handleNode = default;

            if (target == null || selectedRailNodeIndex < 0)
                return false;

            var nodes = new List<MovingPlatformEditorRailNodeHandleData>();
            if (!target.TryCollectEditorRailNodeHandleData(nodes))
                return false;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].RailNodeIndex != selectedRailNodeIndex)
                    continue;

                handleNode = nodes[i];
                return true;
            }

            return false;
        }

        private static string BuildNodeLabel(MovingPlatformEditorRailNodeHandleData node)
        {
            string nodePath = string.IsNullOrWhiteSpace(node.NodePath) ? "Node" : node.NodePath;
            return $"{node.RailNodeIndex + 1}: {nodePath}";
        }

        private static float ResolveNodeHandleRadius(float handleSize)
        {
            return Mathf.Max(0.1f, handleSize * SceneHandleStyleTokens.HandleSizeMultiplier * 1.2f);
        }

        private static float ResolveNodePickRadius(float handleSize)
        {
            return Mathf.Max(0.15f, handleSize * SceneHandleStyleTokens.HandleSizeMultiplier * 2.0f);
        }
    }
}

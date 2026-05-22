using System;
using System.Collections.Generic;
using BC.Gimmick.MovingPlatform;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(MovingPlatformNodePathDropdownAttribute))]
    public sealed class MovingPlatformNodePathDropdownDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use MovingPlatformNodePathDropdown on string fields only.");
                return;
            }

            List<NodeOption> options = BuildNodeOptions(property.serializedObject);
            Rect fieldRect = EditorGUI.PrefixLabel(position, label);
            GUIContent buttonContent = BuildButtonContent(property.stringValue, options);

            using (new EditorGUI.DisabledScope(options.Count == 0))
            {
                if (EditorGUI.DropdownButton(fieldRect, buttonContent, FocusType.Keyboard, EditorStyles.popup))
                    ShowNodeMenu(fieldRect, property, options);
            }
        }

        private static GUIContent BuildButtonContent(string currentPath, List<NodeOption> options)
        {
            if (options.Count == 0)
                return new GUIContent("No Shared Rails Nodes", "SharedRails に node がまだありません。");

            string normalizedPath = NormalizeNodePath(currentPath);
            for (int i = 0; i < options.Count; i++)
            {
                if (!string.Equals(options[i].Path, normalizedPath, StringComparison.Ordinal))
                    continue;

                return new GUIContent(options[i].Label, options[i].Tooltip);
            }

            if (!string.IsNullOrWhiteSpace(normalizedPath))
                return new GUIContent($"Missing: {normalizedPath}", "現在の route は SharedRails 上の node に一致していません。");

            return new GUIContent("None", "SharedRails の node を選択してください。");
        }

        private static void ShowNodeMenu(Rect fieldRect, SerializedProperty property, List<NodeOption> options)
        {
            GenericMenu menu = new();
            string normalizedPath = NormalizeNodePath(property.stringValue);

            for (int i = 0; i < options.Count; i++)
            {
                NodeOption option = options[i];
                bool isCurrent = string.Equals(option.Path, normalizedPath, StringComparison.Ordinal);
                menu.AddItem(new GUIContent(option.Label), isCurrent, () => ApplySelection(property, option.Path));
            }

            menu.DropDown(fieldRect);
        }

        private static void ApplySelection(SerializedProperty property, string nodePath)
        {
            UnityEngine.Object[] targets = property.serializedObject.targetObjects;
            string propertyPath = property.propertyPath;

            for (int i = 0; i < targets.Length; i++)
            {
                UnityEngine.Object target = targets[i];
                if (target == null)
                    continue;

                Undo.RecordObject(target, "Select Shared Rail Node");
                SerializedObject serializedObject = new(target);
                SerializedProperty targetProperty = serializedObject.FindProperty(propertyPath);
                if (targetProperty == null)
                    continue;

                targetProperty.stringValue = nodePath;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static List<NodeOption> BuildNodeOptions(SerializedObject serializedObject)
        {
            var options = new List<NodeOption>();
            SerializedProperty railNodesProperty = serializedObject.FindProperty("railNodes");
            if (railNodesProperty == null || !railNodesProperty.isArray)
                return options;

            var usedLabels = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < railNodesProperty.arraySize; i++)
            {
                SerializedProperty nodeProperty = railNodesProperty.GetArrayElementAtIndex(i);
                SerializedProperty nodePathProperty = nodeProperty.FindPropertyRelative("nodePath");
                if (nodePathProperty == null)
                    continue;

                string nodePath = NormalizeNodePath(nodePathProperty.stringValue);
                if (string.IsNullOrWhiteSpace(nodePath))
                    continue;

                string label = nodePath;

                if (usedLabels.TryGetValue(label, out int duplicateCount))
                {
                    duplicateCount++;
                    usedLabels[label] = duplicateCount;
                    label = $"{label} [{duplicateCount}]";
                }
                else
                {
                    usedLabels.Add(label, 1);
                }

                options.Add(new NodeOption(nodePath, label, $"Node Path: {nodePath}"));
            }

            return options;
        }

        private static string NormalizeNodePath(string nodePath)
        {
            return string.IsNullOrWhiteSpace(nodePath) ? string.Empty : nodePath.Trim();
        }

        private readonly struct NodeOption
        {
            public readonly string Path;
            public readonly string Label;
            public readonly string Tooltip;

            public NodeOption(string path, string label, string tooltip)
            {
                Path = path;
                Label = label;
                Tooltip = tooltip;
            }
        }
    }
}

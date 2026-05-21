using System;
using BC.Editor.Foundation.IMGUI;
using UnityEditor;

namespace BC.Editor.Action
{
    internal static class ActionStepContextMenuUtility
    {
        internal static void Show(SerializedProperty stepProperty, int index)
        {
            if (stepProperty == null)
                return;

            UnityEngine.Object[] targets = stepProperty.serializedObject.targetObjects;
            string stepPropertyPath = stepProperty.propertyPath;
            string listPropertyPath = ResolveParentListPropertyPath(stepPropertyPath);
            string renameRowKey = InlineActionEditorState.GetRenameRowKey(stepProperty);
            string currentDisplayName = stepProperty.FindPropertyRelative("DisplayName")?.stringValue ?? string.Empty;
            bool hasCustomLabel = !string.IsNullOrWhiteSpace(currentDisplayName);
            int arraySize = ResolveArraySize(stepProperty);

            ContextMenuBuilder menu = new();
            menu.AddItem(
                "Rename Label",
                stepProperty.managedReferenceValue != null,
                () => InlineActionEditorState.BeginRename(renameRowKey, currentDisplayName));
            menu.AddItem(
                "Clear Label",
                hasCustomLabel,
                () => ActionStepManagedReferenceUtility.ClearDisplayName(targets, stepPropertyPath));
            menu.AddSeparator();
            menu.AddItem(
                "Duplicate Step",
                !string.IsNullOrWhiteSpace(listPropertyPath),
                () => ActionStepManagedReferenceUtility.DuplicateStep(targets, listPropertyPath, index));
            menu.AddItem(
                "Delete Step",
                !string.IsNullOrWhiteSpace(listPropertyPath),
                () =>
                {
                    InlineActionEditorState.CancelRename(renameRowKey);
                    ActionStepManagedReferenceUtility.DeleteStep(targets, listPropertyPath, index);
                });
            menu.AddSeparator();
            menu.AddItem(
                "Move Up",
                index > 0,
                () => ActionStepManagedReferenceUtility.MoveStep(targets, listPropertyPath, index, index - 1));
            menu.AddItem(
                "Move Down",
                index >= 0 && index < arraySize - 1,
                () => ActionStepManagedReferenceUtility.MoveStep(targets, listPropertyPath, index, index + 1));
            menu.ShowAsContext();
        }

        private static int ResolveArraySize(SerializedProperty stepProperty)
        {
            string listPropertyPath = ResolveParentListPropertyPath(stepProperty.propertyPath);
            SerializedProperty listProperty = stepProperty.serializedObject.FindProperty(listPropertyPath);
            return listProperty != null && listProperty.isArray ? listProperty.arraySize : 0;
        }

        private static string ResolveParentListPropertyPath(string stepPropertyPath)
        {
            if (string.IsNullOrWhiteSpace(stepPropertyPath))
                return string.Empty;

            int markerIndex = stepPropertyPath.LastIndexOf(".Array.data[", StringComparison.Ordinal);
            return markerIndex < 0 ? string.Empty : stepPropertyPath.Substring(0, markerIndex);
        }
    }
}

using System;
using System.Collections.Generic;
using BC.Editor.Foundation.IMGUI;
using UnityEditor;

namespace BC.Editor.ActionSystem
{
    internal static class ActionStepContextMenuUtility
    {
        internal static void Show(SerializedProperty stepProperty, int index)
        {
            if (stepProperty == null)
                return;

            UnityEngine.Object[] targets = stepProperty.serializedObject.targetObjects;
            string stepPropertyPath = stepProperty.propertyPath;
            string listPropertyPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(stepPropertyPath);
            string renameRowKey = InlineActionEditorState.GetRenameRowKey(stepProperty);
            string currentDisplayName = stepProperty.FindPropertyRelative("DisplayName")?.stringValue ?? string.Empty;
            bool hasCustomLabel = !string.IsNullOrWhiteSpace(currentDisplayName);
            int arraySize = ResolveArraySize(stepProperty);
            IReadOnlyList<Type> stepTypes = ActionStepManagedReferenceUtility.GetStepTypes();

            ContextMenuBuilder menu = new();
            // Structural commands come first so right-click can replace the old inline footer buttons.
            for (int i = 0; i < stepTypes.Count; i++)
            {
                Type stepType = stepTypes[i];
                menu.AddItem(
                    $"Add Step/{ActionStepManagedReferenceUtility.GetStepTypeLabel(stepType)}",
                    !string.IsNullOrWhiteSpace(listPropertyPath),
                    () => ActionStepManagedReferenceUtility.AddStep(targets, listPropertyPath, stepType, index + 1));
            }

            menu.AddSeparator();
            menu.AddItem(
                "Delete",
                !string.IsNullOrWhiteSpace(listPropertyPath),
                () =>
                {
                    InlineActionEditorState.CancelRename(renameRowKey);
                    InlineActionEditorState.ClearActive();
                    ActionStepManagedReferenceUtility.DeleteStep(targets, listPropertyPath, index);
                });
            menu.AddItem(
                "Copy",
                stepProperty.managedReferenceValue != null,
                () => ActionStepManagedReferenceUtility.CopyStep(stepProperty));
            menu.AddItem(
                "Paste",
                !string.IsNullOrWhiteSpace(listPropertyPath) && ActionStepManagedReferenceUtility.CanPasteStep(),
                () => ActionStepManagedReferenceUtility.PasteStep(targets, listPropertyPath, index + 1));

            menu.AddSeparator();
            // Renaming and label management stay secondary because they do not change the tree structure.
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
            string listPropertyPath = ActionStepManagedReferenceUtility.ResolveParentListPropertyPath(stepProperty.propertyPath);
            SerializedProperty listProperty = stepProperty.serializedObject.FindProperty(listPropertyPath);
            return listProperty != null && listProperty.isArray ? listProperty.arraySize : 0;
        }
    }
}

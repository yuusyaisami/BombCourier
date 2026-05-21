using System;
using System.Collections.Generic;
using BC.ActionSystem;
using BC.Editor.Foundation;
using BC.Editor.Foundation.IMGUI;
using UnityEditor;

namespace BC.Editor.Action
{
    internal static class ActionStepManagedReferenceUtility
    {
        private const string StepTypeSuffix = "StepAuthoring";

        private static IReadOnlyList<Type> cachedStepTypes;

        internal static IReadOnlyList<Type> GetStepTypes()
        {
            if (cachedStepTypes != null)
                return cachedStepTypes;

            List<Type> stepTypes = new();
            var discoveredTypes = TypeCache.GetTypesDerivedFrom<ActionStepAuthoring>();

            for (int i = 0; i < discoveredTypes.Count; i++)
            {
                Type stepType = discoveredTypes[i];

                if (stepType == null || stepType.IsAbstract || stepType.IsGenericTypeDefinition)
                    continue;

                stepTypes.Add(stepType);
            }

            stepTypes.Sort(static (left, right) => string.Compare(
                GetStepTypeLabel(left),
                GetStepTypeLabel(right),
                StringComparison.Ordinal));

            cachedStepTypes = stepTypes;
            return cachedStepTypes;
        }

        internal static string GetStepTypeLabel(Type stepType)
        {
            if (stepType == null)
                return "Missing";

            string typeName = stepType.Name;

            if (typeName.EndsWith(StepTypeSuffix, StringComparison.Ordinal))
                typeName = typeName.Substring(0, typeName.Length - StepTypeSuffix.Length);

            return ObjectNames.NicifyVariableName(typeName);
        }

        internal static void AddStep(UnityEngine.Object[] targets, string listPropertyPath, Type stepType)
        {
            if (stepType == null)
                return;

            ApplyListMutation(targets, listPropertyPath, "Add Action Step", listProperty =>
            {
                ManagedReferenceListController.AddNewElement(listProperty, stepType);
            });
        }

        internal static void DuplicateStep(UnityEngine.Object[] targets, string listPropertyPath, int index)
        {
            ApplyListMutation(targets, listPropertyPath, "Duplicate Action Step", listProperty =>
            {
                if (index < 0 || index >= listProperty.arraySize)
                    return;

                ManagedReferenceListController.DuplicateElement(listProperty, index);
            });
        }

        internal static void DeleteStep(UnityEngine.Object[] targets, string listPropertyPath, int index)
        {
            ApplyListMutation(targets, listPropertyPath, "Delete Action Step", listProperty =>
            {
                if (index < 0 || index >= listProperty.arraySize)
                    return;

                ManagedReferenceListController.DeleteElement(listProperty, index);
            });
        }

        internal static void ClearSteps(UnityEngine.Object[] targets, string listPropertyPath)
        {
            ApplyListMutation(targets, listPropertyPath, "Clear Action Steps", listProperty =>
            {
                listProperty.arraySize = 0;
            });
        }

        internal static void ClearInlineAction(UnityEngine.Object[] targets, string inlineActionPropertyPath)
        {
            if (targets == null || string.IsNullOrWhiteSpace(inlineActionPropertyPath))
                return;

            UndoApplyUtility.ApplyToTargets(
                targets,
                "Clear Inline Action Branch",
                serializedObject =>
                {
                    SerializedProperty inlineActionProperty = serializedObject.FindProperty(inlineActionPropertyPath);

                    if (inlineActionProperty != null)
                        inlineActionProperty.boxedValue = null;
                });
        }

        internal static void MoveStep(UnityEngine.Object[] targets, string listPropertyPath, int sourceIndex, int destinationIndex)
        {
            ApplyListMutation(targets, listPropertyPath, "Move Action Step", listProperty =>
            {
                if (sourceIndex < 0 || sourceIndex >= listProperty.arraySize)
                    return;

                if (destinationIndex < 0 || destinationIndex >= listProperty.arraySize)
                    return;

                ManagedReferenceListController.MoveElement(listProperty, sourceIndex, destinationIndex);
            });
        }

        internal static void ClearDisplayName(UnityEngine.Object[] targets, string stepPropertyPath)
        {
            SetDisplayName(targets, stepPropertyPath, string.Empty, "Clear Action Label");
        }

        internal static void SetDisplayName(
            UnityEngine.Object[] targets,
            string stepPropertyPath,
            string displayName,
            string undoName = "Rename Action Label")
        {
            if (targets == null || string.IsNullOrWhiteSpace(stepPropertyPath))
                return;

            string normalizedName = string.IsNullOrWhiteSpace(displayName)
                ? string.Empty
                : displayName.Trim();

            UndoApplyUtility.ApplyToTargets(
                targets,
                undoName,
                serializedObject =>
                {
                    SerializedProperty stepProperty = serializedObject.FindProperty(stepPropertyPath);
                    SerializedProperty displayNameProperty = stepProperty?.FindPropertyRelative("DisplayName");

                    if (displayNameProperty != null)
                        displayNameProperty.stringValue = normalizedName;
                });
        }

        private static void ApplyListMutation(
            UnityEngine.Object[] targets,
            string listPropertyPath,
            string undoName,
            Action<SerializedProperty> mutate)
        {
            if (targets == null || string.IsNullOrWhiteSpace(listPropertyPath) || mutate == null)
                return;

            UndoApplyUtility.ApplyToTargets(
                targets,
                undoName,
                serializedObject =>
                {
                    SerializedProperty listProperty = serializedObject.FindProperty(listPropertyPath);

                    if (listProperty == null || !listProperty.isArray)
                        return;

                    mutate(listProperty);
                });
        }
    }
}

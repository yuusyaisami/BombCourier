using System;
using System.Collections.Generic;
using BC.ActionSystem;
using BC.Editor.Foundation;
using BC.Editor.Foundation.IMGUI;
using UnityEditor;

namespace BC.Editor.ActionSystem
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
            AddStep(targets, listPropertyPath, stepType, -1);
        }

        internal static void AddStep(UnityEngine.Object[] targets, string listPropertyPath, Type stepType, int insertIndex)
        {
            if (stepType == null)
                return;

            ActionAuthoringSystemDataStore.RecordStepSelection(stepType);

            // Inline and window UIs both insert relative to the current selection, so centralize the index handling here.
            ApplyListMutation(targets, listPropertyPath, "Add Action Step", listProperty =>
            {
                int targetIndex = ResolveInsertIndex(listProperty, insertIndex);
                listProperty.InsertArrayElementAtIndex(targetIndex);
                listProperty.GetArrayElementAtIndex(targetIndex).managedReferenceValue = Activator.CreateInstance(stepType);
            });
        }

        internal static void CopyStep(SerializedProperty stepProperty)
        {
            ActionStepClipboard.Copy(stepProperty);
        }

        internal static bool CanPasteStep()
        {
            return ActionStepClipboard.HasStep;
        }

        internal static void PasteStep(UnityEngine.Object[] targets, string listPropertyPath)
        {
            PasteStep(targets, listPropertyPath, -1);
        }

        internal static void PasteStep(UnityEngine.Object[] targets, string listPropertyPath, int insertIndex)
        {
            if (!CanPasteStep())
                return;

            ApplyListMutation(targets, listPropertyPath, "Paste Action Step", listProperty =>
            {
                // Paste always materializes a fresh clone so the clipboard payload stays immutable across edits.
                object clone = ActionStepClipboard.CloneStep();

                if (clone == null)
                    return;

                int targetIndex = ResolveInsertIndex(listProperty, insertIndex);
                listProperty.InsertArrayElementAtIndex(targetIndex);
                listProperty.GetArrayElementAtIndex(targetIndex).managedReferenceValue = clone;
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

        internal static void DeleteSteps(UnityEngine.Object[] targets, string listPropertyPath, IReadOnlyList<int> indices)
        {
            if (indices == null || indices.Count <= 0)
                return;

            ApplyListMutation(targets, listPropertyPath, "Delete Action Steps", listProperty =>
            {
                List<int> sortedUnique = BuildSortedUniqueIndices(indices, listProperty.arraySize);

                for (int i = sortedUnique.Count - 1; i >= 0; i--)
                    ManagedReferenceListController.DeleteElement(listProperty, sortedUnique[i]);
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

        internal static void MoveStepBetweenLists(
            UnityEngine.Object[] targets,
            string sourceListPropertyPath,
            int sourceIndex,
            string destinationListPropertyPath,
            int destinationInsertIndex)
        {
            if (targets == null ||
                string.IsNullOrWhiteSpace(sourceListPropertyPath) ||
                string.IsNullOrWhiteSpace(destinationListPropertyPath))
            {
                return;
            }

            UndoApplyUtility.ApplyToTargets(
                targets,
                "Move Action Step",
                serializedObject =>
                {
                    SerializedProperty sourceListProperty = serializedObject.FindProperty(sourceListPropertyPath);
                    SerializedProperty destinationListProperty = serializedObject.FindProperty(destinationListPropertyPath);

                    if (sourceListProperty == null || !sourceListProperty.isArray ||
                        destinationListProperty == null || !destinationListProperty.isArray)
                    {
                        return;
                    }

                    if (sourceIndex < 0 || sourceIndex >= sourceListProperty.arraySize)
                        return;

                    bool isSameList = string.Equals(
                        sourceListPropertyPath,
                        destinationListPropertyPath,
                        StringComparison.Ordinal);

                    if (isSameList)
                    {
                        int moveToIndex = ResolveInsertIndex(sourceListProperty, destinationInsertIndex);

                        if (moveToIndex > sourceIndex)
                            moveToIndex -= 1;

                        if (moveToIndex == sourceIndex)
                            return;

                        ManagedReferenceListController.MoveElement(sourceListProperty, sourceIndex, moveToIndex);
                        return;
                    }

                    object sourceStep = sourceListProperty.GetArrayElementAtIndex(sourceIndex).managedReferenceValue;
                    object clone = ManagedReferenceListController.CloneManagedReference(sourceStep);

                    int insertIndex = ResolveInsertIndex(destinationListProperty, destinationInsertIndex);
                    destinationListProperty.InsertArrayElementAtIndex(insertIndex);
                    destinationListProperty.GetArrayElementAtIndex(insertIndex).managedReferenceValue = clone;

                    ManagedReferenceListController.DeleteElement(sourceListProperty, sourceIndex);
                });
        }

        internal static void MoveStepsBetweenLists(
            UnityEngine.Object[] targets,
            string sourceListPropertyPath,
            IReadOnlyList<int> sourceIndices,
            string destinationListPropertyPath,
            int destinationInsertIndex)
        {
            if (targets == null ||
                string.IsNullOrWhiteSpace(sourceListPropertyPath) ||
                string.IsNullOrWhiteSpace(destinationListPropertyPath) ||
                sourceIndices == null ||
                sourceIndices.Count <= 0)
            {
                return;
            }

            UndoApplyUtility.ApplyToTargets(
                targets,
                "Move Action Steps",
                serializedObject =>
                {
                    SerializedProperty sourceListProperty = serializedObject.FindProperty(sourceListPropertyPath);
                    SerializedProperty destinationListProperty = serializedObject.FindProperty(destinationListPropertyPath);

                    if (sourceListProperty == null || !sourceListProperty.isArray ||
                        destinationListProperty == null || !destinationListProperty.isArray)
                    {
                        return;
                    }

                    List<int> sortedUnique = BuildSortedUniqueIndices(sourceIndices, sourceListProperty.arraySize);

                    if (sortedUnique.Count <= 0)
                        return;

                    bool isSameList = string.Equals(
                        sourceListPropertyPath,
                        destinationListPropertyPath,
                        StringComparison.Ordinal);

                    List<object> clones = new(sortedUnique.Count);

                    for (int i = 0; i < sortedUnique.Count; i++)
                    {
                        int sourceIndex = sortedUnique[i];
                        object sourceStep = sourceListProperty.GetArrayElementAtIndex(sourceIndex).managedReferenceValue;
                        clones.Add(ManagedReferenceListController.CloneManagedReference(sourceStep));
                    }

                    for (int i = sortedUnique.Count - 1; i >= 0; i--)
                        ManagedReferenceListController.DeleteElement(sourceListProperty, sortedUnique[i]);

                    int insertIndex = ResolveInsertIndex(destinationListProperty, destinationInsertIndex);

                    if (isSameList)
                    {
                        int removedBeforeInsert = 0;

                        for (int i = 0; i < sortedUnique.Count; i++)
                        {
                            if (sortedUnique[i] < insertIndex)
                                removedBeforeInsert++;
                        }

                        insertIndex = Math.Max(0, insertIndex - removedBeforeInsert);
                    }

                    for (int i = 0; i < clones.Count; i++)
                    {
                        int currentInsert = Math.Min(insertIndex + i, destinationListProperty.arraySize);
                        destinationListProperty.InsertArrayElementAtIndex(currentInsert);
                        destinationListProperty.GetArrayElementAtIndex(currentInsert).managedReferenceValue = clones[i];
                    }
                });
        }

        internal static void ClearDisplayName(UnityEngine.Object[] targets, string stepPropertyPath)
        {
            SetDisplayName(targets, stepPropertyPath, string.Empty, "Clear Action Label");
        }

        internal static string ResolveParentListPropertyPath(string stepPropertyPath)
        {
            if (string.IsNullOrWhiteSpace(stepPropertyPath))
                return string.Empty;

            // Step properties live under `<list>.Array.data[n]`; strip that suffix to target the owning list.
            int markerIndex = stepPropertyPath.LastIndexOf(".Array.data[", StringComparison.Ordinal);
            return markerIndex < 0 ? string.Empty : stepPropertyPath.Substring(0, markerIndex);
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

            // Keep every structural edit on the same Undo + multi-object editing path.
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

        private static int ResolveInsertIndex(SerializedProperty listProperty, int insertIndex)
        {
            if (listProperty == null)
                return 0;

            return insertIndex < 0
                ? listProperty.arraySize
                : Math.Min(Math.Max(insertIndex, 0), listProperty.arraySize);
        }

        private static List<int> BuildSortedUniqueIndices(IReadOnlyList<int> indices, int maxExclusive)
        {
            List<int> sortedUnique = new();

            if (indices == null || maxExclusive <= 0)
                return sortedUnique;

            HashSet<int> uniqueSet = new();

            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];

                if (index < 0 || index >= maxExclusive)
                    continue;

                if (uniqueSet.Add(index))
                    sortedUnique.Add(index);
            }

            sortedUnique.Sort();
            return sortedUnique;
        }
    }
}

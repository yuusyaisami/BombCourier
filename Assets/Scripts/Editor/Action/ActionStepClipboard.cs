using BC.ActionSystem;
using BC.Editor.Foundation.IMGUI;
using UnityEditor;
using System.Collections.Generic;

namespace BC.Editor.ActionSystem
{
    internal static class ActionStepClipboard
    {
        // Store a detached deep copy instead of a live SerializedProperty so copy/paste survives repaints and list mutations.
        private static readonly List<ActionStepAuthoring> copiedSteps = new();

        internal static bool HasStep => copiedSteps.Count > 0;

        internal static void Copy(SerializedProperty stepProperty)
        {
            copiedSteps.Clear();

            if (stepProperty?.managedReferenceValue is not ActionStepAuthoring step)
                return;

            // Keep the clipboard decoupled from the live SerializedProperty instance.
            if (ManagedReferenceListController.CloneManagedReference(step) is ActionStepAuthoring clone)
                copiedSteps.Add(clone);
        }

        internal static void Copy(IReadOnlyList<SerializedProperty> stepProperties)
        {
            copiedSteps.Clear();

            if (stepProperties == null || stepProperties.Count <= 0)
                return;

            for (int i = 0; i < stepProperties.Count; i++)
            {
                SerializedProperty stepProperty = stepProperties[i];

                if (stepProperty?.managedReferenceValue is not ActionStepAuthoring step)
                    continue;

                if (ManagedReferenceListController.CloneManagedReference(step) is ActionStepAuthoring clone)
                    copiedSteps.Add(clone);
            }
        }

        internal static object CloneStep()
        {
            return copiedSteps.Count <= 0
                ? null
                : ManagedReferenceListController.CloneManagedReference(copiedSteps[0]);
        }

        internal static List<object> CloneSteps()
        {
            List<object> clones = new(copiedSteps.Count);

            for (int i = 0; i < copiedSteps.Count; i++)
            {
                object clone = ManagedReferenceListController.CloneManagedReference(copiedSteps[i]);

                if (clone != null)
                    clones.Add(clone);
            }

            return clones;
        }

        internal static void Clear()
        {
            copiedSteps.Clear();
        }
    }
}
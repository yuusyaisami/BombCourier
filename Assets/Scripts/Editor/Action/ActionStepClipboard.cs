using BC.ActionSystem;
using BC.Editor.Foundation.IMGUI;
using UnityEditor;

namespace BC.Editor.ActionSystem
{
    internal static class ActionStepClipboard
    {
        // Store a detached deep copy instead of a live SerializedProperty so copy/paste survives repaints and list mutations.
        private static ActionStepAuthoring copiedStep;

        internal static bool HasStep => copiedStep != null;

        internal static void Copy(SerializedProperty stepProperty)
        {
            if (stepProperty?.managedReferenceValue is not ActionStepAuthoring step)
                return;

            // Keep the clipboard decoupled from the live SerializedProperty instance.
            copiedStep = ManagedReferenceListController.CloneManagedReference(step) as ActionStepAuthoring;
        }

        internal static object CloneStep()
        {
            return copiedStep == null
                ? null
                : ManagedReferenceListController.CloneManagedReference(copiedStep);
        }

        internal static void Clear()
        {
            copiedStep = null;
        }
    }
}
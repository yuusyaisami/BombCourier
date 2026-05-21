using System;
using BC.Editor.Foundation;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BC.Editor.Foundation.Pickers
{
    public static class PickerApplyUtility
    {
        public static void ApplySelection(
            Object[] targets,
            string propertyPath,
            string undoName,
            Action<SerializedProperty> applyToProperty)
        {
            if (targets == null || string.IsNullOrWhiteSpace(propertyPath) || applyToProperty == null)
                return;

            UndoApplyUtility.ApplyToTargets(
                targets,
                undoName,
                serializedObject =>
                {
                    SerializedProperty property = serializedObject.FindProperty(propertyPath);

                    if (property != null)
                        applyToProperty(property);
                });
        }
    }
}

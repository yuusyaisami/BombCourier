using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BC.Editor.Foundation
{
    public static class UndoApplyUtility
    {
        public static void RecordTargets(SerializedObject serializedObject, string undoName)
        {
            if (serializedObject == null)
                return;

            RecordTargets(serializedObject.targetObjects, undoName);
        }

        public static void RecordTargets(Object[] targets, string undoName)
        {
            if (targets == null || targets.Length == 0)
                return;

            Undo.RecordObjects(targets, string.IsNullOrWhiteSpace(undoName) ? "Edit" : undoName);
        }

        public static bool ApplyModifiedProperties(
            SerializedObject serializedObject,
            bool recordPrefabOverrides = true,
            bool markDirty = true)
        {
            if (serializedObject == null)
                return false;

            bool changed = serializedObject.ApplyModifiedProperties();

            if (changed)
                ApplyObjectState(serializedObject.targetObjects, recordPrefabOverrides, markDirty);

            return changed;
        }

        public static void ApplyToTargets(
            Object[] targets,
            string undoName,
            Action<SerializedObject> mutate,
            bool recordPrefabOverrides = true,
            bool markDirty = true)
        {
            if (targets == null || mutate == null)
                return;

            for (int i = 0; i < targets.Length; i++)
            {
                Object target = targets[i];

                if (target == null)
                    continue;

                Undo.RecordObject(target, string.IsNullOrWhiteSpace(undoName) ? "Edit" : undoName);

                SerializedObject serializedObject = new(target);
                serializedObject.UpdateIfRequiredOrScript();
                mutate(serializedObject);
                serializedObject.ApplyModifiedProperties();

                ApplyObjectState(new[] { target }, recordPrefabOverrides, markDirty);
            }
        }

        public static void ApplyObjectState(Object[] targets, bool recordPrefabOverrides = true, bool markDirty = true)
        {
            if (targets == null)
                return;

            for (int i = 0; i < targets.Length; i++)
            {
                Object target = targets[i];

                if (target == null)
                    continue;

                if (recordPrefabOverrides)
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);

                if (markDirty)
                    EditorUtility.SetDirty(target);
            }
        }
    }
}

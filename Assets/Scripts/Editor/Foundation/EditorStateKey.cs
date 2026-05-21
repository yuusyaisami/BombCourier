using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BC.Editor.Foundation
{
    public static class EditorStateKey
    {
        public static string ForSerializedObject(SerializedObject serializedObject, string rootPropertyPath, string suffix = null)
        {
            if (serializedObject == null)
                return Build(0, rootPropertyPath, null, suffix);

            Object target = serializedObject.targetObject;
            return Build(GetStableObjectId(target), rootPropertyPath, null, suffix);
        }

        public static string ForProperty(SerializedProperty property, string suffix = null)
        {
            if (property == null)
                return Build(0, null, null, suffix);

            Object target = property.serializedObject?.targetObject;
            long? managedReferenceId = TryGetManagedReferenceId(property, out long id) ? id : (long?)null;
            return Build(GetStableObjectId(target), property.propertyPath, managedReferenceId, suffix);
        }

        public static string Build(int targetInstanceId, string rootPropertyPath, long? managedReferenceId = null, string suffix = null)
        {
            return Build(targetInstanceId.ToString(), rootPropertyPath, managedReferenceId, suffix);
        }

        public static string Build(string targetId, string rootPropertyPath, long? managedReferenceId = null, string suffix = null)
        {
            string path = string.IsNullOrWhiteSpace(rootPropertyPath) ? "<root>" : rootPropertyPath;
            string reference = managedReferenceId.HasValue ? $"|ref:{managedReferenceId.Value}" : string.Empty;
            string tail = string.IsNullOrWhiteSpace(suffix) ? string.Empty : $"|{suffix}";
            return $"{targetId ?? "0"}:{path}{reference}{tail}";
        }

        private static string GetStableObjectId(Object target)
        {
            if (target == null)
                return "0";

#pragma warning disable CS0618
            return target.GetInstanceID().ToString();
#pragma warning restore CS0618
        }

        private static bool TryGetManagedReferenceId(SerializedProperty property, out long managedReferenceId)
        {
            managedReferenceId = 0;

            if (property == null || property.propertyType != SerializedPropertyType.ManagedReference)
                return false;

            managedReferenceId = property.managedReferenceId;
            return managedReferenceId != 0;
        }
    }
}

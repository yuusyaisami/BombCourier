using System;
using UnityEditor;

namespace BC.Editor.Foundation
{
    public static class SerializedPropertyPathUtility
    {
        public static string Combine(string parentPath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(parentPath))
                return relativePath ?? string.Empty;

            if (string.IsNullOrWhiteSpace(relativePath))
                return parentPath;

            return $"{parentPath}.{relativePath}";
        }

        public static bool TryFindRelative(SerializedProperty property, string relativePath, out SerializedProperty relativeProperty)
        {
            relativeProperty = property == null || string.IsNullOrWhiteSpace(relativePath)
                ? null
                : property.FindPropertyRelative(relativePath);

            return relativeProperty != null;
        }

        public static bool TryFindParent(SerializedObject serializedObject, SerializedProperty property, out SerializedProperty parentProperty)
        {
            parentProperty = null;

            if (serializedObject == null || property == null)
                return false;

            string parentPath = GetParentPath(property.propertyPath);

            if (string.IsNullOrWhiteSpace(parentPath))
                return false;

            parentProperty = serializedObject.FindProperty(parentPath);
            return parentProperty != null;
        }

        public static string GetParentPath(string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath))
                return string.Empty;

            int lastDotIndex = propertyPath.LastIndexOf(".");
            return lastDotIndex <= 0 ? string.Empty : propertyPath.Substring(0, lastDotIndex);
        }

        public static bool IsArrayElement(SerializedProperty property)
        {
            return property != null && IsArrayElementPath(property.propertyPath);
        }

        public static bool IsArrayElementPath(string propertyPath)
        {
            return !string.IsNullOrWhiteSpace(propertyPath) &&
                   propertyPath.IndexOf(".Array.data[", StringComparison.Ordinal) >= 0;
        }

        public static bool TryGetArrayElementIndex(SerializedProperty property, out int index)
        {
            index = -1;

            if (property == null)
                return false;

            string path = property.propertyPath;
            int markerIndex = path.LastIndexOf(".Array.data[", StringComparison.Ordinal);

            if (markerIndex < 0)
                return false;

            int startIndex = markerIndex + ".Array.data[".Length;
            int endIndex = path.IndexOf(']', startIndex);

            return endIndex > startIndex &&
                   int.TryParse(path.Substring(startIndex, endIndex - startIndex), out index);
        }

        public static bool IsManagedReference(SerializedProperty property)
        {
            return property != null && property.propertyType == SerializedPropertyType.ManagedReference;
        }

        public static string ToDisplayLabel(string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath))
                return "(None)";

            string normalized = propertyPath.Replace(".Array.data[", "[");

            int lastDotIndex = normalized.LastIndexOf(".");
            return lastDotIndex >= 0 ? normalized.Substring(lastDotIndex + 1) : normalized;
        }
    }
}

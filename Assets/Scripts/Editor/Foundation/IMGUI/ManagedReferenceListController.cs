using System;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Foundation.IMGUI
{
    public static class ManagedReferenceListController
    {
        public static bool CanUse(SerializedProperty listProperty)
        {
            return listProperty != null && listProperty.isArray;
        }

        public static SerializedProperty AddNewElement(SerializedProperty listProperty, Type elementType)
        {
            if (!CanUse(listProperty))
                throw new ArgumentException("Property must be an array/list.", nameof(listProperty));

            if (elementType == null)
                throw new ArgumentNullException(nameof(elementType));

            object value = Activator.CreateInstance(elementType);
            int index = listProperty.arraySize;
            listProperty.InsertArrayElementAtIndex(index);
            SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
            element.managedReferenceValue = value;
            return element;
        }

        public static SerializedProperty DuplicateElement(SerializedProperty listProperty, int index)
        {
            if (!CanUse(listProperty))
                throw new ArgumentException("Property must be an array/list.", nameof(listProperty));

            SerializedProperty source = listProperty.GetArrayElementAtIndex(index);
            object clone = CloneManagedReference(source.managedReferenceValue);
            int insertIndex = index + 1;
            listProperty.InsertArrayElementAtIndex(insertIndex);
            SerializedProperty target = listProperty.GetArrayElementAtIndex(insertIndex);
            target.managedReferenceValue = clone;
            return target;
        }

        public static void DeleteElement(SerializedProperty listProperty, int index)
        {
            if (!CanUse(listProperty))
                throw new ArgumentException("Property must be an array/list.", nameof(listProperty));

            listProperty.DeleteArrayElementAtIndex(index);
        }

        public static void MoveElement(SerializedProperty listProperty, int sourceIndex, int destinationIndex)
        {
            if (!CanUse(listProperty))
                throw new ArgumentException("Property must be an array/list.", nameof(listProperty));

            if (sourceIndex == destinationIndex)
                return;

            listProperty.MoveArrayElement(sourceIndex, destinationIndex);
        }

        public static object CloneManagedReference(object source)
        {
            if (source == null)
                return null;

            Type type = source.GetType();
            object clone = Activator.CreateInstance(type);
            string json = JsonUtility.ToJson(source);
            JsonUtility.FromJsonOverwrite(json, clone);
            return clone;
        }
    }
}

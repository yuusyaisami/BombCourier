using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BC.Editor.Foundation
{
    public sealed class EditorBindingContext
    {
        public EditorBindingContext(
            SerializedObject serializedObject,
            string rootPropertyPath,
            string ownerLabel = null,
            string stateKey = null)
        {
            SerializedObject = serializedObject ?? throw new ArgumentNullException(nameof(serializedObject));
            RootPropertyPath = rootPropertyPath ?? string.Empty;
            OwnerLabel = ownerLabel ?? string.Empty;
            StateKey = string.IsNullOrWhiteSpace(stateKey)
                ? EditorStateKey.ForSerializedObject(serializedObject, rootPropertyPath)
                : stateKey;
        }

        public SerializedObject SerializedObject { get; }
        public string RootPropertyPath { get; }
        public string OwnerLabel { get; }
        public string StateKey { get; }
        public Object[] TargetObjects => SerializedObject.targetObjects;
        public Object TargetObject => SerializedObject.targetObject;

        public bool TryFindRootProperty(out SerializedProperty property)
        {
            SerializedObject.UpdateIfRequiredOrScript();
            property = string.IsNullOrWhiteSpace(RootPropertyPath)
                ? null
                : SerializedObject.FindProperty(RootPropertyPath);
            return property != null;
        }

        public SerializedProperty FindRootProperty()
        {
            if (TryFindRootProperty(out SerializedProperty property))
                return property;

            throw new InvalidOperationException($"Root property not found: {RootPropertyPath}");
        }

        public void Update()
        {
            SerializedObject.UpdateIfRequiredOrScript();
        }
    }
}

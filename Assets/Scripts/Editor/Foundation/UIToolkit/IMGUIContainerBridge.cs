using System;
using BC.Editor.Foundation;
using UnityEditor;
using UnityEngine.UIElements;

namespace BC.Editor.Foundation.UIToolkit
{
    public sealed class IMGUIContainerBridge
    {
        private readonly IMGUIContainer container;
        private SerializedObject serializedObject;
        private string propertyPath;
        private string emptyMessage;

        public IMGUIContainerBridge(string emptyMessage = null)
        {
            this.emptyMessage = string.IsNullOrWhiteSpace(emptyMessage)
                ? "Select an item to edit."
                : emptyMessage;

            container = new IMGUIContainer(Draw);
            container.style.flexGrow = 1f;
        }

        public VisualElement Root => container;

        public void Bind(SerializedObject sourceSerializedObject, string sourcePropertyPath)
        {
            serializedObject = sourceSerializedObject;
            propertyPath = sourcePropertyPath;
            container.MarkDirtyRepaint();
        }

        public void Clear(string message = null)
        {
            serializedObject = null;
            propertyPath = null;

            if (!string.IsNullOrWhiteSpace(message))
                emptyMessage = message;

            container.MarkDirtyRepaint();
        }

        private void Draw()
        {
            if (serializedObject == null || string.IsNullOrWhiteSpace(propertyPath))
            {
                EditorGUILayout.HelpBox(emptyMessage, MessageType.Info);
                return;
            }

            try
            {
                // Tree mutations can invalidate the previously bound property mid-frame, so re-resolve it on every draw.
                serializedObject.UpdateIfRequiredOrScript();
                SerializedProperty property = serializedObject.FindProperty(propertyPath);

                if (property == null)
                {
                    EditorGUILayout.HelpBox($"Property not found: {propertyPath}", MessageType.Warning);
                    return;
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(property, true);

                if (EditorGUI.EndChangeCheck())
                    UndoApplyUtility.ApplyModifiedProperties(serializedObject);
            }
            catch (ObjectDisposedException)
            {
                // When the selected row was deleted, degrade to an empty-state message instead of spamming IMGUI exceptions.
                serializedObject = null;
                propertyPath = null;
                EditorGUILayout.HelpBox("The selected item changed. Select it again.", MessageType.Info);
            }
        }
    }
}

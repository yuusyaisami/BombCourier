using UnityEditor;
using Object = UnityEngine.Object;

namespace BC.Editor.Foundation.UIToolkit
{
    public sealed class SerializedObjectBridge
    {
        private Object target;
        private SerializedObject serializedObject;

        public int TargetInstanceId { get; private set; }
        public string PropertyPath { get; private set; }
        public SerializedObject SerializedObject => serializedObject;
        public bool IsBound => TargetInstanceId != 0 && !string.IsNullOrWhiteSpace(PropertyPath);

        public void Bind(Object sourceTarget, string sourcePropertyPath)
        {
            target = sourceTarget;
#pragma warning disable CS0618
            TargetInstanceId = sourceTarget != null ? sourceTarget.GetInstanceID() : 0;
#pragma warning restore CS0618
            PropertyPath = sourcePropertyPath ?? string.Empty;
            serializedObject = sourceTarget != null ? new SerializedObject(sourceTarget) : null;
        }

        public bool TryGetProperty(out SerializedProperty property)
        {
            property = null;

            if (!TryResolveTarget(out Object resolvedTarget))
                return false;

            if (serializedObject == null || serializedObject.targetObject != resolvedTarget)
                serializedObject = new SerializedObject(resolvedTarget);

            serializedObject.UpdateIfRequiredOrScript();
            property = serializedObject.FindProperty(PropertyPath);
            return property != null;
        }

        public bool TryResolveTarget(out Object resolvedTarget)
        {
            if (target != null)
            {
                resolvedTarget = target;
                return true;
            }

            if (TargetInstanceId != 0)
            {
#pragma warning disable CS0618
                resolvedTarget = EditorUtility.InstanceIDToObject(TargetInstanceId);
#pragma warning restore CS0618
            }
            else
            {
                resolvedTarget = null;
            }

            target = resolvedTarget;
            return resolvedTarget != null;
        }
    }
}

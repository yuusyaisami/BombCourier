using BC.Editor.Foundation;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BC.Editor.ActionSystem
{
    internal readonly struct ActionInlineWindowLaunchRequest
    {
        public ActionInlineWindowLaunchRequest(int targetInstanceId, string propertyPath, string bindingKey)
        {
            TargetInstanceId = targetInstanceId;
            PropertyPath = propertyPath ?? string.Empty;
            BindingKey = bindingKey ?? string.Empty;
        }

        public int TargetInstanceId { get; }
        public string PropertyPath { get; }
        public string BindingKey { get; }

        public bool IsValid => TargetInstanceId != 0 && !string.IsNullOrWhiteSpace(PropertyPath);

        public Object ResolveTarget()
        {
#pragma warning disable CS0618
            return TargetInstanceId != 0 ? EditorUtility.InstanceIDToObject(TargetInstanceId) : null;
#pragma warning restore CS0618
        }
    }

    internal static class ActionInlineWindowLauncher
    {
        private const string TargetKey = "BC.Editor.ActionSystem.ActionInlineWindowLauncher.TargetInstanceId";
        private const string PropertyPathKey = "BC.Editor.ActionSystem.ActionInlineWindowLauncher.PropertyPath";
        private const string BindingKeyKey = "BC.Editor.ActionSystem.ActionInlineWindowLauncher.BindingKey";

        internal static bool CanLaunch(SerializedProperty inlineActionProperty)
        {
            return inlineActionProperty?.serializedObject?.targetObject != null &&
                   !string.IsNullOrWhiteSpace(inlineActionProperty.propertyPath);
        }

        internal static void Launch(SerializedProperty inlineActionProperty)
        {
            if (!CanLaunch(inlineActionProperty))
                return;

#pragma warning disable CS0618
            int targetInstanceId = inlineActionProperty.serializedObject.targetObject.GetInstanceID();
#pragma warning restore CS0618
            string propertyPath = inlineActionProperty.propertyPath;
            string bindingKey = EditorStateKey.ForProperty(inlineActionProperty, "window");

            SessionState.SetInt(TargetKey, targetInstanceId);
            SessionState.SetString(PropertyPathKey, propertyPath);
            SessionState.SetString(BindingKeyKey, bindingKey);
        }

        internal static void LaunchAndOpen(SerializedProperty inlineActionProperty)
        {
            if (!CanLaunch(inlineActionProperty))
                return;

            Launch(inlineActionProperty);

            if (TryGetLastRequest(out ActionInlineWindowLaunchRequest request))
                ActionInlineWindow.Open(request);
        }

        internal static bool TryGetLastRequest(out ActionInlineWindowLaunchRequest request)
        {
            int targetInstanceId = SessionState.GetInt(TargetKey, 0);
            string propertyPath = SessionState.GetString(PropertyPathKey, string.Empty);
            string bindingKey = SessionState.GetString(BindingKeyKey, string.Empty);
            request = new ActionInlineWindowLaunchRequest(targetInstanceId, propertyPath, bindingKey);
            return request.IsValid;
        }

        internal static void ClearLastRequest()
        {
            SessionState.SetInt(TargetKey, 0);
            SessionState.EraseString(PropertyPathKey);
            SessionState.EraseString(BindingKeyKey);
        }
    }
}

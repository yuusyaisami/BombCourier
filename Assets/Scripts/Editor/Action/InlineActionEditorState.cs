using System;
using BC.Editor.Foundation;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Action
{
    internal static class InlineActionEditorState
    {
        private const string ActiveRenameKey = "BC.Editor.Action.InlineAction.ActiveRename";
        private const string RenameTextPrefix = "BC.Editor.Action.InlineAction.RenameText.";
        private const string RenameFocusPrefix = "BC.Editor.Action.InlineAction.RenameFocus.";
        private const string RenameFocusedPrefix = "BC.Editor.Action.InlineAction.RenameFocused.";

        internal static bool IsExpanded(SerializedProperty stepProperty)
        {
            return SessionState.GetBool(GetExpandedStateKey(stepProperty), false);
        }

        internal static void SetExpanded(SerializedProperty stepProperty, bool expanded)
        {
            SessionState.SetBool(GetExpandedStateKey(stepProperty), expanded);
        }

        internal static bool IsRenameActive(SerializedProperty stepProperty)
        {
            return string.Equals(
                SessionState.GetString(ActiveRenameKey, string.Empty),
                GetRenameRowKey(stepProperty),
                StringComparison.Ordinal);
        }

        internal static void BeginRename(SerializedProperty stepProperty)
        {
            if (stepProperty == null)
                return;

            string rowKey = GetRenameRowKey(stepProperty);
            string currentDisplayName = stepProperty.FindPropertyRelative("DisplayName")?.stringValue ?? string.Empty;
            BeginRename(rowKey, currentDisplayName);
        }

        internal static void BeginRename(string rowKey, string currentDisplayName)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
                return;

            SessionState.SetString(ActiveRenameKey, rowKey);
            SessionState.SetString(GetRenameTextKey(rowKey), currentDisplayName ?? string.Empty);
            SessionState.SetBool(GetRenameFocusKey(rowKey), true);
            SessionState.SetBool(GetRenameFocusedKey(rowKey), false);
        }

        internal static void CancelRename(SerializedProperty stepProperty)
        {
            if (stepProperty == null)
                return;

            CancelRename(GetRenameRowKey(stepProperty));
        }

        internal static void CancelRename(string rowKey)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
                return;

            if (string.Equals(SessionState.GetString(ActiveRenameKey, string.Empty), rowKey, StringComparison.Ordinal))
                SessionState.SetString(ActiveRenameKey, string.Empty);

            SessionState.EraseString(GetRenameTextKey(rowKey));
            SessionState.SetBool(GetRenameFocusKey(rowKey), false);
            SessionState.SetBool(GetRenameFocusedKey(rowKey), false);
        }

        internal static void CommitRename(SerializedProperty stepProperty)
        {
            if (stepProperty == null)
                return;

            ActionStepManagedReferenceUtility.SetDisplayName(
                stepProperty.serializedObject.targetObjects,
                stepProperty.propertyPath,
                GetRenameText(stepProperty));

            CancelRename(stepProperty);
        }

        internal static string GetRenameText(SerializedProperty stepProperty)
        {
            return SessionState.GetString(
                GetRenameTextKey(stepProperty),
                stepProperty?.FindPropertyRelative("DisplayName")?.stringValue ?? string.Empty);
        }

        internal static void SetRenameText(SerializedProperty stepProperty, string value)
        {
            if (stepProperty == null)
                return;

            SessionState.SetString(GetRenameTextKey(stepProperty), value ?? string.Empty);
        }

        internal static bool ConsumeRenameFocus(SerializedProperty stepProperty)
        {
            if (stepProperty == null)
                return false;

            string key = GetRenameFocusKey(stepProperty);
            bool shouldFocus = SessionState.GetBool(key, false);

            if (shouldFocus)
                SessionState.SetBool(key, false);

            return shouldFocus;
        }

        internal static bool ShouldCommitRenameOnFocusLoss(
            SerializedProperty stepProperty,
            bool isFocused,
            EventType eventType)
        {
            if (stepProperty == null)
                return false;

            string focusedStateKey = GetRenameFocusedKey(stepProperty);
            bool hadFocus = SessionState.GetBool(focusedStateKey, false);

            if (isFocused)
            {
                SessionState.SetBool(focusedStateKey, true);
                return false;
            }

            return hadFocus && eventType != EventType.Layout;
        }

        internal static string GetRenameControlName(SerializedProperty stepProperty)
        {
            return $"InlineActionRename_{GetRenameRowKey(stepProperty)}";
        }

        private static string GetExpandedStateKey(SerializedProperty stepProperty)
        {
            return EditorStateKey.ForProperty(stepProperty, "foldout");
        }

        internal static string GetRenameRowKey(SerializedProperty stepProperty)
        {
            return EditorStateKey.ForProperty(stepProperty, "rename");
        }

        private static string GetRenameTextKey(SerializedProperty stepProperty)
        {
            return GetRenameTextKey(GetRenameRowKey(stepProperty));
        }

        private static string GetRenameTextKey(string rowKey)
        {
            return RenameTextPrefix + rowKey;
        }

        private static string GetRenameFocusKey(SerializedProperty stepProperty)
        {
            return GetRenameFocusKey(GetRenameRowKey(stepProperty));
        }

        private static string GetRenameFocusKey(string rowKey)
        {
            return RenameFocusPrefix + rowKey;
        }

        private static string GetRenameFocusedKey(SerializedProperty stepProperty)
        {
            return GetRenameFocusedKey(GetRenameRowKey(stepProperty));
        }

        private static string GetRenameFocusedKey(string rowKey)
        {
            return RenameFocusedPrefix + rowKey;
        }
    }
}

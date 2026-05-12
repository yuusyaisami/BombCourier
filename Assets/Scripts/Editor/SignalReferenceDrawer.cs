using System;
using System.Collections.Generic;
using System.Reflection;
using BC.Base;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(KernelSignalReference))]
    public sealed class KernelSignalReferenceDrawer : SignalReferenceDrawerBase
    {
    }

    [CustomPropertyDrawer(typeof(EntitySignalReference))]
    public sealed class EntitySignalReferenceDrawer : SignalReferenceDrawerBase
    {
    }

    public abstract class SignalReferenceDrawerBase : PropertyDrawer
    {
        private static readonly Dictionary<string, AdvancedDropdownState> DropdownStates = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return TryGetRegistryError(out _) ?
                (EditorGUIUtility.singleLineHeight * 2f) + EditorGUIUtility.standardVerticalSpacing :
                EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (TryGetRegistryError(out string errorMessage))
            {
                EditorGUI.HelpBox(position, $"Signal registry error:\n{errorMessage}", MessageType.Error);
                return;
            }

            SerializedProperty idProperty = property.FindPropertyRelative("id");
            SerializedProperty pathProperty = property.FindPropertyRelative("path");

            if (idProperty == null || pathProperty == null)
            {
                EditorGUI.LabelField(position, label.text, "Signal reference fields are missing.");
                return;
            }

            SignalDropdownAttribute dropdownAttribute =
                fieldInfo?.GetCustomAttribute<SignalDropdownAttribute>(true);

            string pathPrefix = dropdownAttribute?.PathPrefix;
            bool allowNone = dropdownAttribute?.AllowNone ?? true;
            IReadOnlyList<SignalDescriptor> descriptors = SignalRegistry.GetDescriptors(pathPrefix);

            bool hasResolvedDescriptor = TryResolveCurrentDescriptor(
                idProperty.intValue,
                pathProperty.stringValue,
                out SignalDescriptor currentDescriptor);

            bool isCompatibleSelection = hasResolvedDescriptor && ContainsDescriptor(descriptors, currentDescriptor);

            Rect fieldRect = EditorGUI.PrefixLabel(position, label);
            GUIContent buttonContent = BuildButtonContent(
                hasResolvedDescriptor,
                isCompatibleSelection,
                currentDescriptor,
                idProperty.intValue,
                pathProperty.stringValue,
                pathPrefix,
                descriptors.Count);

            using (new EditorGUI.DisabledScope(descriptors.Count == 0 && !allowNone))
            {
                if (EditorGUI.DropdownButton(fieldRect, buttonContent, FocusType.Keyboard, EditorStyles.popup))
                {
                    ShowDropdown(
                        fieldRect,
                        property.serializedObject.targetObjects,
                        property.propertyPath,
                        descriptors,
                        hasResolvedDescriptor && isCompatibleSelection ? currentDescriptor.Id.Value : (int?)null,
                        allowNone,
                        pathPrefix);
                }
            }
        }

        private static GUIContent BuildButtonContent(
            bool hasResolvedDescriptor,
            bool isCompatibleSelection,
            SignalDescriptor currentDescriptor,
            int currentId,
            string currentPath,
            string pathPrefix,
            int availableCount)
        {
            if (hasResolvedDescriptor && isCompatibleSelection)
                return new GUIContent(currentDescriptor.DisplayName, BuildDescriptorTooltip(currentDescriptor));

            if (hasResolvedDescriptor)
            {
                return new GUIContent(
                    $"Incompatible: {currentDescriptor.DisplayName}",
                    $"Current selection does not match this field filter.\n{BuildDescriptorTooltip(currentDescriptor)}\nExpected filter: {BuildFilterSummary(pathPrefix, availableCount)}");
            }

            if (currentId != 0 || !string.IsNullOrEmpty(currentPath))
            {
                string fallbackPath = string.IsNullOrEmpty(currentPath) ? $"Id={currentId}" : currentPath;
                return new GUIContent(
                    $"Missing: {fallbackPath}",
                    $"Stored Signal no longer exists in the registry.\nPath: {fallbackPath}\nExpected filter: {BuildFilterSummary(pathPrefix, availableCount)}");
            }

            return new GUIContent(
                "None",
                $"No Signal is assigned.\nAvailable options: {BuildFilterSummary(pathPrefix, availableCount)}");
        }

        private static string BuildDescriptorTooltip(SignalDescriptor descriptor)
        {
            return $"Path: {descriptor.Path}\nId: {descriptor.Id}";
        }

        private static string BuildFilterSummary(string pathPrefix, int availableCount)
        {
            string pathSummary = string.IsNullOrWhiteSpace(pathPrefix) ? "Any" : pathPrefix.Replace('/', '.');
            return $"Path={pathSummary}, Count={availableCount}";
        }

        private static bool TryResolveCurrentDescriptor(int id, string path, out SignalDescriptor descriptor)
        {
            if (id != 0 && SignalRegistry.TryGetDescriptor(new SignalId(id), out descriptor))
                return true;

            if (!string.IsNullOrWhiteSpace(path) && SignalRegistry.TryGetDescriptor(path, out descriptor))
                return true;

            descriptor = default;
            return false;
        }

        private static bool ContainsDescriptor(IReadOnlyList<SignalDescriptor> descriptors, SignalDescriptor target)
        {
            for (int i = 0; i < descriptors.Count; i++)
            {
                if (descriptors[i].Id.Equals(target.Id))
                    return true;
            }

            return false;
        }

        private static void ShowDropdown(
            Rect fieldRect,
            UnityEngine.Object[] targets,
            string propertyPath,
            IReadOnlyList<SignalDescriptor> descriptors,
            int? currentId,
            bool allowNone,
            string pathPrefix)
        {
            string stateKey = $"{propertyPath}|{pathPrefix}|{allowNone}";

            if (!DropdownStates.TryGetValue(stateKey, out AdvancedDropdownState state))
            {
                state = new AdvancedDropdownState();
                DropdownStates.Add(stateKey, state);
            }

            SignalAdvancedDropdown dropdown = new(
                state,
                descriptors,
                currentId,
                allowNone,
                selectedDescriptor => ApplySelection(targets, propertyPath, selectedDescriptor));

            dropdown.Show(fieldRect);
        }

        private static void ApplySelection(
            UnityEngine.Object[] targets,
            string propertyPath,
            SignalDescriptor? selectedDescriptor)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                UnityEngine.Object target = targets[i];

                if (target == null)
                    continue;

                Undo.RecordObject(target, "Select Signal");

                SerializedObject serializedObject = new(target);
                SerializedProperty property = serializedObject.FindProperty(propertyPath);

                if (property == null)
                    continue;

                SerializedProperty idProperty = property.FindPropertyRelative("id");
                SerializedProperty pathProperty = property.FindPropertyRelative("path");

                if (idProperty == null || pathProperty == null)
                    continue;

                if (selectedDescriptor.HasValue)
                {
                    SignalDescriptor descriptor = selectedDescriptor.Value;
                    idProperty.intValue = descriptor.Id.Value;
                    pathProperty.stringValue = descriptor.Path;
                }
                else
                {
                    idProperty.intValue = 0;
                    pathProperty.stringValue = string.Empty;
                }

                serializedObject.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                EditorUtility.SetDirty(target);
            }
        }

        private static bool TryGetRegistryError(out string errorMessage)
        {
            try
            {
                _ = SignalRegistry.AllDescriptors;
                errorMessage = null;
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return true;
            }
        }

        private sealed class SignalAdvancedDropdown : AdvancedDropdown
        {
            private readonly IReadOnlyList<SignalDescriptor> descriptors;
            private readonly int? currentId;
            private readonly bool allowNone;
            private readonly Action<SignalDescriptor?> onSelected;

            public SignalAdvancedDropdown(
                AdvancedDropdownState state,
                IReadOnlyList<SignalDescriptor> descriptors,
                int? currentId,
                bool allowNone,
                Action<SignalDescriptor?> onSelected)
                : base(state)
            {
                this.descriptors = descriptors;
                this.currentId = currentId;
                this.allowNone = allowNone;
                this.onSelected = onSelected;
                minimumSize = new Vector2(360f, 360f);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                AdvancedDropdownItem root = new("Signals");

                if (allowNone)
                    root.AddChild(new SelectionItem("None", null));

                Dictionary<string, AdvancedDropdownItem> groups = new(StringComparer.Ordinal);

                for (int i = 0; i < descriptors.Count; i++)
                {
                    SignalDescriptor descriptor = descriptors[i];
                    string[] segments = descriptor.Path.Split('.');
                    AdvancedDropdownItem parent = root;
                    string currentGroupPath = string.Empty;

                    for (int segmentIndex = 0; segmentIndex < segments.Length - 1; segmentIndex++)
                    {
                        if (segmentIndex > 0)
                            currentGroupPath += ".";

                        currentGroupPath += segments[segmentIndex];

                        if (!groups.TryGetValue(currentGroupPath, out AdvancedDropdownItem group))
                        {
                            group = new AdvancedDropdownItem(segments[segmentIndex]);
                            parent.AddChild(group);
                            groups.Add(currentGroupPath, group);
                        }

                        parent = group;
                    }

                    string itemName = segments[segments.Length - 1];

                    if (currentId.HasValue && descriptor.Id.Value == currentId.Value)
                        itemName += "  (Current)";

                    parent.AddChild(new SelectionItem(itemName, descriptor));
                }

                if (!allowNone && descriptors.Count == 0)
                    root.AddChild(new AdvancedDropdownItem("No matching Signals"));

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is SelectionItem selectionItem)
                    onSelected(selectionItem.Descriptor);
            }

            private sealed class SelectionItem : AdvancedDropdownItem
            {
                public SelectionItem(string name, SignalDescriptor? descriptor)
                    : base(name)
                {
                    Descriptor = descriptor;
                }

                public SignalDescriptor? Descriptor { get; }
            }
        }
    }
}
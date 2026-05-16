using System;
using System.Collections.Generic;
using System.Reflection;
using BC.Base;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(ValueKeyReference))]
    public sealed class ValueKeyReferenceDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, AdvancedDropdownState> DropdownStates = new();

        internal static float GetFilteredPropertyHeight()
        {
            return TryGetRegistryError(out _)
                ? (EditorGUIUtility.singleLineHeight * 2f) + EditorGUIUtility.standardVerticalSpacing
                : EditorGUIUtility.singleLineHeight;
        }

        internal static void DrawFilteredDropdown(
            Rect position,
            SerializedProperty property,
            GUIContent label,
            Type filterType,
            string pathPrefix = null,
            bool allowNone = true)
        {
            if (TryGetRegistryError(out string errorMessage))
            {
                EditorGUI.HelpBox(position, $"ValueKey registry error:\n{errorMessage}", MessageType.Error);
                return;
            }

            SerializedProperty idProperty = property.FindPropertyRelative("id");
            SerializedProperty pathProperty = property.FindPropertyRelative("path");
            SerializedProperty typeNameProperty = property.FindPropertyRelative("valueTypeName");

            if (idProperty == null || pathProperty == null || typeNameProperty == null)
            {
                EditorGUI.LabelField(position, label.text, "ValueKeyReference fields are missing.");
                return;
            }

            IReadOnlyList<ValueKeyDescriptor> descriptors = ValueKeyRegistry.GetDescriptors(filterType, pathPrefix);
            bool hasResolvedDescriptor = TryResolveCurrentDescriptor(idProperty.intValue, pathProperty.stringValue, out ValueKeyDescriptor currentDescriptor);
            bool isCompatibleSelection = hasResolvedDescriptor && ContainsDescriptor(descriptors, currentDescriptor);

            Rect fieldRect = EditorGUI.PrefixLabel(position, label);
            GUIContent buttonContent = BuildButtonContent(
                hasResolvedDescriptor,
                isCompatibleSelection,
                currentDescriptor,
                idProperty.intValue,
                pathProperty.stringValue,
                typeNameProperty.stringValue,
                filterType,
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
                        filterType,
                        pathPrefix);
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetFilteredPropertyHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ValueKeyDropdownAttribute dropdownAttribute =
                fieldInfo?.GetCustomAttribute<ValueKeyDropdownAttribute>(true);

            DrawFilteredDropdown(
                position,
                property,
                label,
                dropdownAttribute?.ValueType,
                dropdownAttribute?.PathPrefix,
                dropdownAttribute?.AllowNone ?? true);
        }

        private static GUIContent BuildButtonContent(
            bool hasResolvedDescriptor,
            bool isCompatibleSelection,
            ValueKeyDescriptor currentDescriptor,
            int currentId,
            string currentPath,
            string currentTypeName,
            Type filterType,
            string pathPrefix,
            int availableCount)
        {
            if (hasResolvedDescriptor && isCompatibleSelection)
            {
                string label = filterType == null ? currentDescriptor.DisplayName : currentDescriptor.Path;
                string tooltip = BuildDescriptorTooltip(currentDescriptor);
                return new GUIContent(label, tooltip);
            }

            if (hasResolvedDescriptor)
            {
                return new GUIContent(
                    $"Incompatible: {currentDescriptor.DisplayName}",
                    $"Current selection does not match this field filter.\n{BuildDescriptorTooltip(currentDescriptor)}\nExpected filter: {BuildFilterSummary(filterType, pathPrefix, availableCount)}");
            }

            if (currentId != 0 || !string.IsNullOrEmpty(currentPath))
            {
                string fallbackPath = string.IsNullOrEmpty(currentPath) ? $"Id={currentId}" : currentPath;
                string storedType = GetStoredTypeLabel(currentTypeName);
                return new GUIContent(
                    $"Missing: {fallbackPath}",
                    $"Stored ValueKey no longer exists in the registry.\nPath: {fallbackPath}\nStored Type: {storedType}\nExpected filter: {BuildFilterSummary(filterType, pathPrefix, availableCount)}");
            }

            return new GUIContent(
                "None",
                $"No ValueKey is assigned.\nAvailable options: {BuildFilterSummary(filterType, pathPrefix, availableCount)}");
        }

        private static string BuildDescriptorTooltip(ValueKeyDescriptor descriptor)
        {
            string defaultValue = descriptor.DefaultValue != null ? descriptor.DefaultValue.ToString() : "null";
            return $"Path: {descriptor.Path}\nType: {descriptor.TypeName}\nMode: {descriptor.CompositionMode}\nDefault: {defaultValue}";
        }

        private static string BuildFilterSummary(Type filterType, string pathPrefix, int availableCount)
        {
            string typeSummary = filterType != null ? filterType.Name : "Any";
            string pathSummary = string.IsNullOrWhiteSpace(pathPrefix) ? "Any" : pathPrefix.Replace('/', '.');
            return $"Type={typeSummary}, Path={pathSummary}, Count={availableCount}";
        }

        private static string GetStoredTypeLabel(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return "Unknown";

            Type type = Type.GetType(typeName);
            return type != null ? type.Name : typeName;
        }

        private static bool TryResolveCurrentDescriptor(int id, string path, out ValueKeyDescriptor descriptor)
        {
            if (id != 0 && ValueKeyRegistry.TryGetDescriptor(new ValueKeyId(id), out descriptor))
                return true;

            if (!string.IsNullOrWhiteSpace(path) && ValueKeyRegistry.TryGetDescriptor(path, out descriptor))
                return true;

            descriptor = default;
            return false;
        }

        private static bool ContainsDescriptor(IReadOnlyList<ValueKeyDescriptor> descriptors, ValueKeyDescriptor target)
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
            IReadOnlyList<ValueKeyDescriptor> descriptors,
            int? currentId,
            bool allowNone,
            Type filterType,
            string pathPrefix)
        {
            string stateKey = $"{propertyPath}|{filterType?.AssemblyQualifiedName}|{pathPrefix}|{allowNone}";

            if (!DropdownStates.TryGetValue(stateKey, out AdvancedDropdownState state))
            {
                state = new AdvancedDropdownState();
                DropdownStates.Add(stateKey, state);
            }

            ValueKeyAdvancedDropdown dropdown = new(
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
            ValueKeyDescriptor? selectedDescriptor)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                UnityEngine.Object target = targets[i];

                if (target == null)
                    continue;

                Undo.RecordObject(target, "Select ValueKey");

                SerializedObject serializedObject = new(target);
                SerializedProperty property = serializedObject.FindProperty(propertyPath);

                if (property == null)
                    continue;

                SerializedProperty idProperty = property.FindPropertyRelative("id");
                SerializedProperty pathProperty = property.FindPropertyRelative("path");
                SerializedProperty typeNameProperty = property.FindPropertyRelative("valueTypeName");

                if (idProperty == null || pathProperty == null || typeNameProperty == null)
                    continue;

                if (selectedDescriptor.HasValue)
                {
                    ValueKeyDescriptor descriptor = selectedDescriptor.Value;
                    idProperty.intValue = descriptor.Id.Value;
                    pathProperty.stringValue = descriptor.Path;
                    typeNameProperty.stringValue = descriptor.ValueType.AssemblyQualifiedName;
                }
                else
                {
                    idProperty.intValue = 0;
                    pathProperty.stringValue = string.Empty;
                    typeNameProperty.stringValue = string.Empty;
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
                _ = ValueKeyRegistry.AllDescriptors;
                errorMessage = null;
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return true;
            }
        }

        private sealed class ValueKeyAdvancedDropdown : AdvancedDropdown
        {
            private readonly IReadOnlyList<ValueKeyDescriptor> descriptors;
            private readonly int? currentId;
            private readonly bool allowNone;
            private readonly Action<ValueKeyDescriptor?> onSelected;

            public ValueKeyAdvancedDropdown(
                AdvancedDropdownState state,
                IReadOnlyList<ValueKeyDescriptor> descriptors,
                int? currentId,
                bool allowNone,
                Action<ValueKeyDescriptor?> onSelected)
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
                AdvancedDropdownItem root = new("Value Keys");

                if (allowNone)
                {
                    root.AddChild(new SelectionItem("None", null));
                }

                Dictionary<string, AdvancedDropdownItem> groups = new(StringComparer.Ordinal);

                for (int i = 0; i < descriptors.Count; i++)
                {
                    ValueKeyDescriptor descriptor = descriptors[i];
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
                    {
                        itemName += "  (Current)";
                    }

                    parent.AddChild(new SelectionItem(itemName, descriptor));
                }

                if (!allowNone && descriptors.Count == 0)
                {
                    root.AddChild(new AdvancedDropdownItem("No matching ValueKeys"));
                }

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is SelectionItem selectionItem)
                {
                    onSelected(selectionItem.Descriptor);
                }
            }

            private sealed class SelectionItem : AdvancedDropdownItem
            {
                public SelectionItem(string name, ValueKeyDescriptor? descriptor)
                    : base(name)
                {
                    Descriptor = descriptor;
                }

                public ValueKeyDescriptor? Descriptor { get; }
            }
        }
    }
}
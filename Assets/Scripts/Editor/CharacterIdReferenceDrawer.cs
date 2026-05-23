using System;
using System.Collections.Generic;
using BC.Base;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(CharacterIdReference))]
    public sealed class CharacterIdReferenceDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, AdvancedDropdownState> DropdownStates = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return TryGetRegistryError(out _)
                ? (EditorGUIUtility.singleLineHeight * 2f) + EditorGUIUtility.standardVerticalSpacing
                : EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (TryGetRegistryError(out string errorMessage))
            {
                EditorGUI.HelpBox(position, $"CharacterId registry error:\n{errorMessage}", MessageType.Error);
                return;
            }

            SerializedProperty idProperty = property.FindPropertyRelative("id");
            SerializedProperty pathProperty = property.FindPropertyRelative("path");

            if (idProperty == null || pathProperty == null)
            {
                EditorGUI.LabelField(position, label.text, "CharacterIdReference fields are missing.");
                return;
            }

            IReadOnlyList<CharacterIdDescriptor> descriptors = CharacterIdRegistry.AllDescriptors;
            bool hasResolvedDescriptor = TryResolveCurrentDescriptor(idProperty.intValue, pathProperty.stringValue, out CharacterIdDescriptor currentDescriptor);

            Rect fieldRect = EditorGUI.PrefixLabel(position, label);
            GUIContent buttonContent = BuildButtonContent(
                hasResolvedDescriptor,
                currentDescriptor,
                idProperty.intValue,
                pathProperty.stringValue,
                descriptors.Count);

            using (new EditorGUI.DisabledScope(descriptors.Count == 0))
            {
                if (EditorGUI.DropdownButton(fieldRect, buttonContent, FocusType.Keyboard, EditorStyles.popup))
                {
                    ShowDropdown(
                        fieldRect,
                        property.serializedObject.targetObjects,
                        property.propertyPath,
                        descriptors,
                        hasResolvedDescriptor ? currentDescriptor.Id.Value : (int?)null);
                }
            }
        }

        private static GUIContent BuildButtonContent(
            bool hasResolvedDescriptor,
            CharacterIdDescriptor currentDescriptor,
            int currentId,
            string currentPath,
            int availableCount)
        {
            if (hasResolvedDescriptor)
            {
                string displayName = string.IsNullOrWhiteSpace(currentDescriptor.DisplayName)
                    ? currentDescriptor.Path
                    : currentDescriptor.DisplayName;
                return new GUIContent(displayName, $"Name: {displayName}\nPath: {currentDescriptor.Path}\nId: {currentDescriptor.Id}");
            }

            if (currentId != 0 || !string.IsNullOrEmpty(currentPath))
            {
                string fallbackPath = string.IsNullOrEmpty(currentPath) ? $"Id={currentId}" : currentPath;
                return new GUIContent(
                    $"Missing: {fallbackPath}",
                    $"Stored CharacterId no longer exists in the registry.\nPath: {fallbackPath}");
            }

            return new GUIContent("None", $"No CharacterId is assigned.\nAvailable options: {availableCount}");
        }

        private static bool TryResolveCurrentDescriptor(int id, string path, out CharacterIdDescriptor descriptor)
        {
            if (id != 0 && CharacterIdRegistry.TryGetDescriptor(new CharacterId(id), out descriptor))
                return true;

            if (!string.IsNullOrWhiteSpace(path) && CharacterIdRegistry.TryGetDescriptor(path, out descriptor))
                return true;

            descriptor = default;
            return false;
        }

        private static void ShowDropdown(
            Rect fieldRect,
            UnityEngine.Object[] targets,
            string propertyPath,
            IReadOnlyList<CharacterIdDescriptor> descriptors,
            int? currentId)
        {
            string stateKey = $"{propertyPath}|character-id";

            if (!DropdownStates.TryGetValue(stateKey, out AdvancedDropdownState state))
            {
                state = new AdvancedDropdownState();
                DropdownStates.Add(stateKey, state);
            }

            CharacterIdAdvancedDropdown dropdown = new(
                state,
                descriptors,
                currentId,
                selectedDescriptor => ApplySelection(targets, propertyPath, selectedDescriptor));

            dropdown.Show(fieldRect);
        }

        private static void ApplySelection(
            UnityEngine.Object[] targets,
            string propertyPath,
            CharacterIdDescriptor? selectedDescriptor)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                UnityEngine.Object target = targets[i];

                if (target == null)
                    continue;

                Undo.RecordObject(target, "Select CharacterId");

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
                    CharacterIdDescriptor descriptor = selectedDescriptor.Value;
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
                _ = CharacterIdRegistry.AllDescriptors;
                errorMessage = null;
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return true;
            }
        }

        private sealed class CharacterIdAdvancedDropdown : AdvancedDropdown
        {
            private readonly IReadOnlyList<CharacterIdDescriptor> descriptors;
            private readonly int? currentId;
            private readonly Action<CharacterIdDescriptor?> onSelected;

            public CharacterIdAdvancedDropdown(
                AdvancedDropdownState state,
                IReadOnlyList<CharacterIdDescriptor> descriptors,
                int? currentId,
                Action<CharacterIdDescriptor?> onSelected)
                : base(state)
            {
                this.descriptors = descriptors ?? Array.Empty<CharacterIdDescriptor>();
                this.currentId = currentId;
                this.onSelected = onSelected;
                minimumSize = new Vector2(300f, 320f);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                AdvancedDropdownItem root = new("Select Character");
                root.AddChild(new CharacterItem("None", null));

                if (descriptors.Count > 0)
                    root.AddSeparator();

                Dictionary<string, AdvancedDropdownItem> groups = new(StringComparer.Ordinal);

                for (int i = 0; i < descriptors.Count; i++)
                {
                    CharacterIdDescriptor descriptor = descriptors[i];
                    string[] segments = descriptor.Path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                    AdvancedDropdownItem parent = root;
                    string currentPath = string.Empty;

                    for (int segmentIndex = 0; segmentIndex < segments.Length - 1; segmentIndex++)
                    {
                        string segment = ObjectNames.NicifyVariableName(segments[segmentIndex]);
                        currentPath = string.IsNullOrEmpty(currentPath)
                            ? segment
                            : $"{currentPath}/{segment}";

                        if (!groups.TryGetValue(currentPath, out AdvancedDropdownItem group))
                        {
                            group = new AdvancedDropdownItem(segment);
                            groups.Add(currentPath, group);
                            parent.AddChild(group);
                        }

                        parent = group;
                    }

                    string leafName = segments.Length > 0
                        ? descriptor.DisplayName
                        : descriptor.DisplayName;

                    if (string.IsNullOrWhiteSpace(leafName))
                    {
                        leafName = segments.Length > 0
                            ? ObjectNames.NicifyVariableName(segments[^1])
                            : ObjectNames.NicifyVariableName(descriptor.Path);
                    }

                    parent.AddChild(new CharacterItem(leafName, descriptor));
                }

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is not CharacterItem characterItem)
                    return;

                onSelected?.Invoke(characterItem.Descriptor);
            }

            private sealed class CharacterItem : AdvancedDropdownItem
            {
                public CharacterItem(string name, CharacterIdDescriptor? descriptor)
                    : base(name)
                {
                    Descriptor = descriptor;
                }

                public CharacterIdDescriptor? Descriptor { get; }
            }
        }
    }
}

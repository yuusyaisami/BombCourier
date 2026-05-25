using System;
using System.Collections.Generic;
using BC.ActionSystem;
using BC.Base;
using BC.Editor.Foundation.IMGUI;
using BC.Editor.Foundation.Pickers;
using UnityEditor;
using UnityEngine;

namespace BC.Editor
{
    [CustomPropertyDrawer(typeof(ValueStoreWriteAuthoring))]
    public sealed class ValueStoreWriteAuthoringDrawer : PropertyDrawerBase
    {
        protected override float GetPropertyHeightCore(SerializedProperty property, GUIContent label)
        {
            SerializedProperty scopeProperty = property.FindPropertyRelative("storeScope");
            SerializedProperty targetProperty = property.FindPropertyRelative("target");
            SerializedProperty kindProperty = property.FindPropertyRelative("valueKind");
            SerializedProperty numericOperationProperty = property.FindPropertyRelative("numericOperation");
            SerializedProperty keyProperty = property.FindPropertyRelative("key");

            float height = GetControlDelta(LineHeight);

            if (scopeProperty != null && ValueStoreWriteScopeUtility.UsesEntityTarget((ValueStoreWriteStoreScope)scopeProperty.enumValueIndex))
                height += GetControlDelta(EditorGUI.GetPropertyHeight(targetProperty, true));

            height += GetControlDelta(LineHeight);
            height += GetControlDelta(GetKeyFieldHeight());

            if (ShouldDrawNumericOperation(kindProperty, keyProperty) && numericOperationProperty != null)
                height += GetControlDelta(LineHeight);

            height += GetControlDelta(GetValueFieldHeight(property, kindProperty, keyProperty));

            return Mathf.Max(LineHeight, height - Spacing);
        }

        protected override void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty scopeProperty = property.FindPropertyRelative("storeScope");
            SerializedProperty targetProperty = property.FindPropertyRelative("target");
            SerializedProperty kindProperty = property.FindPropertyRelative("valueKind");
            SerializedProperty numericOperationProperty = property.FindPropertyRelative("numericOperation");
            SerializedProperty keyProperty = property.FindPropertyRelative("key");

            if (scopeProperty == null || targetProperty == null || kindProperty == null || keyProperty == null)
            {
                DrawMissingField(position, label, "ValueStore write fields are missing.");
                return;
            }

            Rect contentRect = EditorGUI.IndentedRect(position);
            Rect rowRect = new(contentRect.x, contentRect.y, contentRect.width, LineHeight);

            EditorGUI.PropertyField(rowRect, scopeProperty, new GUIContent(label.text));
            rowRect.y += LineHeight + Spacing;

            if (ValueStoreWriteScopeUtility.UsesEntityTarget((ValueStoreWriteStoreScope)scopeProperty.enumValueIndex))
            {
                float targetHeight = EditorGUI.GetPropertyHeight(targetProperty, true);
                Rect targetRect = new(contentRect.x, rowRect.y, contentRect.width, targetHeight);
                EditorGUI.PropertyField(targetRect, targetProperty, new GUIContent("Target"), true);
                rowRect.y += targetHeight + Spacing;
            }

            EditorGUI.PropertyField(new Rect(contentRect.x, rowRect.y, contentRect.width, LineHeight), kindProperty, new GUIContent("Type"));
            rowRect.y += LineHeight + Spacing;

            DrawKeyDropdown(new Rect(contentRect.x, rowRect.y, contentRect.width, GetKeyFieldHeight()), keyProperty, scopeProperty, kindProperty);
            rowRect.y += GetKeyFieldHeight() + Spacing;

            if (ShouldDrawNumericOperation(kindProperty, keyProperty) && numericOperationProperty != null)
            {
                EditorGUI.PropertyField(new Rect(contentRect.x, rowRect.y, contentRect.width, LineHeight), numericOperationProperty, new GUIContent("Operation"));
                rowRect.y += LineHeight + Spacing;
            }

            DrawValueField(new Rect(contentRect.x, rowRect.y, contentRect.width, GetValueFieldHeight(property, kindProperty, keyProperty)), property, kindProperty, keyProperty);
        }

        private static bool ShouldDrawNumericOperation(SerializedProperty kindProperty, SerializedProperty keyProperty)
        {
            if (!TryResolveEffectiveKind(kindProperty, keyProperty, out ValueStoreWriteValueKind effectiveKind))
                return false;

            return effectiveKind == ValueStoreWriteValueKind.Int || effectiveKind == ValueStoreWriteValueKind.Float;
        }

        private static float GetControlDelta(float controlHeight)
        {
            return controlHeight + Spacing;
        }

        private static float GetKeyFieldHeight()
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private static float GetValueFieldHeight(
            SerializedProperty property,
            SerializedProperty kindProperty,
            SerializedProperty keyProperty)
        {
            if (!TryResolveActiveValueProperty(property, kindProperty, keyProperty, out SerializedProperty valueProperty, out string helpMessage))
                return GetHelpBoxHeight(helpMessage);

            return EditorGUI.GetPropertyHeight(valueProperty, true);
        }

        private static void DrawValueField(
            Rect position,
            SerializedProperty property,
            SerializedProperty kindProperty,
            SerializedProperty keyProperty)
        {
            if (!TryResolveActiveValueProperty(property, kindProperty, keyProperty, out SerializedProperty valueProperty, out string helpMessage))
            {
                EditorGUI.HelpBox(position, helpMessage, MessageType.Info);
                return;
            }

            EditorGUI.PropertyField(position, valueProperty, new GUIContent("Value"), true);
        }

        private static bool TryResolveActiveValueProperty(
            SerializedProperty property,
            SerializedProperty kindProperty,
            SerializedProperty keyProperty,
            out SerializedProperty valueProperty,
            out string helpMessage)
        {
            valueProperty = null;

            if (!TryResolveEffectiveKind(kindProperty, keyProperty, out ValueStoreWriteValueKind effectiveKind))
            {
                helpMessage = "Select a supported ValueKey to author the value field.";
                return false;
            }

            string propertyName = effectiveKind switch
            {
                ValueStoreWriteValueKind.Bool => "boolValue",
                ValueStoreWriteValueKind.Int => "intValue",
                ValueStoreWriteValueKind.Float => "floatValue",
                ValueStoreWriteValueKind.String => "stringValue",
                ValueStoreWriteValueKind.EntityRef => "entityValue",
                ValueStoreWriteValueKind.FaceExpressionId => "faceExpressionValue",
                ValueStoreWriteValueKind.EntityMoveState => "entityMoveStateValue",
                ValueStoreWriteValueKind.ShapeExpressionId => "shapeExpressionValue",
                _ => null,
            };

            if (string.IsNullOrEmpty(propertyName))
            {
                helpMessage = "The selected ValueKey type is not supported by this step.";
                return false;
            }

            valueProperty = property.FindPropertyRelative(propertyName);
            helpMessage = valueProperty == null ? "The active value field could not be found." : null;
            return valueProperty != null;
        }

        private static void DrawKeyDropdown(
            Rect position,
            SerializedProperty keyProperty,
            SerializedProperty scopeProperty,
            SerializedProperty kindProperty)
        {
            IReadOnlyList<ValueKeyDescriptor> descriptors = GetFilteredDescriptors(
                (ValueStoreWriteStoreScope)scopeProperty.enumValueIndex,
                (ValueStoreWriteValueKind)kindProperty.enumValueIndex);

            Rect fieldRect = EditorGUI.PrefixLabel(position, new GUIContent("Key"));
            GUIContent buttonContent = BuildKeyButtonContent(keyProperty, descriptors);

            using (new EditorGUI.DisabledScope(descriptors.Count == 0))
            {
                if (EditorGUI.DropdownButton(fieldRect, buttonContent, FocusType.Keyboard, EditorStyles.popup))
                {
                    ShowKeyMenu(keyProperty, descriptors);
                }
            }
        }

        private static IReadOnlyList<ValueKeyDescriptor> GetFilteredDescriptors(
            ValueStoreWriteStoreScope scope,
            ValueStoreWriteValueKind requestedKind)
        {
            Type filterType = ValueStoreWriteValueTypeUtility.GetValueType(requestedKind);
            List<ValueKeyDescriptor> matches = new();
            IReadOnlyList<ValueKeyDescriptor> allDescriptors = ValueKeyRegistry.AllDescriptors;

            for (int index = 0; index < allDescriptors.Count; index++)
            {
                ValueKeyDescriptor descriptor = allDescriptors[index];

                if (!ValueStoreWriteValueTypeUtility.IsSupportedDescriptor(descriptor))
                    continue;

                if (!ValueStoreWriteScopeUtility.IsKeyCompatible(scope, descriptor))
                    continue;

                if (filterType != null && descriptor.ValueType != filterType)
                    continue;

                matches.Add(descriptor);
            }

            return matches;
        }

        private static GUIContent BuildKeyButtonContent(SerializedProperty keyProperty, IReadOnlyList<ValueKeyDescriptor> descriptors)
        {
            bool hasAssignedKey = HasAssignedKey(keyProperty);

            if (TryResolveCurrentDescriptor(keyProperty, out ValueKeyDescriptor currentDescriptor))
            {
                bool isCompatible = ContainsDescriptor(descriptors, currentDescriptor);
                string label = isCompatible ? currentDescriptor.DisplayName : $"Incompatible: {currentDescriptor.DisplayName}";
                return new GUIContent(label, currentDescriptor.ToString());
            }

            if (hasAssignedKey)
                return new GUIContent($"Missing: {ReadPath(keyProperty)}");

            return new GUIContent("None");
        }

        private static bool ContainsDescriptor(IReadOnlyList<ValueKeyDescriptor> descriptors, ValueKeyDescriptor target)
        {
            for (int index = 0; index < descriptors.Count; index++)
            {
                if (descriptors[index].Id.Equals(target.Id))
                    return true;
            }

            return false;
        }

        private static void ShowKeyMenu(SerializedProperty keyProperty, IReadOnlyList<ValueKeyDescriptor> descriptors)
        {
            int currentId = ReadRawId(keyProperty);
            string currentPath = ReadPath(keyProperty);
            bool hasAssignedKey = HasAssignedKey(keyProperty);
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("None"), !hasAssignedKey, () => ApplySelection(keyProperty, null));

            if (descriptors.Count > 0)
                menu.AddSeparator(string.Empty);

            for (int index = 0; index < descriptors.Count; index++)
            {
                ValueKeyDescriptor descriptor = descriptors[index];
                string menuPath = descriptor.MenuPath.Replace('.', '/');
                bool isCurrent = currentId == descriptor.Id.Value || currentPath == descriptor.Path;
                menu.AddItem(new GUIContent(menuPath), isCurrent, () => ApplySelection(keyProperty, descriptor));
            }

            menu.ShowAsContext();
        }

        private static void ApplySelection(SerializedProperty property, ValueKeyDescriptor? selectedDescriptor)
        {
            PickerApplyUtility.ApplySelection(
                property.serializedObject.targetObjects,
                property.propertyPath,
                "Select ValueKey",
                keyProperty => ApplySelectionToProperty(keyProperty, selectedDescriptor));
        }

        private static void ApplySelectionToProperty(SerializedProperty keyProperty, ValueKeyDescriptor? selectedDescriptor)
        {
            SerializedProperty idProperty = keyProperty?.FindPropertyRelative("id");
            SerializedProperty pathProperty = keyProperty?.FindPropertyRelative("path");
            SerializedProperty typeNameProperty = keyProperty?.FindPropertyRelative("valueTypeName");

            if (idProperty == null || pathProperty == null || typeNameProperty == null)
                return;

            if (selectedDescriptor.HasValue)
            {
                ValueKeyDescriptor descriptor = selectedDescriptor.Value;
                idProperty.intValue = descriptor.Id.Value;
                pathProperty.stringValue = descriptor.Path;
                typeNameProperty.stringValue = descriptor.ValueType.AssemblyQualifiedName;
                return;
            }

            idProperty.intValue = 0;
            pathProperty.stringValue = string.Empty;
            typeNameProperty.stringValue = string.Empty;
        }

        private static bool TryResolveEffectiveKind(
            SerializedProperty kindProperty,
            SerializedProperty keyProperty,
            out ValueStoreWriteValueKind effectiveKind)
        {
            ValueStoreWriteValueKind requestedKind = (ValueStoreWriteValueKind)kindProperty.enumValueIndex;

            if (requestedKind != ValueStoreWriteValueKind.Auto)
            {
                effectiveKind = requestedKind;
                return ValueStoreWriteValueTypeUtility.GetValueType(requestedKind) != null;
            }

            if (!TryResolveCurrentDescriptor(keyProperty, out ValueKeyDescriptor descriptor) ||
                !ValueStoreWriteValueTypeUtility.TryGetKind(descriptor.ValueType, out effectiveKind))
            {
                effectiveKind = ValueStoreWriteValueKind.Auto;
                return false;
            }

            return true;
        }

        private static bool TryResolveCurrentDescriptor(SerializedProperty keyProperty, out ValueKeyDescriptor descriptor)
        {
            int rawId = ReadRawId(keyProperty);

            if (rawId != 0 && ValueKeyRegistry.TryGetDescriptor(new ValueKeyId(rawId), out descriptor))
                return true;

            string path = ReadPath(keyProperty);

            if (!string.IsNullOrWhiteSpace(path) && ValueKeyRegistry.TryGetDescriptor(path, out descriptor))
                return true;

            descriptor = default;
            return false;
        }

        private static bool HasAssignedKey(SerializedProperty keyProperty)
        {
            return ReadRawId(keyProperty) != 0 || !string.IsNullOrWhiteSpace(ReadPath(keyProperty));
        }

        private static int ReadRawId(SerializedProperty keyProperty)
        {
            SerializedProperty idProperty = keyProperty.FindPropertyRelative("id");
            return idProperty != null ? idProperty.intValue : 0;
        }

        private static string ReadPath(SerializedProperty keyProperty)
        {
            SerializedProperty pathProperty = keyProperty.FindPropertyRelative("path");
            return pathProperty != null ? pathProperty.stringValue : string.Empty;
        }

        private static float GetHelpBoxHeight(string message)
        {
            return Mathf.Max(LineHeight * 2f, EditorStyles.helpBox.CalcHeight(new GUIContent(message), EditorGUIUtility.currentViewWidth));
        }
    }
}
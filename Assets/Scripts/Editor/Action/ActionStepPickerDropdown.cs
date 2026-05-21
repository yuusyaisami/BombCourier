using System;
using System.Collections.Generic;
using BC.Editor.Foundation;
using BC.Editor.Foundation.Pickers;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Action
{
    internal static class ActionStepPickerDropdown
    {
        private static readonly PickerStateCache StateCache = new();

        internal static void Show(Rect buttonRect, SerializedProperty stepsProperty)
        {
            Show(buttonRect, stepsProperty, null);
        }

        internal static void Show(Rect buttonRect, SerializedProperty stepsProperty, Action onSelected)
        {
            if (stepsProperty == null || !stepsProperty.isArray)
                return;

            IReadOnlyList<Type> stepTypes = ActionStepManagedReferenceUtility.GetStepTypes();
            UnityEngine.Object[] targets = stepsProperty.serializedObject.targetObjects;
            string listPropertyPath = stepsProperty.propertyPath;
            string stateKey = EditorStateKey.ForSerializedObject(
                stepsProperty.serializedObject,
                listPropertyPath,
                "step-picker");

            StepTypeDropdown dropdown = new(
                StateCache.GetOrCreate(stateKey),
                stepTypes,
                stepType => ActionStepManagedReferenceUtility.AddStep(
                    targets,
                    listPropertyPath,
                    stepType),
                onSelected);

            dropdown.Show(buttonRect);
        }

        private sealed class StepTypeDropdown : AdvancedDropdownPickerBase<Type>
        {
            private readonly Action onSelected;

            public StepTypeDropdown(
                UnityEditor.IMGUI.Controls.AdvancedDropdownState state,
                IReadOnlyList<Type> descriptors,
                Action<Type> onSelected,
                Action onSelectionCompleted)
                : base(state, descriptors, onSelected)
            {
                this.onSelected = onSelectionCompleted;
                minimumSize = new Vector2(Mathf.Max(EditorThemeTokens.MinimumPickerWidth, 240f), 320f);
            }

            protected override string GetRootName()
            {
                return "Add Step";
            }

            protected override string GetDisplayName(Type descriptor)
            {
                return ActionStepManagedReferenceUtility.GetStepTypeLabel(descriptor);
            }

            protected override void HandleSelection(Type descriptor)
            {
                base.HandleSelection(descriptor);
                onSelected?.Invoke();
            }
        }
    }
}

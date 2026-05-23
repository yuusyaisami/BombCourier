using System;
using System.Collections.Generic;
using BC.Editor.Foundation;
using BC.Editor.Foundation.Pickers;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.ActionSystem
{
    internal static class ActionStepPickerDropdown
    {
        private static readonly PickerStateCache StateCache = new();

        private readonly struct StepTypeOption
        {
            internal StepTypeOption(Type stepType, string path)
            {
                StepType = stepType;
                Path = path ?? string.Empty;
            }

            internal Type StepType { get; }
            internal string Path { get; }
        }

        internal static void Show(Rect buttonRect, SerializedProperty stepsProperty)
        {
            Show(buttonRect, stepsProperty, null);
        }

        internal static void Show(Rect buttonRect, SerializedProperty stepsProperty, Action onSelected)
        {
            if (stepsProperty == null || !stepsProperty.isArray)
                return;

            IReadOnlyList<Type> stepTypes = ActionStepManagedReferenceUtility.GetStepTypes();
            IReadOnlyList<Type> recentTypes = ActionStepRecentSelectionUtility.GetRecentStepTypes();
            List<StepTypeOption> options = BuildOptions(stepTypes, recentTypes);
            UnityEngine.Object[] targets = stepsProperty.serializedObject.targetObjects;
            string listPropertyPath = stepsProperty.propertyPath;
            string stateKey = EditorStateKey.ForSerializedObject(
                stepsProperty.serializedObject,
                listPropertyPath,
                "step-picker");

            Action<StepTypeOption> handleSelection = option =>
            {
                Type stepType = option.StepType;

                ActionStepManagedReferenceUtility.AddStep(
                    targets,
                    listPropertyPath,
                    stepType);
                onSelected?.Invoke();
            };

            StepTypeDropdown dropdown = new(
                StateCache.GetOrCreate(stateKey),
                options,
                handleSelection);

            dropdown.Show(buttonRect);
        }

        private static List<StepTypeOption> BuildOptions(
            IReadOnlyList<Type> allStepTypes,
            IReadOnlyList<Type> recentStepTypes)
        {
            List<StepTypeOption> options = new();

            if (recentStepTypes != null)
            {
                for (int i = 0; i < recentStepTypes.Count; i++)
                {
                    Type stepType = recentStepTypes[i];

                    if (stepType == null)
                        continue;

                    options.Add(new StepTypeOption(stepType, ActionStepRecentSelectionUtility.RecentMenuName));
                }
            }

            if (allStepTypes != null)
            {
                for (int i = 0; i < allStepTypes.Count; i++)
                {
                    Type stepType = allStepTypes[i];

                    if (stepType == null)
                        continue;

                    options.Add(new StepTypeOption(stepType, string.Empty));
                }
            }

            return options;
        }

        private sealed class StepTypeDropdown : AdvancedDropdownPickerBase<StepTypeOption>
        {
            public StepTypeDropdown(
                UnityEditor.IMGUI.Controls.AdvancedDropdownState state,
                IReadOnlyList<StepTypeOption> descriptors,
                Action<StepTypeOption> onSelected)
                : base(state, descriptors, onSelected)
            {
                minimumSize = new Vector2(Mathf.Max(EditorThemeTokens.MinimumPickerWidth, 240f), 320f);
            }

            protected override string GetRootName()
            {
                return "Add Step";
            }

            protected override string GetDisplayName(StepTypeOption descriptor)
            {
                return ActionStepManagedReferenceUtility.GetStepTypeLabel(descriptor.StepType);
            }

            protected override string GetPath(StepTypeOption descriptor)
            {
                return descriptor.Path;
            }
        }
    }
}

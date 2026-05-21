using System;
using System.Collections.Generic;
using BC.ActionSystem;
using UnityEditor;

namespace BC.Editor.Action
{
    internal enum ActionStepBadgeKind
    {
        Info = 0,
        Warning = 1,
    }

    internal readonly struct ActionStepBadge
    {
        public ActionStepBadge(string text, ActionStepBadgeKind kind = ActionStepBadgeKind.Info)
        {
            Text = text ?? string.Empty;
            Kind = kind;
        }

        public string Text { get; }
        public ActionStepBadgeKind Kind { get; }
    }

    internal static class ActionStepChildSlotUtility
    {
        internal static IReadOnlyList<ActionStepBadge> GetBadges(SerializedProperty stepProperty)
        {
            List<ActionStepBadge> badges = new();

            if (stepProperty == null)
                return badges;

            SerializedProperty displayNameProperty = stepProperty.FindPropertyRelative("DisplayName");

            if (displayNameProperty != null && !string.IsNullOrWhiteSpace(displayNameProperty.stringValue))
                badges.Add(new ActionStepBadge("Label"));

            if (stepProperty.managedReferenceValue == null)
            {
                badges.Add(new ActionStepBadge("Missing", ActionStepBadgeKind.Warning));
                return badges;
            }

            AddTalkBadges(stepProperty, badges);
            AddChildSlotBadges(stepProperty, badges);
            return badges;
        }

        private static void AddTalkBadges(SerializedProperty stepProperty, ICollection<ActionStepBadge> badges)
        {
            SerializedProperty talkRequestDataProperty = stepProperty.FindPropertyRelative("talkRequestData");

            if (talkRequestDataProperty == null)
                return;

            if (talkRequestDataProperty.FindPropertyRelative("isWaitingActionCompleted")?.boolValue == true)
                badges.Add(new ActionStepBadge("Wait"));
        }

        private static void AddChildSlotBadges(SerializedProperty stepProperty, ICollection<ActionStepBadge> badges)
        {
            if (stepProperty.managedReferenceValue is not ActionStepAuthoring step)
                return;

            IReadOnlyList<ActionChildSlotDescriptor> childSlots = step.GetChildActionSlots();
            int declaredCount = childSlots.Count;
            int missingCount = 0;
            HashSet<string> emittedMetadataBadges = new(StringComparer.Ordinal);

            for (int i = 0; i < childSlots.Count; i++)
            {
                ActionChildSlotDescriptor childSlot = childSlots[i];
                string metadataBadge = childSlot.MetadataBadge;

                if (!string.IsNullOrWhiteSpace(metadataBadge) && emittedMetadataBadges.Add(metadataBadge))
                    badges.Add(new ActionStepBadge(metadataBadge));

                if (!childSlot.IsPresent || childSlot.Action == null)
                {
                    missingCount++;
                    continue;
                }
            }

            if (declaredCount > 0)
                badges.Add(new ActionStepBadge(declaredCount == 1 ? "1 child" : $"{declaredCount} children"));

            if (missingCount > 0)
                badges.Add(new ActionStepBadge(missingCount == 1 ? "Missing child" : $"{missingCount} missing", ActionStepBadgeKind.Warning));
        }
    }
}

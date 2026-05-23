using System;
using System.Collections.Generic;
using BC.Editor.Foundation.IMGUI;

namespace BC.Editor.ActionSystem
{
    internal static class ActionStepRecentSelectionUtility
    {
        internal const int MaxRecentItems = 5;
        internal const string RecentMenuName = "最近選んだSteps";

        internal static IReadOnlyList<Type> GetRecentStepTypes()
        {
            return ActionAuthoringSystemDataStore.GetRecentStepTypes(MaxRecentItems);
        }

        internal static void AppendRecentStepMenuItems(
            ContextMenuBuilder menu,
            string addStepMenuRoot,
            bool enabled,
            Action<Type> onSelected)
        {
            string recentRoot = string.IsNullOrWhiteSpace(addStepMenuRoot)
                ? RecentMenuName
                : $"{addStepMenuRoot}/{RecentMenuName}";

            if (menu == null)
                return;

            IReadOnlyList<Type> recentTypes = GetRecentStepTypes();

            if (!enabled || recentTypes.Count == 0)
            {
                menu.AddItem($"{recentRoot}/(なし)", false, null);
                return;
            }

            for (int i = 0; i < recentTypes.Count; i++)
            {
                Type stepType = recentTypes[i];
                string label = ActionStepManagedReferenceUtility.GetStepTypeLabel(stepType);
                menu.AddItem($"{recentRoot}/{label}", true, () => onSelected?.Invoke(stepType));
            }
        }
    }
}

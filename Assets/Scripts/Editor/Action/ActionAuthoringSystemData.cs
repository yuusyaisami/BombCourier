using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.Editor.ActionSystem
{
    internal sealed class ActionAuthoringSystemData : ScriptableObject
    {
        [Serializable]
        internal sealed class StepSelectionEntry
        {
            [SerializeField] private string stepTypeName;
            [SerializeField] private long lastSelectedUtcTicks;
            [SerializeField] private int selectedCount;

            internal string StepTypeName
            {
                get => stepTypeName;
                set => stepTypeName = value;
            }

            internal long LastSelectedUtcTicks
            {
                get => lastSelectedUtcTicks;
                set => lastSelectedUtcTicks = value;
            }

            internal int SelectedCount
            {
                get => selectedCount;
                set => selectedCount = value;
            }
        }

        [Serializable]
        internal sealed class InlineActionSettings
        {
            [SerializeField] private bool reserved;

            internal bool Reserved
            {
                get => reserved;
                set => reserved = value;
            }
        }

        [SerializeField] private List<StepSelectionEntry> stepSelectionEntries = new();
        [SerializeField] private InlineActionSettings inlineActionSettings = new();

        internal InlineActionSettings InlineSettings => inlineActionSettings;

        internal void RecordStepSelection(string stepTypeName, long selectedUtcTicks)
        {
            if (string.IsNullOrWhiteSpace(stepTypeName))
                return;

            NormalizeEntries();

            StepSelectionEntry entry = FindEntry(stepTypeName);

            if (entry == null)
            {
                entry = new StepSelectionEntry
                {
                    StepTypeName = stepTypeName,
                    LastSelectedUtcTicks = selectedUtcTicks,
                    SelectedCount = 1,
                };
                stepSelectionEntries.Add(entry);
                return;
            }

            entry.LastSelectedUtcTicks = Math.Max(selectedUtcTicks, entry.LastSelectedUtcTicks);
            entry.SelectedCount = Math.Max(0, entry.SelectedCount) + 1;
        }

        internal IReadOnlyList<StepSelectionEntry> GetRecentStepSelections(int maxCount)
        {
            NormalizeEntries();

            if (maxCount <= 0 || stepSelectionEntries.Count == 0)
                return Array.Empty<StepSelectionEntry>();

            List<StepSelectionEntry> sorted = new(stepSelectionEntries);
            sorted.Sort(CompareEntries);

            if (sorted.Count > maxCount)
                sorted.RemoveRange(maxCount, sorted.Count - maxCount);

            return sorted;
        }

        internal void NormalizeEntries()
        {
            if (stepSelectionEntries == null)
            {
                stepSelectionEntries = new List<StepSelectionEntry>();
                return;
            }

            Dictionary<string, StepSelectionEntry> merged = new(StringComparer.Ordinal);

            for (int i = 0; i < stepSelectionEntries.Count; i++)
            {
                StepSelectionEntry source = stepSelectionEntries[i];

                if (source == null || string.IsNullOrWhiteSpace(source.StepTypeName))
                    continue;

                string key = source.StepTypeName.Trim();

                if (!merged.TryGetValue(key, out StepSelectionEntry existing))
                {
                    merged.Add(key, new StepSelectionEntry
                    {
                        StepTypeName = key,
                        LastSelectedUtcTicks = source.LastSelectedUtcTicks,
                        SelectedCount = Math.Max(0, source.SelectedCount),
                    });
                    continue;
                }

                existing.LastSelectedUtcTicks = Math.Max(existing.LastSelectedUtcTicks, source.LastSelectedUtcTicks);
                existing.SelectedCount = Math.Max(0, existing.SelectedCount) + Math.Max(0, source.SelectedCount);
            }

            stepSelectionEntries.Clear();

            foreach (StepSelectionEntry mergedEntry in merged.Values)
                stepSelectionEntries.Add(mergedEntry);
        }

        private StepSelectionEntry FindEntry(string stepTypeName)
        {
            for (int i = 0; i < stepSelectionEntries.Count; i++)
            {
                StepSelectionEntry entry = stepSelectionEntries[i];

                if (entry == null)
                    continue;

                if (string.Equals(entry.StepTypeName, stepTypeName, StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }

        private static int CompareEntries(StepSelectionEntry left, StepSelectionEntry right)
        {
            long leftTicks = left?.LastSelectedUtcTicks ?? 0;
            long rightTicks = right?.LastSelectedUtcTicks ?? 0;
            int compareTicks = rightTicks.CompareTo(leftTicks);

            if (compareTicks != 0)
                return compareTicks;

            int leftCount = left?.SelectedCount ?? 0;
            int rightCount = right?.SelectedCount ?? 0;
            int compareCount = rightCount.CompareTo(leftCount);

            if (compareCount != 0)
                return compareCount;

            string leftName = left?.StepTypeName ?? string.Empty;
            string rightName = right?.StepTypeName ?? string.Empty;
            return string.Compare(leftName, rightName, StringComparison.Ordinal);
        }
    }
}

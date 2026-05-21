using UnityEngine;

namespace BC.Editor.Foundation.Pickers
{
    public static class PickerButtonContentBuilder
    {
        public static GUIContent Build(
            PickerSelectionState state,
            string currentLabel,
            string storedLabel,
            string filterSummary,
            int availableCount)
        {
            string resolvedFilter = string.IsNullOrWhiteSpace(filterSummary) ? "Any" : filterSummary;

            switch (state)
            {
                case PickerSelectionState.Current:
                    return new GUIContent(
                        string.IsNullOrWhiteSpace(currentLabel) ? "None" : currentLabel,
                        $"Current selection.\nFilter: {resolvedFilter}\nAvailable: {availableCount}");

                case PickerSelectionState.Incompatible:
                    return new GUIContent(
                        $"Incompatible: {FallbackLabel(currentLabel, storedLabel)}",
                        $"Current selection does not match this field.\nStored: {FallbackLabel(currentLabel, storedLabel)}\nFilter: {resolvedFilter}");

                case PickerSelectionState.Missing:
                    return new GUIContent(
                        $"Missing: {FallbackLabel(storedLabel, currentLabel)}",
                        $"Stored value no longer exists.\nStored: {FallbackLabel(storedLabel, currentLabel)}\nFilter: {resolvedFilter}");

                case PickerSelectionState.Empty:
                    return new GUIContent(
                        "No matching items",
                        $"No selectable items match this field.\nFilter: {resolvedFilter}");

                default:
                    return new GUIContent(
                        "None",
                        $"No value is assigned.\nFilter: {resolvedFilter}\nAvailable: {availableCount}");
            }
        }

        private static string FallbackLabel(string primary, string secondary)
        {
            if (!string.IsNullOrWhiteSpace(primary))
                return primary;

            return string.IsNullOrWhiteSpace(secondary) ? "(Unknown)" : secondary;
        }
    }
}

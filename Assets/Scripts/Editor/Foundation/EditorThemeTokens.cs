using UnityEditor;
using UnityEngine;

namespace BC.Editor.Foundation
{
    public static class EditorThemeTokens
    {
        public static float LineHeight => EditorGUIUtility.singleLineHeight;
        public static float StandardSpacing => EditorGUIUtility.standardVerticalSpacing;

        public const float CompactRowHeight = 20f;
        public const float RowHeight = 24f;
        public const float RowSpacing = 3f;
        public const float SectionSpacing = 8f;
        public const float PanePadding = 8f;
        public const float IndentWidth = 16f;
        public const float MinimumPickerWidth = 120f;
        public const float MinimumPickerHeight = 18f;
        public const float SceneLabelOffset = 0.35f;

        public static Color WindowBackground =>
            EditorGUIUtility.isProSkin ? new Color(0.13f, 0.13f, 0.13f) : new Color(0.78f, 0.78f, 0.78f);

        public static Color PanelBackground =>
            EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.88f, 0.88f, 0.88f);

        public static Color BlockBackground =>
            EditorGUIUtility.isProSkin ? new Color(0.23f, 0.24f, 0.25f) : new Color(0.82f, 0.84f, 0.86f);

        public static Color StepBackground =>
            EditorGUIUtility.isProSkin ? new Color(0.20f, 0.22f, 0.25f) : new Color(0.91f, 0.92f, 0.94f);

        public static Color BranchBackground =>
            EditorGUIUtility.isProSkin ? new Color(0.18f, 0.25f, 0.22f) : new Color(0.83f, 0.91f, 0.87f);

        public static Color MissingBackground =>
            EditorGUIUtility.isProSkin ? new Color(0.30f, 0.20f, 0.18f) : new Color(0.96f, 0.84f, 0.80f);

        public static Color TypeBadgeBackground =>
            EditorGUIUtility.isProSkin ? new Color(0.24f, 0.28f, 0.34f) : new Color(0.76f, 0.82f, 0.90f);

        public static Color WarningColor =>
            EditorGUIUtility.isProSkin ? new Color(1.0f, 0.72f, 0.28f) : new Color(0.72f, 0.42f, 0.02f);

        public static Color ErrorColor =>
            EditorGUIUtility.isProSkin ? new Color(1.0f, 0.38f, 0.34f) : new Color(0.75f, 0.12f, 0.10f);

        public static Color IncompatibleColor =>
            EditorGUIUtility.isProSkin ? new Color(0.74f, 0.58f, 1.0f) : new Color(0.38f, 0.22f, 0.66f);
    }
}

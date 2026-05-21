using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using BC.ActionSystem;
using BC.Base;
using UnityEditor;

namespace BC.Editor.Action
{
    internal static class ActionStepSummaryUtility
    {
        private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);

        internal static string GetTypeLabel(SerializedProperty stepProperty)
        {
            return ActionStepManagedReferenceUtility.GetStepTypeLabel(stepProperty?.managedReferenceValue?.GetType());
        }

        internal static string GetSummary(SerializedProperty stepProperty)
        {
            if (stepProperty == null)
                return "Unconfigured";

            SerializedProperty displayNameProperty = stepProperty.FindPropertyRelative("DisplayName");

            if (displayNameProperty != null && !string.IsNullOrWhiteSpace(displayNameProperty.stringValue))
                return Normalize(displayNameProperty.stringValue);

            object step = stepProperty.managedReferenceValue;

            if (step == null)
                return "Missing Step";

            string summary = step switch
            {
                WaitFramesStepAuthoring => BuildWaitFramesSummary(stepProperty),
                SubActionStepAuthoring => BuildSubActionSummary(stepProperty),
                IfStepAuthoring => BuildIfSummary(stepProperty),
                ShowToastStepAuthoring => BuildShowToastSummary(stepProperty),
                ShowTalkStepAuthoring => BuildShowTalkSummary(stepProperty),
                ShowTalkChoiceStepAuthoring => BuildShowTalkChoiceSummary(stepProperty),
                SetValueStoreValueStepAuthoring => BuildValueStoreSummary(stepProperty),
                _ => GetTypeLabel(stepProperty),
            };

            return string.IsNullOrWhiteSpace(summary) ? "Unconfigured" : Normalize(summary);
        }

        internal static string GetStateText(SerializedProperty stepProperty)
        {
            if (stepProperty == null)
                return string.Empty;

            List<string> states = new();
            SerializedProperty displayNameProperty = stepProperty.FindPropertyRelative("DisplayName");

            if (displayNameProperty != null && !string.IsNullOrWhiteSpace(displayNameProperty.stringValue))
                states.Add("Label");

            SerializedProperty talkRequestDataProperty = stepProperty.FindPropertyRelative("talkRequestData");

            if (talkRequestDataProperty != null)
            {
                if (talkRequestDataProperty.FindPropertyRelative("isWaitingActionCompleted")?.boolValue == true)
                    states.Add("Wait");

                if (GetInlineActionStepCount(talkRequestDataProperty.FindPropertyRelative("onStartTalkAction")) > 0)
                    states.Add("Start");

                if (GetInlineActionStepCount(talkRequestDataProperty.FindPropertyRelative("onCompleteTalkAction")) > 0)
                    states.Add("Complete");
            }

            int presentChildCount = GetPresentChildSlotCount(stepProperty);

            if (presentChildCount > 0)
                states.Add(presentChildCount == 1 ? "1 child" : $"{presentChildCount} children");

            return string.Join(", ", states);
        }

        private static string BuildWaitFramesSummary(SerializedProperty stepProperty)
        {
            int frames = stepProperty.FindPropertyRelative("frames")?.intValue ?? 0;
            return frames == 1 ? "1 frame" : $"{frames} frames";
        }

        private static string BuildSubActionSummary(SerializedProperty stepProperty)
        {
            int count = GetInlineActionStepCount(stepProperty.FindPropertyRelative("action"));
            return count > 0 ? FormatCount(count, "step") : "Empty";
        }

        private static string BuildIfSummary(SerializedProperty stepProperty)
        {
            string condition = BuildReactiveBoolSummary(stepProperty.FindPropertyRelative("condition"));
            int trueCount = GetInlineActionStepCount(stepProperty.FindPropertyRelative("whenTrue"));
            int falseCount = GetInlineActionStepCount(stepProperty.FindPropertyRelative("whenFalse"));
            return $"if {condition} | T:{trueCount} F:{falseCount}";
        }

        private static string BuildShowToastSummary(SerializedProperty stepProperty)
        {
            SerializedProperty requestProperty = stepProperty.FindPropertyRelative("toastRequestData");

            if (requestProperty == null)
                return "Empty toast";

            string text = requestProperty.FindPropertyRelative("text")?.stringValue;
            bool hasIcon = requestProperty.FindPropertyRelative("icon")?.objectReferenceValue != null;

            if (!string.IsNullOrWhiteSpace(text))
                return BuildTextSnippet(text, "Empty toast");

            return hasIcon ? "Icon only" : "Empty toast";
        }

        private static string BuildShowTalkSummary(SerializedProperty stepProperty)
        {
            SerializedProperty requestProperty = stepProperty.FindPropertyRelative("talkRequestData");

            if (requestProperty == null)
                return "Empty talk";

            string speaker = Normalize(requestProperty.FindPropertyRelative("speakerName")?.stringValue);
            string text = BuildTextSnippet(requestProperty.FindPropertyRelative("dialogueText")?.stringValue, "Empty talk");

            if (!string.IsNullOrWhiteSpace(speaker) && text != "Empty talk")
                return $"{speaker}: {text}";

            if (!string.IsNullOrWhiteSpace(speaker))
                return speaker;

            return text;
        }

        private static string BuildShowTalkChoiceSummary(SerializedProperty stepProperty)
        {
            SerializedProperty optionsProperty = stepProperty.FindPropertyRelative("options");

            if (optionsProperty == null || !optionsProperty.isArray || optionsProperty.arraySize == 0)
                return "No options";

            List<string> segments = new()
            {
                FormatCount(optionsProperty.arraySize, "option"),
            };

            int defaultSelectionIndex = stepProperty.FindPropertyRelative("defaultSelectionIndex")?.intValue ?? 0;

            if (defaultSelectionIndex > 0)
                segments.Add($"default {defaultSelectionIndex}");

            if (stepProperty.FindPropertyRelative("wrapSelection")?.boolValue == false)
                segments.Add("no wrap");

            return string.Join(", ", segments);
        }

        private static string BuildValueStoreSummary(SerializedProperty stepProperty)
        {
            SerializedProperty writeProperty = stepProperty.FindPropertyRelative("write");

            if (writeProperty == null)
                return "No key";

            SerializedProperty keyProperty = writeProperty.FindPropertyRelative("key");
            string keySummary = BuildValueKeySummary(keyProperty);

            if (!TryResolveWriteKind(writeProperty, out ValueStoreWriteValueKind effectiveKind))
                return keySummary;

            string valueSummary = effectiveKind switch
            {
                ValueStoreWriteValueKind.Bool => BuildReactiveBoolSummary(writeProperty.FindPropertyRelative("boolValue")),
                ValueStoreWriteValueKind.Int => BuildReactiveIntSummary(writeProperty.FindPropertyRelative("intValue")),
                ValueStoreWriteValueKind.Float => BuildReactiveFloatSummary(writeProperty.FindPropertyRelative("floatValue")),
                ValueStoreWriteValueKind.String => BuildReactiveStringSummary(writeProperty.FindPropertyRelative("stringValue")),
                ValueStoreWriteValueKind.EntityRef => BuildReactiveEntitySummary(writeProperty.FindPropertyRelative("entityValue")),
                ValueStoreWriteValueKind.FaceExpressionId => BuildReactiveEnumSummary(writeProperty.FindPropertyRelative("faceExpressionValue")),
                ValueStoreWriteValueKind.EntityMoveState => BuildReactiveEnumSummary(writeProperty.FindPropertyRelative("entityMoveStateValue")),
                _ => "Unconfigured",
            };

            SerializedProperty scopeProperty = writeProperty.FindPropertyRelative("storeScope");

            if (scopeProperty != null && (ValueStoreWriteStoreScope)scopeProperty.enumValueIndex == ValueStoreWriteStoreScope.Entity)
            {
                string targetSummary = BuildEntityTargetSummary(writeProperty.FindPropertyRelative("target"));

                if (!string.Equals(targetSummary, "Self", StringComparison.Ordinal))
                    return $"{targetSummary}: {keySummary} = {valueSummary}";
            }

            return $"{keySummary} = {valueSummary}";
        }

        private static string BuildReactiveBoolSummary(SerializedProperty property)
        {
            if (property == null)
                return "Unconfigured";

            return (ReactiveBoolSourceKind)property.FindPropertyRelative("sourceKind")?.enumValueIndex switch
            {
                ReactiveBoolSourceKind.Literal => property.FindPropertyRelative("literal")?.boolValue == true ? "True" : "False",
                ReactiveBoolSourceKind.EntityValueStore => BuildScopedEntityValueSummary(property.FindPropertyRelative("entityValue")),
                ReactiveBoolSourceKind.LocalValueStore => BuildLocalValueSummary(property.FindPropertyRelative("localValue")),
                ReactiveBoolSourceKind.EntityAlive => $"Alive({BuildReactiveEntitySummary(property.FindPropertyRelative("entityAlive")?.FindPropertyRelative("entity"))})",
                ReactiveBoolSourceKind.CompareFloat => BuildCompareFloatSummary(property.FindPropertyRelative("compareFloat")),
                _ => "Unconfigured",
            };
        }

        private static string BuildReactiveIntSummary(SerializedProperty property)
        {
            if (property == null)
                return "Unconfigured";

            return (ReactiveIntSourceKind)property.FindPropertyRelative("sourceKind")?.enumValueIndex switch
            {
                ReactiveIntSourceKind.Literal => property.FindPropertyRelative("literal")?.intValue.ToString(CultureInfo.InvariantCulture) ?? "0",
                ReactiveIntSourceKind.EntityValueStore => BuildScopedEntityValueSummary(property.FindPropertyRelative("entityValue")),
                ReactiveIntSourceKind.LocalValueStore => BuildLocalValueSummary(property.FindPropertyRelative("localValue")),
                _ => "Unconfigured",
            };
        }

        private static string BuildReactiveFloatSummary(SerializedProperty property)
        {
            if (property == null)
                return "Unconfigured";

            return (ReactiveFloatSourceKind)property.FindPropertyRelative("sourceKind")?.enumValueIndex switch
            {
                ReactiveFloatSourceKind.Literal => FormatFloat(property.FindPropertyRelative("literal")?.floatValue ?? 0f),
                ReactiveFloatSourceKind.EntityValueStore => BuildScopedEntityValueSummary(property.FindPropertyRelative("entityValue")),
                ReactiveFloatSourceKind.LocalValueStore => BuildLocalValueSummary(property.FindPropertyRelative("localValue")),
                ReactiveFloatSourceKind.Distance => "Distance",
                _ => "Unconfigured",
            };
        }

        private static string BuildReactiveStringSummary(SerializedProperty property)
        {
            if (property == null)
                return "Unconfigured";

            return (ReactiveStringSourceKind)property.FindPropertyRelative("sourceKind")?.enumValueIndex switch
            {
                ReactiveStringSourceKind.Literal => BuildTextSnippet(property.FindPropertyRelative("literal")?.stringValue, "Empty text"),
                ReactiveStringSourceKind.EntityValueStore => BuildScopedEntityValueSummary(property.FindPropertyRelative("entityValue")),
                ReactiveStringSourceKind.LocalValueStore => BuildLocalValueSummary(property.FindPropertyRelative("localValue")),
                _ => "Unconfigured",
            };
        }

        private static string BuildReactiveEnumSummary(SerializedProperty property)
        {
            if (property == null)
                return "Unconfigured";

            SerializedProperty literalProperty = property.FindPropertyRelative("literal");

            if (literalProperty == null)
                return "Unconfigured";

            string[] displayNames = literalProperty.enumDisplayNames;
            int index = literalProperty.enumValueIndex;

            if (displayNames == null || index < 0 || index >= displayNames.Length)
                return "Unconfigured";

            return Normalize(displayNames[index]);
        }

        private static string BuildCompareFloatSummary(SerializedProperty property)
        {
            if (property == null)
                return "Compare";

            string left = BuildReactiveFloatSummary(property.FindPropertyRelative("left"));
            string right = BuildReactiveFloatSummary(property.FindPropertyRelative("right"));
            string op = BuildComparisonOperator(property.FindPropertyRelative("comparison"));
            return $"{left} {op} {right}";
        }

        private static string BuildComparisonOperator(SerializedProperty comparisonProperty)
        {
            if (comparisonProperty == null)
                return "==";

            return (ReactiveFloatComparisonKind)comparisonProperty.enumValueIndex switch
            {
                ReactiveFloatComparisonKind.Equal => "==",
                ReactiveFloatComparisonKind.NotEqual => "!=",
                ReactiveFloatComparisonKind.Greater => ">",
                ReactiveFloatComparisonKind.GreaterOrEqual => ">=",
                ReactiveFloatComparisonKind.Less => "<",
                ReactiveFloatComparisonKind.LessOrEqual => "<=",
                _ => "==",
            };
        }

        private static string BuildReactiveEntitySummary(SerializedProperty property)
        {
            if (property == null)
                return "Self";

            return (ReactiveEntitySourceKind)property.FindPropertyRelative("sourceKind")?.enumValueIndex switch
            {
                ReactiveEntitySourceKind.Self => "Self",
                ReactiveEntitySourceKind.TriggerEntity => "Trigger",
                ReactiveEntitySourceKind.EntityValueStore => BuildScopedEntityValueSummary(property.FindPropertyRelative("entityValue")),
                ReactiveEntitySourceKind.LocalValueStore => BuildLocalValueSummary(property.FindPropertyRelative("localValue")),
                ReactiveEntitySourceKind.TargetReference => BuildEntityTargetSummary(property.FindPropertyRelative("targetReference")),
                _ => "Self",
            };
        }

        private static string BuildScopedEntityValueSummary(SerializedProperty property)
        {
            if (property == null)
                return "No key";

            string key = BuildValueKeySummary(property.FindPropertyRelative("key"));
            SerializedProperty entitySourceKindProperty = property.FindPropertyRelative("entitySourceKind");
            string entity = entitySourceKindProperty != null &&
                entitySourceKindProperty.enumValueIndex == (int)ReactiveScopedEntitySourceKind.TriggerEntity
                ? "Trigger"
                : "Self";

            return $"{entity}:{key}";
        }

        private static string BuildLocalValueSummary(SerializedProperty property)
        {
            return BuildValueKeySummary(property?.FindPropertyRelative("key"));
        }

        private static string BuildEntityTargetSummary(SerializedProperty property)
        {
            if (property == null)
                return "Self";

            return (EntityTargetResolveMode)property.FindPropertyRelative("mode")?.enumValueIndex switch
            {
                EntityTargetResolveMode.Self => "Self",
                EntityTargetResolveMode.TriggerEntity => "Trigger",
                EntityTargetResolveMode.TagSearch => BuildTagSummary(property),
                _ => "Self",
            };
        }

        private static string BuildTagSummary(SerializedProperty property)
        {
            SerializedProperty tagProperty = property.FindPropertyRelative("tag");
            string path = tagProperty?.FindPropertyRelative("path")?.stringValue;
            string tagLabel = string.IsNullOrWhiteSpace(path) ? "Tag" : $"Tag:{path}";

            if (property.FindPropertyRelative("selection")?.enumValueIndex == (int)EntityTargetSelection.All)
                return $"{tagLabel} (All)";

            return tagLabel;
        }

        private static string BuildValueKeySummary(SerializedProperty property)
        {
            if (property == null)
                return "No key";

            string path = property.FindPropertyRelative("path")?.stringValue;

            if (string.IsNullOrWhiteSpace(path))
                return "No key";

            string[] segments = path.Split('.');

            if (segments.Length == 0)
                return "No key";

            if (string.Equals(segments[0], "Local", StringComparison.Ordinal))
                return $"L:{segments[segments.Length - 1]}";

            if (string.Equals(segments[0], "Kernel", StringComparison.Ordinal))
                return $"K:{segments[segments.Length - 1]}";

            return Normalize(path);
        }

        private static string BuildTextSnippet(string text, string emptyPlaceholder)
        {
            string normalized = Normalize(text);
            return string.IsNullOrWhiteSpace(normalized) ? emptyPlaceholder : normalized;
        }

        private static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string normalized = text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();
            return WhitespaceRegex.Replace(normalized, " ");
        }

        private static string FormatCount(int count, string singular)
        {
            return count == 1 ? $"1 {singular}" : $"{count} {singular}s";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static bool TryResolveWriteKind(SerializedProperty writeProperty, out ValueStoreWriteValueKind effectiveKind)
        {
            effectiveKind = ValueStoreWriteValueKind.Auto;

            if (writeProperty == null)
                return false;

            SerializedProperty valueKindProperty = writeProperty.FindPropertyRelative("valueKind");

            if (valueKindProperty == null)
                return false;

            ValueStoreWriteValueKind requestedKind = (ValueStoreWriteValueKind)valueKindProperty.enumValueIndex;

            if (requestedKind != ValueStoreWriteValueKind.Auto)
            {
                effectiveKind = requestedKind;
                return true;
            }

            string valueTypeName = writeProperty.FindPropertyRelative("key")?.FindPropertyRelative("valueTypeName")?.stringValue;
            Type valueType = string.IsNullOrWhiteSpace(valueTypeName) ? null : Type.GetType(valueTypeName);
            return valueType != null && ValueStoreWriteValueTypeUtility.TryGetKind(valueType, out effectiveKind);
        }

        private static int GetPresentChildSlotCount(SerializedProperty stepProperty)
        {
            if (stepProperty?.managedReferenceValue is not ActionStepAuthoring step)
                return 0;

            IReadOnlyList<ActionChildSlotDescriptor> childSlots = step.GetChildActionSlots();
            int count = 0;

            for (int i = 0; i < childSlots.Count; i++)
            {
                if (childSlots[i].IsPresent)
                    count++;
            }

            return count;
        }

        private static int GetInlineActionStepCount(SerializedProperty inlineActionProperty)
        {
            if (inlineActionProperty == null)
                return 0;

            if (inlineActionProperty.boxedValue is InlineAction inlineAction && inlineAction.Steps != null)
                return inlineAction.Steps.Count;

            SerializedProperty stepsProperty = inlineActionProperty.FindPropertyRelative("_steps");
            return stepsProperty != null && stepsProperty.isArray ? stepsProperty.arraySize : 0;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using BC.ActionSystem;
using BC.Animation;
using BC.Base;
using BC.Managers;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.ActionSystem
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
                SetActiveStepAuthoring => BuildSetActiveSummary(stepProperty),
                SubActionStepAuthoring => BuildSubActionSummary(stepProperty),
                IfStepAuthoring => BuildIfSummary(stepProperty),
                ShowToastStepAuthoring => BuildShowToastSummary(stepProperty),
                ShowTalkStepAuthoring => BuildShowTalkSummary(stepProperty),
                HideTalkStepAuthoring => BuildHideTalkSummary(stepProperty),
                ShowTalkChoiceStepAuthoring => BuildShowTalkChoiceSummary(stepProperty),
                SetValueStoreValueStepAuthoring => BuildValueStoreSummary(stepProperty),
                SetSceneCameraStepAuthoring => BuildSetSceneCameraSummary(stepProperty),
                ClearSceneCameraStepAuthoring => BuildClearSceneCameraSummary(stepProperty),
                SetEntityFacingTargetStepAuthoring => BuildSetEntityFacingTargetSummary(stepProperty),
                ClearEntityFacingStepAuthoring => BuildClearEntityFacingSummary(stepProperty),
                SetEntityAnimationParameterStepAuthoring => BuildSetEntityAnimationParameterSummary(stepProperty),
                SetEntityAnimationLayerWeightStepAuthoring => BuildSetEntityAnimationLayerWeightSummary(stepProperty),
                _ => GetTypeLabel(stepProperty),
            };

            return string.IsNullOrWhiteSpace(summary) ? "Unconfigured" : Normalize(summary);
        }

        internal static string GetClipboardText(SerializedProperty stepProperty)
        {
            if (stepProperty == null)
                return string.Empty;

            object step = stepProperty.managedReferenceValue;

            if (step is ShowTalkStepAuthoring)
                return BuildShowTalkClipboardText(stepProperty);

            return GetSummary(stepProperty);
        }

        internal static string GetStateText(SerializedProperty stepProperty)
        {
            IReadOnlyList<ActionStepBadge> badges = ActionStepChildSlotUtility.GetBadges(stepProperty);

            if (badges == null || badges.Count == 0)
                return string.Empty;

            List<string> texts = new(badges.Count);

            for (int i = 0; i < badges.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(badges[i].Text))
                    texts.Add(badges[i].Text);
            }

            return string.Join(", ", texts);
        }

        private static string BuildWaitFramesSummary(SerializedProperty stepProperty)
        {
            int frames = stepProperty.FindPropertyRelative("frames")?.intValue ?? 0;
            return frames == 1 ? "1 frame" : $"{frames} frames";
        }

        private static string BuildSetActiveSummary(SerializedProperty stepProperty)
        {
            string targetSummary = BuildEntityTargetSummary(stepProperty.FindPropertyRelative("_target"));
            string activeSummary = stepProperty.FindPropertyRelative("_active")?.boolValue == true ? "On" : "Off";
            return IsDefaultSelfTarget(targetSummary) ? activeSummary : $"{targetSummary} -> {activeSummary}";
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

            string talkState = BuildTalkStateSummary(requestProperty.FindPropertyRelative("talkStateId"));
            string speaker = Normalize(requestProperty.FindPropertyRelative("speakerName")?.stringValue);

            if (string.IsNullOrWhiteSpace(speaker))
            {
                SerializedProperty speakerCharacterProperty = requestProperty.FindPropertyRelative("speakerCharacter");
                speaker = Normalize(speakerCharacterProperty?.FindPropertyRelative("path")?.stringValue);
            }

            string text = BuildTextSnippet(requestProperty.FindPropertyRelative("dialogueText")?.stringValue, "Empty talk");
            string bodySummary = text;

            if (!string.IsNullOrWhiteSpace(speaker) && text != "Empty talk")
                bodySummary = $"{speaker}: {text}";
            else if (!string.IsNullOrWhiteSpace(speaker))
                bodySummary = speaker;

            if (string.IsNullOrWhiteSpace(talkState))
                return bodySummary;

            return bodySummary == "Empty talk"
                ? talkState
                : $"{talkState} | {bodySummary}";
        }

        private static string BuildShowTalkClipboardText(SerializedProperty stepProperty)
        {
            SerializedProperty requestProperty = stepProperty.FindPropertyRelative("talkRequestData");

            if (requestProperty == null)
                return string.Empty;

            string speaker = BuildSpeakerClipboardName(requestProperty);
            string text = BuildTextSnippet(requestProperty.FindPropertyRelative("dialogueText")?.stringValue, string.Empty);

            if (string.IsNullOrWhiteSpace(speaker))
                return text;

            if (string.IsNullOrWhiteSpace(text))
                return speaker;

            return $"{speaker}: {text}";
        }

        private static string BuildSpeakerClipboardName(SerializedProperty requestProperty)
        {
            string speaker = ResolveCharacterDisplayName(requestProperty.FindPropertyRelative("speakerCharacter"));

            if (!string.IsNullOrWhiteSpace(speaker))
                return speaker;

            string speakerName = Normalize(requestProperty.FindPropertyRelative("speakerName")?.stringValue);
            return speakerName;
        }

        private static string ResolveCharacterDisplayName(SerializedProperty characterProperty)
        {
            if (characterProperty == null)
                return string.Empty;

            SerializedProperty idProperty = characterProperty.FindPropertyRelative("id");
            SerializedProperty pathProperty = characterProperty.FindPropertyRelative("path");

            if (idProperty != null && idProperty.intValue != 0 && CharacterIdRegistry.TryGetDescriptor(new CharacterId(idProperty.intValue), out CharacterIdDescriptor descriptorById))
                return NormalizeCharacterDisplayName(descriptorById);

            if (pathProperty != null && !string.IsNullOrWhiteSpace(pathProperty.stringValue) && CharacterIdRegistry.TryGetDescriptor(pathProperty.stringValue, out CharacterIdDescriptor descriptorByPath))
                return NormalizeCharacterDisplayName(descriptorByPath);

            return string.Empty;
        }

        private static string NormalizeCharacterDisplayName(CharacterIdDescriptor descriptor)
        {
            if (!string.IsNullOrWhiteSpace(descriptor.DisplayName))
                return Normalize(descriptor.DisplayName);

            if (!string.IsNullOrWhiteSpace(descriptor.Path))
                return Normalize(descriptor.Path);

            return string.Empty;
        }

        private static string BuildHideTalkSummary(SerializedProperty stepProperty)
        {
            SerializedProperty requestProperty = stepProperty.FindPropertyRelative("requestData");
            float duration = requestProperty?.FindPropertyRelative("duration")?.floatValue ?? 0f;
            string durationSummary = Mathf.Approximately(duration, 0f) ? "Instant" : FormatDuration(duration);

            if (requestProperty?.FindPropertyRelative("applyTalkStateOverride")?.boolValue != true)
                return $"{durationSummary}, restore idle";

            string talkState = BuildTalkStateSummary(requestProperty.FindPropertyRelative("talkStateId"));
            return string.IsNullOrWhiteSpace(talkState)
                ? $"{durationSummary}, restore idle"
                : $"{durationSummary}, {talkState}";
        }

        private static string BuildTalkStateSummary(SerializedProperty talkStateProperty)
        {
            if (talkStateProperty == null || talkStateProperty.propertyType != SerializedPropertyType.Enum)
                return string.Empty;

            int enumIndex = talkStateProperty.enumValueIndex;
            string[] displayNames = talkStateProperty.enumDisplayNames;

            if (displayNames == null || enumIndex < 0 || enumIndex >= displayNames.Length)
                return string.Empty;

            string label = Normalize(displayNames[enumIndex]);
            return string.Equals(label, nameof(TalkStateId.None), StringComparison.OrdinalIgnoreCase) ? string.Empty : label;
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
                ValueStoreWriteValueKind.ShapeExpressionId => BuildReactiveEnumSummary(writeProperty.FindPropertyRelative("shapeExpressionValue")),
                _ => "Unconfigured",
            };

            SerializedProperty scopeProperty = writeProperty.FindPropertyRelative("storeScope");

            if (scopeProperty != null && ValueStoreWriteScopeUtility.UsesEntityTarget((ValueStoreWriteStoreScope)scopeProperty.enumValueIndex))
            {
                string targetSummary = BuildEntityTargetSummary(writeProperty.FindPropertyRelative("target"));
                string assignment = BuildValueStoreAssignment(writeProperty, effectiveKind, keySummary, valueSummary);

                if (!IsDefaultSelfTarget(targetSummary))
                    return $"{targetSummary}: {assignment}";
            }

            return BuildValueStoreAssignment(writeProperty, effectiveKind, keySummary, valueSummary);
        }

        private static string BuildValueStoreAssignment(
            SerializedProperty writeProperty,
            ValueStoreWriteValueKind effectiveKind,
            string keySummary,
            string valueSummary)
        {
            SerializedProperty operationProperty = writeProperty.FindPropertyRelative("numericOperation");
            ValueStoreNumericOperation operation = operationProperty != null
                ? (ValueStoreNumericOperation)operationProperty.enumValueIndex
                : ValueStoreNumericOperation.Set;

            if (effectiveKind == ValueStoreWriteValueKind.Int || effectiveKind == ValueStoreWriteValueKind.Float)
            {
                string expression = operation switch
                {
                    ValueStoreNumericOperation.Set => $"{keySummary} = {valueSummary}",
                    ValueStoreNumericOperation.Add => $"{keySummary} += {valueSummary}",
                    ValueStoreNumericOperation.Subtract => $"{keySummary} -= {valueSummary}",
                    ValueStoreNumericOperation.Multiply => $"{keySummary} *= {valueSummary}",
                    ValueStoreNumericOperation.Divide => $"{keySummary} /= {valueSummary}",
                    _ => $"{keySummary} = {valueSummary}",
                };

                return expression;
            }

            return $"{keySummary} = {valueSummary}";
        }

        private static string BuildSetSceneCameraSummary(SerializedProperty stepProperty)
        {
            string cameraName = stepProperty.FindPropertyRelative("camera")?.objectReferenceValue?.name;

            if (string.IsNullOrWhiteSpace(cameraName))
                return "No camera";

            return cameraName;
        }

        private static string BuildClearSceneCameraSummary(SerializedProperty stepProperty)
        {
            return "Clear action camera";
        }

        private static string BuildSetEntityFacingTargetSummary(SerializedProperty stepProperty)
        {
            string targetSummary = BuildEntityTargetSummary(stepProperty.FindPropertyRelative("target"));
            string faceTargetSummary = BuildEntityTargetSummary(stepProperty.FindPropertyRelative("faceTarget"));
            string channel = Normalize(stepProperty.FindPropertyRelative("channel")?.stringValue);

            string summary = IsDefaultSelfTarget(targetSummary)
                ? $"face {faceTargetSummary}"
                : $"{targetSummary} face {faceTargetSummary}";

            if (!IsDefaultActionFacingChannel(channel))
                summary = $"{summary} @ {channel}";

            return summary;
        }

        private static string BuildClearEntityFacingSummary(SerializedProperty stepProperty)
        {
            string targetSummary = BuildEntityTargetSummary(stepProperty.FindPropertyRelative("target"));
            string channel = Normalize(stepProperty.FindPropertyRelative("channel")?.stringValue);
            string summary = targetSummary;

            if (!IsDefaultActionFacingChannel(channel))
                summary = $"{summary} @ {channel}";

            return summary;
        }

        private static string BuildSetEntityAnimationParameterSummary(SerializedProperty stepProperty)
        {
            string targetSummary = BuildEntityTargetSummary(stepProperty.FindPropertyRelative("target"));
            string parameterName = Normalize(stepProperty.FindPropertyRelative("parameterName")?.stringValue);

            if (string.IsNullOrWhiteSpace(parameterName))
                return "Unconfigured";

            EntityAnimatorParameterWriteMode writeMode =
                (EntityAnimatorParameterWriteMode)(stepProperty.FindPropertyRelative("writeMode")?.enumValueIndex ?? 0);

            string body = writeMode switch
            {
                EntityAnimatorParameterWriteMode.SetBool =>
                    $"{parameterName} = {(stepProperty.FindPropertyRelative("boolValue")?.boolValue == true ? "True" : "False")}",
                EntityAnimatorParameterWriteMode.SetFloat =>
                    $"{parameterName} = {FormatFloat(stepProperty.FindPropertyRelative("floatValue")?.floatValue ?? 0f)}",
                EntityAnimatorParameterWriteMode.SetInteger =>
                    $"{parameterName} = {(stepProperty.FindPropertyRelative("intValue")?.intValue ?? 0).ToString(CultureInfo.InvariantCulture)}",
                EntityAnimatorParameterWriteMode.SetTrigger => $"SetTrigger {parameterName}",
                EntityAnimatorParameterWriteMode.ResetTrigger => $"ResetTrigger {parameterName}",
                _ => parameterName,
            };

            return IsDefaultSelfTarget(targetSummary) ? body : $"{targetSummary}: {body}";
        }

        private static string BuildSetEntityAnimationLayerWeightSummary(SerializedProperty stepProperty)
        {
            string targetSummary = BuildEntityTargetSummary(stepProperty.FindPropertyRelative("target"));
            string layerName = Normalize(stepProperty.FindPropertyRelative("layerName")?.stringValue);

            if (string.IsNullOrWhiteSpace(layerName))
                return "Unconfigured";

            float weight = stepProperty.FindPropertyRelative("weight")?.floatValue ?? 0f;
            float duration = stepProperty.FindPropertyRelative("duration")?.floatValue ?? 0f;

            string body = $"{layerName} = {FormatFloat(weight)}";

            if (duration > 0f)
                body = $"{body} in {FormatDuration(duration)}";

            return IsDefaultSelfTarget(targetSummary) ? body : $"{targetSummary}: {body}";
        }

        private static string BuildReactiveBoolSummary(SerializedProperty property)
        {
            if (property == null)
                return "Unconfigured";

            return (ReactiveBoolSourceKind)(property.FindPropertyRelative("sourceKind")?.enumValueIndex ?? 0) switch
            {
                ReactiveBoolSourceKind.Literal => property.FindPropertyRelative("literal")?.boolValue == true ? "True" : "False",
                ReactiveBoolSourceKind.EntityValueStore => BuildScopedEntityValueSummary(property.FindPropertyRelative("entityValue")),
                ReactiveBoolSourceKind.KernelValueStore => BuildKernelValueSummary(property.FindPropertyRelative("localValue")),
                ReactiveBoolSourceKind.EntityAlive => $"Alive({BuildReactiveEntitySummary(property.FindPropertyRelative("entityAlive")?.FindPropertyRelative("entity"))})",
                ReactiveBoolSourceKind.CompareFloat => BuildCompareFloatSummary(property.FindPropertyRelative("compareFloat")),
                _ => "Unconfigured",
            };
        }

        private static string BuildReactiveIntSummary(SerializedProperty property)
        {
            if (property == null)
                return "Unconfigured";

            return (ReactiveIntSourceKind)(property.FindPropertyRelative("sourceKind")?.enumValueIndex ?? 0) switch
            {
                ReactiveIntSourceKind.Literal => (property.FindPropertyRelative("literal")?.intValue ?? 0).ToString(CultureInfo.InvariantCulture),
                ReactiveIntSourceKind.EntityValueStore => BuildScopedEntityValueSummary(property.FindPropertyRelative("entityValue")),
                ReactiveIntSourceKind.KernelValueStore => BuildKernelValueSummary(property.FindPropertyRelative("localValue")),
                _ => "Unconfigured",
            };
        }

        private static string BuildReactiveFloatSummary(SerializedProperty property)
        {
            if (property == null)
                return "Unconfigured";

            return (ReactiveFloatSourceKind)(property.FindPropertyRelative("sourceKind")?.enumValueIndex ?? 0) switch
            {
                ReactiveFloatSourceKind.Literal => FormatFloat(property.FindPropertyRelative("literal")?.floatValue ?? 0f),
                ReactiveFloatSourceKind.EntityValueStore => BuildScopedEntityValueSummary(property.FindPropertyRelative("entityValue")),
                ReactiveFloatSourceKind.KernelValueStore => BuildKernelValueSummary(property.FindPropertyRelative("localValue")),
                ReactiveFloatSourceKind.Distance => "Distance",
                _ => "Unconfigured",
            };
        }

        private static string BuildReactiveStringSummary(SerializedProperty property)
        {
            if (property == null)
                return "Unconfigured";

            return (ReactiveStringSourceKind)(property.FindPropertyRelative("sourceKind")?.enumValueIndex ?? 0) switch
            {
                ReactiveStringSourceKind.Literal => BuildTextSnippet(property.FindPropertyRelative("literal")?.stringValue, "Empty text"),
                ReactiveStringSourceKind.EntityValueStore => BuildScopedEntityValueSummary(property.FindPropertyRelative("entityValue")),
                ReactiveStringSourceKind.KernelValueStore => BuildKernelValueSummary(property.FindPropertyRelative("localValue")),
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

            return (ReactiveEntitySourceKind)(property.FindPropertyRelative("sourceKind")?.enumValueIndex ?? 0) switch
            {
                ReactiveEntitySourceKind.Self => "Self",
                ReactiveEntitySourceKind.TriggerEntity => "Trigger",
                ReactiveEntitySourceKind.EntityValueStore => BuildScopedEntityValueSummary(property.FindPropertyRelative("entityValue")),
                ReactiveEntitySourceKind.KernelValueStore => BuildKernelValueSummary(property.FindPropertyRelative("localValue")),
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

        private static string BuildKernelValueSummary(SerializedProperty property)
        {
            if (property == null)
                return "No key";

            string key = BuildValueKeySummary(property.FindPropertyRelative("key"));
            SerializedProperty scopeProperty = property.FindPropertyRelative("storeScope");
            string scope = scopeProperty != null &&
                scopeProperty.enumValueIndex == (int)ReactiveKernelValueStoreScope.ApplicationKernel
                ? "App"
                : "Scene";

            return $"{scope}:{key}";
        }

        private static string BuildEntityTargetSummary(SerializedProperty property)
        {
            if (property == null)
                return "Self";

            return (EntityTargetResolveMode)(property.FindPropertyRelative("mode")?.enumValueIndex ?? 0) switch
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
            string tagLabel = string.IsNullOrWhiteSpace(path) ? "Tag" : $"Tag:{Normalize(path)}";

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

            string[] segments = path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
                return "No key";

            if (string.Equals(segments[0], "Local", StringComparison.Ordinal))
                return $"L:{BuildCompactKeyPath(segments)}";

            if (string.Equals(segments[0], "Kernel", StringComparison.Ordinal))
                return $"K:{BuildCompactKeyPath(segments)}";

            return Normalize(path);
        }

        private static string BuildCompactKeyPath(IReadOnlyList<string> segments)
        {
            if (segments == null || segments.Count == 0)
                return "Unknown";

            string leaf = segments[segments.Count - 1];

            if (segments.Count >= 3 && IsAmbiguousLeafSegment(leaf))
                return $"{segments[segments.Count - 2]}.{leaf}";

            return leaf;
        }

        private static bool IsAmbiguousLeafSegment(string leaf)
        {
            return string.Equals(leaf, "Index", StringComparison.Ordinal) ||
                   string.Equals(leaf, "SelectedIndex", StringComparison.Ordinal) ||
                   string.Equals(leaf, "Value", StringComparison.Ordinal) ||
                   string.Equals(leaf, "State", StringComparison.Ordinal) ||
                   string.Equals(leaf, "Flag", StringComparison.Ordinal);
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

        private static bool IsDefaultSelfTarget(string targetSummary)
        {
            return string.Equals(targetSummary, "Self", StringComparison.Ordinal);
        }

        private static bool IsDefaultActionFacingChannel(string channel)
        {
            return string.IsNullOrWhiteSpace(channel) || string.Equals(channel, EntityFacingChannels.Action, StringComparison.Ordinal);
        }

        private static string FormatCount(int count, string singular)
        {
            return count == 1 ? $"1 {singular}" : $"{count} {singular}s";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatDuration(float duration)
        {
            return $"{FormatFloat(duration)}s";
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

        private static int GetInlineActionStepCount(SerializedProperty inlineActionProperty)
        {
            if (inlineActionProperty == null)
                return 0;

            if (inlineActionProperty.boxedValue is InlineAction inlineAction)
                return inlineAction.Steps?.Count ?? 0;

            SerializedProperty stepsProperty = inlineActionProperty.FindPropertyRelative("_steps");
            return stepsProperty != null && stepsProperty.isArray ? stepsProperty.arraySize : 0;
        }
    }
}

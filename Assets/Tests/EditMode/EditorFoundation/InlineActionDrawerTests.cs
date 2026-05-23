using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Tests
{
    public sealed class InlineActionDrawerTests
    {
        private ScriptableObject host;
        private SerializedObject serializedObject;

        [SetUp]
        public void SetUp()
        {
            CloseInlineActionWindows();
            Type hostType = GetTypeByFullName("BC.Editor.ActionSystem.InlineActionDrawerTestHost");
            host = ScriptableObject.CreateInstance(hostType);
            serializedObject = new SerializedObject(host);
            ClearWindowLaunchRequest();
        }

        [TearDown]
        public void TearDown()
        {
            ClearWindowLaunchRequest();
            CloseInlineActionWindows();

            if (host != null)
                UnityEngine.Object.DestroyImmediate(host);
        }

        [Test]
        public void InlineActionDrawer_GetPropertyHeight_ForEmptyAction_IncludesLabelAndList()
        {
            PropertyDrawer drawer = CreateDrawer("BC.Editor.ActionSystem.InlineActionDrawer");
            SerializedProperty property = serializedObject.FindProperty("inlineAction");
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float expectedHeight = (lineHeight * 2f) + spacing;

            Assert.That(drawer.GetPropertyHeight(property, new GUIContent("Action")), Is.EqualTo(expectedHeight).Within(0.001f));
        }

        [Test]
        public void ActionStepSummaryUtility_UsesDisplayNameOverride_ThenFallsBackToWaitFramesTemplate()
        {
            SerializedProperty stepProperty = AddStep("BC.ActionSystem.WaitFramesStepAuthoring");
            stepProperty.FindPropertyRelative("DisplayName").stringValue = "  Custom Label  ";
            ApplyChanges();

            Assert.AreEqual("Custom Label", GetSummary(stepProperty));

            stepProperty.FindPropertyRelative("DisplayName").stringValue = string.Empty;
            stepProperty.FindPropertyRelative("frames").intValue = 3;
            ApplyChanges();

            Assert.AreEqual("3 frames", GetSummary(stepProperty));
        }

        [Test]
        public void ActionStepSummaryUtility_BuildsRepresentativeTemplates()
        {
            SerializedProperty ifStep = AddStep("BC.ActionSystem.IfStepAuthoring");
            EnsureInlineAction(ifStep.FindPropertyRelative("whenTrue"));
            EnsureInlineAction(ifStep.FindPropertyRelative("whenFalse"));
            ApplyChanges();
            AddNestedStep(ifStep.FindPropertyRelative("whenTrue"), "BC.ActionSystem.WaitFramesStepAuthoring");
            AddNestedStep(ifStep.FindPropertyRelative("whenFalse"), "BC.ActionSystem.WaitFramesStepAuthoring");
            ApplyChanges();

            Assert.AreEqual("if True | T:1 F:1", GetSummary(ifStep));

            SerializedProperty talkStep = AddStep("BC.ActionSystem.ShowTalkStepAuthoring");
            talkStep.FindPropertyRelative("talkRequestData").FindPropertyRelative("talkStateId").enumValueIndex = 4;
            talkStep.FindPropertyRelative("talkRequestData").FindPropertyRelative("speakerName").stringValue = "Guide";
            talkStep.FindPropertyRelative("talkRequestData").FindPropertyRelative("dialogueText").stringValue = "Line 1\nLine 2";
            ApplyChanges();

            Assert.AreEqual("Happy | Guide: Line 1 Line 2", GetSummary(talkStep));

            SerializedProperty hideTalkStep = AddStep("BC.ActionSystem.HideTalkStepAuthoring");
            SerializedProperty hideRequestProperty = hideTalkStep.FindPropertyRelative("requestData");
            hideRequestProperty.FindPropertyRelative("duration").floatValue = 0.2f;
            hideRequestProperty.FindPropertyRelative("applyTalkStateOverride").boolValue = true;
            hideRequestProperty.FindPropertyRelative("talkStateId").enumValueIndex = 3;
            ApplyChanges();

            Assert.AreEqual("0.2s, Surprised", GetSummary(hideTalkStep));

            SerializedProperty choiceStep = AddStep("BC.ActionSystem.ShowTalkChoiceStepAuthoring");
            SerializedProperty optionsProperty = choiceStep.FindPropertyRelative("options");
            optionsProperty.arraySize = 3;
            choiceStep.FindPropertyRelative("defaultSelectionIndex").intValue = 2;
            choiceStep.FindPropertyRelative("wrapSelection").boolValue = false;
            ApplyChanges();

            Assert.AreEqual("3 options, default 2, no wrap", GetSummary(choiceStep));

            SerializedProperty valueStoreStep = AddStep("BC.ActionSystem.SetValueStoreValueStepAuthoring");
            SerializedProperty writeProperty = valueStoreStep.FindPropertyRelative("write");
            writeProperty.FindPropertyRelative("storeScope").enumValueIndex = 2;
            writeProperty.FindPropertyRelative("key").FindPropertyRelative("path").stringValue = "Local.Choice.SelectedIndex";
            writeProperty.FindPropertyRelative("key").FindPropertyRelative("valueTypeName").stringValue = typeof(int).AssemblyQualifiedName;
            writeProperty.FindPropertyRelative("intValue").FindPropertyRelative("literal").intValue = 42;
            ApplyChanges();

            Assert.AreEqual("L:Choice.SelectedIndex = 42", GetSummary(valueStoreStep));

            SerializedProperty layerStep = AddStep("BC.ActionSystem.SetEntityAnimationLayerWeightStepAuthoring");
            layerStep.FindPropertyRelative("layerName").stringValue = "UpperBody";
            layerStep.FindPropertyRelative("weight").floatValue = 0.5f;
            layerStep.FindPropertyRelative("duration").floatValue = 0.2f;
            ApplyChanges();

            Assert.AreEqual("UpperBody = 0.5 in 0.2s", GetSummary(layerStep));
        }

        [Test]
        public void InlineActionEditorState_CommitsAndCancelsRenameState()
        {
            SerializedProperty stepProperty = AddStep("BC.ActionSystem.WaitFramesStepAuthoring");
            Type stateType = GetTypeByFullName("BC.Editor.ActionSystem.InlineActionEditorState");

            InvokeDeclaredMethod(stateType, null, "BeginRename", stepProperty);
            Assert.IsTrue((bool)InvokeDeclaredMethod(stateType, null, "IsRenameActive", stepProperty));

            InvokeDeclaredMethod(stateType, null, "SetRenameText", stepProperty, "  Renamed Step  ");
            InvokeDeclaredMethod(stateType, null, "CommitRename", stepProperty);
            serializedObject.Update();

            Assert.AreEqual("Renamed Step", stepProperty.FindPropertyRelative("DisplayName").stringValue);
            Assert.IsFalse((bool)InvokeDeclaredMethod(stateType, null, "IsRenameActive", stepProperty));

            InvokeDeclaredMethod(stateType, null, "BeginRename", stepProperty);
            InvokeDeclaredMethod(stateType, null, "SetRenameText", stepProperty, "Canceled");
            Assert.IsFalse((bool)InvokeDeclaredMethod(stateType, null, "ShouldCommitRenameOnFocusLoss", stepProperty, true, EventType.Repaint));
            Assert.IsTrue((bool)InvokeDeclaredMethod(stateType, null, "ShouldCommitRenameOnFocusLoss", stepProperty, false, EventType.MouseMove));
            InvokeDeclaredMethod(stateType, null, "CancelRename", stepProperty);
            serializedObject.Update();

            Assert.AreEqual("Renamed Step", stepProperty.FindPropertyRelative("DisplayName").stringValue);
            Assert.IsFalse((bool)InvokeDeclaredMethod(stateType, null, "IsRenameActive", stepProperty));
        }

        [Test]
        public void ActionStepManagedReferenceUtility_SupportsAddDuplicateMoveAndDelete()
        {
            SerializedProperty firstStep = AddStep("BC.ActionSystem.WaitFramesStepAuthoring");
            firstStep.FindPropertyRelative("frames").intValue = 1;
            SerializedProperty secondStep = AddStep("BC.ActionSystem.WaitFramesStepAuthoring");
            secondStep.FindPropertyRelative("frames").intValue = 2;
            ApplyChanges();

            SerializedProperty stepsProperty = FindStepsProperty();
            Type utilityType = GetTypeByFullName("BC.Editor.ActionSystem.ActionStepManagedReferenceUtility");
            InvokeDeclaredMethod(utilityType, null, "DuplicateStep", serializedObject.targetObjects, stepsProperty.propertyPath, 0);
            serializedObject.Update();
            stepsProperty = FindStepsProperty();

            Assert.AreEqual(3, stepsProperty.arraySize);

            InvokeDeclaredMethod(utilityType, null, "MoveStep", serializedObject.targetObjects, stepsProperty.propertyPath, 2, 0);
            serializedObject.Update();
            stepsProperty = FindStepsProperty();

            Assert.AreEqual(2, stepsProperty.GetArrayElementAtIndex(0).FindPropertyRelative("frames").intValue);

            InvokeDeclaredMethod(utilityType, null, "DeleteStep", serializedObject.targetObjects, stepsProperty.propertyPath, 1);
            serializedObject.Update();
            stepsProperty = FindStepsProperty();

            Assert.AreEqual(2, stepsProperty.arraySize);
        }

        [Test]
        public void InlineActionDrawer_ExpandedNestedAction_IncreasesHeight()
        {
            SerializedProperty subActionStep = AddStep("BC.ActionSystem.SubActionStepAuthoring");
            EnsureInlineAction(subActionStep.FindPropertyRelative("action"));
            ApplyChanges();
            AddNestedStep(subActionStep.FindPropertyRelative("action"), "BC.ActionSystem.WaitFramesStepAuthoring");
            ApplyChanges();

            PropertyDrawer drawer = CreateDrawer("BC.Editor.ActionSystem.InlineActionDrawer");
            SerializedProperty property = serializedObject.FindProperty("inlineAction");
            float collapsedHeight = drawer.GetPropertyHeight(property, GUIContent.none);

            Type stateType = GetTypeByFullName("BC.Editor.ActionSystem.InlineActionEditorState");
            InvokeDeclaredMethod(stateType, null, "SetExpanded", subActionStep, true);
            float expandedHeight = drawer.GetPropertyHeight(property, GUIContent.none);

            Assert.Greater(expandedHeight, collapsedHeight);
        }

        [Test]
        public void InlineActionDrawer_ExpandedShowTalk_AutoExpandsTalkRequestData()
        {
            SerializedProperty talkStep = AddStep("BC.ActionSystem.ShowTalkStepAuthoring");
            SerializedProperty talkRequestData = talkStep.FindPropertyRelative("talkRequestData");
            talkRequestData.isExpanded = false;
            ApplyChanges();

            Type drawerType = GetTypeByFullName("BC.Editor.ActionSystem.InlineActionDrawer");
            InvokeDeclaredMethod(drawerType, null, "GetExpandedDetailHeight", talkStep);
            serializedObject.Update();

            Assert.IsTrue(talkStep.FindPropertyRelative("talkRequestData").isExpanded);
        }

        [Test]
        public void ActionStepChildSlotUtility_UsesDescriptorContractForBadges()
        {
            SerializedProperty subActionStep = AddStep("BC.ActionSystem.SubActionStepAuthoring");
            string[] subActionBadges = GetBadgeTexts(subActionStep);

            CollectionAssert.Contains(subActionBadges, "1 child");

            SerializedProperty ifStep = AddStep("BC.ActionSystem.IfStepAuthoring");
            string[] ifBadges = GetBadgeTexts(ifStep);

            CollectionAssert.Contains(ifBadges, "2 children");

            SerializedProperty talkStep = AddStep("BC.ActionSystem.ShowTalkStepAuthoring");
            string[] missingTalkBadges = GetBadgeTexts(talkStep);
            object talkAuthoring = talkStep.managedReferenceValue;
            Assert.IsNotNull(talkAuthoring, "Expected ShowTalk step instance.");
            int missingTalkCount = CountMissingChildSlots(talkAuthoring);

            CollectionAssert.Contains(missingTalkBadges, "2 children");
            CollectionAssert.Contains(missingTalkBadges, "Start");
            CollectionAssert.Contains(missingTalkBadges, "Complete");

            if (missingTalkCount > 0)
                CollectionAssert.Contains(missingTalkBadges, missingTalkCount == 1 ? "Missing child" : $"{missingTalkCount} missing");

            SerializedProperty talkRequestData = talkStep.FindPropertyRelative("talkRequestData");
            talkRequestData.FindPropertyRelative("isWaitingActionCompleted").boolValue = true;
            EnsureInlineAction(talkRequestData.FindPropertyRelative("onStartTalkAction"));
            EnsureInlineAction(talkRequestData.FindPropertyRelative("onCompleteTalkAction"));
            ApplyChanges();

            string[] talkBadges = GetBadgeTexts(talkStep);

            CollectionAssert.Contains(talkBadges, "Wait");
            CollectionAssert.Contains(talkBadges, "Start");
            CollectionAssert.Contains(talkBadges, "Complete");
            CollectionAssert.Contains(talkBadges, "2 children");

            SerializedProperty choiceStep = AddStep("BC.ActionSystem.ShowTalkChoiceStepAuthoring");
            SerializedProperty optionsProperty = choiceStep.FindPropertyRelative("options");
            optionsProperty.arraySize = 1;
            SerializedProperty optionProperty = optionsProperty.GetArrayElementAtIndex(0);
            optionProperty.FindPropertyRelative("displayText").stringValue = "Take item";
            optionProperty.FindPropertyRelative("outcomeKind").enumValueIndex = 1;
            EnsureInlineAction(optionProperty.FindPropertyRelative("inlineAction"));
            ApplyChanges();

            string[] choiceBadges = GetBadgeTexts(choiceStep);
            CollectionAssert.Contains(choiceBadges, "#1");
            CollectionAssert.Contains(choiceBadges, "1 child");
        }

        [Test]
        public void ActionInlineWindowLauncher_StoresPropertyBoundRequest()
        {
            SerializedProperty property = serializedObject.FindProperty("inlineAction");
            Type launcherType = GetTypeByFullName("BC.Editor.ActionSystem.ActionInlineWindowLauncher");
            InvokeDeclaredMethod(launcherType, null, "Launch", property);

            int storedInstanceId = SessionState.GetInt("BC.Editor.ActionSystem.ActionInlineWindowLauncher.TargetInstanceId", 0);
#pragma warning disable CS0618
            Assert.AreEqual(host.GetInstanceID(), storedInstanceId);
#pragma warning restore CS0618
            Assert.AreEqual("inlineAction", SessionState.GetString("BC.Editor.ActionSystem.ActionInlineWindowLauncher.PropertyPath", string.Empty));
            Assert.That(SessionState.GetString("BC.Editor.ActionSystem.ActionInlineWindowLauncher.BindingKey", string.Empty), Does.Contain("inlineAction"));
            Assert.AreEqual(0, GetInlineActionWindowCount());
        }

        [Test]
        public void ActionInlineWindowLauncher_LaunchAndOpen_OpensWindow()
        {
            SerializedProperty property = serializedObject.FindProperty("inlineAction");
            Type launcherType = GetTypeByFullName("BC.Editor.ActionSystem.ActionInlineWindowLauncher");
            InvokeDeclaredMethod(launcherType, null, "LaunchAndOpen", property);

            Assert.AreEqual(1, GetInlineActionWindowCount());
            Assert.AreEqual("inlineAction", SessionState.GetString("BC.Editor.ActionSystem.ActionInlineWindowLauncher.PropertyPath", string.Empty));
        }

        [Test]
        public void ActionStepManagedReferenceUtility_ClearSteps_RemovesAllSteps()
        {
            AddStep("BC.ActionSystem.WaitFramesStepAuthoring");
            AddStep("BC.ActionSystem.WaitFramesStepAuthoring");
            ApplyChanges();

            SerializedProperty stepsProperty = FindStepsProperty();
            Type utilityType = GetTypeByFullName("BC.Editor.ActionSystem.ActionStepManagedReferenceUtility");
            InvokeDeclaredMethod(utilityType, null, "ClearSteps", serializedObject.targetObjects, stepsProperty.propertyPath);
            serializedObject.Update();

            Assert.AreEqual(0, FindStepsProperty().arraySize);
        }

        [Test]
        public void ActionStepManagedReferenceUtility_ClearInlineAction_SetsBranchToNull()
        {
            SerializedProperty talkStep = AddStep("BC.ActionSystem.ShowTalkStepAuthoring");
            SerializedProperty branchProperty = talkStep.FindPropertyRelative("talkRequestData").FindPropertyRelative("onStartTalkAction");
            EnsureInlineAction(branchProperty);
            AddNestedStep(branchProperty, "BC.ActionSystem.WaitFramesStepAuthoring");
            ApplyChanges();

            Type utilityType = GetTypeByFullName("BC.Editor.ActionSystem.ActionStepManagedReferenceUtility");
            InvokeDeclaredMethod(utilityType, null, "ClearInlineAction", serializedObject.targetObjects, branchProperty.propertyPath);
            serializedObject.Update();

            SerializedProperty clearedBranchProperty = serializedObject.FindProperty(branchProperty.propertyPath);
            Assert.IsNotNull(clearedBranchProperty, "Expected branch property.");
            Assert.IsNull(clearedBranchProperty.boxedValue);
        }

        [Test]
        public void ActionStepManagedReferenceUtility_CopyAndPasteStep_ClonesSelectedStep()
        {
            SerializedProperty sourceStep = AddStep("BC.ActionSystem.WaitFramesStepAuthoring");
            sourceStep.FindPropertyRelative("frames").intValue = 7;
            ApplyChanges();

            SerializedProperty stepsProperty = FindStepsProperty();
            Type utilityType = GetTypeByFullName("BC.Editor.ActionSystem.ActionStepManagedReferenceUtility");
            InvokeDeclaredMethod(utilityType, null, "CopyStep", sourceStep);
            InvokeDeclaredMethod(utilityType, null, "PasteStep", serializedObject.targetObjects, stepsProperty.propertyPath, 1);
            serializedObject.Update();

            SerializedProperty pastedStep = FindStepsProperty().GetArrayElementAtIndex(1);
            Assert.AreEqual(2, FindStepsProperty().arraySize);
            Assert.AreEqual(7, pastedStep.FindPropertyRelative("frames").intValue);
        }

        [Test]
        public void ActionBlockTreeViewModel_UsesStableBranchKeysAcrossStepReorder()
        {
            SerializedProperty firstStep = AddStep("BC.ActionSystem.WaitFramesStepAuthoring");
            firstStep.FindPropertyRelative("DisplayName").stringValue = "First";
            SerializedProperty secondStep = AddStep("BC.ActionSystem.WaitFramesStepAuthoring");
            secondStep.FindPropertyRelative("DisplayName").stringValue = "Second";
            ApplyChanges();

            SerializedProperty rootProperty = serializedObject.FindProperty("inlineAction");
            Type modelType = GetTypeByFullName("BC.Editor.ActionSystem.ActionBlockTreeViewModel");
            object model = Activator.CreateInstance(modelType);
            InvokeDeclaredMethod(modelType, model, "Rebuild", rootProperty);

            string beforeMoveBranchKey = GetStepBranchKey(model, "Second");

            Type utilityType = GetTypeByFullName("BC.Editor.ActionSystem.ActionStepManagedReferenceUtility");
            SerializedProperty stepsProperty = FindStepsProperty();
            InvokeDeclaredMethod(utilityType, null, "MoveStep", serializedObject.targetObjects, stepsProperty.propertyPath, 1, 0);
            serializedObject.Update();

            rootProperty = serializedObject.FindProperty("inlineAction");
            InvokeDeclaredMethod(modelType, model, "Rebuild", rootProperty);
            string afterMoveBranchKey = GetStepBranchKey(model, "Second");

            Assert.AreEqual(beforeMoveBranchKey, afterMoveBranchKey);
        }

        [Test]
        public void ActionBlockTreeViewModel_DoesNotDuplicateChildActionBlockRows()
        {
            SerializedProperty talkStep = AddStep("BC.ActionSystem.ShowTalkStepAuthoring");
            SerializedProperty startBranchProperty = talkStep.FindPropertyRelative("talkRequestData").FindPropertyRelative("onStartTalkAction");
            EnsureInlineAction(startBranchProperty);
            AddNestedStep(startBranchProperty, "BC.ActionSystem.WaitFramesStepAuthoring");
            ApplyChanges();

            SerializedProperty rootProperty = serializedObject.FindProperty("inlineAction");
            Type modelType = GetTypeByFullName("BC.Editor.ActionSystem.ActionBlockTreeViewModel");
            object model = Activator.CreateInstance(modelType);
            InvokeDeclaredMethod(modelType, model, "Rebuild", rootProperty);

            Assert.AreEqual(1, CountTreeItems(model, "Start Talk", "Branch"));
            Assert.AreEqual(0, CountTreeItems(model, "Start Talk", "Block"));
        }

        [Test]
        public void ActionBlockTreeViewModel_StepWithChildSlots_IsExpandable()
        {
            AddStep("BC.ActionSystem.ShowTalkStepAuthoring");
            ApplyChanges();

            SerializedProperty rootProperty = serializedObject.FindProperty("inlineAction");
            Type modelType = GetTypeByFullName("BC.Editor.ActionSystem.ActionBlockTreeViewModel");
            object model = Activator.CreateInstance(modelType);
            InvokeDeclaredMethod(modelType, model, "Rebuild", rootProperty);

            PropertyInfo itemsProperty = modelType.GetProperty("Items", BindingFlags.Instance | BindingFlags.Public);
            IEnumerable items = itemsProperty?.GetValue(model) as IEnumerable;
            Assert.IsNotNull(items, "Expected tree items.");

            bool foundExpandableShowTalk = false;

            foreach (object item in items)
            {
                if (item == null)
                    continue;

                PropertyInfo kindProperty = item.GetType().GetProperty("Kind", BindingFlags.Instance | BindingFlags.Public);
                PropertyInfo titleProperty = item.GetType().GetProperty("Title", BindingFlags.Instance | BindingFlags.Public);
                PropertyInfo expandableProperty = item.GetType().GetProperty("CanExpand", BindingFlags.Instance | BindingFlags.Public);

                if ((int)(kindProperty?.GetValue(item) ?? -1) != 1)
                    continue;

                if (!string.Equals(titleProperty?.GetValue(item) as string, "Show Talk", StringComparison.Ordinal))
                    continue;

                foundExpandableShowTalk = (bool)(expandableProperty?.GetValue(item) ?? false);
                break;
            }

            Assert.IsTrue(foundExpandableShowTalk);
        }

        [Test]
        public void ActionStepManagedReferenceUtility_MoveStepBetweenLists_MovesStepAcrossBranch()
        {
            SerializedProperty firstStep = AddStep("BC.ActionSystem.WaitFramesStepAuthoring");
            firstStep.FindPropertyRelative("DisplayName").stringValue = "First";

            SerializedProperty secondStep = AddStep("BC.ActionSystem.WaitFramesStepAuthoring");
            secondStep.FindPropertyRelative("DisplayName").stringValue = "Second";

            SerializedProperty talkStep = AddStep("BC.ActionSystem.ShowTalkStepAuthoring");
            SerializedProperty startBranchProperty = talkStep.FindPropertyRelative("talkRequestData").FindPropertyRelative("onStartTalkAction");
            EnsureInlineAction(startBranchProperty);
            ApplyChanges();

            SerializedProperty rootStepsProperty = FindStepsProperty();
            SerializedProperty startStepsProperty = startBranchProperty.FindPropertyRelative("_steps");
            Assert.IsNotNull(startStepsProperty, "Expected start talk _steps property.");

            Type utilityType = GetTypeByFullName("BC.Editor.ActionSystem.ActionStepManagedReferenceUtility");
            InvokeDeclaredMethod(
                utilityType,
                null,
                "MoveStepBetweenLists",
                serializedObject.targetObjects,
                rootStepsProperty.propertyPath,
                1,
                startStepsProperty.propertyPath,
                0);

            serializedObject.Update();

            rootStepsProperty = FindStepsProperty();
            startStepsProperty = serializedObject.FindProperty(startStepsProperty.propertyPath);
            Assert.IsNotNull(startStepsProperty, "Expected moved start talk _steps property.");

            Assert.AreEqual(2, rootStepsProperty.arraySize);
            Assert.AreEqual(1, startStepsProperty.arraySize);

            SerializedProperty movedStep = startStepsProperty.GetArrayElementAtIndex(0);
            Assert.AreEqual("Second", movedStep.FindPropertyRelative("DisplayName").stringValue);
        }

        [Test]
        public void ActionAuthoringSystemData_RecordStepSelection_DeduplicatesAndSortsByLatestTimestamp()
        {
            Type dataType = GetTypeByFullName("BC.Editor.ActionSystem.ActionAuthoringSystemData");
            Type waitFramesType = GetTypeByFullName("BC.ActionSystem.WaitFramesStepAuthoring");
            Type showTalkType = GetTypeByFullName("BC.ActionSystem.ShowTalkStepAuthoring");

            ScriptableObject data = ScriptableObject.CreateInstance(dataType);

            try
            {
                string waitFramesTypeName = waitFramesType.AssemblyQualifiedName;
                string showTalkTypeName = showTalkType.AssemblyQualifiedName;

                InvokeDeclaredMethod(dataType, data, "RecordStepSelection", waitFramesTypeName, 100L);
                InvokeDeclaredMethod(dataType, data, "RecordStepSelection", showTalkTypeName, 200L);
                InvokeDeclaredMethod(dataType, data, "RecordStepSelection", waitFramesTypeName, 300L);

                object recent = InvokeDeclaredMethod(dataType, data, "GetRecentStepSelections", 5);
                Assert.IsNotNull(recent, "Expected recent selection entries.");

                List<object> entries = new();
                foreach (object entry in (IEnumerable)recent)
                    entries.Add(entry);

                Assert.AreEqual(2, entries.Count);

                PropertyInfo typeNameProperty = entries[0].GetType().GetProperty("StepTypeName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                PropertyInfo ticksProperty = entries[0].GetType().GetProperty("LastSelectedUtcTicks", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                PropertyInfo countProperty = entries[0].GetType().GetProperty("SelectedCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                Assert.IsNotNull(typeNameProperty, "Expected StepTypeName property.");
                Assert.IsNotNull(ticksProperty, "Expected LastSelectedUtcTicks property.");
                Assert.IsNotNull(countProperty, "Expected SelectedCount property.");

                Assert.AreEqual(waitFramesTypeName, typeNameProperty.GetValue(entries[0]) as string);
                Assert.AreEqual(300L, (long)(ticksProperty.GetValue(entries[0]) ?? 0L));
                Assert.AreEqual(2, (int)(countProperty.GetValue(entries[0]) ?? 0));

                Assert.AreEqual(showTalkTypeName, typeNameProperty.GetValue(entries[1]) as string);
                Assert.AreEqual(200L, (long)(ticksProperty.GetValue(entries[1]) ?? 0L));
                Assert.AreEqual(1, (int)(countProperty.GetValue(entries[1]) ?? 0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        private SerializedProperty AddStep(string stepTypeName)
        {
            Type managedReferenceListType = GetTypeByFullName("BC.Editor.Foundation.IMGUI.ManagedReferenceListController");
            SerializedProperty stepsProperty = FindStepsProperty();
            Type stepType = GetTypeByFullName(stepTypeName);
            object addedProperty = InvokeDeclaredMethod(managedReferenceListType, null, "AddNewElement", stepsProperty, stepType);
            ApplyChanges();

            return serializedObject.FindProperty(((SerializedProperty)addedProperty).propertyPath);
        }

        private void AddNestedStep(SerializedProperty inlineActionProperty, string stepTypeName)
        {
            EnsureInlineAction(inlineActionProperty);
            ApplyChanges();

            Type managedReferenceListType = GetTypeByFullName("BC.Editor.Foundation.IMGUI.ManagedReferenceListController");
            Type stepType = GetTypeByFullName(stepTypeName);
            SerializedProperty nestedStepsProperty = inlineActionProperty.FindPropertyRelative("_steps");
            InvokeDeclaredMethod(managedReferenceListType, null, "AddNewElement", nestedStepsProperty, stepType);
            ApplyChanges();
        }

        private void EnsureInlineAction(SerializedProperty inlineActionProperty)
        {
            if (inlineActionProperty != null && inlineActionProperty.boxedValue == null)
                inlineActionProperty.boxedValue = Activator.CreateInstance(GetTypeByFullName("BC.ActionSystem.InlineAction"));
        }

        private SerializedProperty FindStepsProperty()
        {
            SerializedProperty inlineActionProperty = serializedObject.FindProperty("inlineAction");
            Assert.IsNotNull(inlineActionProperty, "Expected inlineAction property.");

            SerializedProperty stepsProperty = inlineActionProperty.FindPropertyRelative("_steps");
            Assert.IsNotNull(stepsProperty, "Expected InlineAction._steps property.");
            return stepsProperty;
        }

        private string GetSummary(SerializedProperty stepProperty)
        {
            Type utilityType = GetTypeByFullName("BC.Editor.ActionSystem.ActionStepSummaryUtility");
            return (string)InvokeDeclaredMethod(utilityType, null, "GetSummary", stepProperty);
        }

        private string[] GetBadgeTexts(SerializedProperty stepProperty)
        {
            Type utilityType = GetTypeByFullName("BC.Editor.ActionSystem.ActionStepChildSlotUtility");
            IEnumerable badges = InvokeDeclaredMethod(utilityType, null, "GetBadges", stepProperty) as IEnumerable;

            Assert.IsNotNull(badges, "Expected badge collection.");

            System.Collections.Generic.List<string> texts = new();

            foreach (object badge in badges)
            {
                if (badge == null)
                    continue;

                PropertyInfo textProperty = badge.GetType().GetProperty("Text", BindingFlags.Instance | BindingFlags.Public);
                string text = textProperty?.GetValue(badge) as string;

                if (!string.IsNullOrWhiteSpace(text))
                    texts.Add(text);
            }

            return texts.ToArray();
        }

        private static string GetStepBranchKey(object model, string title)
        {
            PropertyInfo itemsProperty = model.GetType().GetProperty("Items", BindingFlags.Instance | BindingFlags.Public);
            IEnumerable items = itemsProperty?.GetValue(model) as IEnumerable;

            Assert.IsNotNull(items, "Expected tree items.");

            foreach (object item in items)
            {
                if (item == null)
                    continue;

                PropertyInfo titleProperty = item.GetType().GetProperty("Title", BindingFlags.Instance | BindingFlags.Public);
                if (!string.Equals(titleProperty?.GetValue(item) as string, title, System.StringComparison.Ordinal))
                    continue;

                PropertyInfo kindProperty = item.GetType().GetProperty("Kind", BindingFlags.Instance | BindingFlags.Public);
                if ((int)(kindProperty?.GetValue(item) ?? -1) != 1)
                    continue;

                PropertyInfo branchKeyProperty = item.GetType().GetProperty("BranchKey", BindingFlags.Instance | BindingFlags.Public);
                object branchKey = branchKeyProperty?.GetValue(item);
                Assert.IsNotNull(branchKey, "Expected branch key.");
                return branchKey.ToString();
            }

            Assert.Fail($"Step '{title}' was not found in the tree model.");
            return null;
        }

        private static int CountTreeItems(object model, string title, string kindName)
        {
            PropertyInfo itemsProperty = model.GetType().GetProperty("Items", BindingFlags.Instance | BindingFlags.Public);
            IEnumerable items = itemsProperty?.GetValue(model) as IEnumerable;
            Assert.IsNotNull(items, "Expected tree items.");

            int count = 0;

            foreach (object item in items)
            {
                if (item == null)
                    continue;

                PropertyInfo titleProperty = item.GetType().GetProperty("Title", BindingFlags.Instance | BindingFlags.Public);
                PropertyInfo kindProperty = item.GetType().GetProperty("Kind", BindingFlags.Instance | BindingFlags.Public);

                if (!string.Equals(titleProperty?.GetValue(item) as string, title, StringComparison.Ordinal))
                    continue;

                if (!string.Equals(kindProperty?.GetValue(item)?.ToString(), kindName, StringComparison.Ordinal))
                    continue;

                count++;
            }

            return count;
        }

        private void ApplyChanges()
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
        }

        private static void ClearWindowLaunchRequest()
        {
            SessionState.SetInt("BC.Editor.ActionSystem.ActionInlineWindowLauncher.TargetInstanceId", 0);
            SessionState.EraseString("BC.Editor.ActionSystem.ActionInlineWindowLauncher.PropertyPath");
            SessionState.EraseString("BC.Editor.ActionSystem.ActionInlineWindowLauncher.BindingKey");
        }

        private static void CloseInlineActionWindows()
        {
            Type windowType = GetTypeByFullName("BC.Editor.ActionSystem.ActionInlineWindow");
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(windowType);

            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] is EditorWindow window)
                    window.Close();
            }
        }

        private static int GetInlineActionWindowCount()
        {
            Type windowType = GetTypeByFullName("BC.Editor.ActionSystem.ActionInlineWindow");
            return Resources.FindObjectsOfTypeAll(windowType).Length;
        }

        private static PropertyDrawer CreateDrawer(string fullTypeName)
        {
            Type drawerType = GetTypeByFullName(fullTypeName);
            return Activator.CreateInstance(drawerType) as PropertyDrawer;
        }

        private static object InvokeDeclaredMethod(Type type, object instance, string methodName, params object[] args)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            MethodInfo[] methods = type.GetMethods(flags);
            MethodInfo method = null;

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo candidate = methods[i];

                if (!string.Equals(candidate.Name, methodName, StringComparison.Ordinal))
                    continue;

                ParameterInfo[] parameters = candidate.GetParameters();

                if (parameters.Length != args.Length)
                    continue;

                if (!AreCompatible(parameters, args))
                    continue;

                method = candidate;
                break;
            }

            Assert.IsNotNull(method, $"Expected method '{methodName}' on '{type.FullName}'.");
            return method.Invoke(instance, args);
        }

        private static bool AreCompatible(ParameterInfo[] parameters, object[] args)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                object argument = args[i];
                Type parameterType = parameters[i].ParameterType;

                if (argument == null)
                {
                    if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) == null)
                        return false;

                    continue;
                }

                if (parameterType.IsAssignableFrom(argument.GetType()))
                    continue;

                return false;
            }

            return true;
        }

        private static int CountMissingChildSlots(object stepAuthoring)
        {
            if (stepAuthoring == null)
                return 0;

            IEnumerable childSlots = InvokeDeclaredMethod(
                stepAuthoring.GetType(),
                stepAuthoring,
                "GetChildActionSlots") as IEnumerable;

            if (childSlots == null)
                return 0;

            int missingCount = 0;

            foreach (object childSlot in childSlots)
            {
                if (childSlot == null)
                    continue;

                PropertyInfo isPresentProperty = childSlot.GetType().GetProperty("IsPresent", BindingFlags.Instance | BindingFlags.Public);
                PropertyInfo actionProperty = childSlot.GetType().GetProperty("Action", BindingFlags.Instance | BindingFlags.Public);
                bool isPresent = isPresentProperty != null && (bool)(isPresentProperty.GetValue(childSlot) ?? false);
                object action = actionProperty?.GetValue(childSlot);

                if (!isPresent || action == null)
                    missingCount++;
            }

            return missingCount;
        }

        private static Type GetTypeByFullName(string fullTypeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullTypeName, false);

                if (type != null)
                    return type;
            }

            Assert.Fail($"Type '{fullTypeName}' was not found.");
            return null;
        }

    }
}

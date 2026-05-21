using System;
using BC.ActionSystem;
using BC.Editor.Foundation.IMGUI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BC.Base.Tests
{
    public sealed class InlineActionDrawerTests
    {
        private InlineActionDrawerTestHost host;
        private SerializedObject serializedObject;

        [SetUp]
        public void SetUp()
        {
            host = ScriptableObject.CreateInstance<InlineActionDrawerTestHost>();
            serializedObject = new SerializedObject(host);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null)
                UnityEngine.Object.DestroyImmediate(host);
        }

        [Test]
        public void ActionStepSummaryUtility_UsesDisplayNameOverride_ThenFallsBackToWaitFramesTemplate()
        {
            SerializedProperty stepsProperty = FindStepsProperty();
            ManagedReferenceListController.AddNewElement(stepsProperty, typeof(WaitFramesStepAuthoring));

            SerializedProperty stepProperty = stepsProperty.GetArrayElementAtIndex(0);
            stepProperty.FindPropertyRelative("DisplayName").stringValue = "  Custom Label  ";
            ApplyChanges();

            Assert.AreEqual("Custom Label", GetSummary(stepProperty));

            stepProperty.FindPropertyRelative("DisplayName").stringValue = string.Empty;
            stepProperty.FindPropertyRelative("frames").intValue = 3;
            ApplyChanges();

            Assert.AreEqual("3 frames", GetSummary(stepProperty));
        }

        [Test]
        public void ActionStepManagedReferenceUtility_SetDisplayName_TrimsWhitespaceAndClearsBlankValues()
        {
            SerializedProperty stepsProperty = FindStepsProperty();
            ManagedReferenceListController.AddNewElement(stepsProperty, typeof(WaitFramesStepAuthoring));
            ApplyChanges();

            SerializedProperty stepProperty = stepsProperty.GetArrayElementAtIndex(0);
            InvokeSetDisplayName(stepProperty.propertyPath, "  Renamed Step  ");
            serializedObject.Update();

            Assert.AreEqual("Renamed Step", stepProperty.FindPropertyRelative("DisplayName").stringValue);

            InvokeSetDisplayName(stepProperty.propertyPath, "   ");
            serializedObject.Update();

            Assert.AreEqual(string.Empty, stepProperty.FindPropertyRelative("DisplayName").stringValue);
        }

        [Test]
        public void InlineActionDrawer_GetPropertyHeight_ForEmptyAction_IncludesLabelListAndFooter()
        {
            PropertyDrawer drawer = ReactiveValueTestUtility.CreateDrawer("BC.Editor.Action.InlineActionDrawer");
            SerializedProperty property = serializedObject.FindProperty("inlineAction");
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float expectedHeight = (lineHeight * 3f) + (spacing * 2f);

            Assert.That(drawer.GetPropertyHeight(property, new GUIContent("Action")), Is.EqualTo(expectedHeight).Within(0.001f));
        }

        private SerializedProperty FindStepsProperty()
        {
            SerializedProperty inlineActionProperty = serializedObject.FindProperty("inlineAction");
            Assert.IsNotNull(inlineActionProperty, "Expected inlineAction property.");

            SerializedProperty stepsProperty = inlineActionProperty.FindPropertyRelative("_steps");
            Assert.IsNotNull(stepsProperty, "Expected InlineAction._steps property.");
            return stepsProperty;
        }

        private void ApplyChanges()
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
        }

        private static string GetSummary(SerializedProperty stepProperty)
        {
            Type utilityType = ReactiveValueTestUtility.GetTypeByFullName("BC.Editor.Action.ActionStepSummaryUtility");
            object summary = ReactiveValueTestUtility.InvokeDeclaredMethod(utilityType, null, "GetSummary", stepProperty);
            Assert.IsNotNull(summary, "Expected non-null summary.");
            return (string)summary;
        }

        private void InvokeSetDisplayName(string stepPropertyPath, string displayName)
        {
            Type utilityType = ReactiveValueTestUtility.GetTypeByFullName("BC.Editor.Action.ActionStepManagedReferenceUtility");
            ReactiveValueTestUtility.InvokeDeclaredMethod(
                utilityType,
                null,
                "SetDisplayName",
                serializedObject.targetObjects,
                stepPropertyPath,
                displayName,
                "Rename Action Label");
        }

        private sealed class InlineActionDrawerTestHost : ScriptableObject
        {
            public InlineAction inlineAction = new();
        }
    }
}
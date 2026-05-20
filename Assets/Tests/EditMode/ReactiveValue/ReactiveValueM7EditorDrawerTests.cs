using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BC.Base.Tests
{
    public sealed class ReactiveValueM7EditorDrawerTests
    {
        private const string EvaluationModeType = "BC.Base.ReactiveEvaluationMode";
        private const string FailurePolicyType = "BC.Base.ReactiveFailurePolicy";
        private const string FloatSourceKindType = "BC.Base.ReactiveFloatSourceKind";
        private const string IntSourceKindType = "BC.Base.ReactiveIntSourceKind";
        private const string BoolSourceKindType = "BC.Base.ReactiveBoolSourceKind";
        private const string Vector3SourceKindType = "BC.Base.ReactiveVector3SourceKind";
        private const string EntitySourceKindType = "BC.Base.ReactiveEntitySourceKind";
        private const string StringSourceKindType = "BC.Base.ReactiveStringSourceKind";
        private const string FaceExpressionSourceKindType = "BC.Base.ReactiveFaceExpressionIdSourceKind";
        private const string EntityMoveStateSourceKindType = "BC.Base.ReactiveEntityMoveStateSourceKind";
        private const string TransformSourceKindType = "BC.Base.ReactiveTransformSourceKind";
        private const string TargetResolveModeType = "BC.Base.EntityTargetResolveMode";
        private const string ValueStoreWriteKindType = "BC.ActionSystem.ValueStoreWriteValueKind";
        private const string ValueStoreWriteScopeType = "BC.ActionSystem.ValueStoreWriteStoreScope";

        private ScriptableObject host;
        private SerializedObject serializedObject;

        [SetUp]
        public void SetUp()
        {
            host = ReactiveValueTestUtility.CreateDrawerHost();
            serializedObject = new SerializedObject(host);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null)
                UnityEngine.Object.DestroyImmediate(host);
        }

        [Test]
        public void ReactiveFloatDrawerNormalizesLiteralWatchedAndMeasuresDistanceFallbackHeight()
        {
            SerializedProperty property = ReactiveValueTestUtility.FindRootProperty(serializedObject, "reactiveFloat");
            PropertyDrawer drawer = ReactiveValueTestUtility.CreateDrawer("BC.Editor.ReactiveFloatDrawer");

            SetEnum(property.FindPropertyRelative("sourceKind"), FloatSourceKindType, "Literal");
            SetEnum(property.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Watched");
            ApplyChanges();

            NormalizeEvaluationMode(drawer, property);
            AssertEnum(property.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Snapshot");

            SetEnum(property.FindPropertyRelative("sourceKind"), FloatSourceKindType, "Distance");
            SetEnum(property.FindPropertyRelative("failurePolicy"), FailurePolicyType, "UseFallback");
            ApplyChanges();

            float expectedHeight =
                BaseDrawerHeight() +
                ControlDelta(EditorGUI.GetPropertyHeight(property.FindPropertyRelative("distance").FindPropertyRelative("fromEntity"), true)) +
                ControlDelta(EditorGUI.GetPropertyHeight(property.FindPropertyRelative("distance").FindPropertyRelative("toEntity"), true)) +
                ControlDelta(EditorGUI.GetPropertyHeight(property.FindPropertyRelative("fallbackValue"), true)) -
                EditorGUIUtility.standardVerticalSpacing;

            Assert.That(drawer.GetPropertyHeight(property, GUIContent.none), Is.EqualTo(expectedHeight).Within(0.001f));
        }

        [Test]
        public void ReactiveIntDrawerNormalizesEntityStoreContinuousToWatched()
        {
            SerializedProperty property = ReactiveValueTestUtility.FindRootProperty(serializedObject, "reactiveInt");
            PropertyDrawer drawer = ReactiveValueTestUtility.CreateDrawer("BC.Editor.ReactiveIntDrawer");

            SetEnum(property.FindPropertyRelative("sourceKind"), IntSourceKindType, "EntityValueStore");
            SetEnum(property.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Continuous");
            ApplyChanges();

            NormalizeEvaluationMode(drawer, property);
            AssertEnum(property.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Watched");
        }

        [Test]
        public void ReactiveIntDrawerNormalizesLocalStoreContinuousToWatched()
        {
            SerializedProperty property = ReactiveValueTestUtility.FindRootProperty(serializedObject, "reactiveInt");
            PropertyDrawer drawer = ReactiveValueTestUtility.CreateDrawer("BC.Editor.ReactiveIntDrawer");

            SetEnum(property.FindPropertyRelative("sourceKind"), IntSourceKindType, "LocalValueStore");
            SetEnum(property.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Continuous");
            AssignValueKey(property.FindPropertyRelative("localValue").FindPropertyRelative("key"), "BC.Base.ValueKeys+Local+Values", "Int0", typeof(int));
            ApplyChanges();

            NormalizeEvaluationMode(drawer, property);
            AssertEnum(property.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Watched");
        }

        [Test]
        public void ReactiveBoolDrawerNormalizesEntityAliveWatchedAndMeasuresCompareFloatFallbackHeight()
        {
            SerializedProperty property = ReactiveValueTestUtility.FindRootProperty(serializedObject, "reactiveBool");
            PropertyDrawer drawer = ReactiveValueTestUtility.CreateDrawer("BC.Editor.ReactiveBoolDrawer");

            SetEnum(property.FindPropertyRelative("sourceKind"), BoolSourceKindType, "EntityAlive");
            SetEnum(property.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Watched");
            ApplyChanges();

            NormalizeEvaluationMode(drawer, property);
            AssertEnum(property.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Snapshot");

            SetEnum(property.FindPropertyRelative("sourceKind"), BoolSourceKindType, "CompareFloat");
            SetEnum(property.FindPropertyRelative("failurePolicy"), FailurePolicyType, "UseFallback");
            ApplyChanges();

            SerializedProperty compareProperty = property.FindPropertyRelative("compareFloat");
            float expectedHeight =
                BaseDrawerHeight() +
                ControlDelta(EditorGUI.GetPropertyHeight(compareProperty.FindPropertyRelative("left"), true)) +
                ControlDelta(EditorGUI.GetPropertyHeight(compareProperty.FindPropertyRelative("right"), true)) +
                ControlDelta(EditorGUI.GetPropertyHeight(compareProperty.FindPropertyRelative("comparison"), true)) +
                ControlDelta(EditorGUI.GetPropertyHeight(compareProperty.FindPropertyRelative("epsilon"), true)) +
                ControlDelta(EditorGUI.GetPropertyHeight(property.FindPropertyRelative("fallbackValue"), true)) -
                EditorGUIUtility.standardVerticalSpacing;

            Assert.That(drawer.GetPropertyHeight(property, GUIContent.none), Is.EqualTo(expectedHeight).Within(0.001f));
        }

        [Test]
        public void ReactiveVector3DrawerNormalizesTransformPayloadAndMeasuresDirectionFallbackHeight()
        {
            SerializedProperty property = ReactiveValueTestUtility.FindRootProperty(serializedObject, "reactiveVector3");
            PropertyDrawer drawer = ReactiveValueTestUtility.CreateDrawer("BC.Editor.ReactiveVector3Drawer");

            SetEnum(property.FindPropertyRelative("sourceKind"), Vector3SourceKindType, "EntityTransformPosition");
            SerializedProperty transformValue = property.FindPropertyRelative("transformValue");
            SetEnum(transformValue.FindPropertyRelative("sourceKind"), TransformSourceKindType, "Forward");
            ApplyChanges();

            NormalizeTransformSourceKind(transformValue, "Position");
            AssertEnum(transformValue.FindPropertyRelative("sourceKind"), TransformSourceKindType, "Position");

            SetEnum(property.FindPropertyRelative("sourceKind"), Vector3SourceKindType, "Direction");
            SetEnum(property.FindPropertyRelative("failurePolicy"), FailurePolicyType, "UseFallback");
            ApplyChanges();

            SerializedProperty directionProperty = property.FindPropertyRelative("directionSource");
            float expectedHeight =
                BaseDrawerHeight() +
                ControlDelta(EditorGUI.GetPropertyHeight(directionProperty.FindPropertyRelative("fromEntity"), true)) +
                ControlDelta(EditorGUI.GetPropertyHeight(directionProperty.FindPropertyRelative("toEntity"), true)) +
                ControlDelta(EditorGUI.GetPropertyHeight(property.FindPropertyRelative("fallbackValue"), true)) -
                EditorGUIUtility.standardVerticalSpacing;

            Assert.That(drawer.GetPropertyHeight(property, GUIContent.none), Is.EqualTo(expectedHeight).Within(0.001f));
        }

        [Test]
        public void ReactiveEntityDrawerNormalizesTargetReferenceWatchedAndAdjustsHeightForTagSearchAndFallback()
        {
            SerializedProperty property = ReactiveValueTestUtility.FindRootProperty(serializedObject, "reactiveEntity");
            PropertyDrawer drawer = ReactiveValueTestUtility.CreateDrawer("BC.Editor.ReactiveEntityRefDrawer");

            SetEnum(property.FindPropertyRelative("sourceKind"), EntitySourceKindType, "TargetReference");
            SetEnum(property.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Watched");
            ApplyChanges();

            NormalizeEvaluationMode(drawer, property);
            AssertEnum(property.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Snapshot");

            SetEnum(property.FindPropertyRelative("failurePolicy"), FailurePolicyType, "UseFallback");
            SerializedProperty targetReference = property.FindPropertyRelative("targetReference");
            SetEnum(targetReference.FindPropertyRelative("mode"), TargetResolveModeType, "Self");
            ApplyChanges();

            float selfHeight = drawer.GetPropertyHeight(property, GUIContent.none);

            SetEnum(targetReference.FindPropertyRelative("mode"), TargetResolveModeType, "TagSearch");
            ApplyChanges();

            float tagSearchHeight = drawer.GetPropertyHeight(property, GUIContent.none);
            float expectedExtraHeight =
                ControlDelta(EditorGUIUtility.singleLineHeight) +
                ControlDelta(EditorGUI.GetPropertyHeight(targetReference.FindPropertyRelative("tag"), true));

            Assert.That(tagSearchHeight - selfHeight, Is.EqualTo(expectedExtraHeight).Within(0.001f));
        }

        [Test]
        public void ReactiveStringDrawerNormalizesEntityStoreContinuousToWatched()
        {
            SerializedProperty property = ReactiveValueTestUtility.FindRootProperty(serializedObject, "reactiveString");
            PropertyDrawer drawer = ReactiveValueTestUtility.CreateDrawer("BC.Editor.ReactiveStringDrawer");

            SetEnum(property.FindPropertyRelative("sourceKind"), StringSourceKindType, "EntityValueStore");
            SetEnum(property.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Continuous");
            ApplyChanges();

            NormalizeEvaluationMode(drawer, property);
            AssertEnum(property.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Watched");
        }

        [Test]
        public void ReactiveSpecialEnumDrawersNormalizeUnsupportedModes()
        {
            SerializedProperty faceProperty = ReactiveValueTestUtility.FindRootProperty(serializedObject, "reactiveFaceExpression");
            PropertyDrawer faceDrawer = ReactiveValueTestUtility.CreateDrawer("BC.Editor.ReactiveFaceExpressionIdDrawer");

            SetEnum(faceProperty.FindPropertyRelative("sourceKind"), FaceExpressionSourceKindType, "Literal");
            SetEnum(faceProperty.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Watched");
            ApplyChanges();

            NormalizeEvaluationMode(faceDrawer, faceProperty);
            AssertEnum(faceProperty.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Snapshot");

            SerializedProperty moveStateProperty = ReactiveValueTestUtility.FindRootProperty(serializedObject, "reactiveEntityMoveState");
            PropertyDrawer moveStateDrawer = ReactiveValueTestUtility.CreateDrawer("BC.Editor.ReactiveEntityMoveStateDrawer");

            SetEnum(moveStateProperty.FindPropertyRelative("sourceKind"), EntityMoveStateSourceKindType, "EntityValueStore");
            SetEnum(moveStateProperty.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Continuous");
            ApplyChanges();

            NormalizeEvaluationMode(moveStateDrawer, moveStateProperty);
            AssertEnum(moveStateProperty.FindPropertyRelative("evaluationMode"), EvaluationModeType, "Watched");
        }

        [Test]
        public void ValueStoreWriteDrawerSwitchesActiveValueFieldForAutoAndExplicitKinds()
        {
            SerializedProperty property = ReactiveValueTestUtility.FindRootProperty(serializedObject, "valueStoreWrite");

            SetEnum(property.FindPropertyRelative("storeScope"), ValueStoreWriteScopeType, "Entity");
            SetEnum(property.FindPropertyRelative("valueKind"), ValueStoreWriteKindType, "Auto");
            AssignValueKey(property.FindPropertyRelative("key"), "BC.Base.ValueKeys+Runtime", "FaceExpression", ReactiveValueTestUtility.GetTypeByFullName("BC.Base.FaceExpressionId"));
            ApplyChanges();

            AssertActiveValuePropertyPath(property, "faceExpressionValue");

            SetEnum(property.FindPropertyRelative("valueKind"), ValueStoreWriteKindType, "String");
            AssignValueKey(property.FindPropertyRelative("key"), "BC.Base.ValueKeys+Identity", "DisplayName", typeof(string));
            ApplyChanges();

            AssertActiveValuePropertyPath(property, "stringValue");

            SetEnum(property.FindPropertyRelative("storeScope"), ValueStoreWriteScopeType, "Local");
            SetEnum(property.FindPropertyRelative("valueKind"), ValueStoreWriteKindType, "Auto");
            AssignValueKey(property.FindPropertyRelative("key"), "BC.Base.ValueKeys+Local+Values", "Int0", typeof(int));
            ApplyChanges();

            AssertActiveValuePropertyPath(property, "intValue");
        }

        [Test]
        public void ValueStoreWriteDrawerFiltersLocalScopeToLocalKeysOnly()
        {
            Type drawerType = ReactiveValueTestUtility.GetTypeByFullName("BC.Editor.ValueStoreWriteAuthoringDrawer");
            MethodInfo method = drawerType.GetMethod("GetFilteredDescriptors", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Expected ValueStoreWriteAuthoringDrawer.GetFilteredDescriptors.");

            object descriptors = method.Invoke(
                null,
                new object[]
                {
                    ReactiveValueTestUtility.ParseEnum(ValueStoreWriteScopeType, "Local"),
                    ReactiveValueTestUtility.ParseEnum(ValueStoreWriteKindType, "Int"),
                });

            int count = 0;
            foreach (object descriptor in (System.Collections.IEnumerable)descriptors)
            {
                string path = (string)descriptor.GetType().GetProperty("Path")?.GetValue(descriptor);
                Assert.IsTrue(path.StartsWith("Local.", StringComparison.Ordinal), $"Expected Local.* key but got '{path}'.");
                count++;
            }

            Assert.Greater(count, 0);
        }

        private void ApplyChanges()
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();
        }

        private static void NormalizeEvaluationMode(PropertyDrawer drawer, SerializedProperty property)
        {
            ReactiveValueTestUtility.InvokeDeclaredMethod(
                ReactiveValueTestUtility.GetTypeByFullName("BC.Editor.ReactiveValueDrawerBase"),
                drawer,
                "NormalizeEvaluationMode",
                property.FindPropertyRelative("evaluationMode"),
                property.FindPropertyRelative("sourceKind").enumValueIndex);
        }

        private static void NormalizeTransformSourceKind(SerializedProperty property, string expectedKind)
        {
            ReactiveValueTestUtility.InvokeDeclaredMethod(
                ReactiveValueTestUtility.GetTypeByFullName("BC.Editor.ReactiveValueDrawerBase"),
                null,
                "NormalizeTransformSourceKind",
                property,
                ReactiveValueTestUtility.ParseEnum(TransformSourceKindType, expectedKind));
        }

        private static void SetEnum(SerializedProperty property, string typeName, string memberName)
        {
            property.enumValueIndex = System.Convert.ToInt32(ReactiveValueTestUtility.ParseEnum(typeName, memberName));
        }

        private static void AssertEnum(SerializedProperty property, string typeName, string memberName)
        {
            Assert.AreEqual(System.Convert.ToInt32(ReactiveValueTestUtility.ParseEnum(typeName, memberName)), property.enumValueIndex);
        }

        private static void AssignValueKey(SerializedProperty property, string hostTypeName, string fieldName, Type valueType)
        {
            object key = ReactiveValueTestUtility.GetStaticFieldValue(hostTypeName, fieldName);
            object reference = ReactiveValueTestUtility.InvokeGenericStatic("BC.Base.ValueKeyReference", "From", valueType, key);

            property.FindPropertyRelative("id").intValue = (int)ReactiveValueTestUtility.GetPropertyValue(reference, "RawId");
            property.FindPropertyRelative("path").stringValue = (string)ReactiveValueTestUtility.GetPropertyValue(reference, "Path");
            property.FindPropertyRelative("valueTypeName").stringValue = (string)ReactiveValueTestUtility.GetPropertyValue(reference, "ValueTypeName");
        }

        private static void AssertActiveValuePropertyPath(SerializedProperty property, string expectedPropertyName)
        {
            Type drawerType = ReactiveValueTestUtility.GetTypeByFullName("BC.Editor.ValueStoreWriteAuthoringDrawer");
            MethodInfo method = drawerType.GetMethod("TryResolveActiveValueProperty", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Expected ValueStoreWriteAuthoringDrawer.TryResolveActiveValueProperty.");

            object[] arguments =
            {
                property,
                property.FindPropertyRelative("valueKind"),
                property.FindPropertyRelative("key"),
                null,
                null,
            };

            bool resolved = (bool)method.Invoke(null, arguments);
            Assert.IsTrue(resolved);
            SerializedProperty valueProperty = arguments[3] as SerializedProperty;
            Assert.IsNotNull(valueProperty);
            Assert.AreEqual(expectedPropertyName, valueProperty.name);
        }

        private static float BaseDrawerHeight()
        {
            return ControlDelta(EditorGUIUtility.singleLineHeight) * 3f;
        }

        private static float ControlDelta(float controlHeight)
        {
            return controlHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
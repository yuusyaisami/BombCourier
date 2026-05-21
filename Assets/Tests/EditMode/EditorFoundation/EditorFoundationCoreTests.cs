using BC.Editor.Foundation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Tests
{
    public sealed class EditorFoundationCoreTests
    {
        private EditorFoundationTestHost host;
        private SerializedObject serializedObject;

        [SetUp]
        public void SetUp()
        {
            host = ScriptableObject.CreateInstance<EditorFoundationTestHost>();
            host.reference = new TestManagedReference { name = "Source", amount = 7 };
            serializedObject = new SerializedObject(host);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null)
                Object.DestroyImmediate(host);
        }

        [Test]
        public void EditorStateKeyIncludesTargetPathAndSuffix()
        {
            SerializedProperty property = serializedObject.FindProperty("label");
            string key = EditorStateKey.ForProperty(property, "foldout");

            Assert.That(key, Does.Contain("label"));
            Assert.That(key, Does.Contain("foldout"));
        }

        [Test]
        public void SerializedPropertyPathUtilityResolvesArrayElementIndex()
        {
            SerializedProperty values = serializedObject.FindProperty("values");
            values.arraySize = 3;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Update();

            SerializedProperty element = serializedObject.FindProperty("values").GetArrayElementAtIndex(2);

            Assert.IsTrue(SerializedPropertyPathUtility.IsArrayElement(element));
            Assert.IsTrue(SerializedPropertyPathUtility.TryGetArrayElementIndex(element, out int index));
            Assert.AreEqual(2, index);
            Assert.AreEqual("values.Array", SerializedPropertyPathUtility.GetParentPath(element.propertyPath));
        }

        [Test]
        public void EditorBindingContextFindsRootProperty()
        {
            EditorBindingContext context = new(serializedObject, "reference", "Host");

            Assert.IsTrue(context.TryFindRootProperty(out SerializedProperty property));
            Assert.AreEqual("reference", property.name);
            Assert.That(context.StateKey, Does.Contain("reference"));
        }
    }
}

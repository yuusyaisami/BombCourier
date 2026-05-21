using BC.Editor.Foundation.IMGUI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Tests
{
    public sealed class EditorFoundationIMGUITests
    {
        [Test]
        public void RectLayoutUtilityTakesLinesAndAdvancesCursor()
        {
            Rect cursor = new(0f, 0f, 100f, 100f);

            Rect first = RectLayoutUtility.TakeHeight(ref cursor, 20f);
            Rect second = RectLayoutUtility.TakeLine(ref cursor);

            Assert.AreEqual(20f, first.height);
            Assert.AreEqual(100f, first.width);
            Assert.Greater(second.y, first.yMax);
            Assert.Less(cursor.height, 100f);
        }

        [Test]
        public void ManagedReferenceListControllerClonesBySerializedValue()
        {
            TestManagedReference source = new() { name = "Clone", amount = 12 };

            object cloneObject = ManagedReferenceListController.CloneManagedReference(source);
            TestManagedReference clone = cloneObject as TestManagedReference;

            Assert.IsNotNull(clone);
            Assert.AreNotSame(source, clone);
            Assert.AreEqual("Clone", clone.name);
            Assert.AreEqual(12, clone.amount);
        }

        [Test]
        public void InlineListControllerReturnsSingleLineForEmptyArray()
        {
            InlineListController controller = new(
                (_, _) => 20f,
                (_, _, _) => { });

            EditorFoundationTestHost host = ScriptableObject.CreateInstance<EditorFoundationTestHost>();
            SerializedObject serializedObject = new(host);
            SerializedProperty values = serializedObject.FindProperty("values");

            try
            {
                Assert.AreEqual(EditorGUIUtility.singleLineHeight, controller.GetHeight(values));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}

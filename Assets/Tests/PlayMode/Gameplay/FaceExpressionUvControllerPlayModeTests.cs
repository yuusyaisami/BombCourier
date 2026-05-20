using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class FaceExpressionUvControllerPlayModeTests
    {
        private const string ControllerTypeName = "BC.Character.FaceExpressionUvControllerMB";
        private const string RemapperTypeName = "BC.Character.MeshUvRectRemapperMB";
        private const string UvSetTypeName = "BC.Character.FaceExpressionUvSet";
        private const string FaceExpressionTypeName = "BC.Base.FaceExpressionId";

        private readonly List<UnityEngine.Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object createdObject = createdObjects[i];
                if (createdObject != null)
                    UnityEngine.Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator MissingExpression_FallsBackToNeutralUvRect()
        {
            GameObject root = CreateFaceRoot("FaceFallbackRoot");
            ScriptableObject uvSet = CreateUvSet(
                new ExpressionEntrySpec(0, new Rect(20f, 30f, 20f, 20f), false, 0),
                new ExpressionEntrySpec(4, new Rect(60f, 10f, 20f, 20f), false, 0));

            Component controller = AttachController(root, uvSet, minBlinkInterval: 100f, maxBlinkInterval: 100f, blinkDuration: 0.03f);

            root.SetActive(true);
            yield return null;

            InvokeMethod(controller, "SetExpression", ParseFaceExpression("Running"));
            yield return null;

            AssertMeshUvRect(root.GetComponent<MeshFilter>().sharedMesh.uv, 0.20f, 0.30f, 0.20f, 0.20f);
        }

        [UnityTest]
        public IEnumerator BlinkTemporarilyOverridesAndRestoresBaseExpression()
        {
            GameObject root = CreateFaceRoot("FaceBlinkRoot");
            ScriptableObject uvSet = CreateUvSet(
                new ExpressionEntrySpec(0, new Rect(10f, 10f, 20f, 20f), true, 4),
                new ExpressionEntrySpec(4, new Rect(60f, 10f, 20f, 20f), false, 0));

            Component controller = AttachController(root, uvSet, minBlinkInterval: 100f, maxBlinkInterval: 100f, blinkDuration: 0.03f);

            root.SetActive(true);
            yield return null;

            InvokeMethod(controller, "SetExpression", ParseFaceExpression("Neutral"));
            yield return null;

            AssertMeshUvRect(root.GetComponent<MeshFilter>().sharedMesh.uv, 0.10f, 0.10f, 0.20f, 0.20f);

            SetPrivateField(controller, "nextBlinkTime", Time.time - 1f);
            yield return null;

            AssertMeshUvRect(root.GetComponent<MeshFilter>().sharedMesh.uv, 0.60f, 0.10f, 0.20f, 0.20f);

            yield return new WaitForSeconds(0.05f);

            AssertMeshUvRect(root.GetComponent<MeshFilter>().sharedMesh.uv, 0.10f, 0.10f, 0.20f, 0.20f);
        }

        private GameObject CreateFaceRoot(string name)
        {
            GameObject root = new GameObject(name);
            root.SetActive(false);
            createdObjects.Add(root);

            MeshFilter meshFilter = root.AddComponent<MeshFilter>();
            root.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = CreateQuadMesh();

            return root;
        }

        private Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh
            {
                name = "FaceQuad"
            };

            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f),
                new Vector3(1f, 1f, 0f),
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };
            createdObjects.Add(mesh);
            return mesh;
        }

        private ScriptableObject CreateUvSet(params ExpressionEntrySpec[] entries)
        {
            Type uvSetType = FindRuntimeType(UvSetTypeName);
            ScriptableObject uvSet = ScriptableObject.CreateInstance(uvSetType);
            createdObjects.Add(uvSet);

            Texture2D atlasTexture = new Texture2D(100, 100, TextureFormat.RGBA32, false)
            {
                name = "FaceAtlas"
            };
            createdObjects.Add(atlasTexture);

            SetPrivateField(uvSet, "atlasTexture", atlasTexture);
            SetPrivateField(uvSet, "pixelRectUsesTopLeftOrigin", false);
            SetPrivateField(uvSet, "entries", BuildEntriesArray(uvSetType, entries));

            return uvSet;
        }

        private Component AttachController(GameObject root, ScriptableObject uvSet, float minBlinkInterval, float maxBlinkInterval, float blinkDuration)
        {
            Component remapper = root.AddComponent(FindRuntimeType(RemapperTypeName));
            Component controller = root.AddComponent(FindRuntimeType(ControllerTypeName));

            SetPrivateField(controller, "remapper", remapper);
            SetPrivateField(controller, "uvSet", uvSet);
            SetPrivateField(controller, "bindToRuntimeFaceExpression", false);
            SetPrivateField(controller, "enableBlink", true);
            SetPrivateField(controller, "minBlinkInterval", minBlinkInterval);
            SetPrivateField(controller, "maxBlinkInterval", maxBlinkInterval);
            SetPrivateField(controller, "blinkDuration", blinkDuration);

            return controller;
        }

        private Array BuildEntriesArray(Type uvSetType, ExpressionEntrySpec[] entries)
        {
            Type entryType = uvSetType.GetNestedType("Entry", BindingFlags.NonPublic);
            Type blinkType = uvSetType.GetNestedType("BlinkSettings", BindingFlags.NonPublic);

            Assert.IsNotNull(entryType, "Expected FaceExpressionUvSet.Entry to exist.");
            Assert.IsNotNull(blinkType, "Expected FaceExpressionUvSet.BlinkSettings to exist.");

            Array entryArray = Array.CreateInstance(entryType, entries.Length);
            Type faceExpressionType = FindRuntimeType(FaceExpressionTypeName);

            for (int i = 0; i < entries.Length; i++)
            {
                object blinkSettings = Activator.CreateInstance(blinkType);
                SetFieldValue(blinkSettings, "enabled", entries[i].BlinkEnabled);
                SetFieldValue(blinkSettings, "blinkExpression", Enum.ToObject(faceExpressionType, entries[i].BlinkExpressionId));

                object entry = Activator.CreateInstance(entryType);
                SetFieldValue(entry, "expression", Enum.ToObject(faceExpressionType, entries[i].ExpressionId));
                SetFieldValue(entry, "pixelRect", entries[i].PixelRect);
                SetFieldValue(entry, "blink", blinkSettings);
                entryArray.SetValue(entry, i);
            }

            return entryArray;
        }

        private static object ParseFaceExpression(string name)
        {
            Type enumType = FindRuntimeType(FaceExpressionTypeName);
            return Enum.Parse(enumType, name);
        }

        private static void AssertMeshUvRect(IReadOnlyList<Vector2> uvs, float x, float y, float width, float height)
        {
            Assert.AreEqual(4, uvs.Count, "Expected the quad test mesh to keep four UVs.");
            AssertVector2(uvs[0], new Vector2(x, y));
            AssertVector2(uvs[1], new Vector2(x + width, y));
            AssertVector2(uvs[2], new Vector2(x, y + height));
            AssertVector2(uvs[3], new Vector2(x + width, y + height));
        }

        private static void AssertVector2(Vector2 actual, Vector2 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
        }

        private static Type FindRuntimeType(string fullTypeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullTypeName);
                if (type != null)
                    return type;
            }

            Assert.Fail($"Expected runtime type to exist: {fullTypeName}");
            return null;
        }

        private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static void SetFieldValue(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static void InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            method.Invoke(target, args);
        }

        private readonly struct ExpressionEntrySpec
        {
            public ExpressionEntrySpec(int expressionId, Rect pixelRect, bool blinkEnabled, int blinkExpressionId)
            {
                ExpressionId = expressionId;
                PixelRect = pixelRect;
                BlinkEnabled = blinkEnabled;
                BlinkExpressionId = blinkExpressionId;
            }

            public int ExpressionId { get; }
            public Rect PixelRect { get; }
            public bool BlinkEnabled { get; }
            public int BlinkExpressionId { get; }
        }
    }
}
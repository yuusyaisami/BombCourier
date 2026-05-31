using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class ConditionDrivenColliderObjectPlayModeTests
    {
        private const string ConditionDrivenColliderObjectTypeName = "BC.Gimmick.ConditionDrivenColliderObject.ConditionDrivenColliderObjectMB";
        private const string ReactiveBoolTypeName = "BC.Base.ReactiveBool";
        private const string ReactiveEvaluationModeTypeName = "BC.Base.ReactiveEvaluationMode";

        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                GameObject createdObject = createdObjects[i];
                if (createdObject != null)
                    UnityEngine.Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator LiteralTrue_EnablesColliderAndKeepsOpaqueAlpha()
        {
            GameObject gameObject = CreateCube("ConditionColliderTrue");
            Collider collider = gameObject.GetComponent<Collider>();
            Renderer renderer = gameObject.GetComponent<Renderer>();
            Component target = gameObject.AddComponent(FindRuntimeType(ConditionDrivenColliderObjectTypeName));

            ConfigureTarget(target, collider, renderer, literalValue: true, enableColliderWhenConditionTrue: true, enabledAlpha: 1f, disabledAlpha: 0.25f);

            yield return null;

            Assert.IsTrue(collider.enabled, "Literal true should enable the collider when the flag is not inverted.");
            AssertPropertyBlockAlpha(renderer, 1f);
        }

        [UnityTest]
        public IEnumerator LiteralTrue_WithInversion_DisablesColliderAndLowersAlpha()
        {
            GameObject gameObject = CreateCube("ConditionColliderInvert");
            Collider collider = gameObject.GetComponent<Collider>();
            Renderer renderer = gameObject.GetComponent<Renderer>();
            Component target = gameObject.AddComponent(FindRuntimeType(ConditionDrivenColliderObjectTypeName));

            ConfigureTarget(target, collider, renderer, literalValue: true, enableColliderWhenConditionTrue: false, enabledAlpha: 1f, disabledAlpha: 0.25f);

            yield return null;

            Assert.IsFalse(collider.enabled, "Literal true should disable the collider when the flag is inverted.");
            AssertPropertyBlockAlpha(renderer, 0.25f);
        }

        [Test]
        public void OnValidate_RebuildsConditionBinding_WhenLiteralChanges()
        {
            GameObject gameObject = CreateCube("ConditionColliderRebuild");
            Collider collider = gameObject.GetComponent<Collider>();
            Renderer renderer = gameObject.GetComponent<Renderer>();
            Component target = gameObject.AddComponent(FindRuntimeType(ConditionDrivenColliderObjectTypeName));

            ConfigureTarget(target, collider, renderer, literalValue: true, enableColliderWhenConditionTrue: true, enabledAlpha: 1f, disabledAlpha: 0.3f);
            Assert.IsTrue(collider.enabled, "Initial literal true should enable the collider.");

            SetPrivateField(target, "condition", BuildLiteralReactiveBool(false));
            InvokeMethod(target, "OnValidate");

            Assert.IsFalse(collider.enabled, "Rebuilt literal false should disable the collider.");
            AssertPropertyBlockAlpha(renderer, 0.3f);
        }

        [Test]
        public void MissingKernelForKernelValueCondition_FailsClosed()
        {
            GameObject gameObject = CreateCube("ConditionColliderKernelFailure");
            Collider collider = gameObject.GetComponent<Collider>();
            Renderer renderer = gameObject.GetComponent<Renderer>();
            Component target = gameObject.AddComponent(FindRuntimeType(ConditionDrivenColliderObjectTypeName));

            SetPrivateField(target, "targetColliders", new[] { collider });
            SetPrivateField(target, "targetRenderers", new[] { renderer });
            SetPrivateField(target, "condition", BuildKernelReactiveBool());
            SetPrivateField(target, "enableColliderWhenConditionTrue", true);
            SetPrivateField(target, "enabledAlpha", 1f);
            SetPrivateField(target, "disabledAlpha", 0.2f);

            LogAssert.Expect(LogType.Error, new Regex("ConditionDrivenColliderObjectMB"));
            InvokeMethod(target, "OnValidate");

            Assert.IsFalse(collider.enabled, "Kernel-backed condition without SceneKernel should fail closed.");
            Assert.IsFalse(GetPropertyValue<bool>(target, "ConditionReadSucceeded"), "Condition read should be reported as failed.");
            AssertPropertyBlockAlpha(renderer, 0.2f);
        }

        private void ConfigureTarget(
            Component target,
            Collider collider,
            Renderer renderer,
            bool literalValue,
            bool enableColliderWhenConditionTrue,
            float enabledAlpha,
            float disabledAlpha)
        {
            SetPrivateField(target, "targetColliders", new[] { collider });
            SetPrivateField(target, "targetRenderers", new[] { renderer });
            SetPrivateField(target, "condition", BuildLiteralReactiveBool(literalValue));
            SetPrivateField(target, "enableColliderWhenConditionTrue", enableColliderWhenConditionTrue);
            SetPrivateField(target, "enabledAlpha", enabledAlpha);
            SetPrivateField(target, "disabledAlpha", disabledAlpha);
            InvokeMethod(target, "OnValidate");
        }

        private static object BuildLiteralReactiveBool(bool value)
        {
            object evaluationMode = ParseEnumValue(ReactiveEvaluationModeTypeName, "Snapshot");
            return InvokeStaticMethod(ReactiveBoolTypeName, "LiteralValue", value, evaluationMode);
        }

        private static object BuildKernelReactiveBool()
        {
            object boolKey = GetStaticFieldValue("BC.Base.ValueKeys+Kernel+Gimmick", "GlobalEnabled");
            object keyReference = InvokeGenericStaticMethod("BC.Base.ValueKeyReference", "From", typeof(bool), boolKey);
            object evaluationMode = ParseEnumValue(ReactiveEvaluationModeTypeName, "Watched");
            return InvokeStaticMethod(ReactiveBoolTypeName, "KernelValueStore", keyReference, evaluationMode);
        }

        private GameObject CreateCube(string name)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void AssertPropertyBlockAlpha(Renderer renderer, float expectedAlpha)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            Color appliedBaseColor = propertyBlock.GetColor(Shader.PropertyToID("_BaseColor"));
            Assert.AreEqual(expectedAlpha, appliedBaseColor.a, 0.001f);
        }

        private static object ParseEnumValue(string fullTypeName, string enumName)
        {
            Type enumType = FindRuntimeType(fullTypeName);
            return Enum.Parse(enumType, enumName, ignoreCase: false);
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

        private static void InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            method.Invoke(target, args);
        }

        private static T GetPropertyValue<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property on {target.GetType().Name}: {propertyName}");
            return (T)property.GetValue(target);
        }

        private static object GetStaticFieldValue(string fullTypeName, string fieldName)
        {
            Type type = FindRuntimeType(fullTypeName);
            FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected static field: {fullTypeName}.{fieldName}");
            return field.GetValue(null);
        }

        private static object InvokeStaticMethod(string fullTypeName, string methodName, params object[] args)
        {
            Type type = FindRuntimeType(fullTypeName);
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected static method: {fullTypeName}.{methodName}");
            return method.Invoke(null, args);
        }

        private static object InvokeGenericStaticMethod(string fullTypeName, string methodName, Type genericType, params object[] args)
        {
            Type type = FindRuntimeType(fullTypeName);
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != methodName || !method.IsGenericMethodDefinition)
                    continue;

                MethodInfo closedMethod = method.MakeGenericMethod(genericType);
                return closedMethod.Invoke(null, args);
            }

            Assert.Fail($"Expected generic static method: {fullTypeName}.{methodName}<{genericType.Name}>");
            return null;
        }
    }
}

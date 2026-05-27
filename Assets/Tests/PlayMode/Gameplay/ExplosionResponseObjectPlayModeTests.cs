using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class ExplosionResponseObjectPlayModeTests
    {
        private const string ExplosionResponseObjectTypeName = "BC.Gimmick.ExplosionResponseObject.ExplosionResponseObjectMB";
        private const string ExplosionResponseModeTypeName = "BC.Gimmick.ExplosionResponseObject.ExplosionResponseMode";

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
        public IEnumerator TimerMode_TurnsOffAfterDuration_AndLegacyReceiverStillTriggers()
        {
            Component responseObject = CreateResponseObject("TimerResponse", "Timer", 0.08f, 0.5f);

            InvokeMethod(responseObject, "OnBombImpactReceived", Vector3.forward, 2.0f);

            Assert.IsTrue(GetPropertyValue<bool>(responseObject, "IsActive"), "Legacy bomb impact receiver should activate the response object.");

            yield return WaitUntilOrTimeout(
                () => !GetPropertyValue<bool>(responseObject, "IsActive"),
                0.25f,
                "Timer mode did not return to Off after the active duration elapsed.");
        }

        [Test]
        public void ToggleMode_FlipsStateOnEachExplosionImpact()
        {
            Component responseObject = CreateResponseObject("ToggleResponse", "Toggle", 0.1f, 0.0f);

            InvokeMethod(responseObject, "OnExplosionImpactReceived", Vector3.right, 1.0f);
            Assert.IsTrue(GetPropertyValue<bool>(responseObject, "IsActive"), "First explosion impact should turn Toggle mode On.");

            InvokeMethod(responseObject, "OnExplosionImpactReceived", Vector3.left, 1.0f);
            Assert.IsFalse(GetPropertyValue<bool>(responseObject, "IsActive"), "Second explosion impact should turn Toggle mode Off.");
        }

        [Test]
        public void OnceMode_StaysOnAfterRepeatedExplosionImpacts()
        {
            Component responseObject = CreateResponseObject("OnceResponse", "Once", 0.1f, 0.0f);

            InvokeMethod(responseObject, "OnExplosionImpactReceived", Vector3.up, 1.0f);
            Assert.IsTrue(GetPropertyValue<bool>(responseObject, "IsActive"), "First explosion impact should turn Once mode On.");

            InvokeMethod(responseObject, "OnExplosionImpactReceived", Vector3.down, 3.0f);
            Assert.IsTrue(GetPropertyValue<bool>(responseObject, "IsActive"), "Once mode must stay On after later explosion impacts.");
        }

        [Test]
        public void MinimumImpactForce_FiltersWeakExplosions()
        {
            Component responseObject = CreateResponseObject("ThresholdResponse", "Toggle", 0.1f, 2.0f);

            InvokeMethod(responseObject, "OnExplosionImpactReceived", Vector3.forward, 1.0f);

            Assert.IsFalse(GetPropertyValue<bool>(responseObject, "IsActive"), "Explosion weaker than the threshold should be ignored.");
            Assert.AreEqual(1.0f, GetPropertyValue<float>(responseObject, "LastImpactForce"), 0.0001f, "LastImpactForce should still reflect the received explosion strength.");
        }

        private Component CreateResponseObject(string name, string modeName, float activeDuration, float minimumImpactForce)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);

            gameObject.AddComponent<SphereCollider>();

            Component responseObject = gameObject.AddComponent(FindRuntimeType(ExplosionResponseObjectTypeName));
            SetPrivateField(responseObject, "mode", ParseEnumValue(ExplosionResponseModeTypeName, modeName));
            SetPrivateField(responseObject, "activeDuration", activeDuration);
            SetPrivateField(responseObject, "minimumImpactForce", minimumImpactForce);
            return responseObject;
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

        private static T GetPropertyValue<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property on {target.GetType().Name}: {propertyName}");
            return (T)property.GetValue(target);
        }

        private static void InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            method.Invoke(target, args);
        }

        private static IEnumerator WaitUntilOrTimeout(Func<bool> condition, float timeoutSeconds, string timeoutMessage)
        {
            float elapsed = 0f;

            while (elapsed < timeoutSeconds)
            {
                if (condition())
                    yield break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Fail(timeoutMessage);
        }
    }
}
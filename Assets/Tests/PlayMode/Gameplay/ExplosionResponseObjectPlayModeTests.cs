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
        private const string ValueStoreServiceTypeName = "BC.Base.ValueStoreService";
        private const string EntityIdAllocatorTypeName = "BC.Base.EntityIdAllocator";
        private const string GameLogicSnapshotUtilityTypeName = "BC.Base.GameLogicValueStoreSnapshotUtility";
        private const string ValueKeysGameLogicTalkTypeName = "BC.Base.ValueKeys+GameLogic+Talk";
        private const string ValueKeysGameLogicInteractionTypeName = "BC.Base.ValueKeys+GameLogic+Interaction";
        private const string ValueKeysRuntimeTypeName = "BC.Base.ValueKeys+Runtime";

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

        [UnityTest]
        public IEnumerator CheckpointRestore_ReappliesActiveTimerState()
        {
            Component responseObject = CreateResponseObject("CheckpointResponse", "Timer", 0.12f, 0.0f);

            InvokeMethod(responseObject, "OnExplosionImpactReceived", Vector3.forward, 1.0f);
            object checkpoint = InvokeMethodWithResult(responseObject, "CaptureStageState");

            Assert.IsTrue(GetPropertyValue<bool>(responseObject, "IsActive"), "Timer mode should be active immediately after impact.");

            yield return WaitUntilOrTimeout(
                () => !GetPropertyValue<bool>(responseObject, "IsActive"),
                0.3f,
                "Timer mode did not switch off before checkpoint restore.");

            InvokeMethod(responseObject, "RestoreStageState", checkpoint);
            Assert.IsTrue(GetPropertyValue<bool>(responseObject, "IsActive"), "Checkpoint restore should reactivate the saved Timer state.");

            yield return WaitUntilOrTimeout(
                () => !GetPropertyValue<bool>(responseObject, "IsActive"),
                0.3f,
                "Restored Timer state did not expire after the saved remaining duration.");
        }

        [Test]
        public void GameLogicSnapshot_RestoresOnlyGameLogicKeys()
        {
            object store = Activator.CreateInstance(FindRuntimeType(ValueStoreServiceTypeName));
            object allocator = Activator.CreateInstance(FindRuntimeType(EntityIdAllocatorTypeName));
            object entity = InvokeMethodWithResult(allocator, "Allocate");
            object talkCountKey = GetStaticFieldValue(ValueKeysGameLogicTalkTypeName, "TalkCount");
            object isStateRedKey = GetStaticFieldValue(ValueKeysGameLogicInteractionTypeName, "IsStateRed");
            object isDeadKey = GetStaticFieldValue(ValueKeysRuntimeTypeName, "IsDead");
            Type snapshotUtilityType = FindRuntimeType(GameLogicSnapshotUtilityTypeName);

            InvokeGenericMethod(store, "Set", typeof(int), entity, talkCountKey, 7);
            InvokeGenericMethod(store, "Set", typeof(bool), entity, isStateRedKey, true);
            InvokeGenericMethod(store, "Set", typeof(bool), entity, isDeadKey, false);

            object snapshot = InvokeStaticMethodWithResult(snapshotUtilityType, "Capture", store, entity);

            InvokeGenericMethod(store, "Set", typeof(int), entity, talkCountKey, 99);
            InvokeGenericMethod(store, "Set", typeof(bool), entity, isStateRedKey, false);
            InvokeGenericMethod(store, "Set", typeof(bool), entity, isDeadKey, true);

            InvokeStaticMethod(snapshotUtilityType, "Restore", store, entity, snapshot);

            Assert.AreEqual(7, InvokeGenericMethodWithResult(store, "Get", typeof(int), entity, talkCountKey), "GameLogic int values should be restored from the checkpoint snapshot.");
            Assert.AreEqual(true, InvokeGenericMethodWithResult(store, "Get", typeof(bool), entity, isStateRedKey), "GameLogic bool values should be restored from the checkpoint snapshot.");
            Assert.AreEqual(true, InvokeGenericMethodWithResult(store, "Get", typeof(bool), entity, isDeadKey), "Non-GameLogic values must not be overwritten by the snapshot restore.");
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

        private static object InvokeMethodWithResult(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, args);
        }

        private static void InvokeGenericMethod(object target, string methodName, Type genericType, params object[] args)
        {
            MethodInfo method = FindGenericMethod(target.GetType(), methodName, genericType, args);
            Assert.IsNotNull(method, $"Expected generic method on {target.GetType().Name}: {methodName}<{genericType.Name}>");
            method.Invoke(target, args);
        }

        private static object InvokeGenericMethodWithResult(object target, string methodName, Type genericType, params object[] args)
        {
            MethodInfo method = FindGenericMethod(target.GetType(), methodName, genericType, args);
            Assert.IsNotNull(method, $"Expected generic method on {target.GetType().Name}: {methodName}<{genericType.Name}>");
            return method.Invoke(target, args);
        }

        private static MethodInfo FindGenericMethod(Type targetType, string methodName, Type genericType, object[] args)
        {
            MethodInfo[] methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != methodName || !method.IsGenericMethodDefinition)
                    continue;

                Type[] genericArgs = method.GetGenericArguments();
                if (genericArgs.Length != 1)
                    continue;

                MethodInfo closedMethod = method.MakeGenericMethod(genericType);
                ParameterInfo[] parameters = closedMethod.GetParameters();
                if (parameters.Length != args.Length)
                    continue;

                bool matches = true;
                for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                {
                    object argument = args[parameterIndex];
                    if (argument == null)
                        continue;

                    if (!parameters[parameterIndex].ParameterType.IsInstanceOfType(argument))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return closedMethod;
            }

            return null;
        }

        private static object GetStaticFieldValue(string fullTypeName, string fieldName)
        {
            Type type = FindRuntimeType(fullTypeName);
            FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected static field on {fullTypeName}: {fieldName}");
            return field.GetValue(null);
        }

        private static void InvokeStaticMethod(Type targetType, string methodName, params object[] args)
        {
            MethodInfo method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected static method on {targetType.FullName}: {methodName}");
            method.Invoke(null, args);
        }

        private static object InvokeStaticMethodWithResult(Type targetType, string methodName, params object[] args)
        {
            MethodInfo method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected static method on {targetType.FullName}: {methodName}");
            return method.Invoke(null, args);
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

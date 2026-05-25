using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class PlayerMoveM2RuntimeStateIntentPlayModeTests
    {
        private const string EntityMoveMotorTypeName = "BC.Base.EntityMoveMotorMB";
        private const string EntityMoveRuntimeStateTypeName = "BC.Base.EntityMoveRuntimeState";
        private const string EntityMoveIntentTypeName = "BC.Base.EntityMoveIntent";
        private const string AutoMoveStateTypeName = "BC.Base.AutoMoveState";
        private const string AutoMoveDriverTypeName = "BC.Base.AutoMoveDriver";

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

        [Test]
        public void EntityMoveIntent_Clear_ResetsAllFields()
        {
            Type intentType = FindRuntimeType(EntityMoveIntentTypeName);
            object boxedIntent = Activator.CreateInstance(intentType);

            SetFieldValue(ref boxedIntent, "WorldMoveDirection", new Vector3(1.0f, 0.0f, -0.5f));
            SetFieldValue(ref boxedIntent, "HasMoveInput", true);
            SetFieldValue(ref boxedIntent, "SprintHeld", true);
            SetFieldValue(ref boxedIntent, "JumpPressed", true);
            SetFieldValue(ref boxedIntent, "JumpHeld", true);
            SetFieldValue(ref boxedIntent, "IsAutoMove", true);

            InvokeMethod(ref boxedIntent, "Clear");

            AssertVectorApproximately(Vector3.zero, GetFieldValue<Vector3>(boxedIntent, "WorldMoveDirection"));
            Assert.IsFalse(GetFieldValue<bool>(boxedIntent, "HasMoveInput"));
            Assert.IsFalse(GetFieldValue<bool>(boxedIntent, "SprintHeld"));
            Assert.IsFalse(GetFieldValue<bool>(boxedIntent, "JumpPressed"));
            Assert.IsFalse(GetFieldValue<bool>(boxedIntent, "JumpHeld"));
            Assert.IsFalse(GetFieldValue<bool>(boxedIntent, "IsAutoMove"));
        }

        [Test]
        public void AutoMoveDriver_BuildDirectionAndCancel_UpdatesStateConsistently()
        {
            GameObject root = new GameObject("M2AutoMoveRoot");
            createdObjects.Add(root);

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.position = Vector3.zero;

            object autoMoveState = Activator.CreateInstance(FindRuntimeType(AutoMoveStateTypeName));
            object autoMoveDriver = Activator.CreateInstance(FindRuntimeType(AutoMoveDriverTypeName));

            object cancellationSource = InvokeMethod(autoMoveDriver, "BeginNew", autoMoveState);
            Assert.IsNotNull(cancellationSource);
            Assert.IsNotNull(GetFieldValue<object>(autoMoveState, "ActiveCancellationTokenSource"));

            InvokeMethod(autoMoveDriver, "BeginMove", autoMoveState, new Vector3(2.0f, 0.0f, 0.0f), 0.2f);

            Vector3 firstDirection = (Vector3)InvokeMethod(autoMoveDriver, "BuildDirection", autoMoveState, body);
            Assert.Greater(firstDirection.x, 0.9f);
            Assert.IsTrue(GetFieldValue<bool>(autoMoveState, "IsActive"));
            Assert.IsFalse(GetFieldValue<bool>(autoMoveState, "ReachedTarget"));

            body.position = new Vector3(2.0f, 0.0f, 0.0f);
            Vector3 reachedDirection = (Vector3)InvokeMethod(autoMoveDriver, "BuildDirection", autoMoveState, body);
            AssertVectorApproximately(Vector3.zero, reachedDirection);
            Assert.IsTrue(GetFieldValue<bool>(autoMoveState, "ReachedTarget"));

            InvokeMethod(autoMoveDriver, "CompleteTarget", autoMoveState);
            Assert.IsFalse(GetFieldValue<bool>(autoMoveState, "IsActive"));

            InvokeMethod(autoMoveDriver, "Cancel", autoMoveState);
            Assert.IsFalse(GetFieldValue<bool>(autoMoveState, "IsActive"));
            Assert.IsFalse(GetFieldValue<bool>(autoMoveState, "ReachedTarget"));

            InvokeMethod(autoMoveDriver, "CompleteAndDispose", autoMoveState, cancellationSource);
            Assert.IsNull(GetFieldValue<object>(autoMoveState, "ActiveCancellationTokenSource"));
        }

        [Test]
        public void EntityMoveMotor_RuntimeStateBackedProperties_AreConsistent()
        {
            Component moveMotor = CreateMotorRoot("M2RuntimeStateMotor", Vector3.zero);

            object runtimeState = GetPrivateField<object>(moveMotor, "runtimeState");
            object channels = GetFieldValue<object>(runtimeState, "Velocity");
            SetFieldValue(ref channels, "InputPlanar", new Vector3(1.5f, 0.0f, -0.5f));
            SetFieldValue(ref channels, "Vertical", 3.0f);
            SetFieldValue(ref channels, "External", new Vector3(-0.5f, 1.0f, 0.25f));
            SetFieldValue(ref channels, "SupportCarry", new Vector3(0.2f, 0.0f, 0.0f));
            SetFieldValue(ref channels, "InheritedSupport", new Vector3(0.3f, 0.0f, 0.1f));
            SetFieldValue(ref runtimeState, "Velocity", channels);
            SetFieldValue(ref runtimeState, "IsDead", true);

            AssertVectorApproximately(new Vector3(1.5f, 0.0f, -0.5f), GetPropertyValue<Vector3>(moveMotor, "PlanarVelocity"));
            Assert.AreEqual(3.0f, GetPropertyValue<float>(moveMotor, "VerticalVelocity"), 0.0001f);
            AssertVectorApproximately(new Vector3(-0.5f, 1.0f, 0.25f), GetPropertyValue<Vector3>(moveMotor, "ExternalVelocity"));
            AssertVectorApproximately(new Vector3(0.2f, 0.0f, 0.0f), GetPropertyValue<Vector3>(moveMotor, "PlatformVelocity"));
            AssertVectorApproximately(new Vector3(1.5f, 4.0f, -0.15f), GetPropertyValue<Vector3>(moveMotor, "CurrentVelocity"));
            Assert.IsTrue(GetPropertyValue<bool>(moveMotor, "IsDead"));
        }

        [Test]
        public void EntityMoveMotor_SetMoveIntentAndClearMoveIntent_UseIntentAndRuntimeState()
        {
            Component moveMotor = CreateMotorRoot("M2IntentMotor", Vector3.zero);

            InvokeMethod(moveMotor, "SetMoveIntent", new Vector3(2.0f, 0.5f, 0.0f), true, true, true, 0.02f);

            object currentIntent = GetPrivateField<object>(moveMotor, "currentIntent");
            object runtimeState = GetPrivateField<object>(moveMotor, "runtimeState");

            Vector3 moveDirection = GetFieldValue<Vector3>(currentIntent, "WorldMoveDirection");
            Assert.AreEqual(0.0f, moveDirection.magnitude, 0.0001f);
            Assert.IsFalse(GetFieldValue<bool>(currentIntent, "SprintHeld"));
            Assert.IsFalse(GetFieldValue<bool>(currentIntent, "JumpHeld"));
            Assert.Less(GetFieldValue<float>(runtimeState, "JumpBufferCounter"), 0.0f);

            InvokeMethod(moveMotor, "ClearMoveIntent");

            currentIntent = GetPrivateField<object>(moveMotor, "currentIntent");
            runtimeState = GetPrivateField<object>(moveMotor, "runtimeState");

            AssertVectorApproximately(Vector3.zero, GetFieldValue<Vector3>(currentIntent, "WorldMoveDirection"));
            Assert.IsFalse(GetFieldValue<bool>(currentIntent, "SprintHeld"));
            Assert.IsFalse(GetFieldValue<bool>(currentIntent, "JumpHeld"));
            Assert.AreEqual(0.0f, GetFieldValue<float>(runtimeState, "JumpBufferCounter"), 0.0001f);
        }

        private Component CreateMotorRoot(string name, Vector3 position)
        {
            GameObject root = new GameObject(name);
            createdObjects.Add(root);
            root.transform.position = position;

            root.AddComponent<Rigidbody>();
            root.AddComponent<CapsuleCollider>();
            Component moveMotor = root.AddComponent(FindRuntimeType(EntityMoveMotorTypeName));

            InvokeMethod(moveMotor, "EnsureMovementBody");
            return moveMotor;
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

        private static object InvokeMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, arguments);
        }

        private static void InvokeMethod(ref object boxedStruct, string methodName, params object[] arguments)
        {
            MethodInfo method = boxedStruct.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {boxedStruct.GetType().Name}: {methodName}");
            method.Invoke(boxedStruct, arguments);
        }

        private static T GetPropertyValue<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property on {target.GetType().Name}: {propertyName}");
            return (T)property.GetValue(target);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            return (T)field.GetValue(target);
        }

        private static T GetFieldValue<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field on {target.GetType().Name}: {fieldName}");
            return (T)field.GetValue(target);
        }

        private static void SetFieldValue<T>(ref object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class PlayerMoveM8VelocityChannelsPlayModeTests
    {
        private const string VelocityChannelsTypeName = "BC.Base.VelocityChannels";
        private const string VelocityComposerTypeName = "BC.Base.VelocityComposer";
        private const string EntityMoveMotorTypeName = "BC.Base.EntityMoveMotorMB";

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
        public void VelocityComposer_ComposeFinalVelocity_SumsExpectedChannels()
        {
            object channels = Activator.CreateInstance(FindRuntimeType(VelocityChannelsTypeName));
            SetFieldValue(channels, "InputPlanar", new Vector3(2.0f, 0.0f, 1.0f));
            SetFieldValue(channels, "Vertical", 3.0f);
            SetFieldValue(channels, "External", new Vector3(-1.0f, 0.5f, 0.0f));
            SetFieldValue(channels, "InheritedSupport", new Vector3(0.3f, 0.0f, -0.2f));
            SetFieldValue(channels, "SupportCarry", new Vector3(1.0f, 0.0f, 0.2f));
            SetFieldValue(channels, "ConstraintCorrectionVelocity", new Vector3(0.1f, 0.0f, 0.0f));

            Vector3 finalVelocity = (Vector3)InvokeStatic(
                VelocityComposerTypeName,
                "ComposeFinalVelocity",
                channels);

            Assert.AreEqual(2.4f, finalVelocity.x, 0.0001f);
            Assert.AreEqual(3.5f, finalVelocity.y, 0.0001f);
            Assert.AreEqual(1.0f, finalVelocity.z, 0.0001f);
        }

        [Test]
        public void VelocityComposer_CurrentPlanarSpeed_UsesInputPlanarOnly()
        {
            object channels = Activator.CreateInstance(FindRuntimeType(VelocityChannelsTypeName));
            SetFieldValue(channels, "InputPlanar", new Vector3(3.0f, 10.0f, 4.0f));
            SetFieldValue(channels, "External", new Vector3(50.0f, 0.0f, 0.0f));
            SetFieldValue(channels, "SupportCarry", new Vector3(40.0f, 0.0f, 0.0f));

            float speed = (float)InvokeStatic(
                VelocityComposerTypeName,
                "CurrentPlanarSpeed",
                channels);

            Assert.AreEqual(5.0f, speed, 0.0001f);
        }

        [Test]
        public void EntityMoveMotor_PublicVelocityProperties_MatchM8Contract()
        {
            GameObject motorObject = new GameObject("M8_Motor");
            createdObjects.Add(motorObject);

            motorObject.AddComponent<Rigidbody>();
            motorObject.AddComponent<CapsuleCollider>();
            Component motor = motorObject.AddComponent(FindRuntimeType(EntityMoveMotorTypeName));

            object runtimeState = GetPrivateField<object>(motor, "runtimeState");
            object channels = Activator.CreateInstance(FindRuntimeType(VelocityChannelsTypeName));
            SetFieldValue(channels, "InputPlanar", new Vector3(2.0f, 0.0f, 1.0f));
            SetFieldValue(channels, "Vertical", 3.0f);
            SetFieldValue(channels, "External", new Vector3(-1.0f, 0.5f, 0.0f));
            SetFieldValue(channels, "InheritedSupport", new Vector3(0.3f, 0.0f, -0.2f));
            SetFieldValue(channels, "SupportCarry", new Vector3(1.0f, 0.0f, 0.2f));
            SetFieldValue(channels, "ConstraintCorrectionVelocity", new Vector3(0.1f, 0.0f, 0.0f));
            SetFieldValue(runtimeState, "Velocity", channels);

            Vector3 planar = GetPropertyValue<Vector3>(motor, "PlanarVelocity");
            float vertical = GetPropertyValue<float>(motor, "VerticalVelocity");
            Vector3 external = GetPropertyValue<Vector3>(motor, "ExternalVelocity");
            Vector3 platform = GetPropertyValue<Vector3>(motor, "PlatformVelocity");
            Vector3 current = GetPropertyValue<Vector3>(motor, "CurrentVelocity");
            float planarSpeed = GetPropertyValue<float>(motor, "CurrentPlanarSpeed");

            Assert.AreEqual(2.0f, planar.x, 0.0001f);
            Assert.AreEqual(1.0f, planar.z, 0.0001f);
            Assert.AreEqual(3.0f, vertical, 0.0001f);
            Assert.AreEqual(-1.0f, external.x, 0.0001f);
            Assert.AreEqual(1.0f, platform.x, 0.0001f);

            Assert.AreEqual(2.4f, current.x, 0.0001f);
            Assert.AreEqual(3.5f, current.y, 0.0001f);
            Assert.AreEqual(1.0f, current.z, 0.0001f);
            Assert.AreEqual(Mathf.Sqrt(5.0f), planarSpeed, 0.0001f);
        }

        [Test]
        public void EntityMoveMotor_SyncFromLegacy_DoesNotEraseConstraintChannel()
        {
            GameObject motorObject = new GameObject("M8_Motor_ConstraintSync");
            createdObjects.Add(motorObject);

            motorObject.AddComponent<Rigidbody>();
            motorObject.AddComponent<CapsuleCollider>();
            Component motor = motorObject.AddComponent(FindRuntimeType(EntityMoveMotorTypeName));

            object runtimeState = GetPrivateField<object>(motor, "runtimeState");
            object channels = Activator.CreateInstance(FindRuntimeType(VelocityChannelsTypeName));
            SetFieldValue(channels, "ConstraintCorrectionVelocity", new Vector3(0.7f, 0.0f, 0.0f));
            SetFieldValue(runtimeState, "Velocity", channels);

            SetFieldValue(runtimeState, "PlanarVelocity", new Vector3(1.0f, 0.0f, 0.0f));
            SetFieldValue(runtimeState, "VerticalVelocity", 2.0f);
            SetFieldValue(runtimeState, "ExternalVelocity", Vector3.zero);
            SetFieldValue(runtimeState, "InheritedSupportVelocity", Vector3.zero);
            SetFieldValue(runtimeState, "PlatformVelocity", Vector3.zero);

            InvokeInstance(motor, "SyncVelocityChannelsFromLegacyFields");

            object syncedChannels = GetFieldValue<object>(runtimeState, "Velocity");
            Vector3 constraint = GetFieldValue<Vector3>(syncedChannels, "ConstraintCorrectionVelocity");
            Assert.AreEqual(0.7f, constraint.x, 0.0001f);
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

        private static object InvokeStatic(string fullTypeName, string methodName, params object[] arguments)
        {
            Type type = FindRuntimeType(fullTypeName);
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected static method: {fullTypeName}.{methodName}");
            return method.Invoke(null, arguments);
        }

        private static object InvokeInstance(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected instance method: {target.GetType().Name}.{methodName}");
            return method.Invoke(target, arguments);
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

        private static T GetPropertyValue<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property on {target.GetType().Name}: {propertyName}");
            return (T)property.GetValue(target);
        }

        private static void SetFieldValue<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }
    }
}

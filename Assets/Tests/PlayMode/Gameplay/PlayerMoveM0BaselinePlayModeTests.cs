using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class PlayerMoveM0BaselinePlayModeTests
    {
        private const string EntityMoveMotorTypeName = "BC.Base.EntityMoveMotorMB";
        private const string PlayerMoveControllerTypeName = "BC.Base.PlayerMoveController";
        private const string PlayerRagdollControllerTypeName = "BC.Manager.PlayerRagdollControllerMB";
        private const string EntityLandingImpactTypeName = "BC.Base.EntityLandingImpactMB";
        private const string CushionImpactResultTypeName = "BC.Gimmick.Cushion.CushionImpactResult";
        private const string CushionImpactHandlerTypeName = "BC.Base.CushionImpactHandler";
        private const string CushionHighJumpBufferTypeName = "BC.Base.CushionHighJumpBuffer";

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
        public void PlayerMove_Grounded_OnFlatGround()
        {
            GameObject ground = new GameObject("FlatGround");
            createdObjects.Add(ground);
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(12f, 1f, 12f);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);

            Component moveMotor = CreateMotorRoot("GroundedPlayer", new Vector3(0f, 1.05f, 0f));

            InvokeMethod(moveMotor, "EnsureMovementBody");
            InvokeMethod(moveMotor, "ProbeGround");

            Assert.IsTrue(GetPropertyValue<bool>(moveMotor, "IsGrounded"));
        }

        [Test]
        public void PlayerMove_DoesNotDoubleApplyPlatformVelocity()
        {
            Component moveMotor = CreateMotorRoot("PlatformVelocityPlayer", Vector3.zero);
            Rigidbody rb = (Rigidbody)GetPrivateField<object>(moveMotor, "bodyRigidbody");
            object runtimeState = GetPrivateField<object>(moveMotor, "runtimeState");
            object channels = GetFieldValue<object>(runtimeState, "Velocity");
            SetFieldValue(channels, "SupportCarry", new Vector3(2f, 0f, 0f));
            SetFieldValue(runtimeState, "Velocity", channels);
            SetPrivateField(moveMotor, "suppressPlatformVelocityInjectionThisTick", true);

            InvokeMethod(moveMotor, "ApplyCurrentVelocityToBody", new Vector3(3f, 0f, 0f), true);
            AssertVectorApproximately(new Vector3(3f, 0f, 0f), rb.linearVelocity);
        }

        [Test]
        public void PlayerMove_JumpKeepsExistingHeight()
        {
            Component moveMotor = CreateMotorRoot("JumpPreservePlayer", Vector3.zero);

            object runtimeState = GetPrivateField<object>(moveMotor, "runtimeState");
            object channels = GetFieldValue<object>(runtimeState, "Velocity");
            SetFieldValue(channels, "Vertical", 6.0f);
            SetFieldValue(runtimeState, "Velocity", channels);
            SetFieldValue(runtimeState, "JumpBufferCounter", 0.1f);
            SetFieldValue(runtimeState, "LastGroundedTime", -999.0f);

            InvokeMethod(moveMotor, "UpdateVerticalVelocity", 0.02f, false);

            float resolvedVerticalVelocity = GetPropertyValue<float>(moveMotor, "VerticalVelocity");
            Assert.Greater(resolvedVerticalVelocity, 5.0f);
        }

        [Test]
        public void PlayerMove_CushionBounceStillWorks()
        {
            Component moveMotor = CreateMotorRoot("CushionBouncePlayer", Vector3.zero);
            object runtimeState = GetPrivateField<object>(moveMotor, "runtimeState");
            object highJumpBuffer = Activator.CreateInstance(FindRuntimeType(CushionHighJumpBufferTypeName));

            object impactResult = InvokeStaticMethod(
                CushionImpactResultTypeName,
                "Bounce",
                new Vector3(0f, 8f, 0f),
                8f,
                1f,
                true);

            object applyResult = InvokeStaticMethod(
                CushionImpactHandlerTypeName,
                "ApplyImpact",
                runtimeState,
                impactResult,
                true,
                false,
                highJumpBuffer,
                -3.0f,
                Time.time,
                0.12f);

            bool handled = GetPropertyValue<bool>(applyResult, "Handled");

            Assert.IsTrue(handled);
            object channels = GetFieldValue<object>(runtimeState, "Velocity");
            float vertical = GetFieldValue<float>(channels, "Vertical");
            Vector3 external = GetFieldValue<Vector3>(channels, "External");
            Assert.Greater(vertical, 7.5f);
            Assert.AreEqual(0.0f, external.y, 0.0001f);
        }

        [Test]
        public void PlayerMove_HardLandingUsesPreviousVerticalVelocity()
        {
            Component moveMotor = CreateMotorRoot("LandingPlayer", Vector3.zero);

            GameObject landingObject = ((Component)moveMotor).gameObject;
            Component landingImpact = landingObject.AddComponent(FindRuntimeType(EntityLandingImpactTypeName));

            SetPrivateField(landingImpact, "moveMotor", moveMotor);
            SetPrivateField(landingImpact, "previousVerticalVelocity", -17.0f);

            Vector3 landingVelocity = (Vector3)InvokeMethod(landingImpact, "BuildLandingVelocity");
            Assert.AreEqual(-17.0f, landingVelocity.y, 0.0001f);
        }

        [Test]
        public void PlayerMove_BombImpactStillKillsOrRagdolls()
        {
            GameObject root = new GameObject("BombImpactPlayer");
            createdObjects.Add(root);

            root.AddComponent<Rigidbody>();
            root.AddComponent<CapsuleCollider>();

            Component moveMotor = root.AddComponent(FindRuntimeType(EntityMoveMotorTypeName));
            Component playerController = root.AddComponent(FindRuntimeType(PlayerMoveControllerTypeName));
            Component ragdollController = root.AddComponent(FindRuntimeType(PlayerRagdollControllerTypeName));

            SetPrivateField(moveMotor, "currentCanMoveBySystem", true);
            SetPrivateField(playerController, "moveMotor", moveMotor);
            SetPrivateField(playerController, "ragdollController", ragdollController);

            InvokeMethod(playerController, "OnBombImpactReceived", Vector3.forward, 6.0f);

            Assert.IsTrue(GetPropertyValue<bool>(moveMotor, "IsDead"));
            Assert.IsTrue(GetPropertyValue<bool>(ragdollController, "IsRagdollActive"));
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

        private static object InvokeStaticMethod(string fullTypeName, string methodName, params object[] arguments)
        {
            Type type = FindRuntimeType(fullTypeName);
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected static method: {fullTypeName}.{methodName}");
            return method.Invoke(null, arguments);
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

        private static void SetFieldValue<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
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
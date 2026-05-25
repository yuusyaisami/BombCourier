using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class PlayerMoveM7SupportMotionTrackerPlayModeTests
    {
        private const string SupportMotionTrackerTypeName = "BC.Base.SupportMotionTracker";
        private const string EntityMoveRuntimeStateTypeName = "BC.Base.EntityMoveRuntimeState";
        private const string GroundHitInfoTypeName = "BC.Base.GroundHitInfo";
        private const string GroundSurfaceKindTypeName = "BC.Base.GroundSurfaceKind";

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
        public void SupportMotionTracker_SuppressesReattach_DuringCooldownWindow()
        {
            GameObject actor = CreateActor("Actor_ReattachSuppressed");
            GameObject platform = CreatePlatform("Platform_ReattachSuppressed", withRigidbody: false);

            object tracker = Activator.CreateInstance(FindRuntimeType(SupportMotionTrackerTypeName));
            object runtimeState = Activator.CreateInstance(FindRuntimeType(EntityMoveRuntimeStateTypeName));
            object ground = CreateGroundHitInfo(platform.GetComponent<Collider>(), platform.transform, isWalkable: true);

            InvokeInstance(
                tracker,
                "Update",
                actor.transform,
                actor.GetComponent<Rigidbody>(),
                ground,
                true,
                runtimeState,
                0.02f,
                1.0f,
                2.0f,
                0.0f,
                0.05f);

            Vector3 platformVelocity = GetFieldValue<Vector3>(runtimeState, "PlatformVelocity");
            object support = GetFieldValue<object>(runtimeState, "Support");

            Assert.AreEqual(0.0f, platformVelocity.x, 0.0001f);
            Assert.AreEqual(0.0f, platformVelocity.y, 0.0001f);
            Assert.AreEqual(0.0f, platformVelocity.z, 0.0001f);
            Assert.IsFalse(GetFieldValue<bool>(support, "HasSupport"));
        }

        [Test]
        public void SupportMotionTracker_FallbackPoseDelta_UpdatesPlatformVelocity()
        {
            GameObject actor = CreateActor("Actor_FallbackPose");
            GameObject platform = CreatePlatform("Platform_FallbackPose", withRigidbody: false);
            actor.transform.position = new Vector3(1.0f, 0.0f, 0.0f);

            object tracker = Activator.CreateInstance(FindRuntimeType(SupportMotionTrackerTypeName));
            object runtimeState = Activator.CreateInstance(FindRuntimeType(EntityMoveRuntimeStateTypeName));
            object ground = CreateGroundHitInfo(platform.GetComponent<Collider>(), platform.transform, isWalkable: true);

            InvokeInstance(
                tracker,
                "Update",
                actor.transform,
                actor.GetComponent<Rigidbody>(),
                ground,
                true,
                runtimeState,
                0.02f,
                10.0f,
                0.0f,
                0.0f,
                0.05f);

            InvokeInstance(tracker, "StorePlatformPose");

            platform.transform.rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);

            InvokeInstance(
                tracker,
                "Update",
                actor.transform,
                actor.GetComponent<Rigidbody>(),
                ground,
                true,
                runtimeState,
                0.02f,
                10.02f,
                0.0f,
                0.0f,
                0.05f);

            Vector3 platformVelocity = GetFieldValue<Vector3>(runtimeState, "PlatformVelocity");
            object support = GetFieldValue<object>(runtimeState, "Support");

            Assert.Greater(Mathf.Abs(platformVelocity.x), 40.0f);
            Assert.Greater(Mathf.Abs(platformVelocity.z), 40.0f);
            Assert.IsTrue(GetFieldValue<bool>(support, "HasSupport"));

            Vector3 passengerDelta = GetFieldValue<Vector3>(support, "PassengerDelta");
            Assert.Greater(Mathf.Abs(passengerDelta.x), 0.8f);
            Assert.Greater(Mathf.Abs(passengerDelta.z), 0.8f);
        }

        [Test]
        public void SupportMotionTracker_UsesRigidbodySupportVelocity_WhenAvailable()
        {
            GameObject actor = CreateActor("Actor_RigidbodySupport");
            GameObject platform = CreatePlatform("Platform_RigidbodySupport", withRigidbody: true);
            Rigidbody platformBody = platform.GetComponent<Rigidbody>();
            platformBody.linearVelocity = new Vector3(3.0f, 0.0f, 0.0f);

            object tracker = Activator.CreateInstance(FindRuntimeType(SupportMotionTrackerTypeName));
            object runtimeState = Activator.CreateInstance(FindRuntimeType(EntityMoveRuntimeStateTypeName));
            object ground = CreateGroundHitInfo(platform.GetComponent<Collider>(), platform.transform, isWalkable: true);

            InvokeInstance(
                tracker,
                "Update",
                actor.transform,
                actor.GetComponent<Rigidbody>(),
                ground,
                true,
                runtimeState,
                0.02f,
                30.0f,
                0.0f,
                0.0f,
                0.05f);

            Vector3 platformVelocity = GetFieldValue<Vector3>(runtimeState, "PlatformVelocity");
            object support = GetFieldValue<object>(runtimeState, "Support");

            Assert.Greater(platformVelocity.x, 2.8f);
            Assert.IsTrue(GetFieldValue<bool>(support, "HasSupport"));

            Vector3 passengerVelocity = GetFieldValue<Vector3>(support, "PassengerVelocity");
            Assert.Greater(passengerVelocity.x, 2.8f);
        }

        private GameObject CreateActor(string name)
        {
            GameObject actor = new GameObject(name);
            createdObjects.Add(actor);

            Rigidbody body = actor.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = false;

            return actor;
        }

        private GameObject CreatePlatform(string name, bool withRigidbody)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            createdObjects.Add(platform);

            if (withRigidbody)
            {
                Rigidbody body = platform.AddComponent<Rigidbody>();
                body.useGravity = false;
                body.isKinematic = false;
            }

            return platform;
        }

        private static object CreateGroundHitInfo(Collider collider, Transform supportTransform, bool isWalkable)
        {
            Type hitInfoType = FindRuntimeType(GroundHitInfoTypeName);
            Type kindType = FindRuntimeType(GroundSurfaceKindTypeName);
            object kind = Enum.Parse(kindType, isWalkable ? "Walkable" : "Wall");

            return Activator.CreateInstance(
                hitInfoType,
                true,
                collider,
                supportTransform,
                supportTransform.position,
                Vector3.up,
                0.01f,
                2.0f,
                kind,
                isWalkable);
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

        private static object InvokeInstance(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected instance method: {target.GetType().Name}.{methodName}");
            return method.Invoke(target, arguments);
        }

        private static T GetFieldValue<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field on {target.GetType().Name}: {fieldName}");
            return (T)field.GetValue(target);
        }
    }
}

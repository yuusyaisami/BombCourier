using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class PlayerMoveM5GroundSnapPlayModeTests
    {
        private const string GroundSnapSettingsTypeName = "BC.Base.GroundSnapSettings";
        private const string GroundSnapSolverTypeName = "BC.Base.GroundSnapSolver";
        private const string EntityMoveRuntimeStateTypeName = "BC.Base.EntityMoveRuntimeState";
        private const string GroundHitInfoTypeName = "BC.Base.GroundHitInfo";
        private const string GroundSurfaceKindTypeName = "BC.Base.GroundSurfaceKind";
        private const string GroundProbeSolverTypeName = "BC.Base.GroundProbeSolver";

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
        public void GroundSnapSettings_UsesM5WeakDefaults()
        {
            object settings = Activator.CreateInstance(FindRuntimeType(GroundSnapSettingsTypeName));

            Assert.AreEqual(0.12f, GetFieldValue<float>(settings, "MaxSnapDistance"), 0.0001f);
            Assert.AreEqual(8.0f, GetFieldValue<float>(settings, "SnapSpeed"), 0.0001f);
            Assert.AreEqual(0.12f, GetFieldValue<float>(settings, "MaxSnapDistancePerTick"), 0.0001f);
            Assert.AreEqual(0.12f, GetFieldValue<float>(settings, "DisableAfterJumpTime"), 0.0001f);
            Assert.AreEqual(0.16f, GetFieldValue<float>(settings, "DisableAfterSupportLaunchTime"), 0.0001f);
        }

        [Test]
        public void GroundSnapSolver_DoesNotSnapImmediatelyAfterSupportLaunch()
        {
            object settings = Activator.CreateInstance(FindRuntimeType(GroundSnapSettingsTypeName));
            object runtimeState = Activator.CreateInstance(FindRuntimeType(EntityMoveRuntimeStateTypeName));
            SetFieldValue(runtimeState, "WasGrounded", true);
            SetFieldValue(runtimeState, "LastSupportLaunchTime", 10.0f);

            object groundHit = CreateGroundHitInfo(distance: 0.08f, angle: 5.0f, isWalkable: true);

            object correction = InvokeStatic(
                GroundSnapSolverTypeName,
                "Resolve",
                settings,
                runtimeState,
                groundHit,
                true,
                -0.5f,
                10.05f,
                0.02f);

            Assert.IsFalse(GetPropertyValue<bool>(correction, "HasCorrection"));
        }

        [Test]
        public void GroundSnapSolver_ClampsByWeakDistanceAndSpeed()
        {
            object settings = Activator.CreateInstance(FindRuntimeType(GroundSnapSettingsTypeName));
            object runtimeState = Activator.CreateInstance(FindRuntimeType(EntityMoveRuntimeStateTypeName));
            SetFieldValue(runtimeState, "WasGrounded", true);

            object groundHit = CreateGroundHitInfo(distance: 0.20f, angle: 5.0f, isWalkable: true);

            object correction = InvokeStatic(
                GroundSnapSolverTypeName,
                "Resolve",
                settings,
                runtimeState,
                groundHit,
                true,
                -0.5f,
                10.0f,
                0.02f);

            Assert.IsFalse(GetPropertyValue<bool>(correction, "HasCorrection"));
        }

        [Test]
        public void GroundSnapSolver_ReturnsVerticalOnlyCorrection_NearCliffEdge()
        {
            GameObject ground = new GameObject("M5_CliffGround");
            createdObjects.Add(ground);
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(5.0f, 1.0f, 6.0f);
            ground.transform.position = new Vector3(0.0f, -0.5f, 0.0f);

            GameObject body = new GameObject("M5_ProbeBody");
            createdObjects.Add(body);
            body.transform.position = new Vector3(2.2f, 0.55f, 0.0f);

            CapsuleCollider bodyCollider = body.AddComponent<CapsuleCollider>();
            bodyCollider.height = 2.0f;
            bodyCollider.radius = 0.5f;
            bodyCollider.center = new Vector3(0.0f, 0.5f, 0.0f);

            object hit = InvokeStatic(
                GroundProbeSolverTypeName,
                "Probe",
                body.transform,
                bodyCollider,
                (LayerMask)(~0),
                0.18f,
                0.03f,
                55.0f,
                new RaycastHit[8]);

            Assert.IsTrue(GetPropertyValue<bool>(hit, "IsValid"));
            Assert.IsTrue(GetPropertyValue<bool>(hit, "IsWalkable"));

            object settings = Activator.CreateInstance(FindRuntimeType(GroundSnapSettingsTypeName));
            object runtimeState = Activator.CreateInstance(FindRuntimeType(EntityMoveRuntimeStateTypeName));
            SetFieldValue(runtimeState, "WasGrounded", true);

            object correction = InvokeStatic(
                GroundSnapSolverTypeName,
                "Resolve",
                settings,
                runtimeState,
                hit,
                true,
                -0.2f,
                10.0f,
                0.02f);

            bool hasCorrection = GetPropertyValue<bool>(correction, "HasCorrection");
            Vector3 delta = hasCorrection
                ? GetPropertyValue<Vector3>(correction, "Delta")
                : Vector3.zero;

            Assert.AreEqual(0.0f, delta.x, 0.0001f);
            Assert.AreEqual(0.0f, delta.z, 0.0001f);

            if (hasCorrection)
                Assert.Less(delta.y, 0.0f);
        }

        private static object CreateGroundHitInfo(float distance, float angle, bool isWalkable)
        {
            Type hitInfoType = FindRuntimeType(GroundHitInfoTypeName);
            Type kindType = FindRuntimeType(GroundSurfaceKindTypeName);
            object kind = Enum.Parse(kindType, isWalkable ? "Walkable" : "Wall");

            return Activator.CreateInstance(
                hitInfoType,
                true,
                null,
                null,
                Vector3.zero,
                Vector3.up,
                distance,
                angle,
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

        private static object InvokeStatic(string fullTypeName, string methodName, params object[] arguments)
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
    }
}

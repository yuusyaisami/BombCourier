using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class PlayerMoveM3GroundSnapStepSupportPlayModeTests
    {
        private const string GroundSnapSettingsTypeName = "BC.Base.GroundSnapSettings";
        private const string GroundSnapSolverTypeName = "BC.Base.GroundSnapSolver";
        private const string EntityMoveRuntimeStateTypeName = "BC.Base.EntityMoveRuntimeState";
        private const string GroundHitInfoTypeName = "BC.Base.GroundHitInfo";
        private const string GroundSurfaceKindTypeName = "BC.Base.GroundSurfaceKind";
        private const string SupportInertiaSettingsTypeName = "BC.Base.SupportInertiaSettings";
        private const string SupportInertiaSolverTypeName = "BC.Base.SupportInertiaSolver";

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
        public void GroundSnapSolver_ReturnsCorrection_WhenEligible()
        {
            object settings = Activator.CreateInstance(FindRuntimeType(GroundSnapSettingsTypeName));
            object runtimeState = Activator.CreateInstance(FindRuntimeType(EntityMoveRuntimeStateTypeName));
            object groundHit = CreateGroundHitInfo(distance: 0.08f, angle: 5.0f, isWalkable: true);

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

            Assert.IsTrue(GetPropertyValue<bool>(correction, "HasCorrection"));
            Vector3 delta = GetPropertyValue<Vector3>(correction, "Delta");
            Assert.Less(delta.y, -0.0001f);
        }

        [Test]
        public void GroundSnapSolver_DisabledAfterJump_DoesNotSnap()
        {
            object settings = Activator.CreateInstance(FindRuntimeType(GroundSnapSettingsTypeName));
            object runtimeState = Activator.CreateInstance(FindRuntimeType(EntityMoveRuntimeStateTypeName));
            SetFieldValue(runtimeState, "LastJumpTime", 9.95f);
            object groundHit = CreateGroundHitInfo(distance: 0.08f, angle: 5.0f, isWalkable: true);

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
        public void SupportInertiaSolver_EmitsLaunchAndDisablesSnapWindow()
        {
            object settings = Activator.CreateInstance(FindRuntimeType(SupportInertiaSettingsTypeName));
            SetFieldValue(settings, "HorizontalRetainRate", 0.5f);
            object runtimeState = Activator.CreateInstance(FindRuntimeType(EntityMoveRuntimeStateTypeName));

            object support = GetFieldValue<object>(runtimeState, "Support");
            SetFieldValue(support, "HadSupport", true);
            SetFieldValue(support, "PreviousPassengerVelocity", new Vector3(4.0f, 10.0f, 0.0f));
            SetFieldValue(support, "PassengerVelocity", new Vector3(0.0f, 0.5f, 0.0f));
            SetFieldValue(runtimeState, "Support", support);

            object[] args =
            {
                settings,
                runtimeState,
                false,
                20.0f,
                0.0f,
            };

            bool launched = (bool)InvokeStaticWithRefOut(SupportInertiaSolverTypeName, "TryResolveLaunch", args);

            Assert.IsTrue(launched);
            Assert.Greater(GetFieldValue<float>(runtimeState, "VerticalVelocity"), 0.1f);
            Assert.Greater(GetFieldValue<float>(runtimeState, "GroundSnapDisabledUntilTime"), 20.0f);
            Assert.Greater(GetFieldValue<float>(runtimeState, "SupportReattachDisabledUntilTime"), 20.0f);

            Vector3 inherited = GetFieldValue<Vector3>(runtimeState, "InheritedSupportVelocity");
            Assert.Greater(inherited.x, 1.9f);
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

        private static object InvokeStaticWithRefOut(string fullTypeName, string methodName, object[] arguments)
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

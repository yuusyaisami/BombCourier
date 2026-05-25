using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class CushionHighJumpPlayModeTests
    {
        private const string CushionSurfaceTypeName = "BC.Gimmick.Cushion.CushionSurfaceMB";
        private const string CushionImpactDataTypeName = "BC.Gimmick.Cushion.CushionImpactData";
        private const string EntityTagIdTypeName = "BC.Base.EntityTagId";
        private const string EntityMoveMotorTypeName = "BC.Base.EntityMoveMotorMB";
        private const string EntityMoveStateTypeName = "BC.Base.EntityMoveState";
        private const string ToastSystemManagerTypeName = "BC.Managers.ToastSystemManagerMB";
        private const string ToastRequestDataTypeName = "BC.Managers.ToastRequestData";
        private const string UIToastItemTypeName = "BC.UI.UIToastItemMB";

        private readonly List<UnityEngine.Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            DestroyToastManagerInstance();

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object createdObject = createdObjects[i];
                if (createdObject != null)
                    UnityEngine.Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        [Test]
        public void CushionSurface_ClampsBounceSpeedAndPreservesHighJumpMultiplier()
        {
            DestroyToastManagerInstance();

            GameObject surfaceObject = new GameObject("CushionSurface");
            createdObjects.Add(surfaceObject);

            Component surface = surfaceObject.AddComponent(FindRuntimeType(CushionSurfaceTypeName));
            SetPrivateField(surface, "acceptAnyTag", true);
            SetPrivateField(surface, "bounceRate", 1.0f);
            SetPrivateField(surface, "bounceSpeed", 8.0f);
            SetPrivateField(surface, "minBounceSpeed", 4.0f);
            SetPrivateField(surface, "maxBounceSpeed", 10.0f);
            SetPrivateField(surface, "highJumpSpeedMultiplier", 1.6f);

            GameObject sourceObject = new GameObject("Source");
            createdObjects.Add(sourceObject);

            object impactData = Activator.CreateInstance(
                FindRuntimeType(CushionImpactDataTypeName),
                sourceObject,
                sourceObject.transform,
                null,
                Activator.CreateInstance(FindRuntimeType(EntityTagIdTypeName)),
                null,
                null,
                Vector3.zero,
                Vector3.up,
                Vector3.down * 24f,
                24f);

            MethodInfo tryEvaluateMethod = surface.GetType().GetMethod("TryEvaluate", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(tryEvaluateMethod);

            object[] args = { impactData, null };
            bool handled = (bool)tryEvaluateMethod.Invoke(surface, args);
            object result = args[1];

            Assert.IsTrue(handled);
            Assert.AreEqual("Bounce", GetPropertyValue<object>(result, "ResponseKind").ToString());
            Assert.That(((Vector3)GetPropertyValue<object>(result, "BounceVelocity")).magnitude, Is.EqualTo(10.0f).Within(0.001f));
            Assert.That(Convert.ToSingle(GetPropertyValue<object>(result, "BounceSpeedLimit")), Is.EqualTo(10.0f).Within(0.001f));
            Assert.That(Convert.ToSingle(GetPropertyValue<object>(result, "HighJumpSpeedMultiplier")), Is.EqualTo(1.6f).Within(0.001f));
        }

        [Test]
        public void CushionSurface_StopWithoutAttach_ReturnsDampenWithConfiguredRetainedVelocityRate()
        {
            DestroyToastManagerInstance();

            GameObject surfaceObject = new GameObject("CushionSurface");
            createdObjects.Add(surfaceObject);

            Component surface = surfaceObject.AddComponent(FindRuntimeType(CushionSurfaceTypeName));
            SetPrivateField(surface, "acceptAnyTag", true);
            SetPrivateField(surface, "bounceRate", 0.0f);
            SetPrivateField(surface, "attachWhenStopped", false);
            SetPrivateField(surface, "defaultRetainedVelocityRate", 0.0f);

            GameObject sourceObject = new GameObject("Source");
            createdObjects.Add(sourceObject);

            object impactData = Activator.CreateInstance(
                FindRuntimeType(CushionImpactDataTypeName),
                sourceObject,
                sourceObject.transform,
                null,
                Activator.CreateInstance(FindRuntimeType(EntityTagIdTypeName)),
                null,
                null,
                Vector3.zero,
                Vector3.up,
                Vector3.down * 10f,
                10f);

            MethodInfo tryEvaluateMethod = surface.GetType().GetMethod("TryEvaluate", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(tryEvaluateMethod);

            object[] args = { impactData, null };
            bool handled = (bool)tryEvaluateMethod.Invoke(surface, args);
            object result = args[1];

            Assert.IsTrue(handled);
            Assert.AreEqual("Dampen", GetPropertyValue<object>(result, "ResponseKind").ToString());
            Assert.That(Convert.ToSingle(GetPropertyValue<object>(result, "RetainedLinearVelocityRate")), Is.EqualTo(0.0f).Within(0.001f));
        }

        [Test]
        public void EntityMoveMotor_HighJumpBuilder_UsesHeldJumpAndCapsSpeed()
        {
            DestroyToastManagerInstance();

            GameObject motorObject = new GameObject("PlayerMotor");
            createdObjects.Add(motorObject);

            motorObject.AddComponent<Rigidbody>();
            motorObject.AddComponent<CapsuleCollider>();
            Component moveMotor = motorObject.AddComponent(FindRuntimeType(EntityMoveMotorTypeName));

            SetPrivateField(moveMotor, "jumpHeld", true);

            MethodInfo method = moveMotor.GetType().GetMethod(
                "TryBuildHighJumpBounceVelocity",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(method);

            object[] args =
            {
                Vector3.up * 10f,
                9f,
                2f,
                null,
            };

            bool handled = (bool)method.Invoke(moveMotor, args);
            Vector3 resolvedVelocity = (Vector3)args[3];

            Assert.IsTrue(handled);
            Assert.That(resolvedVelocity.magnitude, Is.EqualTo(18f).Within(0.001f));
            Assert.AreEqual("Jumping", GetPropertyValue<object>(moveMotor, "MoveState").ToString());
        }

        [UnityTest]
        public IEnumerator ToastSystem_ShowToast_StacksItemsAndCleansThemUp()
        {
            DestroyToastManagerInstance();

            GameObject managerObject = new GameObject("ToastManager");
            createdObjects.Add(managerObject);

            Component manager = managerObject.AddComponent(FindRuntimeType(ToastSystemManagerTypeName));
            yield return null;

            object request = Activator.CreateInstance(FindRuntimeType(ToastRequestDataTypeName));
            SetFieldValue(request, "text", "toast");
            SetFieldValue(request, "visibleDuration", 0.02f);
            SetFieldValue(request, "fadeInDuration", 0f);
            SetFieldValue(request, "fadeOutDuration", 0f);

            MethodInfo showToastMethod = manager.GetType().GetMethod("ShowToast", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(showToastMethod);

            showToastMethod.Invoke(manager, new[] { request });
            showToastMethod.Invoke(manager, new[] { request });

            yield return null;

            Component[] activeItems = manager.gameObject.GetComponentsInChildren(FindRuntimeType(UIToastItemTypeName), true);
            Assert.AreEqual(2, activeItems.Length);

            yield return new WaitForSeconds(0.08f);

            activeItems = manager.gameObject.GetComponentsInChildren(FindRuntimeType(UIToastItemTypeName), true);
            Assert.AreEqual(0, activeItems.Length);
        }

        private static void DestroyToastManagerInstance()
        {
            Type toastManagerType = FindRuntimeType(ToastSystemManagerTypeName, false);
            if (toastManagerType == null)
                return;

            PropertyInfo instanceProperty = toastManagerType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            if (instanceProperty?.GetValue(null) is Component instance)
                UnityEngine.Object.DestroyImmediate(instance.gameObject);
        }

        private static Type FindRuntimeType(string fullTypeName, bool failIfMissing = true)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullTypeName);
                if (type != null)
                    return type;
            }

            if (failIfMissing)
                Assert.Fail($"Expected runtime type to exist: {fullTypeName}");

            return null;
        }

        private static T GetPropertyValue<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property on {target.GetType().Name}: {propertyName}");
            return (T)property.GetValue(target);
        }

        private static void SetFieldValue(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }
    }
}
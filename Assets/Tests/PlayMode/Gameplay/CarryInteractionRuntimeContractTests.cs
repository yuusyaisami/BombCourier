using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class CarryInteractionRuntimeContractTests
    {
        private const string CarryableObjectTypeName = "BC.Item.CarryableObjectMB";
        private const string PlayerInteractionControllerTypeName = "BC.Player.PlayerInteractionController";
        private const string CarryableItemInteractableAdapterTypeName = "BC.Player.CarryableItemInteractableAdapter";
        private const string UICarryThrowTypeName = "BC.UI.UICarryThrowMB";

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
        public void CarryableObjectRestoreAsUnhandledReenablesColliderAndPhysics()
        {
            GameObject itemObject = new GameObject("CarryableRestoreContract");
            createdObjects.Add(itemObject);

            SphereCollider collider = itemObject.AddComponent<SphereCollider>();
            Rigidbody body = itemObject.AddComponent<Rigidbody>();
            Component carryable = itemObject.AddComponent(FindRuntimeType(CarryableObjectTypeName));

            GameObject handlePointObject = new GameObject("CarryHandlePoint");
            createdObjects.Add(handlePointObject);

            object unhandledCheckpoint = InvokePrivateOrPublicMethod(carryable, "CaptureStageState");
            InvokePrivateOrPublicMethod(carryable, "OnHandle", handlePointObject.transform);

            Assert.IsTrue(GetPropertyValue<bool>(carryable, "IsHandled"));
            Assert.IsFalse(collider.enabled);
            Assert.IsTrue(body.isKinematic);

            InvokePrivateOrPublicMethod(carryable, "RestoreStageState", unhandledCheckpoint);

            Assert.IsFalse(GetPropertyValue<bool>(carryable, "IsHandled"));
            Assert.IsTrue(collider.enabled, "Unhandled restore must not leave the carry collider disabled.");
            Assert.IsFalse(body.isKinematic, "Unhandled restore must return the object to dynamic physics.");
            Assert.IsTrue(body.detectCollisions);
            Assert.IsTrue(body.useGravity);
        }

        [Test]
        public void PlayerInteractionControllerPrunesDestroyedCarryableAdapterCache()
        {
            GameObject interactionPointObject = new GameObject("InteractionPoint");
            GameObject facingObject = new GameObject("Facing");
            GameObject carryableObject = new GameObject("DestroyedCarryable");
            createdObjects.Add(interactionPointObject);
            createdObjects.Add(facingObject);
            createdObjects.Add(carryableObject);

            carryableObject.AddComponent<SphereCollider>();
            carryableObject.AddComponent<Rigidbody>();
            Component carryable = carryableObject.AddComponent(FindRuntimeType(CarryableObjectTypeName));

            Type controllerType = FindRuntimeType(PlayerInteractionControllerTypeName);
            object controller = Activator.CreateInstance(
                controllerType,
                null,
                interactionPointObject.transform,
                facingObject.transform,
                null,
                1f,
                180f,
                (LayerMask)(~0));
            Type adapterType = FindRuntimeType(CarryableItemInteractableAdapterTypeName);
            object adapter = Activator.CreateInstance(adapterType, carryable, carryable);

            IDictionary adapters = GetPrivateField<IDictionary>(controller, "carryableAdapters");
            adapters.Add(carryable, adapter);

            UnityEngine.Object.DestroyImmediate(carryableObject);
            createdObjects.Remove(carryableObject);

            InvokePrivateOrPublicMethod(controller, "PruneDestroyedCarryableAdapters");

            Assert.AreEqual(0, adapters.Count, "Destroyed carryable adapter keys must not remain cached.");
        }

        [Test]
        public void UICarryThrowKillsVisibilityTweenOnDestroy()
        {
            GameObject root = new GameObject("UICarryThrowContract");
            createdObjects.Add(root);

            Component ui = root.AddComponent(FindRuntimeType(UICarryThrowTypeName));
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

            GameObject sliderObject = new GameObject("ThrowPowerSlider");
            createdObjects.Add(sliderObject);
            Slider slider = sliderObject.AddComponent<Slider>();

            SetPrivateField(ui, "throwPowerSlider", slider);
            SetPrivateField(ui, "canvasGroup", canvasGroup);

            InvokePrivateOrPublicMethod(ui, "StartThrowCharge");
            object tween = GetPrivateField<object>(ui, "visibilityTween");

            Assert.IsNotNull(tween, "Starting throw charge should create an owned visibility tween.");
            Assert.IsTrue(GetPublicFieldOrProperty<bool>(tween, "active"));

            InvokePrivateOrPublicMethod(ui, "OnDestroy");

            Assert.IsNull(GetPrivateField<object>(ui, "visibilityTween"));
            Assert.IsFalse(GetPublicFieldOrProperty<bool>(tween, "active"), "Destroy must kill the owned tween instead of leaving DOTween work alive.");
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
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

        private static T GetPublicFieldOrProperty<T>(object target, string memberName)
        {
            FieldInfo field = target.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
                return (T)field.GetValue(target);

            PropertyInfo property = target.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(property, $"Expected public field or property on {target.GetType().Name}: {memberName}");
            return (T)property.GetValue(target);
        }

        private static object InvokePrivateOrPublicMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

            Assert.IsNotNull(method, $"Expected private method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, args);
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
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class PlayerItemHandleStatePlayModeTests
    {
        private const string PlayerItemHandleStateTypeName = "BC.Player.PlayerItemHandleStateMB";
        private const string CushionTypeName = "BC.Gimmick.Cushion.CushionMB";
        private const string BombTypeName = "BC.Bomb.BombMB";

        private readonly List<UnityEngine.Object> createdObjects = new();
        private readonly List<InputAction> createdActions = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdActions.Count - 1; i >= 0; i--)
            {
                createdActions[i]?.Dispose();
            }

            createdActions.Clear();

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object createdObject = createdObjects[i];
                if (createdObject != null)
                    UnityEngine.Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        [UnityTest]
        public System.Collections.IEnumerator QuickTap_ReleasesHeldItemAsSoftDrop_WithoutThrowAnimationTrigger()
        {
            LogAssert.Expect(LogType.Warning, "PlayerItemHandleStateMB: EntityMB is not found or not bound.");

            PlayerHandleFixture fixture = CreateFixture();
            Component carriedItem = CreateCarryItem("SoftDropCarryItem", new Vector3(0f, 0.5f, 0f));
            Rigidbody carriedBody = carriedItem.GetComponent<Rigidbody>();

            SetInteractionState(fixture.Controller, isInputPressed: false, pressSequence: 0);
            InvokeMethod(fixture.HandleState, "HandleItem", carriedItem);
            InvokeMethod(fixture.HandleState, "TickThrow");

            SetInteractionState(fixture.Controller, isInputPressed: true, pressSequence: 1);
            InvokeMethod(fixture.HandleState, "TickThrow");

            Assert.IsTrue(GetPrivateField<bool>(fixture.HandleState, "isThrowChargePending"), "Second press should enter the short pending window before throw charge starts.");
            Assert.IsFalse(GetPrivateField<bool>(fixture.HandleState, "isThrowCharging"), "Quick tap must not start the actual throw charge yet.");
            Assert.IsFalse(InvokeMethod<bool>(fixture.HandleState, "IsThrowPoseActive"), "Throw pose must stay inactive during the quick-tap pending window.");
            Assert.AreEqual(0, fixture.ThrowChargeStartCount, "Quick tap must not raise throw charge start.");

            SetInteractionState(fixture.Controller, isInputPressed: false, pressSequence: 1);
            InvokeMethod(fixture.HandleState, "TickThrow");

            yield return new WaitForFixedUpdate();

            Assert.IsFalse(GetPropertyValue<bool>(fixture.HandleState, "IsHandlingItem"), "Quick tap should release the held item.");
            Assert.AreEqual(0, GetPrivateField<int>(fixture.HandleState, "throwSequence"), "Soft drop must not increment the throw animation sequence.");
            Assert.AreEqual(0, fixture.ThrowChargeStartCount, "Soft drop must not start the throw charge UI/event flow.");
            Assert.AreEqual(0, fixture.ThrowChargeEndCount, "Soft drop must not end a throw charge that never started.");
            Assert.Greater(carriedBody.linearVelocity.z, 0.1f, "Soft drop should nudge the item forward in front of the player.");
            Assert.Less(carriedBody.linearVelocity.magnitude, 2.0f, "Soft drop velocity should stay much smaller than a real throw.");
        }

        [UnityTest]
        public System.Collections.IEnumerator HoldPastActivationWindow_StartsThrowCharge_AndReleaseThrowsItem()
        {
            LogAssert.Expect(LogType.Warning, "PlayerItemHandleStateMB: EntityMB is not found or not bound.");

            PlayerHandleFixture fixture = CreateFixture();
            Component carriedItem = CreateCarryItem("ThrowCarryItem", new Vector3(0f, 0.5f, 0f));
            Rigidbody carriedBody = carriedItem.GetComponent<Rigidbody>();

            SetInteractionState(fixture.Controller, isInputPressed: false, pressSequence: 0);
            InvokeMethod(fixture.HandleState, "HandleItem", carriedItem);
            InvokeMethod(fixture.HandleState, "TickThrow");

            SetInteractionState(fixture.Controller, isInputPressed: true, pressSequence: 1);
            InvokeMethod(fixture.HandleState, "TickThrow");

            float activationHoldTime = GetPrivateField<float>(fixture.HandleState, "throwChargeActivationHoldTime");
            SetPrivateField(fixture.HandleState, "throwChargePendingTimer", activationHoldTime);

            InvokeMethod(fixture.HandleState, "TickThrow");

            Assert.IsFalse(GetPrivateField<bool>(fixture.HandleState, "isThrowChargePending"), "Long hold should leave the pending state once throw charge starts.");
            Assert.IsTrue(GetPrivateField<bool>(fixture.HandleState, "isThrowCharging"), "Long hold should activate throw charge.");
            Assert.IsTrue(InvokeMethod<bool>(fixture.HandleState, "IsThrowPoseActive"), "Throw pose should only become active after the hold threshold is reached.");
            Assert.AreEqual(1, fixture.ThrowChargeStartCount, "Throw charge should start exactly once when the hold threshold is crossed.");

            SetInteractionState(fixture.Controller, isInputPressed: false, pressSequence: 1);
            InvokeMethod(fixture.HandleState, "TickThrow");

            yield return new WaitForFixedUpdate();

            Assert.IsFalse(GetPropertyValue<bool>(fixture.HandleState, "IsHandlingItem"), "Releasing after charge should throw the item.");
            Assert.AreEqual(1, GetPrivateField<int>(fixture.HandleState, "throwSequence"), "Actual throws must increment the throw sequence for animation triggering.");
            Assert.AreEqual(1, fixture.ThrowChargeEndCount, "Throw charge end should fire once on release.");
            Assert.Greater(carriedBody.linearVelocity.magnitude, 3.5f, "Charged throw should launch the item much faster than a drop.");
            Assert.Greater(carriedBody.linearVelocity.y, 0.1f, "Charged throw should keep the upward compensation arc.");
        }

        [UnityTest]
        public System.Collections.IEnumerator HeldBombExplosionClearsHandledStateImmediately()
        {
            LogAssert.Expect(LogType.Warning, "PlayerItemHandleStateMB: EntityMB is not found or not bound.");

            PlayerHandleFixture fixture = CreateFixture();
            Component bomb = CreateBomb("HeldBomb", new Vector3(0f, 0.5f, 0f));

            SetInteractionState(fixture.Controller, isInputPressed: false, pressSequence: 0);
            InvokeMethod(fixture.HandleState, "HandleItem", bomb);

            Assert.IsTrue(GetPropertyValue<bool>(fixture.HandleState, "IsHandlingItem"), "Bomb should be held before the explosion.");
            Assert.IsNotNull(GetPropertyValue<object>(fixture.HandleState, "CurrentHandledItem"), "Held item reference should be present before the explosion.");

            InvokeMethod(bomb, "Explode");

            Assert.IsFalse(GetPropertyValue<bool>(fixture.HandleState, "IsHandlingItem"), "Held bomb explosion must immediately clear player carry state.");
            Assert.IsNull(GetPropertyValue<object>(fixture.HandleState, "CurrentHandledItem"), "Destroyed bomb must not remain as the current held item.");

            yield return null;
        }

        private PlayerHandleFixture CreateFixture()
        {
            GameObject root = new GameObject("PlayerItemHandleStateTestRoot");
            root.SetActive(false);
            createdObjects.Add(root);

            GameObject handlePointObject = new GameObject("HandlePoint");
            handlePointObject.transform.SetParent(root.transform, false);
            handlePointObject.transform.localPosition = new Vector3(0f, 1f, 0.6f);

            GameObject playerModelObject = new GameObject("PlayerModel");
            playerModelObject.transform.SetParent(root.transform, false);
            playerModelObject.transform.localRotation = Quaternion.identity;

            Component handleState = root.AddComponent(FindRuntimeType(PlayerItemHandleStateTypeName));
            InputActionAsset inputActionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            createdObjects.Add(inputActionAsset);

            InputActionMap gameplayMap = new InputActionMap("Gameplay");
            inputActionAsset.AddActionMap(gameplayMap);

            InputAction handleAction = gameplayMap.AddAction("Handle", InputActionType.Button);
            createdActions.Add(handleAction);
            InputActionReference handleActionReference = InputActionReference.Create(handleAction);
            createdObjects.Add(handleActionReference);

            SetPrivateField(handleState, "handleItemPoint", handlePointObject.transform);
            SetPrivateField(handleState, "playerModel", playerModelObject);
            SetPrivateField(handleState, "handleItemAction", handleActionReference);

            root.SetActive(true);

            PlayerHandleFixture fixture = new PlayerHandleFixture
            {
                HandleState = handleState,
                Controller = GetPrivateField<object>(handleState, "interactionController"),
                HandlePoint = handlePointObject.transform,
                PlayerModel = playerModelObject,
            };

            Assert.IsNotNull(fixture.Controller, "Expected PlayerItemHandleStateMB to create its interaction controller during Awake.");

            SetProperty(handleState, "OnThrowChargeStart", (Action)(() => fixture.ThrowChargeStartCount++));
            SetProperty(handleState, "OnThrowChargeEnd", (Action)(() => fixture.ThrowChargeEndCount++));

            return fixture;
        }

        private Component CreateCarryItem(string name, Vector3 position)
        {
            GameObject itemObject = new GameObject(name);
            itemObject.transform.position = position;
            createdObjects.Add(itemObject);

            SphereCollider collider = itemObject.AddComponent<SphereCollider>();
            collider.radius = 0.5f;

            Rigidbody rigidbody = itemObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;

            return itemObject.AddComponent(FindRuntimeType(CushionTypeName));
        }

        private Component CreateBomb(string name, Vector3 position)
        {
            GameObject itemObject = new GameObject(name);
            itemObject.transform.position = position;
            createdObjects.Add(itemObject);

            SphereCollider collider = itemObject.AddComponent<SphereCollider>();
            collider.radius = 0.5f;

            Rigidbody rigidbody = itemObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;

            Component bomb = itemObject.AddComponent(FindRuntimeType(BombTypeName));
            SetPrivateField(bomb, "startFuseOnHandle", false);
            return bomb;
        }

        private static void SetInteractionState(object controller, bool isInputPressed, int pressSequence)
        {
            SetPrivateField(controller, "isInputPressed", isInputPressed);
            SetPrivateField(controller, "inputPressSequence", pressSequence);
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

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            return (T)field.GetValue(target);
        }

        private static void SetProperty<TValue>(object target, string propertyName, TValue value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property on {target.GetType().Name}: {propertyName}");
            property.SetValue(target, value);
        }

        private static T GetPropertyValue<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property on {target.GetType().Name}: {propertyName}");
            return (T)property.GetValue(target);
        }

        private static T InvokeMethod<T>(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            object result = method.Invoke(target, args);
            return result is T typedResult ? typedResult : default;
        }

        private static void InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            method.Invoke(target, args);
        }

        private sealed class PlayerHandleFixture
        {
            public Component HandleState { get; set; }
            public object Controller { get; set; }
            public Transform HandlePoint { get; set; }
            public GameObject PlayerModel { get; set; }
            public int ThrowChargeStartCount { get; set; }
            public int ThrowChargeEndCount { get; set; }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class UIBombStatePlayModeTests
    {
        private const string GameLogicManagerTypeName = "BC.Manager.GameLogicManagerMB";
        private const string UIBombStateTypeName = "BC.UI.UIBombStateMB";
        private const string PlayerItemHandleStateTypeName = "BC.Player.PlayerItemHandleStateMB";
        private const string BombTypeName = "BC.Bomb.BombMB";
        private const string CushionTypeName = "BC.Gimmick.Cushion.CushionMB";

        private readonly List<UnityEngine.Object> createdObjects = new();
        private readonly List<InputAction> createdActions = new();

        [TearDown]
        public void TearDown()
        {
            ClearGameLogicManagerInstance();

            for (int i = createdActions.Count - 1; i >= 0; i--)
                createdActions[i]?.Dispose();

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
        public IEnumerator BindItemHandleState_WithAlreadyHeldBomb_SyncsExistingBombStateImmediately()
        {
            LogAssert.Expect(LogType.Warning, "PlayerItemHandleStateMB: EntityMB is not found or not bound.");
            ClearGameLogicManagerInstance();

            UIBombFixture uiFixture = CreateUIBombFixture();
            PlayerHandleFixture playerFixture = CreatePlayerHandleFixture();
            Component bomb = CreateBombItem("HeldBomb", new Vector3(0f, 0.5f, 0f), fuseTime: 8f, remainingFuseTime: 4f, lastImpactForce: 5f, impactThreshold: 10f);

            InvokeMethod(playerFixture.HandleState, "HandleItem", bomb);
            Assert.AreSame(bomb, GetPropertyValue<object>(playerFixture.HandleState, "CurrentHandledItem"));

            InvokeMethod(uiFixture.BombState, "BindItemHandleState", playerFixture.HandleState);
            InvokeMethod(uiFixture.BombState, "Update");

            Assert.AreSame(bomb, GetPrivateField<object>(uiFixture.BombState, "bomb"));
            Assert.AreEqual(0.5f, uiFixture.TimerSlider.value, 0.0001f);
            Assert.AreEqual(0.5f, uiFixture.ImpactSlider.value, 0.0001f);

            yield return null;
        }

        [UnityTest]
        public IEnumerator HeldItemSwitchToNonBomb_ClearsBombSourceAndResetsTimerToDefault()
        {
            LogAssert.Expect(LogType.Warning, "PlayerItemHandleStateMB: EntityMB is not found or not bound.");
            ClearGameLogicManagerInstance();

            UIBombFixture uiFixture = CreateUIBombFixture();
            PlayerHandleFixture playerFixture = CreatePlayerHandleFixture();
            Component bomb = CreateBombItem("HeldBomb", new Vector3(0f, 0.5f, 0f), fuseTime: 6f, remainingFuseTime: 3f, lastImpactForce: 4f, impactThreshold: 8f);
            Component nonBomb = CreateCarryItem("HeldCushion", new Vector3(0f, 0.5f, 0f));

            InvokeMethod(uiFixture.BombState, "BindItemHandleState", playerFixture.HandleState);
            InvokeMethod(playerFixture.HandleState, "HandleItem", bomb);
            InvokeMethod(uiFixture.BombState, "Update");

            Assert.AreSame(bomb, GetPrivateField<object>(uiFixture.BombState, "bomb"));

            InvokeMethod(playerFixture.HandleState, "ClearHeldState");
            InvokeMethod(playerFixture.HandleState, "HandleItem", nonBomb);
            InvokeMethod(uiFixture.BombState, "Update");

            Assert.AreSame(nonBomb, GetPropertyValue<object>(playerFixture.HandleState, "CurrentHandledItem"));
            Assert.IsNull(GetPrivateField<object>(uiFixture.BombState, "bomb"));
            Assert.AreEqual(0f, uiFixture.TimerSlider.value, 0.0001f);

            yield return null;
        }

        private UIBombFixture CreateUIBombFixture()
        {
            GameObject root = new GameObject("UIBombStateRoot");
            root.SetActive(false);
            createdObjects.Add(root);

            GameObject timerSliderObject = new GameObject("TimerSlider");
            timerSliderObject.transform.SetParent(root.transform, false);
            Slider timerSlider = timerSliderObject.AddComponent<Slider>();

            GameObject impactSliderObject = new GameObject("ImpactSlider");
            impactSliderObject.transform.SetParent(root.transform, false);
            Slider impactSlider = impactSliderObject.AddComponent<Slider>();

            Component bombState = root.AddComponent(FindRuntimeType(UIBombStateTypeName));
            SetPrivateField(bombState, "bombTimerSlider", timerSlider);
            SetPrivateField(bombState, "bombImpactExplosionSlider", impactSlider);

            root.SetActive(true);

            return new UIBombFixture
            {
                BombState = bombState,
                TimerSlider = timerSlider,
                ImpactSlider = impactSlider,
            };
        }

        private PlayerHandleFixture CreatePlayerHandleFixture()
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

            return new PlayerHandleFixture
            {
                HandleState = handleState,
            };
        }

        private Component CreateBombItem(string name, Vector3 position, float fuseTime, float remainingFuseTime, float lastImpactForce, float impactThreshold)
        {
            GameObject itemObject = new GameObject(name);
            itemObject.SetActive(false);
            itemObject.transform.position = position;
            createdObjects.Add(itemObject);

            SphereCollider collider = itemObject.AddComponent<SphereCollider>();
            collider.radius = 0.5f;

            Rigidbody rigidbody = itemObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;

            Component bomb = itemObject.AddComponent(FindRuntimeType(BombTypeName));
            SetPrivateField(bomb, "startFuseOnHandle", false);
            SetPrivateField(bomb, "fuseTime", fuseTime);
            SetPrivateField(bomb, "explosionThreshold", impactThreshold);

            itemObject.SetActive(true);

            SetPrivateField(bomb, "fuseStarted", true);
            SetPrivateField(bomb, "remainingFuseTime", remainingFuseTime);
            SetPrivateField(bomb, "lastImpactThreshold", impactThreshold);
            SetFieldByName(bomb, "<LastImpactForce>k__BackingField", lastImpactForce);
            return bomb;
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

        private static void ClearGameLogicManagerInstance()
        {
            Type gameLogicType = FindRuntimeType(GameLogicManagerTypeName);
            FieldInfo field = gameLogicType.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, null);
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

        private static void SetFieldByName<TValue>(object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, $"Expected field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            return (T)field.GetValue(target);
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

        private sealed class UIBombFixture
        {
            public Component BombState { get; set; }
            public Slider TimerSlider { get; set; }
            public Slider ImpactSlider { get; set; }
        }

        private sealed class PlayerHandleFixture
        {
            public Component HandleState { get; set; }
        }
    }
}
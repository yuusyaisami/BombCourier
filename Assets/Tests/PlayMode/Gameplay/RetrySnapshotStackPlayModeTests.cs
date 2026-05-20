using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class RetrySnapshotStackPlayModeTests
    {
        private const string GameLogicManagerTypeName = "BC.Manager.GameLogicManagerMB";
        private const string GameStateManagerTypeName = "BC.Manager.GameStateManagerMB";
        private const string StageManagerTypeName = "BC.Manager.StageManagerMB";
        private const string StageCheckpointServiceTypeName = "BC.Stage.StageCheckpointServiceMB";
        private const string StageSaveMarkTypeName = "BC.Stage.StageSaveMarkMB";
        private const string PlayerTypeName = "BC.Base.PlayerMB";
        private const string BombTypeName = "BC.Bomb.BombMB";
        private const string GameStateTypeName = "BC.Manager.GameState";

        private readonly List<UnityEngine.Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            ClearSingletonInstance(GameLogicManagerTypeName);
            ClearSingletonInstance(GameStateManagerTypeName);
            ClearSingletonInstance(StageManagerTypeName);

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object createdObject = createdObjects[i];
                if (createdObject != null)
                    UnityEngine.Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator EmptyRetryHistory_DoesNotDispatchReloadOrReset()
        {
            RetryFixture fixture = CreateFixture();
            SetGameState(fixture.GameStateManager, "SetupPlaying");

            InvokeMethod(fixture.GameLogicManager, "RequestRetryAction");

            Assert.AreEqual("None", GetPropertyValue<object>(fixture.GameLogicManager, "CurrentRetryActionMode").ToString());
            Assert.AreEqual("SetupPlaying", GetPropertyValue<object>(fixture.GameStateManager, "CurrentState").ToString());

            yield return null;
        }

        [UnityTest]
        public IEnumerator Reload_PopsLatestSnapshotFirst_AndArmsResetAfterLastEntry()
        {
            RetryFixture fixture = CreateFixture();
            SetGameState(fixture.GameStateManager, "SetupPlaying");

            fixture.Player.transform.position = new Vector3(1f, 0f, 0f);
            InvokeMethod(fixture.GameLogicManager, "CaptureRetryCheckpointBeforeBombPickup", fixture.BombA);

            fixture.Player.transform.position = new Vector3(2f, 0f, 0f);
            InvokeMethod(fixture.GameLogicManager, "CaptureRetryCheckpointBeforeBombPickup", fixture.BombB);

            fixture.Player.transform.position = new Vector3(99f, 0f, 0f);
            InvokeMethod(fixture.GameLogicManager, "OnStageChanged", ParseGameState("Reload"));
            yield return null;

            Assert.AreEqual(new Vector3(2f, 0f, 0f), fixture.Player.transform.position);
            Assert.AreSame(fixture.BombB, GetPropertyValue<object>(fixture.GameLogicManager, "CurrentBomb"));
            Assert.AreEqual("ReloadCheckpoint", GetPropertyValue<object>(fixture.GameLogicManager, "CurrentRetryActionMode").ToString());

            fixture.Player.transform.position = new Vector3(77f, 0f, 0f);
            InvokeMethod(fixture.GameLogicManager, "OnStageChanged", ParseGameState("Reload"));
            yield return null;

            Assert.AreEqual(new Vector3(1f, 0f, 0f), fixture.Player.transform.position);
            Assert.AreSame(fixture.BombA, GetPropertyValue<object>(fixture.GameLogicManager, "CurrentBomb"));
            Assert.AreEqual("ResetStage", GetPropertyValue<object>(fixture.GameLogicManager, "CurrentRetryActionMode").ToString());

            InvokeMethod(fixture.GameLogicManager, "RequestRetryAction");
            Assert.AreEqual("ResetStage", GetPropertyValue<object>(fixture.GameStateManager, "CurrentState").ToString());
        }

        [UnityTest]
        public IEnumerator NewSnapshotAfterResetArmed_ReturnsRetryModeToReload()
        {
            RetryFixture fixture = CreateFixture();
            SetGameState(fixture.GameStateManager, "SetupPlaying");

            fixture.Player.transform.position = new Vector3(1f, 0f, 0f);
            InvokeMethod(fixture.GameLogicManager, "CaptureRetryCheckpointBeforeBombPickup", fixture.BombA);

            InvokeMethod(fixture.GameLogicManager, "OnStageChanged", ParseGameState("Reload"));
            yield return null;

            Assert.AreEqual("ResetStage", GetPropertyValue<object>(fixture.GameLogicManager, "CurrentRetryActionMode").ToString());

            fixture.Player.transform.position = new Vector3(3f, 0f, 0f);
            InvokeMethod(fixture.GameLogicManager, "CaptureRetryCheckpointBeforeBombPickup", fixture.BombB);

            Assert.AreEqual("ReloadCheckpoint", GetPropertyValue<object>(fixture.GameLogicManager, "CurrentRetryActionMode").ToString());

            InvokeMethod(fixture.GameLogicManager, "RequestRetryAction");
            Assert.AreEqual("Reload", GetPropertyValue<object>(fixture.GameStateManager, "CurrentState").ToString());
        }

        private RetryFixture CreateFixture()
        {
            GameObject stageManagerObject = new GameObject("StageManagerRoot");
            stageManagerObject.SetActive(false);
            createdObjects.Add(stageManagerObject);

            GameObject stageRootObject = new GameObject("StageRoot");
            stageRootObject.transform.SetParent(stageManagerObject.transform, false);
            createdObjects.Add(stageRootObject);

            Component checkpointService = stageManagerObject.AddComponent(FindRuntimeType(StageCheckpointServiceTypeName));
            Component stageManager = stageManagerObject.AddComponent(FindRuntimeType(StageManagerTypeName));
            SetPrivateField(checkpointService, "stageRoot", stageRootObject.transform);
            SetPrivateField(stageManager, "checkpointService", checkpointService);
            SetPrivateField(stageManager, "stageRoot", stageRootObject.transform);
            ForceSingletonInstance(stageManager.GetType(), stageManager);

            GameObject gameStateManagerObject = new GameObject("GameStateManagerRoot");
            gameStateManagerObject.SetActive(false);
            createdObjects.Add(gameStateManagerObject);

            Component gameStateManager = gameStateManagerObject.AddComponent(FindRuntimeType(GameStateManagerTypeName));
            ForceSingletonInstance(gameStateManager.GetType(), gameStateManager);

            GameObject gameLogicObject = new GameObject("GameLogicRoot");
            gameLogicObject.SetActive(false);
            createdObjects.Add(gameLogicObject);

            Component gameLogicManager = gameLogicObject.AddComponent(FindRuntimeType(GameLogicManagerTypeName));
            ForceSingletonInstance(gameLogicManager.GetType(), gameLogicManager);

            GameObject playerObject = new GameObject("Player");
            playerObject.transform.SetParent(gameLogicObject.transform, false);
            createdObjects.Add(playerObject);

            Component player = playerObject.AddComponent(FindRuntimeType(PlayerTypeName));

            Component bombA = CreateBomb("BombA", stageRootObject.transform, new Vector3(-1f, 0.5f, 0f));
            Component bombB = CreateBomb("BombB", stageRootObject.transform, new Vector3(1f, 0.5f, 0f));

            return new RetryFixture
            {
                GameLogicManager = gameLogicManager,
                GameStateManager = gameStateManager,
                StageManager = stageManager,
                Player = (Component)player,
                BombA = bombA,
                BombB = bombB,
            };
        }

        private Component CreateBomb(string name, Transform parent, Vector3 localPosition)
        {
            GameObject bombObject = new GameObject(name);
            bombObject.transform.SetParent(parent, false);
            bombObject.transform.localPosition = localPosition;
            createdObjects.Add(bombObject);

            SphereCollider collider = bombObject.AddComponent<SphereCollider>();
            collider.radius = 0.5f;

            Rigidbody rigidbody = bombObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;

            Component bomb = bombObject.AddComponent(FindRuntimeType(BombTypeName));
            bombObject.AddComponent(FindRuntimeType(StageSaveMarkTypeName));
            SetPrivateField(bomb, "startFuseOnHandle", false);
            return bomb;
        }

        private static object ParseGameState(string stateName)
        {
            return Enum.Parse(FindRuntimeType(GameStateTypeName), stateName);
        }

        private static void SetGameState(object gameStateManager, string stateName)
        {
            InvokeMethod(gameStateManager, "ChangeState", ParseGameState(stateName));
        }

        private static void ClearSingletonInstance(string fullTypeName)
        {
            Type type = FindRuntimeType(fullTypeName);
            FieldInfo field = type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected singleton backing field on {fullTypeName}.");
            field.SetValue(null, null);
        }

        private static void ForceSingletonInstance(Type type, object instance)
        {
            FieldInfo field = type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected singleton backing field on {type.FullName}.");
            field.SetValue(null, instance);
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

        private static T GetPropertyValue<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property on {target.GetType().Name}: {propertyName}");
            return (T)property.GetValue(target);
        }

        private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static void InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            method.Invoke(target, args);
        }

        private sealed class RetryFixture
        {
            public Component GameLogicManager { get; set; }
            public Component GameStateManager { get; set; }
            public Component StageManager { get; set; }
            public Component Player { get; set; }
            public Component BombA { get; set; }
            public Component BombB { get; set; }
        }
    }
}
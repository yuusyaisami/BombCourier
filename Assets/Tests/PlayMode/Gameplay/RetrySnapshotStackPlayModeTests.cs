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
        private const string MapRuntimeTypeName = "BC.Stage.MapRuntimeMB";
        private const string StageCheckpointServiceTypeName = "BC.Stage.StageCheckpointServiceMB";
        private const string StageSaveMarkTypeName = "BC.Stage.StageSaveMarkMB";
        private const string PlayerTypeName = "BC.Base.PlayerMB";
        private const string EntityTypeName = "BC.Base.EntityMB";
        private const string EntityMoveMotorTypeName = "BC.Base.EntityMoveMotorMB";
        private const string PlayerMoveControllerTypeName = "BC.Base.PlayerMoveController";
        private const string PlayerItemHandleStateTypeName = "BC.Player.PlayerItemHandleStateMB";
        private const string BombTypeName = "BC.Bomb.BombMB";
        private const string CarryableObjectTypeName = "BC.Item.CarryableObjectMB";
        private const string GameStateTypeName = "BC.Manager.GameState";
        private const string SceneKernelTypeName = "BC.Base.SceneKernel";
        private const string ValueStoreServiceTypeName = "BC.Base.ValueStoreService";
        private const string EntityRefTypeName = "BC.Base.EntityRef";
        private const string ManualSnapshotBlockReasonTypeName = "BC.Manager.ManualSnapshotBlockReason";

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

        [UnityTest]
        public IEnumerator ManualSnapshot_StacksBelowBombPickupRetry_AndIsConsumedByReload()
        {
            RetryFixture fixture = CreateFixture();
            SetGameState(fixture.GameStateManager, "SetupPlaying");

            fixture.Player.transform.position = new Vector3(10f, 0f, 0f);
            bool manualCaptureSucceeded = (bool)InvokeMethod(fixture.GameLogicManager, "TryCaptureManualSnapshot");
            Assert.IsTrue(manualCaptureSucceeded, "Manual snapshot should capture in stable SetupPlaying state.");
            Assert.IsTrue(GetPropertyValue<bool>(fixture.GameLogicManager, "HasManualSnapshotBase"));

            fixture.Player.transform.position = new Vector3(20f, 0f, 0f);
            InvokeMethod(fixture.GameLogicManager, "CaptureRetryCheckpointBeforeBombPickup", fixture.BombB);

            fixture.Player.transform.position = new Vector3(99f, 0f, 0f);
            InvokeMethod(fixture.GameLogicManager, "OnStageChanged", ParseGameState("Reload"));
            yield return null;

            Assert.AreEqual(new Vector3(20f, 0f, 0f), fixture.Player.transform.position, "Latest bomb-pickup retry should be restored first.");
            Assert.AreSame(fixture.BombB, GetPropertyValue<object>(fixture.GameLogicManager, "CurrentBomb"));
            Assert.IsTrue(GetPropertyValue<bool>(fixture.GameLogicManager, "HasManualSnapshotBase"), "Manual base snapshot should remain after the upper retry entry is consumed.");

            fixture.Player.transform.position = new Vector3(77f, 0f, 0f);
            InvokeMethod(fixture.GameLogicManager, "OnStageChanged", ParseGameState("Reload"));
            yield return null;

            Assert.AreEqual(new Vector3(10f, 0f, 0f), fixture.Player.transform.position, "Second reload should return to the manual snapshot base.");
            Assert.AreEqual("ResetStage", GetPropertyValue<object>(fixture.GameLogicManager, "CurrentRetryActionMode").ToString());
            Assert.IsFalse(GetPropertyValue<bool>(fixture.GameLogicManager, "HasManualSnapshotBase"), "Manual base snapshot should also be pop-consumed after reload.");
        }

        [UnityTest]
        public IEnumerator ManualSnapshot_RecaptureClearsExistingRetryHistory()
        {
            RetryFixture fixture = CreateFixture();
            SetGameState(fixture.GameStateManager, "SetupPlaying");

            fixture.Player.transform.position = new Vector3(1f, 0f, 0f);
            Assert.IsTrue((bool)InvokeMethod(fixture.GameLogicManager, "TryCaptureManualSnapshot"));

            fixture.Player.transform.position = new Vector3(2f, 0f, 0f);
            InvokeMethod(fixture.GameLogicManager, "CaptureRetryCheckpointBeforeBombPickup", fixture.BombB);

            fixture.Player.transform.position = new Vector3(3f, 0f, 0f);
            Assert.IsTrue((bool)InvokeMethod(fixture.GameLogicManager, "TryCaptureManualSnapshot"), "Recapturing the manual snapshot should replace the existing retry stack.");

            fixture.Player.transform.position = new Vector3(88f, 0f, 0f);
            InvokeMethod(fixture.GameLogicManager, "OnStageChanged", ParseGameState("Reload"));
            yield return null;

            Assert.AreEqual(new Vector3(3f, 0f, 0f), fixture.Player.transform.position, "Recaptured manual snapshot should become the only remaining retry base.");
            Assert.AreEqual("ResetStage", GetPropertyValue<object>(fixture.GameLogicManager, "CurrentRetryActionMode").ToString());
            Assert.IsFalse(GetPropertyValue<bool>(fixture.GameLogicManager, "HasManualSnapshotBase"));
        }

        [Test]
        public void ManualSnapshotAvailability_BlocksHoldingItemAndBombHistory()
        {
            RetryFixture fixture = CreateFixture();
            SetGameState(fixture.GameStateManager, "SetupPlaying");

            object availability = InvokeMethod(fixture.GameLogicManager, "EvaluateManualSnapshotAvailability");
            Assert.IsTrue(GetPropertyValue<bool>(availability, "CanCapture"));
            Assert.IsTrue(GetPropertyValue<bool>(availability, "ShouldShowUi"));

            Component carryable = CreateCarryable("HeldBox", fixture.StageRoot.transform, new Vector3(0f, 0.25f, 0f));
            SetPrivateField(fixture.PlayerItemHandleState, "currentlyHandledItem", carryable);

            availability = InvokeMethod(fixture.GameLogicManager, "EvaluateManualSnapshotAvailability");
            Assert.IsFalse(GetPropertyValue<bool>(availability, "CanCapture"));
            Assert.IsTrue(HasFlag(GetPropertyValue<object>(availability, "BlockReasons"), "HoldingItem"));
            Assert.IsTrue(GetPropertyValue<bool>(availability, "ShouldShowUnavailableOverlay"));

            SetPrivateField<object>(fixture.PlayerItemHandleState, "currentlyHandledItem", null);
            SetPrivateField(fixture.GameLogicManager, "hasStartedAnyBombFuseThisStage", true);

            availability = InvokeMethod(fixture.GameLogicManager, "EvaluateManualSnapshotAvailability");
            Assert.IsFalse(GetPropertyValue<bool>(availability, "CanCapture"));
            Assert.IsTrue(HasFlag(GetPropertyValue<object>(availability, "BlockReasons"), "BombStateDirty"));
            Assert.IsTrue(GetPropertyValue<bool>(availability, "ShouldShowUnavailableOverlay"));
        }

        [Test]
        public void AllSceneBombsExploded_EnablesResetRetryAction()
        {
            RetryFixture fixture = CreateFixture();
            SetGameState(fixture.GameStateManager, "Exploded");

            SetPrivateField(fixture.BombA, "exploded", true);
            SetPrivateField(fixture.BombB, "exploded", true);

            InvokeMethod(fixture.GameLogicManager, "RequestRetryAction");

            Assert.AreEqual("ResetStage", GetPropertyValue<object>(fixture.GameLogicManager, "CurrentRetryActionMode").ToString());
            Assert.AreEqual("ResetStage", GetPropertyValue<object>(fixture.GameStateManager, "CurrentState").ToString());
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
            Component gameLogicEntity = gameLogicObject.AddComponent(FindRuntimeType(EntityTypeName));
            object gameLogicEntityRef = CreateEntityRef(100u);
            InvokeMethod(gameLogicEntity, "Bind", gameLogicEntityRef);
            SetPrivateField(gameLogicManager, "sceneKernel", CreateSceneKernel());
            SetPrivateField(gameLogicManager, "gameLogicManagerRef", gameLogicEntityRef);

            GameObject playerObject = new GameObject("Player");
            playerObject.transform.SetParent(gameLogicObject.transform, false);
            createdObjects.Add(playerObject);

            playerObject.AddComponent<Rigidbody>();
            playerObject.AddComponent<CapsuleCollider>();
            Component playerEntity = playerObject.AddComponent(FindRuntimeType(EntityTypeName));
            object playerEntityRef = CreateEntityRef(200u);
            InvokeMethod(playerEntity, "Bind", playerEntityRef);
            Component moveMotor = playerObject.AddComponent(FindRuntimeType(EntityMoveMotorTypeName));
            SetPrivateField(moveMotor, "currentCanMoveByInput", true);
            Component playerMoveController = playerObject.AddComponent(FindRuntimeType(PlayerMoveControllerTypeName));
            SetPrivateField(playerMoveController, "moveMotor", moveMotor);
            Component playerItemHandleState = CreatePlayerItemHandleState(playerObject);
            Component player = playerObject.AddComponent(FindRuntimeType(PlayerTypeName));
            SetPrivateField(player, "moveController", moveMotor);
            SetPrivateField(player, "playerMoveController", playerMoveController);

            SetPrivateField(gameLogicManager, "playerInstance", player);
            SetPrivateField(gameLogicManager, "playerRef", playerEntityRef);

            Component bombA = CreateBomb("BombA", stageRootObject.transform, new Vector3(-1f, 0.5f, 0f));
            Component bombB = CreateBomb("BombB", stageRootObject.transform, new Vector3(1f, 0.5f, 0f));
            Component mapRuntime = stageRootObject.AddComponent(FindRuntimeType(MapRuntimeTypeName));

            SetPrivateField(mapRuntime, "bombs", CreateTypedList(FindRuntimeType(BombTypeName), bombA, bombB));
            SetPrivateField(gameLogicManager, "currentMapRuntime", mapRuntime);
            SetPrivateField(gameLogicManager, "currentBomb", bombA);

            return new RetryFixture
            {
                GameLogicManager = gameLogicManager,
                GameStateManager = gameStateManager,
                StageManager = stageManager,
                StageRoot = stageRootObject,
                Player = (Component)player,
                PlayerItemHandleState = playerItemHandleState,
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

        private Component CreateCarryable(string name, Transform parent, Vector3 localPosition)
        {
            GameObject carryableObject = new GameObject(name);
            carryableObject.transform.SetParent(parent, false);
            carryableObject.transform.localPosition = localPosition;
            createdObjects.Add(carryableObject);

            carryableObject.AddComponent<BoxCollider>();
            carryableObject.AddComponent<Rigidbody>();
            return carryableObject.AddComponent(FindRuntimeType(CarryableObjectTypeName));
        }

        private Component CreatePlayerItemHandleState(GameObject playerObject)
        {
            GameObject handlePointObject = new GameObject("HandlePoint");
            handlePointObject.transform.SetParent(playerObject.transform, false);
            createdObjects.Add(handlePointObject);

            GameObject playerModelObject = new GameObject("PlayerModel");
            playerModelObject.transform.SetParent(playerObject.transform, false);
            createdObjects.Add(playerModelObject);

            Component handleState = playerObject.AddComponent(FindRuntimeType(PlayerItemHandleStateTypeName));
            SetPrivateField(handleState, "handleItemPoint", handlePointObject.transform);
            SetPrivateField(handleState, "playerModel", playerModelObject);
            SetPrivateField(handleState, "currentCanInteract", true);
            return handleState;
        }

        private static object CreateEntityRef(uint entityId)
        {
            return Activator.CreateInstance(FindRuntimeType(EntityRefTypeName), entityId, 1);
        }

        private static object CreateSceneKernel()
        {
            object sceneKernel = Activator.CreateInstance(FindRuntimeType(SceneKernelTypeName));
            object valueStore = Activator.CreateInstance(FindRuntimeType(ValueStoreServiceTypeName));
            PropertyInfo property = sceneKernel.GetType().GetProperty("ValueStore", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(property, "Expected SceneKernel.ValueStore property.");
            property.SetValue(sceneKernel, valueStore);
            return sceneKernel;
        }

        private static bool HasFlag(object enumValue, string flagName)
        {
            object parsedValue = Enum.Parse(FindRuntimeType(ManualSnapshotBlockReasonTypeName), flagName, ignoreCase: false);
            return ((Enum)enumValue).HasFlag((Enum)parsedValue);
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

        private static object CreateTypedList(Type elementType, params object[] items)
        {
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            for (int i = 0; i < items.Length; i++)
            {
                list.Add(items[i]);
            }

            return list;
        }

        private static object InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, args);
        }

        private sealed class RetryFixture
        {
            public Component GameLogicManager { get; set; }
            public Component GameStateManager { get; set; }
            public Component StageManager { get; set; }
            public GameObject StageRoot { get; set; }
            public Component Player { get; set; }
            public Component PlayerItemHandleState { get; set; }
            public Component BombA { get; set; }
            public Component BombB { get; set; }
        }
    }
}

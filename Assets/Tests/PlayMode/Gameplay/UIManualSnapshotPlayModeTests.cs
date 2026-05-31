using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class UIManualSnapshotPlayModeTests
    {
        private const string UIManualSnapshotTypeName = "BC.UI.UIManualSnapshotMB";
        private const string UIManagerTypeName = "BC.Managers.UIManagerMB";
        private const string GameLogicManagerTypeName = "BC.Manager.GameLogicManagerMB";
        private const string GameStateManagerTypeName = "BC.Manager.GameStateManagerMB";
        private const string StageManagerTypeName = "BC.Manager.StageManagerMB";
        private const string StageCheckpointServiceTypeName = "BC.Stage.StageCheckpointServiceMB";
        private const string StageSaveMarkTypeName = "BC.Stage.StageSaveMarkMB";
        private const string MapRuntimeTypeName = "BC.Stage.MapRuntimeMB";
        private const string BombTypeName = "BC.Bomb.BombMB";
        private const string PlayerTypeName = "BC.Base.PlayerMB";
        private const string PlayerMoveControllerTypeName = "BC.Base.PlayerMoveController";
        private const string PlayerItemHandleStateTypeName = "BC.Player.PlayerItemHandleStateMB";
        private const string EntityMoveMotorTypeName = "BC.Base.EntityMoveMotorMB";
        private const string EntityTypeName = "BC.Base.EntityMB";
        private const string SceneKernelTypeName = "BC.Base.SceneKernel";
        private const string ValueStoreServiceTypeName = "BC.Base.ValueStoreService";
        private const string EntityRefTypeName = "BC.Base.EntityRef";
        private const string CarryableObjectTypeName = "BC.Item.CarryableObjectMB";
        private const string GameStateTypeName = "BC.Manager.GameState";

        private readonly List<UnityEngine.Object> createdObjects = new();
        private readonly List<InputAction> createdActions = new();
        private readonly List<InputDevice> createdDevices = new();

        [TearDown]
        public void TearDown()
        {
            ClearSingletonInstance(GameLogicManagerTypeName);
            ClearSingletonInstance(GameStateManagerTypeName);
            ClearSingletonInstance(StageManagerTypeName);
            ClearSingletonInstance(UIManagerTypeName);

            for (int i = createdActions.Count - 1; i >= 0; i--)
                createdActions[i]?.Dispose();

            createdActions.Clear();

            for (int i = createdDevices.Count - 1; i >= 0; i--)
            {
                if (createdDevices[i] != null)
                    InputSystem.RemoveDevice(createdDevices[i]);
            }

            createdDevices.Clear();

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object createdObject = createdObjects[i];
                if (createdObject != null)
                    UnityEngine.Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator Update_AllowedState_ShowsUiWithoutUnavailableOverlay()
        {
            ManualSnapshotFixture fixture = CreateFixture();
            SetGameState(fixture.GameStateManager, "SetupPlaying");

            InvokeMethod(fixture.Ui, "Update");
            yield return null;

            Assert.AreEqual(1.0f, fixture.CanvasGroup.alpha, 0.0001f);
            Assert.IsFalse(fixture.UnavailableOverlay.enabled, "Allowed state should not show the unavailable overlay.");
        }

        [UnityTest]
        public IEnumerator Update_BlockedGameplayState_ShowsUnavailableOverlay()
        {
            ManualSnapshotFixture fixture = CreateFixture();
            SetGameState(fixture.GameStateManager, "SetupPlaying");

            Component carryable = CreateCarryable("HeldBox", fixture.StageRoot.transform, new Vector3(0f, 0.3f, 0f));
            SetPrivateField(fixture.PlayerItemHandleState, "currentlyHandledItem", carryable);

            InvokeMethod(fixture.Ui, "Update");
            yield return null;

            Assert.AreEqual(1.0f, fixture.CanvasGroup.alpha, 0.0001f);
            Assert.IsTrue(fixture.UnavailableOverlay.enabled, "Blocked but gameplay-visible state should show the unavailable overlay.");
        }

        [UnityTest]
        public IEnumerator Update_IntroState_HidesUi()
        {
            ManualSnapshotFixture fixture = CreateFixture();
            SetGameState(fixture.GameStateManager, "Intro");

            InvokeMethod(fixture.Ui, "Update");
            yield return null;

            Assert.AreEqual(0.0f, fixture.CanvasGroup.alpha, 0.0001f);
            Assert.IsFalse(fixture.UnavailableOverlay.enabled);
        }

        [UnityTest]
        public IEnumerator HoldComplete_CapturesManualSnapshotOnceUntilRelease()
        {
            ManualSnapshotFixture fixture = CreateFixture();
            SetGameState(fixture.GameStateManager, "SetupPlaying");
            SetPrivateField(fixture.Ui, "requiredHoldSeconds", 0.01f);

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                keyboard = InputSystem.AddDevice<Keyboard>();
                createdDevices.Add(keyboard);
            }

            QueueKeyboardState(keyboard, Key.V);
            yield return null;
            InvokeMethod(fixture.Ui, "Update");
            yield return null;
            InvokeMethod(fixture.Ui, "Update");

            Assert.IsTrue(GetPropertyValue<bool>(fixture.GameLogicManager, "HasManualSnapshotBase"), "Completing the hold should capture a manual snapshot.");

            Vector3 originalPosition = fixture.Player.transform.position;
            fixture.Player.transform.position = new Vector3(42f, 0f, 0f);

            yield return null;
            InvokeMethod(fixture.Ui, "Update");
            Assert.IsTrue(GetPropertyValue<bool>(fixture.GameLogicManager, "HasManualSnapshotBase"), "Continuing to hold should not create another capture cycle before release.");

            InvokeMethod(fixture.GameLogicManager, "OnStageChanged", ParseGameState("Reload"));
            yield return null;
            Assert.AreEqual(originalPosition, fixture.Player.transform.position, "Reload should still return to the first captured manual snapshot.");

            QueueKeyboardState(keyboard);
            yield return null;
            InvokeMethod(fixture.Ui, "Update");
        }

        private ManualSnapshotFixture CreateFixture()
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

            GameObject uiManagerObject = new GameObject("UIManagerRoot");
            uiManagerObject.SetActive(false);
            createdObjects.Add(uiManagerObject);
            Component uiManager = uiManagerObject.AddComponent(FindRuntimeType(UIManagerTypeName));
            ForceSingletonInstance(uiManager.GetType(), uiManager);

            GameObject gameLogicObject = new GameObject("GameLogicRoot");
            gameLogicObject.SetActive(false);
            createdObjects.Add(gameLogicObject);

            Component gameLogicManager = gameLogicObject.AddComponent(FindRuntimeType(GameLogicManagerTypeName));
            ForceSingletonInstance(gameLogicManager.GetType(), gameLogicManager);
            Component gameLogicEntity = gameLogicObject.AddComponent(FindRuntimeType(EntityTypeName));
            object gameLogicEntityRef = CreateEntityRef(300u);
            InvokeMethod(gameLogicEntity, "Bind", gameLogicEntityRef);
            SetPrivateField(gameLogicManager, "sceneKernel", CreateSceneKernel());
            SetPrivateField(gameLogicManager, "gameLogicManagerRef", gameLogicEntityRef);

            GameObject playerObject = new GameObject("Player");
            playerObject.transform.SetParent(gameLogicObject.transform, false);
            createdObjects.Add(playerObject);

            playerObject.AddComponent<Rigidbody>();
            playerObject.AddComponent<CapsuleCollider>();
            Component playerEntity = playerObject.AddComponent(FindRuntimeType(EntityTypeName));
            object playerEntityRef = CreateEntityRef(301u);
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

            GameObject uiRoot = new GameObject("ManualSnapshotUIRoot");
            uiRoot.SetActive(false);
            createdObjects.Add(uiRoot);
            CanvasGroup canvasGroup = uiRoot.AddComponent<CanvasGroup>();
            Image actionIcon = new GameObject("ActionIcon").AddComponent<Image>();
            createdObjects.Add(actionIcon.gameObject);
            actionIcon.transform.SetParent(uiRoot.transform, false);
            Image progressImage = new GameObject("Progress").AddComponent<Image>();
            createdObjects.Add(progressImage.gameObject);
            progressImage.transform.SetParent(uiRoot.transform, false);
            Image unavailableOverlay = new GameObject("Unavailable").AddComponent<Image>();
            createdObjects.Add(unavailableOverlay.gameObject);
            unavailableOverlay.transform.SetParent(uiRoot.transform, false);

            InputAction captureAction = new InputAction("ManualSnapshot", InputActionType.Button, "<Keyboard>/v");
            captureAction.Enable();
            createdActions.Add(captureAction);
            InputActionReference actionReference = InputActionReference.Create(captureAction);
            createdObjects.Add(actionReference);

            Component manualSnapshotUi = uiRoot.AddComponent(FindRuntimeType(UIManualSnapshotTypeName));
            SetPrivateField(manualSnapshotUi, "canvasGroup", canvasGroup);
            SetPrivateField(manualSnapshotUi, "actionIconImage", actionIcon);
            SetPrivateField(manualSnapshotUi, "captureProgressImage", progressImage);
            SetPrivateField(manualSnapshotUi, "unavailableOverlayImage", unavailableOverlay);
            SetPrivateField(manualSnapshotUi, "captureInputActionReference", actionReference);
            SetPrivateField(manualSnapshotUi, "fadeDuration", 0.0f);
            uiRoot.SetActive(true);

            return new ManualSnapshotFixture
            {
                Ui = manualSnapshotUi,
                UIManager = uiManager,
                GameLogicManager = gameLogicManager,
                GameStateManager = gameStateManager,
                StageRoot = stageRootObject,
                Player = playerObject.transform,
                PlayerItemHandleState = playerItemHandleState,
                CanvasGroup = canvasGroup,
                UnavailableOverlay = unavailableOverlay,
            };
        }

        private Component CreateBomb(string name, Transform parent, Vector3 localPosition)
        {
            GameObject bombObject = new GameObject(name);
            bombObject.transform.SetParent(parent, false);
            bombObject.transform.localPosition = localPosition;
            createdObjects.Add(bombObject);

            bombObject.AddComponent<SphereCollider>();
            bombObject.AddComponent<Rigidbody>();
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

        private static void QueueKeyboardState(Keyboard keyboard, params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }

        private static object CreateEntityRef(uint entityId)
        {
            return Activator.CreateInstance(FindRuntimeType(EntityRefTypeName), entityId, 1);
        }

        private static object CreateSceneKernel()
        {
            object sceneKernel = Activator.CreateInstance(FindRuntimeType(SceneKernelTypeName));
            object valueStore = Activator.CreateInstance(FindRuntimeType(ValueStoreServiceTypeName));
            PropertyInfo valueStoreProperty = sceneKernel.GetType().GetProperty("ValueStore", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(valueStoreProperty);
            valueStoreProperty.SetValue(sceneKernel, valueStore);
            return sceneKernel;
        }

        private static object ParseGameState(string stateName)
        {
            return Enum.Parse(FindRuntimeType(GameStateTypeName), stateName, ignoreCase: false);
        }

        private static void SetGameState(object gameStateManager, string stateName)
        {
            InvokeMethod(gameStateManager, "ChangeState", ParseGameState(stateName));
        }

        private static void ClearSingletonInstance(string fullTypeName)
        {
            Type type = FindRuntimeType(fullTypeName);
            FieldInfo field = type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, null);
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

        private static object CreateTypedList(Type elementType, params object[] items)
        {
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            for (int i = 0; i < items.Length; i++)
                list.Add(items[i]);

            return list;
        }

        private static T GetPropertyValue<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property on {target.GetType().Name}: {propertyName}");
            return (T)property.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static object InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, args);
        }

        private sealed class ManualSnapshotFixture
        {
            public Component Ui { get; set; }
            public Component UIManager { get; set; }
            public Component GameLogicManager { get; set; }
            public Component GameStateManager { get; set; }
            public GameObject StageRoot { get; set; }
            public Transform Player { get; set; }
            public Component PlayerItemHandleState { get; set; }
            public CanvasGroup CanvasGroup { get; set; }
            public Image UnavailableOverlay { get; set; }
        }
    }
}

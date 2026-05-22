using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class EntityFacingPlayModeTests
    {
        private const string FacingControllerTypeName = "BC.Base.EntityFacingControllerMB";
        private const string PlayerItemHandleStateTypeName = "BC.Player.PlayerItemHandleStateMB";
        private const string NpcObjectTypeName = "BC.Character.NPCObjectMB";
        private const string InteractionEventDataTypeName = "BC.Player.InteractionEventData";
        private const string InteractionEventTypeName = "BC.Player.InteractionEventType";
        private const string EntityRefTypeName = "BC.Base.EntityRef";
        private const string SceneKernelTypeName = "BC.Base.SceneKernel";
        private const string EntityTargetReferenceTypeName = "BC.Base.EntityTargetReference";
        private const string SetFacingRuntimeTypeName = "BC.ActionSystem.SetEntityFacingTargetStepRuntime";
        private const string ClearFacingRuntimeTypeName = "BC.ActionSystem.ClearEntityFacingStepRuntime";

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
        public IEnumerator InteractionFacing_StartsMutualFacing_AndCanceledRestoresMovementChannels()
        {
            LogAssert.Expect(LogType.Warning, "PlayerItemHandleStateMB: EntityMB is not found or not bound.");

            PlayerFacingFixture fixture = CreatePlayerFacingFixture();
            GameObject npcRoot = new GameObject("NpcFacingTarget");
            npcRoot.SetActive(false);
            npcRoot.transform.position = new Vector3(4f, 0f, 0f);
            createdObjects.Add(npcRoot);

            Component npcFacing = npcRoot.AddComponent(FindRuntimeType(FacingControllerTypeName));
            Component npc = npcRoot.AddComponent(FindRuntimeType(NpcObjectTypeName));

            SetPrivateField(npcFacing, "defaultTurnSharpness", 64f);
            SetPrivateField(npc, "requestInteractorToFaceTarget", true);
            SetPrivateField(npc, "faceInteractorOnInteraction", true);
            SetPrivateField(npc, "facingController", npcFacing);

            fixture.Root.SetActive(true);
            npcRoot.SetActive(true);
            yield return null;

            InvokeMethod(fixture.FacingController, "SetFacingDirection", "Movement", Vector3.forward, 0, 64f);
            InvokeMethod(npcFacing, "SetFacingDirection", "Movement", Vector3.forward, 0, 64f);
            ApplyFacingNow(fixture.FacingController);
            ApplyFacingNow(npcFacing);

            object sourceEntity = CreateEntityRef(11u, 1);
            object startedEvent = CreateInteractionEventData(fixture.PlayerModel.transform, sourceEntity, npc, "Started");

            InvokeMethod(fixture.HandleState, "HandleInteractionEvent", startedEvent);
            InvokeMethod(npc, "OnInteractionStarted", startedEvent);
            ApplyFacingNow(fixture.FacingController);
            ApplyFacingNow(npcFacing);

            AssertFacingApproximately(fixture.Root.transform.forward, Vector3.right, "Player should face the interacted NPC.");
            AssertFacingApproximately(npcRoot.transform.forward, Vector3.left, "NPC should face the interacting player.");

            object canceledEvent = CreateInteractionEventData(fixture.PlayerModel.transform, sourceEntity, npc, "Canceled");
            InvokeMethod(fixture.HandleState, "HandleInteractionEvent", canceledEvent);
            InvokeMethod(npc, "OnInteractionCanceled", canceledEvent);
            ApplyFacingNow(fixture.FacingController);
            ApplyFacingNow(npcFacing);

            AssertFacingApproximately(fixture.Root.transform.forward, Vector3.forward, "Player should return to its movement facing after interaction cancel.");
            AssertFacingApproximately(npcRoot.transform.forward, Vector3.forward, "NPC should return to its previous facing after interaction cancel.");
        }

        [UnityTest]
        public IEnumerator ActionFacingSteps_TargetTriggerEntity_AndClearOverride()
        {
            GameObject actorRoot = new GameObject("FacingActor");
            actorRoot.transform.position = Vector3.zero;
            createdObjects.Add(actorRoot);

            Component actorFacing = actorRoot.AddComponent(FindRuntimeType(FacingControllerTypeName));
            SetPrivateField(actorFacing, "defaultTurnSharpness", 64f);

            GameObject triggerRoot = new GameObject("FacingTrigger");
            triggerRoot.transform.position = new Vector3(3f, 0f, 0f);
            createdObjects.Add(triggerRoot);

            actorRoot.SetActive(true);
            triggerRoot.SetActive(true);

            InvokeMethod(actorFacing, "SetFacingDirection", "Movement", Vector3.forward, 0, 64f);
            ApplyFacingNow(actorFacing);

            object sceneKernel = CreateInstance(SceneKernelTypeName);
            object entityComponents = GetPropertyValue(sceneKernel, "EntityComponents");
            object actionService = GetPropertyValue(sceneKernel, "Actions");
            object actorEntity = CreateEntityRef(1u, 1);
            object triggerEntity = CreateEntityRef(2u, 1);

            InvokeMethod(entityComponents, "Register", actorEntity, actorRoot, actorRoot.transform);
            InvokeMethod(entityComponents, "Register", triggerEntity, triggerRoot, triggerRoot.transform);

            object actionContext = CreateInstance(
                "BC.ActionSystem.ActionExecutionContext",
                sceneKernel,
                actionService,
                actorEntity,
                triggerEntity,
                null);

            object selfTarget = InvokeStaticMethod(EntityTargetReferenceTypeName, "Self");
            object triggerTarget = InvokeStaticMethod(EntityTargetReferenceTypeName, "Trigger");

            object setDefinition = CreateInstance(SetFacingRuntimeTypeName, selfTarget, triggerTarget, "Conversation");
            object setRuntime = InvokeMethod(setDefinition, "CreateRuntime");
            InvokeTick(setRuntime, actionContext, 8);
            ApplyFacingNow(actorFacing);

            AssertFacingApproximately(actorRoot.transform.forward, Vector3.right, "Action step should make the actor face the trigger entity.");

            object clearDefinition = CreateInstance(ClearFacingRuntimeTypeName, selfTarget, "Conversation");
            object clearRuntime = InvokeMethod(clearDefinition, "CreateRuntime");
            InvokeTick(clearRuntime, actionContext, 8);
            ApplyFacingNow(actorFacing);

            AssertFacingApproximately(actorRoot.transform.forward, Vector3.forward, "Clearing the action facing channel should restore the lower-priority movement facing.");
            yield break;
        }

        [UnityTest]
        public IEnumerator FrontDirection_BackAxisMakesBackFaceRequestedDirection()
        {
            GameObject root = new GameObject("FrontDirectionFacingRoot");
            createdObjects.Add(root);

            Component facingController = root.AddComponent(FindRuntimeType(FacingControllerTypeName));
            SetPrivateField(facingController, "defaultTurnSharpness", 64f);
            SetPrivateField(facingController, "frontDirection", Vector3.back);

            root.SetActive(true);
            yield return null;

            InvokeMethod(facingController, "SetFacingDirection", "Movement", Vector3.right, 0, 64f);
            ApplyFacingNow(facingController);

            AssertLocalAxisFacingApproximately(root.transform, Vector3.back, Vector3.right,
                "Configured local back axis should face the requested world direction.");
            AssertFacingApproximately(root.transform.forward, Vector3.left,
                "When back is treated as front, local forward should point opposite the requested direction.");
        }

        private PlayerFacingFixture CreatePlayerFacingFixture()
        {
            GameObject root = new GameObject("PlayerFacingRoot");
            root.SetActive(false);
            createdObjects.Add(root);

            GameObject handlePointObject = new GameObject("HandlePoint");
            handlePointObject.transform.SetParent(root.transform, false);
            handlePointObject.transform.localPosition = new Vector3(0f, 1f, 0.6f);

            GameObject playerModelObject = new GameObject("PlayerModel");
            playerModelObject.transform.SetParent(root.transform, false);
            playerModelObject.transform.localRotation = Quaternion.identity;

            Component facingController = root.AddComponent(FindRuntimeType(FacingControllerTypeName));
            SetPrivateField(facingController, "defaultTurnSharpness", 64f);

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
            SetPrivateField(handleState, "faceInteractionTargetOnStart", true);
            SetPrivateField(handleState, "facingController", facingController);

            return new PlayerFacingFixture
            {
                Root = root,
                PlayerModel = playerModelObject,
                HandleState = handleState,
                FacingController = facingController,
            };
        }

        private static object CreateInteractionEventData(Transform sourceFacingTransform, object sourceEntity, object interactable, string eventTypeName)
        {
            Type eventType = FindRuntimeType(InteractionEventTypeName);
            object eventTypeValue = Enum.Parse(eventType, eventTypeName);

            return CreateInstance(
                InteractionEventDataTypeName,
                null,
                sourceEntity,
                sourceFacingTransform,
                interactable,
                eventTypeValue,
                0f,
                0f);
        }

        private static object CreateEntityRef(uint entityId, int version)
        {
            return CreateInstance(EntityRefTypeName, entityId, version);
        }

        private static void InvokeTick(object runtime, object actionContext, int remainingOperations)
        {
            MethodInfo method = runtime.GetType().GetMethod("Tick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {runtime.GetType().Name}: Tick");

            object[] arguments = { actionContext, remainingOperations };
            method.Invoke(runtime, arguments);
        }

        private static void AssertFacingApproximately(Vector3 actualForward, Vector3 expectedForward, string message)
        {
            actualForward.y = 0f;
            expectedForward.y = 0f;

            actualForward.Normalize();
            expectedForward.Normalize();

            float dot = Vector3.Dot(actualForward, expectedForward);
            Assert.Greater(dot, 0.92f, $"{message} Actual dot={dot}");
        }

        private static void AssertLocalAxisFacingApproximately(Transform transform, Vector3 localAxis, Vector3 expectedWorldDirection, string message)
        {
            Vector3 actualWorldDirection = transform.rotation * localAxis;
            AssertFacingApproximately(actualWorldDirection, expectedWorldDirection, message);
        }

        private static void ApplyFacingNow(object facingController)
        {
            MethodInfo method = facingController.GetType().GetMethod("ApplyFacing", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected private method on {facingController.GetType().Name}: ApplyFacing");
            method.Invoke(facingController, new object[] { 1.0f });
        }

        private static object CreateInstance(string fullTypeName, params object[] arguments)
        {
            Type type = FindRuntimeType(fullTypeName);
            return Activator.CreateInstance(type, arguments);
        }

        private static object InvokeStaticMethod(string fullTypeName, string methodName)
        {
            Type type = FindRuntimeType(fullTypeName);
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected static method on {type.Name}: {methodName}");
            return method.Invoke(null, null);
        }

        private static object InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, args);
        }

        private static object GetPropertyValue(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property on {target.GetType().Name}: {propertyName}");
            return property.GetValue(target);
        }

        private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
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

        private sealed class PlayerFacingFixture
        {
            public GameObject Root { get; set; }
            public GameObject PlayerModel { get; set; }
            public Component HandleState { get; set; }
            public Component FacingController { get; set; }
        }
    }
}
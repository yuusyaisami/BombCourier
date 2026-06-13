#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using BC.Base;
using BC.Gimmick;
using BC.Item;
using BC.Player;
using BC.Tutorial;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BC.Editor.Tests
{
    public sealed class TutorialBuiltInConditionsEditModeTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new();
        private readonly List<InputAction> createdActions = new();

        [TearDown]
        public void TearDown()
        {
            ClearApplicationKernelInstance();

            for (int i = createdActions.Count - 1; i >= 0; i--)
                createdActions[i]?.Dispose();

            createdActions.Clear();

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void MoveDistanceCondition_CompletesAfterAccumulatedPlanarDistance()
        {
            SceneKernel kernel = CreateSceneKernel();
            TutorialStageAuthoringMB stage = CreateStageAuthoring();
            EntityRef playerEntity = new EntityRef(1, 1);
            Transform playerTransform = CreateRegisteredTransform(kernel, playerEntity, "Player");

            var authoring = new TutorialMoveDistanceConditionAuthoring();
            SetPrivateField(authoring, "requiredDistance", 2.0f);
            SetPrivateField(authoring, "planarOnly", true);

            ITutorialConditionRuntime runtime = authoring.CreateRuntime();
            runtime.Start(new TutorialConditionContext(kernel, stage, playerEntity, playerEntity), null);

            playerTransform.position = new Vector3(1.0f, 0.0f, 0.0f);
            runtime.Tick(0.016f);
            Assert.IsFalse(runtime.IsCompleted);

            playerTransform.position = new Vector3(1.0f, 3.0f, 0.0f);
            runtime.Tick(0.016f);
            Assert.IsFalse(runtime.IsCompleted);

            playerTransform.position = new Vector3(2.2f, 3.0f, 0.0f);
            runtime.Tick(0.016f);
            Assert.IsTrue(runtime.IsCompleted);
        }

        [Test]
        public void JumpCondition_CompletesAfterConfiguredJumpCount()
        {
            SceneKernel kernel = CreateSceneKernel();
            TutorialStageAuthoringMB stage = CreateStageAuthoring();
            EntityRef playerEntity = new EntityRef(2, 1);
            EntityMoveMotorMB moveMotor = CreateRegisteredMoveMotor(kernel, playerEntity);

            var authoring = new TutorialJumpConditionAuthoring();
            SetPrivateField(authoring, "requiredJumpCount", 2);

            ITutorialConditionRuntime runtime = authoring.CreateRuntime();
            runtime.Start(new TutorialConditionContext(kernel, stage, playerEntity, playerEntity), null);

            RaiseActionEvent(moveMotor, "Jumped");
            Assert.IsFalse(runtime.IsCompleted);

            RaiseActionEvent(moveMotor, "Jumped");
            Assert.IsTrue(runtime.IsCompleted);
        }

        [Test]
        public void ReachLineCondition_CompletesWhenCrossingConfiguredPlane()
        {
            SceneKernel kernel = CreateSceneKernel();
            TutorialStageAuthoringMB stage = CreateStageAuthoring();
            EntityRef playerEntity = new EntityRef(3, 1);
            Transform playerTransform = CreateRegisteredTransform(kernel, playerEntity, "Player");
            playerTransform.position = new Vector3(0.0f, 0.0f, -1.0f);

            TutorialReachLineMB reachLine = CreateReachLine(
                "ReachLine",
                TutorialReachLineNormalAxis.Forward,
                TutorialReachLineTriggerMode.CrossFromBackToFront,
                0.0f);

            var authoring = new TutorialReachLineConditionAuthoring();
            SetPrivateField(authoring, "reachLine", reachLine);

            ITutorialConditionRuntime runtime = authoring.CreateRuntime();
            runtime.Start(new TutorialConditionContext(kernel, stage, playerEntity, playerEntity), null);
            Assert.IsFalse(runtime.IsCompleted);

            playerTransform.position = new Vector3(0.0f, 0.0f, 0.5f);
            runtime.Tick(0.016f);

            Assert.IsTrue(runtime.IsCompleted);
        }

        [Test]
        public void ReachLineCondition_DoesNotCompleteImmediatelyWhenStartingOnFrontSide()
        {
            SceneKernel kernel = CreateSceneKernel();
            TutorialStageAuthoringMB stage = CreateStageAuthoring();
            EntityRef playerEntity = new EntityRef(31, 1);
            Transform playerTransform = CreateRegisteredTransform(kernel, playerEntity, "Player");
            playerTransform.position = new Vector3(1.0f, 0.0f, 0.0f);

            TutorialReachLineMB reachLine = CreateReachLine(
                "ReachLine",
                TutorialReachLineNormalAxis.Right,
                TutorialReachLineTriggerMode.CrossFromBackToFront,
                0.0f);

            var authoring = new TutorialReachLineConditionAuthoring();
            SetPrivateField(authoring, "reachLine", reachLine);

            ITutorialConditionRuntime runtime = authoring.CreateRuntime();
            runtime.Start(new TutorialConditionContext(kernel, stage, playerEntity, playerEntity), null);

            Assert.IsFalse(runtime.IsCompleted);

            playerTransform.position = new Vector3(2.0f, 0.0f, 0.0f);
            runtime.Tick(0.016f);
            Assert.IsFalse(runtime.IsCompleted);

            playerTransform.position = new Vector3(-1.0f, 0.0f, 0.0f);
            runtime.Tick(0.016f);
            Assert.IsFalse(runtime.IsCompleted);

            playerTransform.position = new Vector3(0.25f, 0.0f, 0.0f);
            runtime.Tick(0.016f);

            Assert.IsTrue(runtime.IsCompleted);
        }

        [Test]
        public void ReachLineCondition_CompletesWhenEnteringToleranceBandFromEitherSide()
        {
            SceneKernel kernel = CreateSceneKernel();
            TutorialStageAuthoringMB stage = CreateStageAuthoring();
            EntityRef playerEntity = new EntityRef(32, 1);
            Transform playerTransform = CreateRegisteredTransform(kernel, playerEntity, "Player");
            playerTransform.position = new Vector3(2.0f, 0.0f, 0.0f);

            TutorialReachLineMB reachLine = CreateReachLine(
                "ReachLine",
                TutorialReachLineNormalAxis.Right,
                TutorialReachLineTriggerMode.CrossEitherDirection,
                0.25f);

            var authoring = new TutorialReachLineConditionAuthoring();
            SetPrivateField(authoring, "reachLine", reachLine);

            ITutorialConditionRuntime runtime = authoring.CreateRuntime();
            runtime.Start(new TutorialConditionContext(kernel, stage, playerEntity, playerEntity), null);

            Assert.IsFalse(runtime.IsCompleted);

            playerTransform.position = new Vector3(0.2f, 0.0f, 0.0f);
            runtime.Tick(0.016f);

            Assert.IsTrue(runtime.IsCompleted);
        }

        [Test]
        public void ReachLineMarker_ComputesSignedDistanceFromConfiguredAxis()
        {
            TutorialReachLineMB reachLine = CreateReachLine(
                "ReachLineAxis",
                TutorialReachLineNormalAxis.Right,
                TutorialReachLineTriggerMode.CrossFromBackToFront,
                0.0f);

            reachLine.transform.position = new Vector3(2.0f, 0.0f, 0.0f);
            reachLine.transform.rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);

            float frontDistance = reachLine.ComputeSignedDistance(new Vector3(2.0f, 0.0f, -3.0f));
            float backDistance = reachLine.ComputeSignedDistance(new Vector3(2.0f, 0.0f, 1.5f));

            Assert.Greater(frontDistance, 0.0f);
            Assert.Less(backDistance, 0.0f);
        }

        [Test]
        public void HoldItemCondition_CompletesWhenRequiredTagIsHeld()
        {
            SceneKernel kernel = CreateSceneKernel();
            TutorialStageAuthoringMB stage = CreateStageAuthoring();
            EntityRef playerEntity = new EntityRef(4, 1);
            PlayerItemHandleStateMB handleState = CreateRegisteredHandleState(kernel, playerEntity);
            DummyCarryableItemMB carryItem = CreateTaggedCarryItem(9101);

            var authoring = new TutorialHoldItemConditionAuthoring();
            SetPrivateField(authoring, "requiredHeldItemTag", EntityTagReference.From(new EntityTagId(9101)));

            ITutorialConditionRuntime runtime = authoring.CreateRuntime();
            runtime.Start(new TutorialConditionContext(kernel, stage, playerEntity, playerEntity), null);
            Assert.IsFalse(runtime.IsCompleted);

            SetPrivateField(handleState, "isHandlingItem", true);
            InvokeMethod(handleState, "SetCurrentHandledItem", carryItem);

            Assert.IsTrue(runtime.IsCompleted);
        }

        [Test]
        public void ThrowItemCondition_CompletesAfterThrowSequenceIncrease()
        {
            SceneKernel kernel = CreateSceneKernel();
            TutorialStageAuthoringMB stage = CreateStageAuthoring();
            EntityRef playerEntity = new EntityRef(5, 1);
            CreateRegisteredTransform(kernel, playerEntity, "Player");
            kernel.ValueStore.Set(playerEntity, ValueKeys.Runtime.ThrowSequence, 0);

            var authoring = new TutorialThrowItemConditionAuthoring();
            SetPrivateField(authoring, "requiredThrowCount", 2);

            ITutorialConditionRuntime runtime = authoring.CreateRuntime();
            runtime.Start(new TutorialConditionContext(kernel, stage, playerEntity, playerEntity), null);

            kernel.ValueStore.Set(playerEntity, ValueKeys.Runtime.ThrowSequence, 1);
            runtime.Tick(0.016f);
            Assert.IsFalse(runtime.IsCompleted);

            kernel.ValueStore.Set(playerEntity, ValueKeys.Runtime.ThrowSequence, 2);
            runtime.Tick(0.016f);
            Assert.IsTrue(runtime.IsCompleted);
        }

        [Test]
        public void ThrowItemCondition_DropDoesNotCompleteWhenOptionDisabled()
        {
            SceneKernel kernel = CreateSceneKernel();
            TutorialStageAuthoringMB stage = CreateStageAuthoring();
            EntityRef playerEntity = new EntityRef(51, 1);
            PlayerItemHandleStateMB handleState = CreateRegisteredHandleState(kernel, playerEntity);
            kernel.ValueStore.Set(playerEntity, ValueKeys.Runtime.ThrowSequence, 0);
            SetPrivateField(handleState, "isHandlingItem", true);

            var authoring = new TutorialThrowItemConditionAuthoring();
            SetPrivateField(authoring, "requiredThrowCount", 1);
            SetPrivateField(authoring, "countDropReleaseAsSuccess", false);

            ITutorialConditionRuntime runtime = authoring.CreateRuntime();
            runtime.Start(new TutorialConditionContext(kernel, stage, playerEntity, playerEntity), null);

            SetPrivateField(handleState, "isHandlingItem", false);
            runtime.Tick(0.016f);

            Assert.IsFalse(runtime.IsCompleted);
        }

        [Test]
        public void ThrowItemCondition_DropCompletesWhenOptionEnabled()
        {
            SceneKernel kernel = CreateSceneKernel();
            TutorialStageAuthoringMB stage = CreateStageAuthoring();
            EntityRef playerEntity = new EntityRef(52, 1);
            PlayerItemHandleStateMB handleState = CreateRegisteredHandleState(kernel, playerEntity);
            kernel.ValueStore.Set(playerEntity, ValueKeys.Runtime.ThrowSequence, 0);
            SetPrivateField(handleState, "isHandlingItem", true);

            var authoring = new TutorialThrowItemConditionAuthoring();
            SetPrivateField(authoring, "requiredThrowCount", 1);
            SetPrivateField(authoring, "countDropReleaseAsSuccess", true);

            ITutorialConditionRuntime runtime = authoring.CreateRuntime();
            runtime.Start(new TutorialConditionContext(kernel, stage, playerEntity, playerEntity), null);

            SetPrivateField(handleState, "isHandlingItem", false);
            runtime.Tick(0.016f);

            Assert.IsTrue(runtime.IsCompleted);
        }

        [Test]
        public void ThrowItemCondition_EnabledOptionDoesNotDoubleCountThrowAsDrop()
        {
            SceneKernel kernel = CreateSceneKernel();
            TutorialStageAuthoringMB stage = CreateStageAuthoring();
            EntityRef playerEntity = new EntityRef(53, 1);
            PlayerItemHandleStateMB handleState = CreateRegisteredHandleState(kernel, playerEntity);
            kernel.ValueStore.Set(playerEntity, ValueKeys.Runtime.ThrowSequence, 0);
            SetPrivateField(handleState, "isHandlingItem", true);

            var authoring = new TutorialThrowItemConditionAuthoring();
            SetPrivateField(authoring, "requiredThrowCount", 2);
            SetPrivateField(authoring, "countDropReleaseAsSuccess", true);

            ITutorialConditionRuntime runtime = authoring.CreateRuntime();
            runtime.Start(new TutorialConditionContext(kernel, stage, playerEntity, playerEntity), null);

            kernel.ValueStore.Set(playerEntity, ValueKeys.Runtime.ThrowSequence, 1);
            SetPrivateField(handleState, "isHandlingItem", false);
            runtime.Tick(0.016f);

            Assert.IsFalse(runtime.IsCompleted);
        }

        [Test]
        public void BreakableGateCondition_CompletesAfterGateBreaks()
        {
            SceneKernel kernel = CreateSceneKernel();
            TutorialStageAuthoringMB stage = CreateStageAuthoring();
            EntityRef playerEntity = new EntityRef(6, 1);
            CreateRegisteredTransform(kernel, playerEntity, "Player");
            BreakableGateObjectMB gate = CreateBreakableGate(threshold: 5.0f, explosionForce: 12.0f);

            var authoring = new TutorialBreakableGateBrokenConditionAuthoring();
            SetPrivateField(authoring, "targetGate", gate);

            ITutorialConditionRuntime runtime = authoring.CreateRuntime();
            runtime.Start(new TutorialConditionContext(kernel, stage, playerEntity, playerEntity), null);
            Assert.IsFalse(runtime.IsCompleted);

            gate.OnBombImpactReceived(Vector3.forward, 8.0f);
            runtime.Tick(0.016f);

            Assert.IsTrue(runtime.IsCompleted);
        }

        [Test]
        public void ValueStoreConditions_ObserveEntityAndKernelValues()
        {
            SceneKernel kernel = CreateSceneKernel();
            TutorialStageAuthoringMB stage = CreateStageAuthoring();
            EntityRef playerEntity = new EntityRef(7, 1);
            CreateRegisteredTransform(kernel, playerEntity, "Player");

            kernel.ValueStore.Set(playerEntity, ValueKeys.Runtime.IsGrounded, false);
            kernel.ValueStore.Set(playerEntity, ValueKeys.Runtime.ThrowSequence, 0);

            ApplicationKernel kernelFixture = InstallApplicationKernelFixture();
            kernelFixture.KernelValueStore.Set(ValueKeys.AppSettings.CameraSensitivity, 0.08f);

            var boolAuthoring = new TutorialValueStoreBoolConditionAuthoring();
            SetPrivateField(boolAuthoring, "keyRef", ValueKeyReference.From(ValueKeys.Runtime.IsGrounded));
            SetPrivateField(boolAuthoring, "targetValue", true);

            var intAuthoring = new TutorialValueStoreIntConditionAuthoring();
            SetPrivateField(intAuthoring, "keyRef", ValueKeyReference.From(ValueKeys.Runtime.ThrowSequence));
            SetPrivateField(intAuthoring, "comparison", TutorialNumericComparisonOperator.GreaterOrEqual);
            SetPrivateField(intAuthoring, "compareValue", 2);

            var floatAuthoring = new TutorialValueStoreFloatConditionAuthoring();
            SetPrivateField(floatAuthoring, "storeScope", TutorialValueStoreScope.ApplicationKernel);
            SetPrivateField(floatAuthoring, "keyRef", ValueKeyReference.From(ValueKeys.AppSettings.CameraSensitivity));
            SetPrivateField(floatAuthoring, "comparison", TutorialNumericComparisonOperator.LessOrEqual);
            SetPrivateField(floatAuthoring, "compareValue", 0.05f);
            SetPrivateField(floatAuthoring, "equalityTolerance", 0.001f);

            ITutorialConditionRuntime boolRuntime = boolAuthoring.CreateRuntime();
            ITutorialConditionRuntime intRuntime = intAuthoring.CreateRuntime();
            ITutorialConditionRuntime floatRuntime = floatAuthoring.CreateRuntime();

            TutorialConditionContext context = new TutorialConditionContext(kernel, stage, playerEntity, playerEntity);
            boolRuntime.Start(context, null);
            intRuntime.Start(context, null);
            floatRuntime.Start(context, null);

            Assert.IsFalse(boolRuntime.IsCompleted);
            Assert.IsFalse(intRuntime.IsCompleted);
            Assert.IsFalse(floatRuntime.IsCompleted);

            kernel.ValueStore.Set(playerEntity, ValueKeys.Runtime.IsGrounded, true);
            kernel.ValueStore.Set(playerEntity, ValueKeys.Runtime.ThrowSequence, 2);
            kernelFixture.KernelValueStore.Set(ValueKeys.AppSettings.CameraSensitivity, 0.04f);

            boolRuntime.Tick(0.016f);
            intRuntime.Tick(0.016f);
            floatRuntime.Tick(0.016f);

            Assert.IsTrue(boolRuntime.IsCompleted);
            Assert.IsTrue(intRuntime.IsCompleted);
            Assert.IsTrue(floatRuntime.IsCompleted);
        }

        private SceneKernel CreateSceneKernel()
        {
            return new SceneKernel
            {
                EntitiesRegistry = new ScopedEntityRegistry(EntityLifetimeScope.Scene, new EntityIdAllocator()),
                EntityValueStore = new ValueStoreService(),
            };
        }

        private TutorialStageAuthoringMB CreateStageAuthoring()
        {
            GameObject gameObject = new GameObject("TutorialStage");
            createdObjects.Add(gameObject);
            return gameObject.AddComponent<TutorialStageAuthoringMB>();
        }

        private Transform CreateRegisteredTransform(SceneKernel kernel, EntityRef entity, string name)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            kernel.EntityComponents.Register(entity, gameObject, gameObject.transform);
            return gameObject.transform;
        }

        private TutorialReachLineMB CreateReachLine(
            string name,
            TutorialReachLineNormalAxis normalAxis,
            TutorialReachLineTriggerMode triggerMode,
            float distanceTolerance)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);

            TutorialReachLineMB reachLine = gameObject.AddComponent<TutorialReachLineMB>();
            SetPrivateField(reachLine, "normalAxis", normalAxis);
            SetPrivateField(reachLine, "triggerMode", triggerMode);
            SetPrivateField(reachLine, "distanceTolerance", distanceTolerance);
            return reachLine;
        }

        private EntityMoveMotorMB CreateRegisteredMoveMotor(SceneKernel kernel, EntityRef entity)
        {
            GameObject gameObject = new GameObject("PlayerMoveMotor");
            createdObjects.Add(gameObject);
            gameObject.AddComponent<Rigidbody>();
            gameObject.AddComponent<CapsuleCollider>();
            EntityMoveMotorMB moveMotor = gameObject.AddComponent<EntityMoveMotorMB>();
            kernel.EntityComponents.Register(entity, gameObject, gameObject.transform);
            return moveMotor;
        }

        private PlayerItemHandleStateMB CreateRegisteredHandleState(SceneKernel kernel, EntityRef entity)
        {
            GameObject root = new GameObject("PlayerHandleState");
            root.SetActive(false);
            createdObjects.Add(root);

            GameObject handlePointObject = new GameObject("HandlePoint");
            handlePointObject.transform.SetParent(root.transform, false);
            GameObject playerModelObject = new GameObject("PlayerModel");
            playerModelObject.transform.SetParent(root.transform, false);
            createdObjects.Add(handlePointObject);
            createdObjects.Add(playerModelObject);

            EntityMB entityMb = root.AddComponent<EntityMB>();
            entityMb.Bind(entity);

            PlayerItemHandleStateMB handleState = root.AddComponent<PlayerItemHandleStateMB>();
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
            kernel.EntityComponents.Register(entity, root, root.transform);
            return handleState;
        }

        private DummyCarryableItemMB CreateTaggedCarryItem(int tagId)
        {
            GameObject gameObject = new GameObject("CarryItem");
            createdObjects.Add(gameObject);

            EntityMB entityMb = gameObject.AddComponent<EntityMB>();
            SetPrivateField(entityMb, "tagReference", EntityTagReference.From(new EntityTagId(tagId)));

            return gameObject.AddComponent<DummyCarryableItemMB>();
        }

        private BreakableGateObjectMB CreateBreakableGate(float threshold, float explosionForce)
        {
            GameObject root = new GameObject("BreakableGate");
            createdObjects.Add(root);

            BoxCollider gateCollider = root.AddComponent<BoxCollider>();
            GameObject partObject = new GameObject("Part");
            partObject.transform.SetParent(root.transform, false);
            createdObjects.Add(partObject);

            Rigidbody partBody = partObject.AddComponent<Rigidbody>();
            partBody.isKinematic = true;
            partBody.useGravity = false;
            partObject.AddComponent<BoxCollider>();

            BreakableGateObjectMB gate = root.AddComponent<BreakableGateObjectMB>();
            SetPrivateField(gate, "breakableParts", new List<Rigidbody> { partBody });
            SetPrivateField(gate, "gateCollider", gateCollider);
            SetPrivateField(gate, "breakForceDirection", Vector3.up);
            SetPrivateField(gate, "breakForceOrigin", root.transform);
            SetPrivateField(gate, "breakForceThreshold", threshold);
            SetPrivateField(gate, "explosionForce", explosionForce);
            SetPrivateField(gate, "partCollisionEnableDelay", 0.0f);
            SetPrivateField(gate, "breakStabilizeDuration", 0.0f);
            return gate;
        }

        private ApplicationKernel InstallApplicationKernelFixture()
        {
            GameObject gameObject = new GameObject("ApplicationKernelFixture");
            gameObject.SetActive(false);
            createdObjects.Add(gameObject);

            ApplicationKernelMB kernelMb = gameObject.AddComponent<ApplicationKernelMB>();
            ApplicationKernel kernel = new ApplicationKernel
            {
                KernelValueStore = new KernelValueStoreService(),
            };

            SetPrivateField(kernelMb, "kernel", kernel);
            SetStaticAutoPropertyBackingField(typeof(ApplicationKernelMB), "Instance", kernelMb);
            return kernel;
        }

        private static void ClearApplicationKernelInstance()
        {
            SetStaticAutoPropertyBackingField(typeof(ApplicationKernelMB), "Instance", null);
        }

        private static void RaiseActionEvent(object target, string eventFieldName)
        {
            FieldInfo field = target.GetType().GetField(eventFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected backing event field on {target.GetType().Name}: {eventFieldName}");
            (field.GetValue(target) as Action)?.Invoke();
        }

        private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static void SetStaticAutoPropertyBackingField(Type declaringType, string propertyName, object value)
        {
            FieldInfo field = declaringType.GetField($"<{propertyName}>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected static auto-property backing field: {declaringType.Name}.{propertyName}");
            field.SetValue(null, value);
        }

        private static void InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            method.Invoke(target, args);
        }

        private sealed class DummyCarryableItemMB : MonoBehaviour, ICarryableItem
        {
            public Transform ItemTransform => transform;
            public bool IsHandled { get; private set; }
            public bool CanBeCarried => true;

            public void OnHandle(Transform handlePoint)
            {
                IsHandled = true;
            }

            public void OnRelease(Vector3 throwVelocity)
            {
                IsHandled = false;
            }
        }
    }
}
#endif

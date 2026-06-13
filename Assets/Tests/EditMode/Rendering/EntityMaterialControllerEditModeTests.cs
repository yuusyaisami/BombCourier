#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using BC.Base;
using BC.Manager;
using BC.Managers;
using BC.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace BC.Editor.Tests
{
    public sealed class EntityMaterialControllerEditModeTests
    {
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
        public void TryApply_UsesSharedMaterialReference()
        {
            Material baseMaterial = CreateMaterial("Base");
            Material darkMaterial = CreateMaterial("Dark");
            EntityMaterialSetSO materialSet = CreateMaterialSet(
                CreateDataset(EntityMaterialSetSO.DefaultDatasetKind, ("Body", darkMaterial)));

            MeshRenderer renderer = CreateRenderer("Renderer", baseMaterial);
            EntityMaterialControllerMB controller = CreateController(
                "Controller",
                materialSet,
                new EntityMaterialSlotBindingData("Body", renderer, 0));

            bool applied = controller.TryApplyDatasetKind(EntityMaterialSetSO.DefaultDatasetKind, out string failureReason);

            Assert.IsTrue(applied, failureReason);
            Assert.AreSame(darkMaterial, renderer.sharedMaterials[0]);
        }

        [Test]
        public void TryApply_RejectsMissingSlotMaterialAndKeepsOriginalState()
        {
            Material baseMaterial = CreateMaterial("Base");
            Material darkMaterial = CreateMaterial("Dark");
            EntityMaterialSetSO materialSet = CreateMaterialSet(
                CreateDataset(EntityMaterialSetSO.DefaultDatasetKind, ("Other", darkMaterial)));

            MeshRenderer renderer = CreateRenderer("Renderer", baseMaterial);
            EntityMaterialControllerMB controller = CreateController(
                "Controller",
                materialSet,
                new EntityMaterialSlotBindingData("Body", renderer, 0));

            bool applied = controller.TryApplyDatasetKind(EntityMaterialSetSO.DefaultDatasetKind, out string failureReason);

            Assert.IsFalse(applied);
            Assert.That(failureReason, Does.Contain("does not define slot key 'Body'"));
            Assert.AreSame(baseMaterial, renderer.sharedMaterials[0]);
        }

        [Test]
        public void ValidateDefinition_RejectsDuplicateSlotKeys()
        {
            Material baseMaterial = CreateMaterial("Base");
            Material darkMaterial = CreateMaterial("Dark");
            EntityMaterialSetSO materialSet = CreateMaterialSet(
                CreateDataset(EntityMaterialSetSO.DefaultDatasetKind, ("Body", darkMaterial), ("Body2", darkMaterial)));

            MeshRenderer renderer = CreateRenderer("Renderer", new[] { baseMaterial, baseMaterial });
            EntityMaterialControllerMB controller = CreateController(
                "Controller",
                materialSet,
                new[]
                {
                    new EntityMaterialSlotBindingData("Body", renderer, 0),
                    new EntityMaterialSlotBindingData("Body", renderer, 1),
                });

            bool valid = controller.ValidateDefinition(out string failureReason);

            Assert.IsFalse(valid);
            Assert.That(failureReason, Does.Contain("duplicate slot key 'Body'"));
        }

        [Test]
        public void RegisterController_ReappliesCurrentDatasetToLateController()
        {
            Material baseMaterial = CreateMaterial("Base");
            Material darkMaterial = CreateMaterial("Dark");
            EntityMaterialSetSO materialSet = CreateMaterialSet(
                CreateDataset(EntityMaterialSetSO.DefaultDatasetKind, ("Body", baseMaterial)),
                CreateDataset("Dark", ("Body", darkMaterial)));

            EntityMaterialDatasetServiceMB service = CreateService();
            bool changed = service.TrySetActiveDatasetKind("Dark", out string failureReason);
            Assert.IsTrue(changed, failureReason);

            MeshRenderer renderer = CreateRenderer("Renderer", baseMaterial);
            EntityMaterialControllerMB controller = CreateController(
                "LateController",
                materialSet,
                new EntityMaterialSlotBindingData("Body", renderer, 0));

            service.RegisterController(controller);

            Assert.AreSame(darkMaterial, renderer.sharedMaterials[0]);
            Assert.AreEqual("Dark", controller.CurrentDatasetKind);
        }

        [Test]
        public void PlayerSpawned_ReappliesCurrentDatasetToSpawnedPlayerController()
        {
            Material baseMaterial = CreateMaterial("Base");
            Material darkMaterial = CreateMaterial("Dark");
            EntityMaterialSetSO materialSet = CreateMaterialSet(
                CreateDataset(EntityMaterialSetSO.DefaultDatasetKind, ("Body", baseMaterial)),
                CreateDataset("Dark", ("Body", darkMaterial)));

            EntityMaterialDatasetServiceMB service = CreateService();

            GameObject gameLogicObject = new GameObject("GameLogic");
            createdObjects.Add(gameLogicObject);
            GameLogicManagerMB gameLogic = gameLogicObject.AddComponent<GameLogicManagerMB>();
            InvokePrivateMethod(gameLogic, "Awake");
            InvokePrivateMethod(service, "EnsureGameLogicSubscription");

            GameObject playerObject = new GameObject("Player");
            createdObjects.Add(playerObject);
            PlayerMB player = playerObject.AddComponent<PlayerMB>();

            MeshRenderer renderer = CreateRenderer("PlayerRenderer", baseMaterial, playerObject.transform);
            EntityMaterialControllerMB controller = CreateController(
                "PlayerController",
                materialSet,
                new[] { new EntityMaterialSlotBindingData("Body", renderer, 0) },
                renderer.gameObject);

            controller.enabled = false;

            bool changed = service.TrySetActiveDatasetKind("Dark", out string failureReason);
            Assert.IsTrue(changed, failureReason);
            Assert.AreSame(baseMaterial, renderer.sharedMaterials[0], "Disabled controller should remain at default until spawn reapply.");

            gameLogic.OnPlayerSpawned?.Invoke(player);

            Assert.AreSame(darkMaterial, renderer.sharedMaterials[0]);
        }

        private EntityMaterialDatasetServiceMB CreateService()
        {
            GameObject gameObject = new GameObject("EntityMaterialDatasetService");
            createdObjects.Add(gameObject);
            return gameObject.AddComponent<EntityMaterialDatasetServiceMB>();
        }

        private MeshRenderer CreateRenderer(string name, Material material, Transform parent = null)
        {
            return CreateRenderer(name, new[] { material }, parent);
        }

        private MeshRenderer CreateRenderer(string name, Material[] materials, Transform parent = null)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            if (parent != null)
                gameObject.transform.SetParent(parent, false);

            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return renderer;
        }

        private EntityMaterialControllerMB CreateController(
            string name,
            EntityMaterialSetSO materialSet,
            EntityMaterialSlotBindingData binding,
            GameObject host = null)
        {
            return CreateController(name, materialSet, new[] { binding }, host);
        }

        private EntityMaterialControllerMB CreateController(
            string name,
            EntityMaterialSetSO materialSet,
            EntityMaterialSlotBindingData[] bindings,
            GameObject host = null)
        {
            GameObject gameObject = host ?? new GameObject(name);
            if (host == null)
                createdObjects.Add(gameObject);

            bool restoreActive = gameObject.activeSelf;
            gameObject.SetActive(false);

            EntityMaterialControllerMB controller = gameObject.GetComponent<EntityMaterialControllerMB>();
            if (controller == null)
                controller = gameObject.AddComponent<EntityMaterialControllerMB>();

            SetPrivateField(controller, "defaultMaterialSet", materialSet);
            SetPrivateField(controller, "slotBindings", BuildSlotBindingList(bindings));
            SetPrivateField(controller, "hasTriedInitialApply", false);
            InvokePrivateMethod(controller, "Awake");

            gameObject.SetActive(restoreActive);
            return controller;
        }

        private EntityMaterialSetSO CreateMaterialSet(params EntityMaterialDatasetVariant[] variants)
        {
            EntityMaterialSetSO materialSet = ScriptableObject.CreateInstance<EntityMaterialSetSO>();
            materialSet.name = "EntityMaterialSet";
            createdObjects.Add(materialSet);
            SetPrivateField(materialSet, "datasetVariants", new List<EntityMaterialDatasetVariant>(variants));
            return materialSet;
        }

        private static EntityMaterialDatasetVariant CreateDataset(string datasetKind, params (string slotKey, Material material)[] materials)
        {
            var datasetEntries = new List<EntityMaterialSlotMaterialEntry>(materials.Length);
            for (int i = 0; i < materials.Length; i++)
            {
                datasetEntries.Add(new EntityMaterialSlotMaterialEntry(materials[i].slotKey, materials[i].material));
            }

            return new EntityMaterialDatasetVariant(datasetKind, datasetEntries);
        }

        private static List<EntityMaterialSlotBinding> BuildSlotBindingList(EntityMaterialSlotBindingData[] bindings)
        {
            var result = new List<EntityMaterialSlotBinding>(bindings.Length);
            for (int i = 0; i < bindings.Length; i++)
            {
                result.Add(new EntityMaterialSlotBinding(bindings[i].SlotKey, bindings[i].Renderer, bindings[i].MaterialIndex));
            }

            return result;
        }

        private Material CreateMaterial(string name)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard")
                            ?? Shader.Find("Sprites/Default");
            Assert.IsNotNull(shader, "Expected at least one basic shader to exist for material tests.");

            Material material = new Material(shader)
            {
                name = name,
            };
            createdObjects.Add(material);
            return material;
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected private method on {target.GetType().Name}: {methodName}");
            method.Invoke(target, args);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private readonly struct EntityMaterialSlotBindingData
        {
            public EntityMaterialSlotBindingData(string slotKey, Renderer renderer, int materialIndex)
            {
                SlotKey = slotKey;
                Renderer = renderer;
                MaterialIndex = materialIndex;
            }

            public string SlotKey { get; }
            public Renderer Renderer { get; }
            public int MaterialIndex { get; }
        }
    }
}
#endif

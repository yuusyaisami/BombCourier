using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class EntityMaterialControllerPlayModeTests
    {
        private const string EntityMaterialControllerTypeName = "BC.Rendering.EntityMaterialControllerMB";
        private const string EntityMaterialSetSoTypeName = "BC.Rendering.EntityMaterialSetSO";
        private const string EntityMaterialDatasetVariantTypeName = "BC.Rendering.EntityMaterialDatasetVariant";
        private const string EntityMaterialSlotMaterialEntryTypeName = "BC.Rendering.EntityMaterialSlotMaterialEntry";
        private const string EntityMaterialSlotBindingTypeName = "BC.Rendering.EntityMaterialSlotBinding";
        private const string EntityMaterialDatasetServiceTypeName = "BC.Managers.EntityMaterialDatasetServiceMB";

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
        public void Awake_AppliesDefaultDatasetOnActivation()
        {
            Material baseMaterial = CreateMaterial("Base");
            Material defaultMaterial = CreateMaterial("Default");
            ScriptableObject materialSet = CreateMaterialSet(
                CreateDataset("Default", ("Body", defaultMaterial)));

            MeshRenderer renderer = CreateRenderer("Renderer", baseMaterial);
            GameObject controllerObject = new GameObject("Controller");
            createdObjects.Add(controllerObject);
            controllerObject.SetActive(false);

            Component controller = controllerObject.AddComponent(FindRuntimeType(EntityMaterialControllerTypeName));
            SetPrivateField(controller, "defaultMaterialSet", materialSet);
            SetPrivateField(controller, "slotBindings", BuildSlotBindingList(
                new EntityMaterialSlotBindingData("Body", renderer, 0)));
            SetPrivateField(controller, "hasTriedInitialApply", false);
            InvokeMethod(controller, "Awake");

            controllerObject.SetActive(true);

            Assert.AreSame(defaultMaterial, renderer.sharedMaterials[0]);
            Assert.AreEqual("Default", GetPropertyValue<string>(controller, "CurrentDatasetKind"));
        }

        [Test]
        public void TrySetActiveDatasetKind_AppliesToExistingAndLateControllers()
        {
            Material baseMaterial = CreateMaterial("Base");
            Material defaultMaterial = CreateMaterial("Default");
            Material darkMaterial = CreateMaterial("Dark");
            ScriptableObject materialSet = CreateMaterialSet(
                CreateDataset("Default", ("Body", defaultMaterial)),
                CreateDataset("Dark", ("Body", darkMaterial)));

            Component service = CreateService();

            MeshRenderer firstRenderer = CreateRenderer("ExistingRenderer", baseMaterial);
            Component firstController = CreateController(
                "ExistingController",
                materialSet,
                new EntityMaterialSlotBindingData("Body", firstRenderer, 0));
            InvokeMethod(service, "RegisterController", firstController);

            object[] setActiveArgs = { "Dark", null };
            bool changed = (bool)InvokeMethod(service, "TrySetActiveDatasetKind", setActiveArgs);
            Assert.IsTrue(changed, setActiveArgs[1] as string);
            Assert.AreSame(darkMaterial, firstRenderer.sharedMaterials[0]);

            MeshRenderer lateRenderer = CreateRenderer("LateRenderer", baseMaterial);
            Component lateController = CreateController(
                "LateController",
                materialSet,
                new EntityMaterialSlotBindingData("Body", lateRenderer, 0));

            InvokeMethod(service, "RegisterController", lateController);

            Assert.AreSame(darkMaterial, lateRenderer.sharedMaterials[0]);
            Assert.AreEqual("Dark", GetPropertyValue<string>(lateController, "CurrentDatasetKind"));
        }

        private Component CreateService()
        {
            GameObject gameObject = new GameObject("EntityMaterialDatasetService");
            createdObjects.Add(gameObject);
            return gameObject.AddComponent(FindRuntimeType(EntityMaterialDatasetServiceTypeName));
        }

        private MeshRenderer CreateRenderer(string name, Material material)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { material };
            return renderer;
        }

        private Component CreateController(
            string name,
            ScriptableObject materialSet,
            EntityMaterialSlotBindingData binding)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            gameObject.SetActive(false);

            Component controller = gameObject.AddComponent(FindRuntimeType(EntityMaterialControllerTypeName));
            SetPrivateField(controller, "defaultMaterialSet", materialSet);
            SetPrivateField(controller, "slotBindings", BuildSlotBindingList(binding));
            SetPrivateField(controller, "hasTriedInitialApply", false);
            InvokeMethod(controller, "Awake");

            gameObject.SetActive(true);
            return controller;
        }

        private ScriptableObject CreateMaterialSet(params object[] variants)
        {
            Type setType = FindRuntimeType(EntityMaterialSetSoTypeName);
            ScriptableObject materialSet = ScriptableObject.CreateInstance(setType);
            materialSet.name = "EntityMaterialSet";
            createdObjects.Add(materialSet);

            Type variantType = FindRuntimeType(EntityMaterialDatasetVariantTypeName);
            Type variantListType = typeof(List<>).MakeGenericType(variantType);
            IList variantList = (IList)Activator.CreateInstance(variantListType);
            for (int i = 0; i < variants.Length; i++)
                variantList.Add(variants[i]);

            SetPrivateField(materialSet, "datasetVariants", variantList);
            return materialSet;
        }

        private object CreateDataset(string datasetKind, params (string slotKey, Material material)[] materials)
        {
            Type slotEntryType = FindRuntimeType(EntityMaterialSlotMaterialEntryTypeName);
            Type slotEntryListType = typeof(List<>).MakeGenericType(slotEntryType);
            IList slotEntries = (IList)Activator.CreateInstance(slotEntryListType);
            for (int i = 0; i < materials.Length; i++)
            {
                object entry = Activator.CreateInstance(slotEntryType, materials[i].slotKey, materials[i].material);
                slotEntries.Add(entry);
            }

            return Activator.CreateInstance(
                FindRuntimeType(EntityMaterialDatasetVariantTypeName),
                datasetKind,
                slotEntries);
        }

        private object BuildSlotBindingList(params EntityMaterialSlotBindingData[] bindings)
        {
            Type bindingType = FindRuntimeType(EntityMaterialSlotBindingTypeName);
            Type bindingListType = typeof(List<>).MakeGenericType(bindingType);
            IList bindingList = (IList)Activator.CreateInstance(bindingListType);
            for (int i = 0; i < bindings.Length; i++)
            {
                object binding = Activator.CreateInstance(
                    bindingType,
                    bindings[i].SlotKey,
                    bindings[i].Renderer,
                    bindings[i].MaterialIndex);
                bindingList.Add(binding);
            }

            return bindingList;
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

        private static object InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, args);
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

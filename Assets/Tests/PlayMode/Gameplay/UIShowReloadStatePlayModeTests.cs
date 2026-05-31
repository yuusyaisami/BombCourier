using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class UIShowReloadStatePlayModeTests
    {
        private const string InputManagerTypeName = "BC.Manager.InputManagerMB";
        private const string InputPromptIconDatabaseTypeName = "BC.Inputs.InputPromptIconDatabaseSO";
        private const string InputPromptDeviceKindTypeName = "BC.Inputs.InputPromptDeviceKind";
        private const string UIShowReloadStateTypeName = "BC.UI.UIShowReloadStateMB";

        private readonly List<UnityEngine.Object> createdObjects = new();
        private readonly List<InputAction> createdActions = new();

        [TearDown]
        public void TearDown()
        {
            ClearInputManagerInstance();

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

        [Test]
        public void RefreshPromptIcon_UsesPromptManagerFallbackForCurrentDevice()
        {
            ClearInputManagerInstance();

            Sprite keyboardSprite = CreateSprite(Color.red);
            Sprite gamepadSprite = CreateSprite(Color.blue);
            Component inputManager = CreateInputManager(keyboardSprite, gamepadSprite);
            (Component reloadState, Image iconImage) = CreateReloadStateFixture();

            SetPrivateField(inputManager, "lastUsedPromptDeviceKind", CreatePromptDeviceKindValue(1));
            InvokeMethod(reloadState, "RefreshPromptIcon", true);

            Assert.AreSame(keyboardSprite, iconImage.sprite);
            Assert.IsTrue(iconImage.enabled);

            SetPrivateField(inputManager, "lastUsedPromptDeviceKind", CreatePromptDeviceKindValue(2));
            InvokeMethod(reloadState, "RefreshPromptIcon", true);

            Assert.AreSame(gamepadSprite, iconImage.sprite);
            Assert.IsTrue(iconImage.enabled);
        }

        [Test]
        public void RefreshPromptIcon_UsesConfiguredFallbackWhenInputManagerMissing()
        {
            ClearInputManagerInstance();

            Sprite fallbackSprite = CreateSprite(Color.green);
            (Component reloadState, Image iconImage) = CreateReloadStateFixture();
            SetPrivateField(reloadState, "fallbackActionIcon", fallbackSprite);

            InvokeMethod(reloadState, "RefreshPromptIcon", true);

            Assert.AreSame(fallbackSprite, iconImage.sprite);
            Assert.IsTrue(iconImage.enabled);
        }

        private Component CreateInputManager(Sprite keyboardSprite, Sprite gamepadSprite)
        {
            GameObject inputManagerObject = new GameObject("InputManager");
            createdObjects.Add(inputManagerObject);

            Component inputManager = inputManagerObject.AddComponent(FindRuntimeType(InputManagerTypeName));
            ScriptableObject promptIconDatabase = ScriptableObject.CreateInstance(FindRuntimeType(InputPromptIconDatabaseTypeName));
            createdObjects.Add(promptIconDatabase);

            SetPrivateField(promptIconDatabase, "fallbackKeyboardMouseIcon", keyboardSprite);
            SetPrivateField(promptIconDatabase, "fallbackGamepadIcon", gamepadSprite);
            SetPrivateField(inputManager, "promptIconDatabase", promptIconDatabase);
            return inputManager;
        }

        private (Component ReloadState, Image IconImage) CreateReloadStateFixture()
        {
            GameObject root = new GameObject("UIShowReloadStateRoot");
            createdObjects.Add(root);

            GameObject iconObject = new GameObject("ActionIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(root.transform, false);
            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.enabled = false;

            Component reloadState = root.AddComponent(FindRuntimeType(UIShowReloadStateTypeName));

            InputAction action = new InputAction("Reset", InputActionType.Button, "<Keyboard>/r");
            createdActions.Add(action);
            InputActionReference actionReference = InputActionReference.Create(action);
            createdObjects.Add(actionReference);

            SetPrivateField(reloadState, "actionIconImage", iconImage);
            SetPrivateField(reloadState, "reloadInputActionReference", actionReference);
            return (reloadState, iconImage);
        }

        private Sprite CreateSprite(Color color)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.SetPixel(1, 0, color);
            texture.SetPixel(0, 1, color);
            texture.SetPixel(1, 1, color);
            texture.Apply();
            createdObjects.Add(texture);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            createdObjects.Add(sprite);
            return sprite;
        }

        private static void ClearInputManagerInstance()
        {
            Type inputManagerType = FindRuntimeType(InputManagerTypeName);
            PropertyInfo instanceProperty = inputManagerType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            object instance = instanceProperty?.GetValue(null);
            if (instance is Component inputManagerComponent)
                UnityEngine.Object.DestroyImmediate(inputManagerComponent.gameObject);

            SetPrivateStaticField(inputManagerType, "<Instance>k__BackingField", null);
        }

        private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static void SetPrivateStaticField(System.Type targetType, string fieldName, object value)
        {
            FieldInfo field = targetType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected static private field on {targetType.Name}: {fieldName}");
            field.SetValue(null, value);
        }

        private static object CreatePromptDeviceKindValue(int rawValue)
        {
            return System.Enum.ToObject(FindRuntimeType(InputPromptDeviceKindTypeName), rawValue);
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

        private static void InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            method.Invoke(target, args);
        }
    }
}

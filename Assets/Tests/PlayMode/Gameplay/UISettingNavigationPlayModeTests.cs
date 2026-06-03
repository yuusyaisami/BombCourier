using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class UISettingNavigationPlayModeTests
    {
        private const string UISettingTypeName = "BC.UI.UISettingMB";
        private const string UINoiseOutlineTypeName = "BC.UI.Effect.UINoiseOutlineMB";
        private const string DropdownBridgeTypeName = "BC.UI.UITMPDropdownNavigationBridgeMB";

        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                GameObject createdObject = createdObjects[i];
                if (createdObject != null)
                    UnityEngine.Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();

            GameObject inputManagerObject = GameObject.Find("InputManagerMB");
            if (inputManagerObject != null)
                UnityEngine.Object.DestroyImmediate(inputManagerObject);
        }

        [UnityTest]
        public IEnumerator UISetting_UsesCameraSensitivityAsInitialSelection_AndTargetsSliderHandle()
        {
            EnsureEventSystem();

            GameObject canvasObject = CreateUiObject("CanvasRoot");
            canvasObject.AddComponent<Canvas>();

            GameObject settingObject = CreateUiObject("SettingPanel");
            settingObject.transform.SetParent(canvasObject.transform, false);
            CanvasGroup canvasGroup = settingObject.AddComponent<CanvasGroup>();

            Slider cameraSlider = CreateSlider("CameraSensitivity");
            cameraSlider.transform.SetParent(settingObject.transform, false);

            Component setting = settingObject.AddComponent(FindRuntimeType(UISettingTypeName));
            SetPrivateField(setting, "canvasGroup", canvasGroup);
            SetPrivateField(setting, "cameraSensitivitySlider", cameraSlider);
            SetPrivateField(setting, "fadeDuration", 0f);
            SetPrivateField(setting, "pauseTimeScaleOnOpen", false);

            InvokeMethod(setting, "EnsureSettingFocusWiring");
            InvokeMethod(setting, "ConfigureSettingNavigation");
            InvokeMethod(setting, "EnsureInitialSelectionConfigured");
            InvokeMethod(setting, "ShowPanelAsync");

            yield return null;

            Component noiseOutline = cameraSlider.GetComponent(FindRuntimeType(UINoiseOutlineTypeName));
            Assert.IsNotNull(noiseOutline, "Camera sensitivity slider should receive a runtime noise outline component.");
            Assert.AreSame(
                cameraSlider.handleRect,
                GetPublicProperty<object>(noiseOutline, "TargetRect"),
                "Slider noise outline must target the handle rect.");

            Assert.AreSame(cameraSlider.gameObject, EventSystem.current.currentSelectedGameObject);
        }

        [UnityTest]
        public IEnumerator TMPDropdownBridge_SelectsCurrentItemOnOpen_AndRestoresRootOnClose()
        {
            EnsureEventSystem();

            GameObject dropdownObject = CreateUiObject("Dropdown");
            dropdownObject.AddComponent<Image>();
            TMP_Dropdown dropdown = dropdownObject.AddComponent<TMP_Dropdown>();
            Component bridge = dropdownObject.AddComponent(FindRuntimeType(DropdownBridgeTypeName));

            InvokeMethod(bridge, "Awake");
            InvokeMethod(bridge, "OnEnable");

            GameObject runtimeList = CreateUiObject("RuntimeDropdownList");
            Toggle first = CreateDropdownToggle("Item0", runtimeList.transform);
            Toggle second = CreateDropdownToggle("Item1", runtimeList.transform);
            Toggle third = CreateDropdownToggle("Item2", runtimeList.transform);

            dropdown.value = 1;
            SetPrivateField(dropdown, "m_Dropdown", runtimeList);

            InvokeMethod(bridge, "Update");
            yield return null;

            Assert.AreSame(second.gameObject, EventSystem.current.currentSelectedGameObject);
            Assert.AreSame(first, second.navigation.selectOnUp);
            Assert.AreSame(third, second.navigation.selectOnDown);
            Assert.IsNull(second.navigation.selectOnLeft);
            Assert.IsNull(second.navigation.selectOnRight);

            SetPrivateField(dropdown, "m_Dropdown", null);

            InvokeMethod(bridge, "Update");
            yield return null;

            Assert.AreSame(dropdown.gameObject, EventSystem.current.currentSelectedGameObject);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            createdObjects.Add(eventSystemObject);
        }

        private GameObject CreateUiObject(string name)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private Slider CreateSlider(string name)
        {
            GameObject sliderObject = CreateUiObject(name);
            sliderObject.AddComponent<Image>();
            Slider slider = sliderObject.AddComponent<Slider>();

            GameObject handleObject = CreateUiObject("Handle");
            handleObject.transform.SetParent(sliderObject.transform, false);
            Image handleImage = handleObject.AddComponent<Image>();

            slider.handleRect = handleObject.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            return slider;
        }

        private Toggle CreateDropdownToggle(string name, Transform parent)
        {
            GameObject toggleObject = CreateUiObject(name);
            toggleObject.transform.SetParent(parent, false);
            Image image = toggleObject.AddComponent<Image>();
            Toggle toggle = toggleObject.AddComponent<Toggle>();
            toggle.targetGraphic = image;
            toggle.graphic = image;
            return toggle;
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

        private static object InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, args);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static T GetPublicProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(property, $"Expected public property on {target.GetType().Name}: {propertyName}");
            return (T)property.GetValue(target);
        }
    }
}

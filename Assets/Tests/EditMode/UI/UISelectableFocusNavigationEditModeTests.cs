using System.Collections.Generic;
using System.Reflection;
using BC.UI;
using BC.UI.Components;
using BC.UI.Effect;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BC.Editor.Tests
{
    public sealed class UISelectableFocusNavigationEditModeTests
    {
        private readonly List<Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                    Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void UINoiseOutline_WithoutTarget_UsesOwnRectTransform()
        {
            GameObject owner = CreateUiObject("Owner");
            UINoiseOutlineMB outline = owner.AddComponent<UINoiseOutlineMB>();

            InvokePrivateMethod(outline, "Awake");

            Transform outlineTransform = owner.transform.Find("[NoiseOutline]");
            Assert.IsNotNull(outlineTransform);
            Assert.AreSame(owner.transform, outlineTransform.parent);
        }

        [Test]
        public void UINoiseOutline_WithExplicitTarget_ParentsOutlineUnderTarget()
        {
            GameObject owner = CreateUiObject("Owner");
            GameObject targetObject = CreateUiObject("Target");
            targetObject.transform.SetParent(owner.transform, false);

            UINoiseOutlineMB outline = owner.AddComponent<UINoiseOutlineMB>();
            outline.SetTargetRect(targetObject.GetComponent<RectTransform>());

            Transform outlineTransform = targetObject.transform.Find("[NoiseOutline]");
            Assert.IsNotNull(outlineTransform);
            Assert.AreSame(targetObject.transform, outlineTransform.parent);
        }

        [Test]
        public void UISelectableFocus_IsSelectionTarget_ReturnsTrueForChildObject()
        {
            GameObject sliderObject = CreateSliderObject("Slider");
            UISelectableFocusMB focus = sliderObject.AddComponent<UISelectableFocusMB>();

            GameObject childObject = CreateUiObject("ChildSelection");
            childObject.transform.SetParent(sliderObject.transform, false);

            Assert.IsTrue(focus.IsSelectionTarget(childObject));
        }

        [Test]
        public void UISelectableFocus_OnSelectAndDeselect_TogglesNoiseFocus()
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            createdObjects.Add(eventSystemObject);
            EventSystem eventSystem = eventSystemObject.AddComponent<EventSystem>();

            GameObject toggleObject = CreateToggleObject("Toggle");
            UINoiseOutlineMB outline = toggleObject.AddComponent<UINoiseOutlineMB>();
            InvokePrivateMethod(outline, "Awake");

            UISelectableFocusMB focus = toggleObject.AddComponent<UISelectableFocusMB>();

            focus.OnSelect(new BaseEventData(eventSystem));
            Assert.IsTrue(focus.IsFocused);
            Assert.IsTrue(outline.IsFocused);

            focus.OnDeselect(new BaseEventData(eventSystem));
            Assert.IsFalse(focus.IsFocused);
            Assert.IsFalse(outline.IsFocused);
        }

        [Test]
        public void UISelectableNavigationMap_Apply_PreservesUnspecifiedDirections()
        {
            GameObject mapObject = CreateUiObject("NavigationMap");
            UISelectableNavigationMapMB map = mapObject.AddComponent<UISelectableNavigationMapMB>();

            Button center = CreateButton("Center");
            Button up = CreateButton("Up");
            Button down = CreateButton("Down");
            Button left = CreateButton("Left");
            Button right = CreateButton("Right");

            Navigation original = center.navigation;
            original.mode = Navigation.Mode.Explicit;
            original.selectOnLeft = left;
            original.selectOnRight = right;
            center.navigation = original;

            map.SetEntries(new[]
            {
                new UISelectableNavigationMapMB.NavigationEntry
                {
                    selectable = center,
                    up = up,
                    down = down,
                }
            });

            map.Apply();

            Navigation applied = center.navigation;
            Assert.AreEqual(Navigation.Mode.Explicit, applied.mode);
            Assert.AreSame(up, applied.selectOnUp);
            Assert.AreSame(down, applied.selectOnDown);
            Assert.AreSame(left, applied.selectOnLeft);
            Assert.AreSame(right, applied.selectOnRight);
        }

        private GameObject CreateUiObject(string name)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private GameObject CreateSliderObject(string name)
        {
            GameObject sliderObject = CreateUiObject(name);
            sliderObject.AddComponent<Image>();
            Slider slider = sliderObject.AddComponent<Slider>();

            GameObject handleObject = CreateUiObject("Handle");
            handleObject.transform.SetParent(sliderObject.transform, false);
            Image handleImage = handleObject.AddComponent<Image>();

            slider.handleRect = handleObject.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            return sliderObject;
        }

        private GameObject CreateToggleObject(string name)
        {
            GameObject toggleObject = CreateUiObject(name);
            Image image = toggleObject.AddComponent<Image>();
            Toggle toggle = toggleObject.AddComponent<Toggle>();
            toggle.targetGraphic = image;
            toggle.graphic = image;
            return toggleObject;
        }

        private Button CreateButton(string name)
        {
            GameObject gameObject = CreateUiObject(name);
            gameObject.AddComponent<Image>();
            return gameObject.AddComponent<Button>();
        }

        private static object InvokePrivateMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected private method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, null);
        }
    }
}

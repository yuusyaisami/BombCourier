using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class UIScreenTransitionImagePlayModeTests
    {
        private const string UIScreenTransitionImageTypeName = "BC.UI.Effect.UIScreenTransitionImageMB";

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
        public void SetImmediateSprite_UsesPlainRawImageState()
        {
            (Component transitionImage, RawImage rawImage) = CreateTransitionImage("Immediate");
            Sprite sprite = CreateSprite(64, 64, new Rect(16f, 8f, 32f, 24f));

            bool result = (bool)InvokeMethod(transitionImage, "SetImmediateSprite", sprite);

            Assert.IsTrue(result, "SetImmediateSprite should succeed for a valid sprite.");
            Assert.AreSame(sprite.texture, rawImage.texture, "Idle state should render directly from the sprite texture.");
            Assert.IsNull(rawImage.material, "Idle state should not keep the transition material bound.");
            Assert.IsFalse(rawImage.raycastTarget, "Transition image must not block UI raycasts.");
            AssertRectApproximately(new Rect(0.25f, 0.125f, 0.5f, 0.375f), rawImage.uvRect);
            Assert.AreSame(sprite, GetPropertyValue<Sprite>(transitionImage, "CurrentSprite"), "CurrentSprite should match the displayed sprite.");
        }

        [Test]
        public void ZeroDurationTransition_CompletesWithoutLeavingTransitionMaterialBound()
        {
            (Component transitionImage, RawImage rawImage) = CreateTransitionImage("ZeroDuration");
            Sprite fromSprite = CreateSprite(64, 64, new Rect(0f, 0f, 32f, 32f));
            Sprite toSprite = CreateSprite(64, 64, new Rect(8f, 16f, 24f, 32f));

            bool initialSetResult = (bool)InvokeMethod(transitionImage, "SetImmediateSprite", fromSprite);
            Assert.IsTrue(initialSetResult, "Initial sprite setup should succeed.");

            InvokeMethod(transitionImage, "TransitionToSpriteAsync", toSprite, 0f, null, default(System.Threading.CancellationToken));

            Assert.AreSame(toSprite, GetPropertyValue<Sprite>(transitionImage, "CurrentSprite"), "Zero-duration transition should commit the target sprite immediately.");
            Assert.AreSame(toSprite.texture, rawImage.texture, "Final raw image texture should match the target sprite.");
            Assert.IsNull(rawImage.material, "Transition material should be released after the transition completes.");
            AssertRectApproximately(new Rect(0.125f, 0.25f, 0.375f, 0.5f), rawImage.uvRect);
        }

        private (Component TransitionImage, RawImage RawImage) CreateTransitionImage(string name)
        {
            GameObject root = new GameObject(name);
            createdObjects.Add(root);

            RectTransform rectTransform = root.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(800f, 450f);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            RawImage rawImage = root.AddComponent<RawImage>();
            Type componentType = FindRuntimeType(UIScreenTransitionImageTypeName);
            Component transitionImage = root.AddComponent(componentType);
            return (transitionImage, rawImage);
        }

        private Sprite CreateSprite(int textureWidth, int textureHeight, Rect spriteRect)
        {
            Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            createdObjects.Add(texture);

            Sprite sprite = Sprite.Create(texture, spriteRect, new Vector2(0.5f, 0.5f), 100f);
            createdObjects.Add(sprite);
            return sprite;
        }

        private static void AssertRectApproximately(Rect expected, Rect actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.width, Is.EqualTo(expected.width).Within(0.0001f));
            Assert.That(actual.height, Is.EqualTo(expected.height).Within(0.0001f));
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

        private static object InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, args);
        }
    }
}

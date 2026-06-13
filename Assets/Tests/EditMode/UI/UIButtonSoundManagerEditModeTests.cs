#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using BC.Audio;
using BC.Managers;
using BC.UI.Components;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BC.Editor.Tests
{
    public sealed class UIButtonSoundManagerEditModeTests
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
        public void ResolveUIButtonClickSound_UsesOverrideBeforeDefault()
        {
            GameSoundDataManagerMB manager = CreateSoundManager();
            AudioDataSO defaultSound = CreateAudioData();
            AudioDataSO overrideSound = CreateAudioData();
            UIButtonMB button = CreateButton();

            SetPrivateField(manager, "defaultUIButtonClickSound", defaultSound);
            SetPrivateField(button, "overrideClickSound", overrideSound);

            Assert.AreSame(overrideSound, manager.ResolveUIButtonClickSound(button));
        }

        [Test]
        public void ResolveUIButtonFocusSound_FallsBackToDefaultWhenOverrideIsMissing()
        {
            GameSoundDataManagerMB manager = CreateSoundManager();
            AudioDataSO defaultSound = CreateAudioData();
            UIButtonMB button = CreateButton();

            SetPrivateField(manager, "defaultUIButtonFocusSound", defaultSound);

            Assert.AreSame(defaultSound, manager.ResolveUIButtonFocusSound(button));
        }

        [Test]
        public void ResolveUIButtonClickSound_ReturnsNullWhenNoSoundIsConfigured()
        {
            GameSoundDataManagerMB manager = CreateSoundManager();
            UIButtonMB button = CreateButton();

            Assert.IsNull(manager.ResolveUIButtonClickSound(button));
        }

        private GameSoundDataManagerMB CreateSoundManager()
        {
            GameObject gameObject = new GameObject("GameSoundDataManager");
            createdObjects.Add(gameObject);

            GameSoundDataManagerMB manager = gameObject.AddComponent<GameSoundDataManagerMB>();
            InvokeMethod(manager, "Awake");
            return manager;
        }

        private UIButtonMB CreateButton()
        {
            GameObject gameObject = new GameObject("UIButton");
            createdObjects.Add(gameObject);

            gameObject.AddComponent<RectTransform>();
            gameObject.AddComponent<Image>();
            gameObject.AddComponent<Button>();
            return gameObject.AddComponent<UIButtonMB>();
        }

        private AudioDataSO CreateAudioData()
        {
            AudioDataSO data = ScriptableObject.CreateInstance<AudioDataSO>();
            createdObjects.Add(data);
            return data;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static object InvokeMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, null);
        }
    }
}
#endif

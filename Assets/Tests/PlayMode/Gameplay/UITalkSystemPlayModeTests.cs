using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class UITalkSystemPlayModeTests
    {
        private const string UITalkSystemTypeName = "BC.UI.UITalkSystemMB";
        private const string AudioDataSoTypeName = "BC.Audio.AudioDataSO";

        private readonly List<GameObject> createdObjects = new();
        private readonly List<ScriptableObject> createdScriptableObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdScriptableObjects.Count - 1; i >= 0; i--)
            {
                ScriptableObject scriptableObject = createdScriptableObjects[i];
                if (scriptableObject != null)
                    UnityEngine.Object.DestroyImmediate(scriptableObject);
            }

            createdScriptableObjects.Clear();

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                GameObject createdObject = createdObjects[i];
                if (createdObject != null)
                    UnityEngine.Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        [Test]
        public void ConsumeAdvancePressed_DuringTyping_CompletesLineAndRequiresFreshPress()
        {
            Component talkUi = CreateTalkUi("TalkUI");

            SetPrivateField(talkUi, "bodyTextCompleted", false);
            SetPrivateField(talkUi, "waitForAdvanceRelease", false);

            bool shouldAdvance = (bool)InvokeMethod(talkUi, "ConsumeAdvancePressed");

            Assert.IsFalse(shouldAdvance, "Skip input during typing must not advance to the next talk entry immediately.");
            Assert.IsTrue(GetPrivateField<bool>(talkUi, "bodyTextCompleted"), "Skip input should complete the current line.");
            Assert.IsTrue(GetPrivateField<bool>(talkUi, "waitForAdvanceRelease"), "Skip input should require the player to release and press again before advancing.");
        }

        [Test]
        public void ConsumeAdvancePressed_AfterLineComplete_AdvancesNormally()
        {
            Component talkUi = CreateTalkUi("TalkUIComplete");

            SetPrivateField(talkUi, "bodyTextCompleted", true);
            SetPrivateField(talkUi, "waitForAdvanceRelease", false);

            bool shouldAdvance = (bool)InvokeMethod(talkUi, "ConsumeAdvancePressed");

            Assert.IsTrue(shouldAdvance, "Completed lines should advance on the next fresh input.");
            Assert.IsFalse(GetPrivateField<bool>(talkUi, "waitForAdvanceRelease"), "Advance after completion should not inject an extra release wait.");
        }

        [Test]
        public void UseDefaultCharacterSound_AssignsSerializedDefaultAudioDataSo()
        {
            Component talkUi = CreateTalkUi("TalkUIDefaultAudio");
            ScriptableObject defaultAudioDataSo = ScriptableObject.CreateInstance(FindRuntimeType(AudioDataSoTypeName));
            createdScriptableObjects.Add(defaultAudioDataSo);

            SetPrivateField(talkUi, "defaultAudioDataSO", defaultAudioDataSo);

            InvokeMethod(talkUi, "UseDefaultCharacterSound");

            Assert.AreSame(defaultAudioDataSo, GetPrivateField<object>(talkUi, "currentTalkCharacterSound"));
        }

        private Component CreateTalkUi(string name)
        {
            GameObject root = new GameObject(name);
            createdObjects.Add(root);
            return root.AddComponent(FindRuntimeType(UITalkSystemTypeName));
        }

        private static object InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, args);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            return (T)field.GetValue(target);
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
    }
}

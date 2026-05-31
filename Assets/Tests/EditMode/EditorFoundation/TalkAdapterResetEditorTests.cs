using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BC.Editor.Tests
{
    public sealed class TalkAdapterResetEditorTests
    {
        private const string TalkAdapterTypeName = "BC.Managers.TalkAdapterMB";
        private const string EntityAnimationTypeName = "BC.Animation.EntityAnimationMB";
        private const string HideTalkRequestDataTypeName = "BC.Managers.HideTalkRequestData";
        private const string TalkStatePresentationEntryTypeName = "BC.Managers.TalkStatePresentationEntry";
        private const string TalkAnimatorParameterWriteTypeName = "BC.Managers.TalkAnimatorParameterWrite";
        private const string EntityAnimatorParameterWriteModeTypeName = "BC.Animation.EntityAnimatorParameterWriteMode";

        private readonly List<UnityEngine.Object> createdObjects = new();
        private readonly List<string> createdAssetPaths = new();

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

            for (int i = createdAssetPaths.Count - 1; i >= 0; i--)
            {
                string assetPath = createdAssetPaths[i];
                if (!string.IsNullOrEmpty(assetPath))
                    AssetDatabase.DeleteAsset(assetPath);
            }

            createdAssetPaths.Clear();
            AssetDatabase.Refresh();
        }

        [Test]
        public void HandleTalkHidden_ResetsAllConfiguredTalkAnimatorParameters()
        {
            GameObject root = new GameObject("TalkAdapterResetTest");
            createdObjects.Add(root);

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = CreateAnimatorControllerAsset();

            Component entityAnimation = root.AddComponent(FindRuntimeType(EntityAnimationTypeName));
            Component talkAdapter = root.AddComponent(FindRuntimeType(TalkAdapterTypeName));

            InvokeMethod(entityAnimation, "Awake");

            SetPrivateField(talkAdapter, "entityAnimation", entityAnimation);
            SetPrivateField(talkAdapter, "statePresentations", CreateStatePresentationsArray());

            animator.SetBool("PoseHold", true);
            animator.SetInteger("PoseIndex", 3);

            object requestData = GetStaticPropertyValue(FindRuntimeType(HideTalkRequestDataTypeName), "Default");
            InvokeMethod(talkAdapter, "HandleTalkHidden", requestData);

            Assert.IsFalse(animator.GetBool("PoseHold"), "Auto-reset talk bool parameters should be explicitly cleared when the conversation ends.");
            Assert.AreEqual(1, animator.GetInteger("PoseIndex"), "Manual reset talk int parameters should be explicitly restored when the conversation ends.");
        }

        private RuntimeAnimatorController CreateAnimatorControllerAsset()
        {
            string assetPath = $"Assets/Tests/EditMode/EditorFoundation/__TalkAdapterReset_{Guid.NewGuid():N}.controller";
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(assetPath);
            createdAssetPaths.Add(assetPath);

            controller.AddParameter("PoseHold", AnimatorControllerParameterType.Bool);
            controller.AddParameter("PoseIndex", AnimatorControllerParameterType.Int);
            return controller;
        }

        private Array CreateStatePresentationsArray()
        {
            Type entryType = FindRuntimeType(TalkStatePresentationEntryTypeName);
            Type writeType = FindRuntimeType(TalkAnimatorParameterWriteTypeName);
            Type writeModeType = FindRuntimeType(EntityAnimatorParameterWriteModeTypeName);

            Array entries = Array.CreateInstance(entryType, 2);

            object boolEntry = Activator.CreateInstance(entryType);
            Array boolWrites = Array.CreateInstance(writeType, 1);
            object boolWrite = Activator.CreateInstance(writeType);
            SetFieldValue(boolWrite, "writeMode", Enum.Parse(writeModeType, "SetBool"));
            SetFieldValue(boolWrite, "parameterName", "PoseHold");
            SetFieldValue(boolWrite, "applyAutoReset", true);
            SetFieldValue(boolWrite, "boolValue", true);
            boolWrites.SetValue(boolWrite, 0);
            SetFieldValue(boolEntry, "parameterWrites", boolWrites);
            entries.SetValue(boolEntry, 0);

            object intEntry = Activator.CreateInstance(entryType);
            Array intWrites = Array.CreateInstance(writeType, 1);
            object intWrite = Activator.CreateInstance(writeType);
            SetFieldValue(intWrite, "writeMode", Enum.Parse(writeModeType, "SetInteger"));
            SetFieldValue(intWrite, "parameterName", "PoseIndex");
            SetFieldValue(intWrite, "applyAutoReset", false);
            SetFieldValue(intWrite, "intValue", 3);
            SetFieldValue(intWrite, "resetIntValue", 1);
            intWrites.SetValue(intWrite, 0);
            SetFieldValue(intEntry, "parameterWrites", intWrites);
            entries.SetValue(intEntry, 1);

            return entries;
        }

        private static object GetStaticPropertyValue(Type targetType, string propertyName)
        {
            PropertyInfo property = targetType.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected static property on {targetType.FullName}: {propertyName}");
            return property.GetValue(null);
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

        private static void SetFieldValue(object target, string fieldName, object value)
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
    }
}

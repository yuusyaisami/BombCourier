using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BC.Gameplay.PlayModeTests
{
    public sealed class TalkSystemPlayModeTests
    {
        private const string TalkSystemManagerTypeName = "BC.Managers.TalkSystemManagerMB";
        private const string TalkAdapterTypeName = "BC.Managers.TalkAdapterMB";
        private const string HideTalkRequestDataTypeName = "BC.Managers.HideTalkRequestData";
        private const string EntityMBTypeName = "BC.Base.EntityMB";
        private const string EntityRefTypeName = "BC.Base.EntityRef";

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
        }

        [Test]
        public void TryHideTalkAsync_AllowsOwnerActorToCloseRedirectedConversation()
        {
            Component manager = CreateTalkSystemManager();
            (_, Component ownerAdapter, object ownerEntity) = CreateTalkActor("Owner", 1u);
            (_, Component speakerAdapter, object speakerEntity) = CreateTalkActor("Speaker", 2u);

            SetPrivateField(manager, "activeTalkOwnerActor", ownerEntity);
            SetPrivateField(manager, "activeTalkPresentationActor", speakerEntity);
            SetPrivateField(manager, "activeTalkPresentationAdapter", speakerAdapter);
            SetPrivateField(speakerAdapter, "hasCurrentTalkStatePresentation", true);
            SetPrivateField(speakerAdapter, "isTalkingActivityActive", true);

            LogAssert.Expect(LogType.Warning, "TalkAdapterMB: Entity value store is not available, so face expression updates will be skipped.");

            object requestData = GetStaticPropertyValue(FindRuntimeType(HideTalkRequestDataTypeName), "Default");
            bool result = AwaitUniTaskBool(InvokeMethod(ownerAdapter, "TryHideTalkAsync", requestData, default(CancellationToken)));

            Assert.IsTrue(result, "Owner actor should be able to close a conversation even when the presentation adapter is different.");
            Assert.IsFalse(GetPrivateField<bool>(speakerAdapter, "hasCurrentTalkStatePresentation"), "Presentation adapter should clear its talk state when the owner hides the talk.");
            Assert.IsFalse(GetPrivateField<bool>(speakerAdapter, "isTalkingActivityActive"), "Presentation adapter should stop talking activity when the owner hides the talk.");
            Assert.IsFalse(IsValidEntityRef(GetPrivateField(manager, "activeTalkOwnerActor")), "Conversation owner should be cleared after HideTalk succeeds.");
            Assert.IsFalse(IsValidEntityRef(GetPrivateField(manager, "activeTalkPresentationActor")), "Presentation actor should be cleared after HideTalk succeeds.");
        }

        [Test]
        public void NotifyTalkTypingCompleted_StopsPresentationAdapterOnly()
        {
            Component manager = CreateTalkSystemManager();
            (_, Component ownerAdapter, object ownerEntity) = CreateTalkActor("Owner", 11u);
            (_, Component speakerAdapter, object speakerEntity) = CreateTalkActor("Speaker", 12u);

            SetPrivateField(manager, "activeTalkOwnerActor", ownerEntity);
            SetPrivateField(manager, "activeTalkPresentationActor", speakerEntity);
            SetPrivateField(manager, "activeTalkPresentationAdapter", speakerAdapter);
            SetPrivateField(ownerAdapter, "isTalkingActivityActive", true);
            SetPrivateField(speakerAdapter, "isTalkingActivityActive", true);

            InvokeMethod(manager, "NotifyTalkTypingCompleted");

            Assert.IsTrue(GetPrivateField<bool>(ownerAdapter, "isTalkingActivityActive"), "Owner adapter should not be modified when it is not the active presentation adapter.");
            Assert.IsFalse(GetPrivateField<bool>(speakerAdapter, "isTalkingActivityActive"), "Presentation adapter should stop talking activity when typing completes.");
        }

        private Component CreateTalkSystemManager()
        {
            GameObject root = new GameObject("TalkSystemManager");
            createdObjects.Add(root);

            Component manager = root.AddComponent(FindRuntimeType(TalkSystemManagerTypeName));
            if (GetStaticPropertyValue(FindRuntimeType(TalkSystemManagerTypeName), "Instance") == null)
                InvokeMethod(manager, "Awake");

            return manager;
        }

        private (Component EntityMB, Component TalkAdapter, object EntityRef) CreateTalkActor(string name, uint entityId)
        {
            GameObject root = new GameObject(name);
            createdObjects.Add(root);

            Component entityMB = root.AddComponent(FindRuntimeType(EntityMBTypeName));
            Component talkAdapter = root.AddComponent(FindRuntimeType(TalkAdapterTypeName));
            object entityRef = CreateEntityRef(entityId, 1);
            InvokeMethod(entityMB, "Bind", entityRef);
            return (entityMB, talkAdapter, entityRef);
        }

        private static object CreateEntityRef(uint entityId, int version)
        {
            Type entityRefType = FindRuntimeType(EntityRefTypeName);
            return Activator.CreateInstance(entityRefType, entityId, version);
        }

        private static bool AwaitUniTaskBool(object uniTask)
        {
            Assert.IsNotNull(uniTask, "Expected UniTask<bool> result.");
            object awaiter = uniTask.GetType().GetMethod("GetAwaiter", BindingFlags.Instance | BindingFlags.Public)?.Invoke(uniTask, null);
            Assert.IsNotNull(awaiter, "Expected UniTask awaiter.");

            PropertyInfo isCompletedProperty = awaiter.GetType().GetProperty("IsCompleted", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(isCompletedProperty, "Expected awaiter to expose IsCompleted.");
            Assert.IsTrue((bool)isCompletedProperty.GetValue(awaiter), "Expected HideTalk path to complete synchronously in this focused test.");

            MethodInfo getResultMethod = awaiter.GetType().GetMethod("GetResult", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(getResultMethod, "Expected awaiter to expose GetResult.");
            return (bool)getResultMethod.Invoke(awaiter, null);
        }

        private static bool IsValidEntityRef(object entityRef)
        {
            PropertyInfo isValidProperty = entityRef.GetType().GetProperty("IsValid", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(isValidProperty, "Expected EntityRef.IsValid property.");
            return (bool)isValidProperty.GetValue(entityRef);
        }

        private static object GetStaticPropertyValue(Type targetType, string propertyName)
        {
            PropertyInfo property = targetType.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected static property on {targetType.FullName}: {propertyName}");
            return property.GetValue(null);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            return field.GetValue(target);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            return (T)GetPrivateField(target, fieldName);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field on {target.GetType().Name}: {fieldName}");
            field.SetValue(target, value);
        }

        private static object InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method on {target.GetType().Name}: {methodName}");
            return method.Invoke(target, args);
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

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BC.Base.Tests
{
    public sealed class AnimationLayerWeightActionTests
    {
        private const string EntityRefTypeName = "BC.Base.EntityRef";
        private const string SceneKernelTypeName = "BC.Base.SceneKernel";
        private const string EntityTargetReferenceTypeName = "BC.Base.EntityTargetReference";
        private const string EntityAnimationTypeName = "BC.Animation.EntityAnimationMB";
        private const string LayerWeightRuntimeTypeName = "BC.ActionSystem.SetEntityAnimationLayerWeightStepRuntime";
        private const string ActionExecutionContextTypeName = "BC.ActionSystem.ActionExecutionContext";
        private const string TestLayerName = "UpperBody";

        private readonly List<UnityEngine.Object> createdObjects = new();
        private readonly List<string> assetPaths = new();

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

            for (int i = assetPaths.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrWhiteSpace(assetPaths[i]))
                    AssetDatabase.DeleteAsset(assetPaths[i]);
            }

            assetPaths.Clear();
        }

        [Test]
        public void LayerWeightRuntimeWithZeroDurationAppliesImmediately()
        {
            AnimationFixture fixture = CreateFixture(0.25f);
            object runtime = CreateRuntimeDefinition(0.75f, 0f);

            object status = InvokeTick(runtime, fixture.Context, 8);

            Assert.AreEqual("Continue", status.ToString());
            Assert.AreEqual(0.75f, fixture.Animator.GetLayerWeight(fixture.LayerIndex), 0.0001f);
        }

        [Test]
        public void LayerWeightRuntimeWithDurationRunsUntilTargetWeightIsReached()
        {
            AnimationFixture fixture = CreateFixture(0f);
            object runtime = CreateRuntimeDefinition(1f, 1f);

            object status = InvokeTick(runtime, fixture.Context, 8);

            Assert.AreEqual("Running", status.ToString());
            Assert.Greater(fixture.Animator.GetLayerWeight(fixture.LayerIndex), 0f);
            Assert.Less(fixture.Animator.GetLayerWeight(fixture.LayerIndex), 1f);

            for (int i = 0; i < 80 && string.Equals(status.ToString(), "Running", StringComparison.Ordinal); i++)
                status = InvokeTick(runtime, fixture.Context, 8);

            Assert.AreEqual("Continue", status.ToString());
            Assert.AreEqual(1f, fixture.Animator.GetLayerWeight(fixture.LayerIndex), 0.0001f);
        }

        private AnimationFixture CreateFixture(float initialLayerWeight)
        {
            GameObject root = new GameObject("LayerWeightActionActor");
            root.SetActive(false);
            createdObjects.Add(root);

            Animator animator = root.AddComponent<Animator>();
            root.AddComponent(FindRuntimeType(EntityAnimationTypeName));

            string controllerPath = $"Assets/__TempLayerWeightAction_{Guid.NewGuid():N}.controller";
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            assetPaths.Add(controllerPath);
            controller.AddLayer(TestLayerName);
            animator.runtimeAnimatorController = controller;

            root.SetActive(true);
            animator.Rebind();
            animator.Update(0f);

            int layerIndex = animator.GetLayerIndex(TestLayerName);
            Assert.GreaterOrEqual(layerIndex, 0, "Expected the temporary AnimatorController to contain the test layer.");
            animator.SetLayerWeight(layerIndex, initialLayerWeight);

            object sceneKernel = CreateInstance(SceneKernelTypeName);
            object entityComponents = GetPropertyValue(sceneKernel, "EntityComponents");
            object actions = GetPropertyValue(sceneKernel, "Actions");
            object actorEntity = CreateEntityRef(301u, 1);
            InvokeMethod(entityComponents, "Register", actorEntity, root, root.transform);

            object context = CreateInstance(
                ActionExecutionContextTypeName,
                sceneKernel,
                actions,
                actorEntity,
                Activator.CreateInstance(FindRuntimeType(EntityRefTypeName)),
                null);

            return new AnimationFixture(animator, layerIndex, context);
        }

        private static object CreateRuntimeDefinition(float weight, float duration)
        {
            object selfTarget = InvokeStaticMethod(EntityTargetReferenceTypeName, "Self");
            object definition = CreateInstance(LayerWeightRuntimeTypeName, selfTarget, TestLayerName, weight, duration);
            return InvokeMethod(definition, "CreateRuntime");
        }

        private static object CreateEntityRef(uint entityId, int version)
        {
            return CreateInstance(EntityRefTypeName, entityId, version);
        }

        private static object InvokeTick(object runtime, object actionContext, int remainingOperations)
        {
            MethodInfo method = runtime.GetType().GetMethod("Tick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Expected runtime Tick method.");
            object[] arguments = { actionContext, remainingOperations };
            return method.Invoke(runtime, arguments);
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

        private static object CreateInstance(string fullTypeName, params object[] arguments)
        {
            Type type = FindRuntimeType(fullTypeName);
            object instance = Activator.CreateInstance(type, arguments);
            Assert.IsNotNull(instance, $"Expected instance: {fullTypeName}");
            return instance;
        }

        private static object InvokeStaticMethod(string fullTypeName, string methodName, params object[] arguments)
        {
            Type type = FindRuntimeType(fullTypeName);
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected static method: {fullTypeName}.{methodName}");
            return method.Invoke(null, arguments);
        }

        private static object InvokeMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Expected method: {target.GetType().Name}.{methodName}");
            return method.Invoke(target, arguments);
        }

        private static object GetPropertyValue(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property: {target.GetType().Name}.{propertyName}");
            return property.GetValue(target);
        }

        private readonly struct AnimationFixture
        {
            public readonly Animator Animator;
            public readonly int LayerIndex;
            public readonly object Context;

            public AnimationFixture(Animator animator, int layerIndex, object context)
            {
                Animator = animator;
                LayerIndex = layerIndex;
                Context = context;
            }
        }
    }
}
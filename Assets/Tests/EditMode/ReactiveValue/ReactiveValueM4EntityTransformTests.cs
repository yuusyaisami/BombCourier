using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Base.Tests
{
    public sealed class ReactiveValueM4EntityTransformTests
    {
        [Test]
        public void ResolverResolvesTargetReferenceAndTransformVectorSources()
        {
            object sceneKernel = CreateSceneKernel();
            object resolver = CreateResolver(sceneKernel);
            GameObject actorObject = new("ReactiveM4_Actor");
            GameObject targetObject = new("ReactiveM4_Target");

            try
            {
                actorObject.transform.SetPositionAndRotation(new Vector3(1f, 0f, 1f), Quaternion.Euler(0f, 90f, 0f));
                targetObject.transform.position = new Vector3(4f, 2f, 5f);

                object actor = RegisterEntity(sceneKernel, actorObject, 9101);
                object target = RegisterEntity(sceneKernel, targetObject, 9102);
                object context = CreateEvalContext(sceneKernel, actor, CreateDefaultEntityRef());
                object targetReference = CreateTagSearchTargetReference(9102);
                object targetSpec = InvokeStatic("BC.Base.ReactiveEntityRef", "TargetReference", targetReference);

                AssertSuccessfulResult(InvokeResolve(resolver, "ResolveEntity", context, targetSpec), target);
                AssertSuccessfulVector3Result(
                    InvokeResolve(resolver, "ResolveVector3", context, InvokeStatic("BC.Base.ReactiveVector3", "EntityTransformPosition", targetSpec)),
                    targetObject.transform.position);
                AssertSuccessfulVector3Result(
                    InvokeResolve(resolver, "ResolveVector3", context, InvokeStatic("BC.Base.ReactiveVector3", "EntityTransformForward", InvokeStatic("BC.Base.ReactiveEntityRef", "Self"))),
                    actorObject.transform.forward);
                AssertSuccessfulVector3Result(
                    InvokeResolve(resolver, "ResolveVector3", context, InvokeStatic("BC.Base.ReactiveVector3", "AddPosition", targetSpec, new Vector3(1f, -2f, 0.5f))),
                    targetObject.transform.position + new Vector3(1f, -2f, 0.5f));
                AssertSuccessfulVector3Result(
                    InvokeResolve(resolver, "ResolveVector3", context, InvokeStatic("BC.Base.ReactiveVector3", "Direction", InvokeStatic("BC.Base.ReactiveEntityRef", "Self"), targetSpec)),
                    (targetObject.transform.position - actorObject.transform.position).normalized);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actorObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void ResolverResolvesDistanceCompareAndEntityAliveSources()
        {
            object sceneKernel = CreateSceneKernel();
            object resolver = CreateResolver(sceneKernel);
            GameObject actorObject = new("ReactiveM4_DistanceActor");
            GameObject targetObject = new("ReactiveM4_DistanceTarget");

            try
            {
                actorObject.transform.position = new Vector3(0f, 0f, 0f);
                targetObject.transform.position = new Vector3(3f, 0f, 4f);

                object actor = RegisterEntity(sceneKernel, actorObject, 9201);
                RegisterEntity(sceneKernel, targetObject, 9202);
                object context = CreateEvalContext(sceneKernel, actor, CreateDefaultEntityRef());
                object selfSpec = InvokeStatic("BC.Base.ReactiveEntityRef", "Self");
                object targetSpec = InvokeStatic("BC.Base.ReactiveEntityRef", "TargetReference", CreateTagSearchTargetReference(9202));
                object distanceSpec = InvokeStatic("BC.Base.ReactiveFloat", "Distance", selfSpec, targetSpec);
                object literalFive = InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 5f);
                object literalFiveInt = InvokeStatic("BC.Base.ReactiveInt", "LiteralValue", 5);
                object compareSpec = InvokeStatic(
                    "BC.Base.ReactiveBool",
                    "CompareNumber",
                    distanceSpec,
                    literalFive,
                    GetReactiveNumberComparisonKind("Equal"),
                    0.001f,
                    GetReactiveEvaluationMode("Snapshot"));
                object mixedCompareSpec = InvokeStatic(
                    "BC.Base.ReactiveBool",
                    "CompareNumber",
                    distanceSpec,
                    literalFiveInt,
                    GetReactiveNumberComparisonKind("Equal"),
                    0.001f,
                    GetReactiveEvaluationMode("Snapshot"));
                object aliveSpec = InvokeStatic("BC.Base.ReactiveBool", "EntityAlive", selfSpec);

                AssertSuccessfulFloatResult(InvokeResolve(resolver, "ResolveFloat", context, distanceSpec), 5f);
                AssertSuccessfulResult(InvokeResolve(resolver, "ResolveBool", context, compareSpec), true);
                AssertSuccessfulResult(InvokeResolve(resolver, "ResolveBool", context, mixedCompareSpec), true);
                AssertSuccessfulResult(InvokeResolve(resolver, "ResolveBool", context, aliveSpec), true);

                UnregisterEntity(sceneKernel, actor);
                AssertSuccessfulResult(InvokeResolve(resolver, "ResolveBool", context, aliveSpec), false);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actorObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void ResolverRejectsAmbiguousTargetReferenceAndMissingTransform()
        {
            object sceneKernel = CreateSceneKernel();
            object resolver = CreateResolver(sceneKernel);
            GameObject firstObject = new("ReactiveM4_AmbiguousA");
            GameObject secondObject = new("ReactiveM4_AmbiguousB");

            try
            {
                object actor = RegisterEntity(sceneKernel, firstObject, 9301);
                RegisterEntity(sceneKernel, secondObject, 9301);
                object context = CreateEvalContext(sceneKernel, actor, CreateDefaultEntityRef());
                object ambiguousSpec = InvokeStatic("BC.Base.ReactiveEntityRef", "TargetReference", CreateTagSearchTargetReference(9301, "All"));

                AssertFailedResult(InvokeResolve(resolver, "ResolveEntity", context, ambiguousSpec), "MultipleTargetsNotAllowed");

                object missingContext = CreateEvalContext(sceneKernel, CreateEntityRef(999u, 1), CreateDefaultEntityRef());
                object missingTransformSpec = InvokeStatic("BC.Base.ReactiveVector3", "EntityTransformPosition", InvokeStatic("BC.Base.ReactiveEntityRef", "Self"));
                AssertFailedResult(InvokeResolve(resolver, "ResolveVector3", missingContext, missingTransformSpec), "TransformNotFound");

                object watchedDistance = InvokeStatic("BC.Base.ReactiveFloat", "Distance", InvokeStatic("BC.Base.ReactiveEntityRef", "Self"), InvokeStatic("BC.Base.ReactiveEntityRef", "Self"));
                SetFieldValue(watchedDistance, "evaluationMode", GetReactiveEvaluationMode("Watched"));
                AssertFailedResult(InvokeResolve(resolver, "ResolveFloatWatch", missingContext, watchedDistance), "UnsupportedEvaluationMode");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstObject);
                UnityEngine.Object.DestroyImmediate(secondObject);
            }
        }

        [Test]
        public void ContinuousVectorBindingReevaluatesTransformPosition()
        {
            object sceneKernel = CreateSceneKernel();
            object resolver = CreateResolver(sceneKernel);
            GameObject actorObject = new("ReactiveM4_ContinuousActor");

            try
            {
                actorObject.transform.position = new Vector3(2f, 1f, -3f);

                object actor = RegisterEntity(sceneKernel, actorObject, 9401);
                object context = CreateEvalContext(sceneKernel, actor, CreateDefaultEntityRef());
                object spec = InvokeStatic("BC.Base.ReactiveVector3", "EntityTransformPosition", InvokeStatic("BC.Base.ReactiveEntityRef", "Self"));
                SetFieldValue(spec, "evaluationMode", GetReactiveEvaluationMode("Continuous"));
                object binding = CreateInstance("BC.Base.ReactiveVector3Binding", resolver, context, spec);

                Assert.AreEqual(true, GetPropertyValue(binding, "IsDirty"));
                AssertSuccessfulVector3Result(InvokeMethod(binding, "Read"), actorObject.transform.position);

                actorObject.transform.position = new Vector3(8f, -2f, 6f);
                AssertSuccessfulVector3Result(InvokeMethod(binding, "Read"), actorObject.transform.position);
                Assert.AreEqual(true, GetPropertyValue(binding, "IsDirty"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actorObject);
            }
        }

        [Test]
        public void M4BindingsApplyFallbackAndRejectMalformedVectorPayloads()
        {
            object sceneKernel = CreateSceneKernel();
            object resolver = CreateResolver(sceneKernel);
            object missingContext = CreateEvalContext(sceneKernel, CreateEntityRef(777u, 1), CreateDefaultEntityRef());
            GameObject actorObject = new("ReactiveM4_MalformedActor");

            try
            {
                object fallbackSpec = InvokeStatic("BC.Base.ReactiveVector3", "EntityTransformPosition", InvokeStatic("BC.Base.ReactiveEntityRef", "Self"));
                SetFieldValue(fallbackSpec, "failurePolicy", Enum.Parse(GetTypeByFullName("BC.Base.ReactiveFailurePolicy"), "UseFallback"));
                SetFieldValue(fallbackSpec, "fallbackValue", new Vector3(9f, 8f, 7f));

                object binding = CreateInstance("BC.Base.ReactiveVector3Binding", resolver, missingContext, fallbackSpec);
                AssertSuccessfulVector3Result(InvokeMethod(binding, "Read"), new Vector3(9f, 8f, 7f));

                object actor = RegisterEntity(sceneKernel, actorObject, 9501);
                object validContext = CreateEvalContext(sceneKernel, actor, CreateDefaultEntityRef());
                object malformedSpec = InvokeStatic("BC.Base.ReactiveVector3", "EntityTransformPosition", InvokeStatic("BC.Base.ReactiveEntityRef", "Self"));
                object malformedTransformValue = GetFieldValue(malformedSpec, "transformValue");
                SetFieldValue(malformedTransformValue, "sourceKind", Enum.Parse(GetTypeByFullName("BC.Base.ReactiveTransformSourceKind"), "Forward"));
                SetFieldValue(malformedSpec, "transformValue", malformedTransformValue);
                AssertFailedResult(InvokeResolve(resolver, "ResolveVector3", validContext, malformedSpec), "UnsupportedSource");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actorObject);
            }
        }

        private static object CreateSceneKernel()
        {
            object sceneKernel = CreateInstance("BC.Base.SceneKernel");
            object registry = CreateInstance(
                "BC.Base.ScopedEntityRegistry",
                Enum.Parse(GetTypeByFullName("BC.Base.EntityLifetimeScope"), "Scene"),
                CreateInstance("BC.Base.EntityIdAllocator"));
            SetPropertyValue(sceneKernel, "EntitiesRegistry", registry);
            return sceneKernel;
        }

        private static object CreateResolver(object sceneKernel)
        {
            return CreateInstance("BC.Base.ReactiveValueResolverService", sceneKernel);
        }

        private static object CreateEvalContext(object sceneKernel, object actor, object trigger)
        {
            return CreateInstance("BC.Base.ReactiveEvalContext", sceneKernel, actor, trigger);
        }

        private static object CreateEntityRef(uint entityId, int version)
        {
            return CreateInstance("BC.Base.EntityRef", entityId, version);
        }

        private static object CreateDefaultEntityRef()
        {
            return Activator.CreateInstance(GetTypeByFullName("BC.Base.EntityRef"));
        }

        private static object CreateTagSearchTargetReference(int tagId, string selection = "First")
        {
            object target = Activator.CreateInstance(GetTypeByFullName("BC.Base.EntityTargetReference"));
            SetFieldValue(target, "mode", Enum.Parse(GetTypeByFullName("BC.Base.EntityTargetResolveMode"), "TagSearch"));
            SetFieldValue(target, "selection", Enum.Parse(GetTypeByFullName("BC.Base.EntityTargetSelection"), selection));
            SetFieldValue(target, "tag", InvokeStatic("BC.Base.EntityTagReference", "From", CreateInstance("BC.Base.EntityTagId", tagId)));
            return target;
        }

        private static object RegisterEntity(object sceneKernel, GameObject gameObject, int tagId)
        {
            object registry = GetPropertyValue(sceneKernel, "EntitiesRegistry");
            object request = CreateInstance(
                "BC.Base.EntityRegistryRequest",
                gameObject,
                gameObject.transform,
                CreateInstance("BC.Base.EntityTagId", tagId),
                Enum.Parse(GetTypeByFullName("BC.Base.EntityFlags"), "None"));
            object entity = InvokeMethod(registry, "Register", request);
            object components = GetPropertyValue(sceneKernel, "EntityComponents");
            InvokeMethod(components, "Register", entity, gameObject, gameObject.transform);
            return entity;
        }

        private static void UnregisterEntity(object sceneKernel, object entity)
        {
            object registry = GetPropertyValue(sceneKernel, "EntitiesRegistry");
            object components = GetPropertyValue(sceneKernel, "EntityComponents");
            InvokeMethod(registry, "Unregister", entity);
            InvokeMethod(components, "Unregister", entity);
        }

        private static object InvokeResolve(object resolver, string methodName, object context, object spec)
        {
            return InvokeMethod(resolver, methodName, context, spec);
        }

        private static object InvokeStatic(string fullTypeName, string methodName, params object[] arguments)
        {
            Type type = GetTypeByFullName(fullTypeName);
            MethodInfo method = FindMethod(type, methodName, BindingFlags.Public | BindingFlags.Static, arguments);
            Assert.IsNotNull(method, $"Expected static method: {fullTypeName}.{methodName}");
            return method.Invoke(null, BuildInvokeArguments(method.GetParameters(), arguments));
        }

        private static object InvokeMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(instance.GetType(), methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, arguments);
            Assert.IsNotNull(method, $"Expected method: {instance.GetType().FullName}.{methodName}");
            return method.Invoke(instance, BuildInvokeArguments(method.GetParameters(), arguments));
        }

        private static object CreateInstance(string fullTypeName, params object[] arguments)
        {
            Type type = GetTypeByFullName(fullTypeName);
            ConstructorInfo ctor = FindConstructor(type, arguments);
            Assert.IsNotNull(ctor, $"Expected constructor: {fullTypeName}");
            return ctor.Invoke(BuildInvokeArguments(ctor.GetParameters(), arguments));
        }

        private static void AssertSuccessfulResult(object result, object expectedValue)
        {
            Assert.AreEqual(true, GetFieldValue(result, "Success"));
            Assert.AreEqual(false, GetPropertyValue(result, "Failed"));
            Assert.AreEqual(expectedValue, GetFieldValue(result, "Value"));
        }

        private static void AssertSuccessfulFloatResult(object result, float expectedValue, float tolerance = 0.0001f)
        {
            Assert.AreEqual(true, GetFieldValue(result, "Success"));
            Assert.AreEqual(false, GetPropertyValue(result, "Failed"));
            Assert.That((float)GetFieldValue(result, "Value"), Is.EqualTo(expectedValue).Within(tolerance));
        }

        private static void AssertSuccessfulVector3Result(object result, Vector3 expectedValue, float tolerance = 0.0001f)
        {
            Assert.AreEqual(true, GetFieldValue(result, "Success"));
            Assert.AreEqual(false, GetPropertyValue(result, "Failed"));
            Vector3 actualValue = (Vector3)GetFieldValue(result, "Value");
            Assert.That((actualValue - expectedValue).sqrMagnitude, Is.LessThanOrEqualTo(tolerance * tolerance));
        }

        private static void AssertFailedResult(object result, string expectedErrorCodeName)
        {
            Assert.AreEqual(false, GetFieldValue(result, "Success"));
            Assert.AreEqual(true, GetPropertyValue(result, "Failed"));

            object error = GetFieldValue(result, "Error");
            Assert.AreEqual(expectedErrorCodeName, GetFieldValue(error, "Code").ToString());
        }

        private static object GetFieldValue(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field: {fieldName}");
            return field.GetValue(instance);
        }

        private static object GetPropertyValue(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property: {propertyName}");
            return property.GetValue(instance);
        }

        private static void SetFieldValue(object instance, string fieldName, object value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field: {fieldName}");
            field.SetValue(instance, value);
        }

        private static void SetPropertyValue(object instance, string propertyName, object value)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property: {propertyName}");
            property.SetValue(instance, value);
        }

        private static object GetReactiveEvaluationMode(string memberName)
        {
            return Enum.Parse(GetTypeByFullName("BC.Base.ReactiveEvaluationMode"), memberName);
        }

        private static object GetReactiveNumberComparisonKind(string memberName)
        {
            return Enum.Parse(GetTypeByFullName("BC.Base.ReactiveNumberComparisonKind"), memberName);
        }

        private static Type GetTypeByFullName(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }

            Assert.Fail($"Expected type: {fullName}");
            return null;
        }

        private static MethodInfo FindMethod(Type ownerType, string methodName, BindingFlags bindingFlags, object[] arguments)
        {
            MethodInfo[] methods = ownerType.GetMethods(bindingFlags);

            for (int index = 0; index < methods.Length; index++)
            {
                MethodInfo method = methods[index];
                if (method.Name != methodName || method.IsGenericMethodDefinition)
                    continue;

                if (CanAcceptArguments(method.GetParameters(), arguments))
                    return method;
            }

            return null;
        }

        private static ConstructorInfo FindConstructor(Type ownerType, object[] arguments)
        {
            ConstructorInfo[] constructors = ownerType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int index = 0; index < constructors.Length; index++)
            {
                ConstructorInfo ctor = constructors[index];
                if (CanAcceptArguments(ctor.GetParameters(), arguments))
                    return ctor;
            }

            return null;
        }

        private static bool CanAcceptArguments(ParameterInfo[] parameters, object[] arguments)
        {
            arguments ??= Array.Empty<object>();

            if (arguments.Length > parameters.Length)
                return false;

            for (int index = 0; index < arguments.Length; index++)
            {
                Type parameterType = GetEffectiveParameterType(parameters[index]);
                object argument = arguments[index];

                if (argument == null)
                {
                    if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) == null)
                        return false;

                    continue;
                }

                if (!parameterType.IsInstanceOfType(argument))
                    return false;
            }

            for (int index = arguments.Length; index < parameters.Length; index++)
            {
                if (!parameters[index].IsOptional)
                    return false;
            }

            return true;
        }

        private static object[] BuildInvokeArguments(ParameterInfo[] parameters, object[] arguments)
        {
            arguments ??= Array.Empty<object>();

            object[] resolved = new object[parameters.Length];

            for (int index = 0; index < parameters.Length; index++)
            {
                resolved[index] = index < arguments.Length ? arguments[index] : Type.Missing;
            }

            return resolved;
        }

        private static Type GetEffectiveParameterType(ParameterInfo parameter)
        {
            return parameter.ParameterType.IsByRef
                ? parameter.ParameterType.GetElementType()
                : parameter.ParameterType;
        }
    }
}

using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BC.Base.Tests
{
    public sealed class ReactiveValueM2RuntimeTests
    {
        [Test]
        public void ResolverResolvesLiteralValues()
        {
            object resolver = CreateResolver();
            object actor = CreateEntityRef(10u, 1);
            object trigger = CreateEntityRef(20u, 1);
            object context = CreateEvalContext(null, actor, trigger);

            AssertSuccessfulResult(InvokeResolve(resolver, "ResolveFloat", context, InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 3.5f)), 3.5f);
            AssertSuccessfulResult(InvokeResolve(resolver, "ResolveInt", context, InvokeStatic("BC.Base.ReactiveInt", "LiteralValue", 8)), 8);
            AssertSuccessfulResult(InvokeResolve(resolver, "ResolveBool", context, InvokeStatic("BC.Base.ReactiveBool", "LiteralValue", true)), true);
            AssertSuccessfulResult(
                InvokeResolve(resolver, "ResolveVector3", context, InvokeStatic("BC.Base.ReactiveVector3", "LiteralValue", new Vector3(1f, 2f, 3f))),
                new Vector3(1f, 2f, 3f));
        }

        [Test]
        public void ResolverResolvesSelfAndTriggerEntities()
        {
            object resolver = CreateResolver();
            object actor = CreateEntityRef(100u, 2);
            object trigger = CreateEntityRef(200u, 3);
            object context = CreateEvalContext(null, actor, trigger);

            AssertSuccessfulResult(InvokeResolve(resolver, "ResolveEntity", context, InvokeStatic("BC.Base.ReactiveEntityRef", "Self")), actor);
            AssertSuccessfulResult(InvokeResolve(resolver, "ResolveEntity", context, InvokeStatic("BC.Base.ReactiveEntityRef", "TriggerEntity")), trigger);
        }

        [Test]
        public void ResolverFailsForUnsupportedWatchedModeAndMissingTrigger()
        {
            object resolver = CreateResolver();
            object actor = CreateEntityRef(5u, 1);
            object missingTrigger = Activator.CreateInstance(GetTypeByFullName("BC.Base.EntityRef"));
            object context = CreateEvalContext(null, actor, missingTrigger);

            object watchedFloat = InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 1.25f);
            SetFieldValue(watchedFloat, "evaluationMode", Enum.Parse(GetTypeByFullName("BC.Base.ReactiveEvaluationMode"), "Watched"));
            AssertFailedResult(
                InvokeResolve(resolver, "ResolveFloat", context, watchedFloat),
                "UnsupportedEvaluationMode");

            AssertFailedResult(
                InvokeResolve(resolver, "ResolveEntity", context, InvokeStatic("BC.Base.ReactiveEntityRef", "TriggerEntity")),
                "TargetNotFound");

            object fallbackFloat = InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 4.5f);
            SetFieldValue(fallbackFloat, "evaluationMode", Enum.Parse(GetTypeByFullName("BC.Base.ReactiveEvaluationMode"), "Watched"));
            SetFieldValue(fallbackFloat, "failurePolicy", Enum.Parse(GetTypeByFullName("BC.Base.ReactiveFailurePolicy"), "UseFallback"));
            SetFieldValue(fallbackFloat, "fallbackValue", 9.5f);

            object fallbackBinding = CreateInstance("BC.Base.ReactiveFloatBinding", resolver, context, fallbackFloat);
            AssertSuccessfulResult(InvokeMethod(fallbackBinding, "Read"), 9.5f);
        }

        [Test]
        public void ActionScopeDisposesOwnedBindings()
        {
            object resolver = CreateResolver();
            object actor = CreateEntityRef(7u, 1);
            object trigger = CreateEntityRef(9u, 1);
            object handle = Activator.CreateInstance(GetTypeByFullName("BC.ActionSystem.ActionExecutionHandle"), 1UL, actor);
            object scope = InvokeMethod(resolver, "CreateActionScope", handle, actor, trigger);
            object binding = InvokeMethod(scope, "Bind", InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 2.75f));

            Assert.AreEqual(true, GetPropertyValue(binding, "IsValid"));
            AssertSuccessfulResult(InvokeMethod(binding, "Read"), 2.75f);

            Dispose(scope);
            Assert.AreEqual(false, GetPropertyValue(binding, "IsValid"));

            TargetInvocationException disposedRead = Assert.Throws<TargetInvocationException>(() => InvokeMethod(binding, "Read"));
            Assert.IsInstanceOf<ObjectDisposedException>(disposedRead.InnerException);

            TargetInvocationException disposedBind = Assert.Throws<TargetInvocationException>(() => InvokeMethod(scope, "Bind", InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 1.0f)));
            Assert.IsInstanceOf<ObjectDisposedException>(disposedBind.InnerException);
        }

        [Test]
        public void ContinuousBindingReevaluatesAndSnapshotBindingStabilizesDirtyFlag()
        {
            object resolver = CreateResolver();
            object context = CreateEvalContext(null, CreateEntityRef(1u, 1), CreateEntityRef(2u, 1));
            object snapshotBinding = CreateInstance(
                "BC.Base.ReactiveFloatBinding",
                resolver,
                context,
                InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 11f));

            object continuousSpec = InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 12f);
            SetFieldValue(continuousSpec, "evaluationMode", Enum.Parse(GetTypeByFullName("BC.Base.ReactiveEvaluationMode"), "Continuous"));
            object continuousBinding = CreateInstance(
                "BC.Base.ReactiveFloatBinding",
                resolver,
                context,
                continuousSpec);

            Assert.AreEqual(true, GetPropertyValue(snapshotBinding, "IsDirty"));
            AssertSuccessfulResult(InvokeMethod(snapshotBinding, "Read"), 11f);
            Assert.AreEqual(false, GetPropertyValue(snapshotBinding, "IsDirty"));

            Assert.AreEqual(true, GetPropertyValue(continuousBinding, "IsDirty"));
            AssertSuccessfulResult(InvokeMethod(continuousBinding, "Read"), 12f);
            Assert.AreEqual(true, GetPropertyValue(continuousBinding, "IsDirty"));
        }

        private static object CreateResolver()
        {
            return CreateInstance("BC.Base.ReactiveValueResolverService", new object[] { null });
        }

        private static object CreateEntityRef(uint entityId, int version)
        {
            return CreateInstance("BC.Base.EntityRef", entityId, version);
        }

        private static object CreateEvalContext(object sceneKernel, object actor, object trigger)
        {
            return CreateInstance("BC.Base.ReactiveEvalContext", sceneKernel, actor, trigger);
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

        private static void Dispose(object instance)
        {
            InvokeMethod(instance, "Dispose");
        }

        private static void AssertSuccessfulResult(object result, object expectedValue)
        {
            Assert.AreEqual(true, GetFieldValue(result, "Success"));
            Assert.AreEqual(false, GetPropertyValue(result, "Failed"));
            Assert.AreEqual(expectedValue, GetFieldValue(result, "Value"));
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
                if (method.Name != methodName)
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
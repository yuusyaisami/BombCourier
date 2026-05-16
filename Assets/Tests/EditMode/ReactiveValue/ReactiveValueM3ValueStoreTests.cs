using System;
using System.Reflection;
using NUnit.Framework;

namespace BC.Base.Tests
{
    public sealed class ReactiveValueM3ValueStoreTests
    {
        [Test]
        public void EntityValueStoreSnapshotAndWatchedFloatBindingsObserveStoreChanges()
        {
            object sceneKernel = CreateConfiguredSceneKernel();
            object resolver = CreateResolver(sceneKernel);
            object actor = CreateEntityRef(10u, 1);
            object context = CreateEvalContext(sceneKernel, actor, CreateEntityRef(20u, 1));
            object floatKey = GetStaticFieldValue("BC.Base.ValueKeys+Move", "BaseSpeed");
            object floatKeyReference = CreateValueKeyReference(typeof(float), floatKey);
            object self = InvokeStatic("BC.Base.ReactiveEntityRef", "Self");

            Assert.AreEqual(true, SetEntityStoreValue(sceneKernel, actor, typeof(float), floatKey, 7.5f));

            object snapshotSpec = InvokeStatic(
                "BC.Base.ReactiveFloat",
                "EntityValueStore",
                self,
                floatKeyReference,
                GetReactiveEvaluationMode("Snapshot"));
            object watchedSpec = InvokeStatic(
                "BC.Base.ReactiveFloat",
                "EntityValueStore",
                self,
                floatKeyReference,
                GetReactiveEvaluationMode("Watched"));

            object snapshotBinding = CreateInstance("BC.Base.ReactiveFloatBinding", resolver, context, snapshotSpec);
            object watchedBinding = CreateInstance("BC.Base.ReactiveFloatBinding", resolver, context, watchedSpec);

            AssertSuccessfulResult(InvokeMethod(snapshotBinding, "Read"), 7.5f);
            AssertSuccessfulResult(InvokeMethod(watchedBinding, "Read"), 7.5f);
            Assert.AreEqual(false, GetPropertyValue(watchedBinding, "IsDirty"));

            Assert.AreEqual(true, SetEntityStoreValue(sceneKernel, actor, typeof(float), floatKey, 9.5f));
            Assert.AreEqual(true, GetPropertyValue(watchedBinding, "IsDirty"));

            object watchedResult = InvokeMethod(watchedBinding, "Read");
            AssertSuccessfulResult(watchedResult, 9.5f);
            Assert.AreEqual(2, GetFieldValue(watchedResult, "Version"));
            Assert.AreEqual(false, GetPropertyValue(watchedBinding, "IsDirty"));

            AssertSuccessfulResult(InvokeMethod(snapshotBinding, "Read"), 7.5f);
        }

        [Test]
        public void KernelValueStoreBoolAndEntityValueStoreEntityResolveCurrentValues()
        {
            object sceneKernel = CreateConfiguredSceneKernel();
            object resolver = CreateResolver(sceneKernel);
            object actor = CreateEntityRef(30u, 2);
            object context = CreateEvalContext(sceneKernel, actor, CreateEntityRef(40u, 1));

            object kernelBoolKey = GetStaticFieldValue("BC.Base.ValueKeys+Kernel+Gimmick", "GlobalEnabled");
            object kernelBoolKeyReference = CreateValueKeyReference(typeof(bool), kernelBoolKey);
            Assert.AreEqual(true, SetKernelStoreValue(sceneKernel, typeof(bool), kernelBoolKey, false));

            object kernelBoolSpec = InvokeStatic(
                "BC.Base.ReactiveBool",
                "KernelValueStore",
                kernelBoolKeyReference,
                GetReactiveEvaluationMode("Snapshot"));
            object kernelBoolBinding = CreateInstance("BC.Base.ReactiveBoolBinding", resolver, context, kernelBoolSpec);
            AssertSuccessfulResult(InvokeMethod(kernelBoolBinding, "Read"), false);

            object entityKey = GetStaticFieldValue("BC.Base.ValueKeys+Runtime", "FocusEntity");
            object entityKeyReference = CreateValueKeyReference(GetTypeByFullName("BC.Base.EntityRef"), entityKey);
            object trackedEntity = CreateEntityRef(300u, 5);
            Assert.AreEqual(true, SetEntityStoreValue(sceneKernel, actor, GetTypeByFullName("BC.Base.EntityRef"), entityKey, trackedEntity));

            object entitySpec = InvokeStatic(
                "BC.Base.ReactiveEntityRef",
                "EntityValueStore",
                InvokeStatic("BC.Base.ReactiveEntityRef", "Self"),
                entityKeyReference,
                GetReactiveEvaluationMode("Snapshot"));
            object entityBinding = CreateInstance("BC.Base.ReactiveEntityRefBinding", resolver, context, entitySpec);
            AssertSuccessfulResult(InvokeMethod(entityBinding, "Read"), trackedEntity);
        }

        [Test]
        public void ValueStoreResolverReportsExplicitConfigurationFailures()
        {
            object actor = CreateEntityRef(50u, 1);
            object trigger = CreateEntityRef(60u, 1);
            object floatKey = GetStaticFieldValue("BC.Base.ValueKeys+Move", "BaseSpeed");
            object floatKeyReference = CreateValueKeyReference(typeof(float), floatKey);

            object missingKernelResolver = CreateResolver(null);
            object missingKernelContext = CreateEvalContext(null, actor, trigger);
            object kernelFloatSpec = InvokeStatic(
                "BC.Base.ReactiveFloat",
                "KernelValueStore",
                floatKeyReference,
                GetReactiveEvaluationMode("Snapshot"));
            AssertFailedResult(InvokeResolve(missingKernelResolver, "ResolveFloat", missingKernelContext, kernelFloatSpec), "MissingSceneKernel");

            object missingStoreKernel = CreateInstance("BC.Base.SceneKernel");
            object missingStoreResolver = CreateResolver(missingStoreKernel);
            object missingStoreContext = CreateEvalContext(missingStoreKernel, actor, trigger);
            AssertFailedResult(InvokeResolve(missingStoreResolver, "ResolveFloat", missingStoreContext, kernelFloatSpec), "MissingValueStore");

            object unassignedKeyReference = Activator.CreateInstance(GetTypeByFullName("BC.Base.ValueKeyReference"));
            object unassignedKeySpec = InvokeStatic(
                "BC.Base.ReactiveFloat",
                "KernelValueStore",
                unassignedKeyReference,
                GetReactiveEvaluationMode("Snapshot"));
            object configuredKernel = CreateConfiguredSceneKernel();
            object configuredResolver = CreateResolver(configuredKernel);
            object configuredContext = CreateEvalContext(configuredKernel, actor, trigger);
            AssertFailedResult(InvokeResolve(configuredResolver, "ResolveFloat", configuredContext, unassignedKeySpec), "ValueKeyNotAssigned");

            object boolKey = GetStaticFieldValue("BC.Base.ValueKeys+Kernel+Gimmick", "GlobalEnabled");
            object boolKeyReference = CreateValueKeyReference(typeof(bool), boolKey);
            object wrongTypeSpec = InvokeStatic(
                "BC.Base.ReactiveFloat",
                "KernelValueStore",
                boolKeyReference,
                GetReactiveEvaluationMode("Snapshot"));
            AssertFailedResult(InvokeResolve(configuredResolver, "ResolveFloat", configuredContext, wrongTypeSpec), "ValueKeyTypeMismatch");

            object invalidActor = Activator.CreateInstance(GetTypeByFullName("BC.Base.EntityRef"));
            object invalidEntityContext = CreateEvalContext(configuredKernel, invalidActor, trigger);
            object entityFloatSpec = InvokeStatic(
                "BC.Base.ReactiveFloat",
                "EntityValueStore",
                InvokeStatic("BC.Base.ReactiveEntityRef", "Self"),
                floatKeyReference,
                GetReactiveEvaluationMode("Snapshot"));
            AssertFailedResult(InvokeResolve(configuredResolver, "ResolveFloat", invalidEntityContext, entityFloatSpec), "InvalidEntity");
        }

        [Test]
        public void ContinuousValueStoreBindingsCanUseFallback()
        {
            object sceneKernel = CreateConfiguredSceneKernel();
            object resolver = CreateResolver(sceneKernel);
            object actor = CreateEntityRef(70u, 1);
            object context = CreateEvalContext(sceneKernel, actor, CreateEntityRef(80u, 1));
            object floatKey = GetStaticFieldValue("BC.Base.ValueKeys+Move", "BaseSpeed");
            object floatKeyReference = CreateValueKeyReference(typeof(float), floatKey);
            object self = InvokeStatic("BC.Base.ReactiveEntityRef", "Self");

            Assert.AreEqual(true, SetEntityStoreValue(sceneKernel, actor, typeof(float), floatKey, 6.0f));

            object invalidModeSpec = InvokeStatic(
                "BC.Base.ReactiveFloat",
                "EntityValueStore",
                self,
                floatKeyReference,
                GetReactiveEvaluationMode("Continuous"));

            AssertFailedResult(InvokeResolve(resolver, "ResolveFloat", context, invalidModeSpec), "UnsupportedEvaluationMode");

            SetFieldValue(invalidModeSpec, "failurePolicy", GetReactiveFailurePolicy("UseFallback"));
            SetFieldValue(invalidModeSpec, "fallbackValue", 3.25f);

            object binding = CreateInstance("BC.Base.ReactiveFloatBinding", resolver, context, invalidModeSpec);
            AssertSuccessfulResult(InvokeMethod(binding, "Read"), 3.25f);
            Assert.AreEqual(true, GetPropertyValue(binding, "IsDirty"));
        }

        [Test]
        public void EntityValueStoreFactoryRejectsNestedStoreSelectors()
        {
            object floatKey = GetStaticFieldValue("BC.Base.ValueKeys+Move", "BaseSpeed");
            object floatKeyReference = CreateValueKeyReference(typeof(float), floatKey);
            object entityKey = GetStaticFieldValue("BC.Base.ValueKeys+Runtime", "FocusEntity");
            object entityKeyReference = CreateValueKeyReference(GetTypeByFullName("BC.Base.EntityRef"), entityKey);
            object nestedSelector = InvokeStatic(
                "BC.Base.ReactiveEntityRef",
                "KernelValueStore",
                entityKeyReference,
                GetReactiveEvaluationMode("Snapshot"));

            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
                InvokeStatic(
                    "BC.Base.ReactiveFloat",
                    "EntityValueStore",
                    nestedSelector,
                    floatKeyReference,
                    GetReactiveEvaluationMode("Snapshot")));

            Assert.IsInstanceOf<ArgumentOutOfRangeException>(exception.InnerException);
        }

        [Test]
        public void EntityValueStoreEntityBindingCanFallbackToTriggerEntity()
        {
            object sceneKernel = CreateConfiguredSceneKernel();
            object resolver = CreateResolver(sceneKernel);
            object actor = CreateEntityRef(90u, 1);
            object trigger = CreateEntityRef(91u, 2);
            object context = CreateEvalContext(sceneKernel, actor, trigger);
            object entityKey = GetStaticFieldValue("BC.Base.ValueKeys+Runtime", "FocusEntity");
            object entityKeyReference = CreateValueKeyReference(GetTypeByFullName("BC.Base.EntityRef"), entityKey);
            object invalidModeSpec = InvokeStatic(
                "BC.Base.ReactiveEntityRef",
                "EntityValueStore",
                InvokeStatic("BC.Base.ReactiveEntityRef", "Self"),
                entityKeyReference,
                GetReactiveEvaluationMode("Continuous"));

            SetFieldValue(invalidModeSpec, "failurePolicy", GetReactiveFailurePolicy("UseFallback"));
            SetFieldValue(invalidModeSpec, "fallbackKind", Enum.Parse(GetTypeByFullName("BC.Base.ReactiveEntityFallbackKind"), "TriggerEntity"));

            object binding = CreateInstance("BC.Base.ReactiveEntityRefBinding", resolver, context, invalidModeSpec);
            AssertSuccessfulResult(InvokeMethod(binding, "Read"), trigger);
            Assert.AreEqual(true, GetPropertyValue(binding, "IsDirty"));
        }

        private static object CreateConfiguredSceneKernel()
        {
            object sceneKernel = CreateInstance("BC.Base.SceneKernel");
            SetPropertyValue(sceneKernel, "EntityValueStore", CreateInstance("BC.Base.ValueStoreService"));
            SetPropertyValue(sceneKernel, "KernelValueStore", CreateInstance("BC.Base.KernelValueStoreService"));
            return sceneKernel;
        }

        private static object CreateResolver(object sceneKernel)
        {
            return CreateInstance("BC.Base.ReactiveValueResolverService", sceneKernel);
        }

        private static object CreateEntityRef(uint entityId, int version)
        {
            return CreateInstance("BC.Base.EntityRef", entityId, version);
        }

        private static object CreateEvalContext(object sceneKernel, object actor, object trigger)
        {
            return CreateInstance("BC.Base.ReactiveEvalContext", sceneKernel, actor, trigger);
        }

        private static object CreateValueKeyReference(Type valueType, object key)
        {
            return InvokeGenericStatic("BC.Base.ValueKeyReference", "From", valueType, key);
        }

        private static bool SetEntityStoreValue(object sceneKernel, object entity, Type valueType, object key, object value)
        {
            object store = GetPropertyValue(sceneKernel, "EntityValueStore");
            return (bool)InvokeGenericMethod(store, "Set", valueType, entity, key, value);
        }

        private static bool SetKernelStoreValue(object sceneKernel, Type valueType, object key, object value)
        {
            object store = GetPropertyValue(sceneKernel, "KernelValueStore");
            return (bool)InvokeGenericMethod(store, "Set", valueType, key, value);
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

        private static object InvokeGenericStatic(string fullTypeName, string methodName, Type genericType, params object[] arguments)
        {
            Type type = GetTypeByFullName(fullTypeName);
            MethodInfo method = FindGenericMethod(type, methodName, BindingFlags.Public | BindingFlags.Static, genericType, arguments);
            Assert.IsNotNull(method, $"Expected generic static method: {fullTypeName}.{methodName}<{genericType.Name}>");
            return method.Invoke(null, BuildInvokeArguments(method.GetParameters(), arguments));
        }

        private static object InvokeMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(instance.GetType(), methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, arguments);
            Assert.IsNotNull(method, $"Expected method: {instance.GetType().FullName}.{methodName}");
            return method.Invoke(instance, BuildInvokeArguments(method.GetParameters(), arguments));
        }

        private static object InvokeGenericMethod(object instance, string methodName, Type genericType, params object[] arguments)
        {
            MethodInfo method = FindGenericMethod(instance.GetType(), methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, genericType, arguments);
            Assert.IsNotNull(method, $"Expected generic method: {instance.GetType().FullName}.{methodName}<{genericType.Name}>");
            return method.Invoke(instance, BuildInvokeArguments(method.GetParameters(), arguments));
        }

        private static object CreateInstance(string fullTypeName, params object[] arguments)
        {
            Type type = GetTypeByFullName(fullTypeName);
            ConstructorInfo ctor = FindConstructor(type, arguments);
            Assert.IsNotNull(ctor, $"Expected constructor: {fullTypeName}");
            return ctor.Invoke(BuildInvokeArguments(ctor.GetParameters(), arguments));
        }

        private static object GetStaticFieldValue(string fullTypeName, string fieldName)
        {
            Type type = GetTypeByFullName(fullTypeName);
            FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected static field: {fullTypeName}.{fieldName}");
            return field.GetValue(null);
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

        private static object GetReactiveFailurePolicy(string memberName)
        {
            return Enum.Parse(GetTypeByFullName("BC.Base.ReactiveFailurePolicy"), memberName);
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

        private static MethodInfo FindGenericMethod(
            Type ownerType,
            string methodName,
            BindingFlags bindingFlags,
            Type genericType,
            object[] arguments)
        {
            MethodInfo[] methods = ownerType.GetMethods(bindingFlags);

            for (int index = 0; index < methods.Length; index++)
            {
                MethodInfo method = methods[index];
                if (method.Name != methodName || !method.IsGenericMethodDefinition || method.GetGenericArguments().Length != 1)
                    continue;

                MethodInfo closedMethod = method.MakeGenericMethod(genericType);

                if (CanAcceptArguments(closedMethod.GetParameters(), arguments))
                    return closedMethod;
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
using System;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

namespace BC.Base.Tests
{
    public sealed class ReactiveValueM5ActionIntegrationTests
    {
        private static readonly Lazy<Type> ThrowingActionDefinitionType = new(BuildThrowingActionDefinitionType);

        [Test]
        public void SceneKernelCreatesReactiveResolverAndLegacyContextKeepsReactiveNull()
        {
            object sceneKernel = CreateInstance("BC.Base.SceneKernel");

            try
            {
                Assert.IsNotNull(GetPropertyValue(sceneKernel, "ReactiveValues"));

                object context = CreateInstance(
                    "BC.ActionSystem.ActionExecutionContext",
                    sceneKernel,
                    GetPropertyValue(sceneKernel, "Actions"),
                    CreateEntityRef(1u, 1));

                Assert.IsNull(GetFieldValue(context, "Reactive"));
            }
            finally
            {
                InvokeMethod(sceneKernel, "Dispose");
            }
        }

        [Test]
        public void ActionExecutionInjectsReactiveScopeAndDisposesItOnCompletion()
        {
            object sceneKernel = CreateInstance("BC.Base.SceneKernel");

            try
            {
                object actionService = GetPropertyValue(sceneKernel, "Actions");
                object actor = CreateEntityRef(10u, 1);
                object literalSpec = InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 2.5f);
                object handle = InvokeMethod(actionService, "Execute", actor, CreateWaitFramesAction(1));

                Assert.AreEqual(true, GetPropertyValue(handle, "IsValid"));
                Assert.IsTrue(TryGetExecution(actionService, handle, out object execution));

                object initialScope = GetReactiveScope(execution);
                Assert.IsNotNull(initialScope);

                object binding = InvokeMethod(initialScope, "Bind", literalSpec);
                AssertSuccessfulResult(InvokeMethod(binding, "Read"), 2.5f);

                InvokeMethod(actionService, "Tick", 0f);
                Assert.IsTrue(TryGetExecution(actionService, handle, out execution));
                object runningScope = GetReactiveScope(execution);
                Assert.AreSame(initialScope, runningScope);

                InvokeMethod(actionService, "Tick", 0f);
                Assert.IsFalse(TryGetExecution(actionService, handle, out _));
                Assert.AreEqual(false, GetPropertyValue(binding, "IsValid"));

                TargetInvocationException disposedBind = Assert.Throws<TargetInvocationException>(() =>
                    InvokeMethod(initialScope, "Bind", InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 1f)));
                Assert.IsInstanceOf<ObjectDisposedException>(disposedBind.InnerException);
            }
            finally
            {
                InvokeMethod(sceneKernel, "Dispose");
            }
        }

        [Test]
        public void ActionCancelDisposesReactiveScopeAndBindings()
        {
            object sceneKernel = CreateInstance("BC.Base.SceneKernel");

            try
            {
                object actionService = GetPropertyValue(sceneKernel, "Actions");
                object actor = CreateEntityRef(20u, 1);
                object handle = InvokeMethod(actionService, "Execute", actor, CreateWaitFramesAction(3));

                Assert.IsTrue(TryGetExecution(actionService, handle, out object execution));
                object scope = GetReactiveScope(execution);
                object binding = InvokeMethod(scope, "Bind", InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 4.5f));
                AssertSuccessfulResult(InvokeMethod(binding, "Read"), 4.5f);

                InvokeMethod(actionService, "Cancel", actor, "test cancel");

                Assert.IsFalse(TryGetExecution(actionService, handle, out _));
                Assert.AreEqual(false, GetPropertyValue(binding, "IsValid"));

                TargetInvocationException disposedBind = Assert.Throws<TargetInvocationException>(() =>
                    InvokeMethod(scope, "Bind", InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 1f)));
                Assert.IsInstanceOf<ObjectDisposedException>(disposedBind.InnerException);
            }
            finally
            {
                InvokeMethod(sceneKernel, "Dispose");
            }
        }

        [Test]
        public void ReplacingAnExecutionDisposesPreviousReactiveScope()
        {
            object sceneKernel = CreateInstance("BC.Base.SceneKernel");

            try
            {
                object actionService = GetPropertyValue(sceneKernel, "Actions");
                object actor = CreateEntityRef(30u, 1);
                object firstHandle = InvokeMethod(actionService, "Execute", actor, CreateWaitFramesAction(3));

                Assert.IsTrue(TryGetExecution(actionService, firstHandle, out object firstExecution));
                object firstScope = GetReactiveScope(firstExecution);
                object firstBinding = InvokeMethod(firstScope, "Bind", InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 6.5f));
                AssertSuccessfulResult(InvokeMethod(firstBinding, "Read"), 6.5f);

                object secondHandle = InvokeMethod(actionService, "Execute", actor, CreateWaitFramesAction(0));

                Assert.AreEqual(false, GetPropertyValue(firstBinding, "IsValid"));

                TargetInvocationException disposedBind = Assert.Throws<TargetInvocationException>(() =>
                    InvokeMethod(firstScope, "Bind", InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 1f)));
                Assert.IsInstanceOf<ObjectDisposedException>(disposedBind.InnerException);

                Assert.AreEqual(true, GetPropertyValue(secondHandle, "IsValid"));
                Assert.IsTrue(TryGetExecution(actionService, secondHandle, out object secondExecution));
                Assert.IsNotNull(GetReactiveScope(secondExecution));
            }
            finally
            {
                InvokeMethod(sceneKernel, "Dispose");
            }
        }

        [Test]
        public void ActionExecutionDisposesReactiveScopeWhenTickThrows()
        {
            object sceneKernel = CreateInstance("BC.Base.SceneKernel");

            try
            {
                object actionService = GetPropertyValue(sceneKernel, "Actions");
                object actor = CreateEntityRef(40u, 1);
                object handle = InvokeMethod(actionService, "Execute", actor, CreateThrowingAction(throwOnTick: true));

                Assert.IsTrue(TryGetExecution(actionService, handle, out object execution));
                object scope = GetReactiveScope(execution);
                object binding = InvokeMethod(scope, "Bind", InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 8.5f));
                AssertSuccessfulResult(InvokeMethod(binding, "Read"), 8.5f);

                InvokeMethod(actionService, "Tick", 0f);

                Assert.IsFalse(TryGetExecution(actionService, handle, out _));
                Assert.AreEqual(false, GetPropertyValue(binding, "IsValid"));

                TargetInvocationException disposedBind = Assert.Throws<TargetInvocationException>(() =>
                    InvokeMethod(scope, "Bind", InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 1f)));
                Assert.IsInstanceOf<ObjectDisposedException>(disposedBind.InnerException);
            }
            finally
            {
                InvokeMethod(sceneKernel, "Dispose");
            }
        }

        [Test]
        public void ActionExecutionDisposesReactiveScopeWhenCancelThrows()
        {
            object sceneKernel = CreateInstance("BC.Base.SceneKernel");

            try
            {
                object actionService = GetPropertyValue(sceneKernel, "Actions");
                object actor = CreateEntityRef(50u, 1);
                object handle = InvokeMethod(actionService, "Execute", actor, CreateThrowingAction(throwOnCancel: true));

                Assert.IsTrue(TryGetExecution(actionService, handle, out object execution));
                object scope = GetReactiveScope(execution);
                object binding = InvokeMethod(scope, "Bind", InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 9.5f));
                AssertSuccessfulResult(InvokeMethod(binding, "Read"), 9.5f);

                InvokeMethod(actionService, "Cancel", actor, "test cancel throw");

                Assert.IsFalse(TryGetExecution(actionService, handle, out _));
                Assert.AreEqual(false, GetPropertyValue(binding, "IsValid"));

                TargetInvocationException disposedBind = Assert.Throws<TargetInvocationException>(() =>
                    InvokeMethod(scope, "Bind", InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 1f)));
                Assert.IsInstanceOf<ObjectDisposedException>(disposedBind.InnerException);
            }
            finally
            {
                InvokeMethod(sceneKernel, "Dispose");
            }
        }

        [Test]
        public void SetValueStoreValueStepRuntimeWritesEntityFaceExpressionWithAutoKind()
        {
            object sceneKernel = CreateConfiguredSceneKernel();

            try
            {
                object actor = CreateEntityRef(60u, 1);
                object trigger = CreateEntityRef(61u, 1);
                Type faceExpressionType = GetTypeByFullName("BC.Base.FaceExpressionId");
                object faceExpressionKey = GetStaticFieldValue("BC.Base.ValueKeys+Runtime", "FaceExpression");
                object faceExpressionKeyReference = InvokeGenericStatic("BC.Base.ValueKeyReference", "From", faceExpressionType, faceExpressionKey);
                object definition = CreateInstance(
                    "BC.ActionSystem.SetValueStoreValueStepRuntime",
                    Enum.Parse(GetTypeByFullName("BC.ActionSystem.ValueStoreWriteStoreScope"), "Entity"),
                    InvokeStatic("BC.Base.EntityTargetReference", "Self"),
                    Enum.Parse(GetTypeByFullName("BC.ActionSystem.ValueStoreWriteValueKind"), "Auto"),
                    faceExpressionKeyReference,
                    InvokeStatic("BC.Base.ReactiveBool", "LiteralValue", false),
                    InvokeStatic("BC.Base.ReactiveInt", "LiteralValue", 0),
                    InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 0f),
                    InvokeStatic("BC.Base.ReactiveString", "LiteralValue", string.Empty),
                    InvokeStatic("BC.Base.ReactiveEntityRef", "Self"),
                    InvokeStatic("BC.Base.ReactiveFaceExpressionId", "LiteralValue", Enum.Parse(faceExpressionType, "Happy")),
                    InvokeStatic("BC.Base.ReactiveEntityMoveState", "LiteralValue", Enum.Parse(GetTypeByFullName("BC.Base.EntityMoveState"), "Idle")));

                object runtime = InvokeMethod(definition, "CreateRuntime");
                object context = CreateInstance("BC.ActionSystem.ActionExecutionContext", sceneKernel, GetPropertyValue(sceneKernel, "Actions"), actor, trigger);
                object status = InvokeMethod(runtime, "Tick", context, 8);

                Assert.AreEqual("Continue", status.ToString());
                object entityValueStore = GetPropertyValue(sceneKernel, "EntityValueStore");
                Assert.AreEqual(
                    Enum.Parse(faceExpressionType, "Happy"),
                    InvokeGenericMethod(entityValueStore, "Get", faceExpressionType, actor, faceExpressionKey));
            }
            finally
            {
                InvokeMethod(sceneKernel, "Dispose");
            }
        }

        [Test]
        public void SetValueStoreValueStepRuntimeWritesKernelIntWithExplicitKind()
        {
            object sceneKernel = CreateConfiguredSceneKernel();

            try
            {
                object actor = CreateEntityRef(62u, 1);
                object kernelIntKey = GetStaticFieldValue("BC.Base.ValueKeys+Kernel+Gimmick", "SequenceIndex");
                object kernelIntKeyReference = InvokeGenericStatic("BC.Base.ValueKeyReference", "From", typeof(int), kernelIntKey);
                object definition = CreateInstance(
                    "BC.ActionSystem.SetValueStoreValueStepRuntime",
                    Enum.Parse(GetTypeByFullName("BC.ActionSystem.ValueStoreWriteStoreScope"), "Kernel"),
                    InvokeStatic("BC.Base.EntityTargetReference", "Self"),
                    Enum.Parse(GetTypeByFullName("BC.ActionSystem.ValueStoreWriteValueKind"), "Int"),
                    kernelIntKeyReference,
                    InvokeStatic("BC.Base.ReactiveBool", "LiteralValue", false),
                    InvokeStatic("BC.Base.ReactiveInt", "LiteralValue", 42),
                    InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 0f),
                    InvokeStatic("BC.Base.ReactiveString", "LiteralValue", string.Empty),
                    InvokeStatic("BC.Base.ReactiveEntityRef", "Self"),
                    InvokeStatic("BC.Base.ReactiveFaceExpressionId", "LiteralValue", Enum.Parse(GetTypeByFullName("BC.Base.FaceExpressionId"), "Neutral")),
                    InvokeStatic("BC.Base.ReactiveEntityMoveState", "LiteralValue", Enum.Parse(GetTypeByFullName("BC.Base.EntityMoveState"), "Idle")));

                object runtime = InvokeMethod(definition, "CreateRuntime");
                object context = CreateInstance("BC.ActionSystem.ActionExecutionContext", sceneKernel, GetPropertyValue(sceneKernel, "Actions"), actor);
                object status = InvokeMethod(runtime, "Tick", context, 8);

                Assert.AreEqual("Continue", status.ToString());
                object kernelValueStore = GetPropertyValue(sceneKernel, "KernelValueStore");
                Assert.AreEqual(42, InvokeGenericMethod(kernelValueStore, "Get", typeof(int), kernelIntKey));
            }
            finally
            {
                InvokeMethod(sceneKernel, "Dispose");
            }
        }

        [Test]
        public void ActionExecutionInjectsLocalValueStoreAndClearsItOnCompletion()
        {
            object sceneKernel = CreateConfiguredSceneKernel();

            try
            {
                object actionService = GetPropertyValue(sceneKernel, "Actions");
                object actor = CreateEntityRef(70u, 1);
                object handle = InvokeMethod(actionService, "Execute", actor, CreateWaitFramesAction(1));

                Assert.IsTrue(TryGetExecution(actionService, handle, out object execution));

                object context = GetPropertyValue(execution, "Context");
                object localValueStore = GetFieldValue(context, "LocalValueStore");
                object localIntKey = GetStaticFieldValue("BC.Base.ValueKeys+Local+Values", "Int0");

                Assert.IsNotNull(localValueStore);
                Assert.AreEqual(true, InvokeGenericMethod(localValueStore, "Set", typeof(int), localIntKey, 27));
                Assert.AreEqual(27, InvokeGenericMethod(localValueStore, "Get", typeof(int), localIntKey));

                InvokeMethod(actionService, "Tick", 0f);
                InvokeMethod(actionService, "Tick", 0f);

                Assert.IsFalse(TryGetExecution(actionService, handle, out _));
                Assert.AreEqual(0, InvokeGenericMethod(localValueStore, "Get", typeof(int), localIntKey));
            }
            finally
            {
                InvokeMethod(sceneKernel, "Dispose");
            }
        }

        [Test]
        public void SetValueStoreValueStepRuntimeWritesLocalIntAndReactiveBindingReadsIt()
        {
            object sceneKernel = CreateConfiguredSceneKernel();

            try
            {
                object actionService = GetPropertyValue(sceneKernel, "Actions");
                object actor = CreateEntityRef(71u, 1);
                object handle = InvokeMethod(actionService, "Execute", actor, CreateWaitFramesAction(2));

                Assert.IsTrue(TryGetExecution(actionService, handle, out object execution));

                object context = GetPropertyValue(execution, "Context");
                object localIntKey = GetStaticFieldValue("BC.Base.ValueKeys+Local+Values", "Int0");
                object localIntKeyReference = InvokeGenericStatic("BC.Base.ValueKeyReference", "From", typeof(int), localIntKey);
                object definition = CreateInstance(
                    "BC.ActionSystem.SetValueStoreValueStepRuntime",
                    Enum.Parse(GetTypeByFullName("BC.ActionSystem.ValueStoreWriteStoreScope"), "Local"),
                    InvokeStatic("BC.Base.EntityTargetReference", "Self"),
                    Enum.Parse(GetTypeByFullName("BC.ActionSystem.ValueStoreWriteValueKind"), "Int"),
                    localIntKeyReference,
                    InvokeStatic("BC.Base.ReactiveBool", "LiteralValue", false),
                    InvokeStatic("BC.Base.ReactiveInt", "LiteralValue", 42),
                    InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 0f),
                    InvokeStatic("BC.Base.ReactiveString", "LiteralValue", string.Empty),
                    InvokeStatic("BC.Base.ReactiveEntityRef", "Self"),
                    InvokeStatic("BC.Base.ReactiveFaceExpressionId", "LiteralValue", Enum.Parse(GetTypeByFullName("BC.Base.FaceExpressionId"), "Neutral")),
                    InvokeStatic("BC.Base.ReactiveEntityMoveState", "LiteralValue", Enum.Parse(GetTypeByFullName("BC.Base.EntityMoveState"), "Idle")));

                object runtime = InvokeMethod(definition, "CreateRuntime");
                object status = InvokeMethod(runtime, "Tick", context, 8);

                Assert.AreEqual("Continue", status.ToString());

                object localValueStore = GetFieldValue(context, "LocalValueStore");
                Assert.AreEqual(42, InvokeGenericMethod(localValueStore, "Get", typeof(int), localIntKey));

                object localReactive = InvokeStatic("BC.Base.ReactiveInt", "LocalValueStore", localIntKeyReference);
                object scope = GetReactiveScope(execution);
                object binding = InvokeMethod(scope, "Bind", localReactive);

                AssertSuccessfulResult(InvokeMethod(binding, "Read"), 42);
            }
            finally
            {
                InvokeMethod(sceneKernel, "Dispose");
            }
        }

        private static object CreateConfiguredSceneKernel()
        {
            object sceneKernel = CreateInstance("BC.Base.SceneKernel");
            SetPropertyValue(sceneKernel, "EntityValueStore", CreateInstance("BC.Base.ValueStoreService"));
            SetPropertyValue(sceneKernel, "KernelValueStore", CreateInstance("BC.Base.KernelValueStoreService"));
            return sceneKernel;
        }

        private static object CreateEntityRef(uint entityId, int version)
        {
            return CreateInstance("BC.Base.EntityRef", entityId, version);
        }

        private static object CreateThrowingAction(
            bool throwOnCreateRuntime = false,
            bool throwOnTick = false,
            bool throwOnCancel = false)
        {
            object definition = Activator.CreateInstance(
                ThrowingActionDefinitionType.Value,
                throwOnCreateRuntime,
                throwOnTick,
                throwOnCancel);

            return CreateCompiledAction(definition);
        }

        private static object CreateWaitFramesAction(int frames)
        {
            object definition = CreateInstance("BC.ActionSystem.WaitFramesStepRuntime", frames);
            return CreateCompiledAction(definition);
        }

        private static object CreateCompiledAction(object definition)
        {
            Type nodeDefinitionType = GetTypeByFullName("BC.ActionSystem.IActionNodeDefinition");
            Array definitions = Array.CreateInstance(nodeDefinitionType, 1);
            definitions.SetValue(definition, 0);
            object block = CreateInstance("BC.ActionSystem.ActionBlockDefinition", definitions);
            return CreateInstance("BC.ActionSystem.CompiledAction", block);
        }

        private static object GetReactiveScope(object execution)
        {
            object context = GetPropertyValue(execution, "Context");
            return GetFieldValue(context, "Reactive");
        }

        private static bool TryGetExecution(object actionService, object handle, out object execution)
        {
            MethodInfo method = FindTryGetExecutionMethod(actionService.GetType());
            Assert.IsNotNull(method, "Expected method: ActionService.TryGetExecution");

            object[] arguments = { handle, null };
            bool success = (bool)method.Invoke(actionService, arguments);
            execution = arguments[1];
            return success;
        }

        private static MethodInfo FindTryGetExecutionMethod(Type ownerType)
        {
            MethodInfo[] methods = ownerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int index = 0; index < methods.Length; index++)
            {
                MethodInfo method = methods[index];
                if (method.Name != "TryGetExecution")
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 2 && parameters[1].ParameterType.IsByRef)
                    return method;
            }

            return null;
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

        private static object InvokeGenericStatic(string fullTypeName, string methodName, Type genericType, params object[] arguments)
        {
            Type type = GetTypeByFullName(fullTypeName);
            MethodInfo method = FindGenericMethod(type, methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, genericType, arguments);
            Assert.IsNotNull(method, $"Expected generic static method: {fullTypeName}.{methodName}<{genericType.Name}>");
            return method.Invoke(null, BuildInvokeArguments(method.GetParameters(), arguments));
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

        private static void AssertSuccessfulResult(object result, object expectedValue)
        {
            Assert.AreEqual(true, GetFieldValue(result, "Success"));
            Assert.AreEqual(false, GetPropertyValue(result, "Failed"));
            Assert.AreEqual(expectedValue, GetFieldValue(result, "Value"));
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

        private static object GetStaticFieldValue(string fullTypeName, string fieldName)
        {
            Type type = GetTypeByFullName(fullTypeName);
            FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected static field: {fullTypeName}.{fieldName}");
            return field.GetValue(null);
        }

        private static void SetPropertyValue(object instance, string propertyName, object value)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property: {propertyName}");
            property.SetValue(instance, value);
        }

        private static Type BuildThrowingActionDefinitionType()
        {
            AssemblyName assemblyName = new("BC.Base.Tests.DynamicActionRuntime");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            Type nodeDefinitionType = GetTypeByFullName("BC.ActionSystem.IActionNodeDefinition");
            Type nodeRuntimeType = GetTypeByFullName("BC.ActionSystem.IActionNodeRuntime");
            Type contextType = GetTypeByFullName("BC.ActionSystem.ActionExecutionContext");
            Type statusType = GetTypeByFullName("BC.ActionSystem.ActionNodeStatus");

            Type runtimeType = BuildThrowingRuntimeType(moduleBuilder, nodeRuntimeType, contextType, statusType);
            return BuildThrowingDefinitionType(moduleBuilder, nodeDefinitionType, runtimeType);
        }

        private static Type BuildThrowingRuntimeType(
            ModuleBuilder moduleBuilder,
            Type nodeRuntimeType,
            Type contextType,
            Type statusType)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                "ThrowingActionRuntime",
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);
            typeBuilder.AddInterfaceImplementation(nodeRuntimeType);

            FieldBuilder throwOnTickField = typeBuilder.DefineField("throwOnTick", typeof(bool), FieldAttributes.Private | FieldAttributes.InitOnly);
            FieldBuilder throwOnCancelField = typeBuilder.DefineField("throwOnCancel", typeof(bool), FieldAttributes.Private | FieldAttributes.InitOnly);

            ConstructorBuilder ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] { typeof(bool), typeof(bool) });
            ILGenerator ctorIl = ctor.GetILGenerator();
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldarg_1);
            ctorIl.Emit(OpCodes.Stfld, throwOnTickField);
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldarg_2);
            ctorIl.Emit(OpCodes.Stfld, throwOnCancelField);
            ctorIl.Emit(OpCodes.Ret);

            MethodInfo tickContract = nodeRuntimeType.GetMethod("Tick");
            MethodBuilder tickMethod = typeBuilder.DefineMethod(
                tickContract.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                statusType,
                new[] { contextType.MakeByRefType(), typeof(int).MakeByRefType() });
            tickMethod.DefineParameter(1, ParameterAttributes.In, "context");
            tickMethod.DefineParameter(2, ParameterAttributes.None, "remainingOperations");
            ILGenerator tickIl = tickMethod.GetILGenerator();
            Label continueLabel = tickIl.DefineLabel();
            tickIl.Emit(OpCodes.Ldarg_0);
            tickIl.Emit(OpCodes.Ldfld, throwOnTickField);
            tickIl.Emit(OpCodes.Brfalse_S, continueLabel);
            tickIl.Emit(OpCodes.Ldstr, "throw from Tick");
            tickIl.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }));
            tickIl.Emit(OpCodes.Throw);
            tickIl.MarkLabel(continueLabel);
            tickIl.Emit(OpCodes.Ldc_I4_0);
            tickIl.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(tickMethod, tickContract);

            MethodInfo cancelContract = nodeRuntimeType.GetMethod("Cancel");
            MethodBuilder cancelMethod = typeBuilder.DefineMethod(
                cancelContract.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                typeof(void),
                new[] { contextType.MakeByRefType() });
            cancelMethod.DefineParameter(1, ParameterAttributes.In, "context");
            ILGenerator cancelIl = cancelMethod.GetILGenerator();
            Label cancelReturn = cancelIl.DefineLabel();
            cancelIl.Emit(OpCodes.Ldarg_0);
            cancelIl.Emit(OpCodes.Ldfld, throwOnCancelField);
            cancelIl.Emit(OpCodes.Brfalse_S, cancelReturn);
            cancelIl.Emit(OpCodes.Ldstr, "throw from Cancel");
            cancelIl.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }));
            cancelIl.Emit(OpCodes.Throw);
            cancelIl.MarkLabel(cancelReturn);
            cancelIl.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(cancelMethod, cancelContract);

            return typeBuilder.CreateType();
        }

        private static Type BuildThrowingDefinitionType(ModuleBuilder moduleBuilder, Type nodeDefinitionType, Type runtimeType)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                "ThrowingActionDefinition",
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);
            typeBuilder.AddInterfaceImplementation(nodeDefinitionType);

            FieldBuilder throwOnCreateRuntimeField = typeBuilder.DefineField("throwOnCreateRuntime", typeof(bool), FieldAttributes.Private | FieldAttributes.InitOnly);
            FieldBuilder throwOnTickField = typeBuilder.DefineField("throwOnTick", typeof(bool), FieldAttributes.Private | FieldAttributes.InitOnly);
            FieldBuilder throwOnCancelField = typeBuilder.DefineField("throwOnCancel", typeof(bool), FieldAttributes.Private | FieldAttributes.InitOnly);

            ConstructorBuilder ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] { typeof(bool), typeof(bool), typeof(bool) });
            ILGenerator ctorIl = ctor.GetILGenerator();
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldarg_1);
            ctorIl.Emit(OpCodes.Stfld, throwOnCreateRuntimeField);
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldarg_2);
            ctorIl.Emit(OpCodes.Stfld, throwOnTickField);
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldarg_3);
            ctorIl.Emit(OpCodes.Stfld, throwOnCancelField);
            ctorIl.Emit(OpCodes.Ret);

            MethodInfo createRuntimeContract = nodeDefinitionType.GetMethod("CreateRuntime");
            ConstructorInfo runtimeCtor = runtimeType.GetConstructor(new[] { typeof(bool), typeof(bool) });
            MethodBuilder createRuntimeMethod = typeBuilder.DefineMethod(
                createRuntimeContract.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                runtimeType,
                Type.EmptyTypes);
            ILGenerator createRuntimeIl = createRuntimeMethod.GetILGenerator();
            Label createRuntimeLabel = createRuntimeIl.DefineLabel();
            createRuntimeIl.Emit(OpCodes.Ldarg_0);
            createRuntimeIl.Emit(OpCodes.Ldfld, throwOnCreateRuntimeField);
            createRuntimeIl.Emit(OpCodes.Brfalse_S, createRuntimeLabel);
            createRuntimeIl.Emit(OpCodes.Ldstr, "throw from CreateRuntime");
            createRuntimeIl.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }));
            createRuntimeIl.Emit(OpCodes.Throw);
            createRuntimeIl.MarkLabel(createRuntimeLabel);
            createRuntimeIl.Emit(OpCodes.Ldarg_0);
            createRuntimeIl.Emit(OpCodes.Ldfld, throwOnTickField);
            createRuntimeIl.Emit(OpCodes.Ldarg_0);
            createRuntimeIl.Emit(OpCodes.Ldfld, throwOnCancelField);
            createRuntimeIl.Emit(OpCodes.Newobj, runtimeCtor);
            createRuntimeIl.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(createRuntimeMethod, createRuntimeContract);

            return typeBuilder.CreateType();
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
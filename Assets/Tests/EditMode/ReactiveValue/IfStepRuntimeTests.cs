using System;
using NUnit.Framework;

namespace BC.Base.Tests
{
    public sealed class IfStepRuntimeTests
    {
        [Test]
        public void IfStepRuntimeExecutesTrueBranchForLocalReactiveBoolCondition()
        {
            object sceneKernel = ReactiveValueTestUtility.CreateInstance("BC.Base.SceneKernel");

            try
            {
                object actor = CreateEntityRef(80u, 1);
                object trigger = CreateEntityRef(81u, 1);
                object localValueStore = ReactiveValueTestUtility.CreateInstance("BC.Base.ActionLocalValueStoreService");
                object localFlagKey = ReactiveValueTestUtility.GetStaticFieldValue("BC.Base.ValueKeys+Local+Flags", "Flag0");
                object localIntKey = ReactiveValueTestUtility.GetStaticFieldValue("BC.Base.ValueKeys+Local+Values", "Int0");
                object localFlagKeyReference = ReactiveValueTestUtility.InvokeGenericStatic("BC.Base.ValueKeyReference", "From", typeof(bool), localFlagKey);
                object localIntKeyReference = ReactiveValueTestUtility.InvokeGenericStatic("BC.Base.ValueKeyReference", "From", typeof(int), localIntKey);

                Assert.AreEqual(true, ReactiveValueTestUtility.InvokeGenericMethod(localValueStore, "Set", typeof(bool), localFlagKey, true));

                object definition = ReactiveValueTestUtility.CreateInstance(
                    "BC.ActionSystem.IfStepRuntime",
                    ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveBool", "LocalValueStore", localFlagKeyReference),
                    CreateBlock(CreateLocalIntWriteStep(localIntKeyReference, 11)),
                    CreateBlock(CreateLocalIntWriteStep(localIntKeyReference, 29)));
                object runtime = ReactiveValueTestUtility.InvokeMethod(definition, "CreateRuntime");
                object context = ReactiveValueTestUtility.CreateInstance(
                    "BC.ActionSystem.ActionExecutionContext",
                    sceneKernel,
                    ReactiveValueTestUtility.GetPropertyValue(sceneKernel, "Actions"),
                    actor,
                    trigger,
                    localValueStore);

                object status = ReactiveValueTestUtility.InvokeMethod(runtime, "Tick", context, 16);

                Assert.AreEqual("Continue", status.ToString());
                Assert.AreEqual(11, ReactiveValueTestUtility.InvokeGenericMethod(localValueStore, "Get", typeof(int), localIntKey));
            }
            finally
            {
                ReactiveValueTestUtility.InvokeMethod(sceneKernel, "Dispose");
            }
        }

        [Test]
        public void IfStepRuntimeExecutesFalseBranchForLocalReactiveBoolCondition()
        {
            object sceneKernel = ReactiveValueTestUtility.CreateInstance("BC.Base.SceneKernel");

            try
            {
                object actor = CreateEntityRef(82u, 1);
                object trigger = CreateEntityRef(83u, 1);
                object localValueStore = ReactiveValueTestUtility.CreateInstance("BC.Base.ActionLocalValueStoreService");
                object localFlagKey = ReactiveValueTestUtility.GetStaticFieldValue("BC.Base.ValueKeys+Local+Flags", "Flag0");
                object localIntKey = ReactiveValueTestUtility.GetStaticFieldValue("BC.Base.ValueKeys+Local+Values", "Int0");
                object localFlagKeyReference = ReactiveValueTestUtility.InvokeGenericStatic("BC.Base.ValueKeyReference", "From", typeof(bool), localFlagKey);
                object localIntKeyReference = ReactiveValueTestUtility.InvokeGenericStatic("BC.Base.ValueKeyReference", "From", typeof(int), localIntKey);

                Assert.AreEqual(true, ReactiveValueTestUtility.InvokeGenericMethod(localValueStore, "Set", typeof(bool), localFlagKey, false));

                object definition = ReactiveValueTestUtility.CreateInstance(
                    "BC.ActionSystem.IfStepRuntime",
                    ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveBool", "LocalValueStore", localFlagKeyReference),
                    CreateBlock(CreateLocalIntWriteStep(localIntKeyReference, 11)),
                    CreateBlock(CreateLocalIntWriteStep(localIntKeyReference, 29)));
                object runtime = ReactiveValueTestUtility.InvokeMethod(definition, "CreateRuntime");
                object context = ReactiveValueTestUtility.CreateInstance(
                    "BC.ActionSystem.ActionExecutionContext",
                    sceneKernel,
                    ReactiveValueTestUtility.GetPropertyValue(sceneKernel, "Actions"),
                    actor,
                    trigger,
                    localValueStore);

                object status = ReactiveValueTestUtility.InvokeMethod(runtime, "Tick", context, 16);

                Assert.AreEqual("Continue", status.ToString());
                Assert.AreEqual(29, ReactiveValueTestUtility.InvokeGenericMethod(localValueStore, "Get", typeof(int), localIntKey));
            }
            finally
            {
                ReactiveValueTestUtility.InvokeMethod(sceneKernel, "Dispose");
            }
        }

        private static object CreateEntityRef(uint entityId, int version)
        {
            return ReactiveValueTestUtility.CreateInstance("BC.Base.EntityRef", entityId, version);
        }

        private static object CreateBlock(object definition)
        {
            Type nodeDefinitionType = ReactiveValueTestUtility.GetTypeByFullName("BC.ActionSystem.IActionNodeDefinition");
            Array definitions = Array.CreateInstance(nodeDefinitionType, 1);
            definitions.SetValue(definition, 0);
            return ReactiveValueTestUtility.CreateInstance("BC.ActionSystem.ActionBlockDefinition", definitions);
        }

        private static object CreateLocalIntWriteStep(object keyReference, int value)
        {
            return ReactiveValueTestUtility.CreateInstance(
                "BC.ActionSystem.SetValueStoreValueStepRuntime",
                Enum.Parse(ReactiveValueTestUtility.GetTypeByFullName("BC.ActionSystem.ValueStoreWriteStoreScope"), "Local"),
                ReactiveValueTestUtility.InvokeStatic("BC.Base.EntityTargetReference", "Self"),
                Enum.Parse(ReactiveValueTestUtility.GetTypeByFullName("BC.ActionSystem.ValueStoreWriteValueKind"), "Int"),
                keyReference,
                ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveBool", "LiteralValue", false),
                ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveInt", "LiteralValue", value),
                ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveFloat", "LiteralValue", 0f),
                ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveString", "LiteralValue", string.Empty),
                ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveEntityRef", "Self"),
                ReactiveValueTestUtility.InvokeStatic(
                    "BC.Base.ReactiveFaceExpressionId",
                    "LiteralValue",
                    Enum.Parse(ReactiveValueTestUtility.GetTypeByFullName("BC.Base.FaceExpressionId"), "Neutral")),
                ReactiveValueTestUtility.InvokeStatic(
                    "BC.Base.ReactiveEntityMoveState",
                    "LiteralValue",
                    Enum.Parse(ReactiveValueTestUtility.GetTypeByFullName("BC.Base.EntityMoveState"), "Idle")));
        }
    }
}
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
                object kernelValueStore = ReactiveValueTestUtility.CreateInstance("BC.Base.KernelValueStoreService");
                object kernelFlagKey = ReactiveValueTestUtility.GetStaticFieldValue("BC.Base.ValueKeys+Kernel+Gimmick", "GlobalEnabled");
                object localIntKey = ReactiveValueTestUtility.GetStaticFieldValue("BC.Base.ValueKeys+Local+Values", "Int0");
                object kernelFlagKeyReference = ReactiveValueTestUtility.InvokeGenericStatic("BC.Base.ValueKeyReference", "From", typeof(bool), kernelFlagKey);
                object localIntKeyReference = ReactiveValueTestUtility.InvokeGenericStatic("BC.Base.ValueKeyReference", "From", typeof(int), localIntKey);

                ReactiveValueTestUtility.SetPropertyValue(sceneKernel, "KernelValueStore", kernelValueStore);
                ReactiveValueTestUtility.InvokeGenericMethod(kernelValueStore, "Set", typeof(bool), kernelFlagKey, true);
                Assert.AreEqual(true, ReactiveValueTestUtility.InvokeGenericMethod(kernelValueStore, "Get", typeof(bool), kernelFlagKey));

                object definition = ReactiveValueTestUtility.CreateInstance(
                    "BC.ActionSystem.IfStepRuntime",
                    ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveBool", "KernelValueStore", kernelFlagKeyReference),
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
                object kernelValueStore = ReactiveValueTestUtility.CreateInstance("BC.Base.KernelValueStoreService");
                object kernelFlagKey = ReactiveValueTestUtility.GetStaticFieldValue("BC.Base.ValueKeys+Kernel+Gimmick", "GlobalEnabled");
                object localIntKey = ReactiveValueTestUtility.GetStaticFieldValue("BC.Base.ValueKeys+Local+Values", "Int0");
                object kernelFlagKeyReference = ReactiveValueTestUtility.InvokeGenericStatic("BC.Base.ValueKeyReference", "From", typeof(bool), kernelFlagKey);
                object localIntKeyReference = ReactiveValueTestUtility.InvokeGenericStatic("BC.Base.ValueKeyReference", "From", typeof(int), localIntKey);

                ReactiveValueTestUtility.SetPropertyValue(sceneKernel, "KernelValueStore", kernelValueStore);
                ReactiveValueTestUtility.InvokeGenericMethod(kernelValueStore, "Set", typeof(bool), kernelFlagKey, false);
                Assert.AreEqual(false, ReactiveValueTestUtility.InvokeGenericMethod(kernelValueStore, "Get", typeof(bool), kernelFlagKey));

                object definition = ReactiveValueTestUtility.CreateInstance(
                    "BC.ActionSystem.IfStepRuntime",
                    ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveBool", "KernelValueStore", kernelFlagKeyReference),
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
                Enum.Parse(ReactiveValueTestUtility.GetTypeByFullName("BC.ActionSystem.ValueStoreNumericOperation"), "Set"),
                keyReference,
                ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveSnapshotBool", "LiteralValue", false),
                ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveSnapshotInt", "LiteralValue", value),
                ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveSnapshotFloat", "LiteralValue", 0f),
                ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveSnapshotString", "LiteralValue", string.Empty),
                ReactiveValueTestUtility.InvokeStatic("BC.Base.ReactiveSnapshotEntityRef", "Self"),
                ReactiveValueTestUtility.InvokeStatic(
                    "BC.Base.ReactiveFaceExpressionId",
                    "LiteralValue",
                    Enum.Parse(ReactiveValueTestUtility.GetTypeByFullName("BC.Base.FaceExpressionId"), "Neutral")),
                ReactiveValueTestUtility.InvokeStatic(
                    "BC.Base.ReactiveEntityMoveState",
                    "LiteralValue",
                    Enum.Parse(ReactiveValueTestUtility.GetTypeByFullName("BC.Base.EntityMoveState"), "Idle")),
                ReactiveValueTestUtility.InvokeStatic(
                    "BC.Base.ReactiveShapeExpressionId",
                    "LiteralValue",
                    Enum.Parse(ReactiveValueTestUtility.GetTypeByFullName("BC.Base.ShapeExpressionId"), "Neutral")));
        }
    }
}

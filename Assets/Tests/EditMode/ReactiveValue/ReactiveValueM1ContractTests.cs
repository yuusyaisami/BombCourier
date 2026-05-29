using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace BC.Base.Tests
{
    public sealed class ReactiveValueM1ContractTests
    {
        [Test]
        public void M1EnumsExposeTheExpectedBaselineMembers()
        {
            AssertEnumMembers("BC.Base.ReactiveEvaluationMode", "Snapshot", "Watched", "Continuous");
            AssertEnumMembers("BC.Base.ReactiveFailurePolicy", "FailAction", "UseFallback");
            AssertEnumMembers("BC.Base.ReactiveFloatSourceKind", "Literal", "EntityValueStore", "KernelValueStore", "Distance");
            AssertEnumMembers("BC.Base.ReactiveIntSourceKind", "Literal", "EntityValueStore", "KernelValueStore");
            AssertEnumMembers("BC.Base.ReactiveBoolSourceKind", "Literal", "EntityValueStore", "KernelValueStore", "EntityAlive", "CompareNumber");
            AssertEnumMembers("BC.Base.ReactiveVector3SourceKind", "Literal", "EntityTransformPosition", "EntityTransformForward", "AddPosition", "AddForward", "Direction");
            AssertEnumMembers("BC.Base.ReactiveEntitySourceKind", "Self", "TriggerEntity", "EntityValueStore", "KernelValueStore", "TargetReference");
        }

        [Test]
        public void M1WrapperStructsAreSerializableValueTypes()
        {
            AssertSerializableStruct("BC.Base.ReactiveFloat");
            AssertSerializableStruct("BC.Base.ReactiveInt");
            AssertSerializableStruct("BC.Base.ReactiveBool");
            AssertSerializableStruct("BC.Base.ReactiveVector3");
            AssertSerializableStruct("BC.Base.ReactiveEntityRef");
        }

        [Test]
        public void ReactiveEvalContextKeepsActorAndTriggerReferences()
        {
            Type entityRefType = GetTypeByFullName("BC.Base.EntityRef");
            Type evalContextType = GetTypeByFullName("BC.Base.ReactiveEvalContext");
            object actor = Activator.CreateInstance(entityRefType, new object[] { 10u, 2 });
            object trigger = Activator.CreateInstance(entityRefType, new object[] { 20u, 3 });
            object context = Activator.CreateInstance(evalContextType, new object[] { null, actor, trigger });

            Assert.AreEqual(actor, GetFieldValue(context, "ActorEntity"));
            Assert.AreEqual(trigger, GetFieldValue(context, "TriggerEntity"));
            Assert.AreEqual(actor, GetPropertyValue(context, "SelfEntity"));

            ConstructorInfo actionContextCtor = evalContextType
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(candidate =>
                {
                    ParameterInfo[] parameters = candidate.GetParameters();
                    return parameters.Length == 1 &&
                           parameters[0].ParameterType.IsByRef &&
                           parameters[0].ParameterType.GetElementType()?.FullName == "BC.ActionSystem.ActionExecutionContext";
                });

            Assert.IsNotNull(actionContextCtor, "ReactiveEvalContext must expose an ActionExecutionContext conversion constructor.");
        }

        [Test]
        public void ReactiveResultOkAndFailExposeStableOutcomeFields()
        {
            Type errorCodeType = GetTypeByFullName("BC.Base.ReactiveErrorCode");
            Type errorType = GetTypeByFullName("BC.Base.ReactiveError");
            Type entityRefType = GetTypeByFullName("BC.Base.EntityRef");
            Type resultType = GetTypeByFullName("BC.Base.ReactiveResult`1").MakeGenericType(typeof(int));
            object actor = Activator.CreateInstance(entityRefType, new object[] { 1u, 1 });
            object trigger = Activator.CreateInstance(entityRefType, new object[] { 2u, 1 });
            object unsupportedSource = Enum.Parse(errorCodeType, "UnsupportedSource");
            object error = Activator.CreateInstance(errorType, unsupportedSource, "missing", actor, trigger);

            object okResult = resultType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, new object[] { 42, 7 });
            Assert.IsNotNull(okResult, "ReactiveResult<T>.Ok must exist.");
            Assert.AreEqual(true, GetFieldValue(okResult, "Success"));
            Assert.AreEqual(42, GetFieldValue(okResult, "Value"));
            Assert.AreEqual(7, GetFieldValue(okResult, "Version"));
            Assert.AreEqual(false, GetPropertyValue(okResult, "Failed"));

            object failResult = resultType.GetMethod("Fail", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, new[] { error });
            Assert.IsNotNull(failResult, "ReactiveResult<T>.Fail must exist.");
            Assert.AreEqual(false, GetFieldValue(failResult, "Success"));
            Assert.AreEqual(true, GetPropertyValue(failResult, "Failed"));
            Assert.AreEqual(error, GetFieldValue(failResult, "Error"));
            Assert.AreEqual(0, GetFieldValue(failResult, "Version"));
        }

        private static void AssertEnumMembers(string fullName, params string[] expectedMembers)
        {
            Type type = GetTypeByFullName(fullName);
            Assert.IsTrue(type.IsEnum, $"Expected enum type: {fullName}");
            CollectionAssert.AreEqual(expectedMembers, Enum.GetNames(type));
        }

        private static void AssertSerializableStruct(string fullName)
        {
            Type type = GetTypeByFullName(fullName);
            Assert.IsTrue(type.IsValueType, $"Expected struct type: {fullName}");
            Assert.IsTrue(type.IsDefined(typeof(SerializableAttribute), false), $"Expected [Serializable]: {fullName}");
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
    }
}
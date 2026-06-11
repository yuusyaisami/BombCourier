using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace BC.Base.Tests
{
    public sealed class ValueStoreServiceContractTests
    {
        private const string EntityRefTypeName = "BC.Base.EntityRef";
        private const string ValueStoreServiceTypeName = "BC.Base.ValueStoreService";
        private const string ValueModifierTagIdTypeName = "BC.Base.ValueModifierTagId";

        [Test]
        public void EntityValueStoreRejectsInvalidEntityRef()
        {
            object store = Activator.CreateInstance(GetTypeByFullName(ValueStoreServiceTypeName));
            object invalidEntity = Activator.CreateInstance(GetTypeByFullName(EntityRefTypeName));
            object baseSpeedKey = GetStaticFieldValue("BC.Base.ValueKeys+Move", "BaseSpeed");
            object canMoveByInputKey = GetStaticFieldValue("BC.Base.ValueKeys+Move", "CanMoveByInput");
            object addTag = Activator.CreateInstance(GetTypeByFullName(ValueModifierTagIdTypeName), 1);
            object boolTag = Activator.CreateInstance(GetTypeByFullName(ValueModifierTagIdTypeName), 2);

            AssertThrowsInvalidOperation(() => InvokeGenericValueKeyMethod(store, "Set", typeof(float), invalidEntity, baseSpeedKey, 6.0f));
            AssertThrowsInvalidOperation(() => InvokeGenericValueKeyMethod(store, "Get", typeof(float), invalidEntity, baseSpeedKey));
            AssertThrowsInvalidOperation(() => InvokeGenericValueKeyMethod(store, "GetHandle", typeof(float), invalidEntity, baseSpeedKey));
            AssertThrowsInvalidOperation(() => InvokeValueKeyMethod(store, "SetAdd", baseSpeedKey, invalidEntity, baseSpeedKey, addTag, 1.0f));
            AssertThrowsInvalidOperation(() => InvokeValueKeyMethod(store, "SetBoolModifier", canMoveByInputKey, invalidEntity, canMoveByInputKey, boolTag, false));
        }

        private static object InvokeGenericValueKeyMethod(object target, string methodName, Type genericArgument, params object[] args)
        {
            MethodInfo method = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(candidate => candidate.Name == methodName &&
                                    candidate.IsGenericMethodDefinition &&
                                    candidate.GetParameters().Length == args.Length &&
                                    IsValueKeyParameter(candidate.GetParameters()[1].ParameterType))
                .MakeGenericMethod(genericArgument);
            return method.Invoke(target, args);
        }

        private static object InvokeValueKeyMethod(object target, string methodName, object valueKey, params object[] args)
        {
            Type valueKeyType = valueKey.GetType();
            MethodInfo method = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(candidate => candidate.Name == methodName &&
                                    !candidate.IsGenericMethodDefinition &&
                                    candidate.GetParameters().Length == args.Length &&
                                    candidate.GetParameters()[1].ParameterType == valueKeyType);
            return method.Invoke(target, args);
        }

        private static bool IsValueKeyParameter(Type parameterType)
        {
            return parameterType.IsGenericType &&
                   parameterType.GetGenericTypeDefinition().FullName == "BC.Base.ValueKey`1";
        }

        private static void AssertThrowsInvalidOperation(TestDelegate action)
        {
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(action);
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
        }

        private static object GetStaticFieldValue(string typeName, string fieldName)
        {
            Type type = GetTypeByFullName(typeName);
            FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected static field: {typeName}.{fieldName}");
            return field.GetValue(null);
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
    }
}

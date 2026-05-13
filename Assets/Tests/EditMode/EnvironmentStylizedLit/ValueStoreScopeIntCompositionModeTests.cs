using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace BC.Rendering.Tests
{
    public sealed class ValueStoreScopeIntCompositionModeTests
    {
        [Test]
        public void RawAndNumericIntValueKeysUseTheCorrectCompositionMode()
        {
            Type valueStoreType = GetTypeByFullName("BC.Base.ValueStoreService");
            Type entityRefType = GetTypeByFullName("BC.Base.EntityRef");
            Type modifierTagType = GetTypeByFullName("BC.Base.ValueModifierTagId");
            Type valueKeysType = GetTypeByFullName("BC.Base.ValueKeys");
            Type runtimeKeysType = valueKeysType.GetNestedType("Runtime", BindingFlags.Public | BindingFlags.NonPublic);
            Type healthKeysType = valueKeysType.GetNestedType("Health", BindingFlags.Public | BindingFlags.NonPublic);

            object valueStore = Activator.CreateInstance(valueStoreType);
            object entity = Activator.CreateInstance(entityRefType, new object[] { 1u, 1 });
            object throwSequenceKey = runtimeKeysType.GetField("ThrowSequence", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            object maxHpKey = healthKeysType.GetField("MaxHP", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            object modifierTag = Activator.CreateInstance(modifierTagType, new object[] { 9001 });

            object rawHandle = InvokeGenericValueKeyMethod(valueStoreType, valueStore, "GetHandle", typeof(int), entity, throwSequenceKey);
            object numericHandle = InvokeGenericValueKeyMethod(valueStoreType, valueStore, "GetHandle", typeof(int), entity, maxHpKey);
            int rawInitialVersion = GetProperty<int>(rawHandle, "Version");
            int numericInitialVersion = GetProperty<int>(numericHandle, "Version");

            Assert.AreEqual(0, GetProperty<int>(rawHandle, "CurrentValue"));
            Assert.AreEqual(100, GetProperty<int>(numericHandle, "CurrentValue"));

            Assert.IsTrue((bool)InvokeGenericValueKeyMethod(valueStoreType, valueStore, "Set", typeof(int), entity, throwSequenceKey, 7));
            Assert.IsTrue((bool)InvokeGenericValueKeyMethod(valueStoreType, valueStore, "Set", typeof(int), entity, maxHpKey, 120));
            Assert.IsTrue((bool)InvokeMethod(valueStoreType, valueStore, "SetAdd", entity, maxHpKey, modifierTag, 10f));

            Assert.AreEqual(7, (int)InvokeGenericValueKeyMethod(valueStoreType, valueStore, "Get", typeof(int), entity, throwSequenceKey));
            Assert.AreEqual(130, (int)InvokeGenericValueKeyMethod(valueStoreType, valueStore, "Get", typeof(int), entity, maxHpKey));

            AssertHandleReportsChange(rawHandle, rawInitialVersion, 7);
            AssertHandleReportsChange(numericHandle, numericInitialVersion, 130);
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

        private static object InvokeGenericValueKeyMethod(Type declaringType, object instance, string methodName, Type genericTypeArgument, params object[] arguments)
        {
            MethodInfo method = FindGenericValueKeyMethod(declaringType, methodName, arguments.Length);
            return method.MakeGenericMethod(genericTypeArgument).Invoke(instance, arguments);
        }

        private static MethodInfo FindGenericValueKeyMethod(Type declaringType, string methodName, int parameterCount)
        {
            return declaringType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .First(method =>
                {
                    if (method.Name != methodName || !method.IsGenericMethodDefinition)
                        return false;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != parameterCount)
                        return false;

                    return parameters.Length >= 2 &&
                           parameters[1].ParameterType.IsGenericType &&
                           parameters[1].ParameterType.GetGenericTypeDefinition().FullName == "BC.Base.ValueKey`1";
                });
        }

        private static object InvokeMethod(Type declaringType, object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = declaringType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .First(candidate =>
                {
                    if (candidate.Name != methodName || candidate.GetParameters().Length != arguments.Length)
                        return false;

                    ParameterInfo[] parameters = candidate.GetParameters();

                    for (int index = 0; index < parameters.Length; index++)
                    {
                        object argument = arguments[index];
                        if (argument == null)
                            continue;

                        if (!parameters[index].ParameterType.IsInstanceOfType(argument))
                            return false;
                    }

                    return true;
                });

            return method.Invoke(instance, arguments);
        }

        private static T GetProperty<T>(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property: {propertyName}");
            return (T)property.GetValue(instance);
        }

        private static void AssertHandleReportsChange(object handle, int lastSeenVersion, int expectedValue)
        {
            MethodInfo tryGetChangedMethod = handle.GetType().GetMethod("TryGetChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(tryGetChangedMethod, "Expected TryGetChanged method on value watch handle.");

            object[] arguments = { lastSeenVersion, null };
            Assert.IsTrue((bool)tryGetChangedMethod.Invoke(handle, arguments));
            Assert.AreEqual(expectedValue, arguments[1]);
            Assert.AreEqual(GetProperty<int>(handle, "Version"), (int)arguments[0]);
        }
    }
}
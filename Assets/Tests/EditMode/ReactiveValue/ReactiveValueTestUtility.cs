using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BC.Base.Tests
{
    internal static class ReactiveValueTestUtility
    {
        public static Type GetTypeByFullName(string fullName)
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

        public static object CreateInstance(string fullTypeName, params object[] arguments)
        {
            Type type = GetTypeByFullName(fullTypeName);
            ConstructorInfo ctor = FindConstructor(type, arguments);
            Assert.IsNotNull(ctor, $"Expected constructor: {fullTypeName}");
            return ctor.Invoke(BuildInvokeArguments(ctor.GetParameters(), arguments));
        }

        public static object InvokeStatic(string fullTypeName, string methodName, params object[] arguments)
        {
            Type type = GetTypeByFullName(fullTypeName);
            MethodInfo method = FindMethod(type, methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, arguments);
            Assert.IsNotNull(method, $"Expected static method: {fullTypeName}.{methodName}");
            return method.Invoke(null, BuildInvokeArguments(method.GetParameters(), arguments));
        }

        public static object InvokeMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(instance.GetType(), methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, arguments);
            Assert.IsNotNull(method, $"Expected method: {instance.GetType().FullName}.{methodName}");
            return method.Invoke(instance, BuildInvokeArguments(method.GetParameters(), arguments));
        }

        public static object InvokeDeclaredMethod(Type ownerType, object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(ownerType, methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, arguments);
            Assert.IsNotNull(method, $"Expected declared method: {ownerType.FullName}.{methodName}");
            return method.Invoke(instance, BuildInvokeArguments(method.GetParameters(), arguments));
        }

        public static object GetFieldValue(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field: {fieldName}");
            return field.GetValue(instance);
        }

        public static object GetPropertyValue(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, $"Expected property: {propertyName}");
            return property.GetValue(instance);
        }

        public static ScriptableObject CreateDrawerHost()
        {
            Type hostType = GetTypeByFullName("BC.Editor.ReactiveValueDrawerTestHost");
            ScriptableObject host = ScriptableObject.CreateInstance(hostType);
            Assert.IsNotNull(host, "Expected ReactiveValueDrawerTestHost instance.");
            return host;
        }

        public static SerializedProperty FindRootProperty(SerializedObject serializedObject, string propertyPath)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            Assert.IsNotNull(property, $"Expected property: {propertyPath}");
            return property;
        }

        public static PropertyDrawer CreateDrawer(string fullTypeName)
        {
            Type drawerType = GetTypeByFullName(fullTypeName);
            PropertyDrawer drawer = Activator.CreateInstance(drawerType) as PropertyDrawer;
            Assert.IsNotNull(drawer, $"Expected property drawer: {fullTypeName}");
            return drawer;
        }

        public static object ParseEnum(string fullTypeName, string memberName)
        {
            return Enum.Parse(GetTypeByFullName(fullTypeName), memberName);
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
                resolved[index] = index < arguments.Length ? arguments[index] : Type.Missing;

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
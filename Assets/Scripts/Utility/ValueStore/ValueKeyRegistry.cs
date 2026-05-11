using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace BC.Base
{
    public readonly struct ValueKeyDescriptor : IEquatable<ValueKeyDescriptor>
    {
        private readonly object rawKey;

        internal ValueKeyDescriptor(
            ValueKeyId id,
            string path,
            Type valueType,
            object defaultValue,
            ValueCompositionMode compositionMode,
            object rawKey)
        {
            Id = id;
            Path = path ?? throw new ArgumentNullException(nameof(path));
            ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
            DefaultValue = defaultValue;
            CompositionMode = compositionMode;
            this.rawKey = rawKey ?? throw new ArgumentNullException(nameof(rawKey));
        }

        public ValueKeyId Id { get; }
        public string Path { get; }
        public Type ValueType { get; }
        public object DefaultValue { get; }
        public ValueCompositionMode CompositionMode { get; }
        public string TypeName => ValueType.Name;
        public string MenuPath => Path.Replace('.', '/');
        public string DisplayName => $"{Path} [{TypeName}]";

        public ValueKey<T> GetKey<T>()
        {
            if (ValueType != typeof(T))
            {
                throw new InvalidOperationException(
                    $"ValueKey type mismatch. Path={Path}, Expected={typeof(T).Name}, Actual={TypeName}");
            }

            return (ValueKey<T>)rawKey;
        }

        public bool Equals(ValueKeyDescriptor other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is ValueKeyDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Path} ({TypeName}, {CompositionMode})";
        }
    }

    public static class ValueKeyRegistry
    {
        private static readonly IReadOnlyList<ValueKeyDescriptor> allDescriptors = BuildDescriptors();
        private static readonly Dictionary<ValueKeyId, ValueKeyDescriptor> descriptorsById = BuildDescriptorMapById();
        private static readonly Dictionary<string, ValueKeyDescriptor> descriptorsByPath = BuildDescriptorMapByPath();

        public static IReadOnlyList<ValueKeyDescriptor> AllDescriptors => allDescriptors;

        public static IReadOnlyList<ValueKeyDescriptor> GetDescriptors(
            Type valueType = null,
            string pathPrefix = null)
        {
            if (valueType == null && string.IsNullOrWhiteSpace(pathPrefix))
                return allDescriptors;

            string normalizedPrefix = NormalizePathPrefix(pathPrefix);
            List<ValueKeyDescriptor> matches = new();

            for (int i = 0; i < allDescriptors.Count; i++)
            {
                ValueKeyDescriptor descriptor = allDescriptors[i];

                if (valueType != null && descriptor.ValueType != valueType)
                    continue;

                if (!PathMatchesPrefix(descriptor.Path, normalizedPrefix))
                    continue;

                matches.Add(descriptor);
            }

            return matches.AsReadOnly();
        }

        public static bool TryGetDescriptor(ValueKeyId id, out ValueKeyDescriptor descriptor)
        {
            return descriptorsById.TryGetValue(id, out descriptor);
        }

        public static bool TryGetDescriptor(string path, out ValueKeyDescriptor descriptor)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                descriptor = default;
                return false;
            }

            return descriptorsByPath.TryGetValue(path, out descriptor);
        }

        public static bool TryGetDescriptor(ValueKeyReference reference, out ValueKeyDescriptor descriptor)
        {
            if (reference.RawId != 0 && descriptorsById.TryGetValue(reference.Id, out descriptor))
                return true;

            if (!string.IsNullOrWhiteSpace(reference.Path) &&
                descriptorsByPath.TryGetValue(reference.Path, out descriptor))
                return true;

            descriptor = default;
            return false;
        }

        public static bool TryResolve<T>(ValueKeyReference reference, out ValueKey<T> key)
        {
            if (TryGetDescriptor(reference, out ValueKeyDescriptor descriptor) &&
                descriptor.ValueType == typeof(T))
            {
                key = descriptor.GetKey<T>();
                return true;
            }

            key = default;
            return false;
        }

        public static ValueKey<T> Resolve<T>(ValueKeyReference reference)
        {
            if (TryResolve(reference, out ValueKey<T> key))
                return key;

            if (TryGetDescriptor(reference, out ValueKeyDescriptor descriptor))
            {
                throw new InvalidOperationException(
                    $"ValueKey type mismatch. Path={descriptor.Path}, Expected={typeof(T).Name}, Actual={descriptor.TypeName}");
            }

            if (reference.IsAssigned)
            {
                throw new InvalidOperationException(
                    $"ValueKey could not be resolved. Id={reference.RawId}, Path={reference.Path}, ExpectedType={typeof(T).Name}");
            }

            throw new InvalidOperationException(
                $"ValueKey is not assigned. ExpectedType={typeof(T).Name}");
        }

        private static IReadOnlyList<ValueKeyDescriptor> BuildDescriptors()
        {
            List<ValueKeyDescriptor> descriptors = new();
            CollectDescriptors(typeof(ValueKeys), descriptors);
            descriptors.Sort((left, right) => StringComparer.Ordinal.Compare(left.Path, right.Path));
            return new ReadOnlyCollection<ValueKeyDescriptor>(descriptors);
        }

        private static Dictionary<ValueKeyId, ValueKeyDescriptor> BuildDescriptorMapById()
        {
            Dictionary<ValueKeyId, ValueKeyDescriptor> map = new();

            for (int i = 0; i < allDescriptors.Count; i++)
            {
                ValueKeyDescriptor descriptor = allDescriptors[i];

                if (!map.TryAdd(descriptor.Id, descriptor))
                {
                    throw new InvalidOperationException(
                        $"Duplicate ValueKey id detected. Id={descriptor.Id}, Path={descriptor.Path}");
                }
            }

            return map;
        }

        private static Dictionary<string, ValueKeyDescriptor> BuildDescriptorMapByPath()
        {
            Dictionary<string, ValueKeyDescriptor> map = new(StringComparer.Ordinal);

            for (int i = 0; i < allDescriptors.Count; i++)
            {
                ValueKeyDescriptor descriptor = allDescriptors[i];

                if (!map.TryAdd(descriptor.Path, descriptor))
                {
                    throw new InvalidOperationException(
                        $"Duplicate ValueKey path detected. Path={descriptor.Path}, Id={descriptor.Id}");
                }
            }

            return map;
        }

        private static void CollectDescriptors(Type hostType, List<ValueKeyDescriptor> descriptors)
        {
            const BindingFlags fieldFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
            const BindingFlags nestedTypeFlags = BindingFlags.Public | BindingFlags.NonPublic;

            FieldInfo[] fields = hostType.GetFields(fieldFlags);

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                Type fieldType = field.FieldType;

                if (!field.IsStatic || !fieldType.IsGenericType || fieldType.GetGenericTypeDefinition() != typeof(ValueKey<>))
                    continue;

                object rawKey = field.GetValue(null);
                Type valueType = fieldType.GetGenericArguments()[0];
                ValueKeyId id = ReadField<ValueKeyId>(fieldType, rawKey, nameof(ValueKey<int>.Id));
                string path = ReadField<string>(fieldType, rawKey, nameof(ValueKey<int>.Path));
                object defaultValue = ReadField<object>(fieldType, rawKey, nameof(ValueKey<int>.DefaultValue));
                ValueCompositionMode compositionMode =
                    ReadField<ValueCompositionMode>(fieldType, rawKey, nameof(ValueKey<int>.CompositionMode));

                ValidatePath(path, field);

                descriptors.Add(
                    new ValueKeyDescriptor(
                        id,
                        path,
                        valueType,
                        defaultValue,
                        compositionMode,
                        rawKey));
            }

            Type[] nestedTypes = hostType.GetNestedTypes(nestedTypeFlags);

            for (int i = 0; i < nestedTypes.Length; i++)
            {
                CollectDescriptors(nestedTypes[i], descriptors);
            }
        }

        private static TField ReadField<TField>(Type fieldType, object rawKey, string fieldName)
        {
            FieldInfo valueField = fieldType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);

            if (valueField == null)
            {
                throw new MissingFieldException(fieldType.FullName, fieldName);
            }

            object value = valueField.GetValue(rawKey);
            return (TField)value;
        }

        private static void ValidatePath(string path, FieldInfo sourceField)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException(
                    $"ValueKey path is empty. Field={sourceField.DeclaringType?.FullName}.{sourceField.Name}");
            }

            string[] segments = path.Split('.');

            for (int i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(segments[i]))
                {
                    throw new InvalidOperationException(
                        $"ValueKey path contains an empty segment. Path={path}, Field={sourceField.DeclaringType?.FullName}.{sourceField.Name}");
                }
            }
        }

        private static string NormalizePathPrefix(string pathPrefix)
        {
            if (string.IsNullOrWhiteSpace(pathPrefix))
                return null;

            return pathPrefix.Trim().Replace('/', '.').Trim('.');
        }

        private static bool PathMatchesPrefix(string path, string normalizedPrefix)
        {
            if (string.IsNullOrEmpty(normalizedPrefix))
                return true;

            return string.Equals(path, normalizedPrefix, StringComparison.Ordinal) ||
                   path.StartsWith(normalizedPrefix + ".", StringComparison.Ordinal);
        }
    }
}
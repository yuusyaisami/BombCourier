using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace BC.Base
{
    public readonly struct SignalDescriptor : IEquatable<SignalDescriptor>
    {
        internal SignalDescriptor(Signal signal)
        {
            Signal = signal;
        }

        public Signal Signal { get; }
        public SignalId Id => Signal.Id;
        public string Path => Signal.Path;
        public string MenuPath => Path.Replace('.', '/');
        public string DisplayName => Path;

        public bool Equals(SignalDescriptor other) => Id.Equals(other.Id);
        public override bool Equals(object obj) => obj is SignalDescriptor other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => $"{Path} ({Id})";
    }

    public static class SignalRegistry
    {
        private static readonly IReadOnlyList<SignalDescriptor> allDescriptors = BuildDescriptors();
        private static readonly Dictionary<SignalId, SignalDescriptor> descriptorsById = BuildDescriptorMapById();
        private static readonly Dictionary<string, SignalDescriptor> descriptorsByPath = BuildDescriptorMapByPath();

        public static IReadOnlyList<SignalDescriptor> AllDescriptors => allDescriptors;

        public static IReadOnlyList<SignalDescriptor> GetDescriptors(string pathPrefix = null)
        {
            if (string.IsNullOrWhiteSpace(pathPrefix))
                return allDescriptors;

            string normalizedPrefix = NormalizePathPrefix(pathPrefix);
            List<SignalDescriptor> matches = new();

            for (int i = 0; i < allDescriptors.Count; i++)
            {
                SignalDescriptor descriptor = allDescriptors[i];

                if (PathMatchesPrefix(descriptor.Path, normalizedPrefix))
                    matches.Add(descriptor);
            }

            return matches.AsReadOnly();
        }

        public static bool TryGetDescriptor(SignalId id, out SignalDescriptor descriptor)
        {
            return descriptorsById.TryGetValue(id, out descriptor);
        }

        public static bool TryGetDescriptor(string path, out SignalDescriptor descriptor)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                descriptor = default;
                return false;
            }

            return descriptorsByPath.TryGetValue(path, out descriptor);
        }

        public static bool TryGetDescriptor(KernelSignalReference reference, out SignalDescriptor descriptor)
        {
            return TryGetDescriptor(reference.RawId, reference.Path, out descriptor);
        }

        public static bool TryGetDescriptor(EntitySignalReference reference, out SignalDescriptor descriptor)
        {
            return TryGetDescriptor(reference.RawId, reference.Path, out descriptor);
        }

        private static bool TryGetDescriptor(int rawId, string path, out SignalDescriptor descriptor)
        {
            if (rawId != 0 && descriptorsById.TryGetValue(new SignalId(rawId), out descriptor))
                return true;

            if (!string.IsNullOrWhiteSpace(path) && descriptorsByPath.TryGetValue(path, out descriptor))
                return true;

            descriptor = default;
            return false;
        }

        private static IReadOnlyList<SignalDescriptor> BuildDescriptors()
        {
            List<SignalDescriptor> descriptors = new();
            CollectDescriptors(typeof(Signals), descriptors);
            descriptors.Sort((left, right) => StringComparer.Ordinal.Compare(left.Path, right.Path));
            return new ReadOnlyCollection<SignalDescriptor>(descriptors);
        }

        private static Dictionary<SignalId, SignalDescriptor> BuildDescriptorMapById()
        {
            Dictionary<SignalId, SignalDescriptor> map = new();

            for (int i = 0; i < allDescriptors.Count; i++)
            {
                SignalDescriptor descriptor = allDescriptors[i];

                if (!map.TryAdd(descriptor.Id, descriptor))
                {
                    throw new InvalidOperationException(
                        $"Duplicate Signal id detected. Id={descriptor.Id}, Path={descriptor.Path}");
                }
            }

            return map;
        }

        private static Dictionary<string, SignalDescriptor> BuildDescriptorMapByPath()
        {
            Dictionary<string, SignalDescriptor> map = new(StringComparer.Ordinal);

            for (int i = 0; i < allDescriptors.Count; i++)
            {
                SignalDescriptor descriptor = allDescriptors[i];

                if (!map.TryAdd(descriptor.Path, descriptor))
                {
                    throw new InvalidOperationException(
                        $"Duplicate Signal path detected. Path={descriptor.Path}, Id={descriptor.Id}");
                }
            }

            return map;
        }

        private static void CollectDescriptors(Type hostType, List<SignalDescriptor> descriptors)
        {
            const BindingFlags fieldFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
            const BindingFlags nestedTypeFlags = BindingFlags.Public | BindingFlags.NonPublic;

            FieldInfo[] fields = hostType.GetFields(fieldFlags);

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                if (!field.IsStatic || field.FieldType != typeof(Signal))
                    continue;

                Signal signal = (Signal)field.GetValue(null);
                ValidatePath(signal.Path, field);
                descriptors.Add(new SignalDescriptor(signal));
            }

            Type[] nestedTypes = hostType.GetNestedTypes(nestedTypeFlags);

            for (int i = 0; i < nestedTypes.Length; i++)
            {
                CollectDescriptors(nestedTypes[i], descriptors);
            }
        }

        private static void ValidatePath(string path, FieldInfo sourceField)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException(
                    $"Signal path is empty. Field={sourceField.DeclaringType?.FullName}.{sourceField.Name}");
            }

            string[] segments = path.Split('.');

            for (int i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(segments[i]))
                {
                    throw new InvalidOperationException(
                        $"Signal path contains an empty segment. Path={path}, Field={sourceField.DeclaringType?.FullName}.{sourceField.Name}");
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
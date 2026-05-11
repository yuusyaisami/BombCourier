using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace BC.Base
{
    public readonly struct EntityTagDescriptor : IEquatable<EntityTagDescriptor>
    {
        internal EntityTagDescriptor(EntityTag tag)
        {
            Tag = tag;
        }

        public EntityTag Tag { get; }
        public EntityTagId Id => Tag.Id;
        public string Path => Tag.Path;
        public string MenuPath => Path.Replace('.', '/');
        public string DisplayName => Path;

        public bool Equals(EntityTagDescriptor other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is EntityTagDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Path} ({Id})";
        }
    }

    public static class EntityTagRegistry
    {
        private static readonly IReadOnlyList<EntityTagDescriptor> allDescriptors = BuildDescriptors();
        private static readonly Dictionary<EntityTagId, EntityTagDescriptor> descriptorsById = BuildDescriptorMapById();
        private static readonly Dictionary<string, EntityTagDescriptor> descriptorsByPath = BuildDescriptorMapByPath();

        public static IReadOnlyList<EntityTagDescriptor> AllDescriptors => allDescriptors;

        public static IReadOnlyList<EntityTagDescriptor> GetDescriptors(string pathPrefix = null)
        {
            if (string.IsNullOrWhiteSpace(pathPrefix))
                return allDescriptors;

            string normalizedPrefix = NormalizePathPrefix(pathPrefix);
            List<EntityTagDescriptor> matches = new();

            for (int i = 0; i < allDescriptors.Count; i++)
            {
                EntityTagDescriptor descriptor = allDescriptors[i];

                if (PathMatchesPrefix(descriptor.Path, normalizedPrefix))
                    matches.Add(descriptor);
            }

            return matches.AsReadOnly();
        }

        public static bool TryGetDescriptor(EntityTagId id, out EntityTagDescriptor descriptor)
        {
            return descriptorsById.TryGetValue(id, out descriptor);
        }

        public static bool TryGetDescriptor(string path, out EntityTagDescriptor descriptor)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                descriptor = default;
                return false;
            }

            return descriptorsByPath.TryGetValue(path, out descriptor);
        }

        public static bool TryGetDescriptor(EntityTagReference reference, out EntityTagDescriptor descriptor)
        {
            if (reference.RawId != 0 && descriptorsById.TryGetValue(reference.Id, out descriptor))
                return true;

            if (!string.IsNullOrWhiteSpace(reference.Path) &&
                descriptorsByPath.TryGetValue(reference.Path, out descriptor))
                return true;

            descriptor = default;
            return false;
        }

        private static IReadOnlyList<EntityTagDescriptor> BuildDescriptors()
        {
            List<EntityTagDescriptor> descriptors = new();
            CollectDescriptors(typeof(EntityTags), descriptors);
            descriptors.Sort((left, right) => StringComparer.Ordinal.Compare(left.Path, right.Path));
            return new ReadOnlyCollection<EntityTagDescriptor>(descriptors);
        }

        private static Dictionary<EntityTagId, EntityTagDescriptor> BuildDescriptorMapById()
        {
            Dictionary<EntityTagId, EntityTagDescriptor> map = new();

            for (int i = 0; i < allDescriptors.Count; i++)
            {
                EntityTagDescriptor descriptor = allDescriptors[i];

                if (!map.TryAdd(descriptor.Id, descriptor))
                {
                    throw new InvalidOperationException(
                        $"Duplicate EntityTag id detected. Id={descriptor.Id}, Path={descriptor.Path}");
                }
            }

            return map;
        }

        private static Dictionary<string, EntityTagDescriptor> BuildDescriptorMapByPath()
        {
            Dictionary<string, EntityTagDescriptor> map = new(StringComparer.Ordinal);

            for (int i = 0; i < allDescriptors.Count; i++)
            {
                EntityTagDescriptor descriptor = allDescriptors[i];

                if (!map.TryAdd(descriptor.Path, descriptor))
                {
                    throw new InvalidOperationException(
                        $"Duplicate EntityTag path detected. Path={descriptor.Path}, Id={descriptor.Id}");
                }
            }

            return map;
        }

        private static void CollectDescriptors(Type hostType, List<EntityTagDescriptor> descriptors)
        {
            const BindingFlags fieldFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
            const BindingFlags nestedTypeFlags = BindingFlags.Public | BindingFlags.NonPublic;

            FieldInfo[] fields = hostType.GetFields(fieldFlags);

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                if (!field.IsStatic || field.FieldType != typeof(EntityTag))
                    continue;

                EntityTag tag = (EntityTag)field.GetValue(null);
                ValidatePath(tag.Path, field);
                descriptors.Add(new EntityTagDescriptor(tag));
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
                    $"EntityTag path is empty. Field={sourceField.DeclaringType?.FullName}.{sourceField.Name}");
            }

            string[] segments = path.Split('.');

            for (int i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(segments[i]))
                {
                    throw new InvalidOperationException(
                        $"EntityTag path contains an empty segment. Path={path}, Field={sourceField.DeclaringType?.FullName}.{sourceField.Name}");
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

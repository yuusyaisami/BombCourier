using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace BC.Base
{
    public static class CharacterIdRegistry
    {
        private static readonly IReadOnlyList<CharacterIdDescriptor> allDescriptors = BuildDescriptors();
        private static readonly Dictionary<CharacterId, CharacterIdDescriptor> descriptorsById = BuildDescriptorMapById();
        private static readonly Dictionary<string, CharacterIdDescriptor> descriptorsByPath = BuildDescriptorMapByPath();

        public static IReadOnlyList<CharacterIdDescriptor> AllDescriptors => allDescriptors;

        public static bool TryGetDescriptor(CharacterId id, out CharacterIdDescriptor descriptor)
        {
            return descriptorsById.TryGetValue(id, out descriptor);
        }

        public static bool TryGetDescriptor(string path, out CharacterIdDescriptor descriptor)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                descriptor = default;
                return false;
            }

            return descriptorsByPath.TryGetValue(path, out descriptor);
        }

        public static bool TryGetDescriptor(CharacterIdReference reference, out CharacterIdDescriptor descriptor)
        {
            if (reference.RawId != 0 && descriptorsById.TryGetValue(reference.Id, out descriptor))
                return true;

            if (!string.IsNullOrWhiteSpace(reference.Path) &&
                descriptorsByPath.TryGetValue(reference.Path, out descriptor))
            {
                return true;
            }

            descriptor = default;
            return false;
        }

        private static IReadOnlyList<CharacterIdDescriptor> BuildDescriptors()
        {
            List<CharacterIdDescriptor> descriptors = new();
            CollectDescriptors(typeof(CharacterIds), descriptors, string.Empty);
            descriptors.Sort((left, right) => StringComparer.Ordinal.Compare(left.Path, right.Path));
            return new ReadOnlyCollection<CharacterIdDescriptor>(descriptors);
        }

        private static Dictionary<CharacterId, CharacterIdDescriptor> BuildDescriptorMapById()
        {
            Dictionary<CharacterId, CharacterIdDescriptor> map = new();

            for (int i = 0; i < allDescriptors.Count; i++)
            {
                CharacterIdDescriptor descriptor = allDescriptors[i];

                if (!map.TryAdd(descriptor.Id, descriptor))
                {
                    throw new InvalidOperationException(
                        $"Duplicate CharacterId id detected. Id={descriptor.Id}, Path={descriptor.Path}");
                }
            }

            return map;
        }

        private static Dictionary<string, CharacterIdDescriptor> BuildDescriptorMapByPath()
        {
            Dictionary<string, CharacterIdDescriptor> map = new(StringComparer.Ordinal);

            for (int i = 0; i < allDescriptors.Count; i++)
            {
                CharacterIdDescriptor descriptor = allDescriptors[i];

                if (!map.TryAdd(descriptor.Path, descriptor))
                {
                    throw new InvalidOperationException(
                        $"Duplicate CharacterId path detected. Path={descriptor.Path}, Id={descriptor.Id}");
                }
            }

            return map;
        }

        private static void CollectDescriptors(Type type, ICollection<CharacterIdDescriptor> descriptors, string pathPrefix)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo[] fields = type.GetFields(flags);

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                if (field.FieldType != typeof(CharacterId))
                    continue;

                CharacterId id = (CharacterId)field.GetValue(null);

                if (!id.IsValid)
                    continue;

                string path = string.IsNullOrWhiteSpace(pathPrefix)
                    ? field.Name
                    : $"{pathPrefix}.{field.Name}";
                string displayName = ResolveDisplayName(field, path);
                descriptors.Add(new CharacterIdDescriptor(id, path, displayName));
            }

            Type[] nestedTypes = type.GetNestedTypes(flags);

            for (int i = 0; i < nestedTypes.Length; i++)
            {
                Type nested = nestedTypes[i];

                if (!nested.IsAbstract || !nested.IsSealed)
                    continue;

                string nestedPath = string.IsNullOrWhiteSpace(pathPrefix)
                    ? nested.Name
                    : $"{pathPrefix}.{nested.Name}";

                CollectDescriptors(nested, descriptors, nestedPath);
            }
        }

        private static string ResolveDisplayName(FieldInfo field, string path)
        {
            CharacterDisplayNameAttribute displayNameAttribute =
                field?.GetCustomAttribute<CharacterDisplayNameAttribute>(true);

            if (displayNameAttribute != null && !string.IsNullOrWhiteSpace(displayNameAttribute.DisplayName))
                return displayNameAttribute.DisplayName.Trim();

            return ObjectNamesUtility.NicifyPath(path);
        }

        private static class ObjectNamesUtility
        {
            internal static string NicifyPath(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    return string.Empty;

                string[] segments = path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < segments.Length; i++)
                    segments[i] = NicifyToken(segments[i]);

                return string.Join(" / ", segments);
            }

            private static string NicifyToken(string token)
            {
                if (string.IsNullOrWhiteSpace(token))
                    return string.Empty;

                StringBuilder builder = new(token.Length + 8);
                char previous = '\0';

                for (int i = 0; i < token.Length; i++)
                {
                    char current = token[i];

                    if (i > 0 &&
                        char.IsUpper(current) &&
                        (char.IsLower(previous) || char.IsDigit(previous)))
                    {
                        builder.Append(' ');
                    }
                    else if (current == '_')
                    {
                        builder.Append(' ');
                        previous = current;
                        continue;
                    }

                    builder.Append(current);
                    previous = current;
                }

                return builder.ToString().Trim();
            }
        }
    }
}

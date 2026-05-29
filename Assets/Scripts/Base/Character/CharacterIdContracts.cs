using System;
using UnityEngine;

namespace BC.Base
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class CharacterDisplayNameAttribute : Attribute
    {
        public CharacterDisplayNameAttribute(string displayName)
        {
            DisplayName = displayName ?? string.Empty;
        }

        public string DisplayName { get; }
    }

    public readonly struct CharacterId : IEquatable<CharacterId>
    {
        public readonly int Value;
        public bool IsValid => Value != 0;

        public CharacterId(int value)
        {
            Value = value;
        }

        public bool Equals(CharacterId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(CharacterId left, CharacterId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CharacterId left, CharacterId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct CharacterIdDescriptor
    {
        public readonly CharacterId Id;
        public readonly string Path;
        public readonly string DisplayName;

        public CharacterIdDescriptor(CharacterId id, string path, string displayName)
        {
            Id = id;
            Path = path ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }
    }

    [Serializable]
    public struct CharacterIdReference : IEquatable<CharacterIdReference>
    {
        [SerializeField] private int id;
        [SerializeField] private string path;

        public CharacterId Id => new(id);
        public int RawId => id;
        public string Path => path;
        public bool IsAssigned => id != 0 || !string.IsNullOrWhiteSpace(path);

        public static CharacterIdReference From(CharacterId id)
        {
            if (CharacterIdRegistry.TryGetDescriptor(id, out CharacterIdDescriptor descriptor))
            {
                return new CharacterIdReference
                {
                    id = descriptor.Id.Value,
                    path = descriptor.Path,
                };
            }

            return new CharacterIdReference
            {
                id = id.Value,
                path = string.Empty,
            };
        }

        public bool Equals(CharacterIdReference other)
        {
            return id == other.id && string.Equals(path, other.path, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterIdReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (id * 397) ^ (path != null ? StringComparer.Ordinal.GetHashCode(path) : 0);
            }
        }

        public override string ToString()
        {
            if (CharacterIdRegistry.TryGetDescriptor(this, out CharacterIdDescriptor descriptor))
                return string.IsNullOrWhiteSpace(descriptor.DisplayName) ? descriptor.Path : descriptor.DisplayName;

            if (!string.IsNullOrWhiteSpace(path))
                return path;

            return id != 0 ? id.ToString() : "(None)";
        }
    }

    // Add project-specific character IDs here. Registry picks up nested static CharacterId fields.
    public static class CharacterIds
    {
        public static class Npc
        {
            [CharacterDisplayName("バニラ")]
            public static readonly CharacterId Vanilla = new(2001);

            [CharacterDisplayName("モニカ")]
            public static readonly CharacterId Monicar = new(2002);

            [CharacterDisplayName("ねずみ")]
            public static readonly CharacterId Nezumi = new(2003);

            [CharacterDisplayName("ヤクモ")]
            public static readonly CharacterId Yakumo = new(2004);
        }
    }
}

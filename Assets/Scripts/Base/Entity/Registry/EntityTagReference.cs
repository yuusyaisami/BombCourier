using System;
using UnityEngine;

namespace BC.Base
{
    [Serializable]
    public struct EntityTagReference : IEquatable<EntityTagReference>
    {
        [SerializeField] private int id;
        [SerializeField] private string path;

        public EntityTagId Id => new EntityTagId(id);
        public int RawId => id;
        public string Path => path;
        public bool IsAssigned => id != 0 || !string.IsNullOrEmpty(path);

        public static EntityTagReference From(EntityTag tag)
        {
            return new EntityTagReference
            {
                id = tag.Id.Value,
                path = tag.Path
            };
        }

        public static EntityTagReference From(EntityTagId tagId)
        {
            if (EntityTagRegistry.TryGetDescriptor(tagId, out EntityTagDescriptor descriptor))
                return From(descriptor.Tag);

            return new EntityTagReference
            {
                id = tagId.Value,
                path = string.Empty
            };
        }

        public bool TryResolve(out EntityTag tag)
        {
            if (EntityTagRegistry.TryGetDescriptor(this, out EntityTagDescriptor descriptor))
            {
                tag = descriptor.Tag;
                return true;
            }

            tag = default;
            return false;
        }

        public EntityTag Resolve()
        {
            if (TryResolve(out EntityTag tag))
                return tag;

            if (IsAssigned)
                throw new InvalidOperationException($"EntityTag could not be resolved. Id={id}, Path={path}");

            throw new InvalidOperationException("EntityTag is not assigned.");
        }

        public bool Matches(EntityTagId tagId)
        {
            return IsAssigned && Id.Equals(tagId);
        }

        public bool Equals(EntityTagReference other)
        {
            return id == other.id && string.Equals(path, other.path, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is EntityTagReference other && Equals(other);
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
            if (EntityTagRegistry.TryGetDescriptor(this, out EntityTagDescriptor descriptor))
                return descriptor.DisplayName;

            if (!string.IsNullOrEmpty(path))
                return path;

            return id != 0 ? id.ToString() : "(None)";
        }
    }
}

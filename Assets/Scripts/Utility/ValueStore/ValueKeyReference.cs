using System;
using UnityEngine;

namespace BC.Base
{
    [Serializable]
    public struct ValueKeyReference : IEquatable<ValueKeyReference>
    {
        [SerializeField] private int id;
        [SerializeField] private string path;
        [SerializeField] private string valueTypeName;

        public ValueKeyId Id => new ValueKeyId(id);
        public int RawId => id;
        public string Path => path;
        public string ValueTypeName => valueTypeName;
        public bool IsAssigned => id != 0 || !string.IsNullOrEmpty(path);

        public Type ValueType
        {
            get
            {
                if (string.IsNullOrEmpty(valueTypeName))
                    return null;

                return Type.GetType(valueTypeName);
            }
        }

        public static ValueKeyReference From<T>(ValueKey<T> key)
        {
            return new ValueKeyReference
            {
                id = key.Id.Value,
                path = key.Path,
                valueTypeName = typeof(T).AssemblyQualifiedName
            };
        }

        public bool TryResolve(out ValueKeyDescriptor descriptor)
        {
            return ValueKeyRegistry.TryGetDescriptor(this, out descriptor);
        }

        public bool TryResolve<T>(out ValueKey<T> key)
        {
            return ValueKeyRegistry.TryResolve(this, out key);
        }

        public ValueKey<T> Resolve<T>()
        {
            return ValueKeyRegistry.Resolve<T>(this);
        }

        public bool Equals(ValueKeyReference other)
        {
            return id == other.id &&
                   string.Equals(path, other.path, StringComparison.Ordinal) &&
                   string.Equals(valueTypeName, other.valueTypeName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ValueKeyReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = id;
                hash = (hash * 397) ^ (path != null ? StringComparer.Ordinal.GetHashCode(path) : 0);
                hash = (hash * 397) ^ (valueTypeName != null ? StringComparer.Ordinal.GetHashCode(valueTypeName) : 0);
                return hash;
            }
        }

        public override string ToString()
        {
            if (TryResolve(out ValueKeyDescriptor descriptor))
                return descriptor.DisplayName;

            if (!string.IsNullOrEmpty(path))
                return path;

            return "(None)";
        }
    }
}
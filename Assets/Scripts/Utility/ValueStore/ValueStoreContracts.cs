using System;
namespace BC.Base
{
    public readonly struct ValueKeyId : IEquatable<ValueKeyId>
    {
        public readonly int Value;

        public ValueKeyId(int value)
        {
            Value = value;
        }

        public bool Equals(ValueKeyId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ValueKeyId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public readonly struct ValueKey<T> : IEquatable<ValueKey<T>>
    {
        public readonly ValueKeyId Id;
        public readonly string Path;
        public readonly T DefaultValue;

        public ValueKey(ValueKeyId id, string path, T defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("ValueKey path is null or empty.", nameof(path));

            Id = id;
            Path = path;
            DefaultValue = defaultValue;
        }

        public bool Equals(ValueKey<T> other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is ValueKey<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
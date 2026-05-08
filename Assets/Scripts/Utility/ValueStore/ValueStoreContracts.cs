using System;
namespace BC.Base
{
    public enum ValueCompositionMode
    {
        Raw = 0,

        // int / float 専用
        NumericAddMul = 10,

        // bool 専用
        BoolAnd = 20,
        BoolOr = 21,

        // 将来用
        OverrideByTag = 30,
    }
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

    public readonly struct ValueModifierTagId : IEquatable<ValueModifierTagId>
    {
        public readonly int Value;

        public ValueModifierTagId(int value)
        {
            Value = value;
        }

        public bool Equals(ValueModifierTagId other) => Value == other.Value;

        public override bool Equals(object obj)
        {
            return obj is ValueModifierTagId other && Equals(other);
        }

        public override int GetHashCode() => Value;

        public override string ToString() => Value.ToString();
    }

    public readonly struct ValueKey<T> : IEquatable<ValueKey<T>>
    {
        public readonly ValueKeyId Id;
        public readonly string Path;
        public readonly T DefaultValue;
        public readonly ValueCompositionMode CompositionMode;

        public ValueKey(
            ValueKeyId id,
            string path,
            T defaultValue = default,
            ValueCompositionMode compositionMode = ValueCompositionMode.Raw)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("ValueKey path is null or empty.", nameof(path));

            Id = id;
            Path = path;
            DefaultValue = defaultValue;
            CompositionMode = compositionMode;
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
            return $"{Path} ({typeof(T).Name}, {CompositionMode})";
        }
    }
}
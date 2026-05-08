using System;

namespace BC.Base
{

    internal abstract class ValueSlot
    {
        public ValueKeyId KeyId { get; }
        public Type ValueType { get; }
        public int Revision { get; protected set; } = 1;


        protected ValueSlot(ValueKeyId keyId, Type valueType)
        {
            KeyId = keyId;
            ValueType = valueType;
        }
    }
    internal sealed class RawValueSlot<T> : ValueSlot
    {
        private T value;

        public RawValueSlot(ValueKey<T> key)
            : base(key.Id, typeof(T))
        {
            value = key.DefaultValue;
        }

        public T Get()
        {
            return value;
        }

        public bool Set(T next)
        {
            if (Equals(value, next))
                return false;

            value = next;
            Revision++;
            return true;
        }
    }
    internal sealed class FloatNumericValueSlot : ValueSlot
    {
        private float baseValue;
        private readonly NumericModifierSet modifiers = new();

        public FloatNumericValueSlot(ValueKey<float> key)
            : base(key.Id, typeof(float))
        {
            baseValue = key.DefaultValue;
        }

        public float Get()
        {
            return modifiers.Evaluate(baseValue);
        }

        public float GetBase()
        {
            return baseValue;
        }

        public bool SetBase(float value)
        {
            if (baseValue.Equals(value))
                return false;

            baseValue = value;
            Revision++;
            return true;
        }

        public bool SetAdd(ValueModifierTagId tag, float value)
        {
            if (!modifiers.SetAdd(tag, value))
                return false;

            Revision++;
            return true;
        }

        public bool SetMul(ValueModifierTagId tag, float value)
        {
            if (!modifiers.SetMul(tag, value))
                return false;

            Revision++;
            return true;
        }

        public bool RemoveAdd(ValueModifierTagId tag)
        {
            if (!modifiers.RemoveAdd(tag))
                return false;

            Revision++;
            return true;
        }

        public bool RemoveMul(ValueModifierTagId tag)
        {
            if (!modifiers.RemoveMul(tag))
                return false;

            Revision++;
            return true;
        }
    }

    internal sealed class IntNumericValueSlot : ValueSlot
    {
        private int baseValue;
        private readonly NumericModifierSet modifiers = new();

        public IntNumericValueSlot(ValueKey<int> key)
            : base(key.Id, typeof(int))
        {
            baseValue = key.DefaultValue;
        }

        public int Get()
        {
            float evaluated = modifiers.Evaluate(baseValue);
            return (int)Math.Round(evaluated, MidpointRounding.AwayFromZero);
        }

        public int GetBase()
        {
            return baseValue;
        }

        public bool SetBase(int value)
        {
            if (baseValue == value)
                return false;

            baseValue = value;
            Revision++;
            return true;
        }

        public bool SetAdd(ValueModifierTagId tag, float value)
        {
            if (!modifiers.SetAdd(tag, value))
                return false;

            Revision++;
            return true;
        }

        public bool SetMul(ValueModifierTagId tag, float value)
        {
            if (!modifiers.SetMul(tag, value))
                return false;

            Revision++;
            return true;
        }

        public bool RemoveAdd(ValueModifierTagId tag)
        {
            if (!modifiers.RemoveAdd(tag))
                return false;

            Revision++;
            return true;
        }

        public bool RemoveMul(ValueModifierTagId tag)
        {
            if (!modifiers.RemoveMul(tag))
                return false;

            Revision++;
            return true;
        }
    }

}
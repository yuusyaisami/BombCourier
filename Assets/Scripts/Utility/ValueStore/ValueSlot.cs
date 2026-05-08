using System;
using System.Collections.Generic;

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

    internal sealed class BoolCompositeValueSlot : ValueSlot
    {
        private bool baseValue;
        private readonly ValueCompositionMode mode;
        private readonly Dictionary<ValueModifierTagId, bool> modifiers = new();

        public BoolCompositeValueSlot(ValueKey<bool> key)
            : base(key.Id, typeof(bool))
        {
            if (key.CompositionMode != ValueCompositionMode.BoolAnd &&
                key.CompositionMode != ValueCompositionMode.BoolOr)
            {
                throw new InvalidOperationException(
                    $"Invalid bool composition mode. Path={key.Path}, Mode={key.CompositionMode}");
            }

            baseValue = key.DefaultValue;
            mode = key.CompositionMode;
        }

        public bool Get()
        {
            if (mode == ValueCompositionMode.BoolAnd)
            {
                bool result = baseValue;

                foreach (bool value in modifiers.Values)
                {
                    result &= value;

                    if (!result)
                        return false;
                }

                return result;
            }

            if (mode == ValueCompositionMode.BoolOr)
            {
                bool result = baseValue;

                foreach (bool value in modifiers.Values)
                {
                    result |= value;

                    if (result)
                        return true;
                }

                return result;
            }

            throw new InvalidOperationException($"Unsupported bool composition mode: {mode}");
        }

        public bool SetBase(bool value)
        {
            if (baseValue == value)
                return false;

            baseValue = value;
            Revision++;
            return true;
        }

        public bool SetModifier(ValueModifierTagId tag, bool value)
        {
            if (modifiers.TryGetValue(tag, out bool current) && current == value)
                return false;

            modifiers[tag] = value;
            Revision++;
            return true;
        }

        public bool RemoveModifier(ValueModifierTagId tag)
        {
            if (!modifiers.Remove(tag))
                return false;

            Revision++;
            return true;
        }

        public void ClearModifiers()
        {
            if (modifiers.Count == 0)
                return;

            modifiers.Clear();
            Revision++;
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
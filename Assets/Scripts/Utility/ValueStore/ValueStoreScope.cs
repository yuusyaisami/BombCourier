using System;
using System.Collections.Generic;

namespace BC.Base
{
    internal sealed class ValueStoreScope
    {
        private readonly Dictionary<ValueKeyId, ValueSlot> slotsByKey = new();

        public T Get<T>(ValueKeyReference key)
        {
            return Get(key.Resolve<T>());
        }

        public T Get<T>(ValueKey<T> key)
        {
            if (typeof(T) == typeof(float))
            {
                var slot = GetOrCreateFloatSlot(UnsafeCastKey<float, T>(key));
                object result = slot.Get();
                return (T)result;
            }

            if (typeof(T) == typeof(int))
            {
                var slot = GetOrCreateIntSlot(UnsafeCastKey<int, T>(key));
                object result = slot.Get();
                return (T)result;
            }

            if (typeof(T) == typeof(bool))
            {
                var slot = GetOrCreateBoolSlot(UnsafeCastKey<bool, T>(key));
                object result = slot.Get();
                return (T)result;
            }

            var raw = GetOrCreateRawSlot(key);
            return raw.Get();
        }

        public bool Set<T>(ValueKeyReference key, T value)
        {
            return Set(key.Resolve<T>(), value);
        }

        public bool Set<T>(ValueKey<T> key, T value)
        {
            if (typeof(T) == typeof(float))
            {
                var slot = GetOrCreateFloatSlot(UnsafeCastKey<float, T>(key));
                return slot.SetBase((float)(object)value);
            }

            if (typeof(T) == typeof(int))
            {
                var slot = GetOrCreateIntSlot(UnsafeCastKey<int, T>(key));
                return slot.SetBase((int)(object)value);
            }

            if (typeof(T) == typeof(bool))
            {
                var slot = GetOrCreateBoolSlot(UnsafeCastKey<bool, T>(key));
                return slot.SetBase((bool)(object)value);
            }

            var raw = GetOrCreateRawSlot(key);
            return raw.Set(value);
        }

        public bool SetBoolModifier(ValueKeyReference key, ValueModifierTagId tag, bool value)
        {
            return SetBoolModifier(key.Resolve<bool>(), tag, value);
        }

        public bool SetBoolModifier(ValueKey<bool> key, ValueModifierTagId tag, bool value)
        {
            return GetOrCreateBoolSlot(key).SetModifier(tag, value);
        }

        public bool RemoveBoolModifier(ValueKeyReference key, ValueModifierTagId tag)
        {
            return RemoveBoolModifier(key.Resolve<bool>(), tag);
        }

        public bool RemoveBoolModifier(ValueKey<bool> key, ValueModifierTagId tag)
        {
            return GetOrCreateBoolSlot(key).RemoveModifier(tag);
        }

        public bool SetAdd(ValueKeyReference key, ValueModifierTagId tag, float value)
        {
            return ExecuteNumericModifier(key, nameof(SetAdd),
                floatKey => SetAdd(floatKey, tag, value),
                intKey => SetAdd(intKey, tag, value));
        }

        public bool SetAdd(ValueKey<float> key, ValueModifierTagId tag, float value)
        {
            return GetOrCreateFloatSlot(key).SetAdd(tag, value);
        }

        public bool SetAdd(ValueKey<int> key, ValueModifierTagId tag, float value)
        {
            return GetOrCreateIntSlot(key).SetAdd(tag, value);
        }

        public bool SetMul(ValueKeyReference key, ValueModifierTagId tag, float value)
        {
            return ExecuteNumericModifier(key, nameof(SetMul),
                floatKey => SetMul(floatKey, tag, value),
                intKey => SetMul(intKey, tag, value));
        }

        public bool SetMul(ValueKey<float> key, ValueModifierTagId tag, float value)
        {
            return GetOrCreateFloatSlot(key).SetMul(tag, value);
        }

        public bool SetMul(ValueKey<int> key, ValueModifierTagId tag, float value)
        {
            return GetOrCreateIntSlot(key).SetMul(tag, value);
        }

        public bool RemoveAdd(ValueKeyReference key, ValueModifierTagId tag)
        {
            return ExecuteNumericModifier(key, nameof(RemoveAdd),
                floatKey => RemoveAdd(floatKey, tag),
                intKey => RemoveAdd(intKey, tag));
        }

        public bool RemoveAdd(ValueKey<float> key, ValueModifierTagId tag)
        {
            return GetOrCreateFloatSlot(key).RemoveAdd(tag);
        }

        public bool RemoveAdd(ValueKey<int> key, ValueModifierTagId tag)
        {
            return GetOrCreateIntSlot(key).RemoveAdd(tag);
        }

        public bool RemoveMul(ValueKeyReference key, ValueModifierTagId tag)
        {
            return ExecuteNumericModifier(key, nameof(RemoveMul),
                floatKey => RemoveMul(floatKey, tag),
                intKey => RemoveMul(intKey, tag));
        }

        public bool RemoveMul(ValueKey<float> key, ValueModifierTagId tag)
        {
            return GetOrCreateFloatSlot(key).RemoveMul(tag);
        }

        public bool RemoveMul(ValueKey<int> key, ValueModifierTagId tag)
        {
            return GetOrCreateIntSlot(key).RemoveMul(tag);
        }

        private RawValueSlot<T> GetOrCreateRawSlot<T>(ValueKey<T> key)
        {
            if (slotsByKey.TryGetValue(key.Id, out ValueSlot slot))
            {
                if (key.CompositionMode != ValueCompositionMode.Raw)
                    throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.Raw}, Actual={key.CompositionMode}");
                if (slot is RawValueSlot<T> typed)
                    return typed;

                throw CreateSlotTypeMismatch(key, slot);
            }

            if (key.CompositionMode != ValueCompositionMode.Raw)
                throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.Raw}, Actual={key.CompositionMode}");

            var created = new RawValueSlot<T>(key);
            slotsByKey.Add(key.Id, created);
            return created;
        }

        private FloatNumericValueSlot GetOrCreateFloatSlot(ValueKey<float> key)
        {
            if (slotsByKey.TryGetValue(key.Id, out ValueSlot slot))
            {
                if (key.CompositionMode != ValueCompositionMode.NumericAddMul)
                    throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");
                if (slot is FloatNumericValueSlot typed)
                    return typed;
                throw CreateSlotTypeMismatch(key, slot);
            }

            if (key.CompositionMode != ValueCompositionMode.NumericAddMul)
                throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");

            var created = new FloatNumericValueSlot(key);
            slotsByKey.Add(key.Id, created);
            return created;
        }

        private IntNumericValueSlot GetOrCreateIntSlot(ValueKey<int> key)
        {
            if (slotsByKey.TryGetValue(key.Id, out ValueSlot slot))
            {
                if (key.CompositionMode != ValueCompositionMode.NumericAddMul)
                    throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");
                if (slot is IntNumericValueSlot typed)
                    return typed;

                throw CreateSlotTypeMismatch(key, slot);
            }

            if (key.CompositionMode != ValueCompositionMode.NumericAddMul)
                throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.NumericAddMul}, Actual={key.CompositionMode}");

            var created = new IntNumericValueSlot(key);
            slotsByKey.Add(key.Id, created);
            return created;
        }

        private BoolCompositeValueSlot GetOrCreateBoolSlot(ValueKey<bool> key)
        {
            if (slotsByKey.TryGetValue(key.Id, out ValueSlot slot))
            {
                if (key.CompositionMode != ValueCompositionMode.BoolAnd &&
                    key.CompositionMode != ValueCompositionMode.BoolOr)
                    throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.BoolAnd} or {ValueCompositionMode.BoolOr}, Actual={key.CompositionMode}");
                if (slot is BoolCompositeValueSlot typed)
                    return typed;

                throw CreateSlotTypeMismatch(key, slot);
            }

            if (key.CompositionMode != ValueCompositionMode.BoolAnd &&
                key.CompositionMode != ValueCompositionMode.BoolOr)
                throw new InvalidOperationException($"Value key composition mode mismatch. Path={key.Path}, Key={key.Id}, Expected={ValueCompositionMode.BoolAnd} or {ValueCompositionMode.BoolOr}, Actual={key.CompositionMode}");

            var created = new BoolCompositeValueSlot(key);
            slotsByKey.Add(key.Id, created);
            return created;
        }

        private static InvalidOperationException CreateSlotTypeMismatch<T>(ValueKey<T> key, ValueSlot slot)
        {
            return new InvalidOperationException(
                $"Value slot type mismatch. Path={key.Path}, Key={key.Id}, Expected={typeof(T).Name}, ActualSlot={slot.GetType().Name}");
        }

        private static ValueKey<TTarget> UnsafeCastKey<TTarget, TSource>(ValueKey<TSource> key)
        {
            return new ValueKey<TTarget>(
                key.Id,
                key.Path,
                (TTarget)(object)key.DefaultValue,
                key.CompositionMode
            );
        }

        private static InvalidOperationException CreateUnsupportedNumericModifierException(
            string operation,
            ValueKeyReference key)
        {
            if (key.TryResolve(out ValueKeyDescriptor descriptor))
            {
                return new InvalidOperationException(
                    $"{operation} supports only int or float ValueKeys. Path={descriptor.Path}, Actual={descriptor.TypeName}");
            }

            return new InvalidOperationException(
                $"{operation} could not resolve ValueKey. Id={key.RawId}, Path={key.Path}");
        }

        private static bool ExecuteNumericModifier(
            ValueKeyReference key,
            string operation,
            Func<ValueKey<float>, bool> floatHandler,
            Func<ValueKey<int>, bool> intHandler)
        {
            if (key.TryResolve(out ValueKeyDescriptor descriptor))
            {
                if (descriptor.ValueType == typeof(float))
                    return floatHandler(descriptor.GetKey<float>());

                if (descriptor.ValueType == typeof(int))
                    return intHandler(descriptor.GetKey<int>());
            }

            throw CreateUnsupportedNumericModifierException(operation, key);
        }
    }
}
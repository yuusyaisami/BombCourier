using System;
using System.Collections.Generic;

namespace BC.Base
{
    public sealed class EntityValueStore
    {
        private readonly Dictionary<ValueKeyId, ValueSlot> slotsByKey = new();

        public EntityRef Entity { get; }

        public EntityValueStore(EntityRef entity)
        {
            Entity = entity;
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

            var raw = GetOrCreateRawSlot(key);
            return raw.Get();
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

            var raw = GetOrCreateRawSlot(key);
            return raw.Set(value);
        }

        public bool SetAdd(ValueKey<float> key, ValueModifierTagId tag, float value)
        {
            return GetOrCreateFloatSlot(key).SetAdd(tag, value);
        }

        public bool SetMul(ValueKey<float> key, ValueModifierTagId tag, float value)
        {
            return GetOrCreateFloatSlot(key).SetMul(tag, value);
        }

        public bool RemoveAdd(ValueKey<float> key, ValueModifierTagId tag)
        {
            return GetOrCreateFloatSlot(key).RemoveAdd(tag);
        }

        public bool RemoveMul(ValueKey<float> key, ValueModifierTagId tag)
        {
            return GetOrCreateFloatSlot(key).RemoveMul(tag);
        }

        public bool SetAdd(ValueKey<int> key, ValueModifierTagId tag, float value)
        {
            return GetOrCreateIntSlot(key).SetAdd(tag, value);
        }

        public bool SetMul(ValueKey<int> key, ValueModifierTagId tag, float value)
        {
            return GetOrCreateIntSlot(key).SetMul(tag, value);
        }

        public bool RemoveAdd(ValueKey<int> key, ValueModifierTagId tag)
        {
            return GetOrCreateIntSlot(key).RemoveAdd(tag);
        }

        public bool RemoveMul(ValueKey<int> key, ValueModifierTagId tag)
        {
            return GetOrCreateIntSlot(key).RemoveMul(tag);
        }

        private RawValueSlot<T> GetOrCreateRawSlot<T>(ValueKey<T> key)
        {
            if (slotsByKey.TryGetValue(key.Id, out ValueSlot slot))
            {
                if (slot is RawValueSlot<T> typed)
                    return typed;

                throw CreateSlotTypeMismatch(key, slot);
            }

            var created = new RawValueSlot<T>(key);
            slotsByKey.Add(key.Id, created);
            return created;
        }

        private FloatNumericValueSlot GetOrCreateFloatSlot(ValueKey<float> key)
        {
            if (slotsByKey.TryGetValue(key.Id, out ValueSlot slot))
            {
                if (slot is FloatNumericValueSlot typed)
                    return typed;

                throw CreateSlotTypeMismatch(key, slot);
            }

            var created = new FloatNumericValueSlot(key);
            slotsByKey.Add(key.Id, created);
            return created;
        }

        private IntNumericValueSlot GetOrCreateIntSlot(ValueKey<int> key)
        {
            if (slotsByKey.TryGetValue(key.Id, out ValueSlot slot))
            {
                if (slot is IntNumericValueSlot typed)
                    return typed;

                throw CreateSlotTypeMismatch(key, slot);
            }

            var created = new IntNumericValueSlot(key);
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
                (TTarget)(object)key.DefaultValue
            );
        }
    }
}
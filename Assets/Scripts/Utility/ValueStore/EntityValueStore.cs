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

        public bool Contains<T>(ValueKey<T> key)
        {
            return slotsByKey.ContainsKey(key.Id);
        }

        public T Get<T>(ValueKey<T> key)
        {
            ValueSlot slot = GetOrCreateSlot(key);
            return slot.Get<T>();
        }

        public bool TryGet<T>(ValueKey<T> key, out T value)
        {
            if (!slotsByKey.TryGetValue(key.Id, out ValueSlot slot))
            {
                value = key.DefaultValue;
                return false;
            }

            value = slot.Get<T>();
            return true;
        }

        public bool Set<T>(ValueKey<T> key, T value)
        {
            ValueSlot slot = GetOrCreateSlot(key);
            return slot.Set(value);
        }

        public int GetRevision<T>(ValueKey<T> key)
        {
            ValueSlot slot = GetOrCreateSlot(key);
            return slot.Revision;
        }

        private ValueSlot GetOrCreateSlot<T>(ValueKey<T> key)
        {
            if (slotsByKey.TryGetValue(key.Id, out ValueSlot slot))
            {
                if (slot.ValueType != typeof(T))
                {
                    throw new InvalidOperationException(
                        $"ValueKey type conflict. Path={key.Path}, Key={key.Id}, SlotType={slot.ValueType.Name}, RequestedType={typeof(T).Name}");
                }

                return slot;
            }

            slot = new ValueSlot(key.Id, typeof(T), key.DefaultValue);
            slotsByKey.Add(key.Id, slot);
            return slot;
        }
    }
}
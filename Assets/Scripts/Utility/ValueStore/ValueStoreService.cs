using System;
using System.Collections.Generic;

namespace BC.Base
{
    public sealed class ValueStoreService
    {
        private readonly Dictionary<uint, EntityValueStore> storesByEntityId = new();

        public T Get<T>(EntityRef entity, ValueKey<T> key)
        {
            return GetRequiredStore(entity).Get(key);
        }

        public bool Set<T>(EntityRef entity, ValueKey<T> key, T value)
        {
            return GetRequiredStore(entity).Set(key, value);
        }

        public bool SetAdd(EntityRef entity, ValueKey<float> key, ValueModifierTagId tag, float value)
        {
            return GetRequiredStore(entity).SetAdd(key, tag, value);
        }

        public bool SetMul(EntityRef entity, ValueKey<float> key, ValueModifierTagId tag, float value)
        {
            return GetRequiredStore(entity).SetMul(key, tag, value);
        }

        public bool RemoveAdd(EntityRef entity, ValueKey<float> key, ValueModifierTagId tag)
        {
            return GetRequiredStore(entity).RemoveAdd(key, tag);
        }

        public bool RemoveMul(EntityRef entity, ValueKey<float> key, ValueModifierTagId tag)
        {
            return GetRequiredStore(entity).RemoveMul(key, tag);
        }

        public bool SetAdd(EntityRef entity, ValueKey<int> key, ValueModifierTagId tag, float value)
        {
            return GetRequiredStore(entity).SetAdd(key, tag, value);
        }

        public bool SetMul(EntityRef entity, ValueKey<int> key, ValueModifierTagId tag, float value)
        {
            return GetRequiredStore(entity).SetMul(key, tag, value);
        }

        public bool RemoveAdd(EntityRef entity, ValueKey<int> key, ValueModifierTagId tag)
        {
            return GetRequiredStore(entity).RemoveAdd(key, tag);
        }

        public bool RemoveMul(EntityRef entity, ValueKey<int> key, ValueModifierTagId tag)
        {
            return GetRequiredStore(entity).RemoveMul(key, tag);
        }
        public bool SetBoolModifier(EntityRef entity, ValueKey<bool> key, ValueModifierTagId tag, bool value)
        {
            return GetRequiredStore(entity).SetBoolModifier(key, tag, value);
        }

        public bool RemoveBoolModifier(EntityRef entity, ValueKey<bool> key, ValueModifierTagId tag)
        {
            return GetRequiredStore(entity).RemoveBoolModifier(key, tag);
        }

        private EntityValueStore GetRequiredStore(EntityRef entity)
        {
            if (!storesByEntityId.TryGetValue(entity.EntityId, out EntityValueStore store))
            {
                store = new EntityValueStore(entity);
                storesByEntityId.Add(entity.EntityId, store);
            }

            if (!store.Entity.Equals(entity))
                throw new System.InvalidOperationException($"Entity version mismatch. Entity={entity}");

            return store;
        }
    }
}
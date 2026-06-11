using System;
using System.Collections.Generic;

namespace BC.Base
{
    public sealed class ValueStoreService : IEntityValueStoreService
    {
        private readonly Dictionary<uint, EntityValueStore> storesByEntityId = new();

        public T Get<T>(EntityRef entity, ValueKeyReference key)
        {
            return GetRequiredStore(entity).Get<T>(key);
        }

        public T Get<T>(EntityRef entity, ValueKey<T> key)
        {
            return GetRequiredStore(entity).Get(key);
        }

        public ValueWatchHandle<T> GetHandle<T>(EntityRef entity, ValueKeyReference key)
        {
            return GetRequiredStore(entity).GetHandle<T>(key);
        }

        public ValueWatchHandle<T> GetHandle<T>(EntityRef entity, ValueKey<T> key)
        {
            return GetRequiredStore(entity).GetHandle(key);
        }

        public bool Set<T>(EntityRef entity, ValueKeyReference key, T value)
        {
            return GetRequiredStore(entity).Set(key, value);
        }

        public bool Set<T>(EntityRef entity, ValueKey<T> key, T value)
        {
            return GetRequiredStore(entity).Set(key, value);
        }

        public bool SetAdd(EntityRef entity, ValueKeyReference key, ValueModifierTagId tag, float value)
        {
            return GetRequiredStore(entity).SetAdd(key, tag, value);
        }

        public bool SetAdd(EntityRef entity, ValueKey<float> key, ValueModifierTagId tag, float value)
        {
            return GetRequiredStore(entity).SetAdd(key, tag, value);
        }

        public bool SetMul(EntityRef entity, ValueKeyReference key, ValueModifierTagId tag, float value)
        {
            return GetRequiredStore(entity).SetMul(key, tag, value);
        }

        public bool SetMul(EntityRef entity, ValueKey<float> key, ValueModifierTagId tag, float value)
        {
            return GetRequiredStore(entity).SetMul(key, tag, value);
        }

        public bool RemoveAdd(EntityRef entity, ValueKeyReference key, ValueModifierTagId tag)
        {
            return GetRequiredStore(entity).RemoveAdd(key, tag);
        }

        public bool RemoveAdd(EntityRef entity, ValueKey<float> key, ValueModifierTagId tag)
        {
            return GetRequiredStore(entity).RemoveAdd(key, tag);
        }

        public bool RemoveMul(EntityRef entity, ValueKeyReference key, ValueModifierTagId tag)
        {
            return GetRequiredStore(entity).RemoveMul(key, tag);
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

        public bool SetBoolModifier(EntityRef entity, ValueKeyReference key, ValueModifierTagId tag, bool value)
        {
            return GetRequiredStore(entity).SetBoolModifier(key, tag, value);
        }

        public bool SetBoolModifier(EntityRef entity, ValueKey<bool> key, ValueModifierTagId tag, bool value)
        {
            return GetRequiredStore(entity).SetBoolModifier(key, tag, value);
        }

        public bool RemoveBoolModifier(EntityRef entity, ValueKeyReference key, ValueModifierTagId tag)
        {
            return GetRequiredStore(entity).RemoveBoolModifier(key, tag);
        }

        public bool RemoveBoolModifier(EntityRef entity, ValueKey<bool> key, ValueModifierTagId tag)
        {
            return GetRequiredStore(entity).RemoveBoolModifier(key, tag);
        }

        public void ClearEntity(EntityRef entity)
        {
            if (!storesByEntityId.TryGetValue(entity.EntityId, out EntityValueStore store))
                return;

            if (!store.Entity.Equals(entity))
                return;

            store.Clear();
            storesByEntityId.Remove(entity.EntityId);
        }

        public void Clear()
        {
            foreach (EntityValueStore store in storesByEntityId.Values)
            {
                store.Clear();
            }

            storesByEntityId.Clear();
        }

        private EntityValueStore GetRequiredStore(EntityRef entity)
        {
            // EntityValueStore は Entity の世代付き参照を契約にしている。
            // default(EntityRef) を許すと EntityId=0 の store が作られ、target 解決漏れを
            // 「成功した書き込み」に見せてしまうため、入口で明示的に落とす。
            if (!entity.IsValid)
                throw new InvalidOperationException($"Entity value store requires a valid entity. Entity={entity}");

            if (!storesByEntityId.TryGetValue(entity.EntityId, out EntityValueStore store))
            {
                store = new EntityValueStore(entity);
                storesByEntityId.Add(entity.EntityId, store);
            }

            if (!store.Entity.Equals(entity))
                throw new InvalidOperationException($"Entity version mismatch. Entity={entity}");

            return store;
        }
    }
}

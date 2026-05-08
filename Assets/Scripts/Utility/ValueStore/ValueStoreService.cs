using System;
using System.Collections.Generic;

namespace BC.Base
{

    public readonly struct ValueChangedEntityEvent : IEntityEvent
    {
        public readonly ValueKeyId KeyId;
        public readonly string Path;
        public readonly Type ValueType;
        public readonly int Revision;

        public ValueChangedEntityEvent(
            ValueKeyId keyId,
            string path,
            Type valueType,
            int revision)
        {
            KeyId = keyId;
            Path = path;
            ValueType = valueType;
            Revision = revision;
        }
    }
    public sealed class ValueStoreService
    {
        private readonly Dictionary<uint, EntityValueStore> storesByEntityId = new();
        private readonly EventService events;

        public ValueStoreService(EventService events)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public void Create(EntityRef entity)
        {
            if (storesByEntityId.ContainsKey(entity.EntityId))
                return;

            storesByEntityId.Add(entity.EntityId, new EntityValueStore(entity));
        }

        public void Remove(EntityRef entity)
        {
            storesByEntityId.Remove(entity.EntityId);
        }

        public bool HasStore(EntityRef entity)
        {
            return storesByEntityId.TryGetValue(entity.EntityId, out EntityValueStore store)
                && store.Entity.Version == entity.Version;
        }

        public T Get<T>(EntityRef entity, ValueKey<T> key)
        {
            EntityValueStore store = GetRequiredStore(entity);
            return store.Get(key);
        }

        public bool TryGet<T>(EntityRef entity, ValueKey<T> key, out T value)
        {
            if (!TryGetStore(entity, out EntityValueStore store))
            {
                value = key.DefaultValue;
                return false;
            }

            return store.TryGet(key, out value);
        }

        public bool Set<T>(EntityRef entity, ValueKey<T> key, T value)
        {
            EntityValueStore store = GetRequiredStore(entity);

            bool changed = store.Set(key, value);

            if (changed)
            {
                events.Publish(
                    entity,
                    new ValueChangedEntityEvent(key.Id, key.Path, typeof(T), store.GetRevision(key))
                );
            }

            return changed;
        }

        public int GetRevision<T>(EntityRef entity, ValueKey<T> key)
        {
            EntityValueStore store = GetRequiredStore(entity);
            return store.GetRevision(key);
        }

        private bool TryGetStore(EntityRef entity, out EntityValueStore store)
        {
            if (!storesByEntityId.TryGetValue(entity.EntityId, out store))
                return false;

            if (store.Entity.Version != entity.Version)
            {
                store = null;
                return false;
            }

            return true;
        }

        private EntityValueStore GetRequiredStore(EntityRef entity)
        {
            if (!TryGetStore(entity, out EntityValueStore store))
            {
                throw new InvalidOperationException($"ValueStore does not exist for entity: {entity}");
            }

            return store;
        }
    }
}
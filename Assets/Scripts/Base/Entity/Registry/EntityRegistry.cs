using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.Base
{


    public sealed class ScopedEntityRegistry
    {
        private EntityIdAllocator idAllocator;
        private EntityLifetimeScope scope;
        private readonly Dictionary<uint, EntityRecord> recordsById = new(); // エンティティのIDをキーにして、エンティティの基本情報を管理する辞書
        private readonly Dictionary<uint, EntityUnityBinding> bindingsById = new(); // Unityとの接点を管理する辞書
        private readonly Dictionary<EntityTagId, List<EntityRef>> entitiesByTag = new(); // タグごとにエンティティを管理する辞書(検索の高速化のため)

        public ScopedEntityRegistry(EntityLifetimeScope scope, EntityIdAllocator entityIdAllocator)
        {
            this.scope = scope;
            this.idAllocator = entityIdAllocator;
        }

        public EntityRef Register(EntityRegistryRequest request)
        {
            if (request.GameObject == null)
                throw new ArgumentNullException(nameof(request.GameObject));

            if (request.Transform == null)
                throw new ArgumentNullException(nameof(request.Transform));

            var entity = idAllocator.Allocate();
            var record = new EntityRecord(entity, request.Tag, request.Flags);
            var binding = new EntityUnityBinding(entity, request.GameObject, request.Transform);

            recordsById.Add(entity.EntityId, record);
            bindingsById.Add(entity.EntityId, binding);

            if (!entitiesByTag.TryGetValue(request.Tag, out var list))
            {
                list = new List<EntityRef>();
                entitiesByTag.Add(request.Tag, list);
            }
            // タグ追加
            list.Add(entity);

            return entity;
        }

        public bool Unregister(EntityRef entity)
        {
            if (!TryGetRecord(entity, out var record))
                return false;

            recordsById.Remove(entity.EntityId);
            bindingsById.Remove(entity.EntityId);

            if (entitiesByTag.TryGetValue(record.Tag, out var list))
            {
                list.Remove(entity);
                // tag削除
                if (list.Count == 0)
                    entitiesByTag.Remove(record.Tag);
            }

            return true;
        }

        public bool IsAlive(EntityRef entity)
        {
            return TryGetRecord(entity, out _);
        }

        public bool TryGetRecord(EntityRef entity, out EntityRecord record)
        {
            if (!recordsById.TryGetValue(entity.EntityId, out record))
                return false;

            return record.Entity.Version == entity.Version;
        }

        public bool TryGetBinding(EntityRef entity, out EntityUnityBinding binding)
        {
            binding = null;

            if (!TryGetRecord(entity, out _))
                return false;

            return bindingsById.TryGetValue(entity.EntityId, out binding);
        }

        public IReadOnlyList<EntityRef> GetEntitiesByTag(EntityTagId tag)
        {
            if (!entitiesByTag.TryGetValue(tag, out var list))
                return Array.Empty<EntityRef>();

            return list;
        }
    }
}
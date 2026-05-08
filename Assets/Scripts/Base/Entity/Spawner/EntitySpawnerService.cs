using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.Base
{
    public sealed class EntitySpawnerService
    {
        private readonly SceneKernel kernel;
        private readonly Transform spawnRoot;
        private readonly Transform poolRoot;

        private readonly Dictionary<GameObject, PrefabPool> poolsByPrefab = new();
        private readonly Dictionary<uint, SpawnedEntityRecord> spawnedByEntityId = new();

        public EntitySpawnerService(SceneKernel kernel, Transform spawnRoot, Transform poolRoot)
        {
            this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            this.spawnRoot = spawnRoot;
            this.poolRoot = poolRoot;
        }

        public EntitySpawnResult Spawn(EntitySpawnRequest request)
        {
            if (request.Prefab == null)
                throw new ArgumentNullException(nameof(request.Prefab));

            GameObject instance = request.UsePool
                ? GetFromPool(request.Prefab)
                : UnityEngine.Object.Instantiate(request.Prefab);

            Transform targetParent = request.Parent != null ? request.Parent : spawnRoot;

            if (request.UseWorldPosition)
            {
                instance.transform.SetParent(targetParent, true);
                instance.transform.SetPositionAndRotation(request.Position, request.Rotation);
            }
            else
            {
                instance.transform.SetParent(targetParent, false);
                instance.transform.localPosition = request.Position;
                instance.transform.localRotation = request.Rotation;
            }

            if (!instance.TryGetComponent(out EntityMB entityMb))
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"Spawned prefab does not have EntityMB. Prefab={request.Prefab.name}");
            }

            EntityRegistryRequest registryRequest = new EntityRegistryRequest(
                instance,
                instance.transform,
                entityMb.Tag,
                entityMb.Flags
            );

            EntityRef entity = kernel.EntityLifecycle.Register(registryRequest);

            entityMb.Bind(entity);

            spawnedByEntityId.Add(
                entity.EntityId,
                new SpawnedEntityRecord(entity, request.Prefab, instance, request.UsePool)
            );

            instance.SetActive(true);

            return new EntitySpawnResult(entity, instance, instance.transform, entityMb);
        }

        public bool Despawn(EntityRef entity)
        {
            if (!spawnedByEntityId.TryGetValue(entity.EntityId, out SpawnedEntityRecord record))
                return false;

            if (!record.Entity.Equals(entity))
                return false;

            spawnedByEntityId.Remove(entity.EntityId);

            if (record.GameObject != null &&
                record.GameObject.TryGetComponent(out EntityMB entityMb) &&
                entityMb.HasEntity)
            {
                entityMb.Unbind(entity);
            }

            kernel.EntityLifecycle.Unregister(entity);

            if (record.UsePool)
            {
                GetOrCreatePool(record.Prefab).Release(record.GameObject);
            }
            else
            {
                UnityEngine.Object.Destroy(record.GameObject);
            }

            return true;
        }

        private GameObject GetFromPool(GameObject prefab)
        {
            return GetOrCreatePool(prefab).Get();
        }

        private PrefabPool GetOrCreatePool(GameObject prefab)
        {
            if (!poolsByPrefab.TryGetValue(prefab, out PrefabPool pool))
            {
                pool = new PrefabPool(prefab, poolRoot);
                poolsByPrefab.Add(prefab, pool);
            }

            return pool;
        }
    }

    internal readonly struct SpawnedEntityRecord
    {
        public readonly EntityRef Entity;
        public readonly GameObject Prefab;
        public readonly GameObject GameObject;
        public readonly bool UsePool;

        public SpawnedEntityRecord(
            EntityRef entity,
            GameObject prefab,
            GameObject gameObject,
            bool usePool)
        {
            Entity = entity;
            Prefab = prefab;
            GameObject = gameObject;
            UsePool = usePool;
        }
    }
}
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

            SpawnedEntityRecord record = RegisterSpawnedEntities(request.Prefab, instance, request.UsePool);

            for (int i = 0; i < record.Bindings.Length; i++)
            {
                spawnedByEntityId.Add(record.Bindings[i].Entity.EntityId, record);
            }

            instance.SetActive(true);

            return new EntitySpawnResult(record.Entity, instance, instance.transform, entityMb);
        }

        public bool Despawn(EntityRef entity, EntityDespawnMode despawnMode = EntityDespawnMode.Auto)
        {
            if (!spawnedByEntityId.TryGetValue(entity.EntityId, out SpawnedEntityRecord record))
                return false;

            for (int i = 0; i < record.Bindings.Length; i++)
            {
                spawnedByEntityId.Remove(record.Bindings[i].Entity.EntityId);
            }

            for (int i = record.Bindings.Length - 1; i >= 0; i--)
            {
                RegisteredEntityBinding binding = record.Bindings[i];

                if (binding.EntityMB != null && binding.EntityMB.HasEntity)
                {
                    binding.EntityMB.Unbind(binding.Entity);
                }

                kernel.EntityLifecycle.Unregister(binding.Entity);
            }

            switch (ResolveDespawnMode(record, despawnMode))
            {
                case EntityDespawnMode.ReturnToPool:
                    GetOrCreatePool(record.Prefab).Release(record.GameObject);
                    break;

                case EntityDespawnMode.Destroy:
                    UnityEngine.Object.Destroy(record.GameObject);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(despawnMode), despawnMode, null);
            }

            return true;
        }

        private static EntityDespawnMode ResolveDespawnMode(
            in SpawnedEntityRecord record,
            EntityDespawnMode despawnMode)
        {
            if (despawnMode != EntityDespawnMode.Auto)
                return despawnMode;

            return record.UsePool ? EntityDespawnMode.ReturnToPool : EntityDespawnMode.Destroy;
        }

        private SpawnedEntityRecord RegisterSpawnedEntities(GameObject prefab, GameObject instance, bool usePool)
        {
            EntityMB[] entityMbs = instance.GetComponentsInChildren<EntityMB>(true);
            RegisteredEntityBinding[] bindings = new RegisteredEntityBinding[entityMbs.Length];

            // Spawn された prefab 配下の Entity は子も含めて同じタイミングで登録する。
            for (int i = 0; i < entityMbs.Length; i++)
            {
                EntityMB entityMb = entityMbs[i];
                EntityRegistryRequest registryRequest = new EntityRegistryRequest(
                    entityMb.gameObject,
                    entityMb.transform,
                    entityMb.Tag,
                    entityMb.Flags
                );

                EntityRef entity = kernel.EntityLifecycle.Register(registryRequest);
                entityMb.Bind(entity, EntityRegistrationMode.Spawned);
                bindings[i] = new RegisteredEntityBinding(entity, entityMb);
            }

            if (!instance.TryGetComponent(out EntityMB rootEntityMb))
            {
                throw new InvalidOperationException(
                    $"Spawned prefab does not have root EntityMB. Prefab={prefab.name}");
            }

            return new SpawnedEntityRecord(rootEntityMb.Entity, prefab, instance, bindings, usePool);
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

    internal readonly struct RegisteredEntityBinding
    {
        public readonly EntityRef Entity;
        public readonly EntityMB EntityMB;

        public RegisteredEntityBinding(EntityRef entity, EntityMB entityMB)
        {
            Entity = entity;
            EntityMB = entityMB;
        }
    }

    internal readonly struct SpawnedEntityRecord
    {
        public readonly EntityRef Entity;
        public readonly GameObject Prefab;
        public readonly GameObject GameObject;
        public readonly RegisteredEntityBinding[] Bindings;
        public readonly bool UsePool;

        public SpawnedEntityRecord(
            EntityRef entity,
            GameObject prefab,
            GameObject gameObject,
            RegisteredEntityBinding[] bindings,
            bool usePool)
        {
            Entity = entity;
            Prefab = prefab;
            GameObject = gameObject;
            Bindings = bindings;
            UsePool = usePool;
        }
    }
}
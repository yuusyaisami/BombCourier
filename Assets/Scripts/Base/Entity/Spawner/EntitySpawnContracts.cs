using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BC.Base
{
    public readonly struct EntitySpawnRequest
    {
        public readonly GameObject Prefab;
        public readonly Transform Parent;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly bool UseWorldPosition;
        public readonly bool UsePool;

        public EntitySpawnRequest(
            GameObject prefab,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            bool useWorldPosition = true,
            bool usePool = true)
        {
            Prefab = prefab;
            Parent = parent;
            Position = position;
            Rotation = rotation;
            UseWorldPosition = useWorldPosition;
            UsePool = usePool;
        }
    }

    public readonly struct EntitySpawnResult
    {
        public readonly EntityRef Entity;
        public readonly GameObject GameObject;
        public readonly Transform Transform;
        public readonly EntityMB EntityMB;

        public EntitySpawnResult(
            EntityRef entity,
            GameObject gameObject,
            Transform transform,
            EntityMB entityMB)
        {
            Entity = entity;
            GameObject = gameObject;
            Transform = transform;
            EntityMB = entityMB;
        }
    }
    public sealed class PrefabPool
    {
        private readonly GameObject prefab;
        private readonly Transform poolRoot;
        private readonly Stack<GameObject> inactiveObjects = new();

        public PrefabPool(GameObject prefab, Transform poolRoot)
        {
            this.prefab = prefab;
            this.poolRoot = poolRoot;
        }

        public GameObject Get()
        {
            if (inactiveObjects.Count > 0)
            {
                return inactiveObjects.Pop();
            }

            return Object.Instantiate(prefab);
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
                return;

            instance.SetActive(false);
            instance.transform.SetParent(poolRoot, false);
            inactiveObjects.Push(instance);
        }
    }
}
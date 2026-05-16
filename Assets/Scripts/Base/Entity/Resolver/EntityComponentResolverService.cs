using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.Base
{
    public sealed class EntityComponentResolverService
    {
        private static readonly object MissingValue = new();

        private readonly SceneKernel sceneKernel;
        private readonly Dictionary<EntityRef, CacheEntry> cacheByEntity = new();

        public EntityComponentResolverService(SceneKernel sceneKernel)
        {
            this.sceneKernel = sceneKernel ?? throw new ArgumentNullException(nameof(sceneKernel));
        }

        public void Register(EntityRef entity, GameObject rootObject, Transform rootTransform)
        {
            if (!entity.IsValid || rootObject == null || rootTransform == null)
                return;

            cacheByEntity[entity] = new CacheEntry(rootObject, rootTransform);
        }

        public void Unregister(EntityRef entity)
        {
            if (!entity.IsValid)
                return;

            cacheByEntity.Remove(entity);
        }

        public void Invalidate(EntityRef entity)
        {
            if (!TryEnsureEntry(entity, out CacheEntry entry))
                return;

            entry.InvalidateDynamicCaches();
        }

        public void Clear()
        {
            cacheByEntity.Clear();
        }

        public bool TryGetGameObject(EntityRef entity, out GameObject gameObject)
        {
            return TryResolve(entity, out gameObject);
        }

        public bool TryGetTransform(EntityRef entity, out Transform transform)
        {
            return TryResolve(entity, out transform);
        }

        public bool TryGetEntityMB(EntityRef entity, out EntityMB entityMb)
        {
            return TryResolve(entity, out entityMb);
        }

        public bool TryResolve<T>(EntityRef entity, out T resolved) where T : class
        {
            resolved = null;

            if (!TryEnsureEntry(entity, out CacheEntry entry))
                return false;

            if (TryResolveFixedBinding(entry, out resolved))
                return resolved != null;

            Type requestedType = typeof(T);

            if (entry.FirstByType.TryGetValue(requestedType, out object cachedValue))
            {
                if (ReferenceEquals(cachedValue, MissingValue))
                    return false;

                if (TryReadCachedValue(cachedValue, out resolved))
                    return true;

                entry.FirstByType.Remove(requestedType);
            }

            resolved = FindFirstInHierarchy<T>(entry);
            entry.FirstByType[requestedType] = PackCachedValue(resolved);
            return resolved != null;
        }

        public int ResolveAll<T>(EntityRef entity, List<T> results) where T : class
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();

            if (!TryEnsureEntry(entity, out CacheEntry entry))
                return 0;

            if (TryResolveFixedBinding(entry, out T fixedBinding))
            {
                results.Add(fixedBinding);
                return results.Count;
            }

            Type requestedType = typeof(T);

            if (entry.AllByType.TryGetValue(requestedType, out object cachedList))
            {
                if (TryCopyCachedList(cachedList, results))
                    return results.Count;

                entry.AllByType.Remove(requestedType);
            }

            Component[] hierarchyComponents = entry.GetHierarchyComponents();

            for (int i = 0; i < hierarchyComponents.Length; i++)
            {
                Component component = hierarchyComponents[i];

                if (component is T matched)
                    results.Add(matched);
            }

            entry.AllByType[requestedType] = new CachedList<T>(results);

            if (results.Count > 0)
            {
                entry.FirstByType[requestedType] = PackCachedValue(results[0]);
            }
            else
            {
                entry.FirstByType[requestedType] = MissingValue;
            }

            return results.Count;
        }

        public bool Prewarm<T>(EntityRef entity) where T : class
        {
            return TryResolve(entity, out T _);
        }

        private bool TryEnsureEntry(EntityRef entity, out CacheEntry entry)
        {
            if (!entity.IsValid)
            {
                entry = null;
                return false;
            }

            if (cacheByEntity.TryGetValue(entity, out entry))
            {
                if (entry.IsAlive)
                    return true;

                cacheByEntity.Remove(entity);
            }

            if (!TryResolveBinding(entity, out EntityUnityBinding binding) || binding.GameObject == null || binding.Transform == null)
            {
                entry = null;
                return false;
            }

            entry = new CacheEntry(binding.GameObject, binding.Transform);
            cacheByEntity.Add(entity, entry);
            return true;
        }

        private bool TryResolveBinding(EntityRef entity, out EntityUnityBinding binding)
        {
            binding = null;

            if (sceneKernel.EntitiesRegistry != null && sceneKernel.EntitiesRegistry.TryGetBinding(entity, out binding))
                return true;

            ApplicationKernel applicationKernel = ApplicationKernelMB.Instance != null
                ? ApplicationKernelMB.Instance.Kernel
                : null;

            if (applicationKernel?.ApplicationEntityRegistry != null &&
                applicationKernel.ApplicationEntityRegistry.TryGetBinding(entity, out binding))
            {
                return true;
            }

            binding = null;
            return false;
        }

        private static bool TryResolveFixedBinding<T>(CacheEntry entry, out T resolved) where T : class
        {
            resolved = null;

            if (typeof(T) == typeof(GameObject))
            {
                resolved = entry.RootGameObject as T;
                return true;
            }

            if (typeof(T) == typeof(Transform))
            {
                resolved = entry.RootTransform as T;
                return true;
            }

            if (typeof(T) == typeof(EntityMB))
            {
                EntityMB entityMb = entry.GetRootEntityMB();
                resolved = entityMb as T;
                return true;
            }

            return false;
        }

        private static T FindFirstInHierarchy<T>(CacheEntry entry) where T : class
        {
            Component[] hierarchyComponents = entry.GetHierarchyComponents();

            for (int i = 0; i < hierarchyComponents.Length; i++)
            {
                Component component = hierarchyComponents[i];

                if (component is T matched)
                    return matched;
            }

            return null;
        }

        private static object PackCachedValue<T>(T value) where T : class
        {
            return value ?? MissingValue;
        }

        private static bool TryReadCachedValue<T>(object cachedValue, out T resolved) where T : class
        {
            if (ReferenceEquals(cachedValue, MissingValue))
            {
                resolved = null;
                return false;
            }

            if (cachedValue is not T typedValue)
            {
                resolved = null;
                return false;
            }

            if (typedValue is UnityEngine.Object unityObject && unityObject == null)
            {
                resolved = null;
                return false;
            }

            resolved = typedValue;
            return true;
        }

        private static bool TryCopyCachedList<T>(object cachedValue, List<T> results) where T : class
        {
            if (cachedValue is not CachedList<T> typedList)
                return false;

            if (!typedList.IsAlive)
                return false;

            results.AddRange(typedList.Items);
            return true;
        }

        private sealed class CacheEntry
        {
            private Component[] hierarchyComponents;
            private EntityMB rootEntityMB;

            public CacheEntry(GameObject rootGameObject, Transform rootTransform)
            {
                RootGameObject = rootGameObject;
                RootTransform = rootTransform;
            }

            public GameObject RootGameObject { get; }
            public Transform RootTransform { get; }
            public Dictionary<Type, object> FirstByType { get; } = new();
            public Dictionary<Type, object> AllByType { get; } = new();
            public bool IsAlive => RootGameObject != null && RootTransform != null;

            public EntityMB GetRootEntityMB()
            {
                if (rootEntityMB == null && RootGameObject != null)
                    rootEntityMB = RootGameObject.GetComponent<EntityMB>();

                return rootEntityMB;
            }

            public Component[] GetHierarchyComponents()
            {
                // 初回だけ階層全体の Component 配列を取り、その後は型別キャッシュで再探索を避ける。
                hierarchyComponents ??= RootGameObject != null
                    ? RootGameObject.GetComponentsInChildren<Component>(true)
                    : Array.Empty<Component>();

                return hierarchyComponents;
            }

            public void InvalidateDynamicCaches()
            {
                hierarchyComponents = null;
                rootEntityMB = null;
                FirstByType.Clear();
                AllByType.Clear();
            }
        }

        private sealed class CachedList<T> where T : class
        {
            public CachedList(List<T> source)
            {
                Items = source.Count == 0 ? Array.Empty<T>() : source.ToArray();
            }

            public T[] Items { get; }

            public bool IsAlive
            {
                get
                {
                    for (int i = 0; i < Items.Length; i++)
                    {
                        T item = Items[i];

                        if (item is UnityEngine.Object unityObject && unityObject == null)
                            return false;
                    }

                    return true;
                }
            }
        }
    }
}

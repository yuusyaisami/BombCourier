using System;
using UnityEngine;
namespace BC.Base
{

    public enum EntityLifetimeScope
    {
        Application,
        Scene,
    }
    public sealed class EntityIdAllocator
    {
        private uint nextId = 1;

        public EntityRef Allocate()
        {
            uint id = nextId++;
            return new EntityRef(id, 1);
        }
    }
    public struct EntityRegistryRequest
    {
        public GameObject GameObject;
        public Transform Transform;
        public EntityTagId Tag;
        public EntityFlags Flags;

        public EntityRegistryRequest(GameObject gameObject, Transform transform, EntityTagId tag, EntityFlags flags)
        {
            GameObject = gameObject;
            Transform = transform;
            Tag = tag;
            Flags = flags;
        }
    }
    public readonly struct EntityRef : IEquatable<EntityRef>
    {
        public readonly uint EntityId;
        public readonly int Version;

        public bool IsValid => EntityId != 0;

        public EntityRef(uint entityId, int version)
        {
            EntityId = entityId;
            Version = version;
        }

        public bool Equals(EntityRef other)
        {
            return EntityId == other.EntityId && Version == other.Version;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EntityId, Version);
        }

        public override string ToString()
        {
            return $"Entity({EntityId}:{Version})";
        }
    }
    // EntityDataは、エンティティの基本的なデータ構造を定義するクラスです。
    // これには、エンティティのID、タグ、その他の基本的な情報が含まれます。
    [System.Serializable]
    public readonly struct EntityData
    {
        public readonly uint EntityId;
        public readonly int Version;
        public readonly EntityTagId Tag;
        public readonly EntityFlags Flags;

        public EntityData(uint entityId, int version, EntityTagId tag, EntityFlags flags)
        {
            EntityId = entityId;
            Version = version;
            Tag = tag;
            Flags = flags;
        }
    }
    public sealed class EntityUnityBinding
    {
        public EntityRef Entity { get; }
        public GameObject GameObject { get; }
        public Transform Transform { get; }

        public EntityUnityBinding(EntityRef entity, GameObject gameObject, Transform transform)
        {
            Entity = entity;
            GameObject = gameObject;
            Transform = transform;
        }
    }
    [Flags]
    public enum EntityFlags
    {
        None = 0,
        DontDestroyOnLoad = 1 << 0,
    }

    public readonly struct EntityTagId : IEquatable<EntityTagId>
    {
        public readonly int Value;
        public bool IsValid => Value != 0;

        public EntityTagId(int value)
        {
            Value = value;
        }

        public bool Equals(EntityTagId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EntityTagId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();

        public static bool operator ==(EntityTagId left, EntityTagId right) => left.Equals(right);
        public static bool operator !=(EntityTagId left, EntityTagId right) => !left.Equals(right);
    }

    public readonly struct EntityRecord
    {
        public readonly EntityRef Entity;
        public readonly EntityTagId Tag;
        public readonly EntityFlags Flags;

        public EntityRecord(EntityRef entity, EntityTagId tag, EntityFlags flags)
        {
            Entity = entity;
            Tag = tag;
            Flags = flags;
        }
    }
}
using System;

namespace BC.Base
{
    public readonly struct EntityTag : IEquatable<EntityTag>
    {
        public readonly EntityTagId Id;
        public readonly string Path;

        public EntityTag(EntityTagId id, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("EntityTag path is null or empty.", nameof(path));

            Id = id;
            Path = path;
        }

        public bool Equals(EntityTag other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is EntityTag other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Path} ({Id})";
        }
    }

    public static class EntityTags
    {
        public static class Actor
        {
            public static readonly EntityTag Player =
                new EntityTag(
                    new EntityTagId(1001),
                    "Actor.Player"
                );
        }

        public static class Item
        {
            public static readonly EntityTag Bomb =
                new EntityTag(
                    new EntityTagId(2001),
                    "Item.Bomb"
                );

            public static readonly EntityTag Carryable =
                new EntityTag(
                    new EntityTagId(2002),
                    "Item.Carryable"
                );
        }

        public static class Gimmick
        {
            public static readonly EntityTag Cushion =
                new EntityTag(
                    new EntityTagId(3001),
                    "Gimmick.Cushion"
                );
        }
    }
}

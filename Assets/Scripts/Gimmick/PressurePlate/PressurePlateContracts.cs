using BC.Base;
using UnityEngine;

namespace BC.Gimmick.PressurePlate
{
    public readonly struct PressurePlateContactData
    {
        public readonly Collider SourceCollider;
        public readonly GameObject SourceObject;
        public readonly Transform SourceRoot;
        public readonly EntityRef SourceEntity;
        public readonly EntityTagId SourceTag;

        public PressurePlateContactData(
            Collider sourceCollider,
            GameObject sourceObject,
            Transform sourceRoot,
            EntityRef sourceEntity,
            EntityTagId sourceTag)
        {
            SourceCollider = sourceCollider;
            SourceObject = sourceObject;
            SourceRoot = sourceRoot;
            SourceEntity = sourceEntity;
            SourceTag = sourceTag;
        }
    }
}
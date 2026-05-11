using BC.Base;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Gimmick.PressurePlate
{
    [DisallowMultipleComponent]
    public sealed class PressurePlateSurfaceMB : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private bool acceptAnyTag;
        [HideIf(nameof(acceptAnyTag))]
        [SerializeField, EntityTagDropdown]
        private EntityTagReference[] targetTags =
        {
            EntityTagReference.From(EntityTags.Actor.Player),
            EntityTagReference.From(EntityTags.Item.Bomb)
        };

        [Header("Filter")]
        [SerializeField] private bool ignoreSelfEntity = true;

        public bool TryEvaluate(in PressurePlateContactData contactData)
        {
            if (ignoreSelfEntity && ShouldIgnoreSelfContact(contactData.SourceRoot))
                return false;

            return MatchesTargetTag(contactData.SourceTag);
        }

        private bool MatchesTargetTag(EntityTagId sourceTag)
        {
            if (acceptAnyTag)
                return true;

            if (!sourceTag.IsValid || targetTags == null || targetTags.Length == 0)
                return false;

            for (int i = 0; i < targetTags.Length; i++)
            {
                if (targetTags[i].Matches(sourceTag))
                    return true;
            }

            return false;
        }

        private bool ShouldIgnoreSelfContact(Transform sourceRoot)
        {
            if (sourceRoot == null)
                return false;

            return transform == sourceRoot || transform.IsChildOf(sourceRoot) || sourceRoot.IsChildOf(transform);
        }
    }
}
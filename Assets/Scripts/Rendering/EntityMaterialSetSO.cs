using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.Rendering
{
    [Serializable]
    public struct EntityMaterialSlotMaterialEntry
    {
        [SerializeField] private string slotKey;
        [SerializeField] private Material material;

        public EntityMaterialSlotMaterialEntry(string slotKey, Material material)
        {
            this.slotKey = slotKey;
            this.material = material;
        }

        public string SlotKey => slotKey;
        public Material Material => material;
    }

    [Serializable]
    public struct EntityMaterialDatasetVariant
    {
        [SerializeField] private string datasetKind;
        [SerializeField] private List<EntityMaterialSlotMaterialEntry> slotMaterials;

        public EntityMaterialDatasetVariant(string datasetKind, List<EntityMaterialSlotMaterialEntry> slotMaterials)
        {
            this.datasetKind = datasetKind;
            this.slotMaterials = slotMaterials ?? new List<EntityMaterialSlotMaterialEntry>();
        }

        public string DatasetKind => datasetKind;
        public IReadOnlyList<EntityMaterialSlotMaterialEntry> SlotMaterials => slotMaterials;

        internal bool TryBuildMaterialMap(out Dictionary<string, Material> materialsByKey, out string failureReason)
        {
            materialsByKey = new Dictionary<string, Material>(StringComparer.Ordinal);
            failureReason = null;

            if (slotMaterials == null || slotMaterials.Count == 0)
            {
                failureReason = $"Dataset '{datasetKind}' does not define any slot materials.";
                return false;
            }

            for (int i = 0; i < slotMaterials.Count; i++)
            {
                EntityMaterialSlotMaterialEntry entry = slotMaterials[i];
                string normalizedKey = EntityMaterialSetSO.NormalizeSlotKey(entry.SlotKey);

                if (string.IsNullOrWhiteSpace(normalizedKey))
                {
                    failureReason = $"Dataset '{datasetKind}' contains an empty slot key at index {i}.";
                    return false;
                }

                if (entry.Material == null)
                {
                    failureReason = $"Dataset '{datasetKind}' slot '{normalizedKey}' does not assign a material.";
                    return false;
                }

                if (!materialsByKey.TryAdd(normalizedKey, entry.Material))
                {
                    failureReason = $"Dataset '{datasetKind}' contains a duplicate slot key '{normalizedKey}'.";
                    return false;
                }
            }

            return true;
        }
    }

    [CreateAssetMenu(
        fileName = "EntityMaterialSet",
        menuName = "BombCourier/Rendering/Entity Material Set")]
    public sealed class EntityMaterialSetSO : ScriptableObject
    {
        public const string DefaultDatasetKind = "Default";
        public const string DackDatasetKind = "Dack";
        [Obsolete("Use DackDatasetKind instead.")]
        public const string DackDatasetKid = DackDatasetKind;

        [SerializeField] private List<EntityMaterialDatasetVariant> datasetVariants = new();

        public IReadOnlyList<EntityMaterialDatasetVariant> DatasetVariants => datasetVariants;

        public static string NormalizeDatasetKind(string datasetKind)
        {
            return string.IsNullOrWhiteSpace(datasetKind)
                ? DefaultDatasetKind
                : datasetKind.Trim();
        }

        public static string NormalizeSlotKey(string slotKey)
        {
            return string.IsNullOrWhiteSpace(slotKey)
                ? string.Empty
                : slotKey.Trim();
        }

        public bool ValidateDefinition(out string failureReason)
        {
            failureReason = null;

            if (datasetVariants == null || datasetVariants.Count == 0)
            {
                failureReason = $"{name}: no dataset variants are defined.";
                return false;
            }

            var datasetKinds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < datasetVariants.Count; i++)
            {
                EntityMaterialDatasetVariant variant = datasetVariants[i];
                string normalizedKind = variant.DatasetKind?.Trim();

                if (string.IsNullOrWhiteSpace(normalizedKind))
                {
                    failureReason = $"{name}: dataset variant at index {i} has an empty dataset kind.";
                    return false;
                }

                if (!datasetKinds.Add(normalizedKind))
                {
                    failureReason = $"{name}: duplicate dataset kind '{normalizedKind}'.";
                    return false;
                }

                if (!variant.TryBuildMaterialMap(out _, out string variantFailure))
                {
                    failureReason = $"{name}: {variantFailure}";
                    return false;
                }

            }

            return true;
        }

        public bool HasDatasetKind(string datasetKind)
        {
            if (datasetVariants == null || datasetVariants.Count == 0)
                return false;

            string normalizedKind = NormalizeDatasetKind(datasetKind);
            for (int i = 0; i < datasetVariants.Count; i++)
            {
                if (string.Equals(datasetVariants[i].DatasetKind?.Trim(), normalizedKind, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public bool TryGetFirstDatasetKind(out string datasetKind)
        {
            datasetKind = null;

            if (datasetVariants == null || datasetVariants.Count == 0)
                return false;

            for (int i = 0; i < datasetVariants.Count; i++)
            {
                string candidate = datasetVariants[i].DatasetKind?.Trim();
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                datasetKind = candidate;
                return true;
            }

            return false;
        }

        public bool TryGetDataset(string datasetKind, out EntityMaterialDatasetVariant variant, out string failureReason)
        {
            variant = default;

            if (!ValidateDefinition(out failureReason))
                return false;

            string normalizedKind = NormalizeDatasetKind(datasetKind);

            for (int i = 0; i < datasetVariants.Count; i++)
            {
                EntityMaterialDatasetVariant candidate = datasetVariants[i];
                if (string.Equals(candidate.DatasetKind?.Trim(), normalizedKind, StringComparison.Ordinal))
                {
                    variant = candidate;
                    failureReason = null;
                    return true;
                }
            }

            failureReason = $"{name}: dataset kind '{normalizedKind}' is not defined.";
            return false;
        }

        internal bool TryBuildMaterialMap(
            string datasetKind,
            out Dictionary<string, Material> materialsByKey,
            out string failureReason)
        {
            materialsByKey = null;

            if (!TryGetDataset(datasetKind, out EntityMaterialDatasetVariant variant, out failureReason))
                return false;

            return variant.TryBuildMaterialMap(out materialsByKey, out failureReason);
        }
    }
}

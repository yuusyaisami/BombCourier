using System;
using System.Collections.Generic;
using BC.Manager;
using BC.Managers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Rendering
{
    [Serializable]
    public struct EntityMaterialApplyRequest
    {
        public EntityMaterialSetSO overrideSet;
        public string datasetKind;

        public EntityMaterialApplyRequest Sanitize()
        {
            return new EntityMaterialApplyRequest
            {
                overrideSet = overrideSet,
                datasetKind = EntityMaterialSetSO.NormalizeDatasetKind(datasetKind),
            };
        }
    }

    [Serializable]
    public struct EntityMaterialSlotBinding
    {
        [SerializeField] private string slotKey;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField, Min(0)] private int materialIndex;

        public EntityMaterialSlotBinding(string slotKey, Renderer targetRenderer, int materialIndex)
        {
            this.slotKey = slotKey;
            this.targetRenderer = targetRenderer;
            this.materialIndex = materialIndex;
        }

        public string SlotKey => slotKey;
        public Renderer TargetRenderer => targetRenderer;
        public int MaterialIndex => materialIndex;
    }

    [DisallowMultipleComponent]
    public sealed class EntityMaterialControllerMB : MonoBehaviour
    {
        [Header("Definition")]
        [SerializeField] private EntityMaterialSetSO defaultMaterialSet;
        [SerializeField] private List<EntityMaterialSlotBinding> slotBindings = new();

        [Header("Runtime")]
        [SerializeField, ReadOnly] private string currentDatasetKind = EntityMaterialSetSO.DefaultDatasetKind;

        private EntityMaterialDatasetServiceMB datasetService;
        private bool isRegisteredToDatasetService;
        private bool hasTriedInitialApply;
        private float nextDatasetServiceLookupTime;

        public EntityMaterialSetSO DefaultMaterialSet => defaultMaterialSet;
        public IReadOnlyList<EntityMaterialSlotBinding> SlotBindings => slotBindings;
        public string CurrentDatasetKind => currentDatasetKind;

        private void Awake()
        {
            TryApplyInitialDataset(logFailure: true);
        }

        private void OnEnable()
        {
            TryRegisterToDatasetService();
        }

        private void LateUpdate()
        {
            if (!isRegisteredToDatasetService)
                TryRegisterToDatasetService();
        }

        private void OnDisable()
        {
            UnregisterFromDatasetService();
        }

        private void OnValidate()
        {
            if (!ValidateDefinition(out string failureReason))
                Debug.LogError($"{nameof(EntityMaterialControllerMB)}: {failureReason}", this);
        }

        public bool TryApply(EntityMaterialApplyRequest request, out string failureReason)
        {
            if (!TryBuildAssignments(request, out List<RendererMaterialAssignment> assignments, out failureReason))
                return false;

            for (int i = 0; i < assignments.Count; i++)
            {
                RendererMaterialAssignment assignment = assignments[i];
                assignment.Renderer.sharedMaterials = assignment.Materials;
            }

            currentDatasetKind = EntityMaterialSetSO.NormalizeDatasetKind(request.datasetKind);
            return true;
        }

        public bool TryApplyDatasetKind(string datasetKind, out string failureReason)
        {
            return TryApply(new EntityMaterialApplyRequest
            {
                overrideSet = defaultMaterialSet,
                datasetKind = datasetKind,
            }, out failureReason);
        }

        public bool ResetToDefault(out string failureReason)
        {
            return TryApplyDatasetKind(EntityMaterialSetSO.DefaultDatasetKind, out failureReason);
        }

        public bool ValidateDefinition(out string failureReason)
        {
            if (slotBindings == null || slotBindings.Count == 0)
            {
                failureReason = $"{name}: no material slot bindings are defined.";
                return false;
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            var locations = new HashSet<RendererSlotLocation>();

            for (int i = 0; i < slotBindings.Count; i++)
            {
                EntityMaterialSlotBinding binding = slotBindings[i];
                string normalizedKey = EntityMaterialSetSO.NormalizeSlotKey(binding.SlotKey);

                if (string.IsNullOrWhiteSpace(normalizedKey))
                {
                    failureReason = $"{name}: slot binding at index {i} has an empty slot key.";
                    return false;
                }

                if (!keys.Add(normalizedKey))
                {
                    failureReason = $"{name}: duplicate slot key '{normalizedKey}'.";
                    return false;
                }

                if (binding.TargetRenderer == null)
                {
                    failureReason = $"{name}: slot '{normalizedKey}' does not assign a renderer.";
                    return false;
                }

                Material[] sharedMaterials = binding.TargetRenderer.sharedMaterials;
                if (binding.MaterialIndex < 0 || binding.MaterialIndex >= sharedMaterials.Length)
                {
                    failureReason = $"{name}: slot '{normalizedKey}' references material index {binding.MaterialIndex}, but renderer '{binding.TargetRenderer.name}' has {sharedMaterials.Length} material slots.";
                    return false;
                }

                RendererSlotLocation location = new RendererSlotLocation(binding.TargetRenderer, binding.MaterialIndex);
                if (!locations.Add(location))
                {
                    failureReason = $"{name}: renderer '{binding.TargetRenderer.name}' material index {binding.MaterialIndex} is bound more than once.";
                    return false;
                }
            }

            if (defaultMaterialSet == null)
            {
                failureReason = $"{name}: default material set is not assigned.";
                return false;
            }

            if (!defaultMaterialSet.ValidateDefinition(out failureReason))
            {
                failureReason = $"{name}: {failureReason}";
                return false;
            }

            return true;
        }

        internal bool CanApply(in EntityMaterialApplyRequest request, out string failureReason)
        {
            return TryBuildAssignments(request, out _, out failureReason);
        }

        [Button("Auto Collect Slots")]
        [ContextMenu("Auto Collect Slots")]
        private void AutoCollectSlots()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            var existingKeys = new Dictionary<RendererSlotLocation, string>();

            if (slotBindings != null)
            {
                for (int i = 0; i < slotBindings.Count; i++)
                {
                    EntityMaterialSlotBinding binding = slotBindings[i];
                    if (binding.TargetRenderer == null)
                        continue;

                    existingKeys[new RendererSlotLocation(binding.TargetRenderer, binding.MaterialIndex)] = binding.SlotKey;
                }
            }

            var collectedBindings = new List<EntityMaterialSlotBinding>(renderers.Length);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null)
                    continue;

                Material[] sharedMaterials = renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                {
                    string defaultKey = $"{renderer.name}[{materialIndex}]";
                    RendererSlotLocation location = new RendererSlotLocation(renderer, materialIndex);
                    if (existingKeys.TryGetValue(location, out string preservedKey) &&
                        !string.IsNullOrWhiteSpace(preservedKey))
                    {
                        defaultKey = preservedKey;
                    }

                    collectedBindings.Add(new EntityMaterialSlotBinding(defaultKey, renderer, materialIndex));
                }
            }

            slotBindings = collectedBindings;
        }

        private void TryApplyInitialDataset(bool logFailure)
        {
            if (hasTriedInitialApply)
                return;

            hasTriedInitialApply = true;

            string initialDatasetKind = ResolveInitialDatasetKind();
            string failureReason = null;
            if (!string.IsNullOrWhiteSpace(initialDatasetKind) &&
                TryApplyDatasetKind(initialDatasetKind, out failureReason))
                return;

            if (logFailure && !string.IsNullOrWhiteSpace(initialDatasetKind))
                Debug.LogError($"{nameof(EntityMaterialControllerMB)}: failed to apply initial dataset '{initialDatasetKind}'. {failureReason}", this);
        }

        private void TryRegisterToDatasetService()
        {
            if (isRegisteredToDatasetService)
                return;

            datasetService ??= EntityMaterialDatasetServiceMB.Instance;
            if (datasetService == null && Time.unscaledTime >= nextDatasetServiceLookupTime)
            {
                datasetService = FindAnyObjectByType<EntityMaterialDatasetServiceMB>(FindObjectsInactive.Include);
                nextDatasetServiceLookupTime = Time.unscaledTime + 0.5f;
            }

            if (datasetService == null)
                return;

            datasetService.RegisterController(this);
            isRegisteredToDatasetService = true;
        }

        private string ResolveInitialDatasetKind()
        {
            if (defaultMaterialSet == null)
                return EntityMaterialSetSO.DefaultDatasetKind;

            datasetService ??= EntityMaterialDatasetServiceMB.Instance;
            if (datasetService != null &&
                !string.IsNullOrWhiteSpace(datasetService.ActiveDatasetKind) &&
                defaultMaterialSet.HasDatasetKind(datasetService.ActiveDatasetKind))
            {
                return datasetService.ActiveDatasetKind;
            }

            GameLogicManagerMB gameLogicManager = GameLogicManagerMB.Instance;
            if (gameLogicManager != null &&
                !string.IsNullOrWhiteSpace(gameLogicManager.CurrentEntityMaterialDatasetKind) &&
                defaultMaterialSet.HasDatasetKind(gameLogicManager.CurrentEntityMaterialDatasetKind))
            {
                return gameLogicManager.CurrentEntityMaterialDatasetKind;
            }

            if (defaultMaterialSet.HasDatasetKind(EntityMaterialSetSO.DefaultDatasetKind))
                return EntityMaterialSetSO.DefaultDatasetKind;

            if (defaultMaterialSet.TryGetFirstDatasetKind(out string firstDatasetKind))
                return firstDatasetKind;

            return string.Empty;
        }

        private void UnregisterFromDatasetService()
        {
            if (!isRegisteredToDatasetService)
                return;

            if (datasetService != null)
                datasetService.UnregisterController(this);

            isRegisteredToDatasetService = false;
        }

        private bool TryBuildAssignments(
            in EntityMaterialApplyRequest request,
            out List<RendererMaterialAssignment> assignments,
            out string failureReason)
        {
            assignments = null;

            if (!TryValidateBindings(out List<ValidatedBinding> validatedBindings, out failureReason))
                return false;

            EntityMaterialApplyRequest sanitizedRequest = request.Sanitize();
            EntityMaterialSetSO materialSet = sanitizedRequest.overrideSet != null
                ? sanitizedRequest.overrideSet
                : defaultMaterialSet;

            if (materialSet == null)
            {
                failureReason = $"{name}: material set is not assigned.";
                return false;
            }

            if (!materialSet.TryBuildMaterialMap(sanitizedRequest.datasetKind, out Dictionary<string, Material> materialsByKey, out failureReason))
            {
                failureReason = $"{name}: {failureReason}";
                return false;
            }

            var assignmentsByRenderer = new Dictionary<Renderer, Material[]>();
            for (int i = 0; i < validatedBindings.Count; i++)
            {
                ValidatedBinding binding = validatedBindings[i];
                if (!materialsByKey.TryGetValue(binding.SlotKey, out Material targetMaterial))
                {
                    failureReason = $"{name}: dataset '{sanitizedRequest.datasetKind}' does not define slot key '{binding.SlotKey}'.";
                    return false;
                }

                if (!assignmentsByRenderer.TryGetValue(binding.Renderer, out Material[] sharedMaterials))
                {
                    sharedMaterials = (Material[])binding.Renderer.sharedMaterials.Clone();
                    assignmentsByRenderer.Add(binding.Renderer, sharedMaterials);
                }

                sharedMaterials[binding.MaterialIndex] = targetMaterial;
            }

            assignments = new List<RendererMaterialAssignment>(assignmentsByRenderer.Count);
            foreach ((Renderer renderer, Material[] sharedMaterials) in assignmentsByRenderer)
            {
                assignments.Add(new RendererMaterialAssignment(renderer, sharedMaterials));
            }

            return true;
        }

        private bool TryValidateBindings(out List<ValidatedBinding> validatedBindings, out string failureReason)
        {
            validatedBindings = null;

            if (!ValidateDefinition(out failureReason))
                return false;

            validatedBindings = new List<ValidatedBinding>(slotBindings.Count);
            for (int i = 0; i < slotBindings.Count; i++)
            {
                EntityMaterialSlotBinding binding = slotBindings[i];
                validatedBindings.Add(new ValidatedBinding(
                    EntityMaterialSetSO.NormalizeSlotKey(binding.SlotKey),
                    binding.TargetRenderer,
                    binding.MaterialIndex));
            }

            return true;
        }

        private readonly struct ValidatedBinding
        {
            public ValidatedBinding(string slotKey, Renderer renderer, int materialIndex)
            {
                SlotKey = slotKey;
                Renderer = renderer;
                MaterialIndex = materialIndex;
            }

            public string SlotKey { get; }
            public Renderer Renderer { get; }
            public int MaterialIndex { get; }
        }

        private readonly struct RendererMaterialAssignment
        {
            public RendererMaterialAssignment(Renderer renderer, Material[] materials)
            {
                Renderer = renderer;
                Materials = materials;
            }

            public Renderer Renderer { get; }
            public Material[] Materials { get; }
        }

        private readonly struct RendererSlotLocation : IEquatable<RendererSlotLocation>
        {
            private readonly Renderer renderer;
            private readonly int materialIndex;

            public RendererSlotLocation(Renderer renderer, int materialIndex)
            {
                this.renderer = renderer;
                this.materialIndex = materialIndex;
            }

            public bool Equals(RendererSlotLocation other)
            {
                return ReferenceEquals(renderer, other.renderer) && materialIndex == other.materialIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is RendererSlotLocation other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((renderer != null ? renderer.GetHashCode() : 0) * 397) ^ materialIndex;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.Character
{
    [DisallowMultipleComponent]
    public sealed class ShapeBlendMappingMB : MonoBehaviour
    {
#pragma warning disable CS0649
        [Serializable]
        private struct BlendShapeBindingAuthoring
        {
            [Tooltip("Alias can decouple animation clips from mesh blendshape names. Leave empty to use BlendShape Name.")]
            public string alias;
            public SkinnedMeshRenderer skinnedMeshRenderer;
            public string blendShapeName;
            [Range(0f, 100f)]
            public float defaultWeight;
        }

        private struct BlendShapeBindingRuntime
        {
            public string Alias;
            public SkinnedMeshRenderer Renderer;
            public int BlendShapeIndex;
            public float DefaultWeight;
            public int ControllerOwnedFrame;
        }
#pragma warning restore CS0649

        [Header("Bindings")]
        [SerializeField] private BlendShapeBindingAuthoring[] bindings = Array.Empty<BlendShapeBindingAuthoring>();

        [Header("Runtime")]
        [SerializeField] private bool resetToDefaultOnDisable = true;

        private readonly Dictionary<string, int> aliasToIndex = new(StringComparer.Ordinal);
        private BlendShapeBindingRuntime[] runtimeBindings = Array.Empty<BlendShapeBindingRuntime>();
        private bool initialized;

        public int AliasCount
        {
            get
            {
                Initialize(logWarnings: false);
                return runtimeBindings.Length;
            }
        }

        private void Reset()
        {
            AutoAssignMissingRenderers();
        }

        private void OnValidate()
        {
            AutoAssignMissingRenderers();
            initialized = false;
        }

        private void Awake()
        {
            Initialize(logWarnings: true);
        }

        private void OnDisable()
        {
            if (!resetToDefaultOnDisable)
                return;

            ResetMappedWeights();
        }

        public bool TryGetAliasIndex(string alias, out int aliasIndex)
        {
            if (!Initialize(logWarnings: false))
            {
                aliasIndex = -1;
                return false;
            }

            return aliasToIndex.TryGetValue(alias ?? string.Empty, out aliasIndex);
        }

        public bool TryGetAliasName(int aliasIndex, out string alias)
        {
            if (!Initialize(logWarnings: false) || aliasIndex < 0 || aliasIndex >= runtimeBindings.Length)
            {
                alias = string.Empty;
                return false;
            }

            alias = runtimeBindings[aliasIndex].Alias;
            return true;
        }

        public bool TryGetDefaultWeight(int aliasIndex, out float defaultWeight)
        {
            if (!Initialize(logWarnings: false) || aliasIndex < 0 || aliasIndex >= runtimeBindings.Length)
            {
                defaultWeight = 0f;
                return false;
            }

            defaultWeight = runtimeBindings[aliasIndex].DefaultWeight;
            return true;
        }

        public bool TrySetWeight(string alias, float weight)
        {
            return TryGetAliasIndex(alias, out int aliasIndex) && TrySetWeight(aliasIndex, weight);
        }

        public bool TrySetWeight(int aliasIndex, float weight)
        {
            if (!Initialize(logWarnings: false) || aliasIndex < 0 || aliasIndex >= runtimeBindings.Length)
                return false;

            if (runtimeBindings[aliasIndex].ControllerOwnedFrame == Time.frameCount)
                return false;

            return ApplyWeight(aliasIndex, weight);
        }

        public bool TrySetWeightFromController(int aliasIndex, float weight, int frameCount)
        {
            if (!Initialize(logWarnings: false) || aliasIndex < 0 || aliasIndex >= runtimeBindings.Length)
                return false;

            BlendShapeBindingRuntime binding = runtimeBindings[aliasIndex];
            binding.ControllerOwnedFrame = frameCount;
            runtimeBindings[aliasIndex] = binding;
            return ApplyWeight(aliasIndex, weight);
        }

        public bool TryGetWeight(string alias, out float weight)
        {
            if (!TryGetAliasIndex(alias, out int aliasIndex))
            {
                weight = 0f;
                return false;
            }

            return TryGetWeight(aliasIndex, out weight);
        }

        public bool TryGetWeight(int aliasIndex, out float weight)
        {
            if (!Initialize(logWarnings: false) || aliasIndex < 0 || aliasIndex >= runtimeBindings.Length)
            {
                weight = 0f;
                return false;
            }

            BlendShapeBindingRuntime binding = runtimeBindings[aliasIndex];

            // bind 後に SkinnedMeshRenderer が破棄される場合がある（顔メッシュだけ差し替え/破棄など）。
            // 他のアクセサ(ApplyWeight)と同じく、参照が無ければ NRE ではなく Try* 契約どおり false を返す。
            if (binding.Renderer == null)
            {
                weight = 0f;
                return false;
            }

            weight = binding.Renderer.GetBlendShapeWeight(binding.BlendShapeIndex);
            return true;
        }

        public void ResetMappedWeights()
        {
            if (!Initialize(logWarnings: false))
                return;

            for (int i = 0; i < runtimeBindings.Length; i++)
                ApplyWeight(i, runtimeBindings[i].DefaultWeight);
        }

        private void AutoAssignMissingRenderers()
        {
            SkinnedMeshRenderer fallbackRenderer = GetComponent<SkinnedMeshRenderer>();

            if (bindings == null)
                return;

            for (int i = 0; i < bindings.Length; i++)
            {
                if (bindings[i].skinnedMeshRenderer == null)
                    bindings[i].skinnedMeshRenderer = fallbackRenderer;
            }
        }

        private bool Initialize(bool logWarnings)
        {
            if (initialized)
                return runtimeBindings.Length > 0;

            initialized = true;
            aliasToIndex.Clear();

            if (bindings == null || bindings.Length == 0)
            {
                runtimeBindings = Array.Empty<BlendShapeBindingRuntime>();
                if (logWarnings)
                    Debug.LogWarning($"{nameof(ShapeBlendMappingMB)}: no blendshape bindings are configured.", this);

                return false;
            }

            List<BlendShapeBindingRuntime> builtBindings = new(bindings.Length);

            for (int i = 0; i < bindings.Length; i++)
            {
                BlendShapeBindingAuthoring authoring = bindings[i];

                if (authoring.skinnedMeshRenderer == null)
                {
                    if (logWarnings)
                        Debug.LogWarning($"{nameof(ShapeBlendMappingMB)}: binding[{i}] has no renderer and was skipped.", this);
                    continue;
                }

                Mesh sharedMesh = authoring.skinnedMeshRenderer.sharedMesh;
                if (sharedMesh == null)
                {
                    if (logWarnings)
                        Debug.LogWarning($"{nameof(ShapeBlendMappingMB)}: binding[{i}] renderer has no sharedMesh and was skipped.", this);
                    continue;
                }

                string blendShapeName = string.IsNullOrWhiteSpace(authoring.blendShapeName) ? string.Empty : authoring.blendShapeName.Trim();
                if (string.IsNullOrEmpty(blendShapeName))
                {
                    if (logWarnings)
                        Debug.LogWarning($"{nameof(ShapeBlendMappingMB)}: binding[{i}] has an empty blendShapeName and was skipped.", this);
                    continue;
                }

                int blendShapeIndex = sharedMesh.GetBlendShapeIndex(blendShapeName);
                if (blendShapeIndex < 0)
                {
                    if (logWarnings)
                        Debug.LogWarning($"{nameof(ShapeBlendMappingMB)}: blendshape '{blendShapeName}' was not found on '{authoring.skinnedMeshRenderer.name}'.", this);
                    continue;
                }

                string alias = string.IsNullOrWhiteSpace(authoring.alias) ? blendShapeName : authoring.alias.Trim();
                if (aliasToIndex.ContainsKey(alias))
                {
                    if (logWarnings)
                        Debug.LogWarning($"{nameof(ShapeBlendMappingMB)}: duplicate alias '{alias}' was skipped.", this);
                    continue;
                }

                BlendShapeBindingRuntime runtimeBinding = new BlendShapeBindingRuntime
                {
                    Alias = alias,
                    Renderer = authoring.skinnedMeshRenderer,
                    BlendShapeIndex = blendShapeIndex,
                    DefaultWeight = Mathf.Clamp(authoring.defaultWeight, 0f, 100f),
                    ControllerOwnedFrame = -1,
                };

                aliasToIndex.Add(alias, builtBindings.Count);
                builtBindings.Add(runtimeBinding);
            }

            runtimeBindings = builtBindings.ToArray();
            return runtimeBindings.Length > 0;
        }

        private bool ApplyWeight(int aliasIndex, float weight)
        {
            BlendShapeBindingRuntime binding = runtimeBindings[aliasIndex];

            if (binding.Renderer == null)
                return false;

            binding.Renderer.SetBlendShapeWeight(binding.BlendShapeIndex, Mathf.Clamp(weight, 0f, 100f));
            return true;
        }
    }
}

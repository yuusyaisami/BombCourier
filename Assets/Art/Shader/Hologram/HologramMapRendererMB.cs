using UnityEngine;
using UnityEngine.Serialization;

namespace BombCourier.Rendering.Hologram
{
    /// <summary>
    /// Component that converts a source mesh into a hologram mesh at runtime and
    /// assigns a hologram material for rendering. This behaviour can optionally
    /// hide the original renderer to avoid drawing both the source and hologram
    /// simultaneously.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HologramMapRendererMB : MonoBehaviour
    {
        [Header("Source Settings")]
        [Tooltip("Mesh filter containing the original model to convert.")]
        [SerializeField] private MeshFilter sourceMeshFilter;

        [Header("Target Settings")]
        [Tooltip("Renderer used to display the hologram mesh.")]
        [SerializeField] private MeshRenderer targetRenderer;

        [Tooltip("Material used for hologram rendering. Should reference the BC/Hologram/HologramWireMap shader.")]
        [SerializeField] private Material hologramMaterial;

        [Tooltip("Automatically generate the hologram mesh on Awake.")]
        [SerializeField] private bool generateOnAwake = true;

        [Tooltip("Hide the source renderer after generating the hologram mesh.")]
        [SerializeField] private bool hideSourceRenderer = true;

        private MeshFilter _targetMeshFilter;

        // ランタイム生成した hologram mesh を保持する。Mesh は GC されない native リソースなので、
        // 再生成時とオブジェクト破棄時に明示的に Destroy しないと scene 再ロード等でリークする。
        private Mesh _generatedMesh;

        private void Awake()
        {
            // Ensure the target mesh filter is available on this GameObject
            _targetMeshFilter = GetComponent<MeshFilter>();

            if (generateOnAwake)
            {
                GenerateHologramMesh();
            }
        }

        /// <summary>
        /// Converts the source mesh into a hologram mesh and applies the material.
        /// Can be called manually from the context menu.
        /// </summary>
        [ContextMenu("Generate Hologram Mesh")]
        public void GenerateHologramMesh()
        {
            if (sourceMeshFilter == null)
            {
                Debug.LogError("HologramMapRendererMB: Source mesh filter is not assigned.");
                return;
            }
            if (_targetMeshFilter == null)
            {
                Debug.LogError("HologramMapRendererMB: Target mesh filter missing on this object.");
                return;
            }
            Mesh sourceMesh = sourceMeshFilter.sharedMesh;
            if (sourceMesh == null)
            {
                Debug.LogError("HologramMapRendererMB: Source mesh filter has no mesh.");
                return;
            }

            // Generate hologram mesh using the builder
            Mesh holoMesh = HologramMeshBuilder.Build(sourceMesh);
            if (holoMesh == null)
            {
                Debug.LogError("HologramMapRendererMB: Failed to build hologram mesh from source mesh.");
                return;
            }

            // 以前に生成した hologram mesh が残っていれば、差し替え前に破棄してリークを防ぐ。
            if (_generatedMesh != null && _generatedMesh != holoMesh)
                DestroyGeneratedMesh();

            _generatedMesh = holoMesh;
            _targetMeshFilter.sharedMesh = holoMesh;

            // Apply the hologram material to the target renderer
            if (targetRenderer != null && hologramMaterial != null)
            {
                targetRenderer.sharedMaterial = hologramMaterial;
            }
            else if (targetRenderer != null)
            {
                Debug.LogWarning("HologramMapRendererMB: Hologram material not assigned.");
            }

            // Optionally hide the original renderer to avoid double drawing
            if (hideSourceRenderer)
            {
                var srcRenderer = sourceMeshFilter.GetComponent<MeshRenderer>();
                if (srcRenderer != null)
                {
                    srcRenderer.enabled = false;
                }
            }
        }

        private void OnDestroy()
        {
            // 生成済み hologram mesh を破棄する（native リソースの確実な解放）。
            DestroyGeneratedMesh();
        }

        private void DestroyGeneratedMesh()
        {
            if (_generatedMesh == null)
                return;

            // 再生成は edit/play 双方から起こり得るため、文脈に応じた破棄 API を使う。
            if (Application.isPlaying)
                Destroy(_generatedMesh);
            else
                DestroyImmediate(_generatedMesh);

            _generatedMesh = null;
        }
    }
}
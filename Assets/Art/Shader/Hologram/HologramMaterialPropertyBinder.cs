using UnityEngine;

namespace BombCourier.Rendering.Hologram
{
    /// <summary>
    /// Helper component that manages a MaterialPropertyBlock for a renderer. This
    /// allows multiple hologram instances to share a material while maintaining
    /// per-instance parameter overrides for animation and control. The binder
    /// exposes methods to set floats and vectors on the property block.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class HologramMaterialPropertyBinder : MonoBehaviour
    {
        private MaterialPropertyBlock _propertyBlock;
        private MeshRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
            // Initialize the block to current values so that default material properties are respected
            _renderer.GetPropertyBlock(_propertyBlock);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Sets a float property on the renderer's MaterialPropertyBlock.
        /// </summary>
        public void SetFloat(string propertyName, float value)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(propertyName, value);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Sets a float property on the renderer's MaterialPropertyBlock using a pre-hashed property ID.
        /// </summary>
        public void SetFloat(int nameID, float value)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(nameID, value);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Sets a Vector4 property on the renderer's MaterialPropertyBlock.
        /// </summary>
        public void SetVector(string propertyName, Vector4 value)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetVector(propertyName, value);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Sets a Vector4 property on the renderer's MaterialPropertyBlock using a pre-hashed property ID.
        /// </summary>
        public void SetVector(int nameID, Vector4 value)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetVector(nameID, value);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Sets a color property on the renderer's MaterialPropertyBlock.
        /// </summary>
        public void SetColor(string propertyName, Color value)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(propertyName, value);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Sets a color property on the renderer's MaterialPropertyBlock using a pre-hashed property ID.
        /// </summary>
        public void SetColor(int nameID, Color value)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(nameID, value);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
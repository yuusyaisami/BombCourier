using System.Collections;
using UnityEngine;

namespace BombCourier.Rendering.Hologram
{
    /// <summary>
    /// Runtime controller that exposes methods to animate and configure the
    /// hologram rendering effects. It manipulates MaterialPropertyBlock
    /// parameters via an associated HologramMaterialPropertyBinder. Call these
    /// methods from gameplay logic to show, hide, and animate hologram maps.
    /// </summary>
    [RequireComponent(typeof(HologramMaterialPropertyBinder))]
    public class HologramMapController : MonoBehaviour
    {
        [Header("Reveal Animation")]
        [Tooltip("Duration in seconds for the reveal animation.")]
        [SerializeField] private float revealDuration = 1.0f;

        [Header("Emission Control")]
        [Tooltip("Default emission strength when the hologram is shown.")]
        [SerializeField] private float defaultEmissionStrength = 2.0f;

        private HologramMaterialPropertyBinder _binder;
        private Coroutine _revealCoroutine;

        private static readonly int RevealProp = Shader.PropertyToID("_Reveal");
        private static readonly int PulseCenterProp = Shader.PropertyToID("_PulseCenter");
        private static readonly int EmissionProp = Shader.PropertyToID("_EmissionStrength");
        private static readonly int GlitchProp = Shader.PropertyToID("_GlitchStrength");
        private static readonly int ScanSpeedProp = Shader.PropertyToID("_ScanSpeed");

        private void Awake()
        {
            _binder = GetComponent<HologramMaterialPropertyBinder>();
            // Initialize reveal and emission to hidden state
            _binder.SetFloat(RevealProp, 0f);
            _binder.SetFloat(EmissionProp, defaultEmissionStrength);
        }

        /// <summary>
        /// Immediately shows the hologram by setting the reveal to 1.
        /// </summary>
        public void Show()
        {
            StopRevealCoroutine();
            _binder.SetFloat(RevealProp, 1f);
            _binder.SetFloat(EmissionProp, defaultEmissionStrength);
        }

        /// <summary>
        /// Immediately hides the hologram by setting the reveal to 0.
        /// </summary>
        public void Hide()
        {
            StopRevealCoroutine();
            _binder.SetFloat(RevealProp, 0f);
        }

        /// <summary>
        /// Sets the reveal value directly. 0 = hidden, 1 = fully shown.
        /// </summary>
        public void SetReveal(float value)
        {
            StopRevealCoroutine();
            float clamped = Mathf.Clamp01(value);
            _binder.SetFloat(RevealProp, clamped);
        }

        /// <summary>
        /// Plays the reveal animation over the configured duration. Uses a cubic
        /// ease-out curve to smooth the animation. Starting from hidden.
        /// </summary>
        public void PlayReveal()
        {
            PlayReveal(revealDuration);
        }

        /// <summary>
        /// Plays the reveal animation over a custom duration.
        /// </summary>
        public void PlayReveal(float duration)
        {
            StopRevealCoroutine();
            _revealCoroutine = StartCoroutine(RevealCoroutine(duration));
        }

        /// <summary>
        /// Updates the pulse center used by the shader. The XZ components of the
        /// world position are used to compute the radial pulse.
        /// </summary>
        public void SetPulseCenter(Vector3 worldPosition)
        {
            _binder.SetVector(PulseCenterProp, new Vector4(worldPosition.x, worldPosition.y, worldPosition.z, 0f));
        }

        /// <summary>
        /// Triggers a pulse at the current object location. Convenience wrapper
        /// around SetPulseCenter.
        /// </summary>
        public void TriggerPulse()
        {
            SetPulseCenter(transform.position);
        }

        /// <summary>
        /// Sets the emission strength multiplier on the shader.
        /// </summary>
        public void SetEmission(float strength)
        {
            _binder.SetFloat(EmissionProp, strength);
        }

        /// <summary>
        /// Sets the glitch strength parameter. Use a small non-zero value for
        /// temporary glitch effects; return to zero for normal appearance.
        /// </summary>
        public void SetGlitch(float strength)
        {
            _binder.SetFloat(GlitchProp, Mathf.Max(0f, strength));
        }

        /// <summary>
        /// Sets the scan speed multiplier. Higher values cause the scan lines to
        /// move faster. Use a negative value to reverse direction.
        /// </summary>
        public void SetScanSpeed(float speed)
        {
            _binder.SetFloat(ScanSpeedProp, speed);
        }

        private IEnumerator RevealCoroutine(float duration)
        {
            // Start hidden
            _binder.SetFloat(RevealProp, 0f);
            _binder.SetFloat(EmissionProp, defaultEmissionStrength);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(duration, 0.0001f));
                // EaseOut cubic: f(t) = 1 - (1 - t)^3
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                _binder.SetFloat(RevealProp, eased);
                yield return null;
            }
            _binder.SetFloat(RevealProp, 1f);
            _revealCoroutine = null;
        }

        private void StopRevealCoroutine()
        {
            if (_revealCoroutine != null)
            {
                StopCoroutine(_revealCoroutine);
                _revealCoroutine = null;
            }
        }
    }
}
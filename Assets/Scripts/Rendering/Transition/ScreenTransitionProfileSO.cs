using UnityEngine;

namespace BC.Rendering.Transition
{
    [CreateAssetMenu(
        fileName = "ScreenTransitionProfile",
        menuName = "BC/Rendering/Transition/Screen Transition Profile")]
    public sealed class ScreenTransitionProfileSO : ScriptableObject
    {
        [Header("Core")]
        [SerializeField] private ScreenTransitionMode mode = ScreenTransitionMode.LinearCrossFade;
        [SerializeField, Min(0f)] private float duration = 0.35f;
        [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Mask")]
        [SerializeField, Range(0.0001f, 0.5f)] private float feather = 0.04f;
        [SerializeField] private Vector2 direction = Vector2.right;
        [SerializeField] private Vector2 center = new(0.5f, 0.5f);

        [Header("Blue Noise")]
        [SerializeField] private Texture2D noiseTexture;
        [SerializeField, Min(0.01f)] private float noiseScale = 8f;
        [SerializeField, Range(0f, 1f)] private float noiseStrength = 1f;
        [SerializeField] private float seed;

        public ScreenTransitionMode Mode => mode;
        public float Duration => duration;
        public AnimationCurve Easing => easing;
        public float Feather => feather;
        public Vector2 Direction => direction;
        public Vector2 Center => center;
        public Texture2D NoiseTexture => noiseTexture;
        public float NoiseScale => noiseScale;
        public float NoiseStrength => noiseStrength;
        public float Seed => seed;
    }
}

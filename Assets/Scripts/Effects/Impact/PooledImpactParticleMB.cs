using System;
using UnityEngine;

namespace BC.Effects.Impact
{
    [DisallowMultipleComponent]
    public sealed class PooledImpactParticleMB : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private ParticleSystem[] particleSystems;
        private ParticleSystemRenderer[] particleRenderers;
        private float[] baseStartSpeedMultipliers;
        private MaterialPropertyBlock propertyBlock;
        private Action<PooledImpactParticleMB> releaseAction;
        private bool releaseWhenStopped;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            if (!releaseWhenStopped)
                return;

            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];

                if (particleSystem != null && particleSystem.IsAlive(true))
                    return;
            }

            releaseWhenStopped = false;
            releaseAction?.Invoke(this);
        }

        public void Play(in ImpactEffectRequest request, Color baseColor, float speedMultiplier, float surfaceOffset, Action<PooledImpactParticleMB> onStopped)
        {
            EnsureInitialized();

            releaseAction = onStopped;
            releaseWhenStopped = false;

            Vector3 normal = request.Normal.sqrMagnitude > 0.0001f ? request.Normal.normalized : Vector3.up;
            transform.SetPositionAndRotation(
                request.Point + normal * Mathf.Max(0.0f, surfaceOffset),
                Quaternion.FromToRotation(Vector3.up, normal));

            ApplyColor(baseColor);
            ApplySpeedMultiplier(speedMultiplier);

            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];

                if (particleSystem == null)
                    continue;

                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Play(true);
            }

            releaseWhenStopped = true;
        }

        public void StopAndReset()
        {
            EnsureInitialized();
            releaseWhenStopped = false;
            releaseAction = null;

            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];

                if (particleSystem == null)
                    continue;

                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                ParticleSystem.MainModule main = particleSystem.main;
                main.startSpeedMultiplier = baseStartSpeedMultipliers[i];
            }

            for (int i = 0; i < particleRenderers.Length; i++)
            {
                ParticleSystemRenderer renderer = particleRenderers[i];

                if (renderer != null)
                    renderer.SetPropertyBlock(null);
            }
        }

        public void EnsureInitialized()
        {
            if (particleSystems != null && particleRenderers != null && propertyBlock != null)
                return;

            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            particleRenderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
            baseStartSpeedMultipliers = new float[particleSystems.Length];

            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];
                baseStartSpeedMultipliers[i] = particleSystem != null ? particleSystem.main.startSpeedMultiplier : 1.0f;
            }

            propertyBlock = new MaterialPropertyBlock();
        }

        private void ApplyColor(Color baseColor)
        {
            for (int i = 0; i < particleRenderers.Length; i++)
            {
                ParticleSystemRenderer renderer = particleRenderers[i];

                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, baseColor);
                propertyBlock.SetColor(ColorId, baseColor);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void ApplySpeedMultiplier(float speedMultiplier)
        {
            float clampedMultiplier = Mathf.Max(0.0f, speedMultiplier);

            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];

                if (particleSystem == null)
                    continue;

                ParticleSystem.MainModule main = particleSystem.main;
                main.startSpeedMultiplier = baseStartSpeedMultipliers[i] * clampedMultiplier;
            }
        }
    }
}
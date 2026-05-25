using UnityEngine;

namespace BC.Base
{
    public static class MovementPhysicsMaterialFactory
    {
        public static void EnsureLowFrictionMaterial(CapsuleCollider bodyCollider, ref PhysicsMaterial lowFrictionMaterial)
        {
            if (bodyCollider == null)
                return;

            if (lowFrictionMaterial == null)
            {
                lowFrictionMaterial = new PhysicsMaterial($"{nameof(EntityMoveMotorMB)}_LowFriction")
                {
                    dynamicFriction = 0.0f,
                    staticFriction = 0.0f,
                    bounciness = 0.0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum,
                };

                lowFrictionMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            if (bodyCollider.sharedMaterial != lowFrictionMaterial)
                bodyCollider.sharedMaterial = lowFrictionMaterial;
        }

        public static void Release(ref PhysicsMaterial lowFrictionMaterial)
        {
            if (lowFrictionMaterial == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(lowFrictionMaterial);
            else
                Object.DestroyImmediate(lowFrictionMaterial);

            lowFrictionMaterial = null;
        }
    }
}
using UnityEngine;

namespace BC.Item
{
    public interface ICarryableItem
    {
        Transform ItemTransform { get; }
        bool IsHandled { get; }
        bool CanBeCarried { get; }

        void OnHandle(Transform handlePoint);
        void OnRelease(Vector3 throwVelocity);
    }

    public interface ICarryMoveModifier
    {
        bool TryGetJumpHeightMultiplier(out float jumpHeightMultiplier);
    }
}

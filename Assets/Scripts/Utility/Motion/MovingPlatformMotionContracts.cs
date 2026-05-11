using UnityEngine;

namespace BC.Base
{
    public readonly struct MovingPlatformPassengerMotion
    {
        public readonly Vector3 Delta;
        public readonly Vector3 Velocity;

        public MovingPlatformPassengerMotion(Vector3 delta, Vector3 velocity)
        {
            Delta = delta;
            Velocity = velocity;
        }
    }

    public interface IMovingPlatformMotionSource
    {
        // CharacterControllerはRigidbodyの接触解決だけでは床移動を継承しないため、足場側が明示的に乗客用デルタを渡す。
        bool TryGetPassengerMotion(Vector3 passengerWorldPosition, float deltaTime, out MovingPlatformPassengerMotion motion);
    }
}
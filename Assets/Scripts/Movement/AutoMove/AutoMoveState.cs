using System.Threading;
using UnityEngine;

namespace BC.Base
{
    public sealed class AutoMoveState
    {
        public CancellationTokenSource ActiveCancellationTokenSource;
        public Vector3 TargetPosition;
        public float ArrivalDistanceSqr;
        public bool IsActive;
        public bool ReachedTarget;
    }
}
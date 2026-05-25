using System;
using System.Threading;
using UnityEngine;

namespace BC.Base
{
    public sealed class AutoMoveDriver
    {
        public bool CanStart(
            bool isRuntimeReady,
            Rigidbody bodyRigidbody,
            CapsuleCollider bodyCollider,
            bool isMotionLocked,
            bool isDead,
            bool canApplySystemMovement)
        {
            return isRuntimeReady &&
                   bodyRigidbody != null &&
                   bodyCollider != null &&
                   !isMotionLocked &&
                   !isDead &&
                   canApplySystemMovement;
        }

        public bool IsAlreadyAtTarget(Rigidbody bodyRigidbody, Vector3 targetPosition, float arriveDistance)
        {
            if (bodyRigidbody == null)
                return false;

            Vector3 toTarget = targetPosition - bodyRigidbody.position;
            toTarget.y = 0.0f;

            return toTarget.sqrMagnitude <= Mathf.Max(0.0001f, arriveDistance * arriveDistance);
        }

        public CancellationTokenSource BeginNew(AutoMoveState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (state.ActiveCancellationTokenSource != null)
                state.ActiveCancellationTokenSource.Cancel();

            state.ActiveCancellationTokenSource = new CancellationTokenSource();
            return state.ActiveCancellationTokenSource;
        }

        public void BeginMove(AutoMoveState state, Vector3 targetPosition, float arriveDistance)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            state.TargetPosition = targetPosition;
            state.ArrivalDistanceSqr = Mathf.Max(0.0001f, arriveDistance * arriveDistance);
            state.ReachedTarget = false;
            state.IsActive = true;
        }

        public void Cancel(AutoMoveState state)
        {
            if (state == null)
                return;

            if (state.ActiveCancellationTokenSource != null)
                state.ActiveCancellationTokenSource.Cancel();

            state.IsActive = false;
            state.ReachedTarget = false;
        }

        public Vector3 BuildDirection(AutoMoveState state, Rigidbody bodyRigidbody)
        {
            if (state == null || bodyRigidbody == null)
                return Vector3.zero;

            Vector3 toTarget = state.TargetPosition - bodyRigidbody.position;
            toTarget.y = 0.0f;

            state.ReachedTarget = toTarget.sqrMagnitude <= state.ArrivalDistanceSqr;

            if (state.ReachedTarget)
                return Vector3.zero;

            return toTarget.normalized;
        }

        public void CompleteTarget(AutoMoveState state)
        {
            if (state == null)
                return;

            state.IsActive = false;
        }

        public void CompleteAndDispose(AutoMoveState state, CancellationTokenSource autoMoveCancellationTokenSource)
        {
            if (state == null)
                return;

            if (ReferenceEquals(state.ActiveCancellationTokenSource, autoMoveCancellationTokenSource))
                state.ActiveCancellationTokenSource = null;

            autoMoveCancellationTokenSource?.Dispose();
        }
    }
}
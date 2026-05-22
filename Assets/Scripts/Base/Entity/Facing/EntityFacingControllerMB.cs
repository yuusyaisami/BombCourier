using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.Base
{
    public static class EntityFacingChannels
    {
        public const string Movement = "Movement";
        public const string ThrowPose = "ThrowPose";
        public const string Interaction = "Interaction";
        public const string Talk = "Talk";
        public const string Action = "Action";
    }

    public static class EntityFacingPriorities
    {
        public const int Movement = 0;
        public const int ThrowPose = 100;
        public const int Interaction = 200;
        public const int Talk = 250;
        public const int Action = 300;
    }

    [DisallowMultipleComponent]
    public sealed class EntityFacingControllerMB : MonoBehaviour
    {
        [Header("Facing")]
        [SerializeField] private Transform facingRoot;
        [SerializeField, Min(0.01f)] private float defaultTurnSharpness = 16.0f;
        // モデルの「正面」とみなすローカル軸です。
        // 例えば見た目上の正面が -Z の mesh なら Vector3.back を指定します。
        [SerializeField] private Vector3 frontDirection = Vector3.forward;
        [SerializeField] private bool usePlanarYawOnly = true;

        // channel ごとに 1 つだけ facing 要求を保持し、priority と更新順で勝者を決めます。
        private readonly Dictionary<string, FacingRequest> requestsByChannel = new(StringComparer.Ordinal);
        private int nextRevision = 1;

        public Transform FacingRoot => facingRoot != null ? facingRoot : transform;

        // 他の component からも「今この entity がどちらを正面として扱っているか」を
        // 同じ定義で参照できるように、frontDirection と facingRoot を解決した world 方向を返します。
        public bool TryGetWorldFrontDirection(out Vector3 worldFrontDirection)
        {
            Transform root = FacingRoot;
            if (root == null)
            {
                worldFrontDirection = Vector3.forward;
                return false;
            }

            worldFrontDirection = root.rotation * ResolveNormalizedFrontDirection();

            if (usePlanarYawOnly)
                worldFrontDirection.y = 0.0f;

            if (worldFrontDirection.sqrMagnitude <= 0.0001f)
            {
                worldFrontDirection = Vector3.forward;
                return false;
            }

            worldFrontDirection.Normalize();
            return true;
        }

        private void Reset()
        {
            facingRoot = transform;
        }

        private void OnValidate()
        {
            if (facingRoot == null)
                facingRoot = transform;

            defaultTurnSharpness = Mathf.Max(0.01f, defaultTurnSharpness);

            if (frontDirection.sqrMagnitude <= 0.0001f)
                frontDirection = Vector3.forward;
        }

        private void LateUpdate()
        {
            ApplyFacing(Time.deltaTime);
        }

        public void SetFacingDirection(string channel, Vector3 worldDirection, int priority, float turnSharpness = -1.0f)
        {
            if (string.IsNullOrWhiteSpace(channel))
                return;

            if (!TryNormalizeDirection(worldDirection, out Vector3 normalizedDirection))
            {
                ClearFacing(channel);
                return;
            }

            requestsByChannel[channel] = FacingRequest.CreateDirection(
                normalizedDirection,
                priority,
                NormalizeTurnSharpness(turnSharpness),
                nextRevision++);
        }

        public void SetFacingTargetPosition(string channel, Vector3 worldPosition, int priority, float turnSharpness = -1.0f)
        {
            if (string.IsNullOrWhiteSpace(channel))
                return;

            requestsByChannel[channel] = FacingRequest.CreatePosition(
                worldPosition,
                priority,
                NormalizeTurnSharpness(turnSharpness),
                nextRevision++);
        }

        public void SetFacingTargetTransform(string channel, Transform targetTransform, int priority, float turnSharpness = -1.0f)
        {
            if (string.IsNullOrWhiteSpace(channel))
                return;

            if (targetTransform == null)
            {
                ClearFacing(channel);
                return;
            }

            requestsByChannel[channel] = FacingRequest.CreateTransform(
                targetTransform,
                priority,
                NormalizeTurnSharpness(turnSharpness),
                nextRevision++);
        }

        public bool ClearFacing(string channel)
        {
            return !string.IsNullOrWhiteSpace(channel) && requestsByChannel.Remove(channel);
        }

        public void ClearAllFacing()
        {
            requestsByChannel.Clear();
        }

        private void ApplyFacing(float deltaTime)
        {
            Transform root = FacingRoot;

            if (root == null || deltaTime <= 0.0f)
                return;

            if (!TryGetActiveRequest(root, out FacingRequest request, out Vector3 direction))
                return;

            // LookRotation(direction) だけだと「local forward が正面」の前提に固定されてしまい、
            // frontDirection を back/right に変えても最終姿勢が変わりません。
            // ここでは「frontDirection が direction を向く」回転を直接組み立てます。
            Quaternion targetRotation = Quaternion.FromToRotation(ResolveNormalizedFrontDirection(), direction);
            float blend = 1.0f - Mathf.Exp(-request.TurnSharpness * deltaTime);
            root.rotation = Quaternion.Slerp(root.rotation, targetRotation, blend);
        }

        private bool TryGetActiveRequest(Transform root, out FacingRequest activeRequest, out Vector3 direction)
        {
            activeRequest = default;
            direction = Vector3.zero;
            bool found = false;

            foreach (KeyValuePair<string, FacingRequest> pair in requestsByChannel)
            {
                FacingRequest request = pair.Value;

                if (!request.TryGetDirection(root.position, usePlanarYawOnly, out Vector3 candidateDirection))
                    continue;

                // priority が同じなら、最後に更新された request を優先して自然に上書きできるようにします。
                if (!found || request.Priority > activeRequest.Priority ||
                    (request.Priority == activeRequest.Priority && request.Revision > activeRequest.Revision))
                {
                    activeRequest = request;
                    direction = candidateDirection;
                    found = true;
                }
            }

            return found;
        }

        private float NormalizeTurnSharpness(float turnSharpness)
        {
            return turnSharpness > 0.0f ? turnSharpness : defaultTurnSharpness;
        }

        private Vector3 ResolveNormalizedFrontDirection()
        {
            Vector3 normalizedFrontDirection = frontDirection;

            if (usePlanarYawOnly)
                normalizedFrontDirection.y = 0.0f;

            if (normalizedFrontDirection.sqrMagnitude <= 0.0001f)
                normalizedFrontDirection = Vector3.forward;

            return normalizedFrontDirection.normalized;
        }

        private bool TryNormalizeDirection(Vector3 worldDirection, out Vector3 normalizedDirection)
        {
            if (usePlanarYawOnly)
                worldDirection.y = 0.0f;

            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                normalizedDirection = Vector3.zero;
                return false;
            }

            normalizedDirection = worldDirection.normalized;
            return true;
        }

        private enum FacingRequestMode
        {
            Direction = 0,
            Position = 1,
            Transform = 2,
        }

        private readonly struct FacingRequest
        {
            public readonly FacingRequestMode Mode;
            public readonly Vector3 WorldDirection;
            public readonly Vector3 WorldPosition;
            public readonly Transform TargetTransform;
            public readonly int Priority;
            public readonly float TurnSharpness;
            public readonly int Revision;

            private FacingRequest(
                FacingRequestMode mode,
                Vector3 worldDirection,
                Vector3 worldPosition,
                Transform targetTransform,
                int priority,
                float turnSharpness,
                int revision)
            {
                Mode = mode;
                WorldDirection = worldDirection;
                WorldPosition = worldPosition;
                TargetTransform = targetTransform;
                Priority = priority;
                TurnSharpness = turnSharpness;
                Revision = revision;
            }

            public static FacingRequest CreateDirection(Vector3 worldDirection, int priority, float turnSharpness, int revision)
            {
                return new FacingRequest(FacingRequestMode.Direction, worldDirection, default, null, priority, turnSharpness, revision);
            }

            public static FacingRequest CreatePosition(Vector3 worldPosition, int priority, float turnSharpness, int revision)
            {
                return new FacingRequest(FacingRequestMode.Position, default, worldPosition, null, priority, turnSharpness, revision);
            }

            public static FacingRequest CreateTransform(Transform targetTransform, int priority, float turnSharpness, int revision)
            {
                return new FacingRequest(FacingRequestMode.Transform, default, default, targetTransform, priority, turnSharpness, revision);
            }

            public bool TryGetDirection(Vector3 originPosition, bool usePlanarYawOnly, out Vector3 direction)
            {
                switch (Mode)
                {
                    case FacingRequestMode.Direction:
                        direction = WorldDirection;
                        break;

                    case FacingRequestMode.Position:
                        direction = WorldPosition - originPosition;
                        break;

                    case FacingRequestMode.Transform:
                        if (TargetTransform == null)
                        {
                            direction = Vector3.zero;
                            return false;
                        }

                        direction = TargetTransform.position - originPosition;
                        break;

                    default:
                        direction = Vector3.zero;
                        return false;
                }

                if (usePlanarYawOnly)
                    direction.y = 0.0f;

                if (direction.sqrMagnitude <= 0.0001f)
                {
                    direction = Vector3.zero;
                    return false;
                }

                direction.Normalize();
                return true;
            }
        }
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 現在の root rotation に対して、frontDirection がどちらを向いているかを可視化します。
            if (facingRoot != null)
            {
                Vector3 origin = facingRoot.position;
                Vector3 forward = facingRoot.rotation * ResolveNormalizedFrontDirection();
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, origin + forward);
            }
        }
#endif
    }
}
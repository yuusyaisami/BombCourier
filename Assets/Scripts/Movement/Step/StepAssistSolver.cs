using UnityEngine;

namespace BC.Base
{
    public static class StepAssistSolver
    {
        private const float SurfaceSkin = 0.02f;
        private const float WallMaxUpDot = 0.25f;
        private const float MinApproachDot = 0.2f;
        private const float MinIntentDirectionSqrFloor = 0.0001f;
        private const int ProbeHitBufferSize = 16;

        private static readonly RaycastHit[] ProbeHitBuffer = new RaycastHit[ProbeHitBufferSize];

        public static bool TryResolve(
            StepAssistSettings settings,
            Transform ownerTransform,
            Rigidbody bodyRigidbody,
            CapsuleCollider bodyCollider,
            LayerMask groundMask,
            float maxGroundAngle,
            float groundProbeExtraDistance,
            bool isGrounded,
            float lastGroundedTime,
            float coyoteTime,
            float verticalVelocity,
            bool isMotionLocked,
            bool isDead,
            Vector3 desiredDirection,
            Vector3 bodyVelocity,
            float dt,
            Collider[] overlapBuffer,
            out PositionCorrection correction,
            out GroundHitInfo stepGround)
        {
            correction = PositionCorrection.None;
            stepGround = default;

            if (settings == null || !settings.Enabled || ownerTransform == null || bodyRigidbody == null || bodyCollider == null)
                return false;

            float upperClearanceSkin = Mathf.Max(0.0f, settings.UpperClearanceSkin);
            float maxSnapDistancePerTick = Mathf.Max(0.0f, settings.SnapSpeed) * Mathf.Max(0.0001f, dt);

            if (isMotionLocked || isDead || verticalVelocity > 0.1f)
                return false;

            bool canStep = isGrounded || Time.time - lastGroundedTime <= coyoteTime;
            if (!canStep)
                return false;

            Vector3 resolveDirection = desiredDirection;
            Vector3 horizontalVelocity = bodyVelocity;
            horizontalVelocity.y = 0.0f;

            float minIntentDirectionSqr = Mathf.Max(MinIntentDirectionSqrFloor, settings.MinIntentMagnitude * settings.MinIntentMagnitude);
            bool hasIntentDirection = resolveDirection.sqrMagnitude >= minIntentDirectionSqr;
            if (hasIntentDirection)
            {
                resolveDirection = resolveDirection.normalized;
            }
            else
            {
                if (horizontalVelocity.sqrMagnitude <= settings.MinIntentMagnitude * settings.MinIntentMagnitude)
                    return false;

                resolveDirection = horizontalVelocity.normalized;
            }

            float horizontalSpeed = horizontalVelocity.magnitude;
            float desiredForwardDistance = horizontalSpeed * Mathf.Max(0.0001f, dt) + SurfaceSkin;
            if (hasIntentDirection)
            {
                float intentProbeFloor = Mathf.Max(SurfaceSkin, settings.ForwardProbeDistance * 0.5f);
                desiredForwardDistance = Mathf.Max(desiredForwardDistance, intentProbeFloor);
            }

            float forwardDistance = Mathf.Clamp(desiredForwardDistance, SurfaceSkin, settings.ForwardProbeDistance);
            if (forwardDistance <= 0.0f)
                return false;

            MovementBodyGeometryUtility.GetCapsuleGeometry(ownerTransform, bodyCollider, bodyRigidbody.position, out Vector3 capsuleBottom, out _, out float capsuleRadius);
            Vector3 capsuleCenter = bodyRigidbody.position + ownerTransform.rotation * bodyCollider.center;
            float castRadius = Mathf.Max(0.01f, capsuleRadius - SurfaceSkin);
            float feetY = capsuleBottom.y - capsuleRadius;
            Vector3 lowerOrigin = new Vector3(capsuleCenter.x, feetY + castRadius + settings.LowerProbeHeight, capsuleCenter.z);
            Vector3 upperOrigin = lowerOrigin + Vector3.up * (settings.MaxStepHeight + upperClearanceSkin);

            if (!TrySphereCastExcludingOwner(lowerOrigin, castRadius, resolveDirection, forwardDistance, ownerTransform, out RaycastHit lowerHit))
                return false;

            float hitUpDot = Mathf.Abs(Vector3.Dot(lowerHit.normal, Vector3.up));
            if (hitUpDot > WallMaxUpDot)
                return false;

            Vector3 wallNormal = Vector3.ProjectOnPlane(lowerHit.normal, Vector3.up);
            if (wallNormal.sqrMagnitude <= 0.0001f)
                return false;

            wallNormal.Normalize();
            if (Vector3.Dot(resolveDirection, -wallNormal) < MinApproachDot)
                return false;

            if (TrySphereCastExcludingOwner(upperOrigin, castRadius, resolveDirection, forwardDistance, ownerTransform, out _))
            {
                return false;
            }

            float candidateForwardOffset = Mathf.Max(lowerHit.distance + castRadius + SurfaceSkin, forwardDistance);
            Vector3 candidatePosition = bodyRigidbody.position + Vector3.up * (settings.MaxStepHeight + upperClearanceSkin) + resolveDirection * candidateForwardOffset;
            if (!TryFindStepGround(ownerTransform, bodyRigidbody, bodyCollider, groundMask, maxGroundAngle, groundProbeExtraDistance, settings, candidatePosition, out Vector3 snappedPosition, out stepGround) &&
                !TryResolveUsingObstacleTop(ownerTransform, bodyRigidbody, bodyCollider, settings, lowerHit, candidatePosition, out snappedPosition, out stepGround))
            {
                return false;
            }

            MovementBodyGeometryUtility.GetCapsuleGeometry(ownerTransform, bodyCollider, snappedPosition, out Vector3 candidateBottom, out _, out float candidateRadius);
            float candidateFeetY = candidateBottom.y - candidateRadius;
            if (!CanOccupyCapsule(ownerTransform, bodyCollider, snappedPosition, candidateFeetY, overlapBuffer))
                return false;

            if (snappedPosition.y <= bodyRigidbody.position.y + 0.0001f)
                return false;

            if (snappedPosition.y - bodyRigidbody.position.y > settings.MaxStepHeight + SurfaceSkin)
                return false;

            Vector3 correctionDelta = snappedPosition - bodyRigidbody.position;
            bool limitedBySnapSpeed = false;
            if (maxSnapDistancePerTick > 0.0001f && correctionDelta.magnitude > maxSnapDistancePerTick)
            {
                correctionDelta = correctionDelta.normalized * maxSnapDistancePerTick;
                limitedBySnapSpeed = true;
            }

            correction = new PositionCorrection(correctionDelta);
            if (!correction.HasCorrection)
                return false;

            if (limitedBySnapSpeed)
            {
                // 目的位置へ到達する前の部分補正は step 完了扱いにしない。
                stepGround = default;
                return false;
            }

            return true;
        }

        private static bool CanOccupyCapsule(Transform ownerTransform, CapsuleCollider bodyCollider, Vector3 bodyPosition, float candidateFeetY, Collider[] overlapBuffer)
        {
            if (overlapBuffer == null || overlapBuffer.Length == 0)
                return false;

            MovementBodyGeometryUtility.GetCapsuleGeometry(ownerTransform, bodyCollider, bodyPosition, out Vector3 capsuleBottom, out Vector3 capsuleTop, out float capsuleRadius);
            Vector3 capsuleCenter = bodyPosition + ownerTransform.rotation * bodyCollider.center;
            int hitCount = Physics.OverlapCapsuleNonAlloc(capsuleBottom, capsuleTop, Mathf.Max(0.01f, capsuleRadius - SurfaceSkin), overlapBuffer, ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = overlapBuffer[i];
                if (hit == null || hit.transform.IsChildOf(ownerTransform))
                    continue;

                Vector3 closestPoint = CanUsePhysicsClosestPoint(hit)
                    ? hit.ClosestPoint(capsuleCenter)
                    : hit.ClosestPointOnBounds(capsuleCenter);
                if (closestPoint.y <= candidateFeetY + SurfaceSkin)
                    continue;

                return false;
            }

            return true;
        }

        private static bool CanUsePhysicsClosestPoint(Collider collider)
        {
            if (collider == null)
                return false;

            if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider)
                return true;

            if (collider is MeshCollider meshCollider)
                return meshCollider.convex;

            return false;
        }

        private static bool TryFindStepGround(
            Transform ownerTransform,
            Rigidbody bodyRigidbody,
            CapsuleCollider bodyCollider,
            LayerMask groundMask,
            float maxGroundAngle,
            float groundProbeExtraDistance,
            StepAssistSettings settings,
            Vector3 candidatePosition,
            out Vector3 snappedPosition,
            out GroundHitInfo stepGround)
        {
            snappedPosition = default;
            stepGround = default;

            MovementBodyGeometryUtility.GetCapsuleGeometry(ownerTransform, bodyCollider, candidatePosition, out Vector3 capsuleBottom, out _, out float capsuleRadius);

            float probeStartOffset = settings.MaxStepHeight + groundProbeExtraDistance + SurfaceSkin;
            Vector3 probeOrigin = capsuleBottom + Vector3.up * probeStartOffset;
            float probeDistance = probeStartOffset + settings.StepDownProbeDistance;

            if (!Physics.SphereCast(probeOrigin, Mathf.Max(0.01f, capsuleRadius - SurfaceSkin), Vector3.down, out RaycastHit hit, probeDistance, groundMask, QueryTriggerInteraction.Ignore))
                return false;

            if (hit.collider == null || hit.collider.transform.IsChildOf(ownerTransform))
                return false;

            float angle = Vector3.Angle(hit.normal, Vector3.up);
            if (angle > maxGroundAngle)
                return false;

            Vector3 targetCapsuleBottom = probeOrigin + Vector3.down * hit.distance;
            Vector3 bodyOffset = candidatePosition - capsuleBottom;
            snappedPosition = targetCapsuleBottom + bodyOffset;

            if (snappedPosition.y - bodyRigidbody.position.y > settings.MaxStepHeight + SurfaceSkin)
                return false;

            stepGround = new GroundHitInfo(true, hit.collider, hit.collider.transform, hit.point, hit.normal, hit.distance, angle, GroundSurfaceKind.Walkable, true);
            return true;
        }

        private static bool TryResolveUsingObstacleTop(
            Transform ownerTransform,
            Rigidbody bodyRigidbody,
            CapsuleCollider bodyCollider,
            StepAssistSettings settings,
            RaycastHit obstacleHit,
            Vector3 candidatePosition,
            out Vector3 snappedPosition,
            out GroundHitInfo stepGround)
        {
            snappedPosition = default;
            stepGround = default;

            Collider obstacleCollider = obstacleHit.collider;
            if (obstacleCollider == null)
                return false;

            Bounds bounds = obstacleCollider.bounds;
            float obstacleTopY = bounds.max.y;

            MovementBodyGeometryUtility.GetCapsuleGeometry(ownerTransform, bodyCollider, bodyRigidbody.position, out Vector3 currentBottom, out _, out float currentRadius);
            float currentFeetY = currentBottom.y - currentRadius;
            float feetOffsetFromBody = currentFeetY - bodyRigidbody.position.y;
            float targetBodyY = obstacleTopY - feetOffsetFromBody;

            if (targetBodyY <= bodyRigidbody.position.y + 0.0001f)
                return false;

            if (targetBodyY - bodyRigidbody.position.y > settings.MaxStepHeight + SurfaceSkin)
                return false;

            snappedPosition = new Vector3(candidatePosition.x, targetBodyY, candidatePosition.z);
            Vector3 contactPoint = new Vector3(obstacleHit.point.x, obstacleTopY, obstacleHit.point.z);
            stepGround = new GroundHitInfo(true, obstacleCollider, obstacleCollider.transform, contactPoint, Vector3.up, 0.0f, 0.0f, GroundSurfaceKind.Walkable, true);
            return true;
        }

        private static bool TrySphereCastExcludingOwner(
            Vector3 origin,
            float radius,
            Vector3 direction,
            float distance,
            Transform ownerTransform,
            out RaycastHit selectedHit)
        {
            selectedHit = default;

            if (direction.sqrMagnitude <= 0.0001f || distance <= 0.0f)
                return false;

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                radius,
                direction,
                ProbeHitBuffer,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            float closestDistance = float.PositiveInfinity;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = ProbeHitBuffer[i];
                Collider hitCollider = hit.collider;
                if (hitCollider == null)
                    continue;

                if (ownerTransform != null && hitCollider.transform.IsChildOf(ownerTransform))
                    continue;

                // 投げた箱/爆弾などの「非kinematicな剛体(loose dynamic body)」は段差として扱わない。
                // Physics.IgnoreCollision はソルバにのみ効き、この手動 SphereCast には作用しない。
                // 除外しないと、下を向いて投げたアイテムを「乗り越える段差」と誤認して Player を上へ
                // スナップ(押し上げ)してしまう。静的段差(RB無し)や moving platform(kinematic)は段差のまま残す。
                if (IsLooseDynamicBodyCollider(hitCollider))
                    continue;

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    selectedHit = hit;
                    found = true;
                }
            }

            return found;
        }

        // 段差ではなく「押せる loose な動的オブジェクト」かどうかを判定する。
        // attachedRigidbody が存在し非kinematic（自由落下/投擲中の物体）なら true。
        // 静的ジオメトリ(RB無し)や moving platform(kinematic RB)は false のまま段差として扱う。
        private static bool IsLooseDynamicBodyCollider(Collider collider)
        {
            Rigidbody attached = collider.attachedRigidbody;
            return attached != null && !attached.isKinematic;
        }
    }
}

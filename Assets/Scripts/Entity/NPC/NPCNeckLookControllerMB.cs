using BC.Base;
using BC.Manager;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Character
{
    [DisallowMultipleComponent]
    public sealed class NPCNeckLookControllerMB : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private EntityFacingControllerMB facingController;
        [SerializeField] private NPCObjectMB npcObject;
        [SerializeField] private Transform neckBone;
        [SerializeField] private Transform headBone;
        [SerializeField] private Transform lookOrigin;

        [Title("Runtime Gates")]
        [SerializeField] private bool suspendWhileNpcInteractionActive;

        [Title("Tracking")]
        [SerializeField, Min(0.1f)] private float maxTrackingDistance = 4.0f;
        [SerializeField, Range(0.0f, 180.0f)] private float maxTrackingAngle = 75.0f;
        [SerializeField] private Vector3 targetWorldOffset = new(0.0f, 1.25f, 0.0f);
        [SerializeField, Min(0.01f)] private float turnSharpness = 10.0f;

        [Title("Rotation Limits")]
        [SerializeField, Range(0.0f, 90.0f)] private float maxYawAngle = 40.0f;
        [SerializeField, Range(0.0f, 80.0f)] private float upPitchAngle = 25.0f;
        [SerializeField, Range(0.0f, 80.0f)] private float downPitchAngle = 15.0f;

        [Title("Bone Weights")]
        [SerializeField, Range(0.0f, 1.0f)] private float neckYawWeight = 0.55f;
        [SerializeField, Range(0.0f, 1.0f)] private float neckPitchWeight = 0.6f;
        [SerializeField, Range(0.0f, 1.0f)] private float headYawWeight = 0.45f;
        [SerializeField, Range(0.0f, 1.0f)] private float headPitchWeight = 0.4f;

        [Title("Bone Axes")]
        [SerializeField] private Vector3 localYawAxis = Vector3.up;
        [SerializeField] private Vector3 localPitchAxis = Vector3.right;

        [Title("Debug")]
        [SerializeField] private bool drawDebugGizmos = true;

        private Quaternion neckInitialLocalRotation;
        private Quaternion headInitialLocalRotation;
        private float currentYaw;
        private float currentPitch;
        private PlayerMB cachedPlayer;
        private bool initialPoseCaptured;
        private bool debugHasTarget;
        private Vector3 debugTargetPosition;

        private void Awake()
        {
            ResolveReferences();
            initialPoseCaptured = false;
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();

            maxTrackingDistance = Mathf.Max(0.1f, maxTrackingDistance);
            turnSharpness = Mathf.Max(0.01f, turnSharpness);
            maxYawAngle = Mathf.Clamp(maxYawAngle, 0.0f, 90.0f);
            upPitchAngle = Mathf.Clamp(upPitchAngle, 0.0f, 80.0f);
            downPitchAngle = Mathf.Clamp(downPitchAngle, 0.0f, 80.0f);

            if (localYawAxis.sqrMagnitude <= 0.0001f)
                localYawAxis = Vector3.up;

            if (localPitchAxis.sqrMagnitude <= 0.0001f)
                localPitchAxis = Vector3.right;

            if (headBone == neckBone)
                headBone = null;
        }

        private void LateUpdate()
        {
            if (!initialPoseCaptured)
            {
                CaptureInitialPose();
            }

            if (!initialPoseCaptured || Time.deltaTime <= 0.0f)
                return;

            UpdateTracking(Time.deltaTime);
            ApplyCurrentPose();
        }

        private void OnDisable()
        {
            currentYaw = 0.0f;
            currentPitch = 0.0f;
            RestoreInitialPose();
        }

        private void ResolveReferences()
        {
            if (facingController == null)
            {
                facingController = GetComponentInParent<EntityFacingControllerMB>();
            }

            if (npcObject == null)
            {
                npcObject = GetComponentInParent<NPCObjectMB>();
            }

            Animator animator = GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = GetComponentInParent<Animator>();
            }

            if (animator != null && animator.isHuman)
            {
                if (neckBone == null)
                {
                    neckBone = animator.GetBoneTransform(HumanBodyBones.Neck);
                }

                if (headBone == null)
                {
                    headBone = animator.GetBoneTransform(HumanBodyBones.Head);
                }
            }

        }

        private void CaptureInitialPose()
        {
            if (neckBone == null && headBone == null)
            {
                initialPoseCaptured = false;
                return;
            }

            // Animator が最初の pose を適用した後の local 回転を neutral として保持します。
            // これで idle pose を基準に首だけを加算しやすくなります。
            if (neckBone != null)
            {
                neckInitialLocalRotation = neckBone.localRotation;
            }

            if (headBone != null)
            {
                headInitialLocalRotation = headBone.localRotation;
            }

            initialPoseCaptured = true;
        }

        private void UpdateTracking(float deltaTime)
        {
            if (suspendWhileNpcInteractionActive && npcObject != null && npcObject.IsInteractionInProgress)
            {
                float interactionBlend = 1.0f - Mathf.Exp(-turnSharpness * deltaTime);
                currentYaw = Mathf.Lerp(currentYaw, 0.0f, interactionBlend);
                currentPitch = Mathf.Lerp(currentPitch, 0.0f, interactionBlend);
                return;
            }

            bool hasTarget = TryResolveTargetAngles(out float targetYaw, out float targetPitch);
            float blend = 1.0f - Mathf.Exp(-turnSharpness * deltaTime);

            currentYaw = Mathf.Lerp(currentYaw, hasTarget ? targetYaw : 0.0f, blend);
            currentPitch = Mathf.Lerp(currentPitch, hasTarget ? targetPitch : 0.0f, blend);
        }

        private void ApplyCurrentPose()
        {
            ApplyBoneRotation(neckBone, neckInitialLocalRotation, neckYawWeight, neckPitchWeight);

            if (headBone != null && headBone != neckBone)
            {
                ApplyBoneRotation(headBone, headInitialLocalRotation, headYawWeight, headPitchWeight);
            }
        }

        private void ApplyBoneRotation(Transform bone, Quaternion initialLocalRotation, float yawWeight, float pitchWeight)
        {
            if (bone == null)
                return;

            Vector3 yawAxis = ResolveNormalizedAxis(localYawAxis, Vector3.up);
            Vector3 pitchAxis = ResolveNormalizedAxis(localPitchAxis, Vector3.right);

            Quaternion yawOffset = Quaternion.AngleAxis(currentYaw * Mathf.Clamp01(yawWeight), yawAxis);
            // Unity の +X 回転は forward を下へ倒すので、上を見る pitch は符号を反転させます。
            Quaternion pitchOffset = Quaternion.AngleAxis(-currentPitch * Mathf.Clamp01(pitchWeight), pitchAxis);
            bone.localRotation = initialLocalRotation * yawOffset * pitchOffset;
        }

        private void RestoreInitialPose()
        {
            if (!initialPoseCaptured)
                return;

            if (neckBone != null)
            {
                neckBone.localRotation = neckInitialLocalRotation;
            }

            if (headBone != null && headBone != neckBone)
            {
                headBone.localRotation = headInitialLocalRotation;
            }
        }

        private bool TryResolveTargetAngles(out float targetYaw, out float targetPitch)
        {
            targetYaw = 0.0f;
            targetPitch = 0.0f;
            debugHasTarget = false;
            debugTargetPosition = Vector3.zero;

            Transform origin = ResolveLookOrigin();
            if (origin == null)
                return false;

            if (!TryResolveTargetPosition(out Vector3 targetPosition))
                return false;

            if (!TryBuildFacingBasis(out Vector3 front, out Vector3 right, out Vector3 up))
                return false;

            Vector3 toTarget = targetPosition - origin.position;
            float sqrDistance = toTarget.sqrMagnitude;
            if (sqrDistance <= 0.0001f || sqrDistance > maxTrackingDistance * maxTrackingDistance)
                return false;

            // 「前方にいるか」の判定は上下差で壊れないよう、正面 cone は facing の up 軸に対する平面上で評価します。
            Vector3 planarToTarget = Vector3.ProjectOnPlane(toTarget, up);
            Vector3 planarFront = Vector3.ProjectOnPlane(front, up);
            if (planarToTarget.sqrMagnitude <= 0.0001f || planarFront.sqrMagnitude <= 0.0001f)
                return false;

            float minDot = Mathf.Cos(maxTrackingAngle * Mathf.Deg2Rad);
            if (Vector3.Dot(planarToTarget.normalized, planarFront.normalized) < minDot)
                return false;

            Vector3 direction = toTarget.normalized;
            float localX = Vector3.Dot(direction, right);
            float localY = Vector3.Dot(direction, up);
            float localZ = Vector3.Dot(direction, front);
            float horizontalMagnitude = Mathf.Max(0.0001f, Mathf.Sqrt((localX * localX) + (localZ * localZ)));

            targetYaw = Mathf.Atan2(localX, localZ) * Mathf.Rad2Deg;
            targetPitch = Mathf.Atan2(localY, horizontalMagnitude) * Mathf.Rad2Deg;

            targetYaw = Mathf.Clamp(targetYaw, -maxYawAngle, maxYawAngle);
            targetPitch = Mathf.Clamp(targetPitch, -downPitchAngle, upPitchAngle);

            debugHasTarget = true;
            debugTargetPosition = targetPosition;
            return true;
        }

        private bool TryResolveTargetPosition(out Vector3 targetPosition)
        {
            PlayerMB player = ResolvePlayer();
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                targetPosition = Vector3.zero;
                return false;
            }

            targetPosition = player.transform.position + targetWorldOffset;
            return true;
        }

        private PlayerMB ResolvePlayer()
        {
            if (cachedPlayer != null && cachedPlayer.gameObject.activeInHierarchy)
                return cachedPlayer;

            GameLogicManagerMB gameLogicManager = GameLogicManagerMB.Instance;
            if (gameLogicManager != null && gameLogicManager.PlayerInstance != null)
            {
                cachedPlayer = gameLogicManager.PlayerInstance;
                return cachedPlayer;
            }

            cachedPlayer = UnityEngine.Object.FindAnyObjectByType<PlayerMB>();
            return cachedPlayer;
        }

        private Transform ResolveLookOrigin()
        {
            if (lookOrigin != null)
                return lookOrigin;

            if (headBone != null)
                return headBone;

            if (neckBone != null)
                return neckBone;

            if (facingController != null)
                return facingController.FacingRoot;

            return transform;
        }

        private bool TryBuildFacingBasis(out Vector3 front, out Vector3 right, out Vector3 up)
        {
            Transform basisTransform = facingController != null ? facingController.FacingRoot : transform;
            up = basisTransform != null ? basisTransform.up : Vector3.up;
            if (up.sqrMagnitude <= 0.0001f)
                up = Vector3.up;

            up.Normalize();

            if (facingController != null && facingController.TryGetWorldFrontDirection(out Vector3 resolvedFront))
            {
                front = resolvedFront;
            }
            else if (basisTransform != null)
            {
                front = basisTransform.forward;
            }
            else
            {
                front = Vector3.forward;
            }

            front = Vector3.ProjectOnPlane(front, up);
            if (front.sqrMagnitude <= 0.0001f)
            {
                right = Vector3.right;
                front = Vector3.forward;
                return false;
            }

            front.Normalize();
            right = Vector3.Cross(up, front);
            if (right.sqrMagnitude <= 0.0001f)
                return false;

            right.Normalize();
            up = Vector3.Cross(front, right).normalized;
            return true;
        }

        private static Vector3 ResolveNormalizedAxis(Vector3 axis, Vector3 fallback)
        {
            if (axis.sqrMagnitude <= 0.0001f)
                return fallback;

            return axis.normalized;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
                return;

            Transform origin = ResolveLookOrigin();
            if (origin == null)
                return;

            if (TryBuildFacingBasis(out Vector3 front, out _, out _))
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(origin.position, origin.position + front * Mathf.Max(0.5f, maxTrackingDistance));
            }

            if (debugHasTarget)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(origin.position, debugTargetPosition);
                Gizmos.DrawSphere(debugTargetPosition, 0.06f);
            }
        }
#endif
    }
}
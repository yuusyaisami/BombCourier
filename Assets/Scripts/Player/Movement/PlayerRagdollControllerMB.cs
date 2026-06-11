using BC.Base;
using UnityEngine;

namespace BC.Manager
{
    public interface IEntityRagdollController
    {
        bool IsRagdollActive { get; }
        void EnterRagdoll(Vector3 impulse);
        void ExitRagdoll();
    }

    public interface IPlayerRagdollController : IEntityRagdollController
    {
    }

    [DefaultExecutionOrder(80)]
    public sealed class PlayerRagdollControllerMB : MonoBehaviour, IPlayerRagdollController
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody movementRigidbody;
        [SerializeField] private Collider movementCollider;
        [SerializeField] private Behaviour movementControllerBehaviour;
        [SerializeField] private PlayerMoveController playerMoveController;

        [Header("Ragdoll Parts Only")]
        [SerializeField] private Rigidbody[] ragdollRigidbodies;
        [SerializeField] private Collider[] ragdollColliders;
        [SerializeField] private Rigidbody impulseBody;
        [SerializeField] private Rigidbody recoveryAnchorBody;
        [SerializeField] private bool snapRootToRecoveryAnchor = true;
        [SerializeField] private bool alignRootYawToAnchor = true;

        [Header("Safety")]
        [SerializeField] private float maxImpulse = 12f;

        private bool isRagdoll;
        private Vector3 recoveryAnchorLocalPosition;

        public bool IsRagdollActive => isRagdoll;

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            movementRigidbody = GetComponent<Rigidbody>();
            movementCollider = GetComponent<Collider>();
            playerMoveController = GetComponent<PlayerMoveController>();
            movementControllerBehaviour = playerMoveController != null ? playerMoveController.MoveMotor : null;
            recoveryAnchorBody = impulseBody;

            // 注意：
            // ここで自動収集したものをそのまま信用しない。
            // Inspector で ragdoll 用の Rigidbody / Collider だけに整理する。
            CollectRagdollParts();
        }
        //自動取得ボタン
        [ContextMenu("Collect Ragdoll Parts")]
        private void CollectRagdollParts()
        {
            CollectRagdoll();
        }
        private void CollectRagdoll()
        {
            ragdollRigidbodies = GetComponentsInChildren<Rigidbody>(true);
            ragdollColliders = GetComponentsInChildren<Collider>(true);
        }
        // ラグドールパーツ同士の衝突を無効化しておく。これも Inspector で整理することを推奨。
        private void IgnoreSelfCollisions()
        {
            if (ragdollColliders == null)
                return;

            for (int i = 0; i < ragdollColliders.Length; i++)
            {
                Collider a = ragdollColliders[i];

                if (a == null)
                    continue;

                for (int j = i + 1; j < ragdollColliders.Length; j++)
                {
                    Collider b = ragdollColliders[j];

                    if (b == null)
                        continue;

                    Physics.IgnoreCollision(a, b, true);
                }
            }
        }

        private void Awake()
        {
            ResolveMovementBody();
            SetRagdollEnabled(false);
            IgnoreSelfCollisions();
            CacheRecoveryAnchorLocalPosition();
        }

        public void EnterRagdoll(Vector3 impulse)
        {
            if (isRagdoll)
                return;

            isRagdoll = true;

            if (playerMoveController != null)
                playerMoveController.enabled = false;

            if (movementControllerBehaviour != null)
                movementControllerBehaviour.enabled = false;

            ResolveMovementBody();

            if (movementCollider != null)
                movementCollider.enabled = false;

            if (movementRigidbody != null)
            {
                movementRigidbody.linearVelocity = Vector3.zero;
                movementRigidbody.angularVelocity = Vector3.zero;
                movementRigidbody.isKinematic = true;
                movementRigidbody.detectCollisions = false;
            }

            if (animator != null)
                animator.enabled = false;

            SetRagdollEnabled(true);

            Vector3 safeImpulse = Vector3.ClampMagnitude(impulse, maxImpulse);

            Rigidbody target = impulseBody != null
                ? impulseBody
                : FindFirstValidRagdollBody();

            if (target != null)
                target.AddForce(safeImpulse, ForceMode.Impulse);
        }

        public void ExitRagdoll()
        {
            if (!isRagdoll)
                return;

            isRagdoll = false;

            SetRagdollEnabled(false);
            SnapRootToRecoveryAnchor();

            if (animator != null)
                animator.enabled = true;

            ResolveMovementBody();

            if (movementCollider != null)
                movementCollider.enabled = true;

            if (movementRigidbody != null)
            {
                movementRigidbody.isKinematic = false;
                movementRigidbody.detectCollisions = true;
                movementRigidbody.useGravity = false;
                movementRigidbody.linearVelocity = Vector3.zero;
                movementRigidbody.angularVelocity = Vector3.zero;
            }

            if (movementControllerBehaviour != null)
                movementControllerBehaviour.enabled = true;

            if (playerMoveController != null)
                playerMoveController.enabled = true;
        }

        private void ResolveMovementBody()
        {
            if (playerMoveController == null)
                playerMoveController = GetComponent<PlayerMoveController>();

            if (movementControllerBehaviour == null && playerMoveController != null)
                movementControllerBehaviour = playerMoveController.MoveMotor;

            if (movementRigidbody == null)
                movementRigidbody = GetComponent<Rigidbody>();

            if (movementCollider == null)
                movementCollider = GetComponent<CapsuleCollider>();

            if (movementCollider == null)
                movementCollider = GetComponent<Collider>();

            if (recoveryAnchorBody == null)
                recoveryAnchorBody = impulseBody != null ? impulseBody : FindFirstValidRagdollBody();
        }

        private void CacheRecoveryAnchorLocalPosition()
        {
            ResolveMovementBody();

            if (recoveryAnchorBody == null)
                return;

            recoveryAnchorLocalPosition = transform.InverseTransformPoint(recoveryAnchorBody.position);
        }

        private void SnapRootToRecoveryAnchor()
        {
            if (!snapRootToRecoveryAnchor || recoveryAnchorBody == null)
                return;

            Vector3 desiredForward = transform.forward;

            if (alignRootYawToAnchor)
            {
                Vector3 anchorForward = Vector3.ProjectOnPlane(recoveryAnchorBody.transform.forward, Vector3.up);

                if (anchorForward.sqrMagnitude > 0.0001f)
                    desiredForward = anchorForward.normalized;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(desiredForward, Vector3.up);
            Vector3 desiredPosition = recoveryAnchorBody.position - desiredRotation * recoveryAnchorLocalPosition;
            transform.SetPositionAndRotation(desiredPosition, desiredRotation);
        }

        private void SetRagdollEnabled(bool enabled)
        {
            if (ragdollRigidbodies != null)
            {
                for (int i = 0; i < ragdollRigidbodies.Length; i++)
                {
                    Rigidbody rb = ragdollRigidbodies[i];

                    if (rb == null)
                        continue;

                    // 先に速度を止めてから kinematic に切り替え、Unity の警告を出さない。
                    if (!enabled)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    rb.isKinematic = !enabled;
                    rb.detectCollisions = enabled;
                }
            }

            if (ragdollColliders != null)
            {
                for (int i = 0; i < ragdollColliders.Length; i++)
                {
                    Collider col = ragdollColliders[i];

                    if (col == null)
                        continue;

                    col.enabled = enabled;
                }
            }
        }

        private Rigidbody FindFirstValidRagdollBody()
        {
            if (ragdollRigidbodies == null)
                return null;

            for (int i = 0; i < ragdollRigidbodies.Length; i++)
            {
                Rigidbody rb = ragdollRigidbodies[i];

                if (rb != null)
                    return rb;
            }

            return null;
        }
    }
}

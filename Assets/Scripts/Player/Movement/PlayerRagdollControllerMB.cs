using BC.Base;
using UnityEngine;

namespace BC.Manager
{
    public interface IPlayerRagdollController
    {
        void EnterRagdoll(Vector3 impulse);
        void ExitRagdoll();
    }

    public sealed class PlayerRagdollControllerMB : MonoBehaviour, IPlayerRagdollController
    {
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerMoveController moveController;

        [Header("Ragdoll Parts Only")]
        [SerializeField] private Rigidbody[] ragdollRigidbodies;
        [SerializeField] private Collider[] ragdollColliders;
        [SerializeField] private Rigidbody impulseBody;

        [Header("Safety")]
        [SerializeField] private float maxImpulse = 12f;

        private bool isRagdoll;

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            characterController = GetComponent<CharacterController>();
            moveController = GetComponent<PlayerMoveController>();

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
            SetRagdollEnabled(false);
            IgnoreSelfCollisions();
        }

        public void EnterRagdoll(Vector3 impulse)
        {
            if (isRagdoll)
                return;

            isRagdoll = true;

            if (moveController != null)
                moveController.enabled = false;

            if (characterController != null)
                characterController.enabled = false;

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

            if (animator != null)
                animator.enabled = true;

            if (characterController != null)
                characterController.enabled = true;

            if (moveController != null)
                moveController.enabled = true;
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

                    rb.isKinematic = !enabled;
                    rb.detectCollisions = enabled;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
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
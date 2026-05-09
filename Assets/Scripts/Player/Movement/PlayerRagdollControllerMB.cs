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
        [SerializeField] private Rigidbody[] ragdollRigidbodies;
        [SerializeField] private Collider[] ragdollColliders;
        [SerializeField] private Rigidbody impulseBody;

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            characterController = GetComponent<CharacterController>();
            moveController = GetComponent<PlayerMoveController>();
            ragdollRigidbodies = GetComponentsInChildren<Rigidbody>(true);
            ragdollColliders = GetComponentsInChildren<Collider>(true);
        }

        private void Awake()
        {
            ExitRagdoll();
        }

        public void EnterRagdoll(Vector3 impulse)
        {
            if (moveController != null)
                moveController.enabled = false;

            if (characterController != null)
                characterController.enabled = false;

            if (animator != null)
                animator.enabled = false;

            for (int i = 0; i < ragdollRigidbodies.Length; i++)
            {
                Rigidbody rb = ragdollRigidbodies[i];

                if (rb == null)
                    continue;

                rb.isKinematic = false;
                rb.detectCollisions = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            for (int i = 0; i < ragdollColliders.Length; i++)
            {
                Collider col = ragdollColliders[i];

                if (col == null)
                    continue;

                // ルートの CharacterController は除外したいので、
                // ragdoll 用 collider だけ inspector で配列指定するのが安全。
                col.enabled = true;
            }

            Rigidbody target = impulseBody != null
                ? impulseBody
                : ragdollRigidbodies.Length > 0
                    ? ragdollRigidbodies[0]
                    : null;

            if (target != null)
                target.AddForce(impulse, ForceMode.Impulse);
        }

        public void ExitRagdoll()
        {
            for (int i = 0; i < ragdollRigidbodies.Length; i++)
            {
                Rigidbody rb = ragdollRigidbodies[i];

                if (rb == null)
                    continue;

                rb.isKinematic = true;
                rb.detectCollisions = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            for (int i = 0; i < ragdollColliders.Length; i++)
            {
                Collider col = ragdollColliders[i];

                if (col == null)
                    continue;

                col.enabled = false;
            }

            if (animator != null)
                animator.enabled = true;

            if (characterController != null)
                characterController.enabled = true;

            if (moveController != null)
            {
                moveController.enabled = true;
                moveController.ReviveFromCheckpoint();
            }
        }
    }
}
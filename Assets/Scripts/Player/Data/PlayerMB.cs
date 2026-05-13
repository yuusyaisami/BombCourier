using BC.Manager;
using BC.Utility;
using UnityEngine;
namespace BC.Base
{

    public interface IEntityHandleItemAnimationSource
    {
        bool IsHandlingItem { get; }
    }

    public class PlayerMB : MonoBehaviour
    {

        [SerializeField] private ParticleSystem respawnEffectPrefab; // スポーン(またはリスポーン)したときの演出
        [SerializeField] private PlayerRagdollControllerMB ragdollController;
        [SerializeField] private PlayerMoveController moveController;

        public PlayerRagdollControllerMB RagdollController => ragdollController;
        public PlayerMoveController MoveController => moveController;
        private void Reset()
        {
            ragdollController = GetComponentInChildren<PlayerRagdollControllerMB>();
            moveController = GetComponent<PlayerMoveController>();
        }
        public void TeleportToSpawnPoint(Vector3 position = default, Quaternion rotation = default)
        {
            transform.position = position;
            transform.rotation = rotation;
        }

        public void PlayRespawnEffect()
        {
            if (respawnEffectPrefab != null)
            {
                Instantiate(respawnEffectPrefab, transform.position, Quaternion.identity).Play();
            }
        }
        public void ResetPlayer()
        {
            ragdollController.ExitRagdoll();
            moveController.ReviveFromCheckpoint();

            PlayRespawnEffect();
        }
    }
}
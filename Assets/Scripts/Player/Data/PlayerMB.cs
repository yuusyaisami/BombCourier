using BC.Camera;
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
        [SerializeField] private EntityMoveMotorMB moveController;
        [SerializeField] private PlayerMoveController playerMoveController;
        [SerializeField] private ThirdPersonCameraController cameraController;

        public PlayerRagdollControllerMB RagdollController => ragdollController;
        public EntityMoveMotorMB MoveController => moveController != null ? moveController : GetComponent<EntityMoveMotorMB>();
        public PlayerMoveController PlayerMoveController => playerMoveController != null ? playerMoveController : GetComponent<PlayerMoveController>();
        public ThirdPersonCameraController CameraController => cameraController != null ? cameraController : GetComponentInChildren<ThirdPersonCameraController>();
        private void Awake()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            // Inspector の設定漏れがあっても、同一 GameObject 上の構成から安全に復元する。
            if (ragdollController == null)
            {
                ragdollController = GetComponentInChildren<PlayerRagdollControllerMB>(true);
            }

            if (moveController == null)
            {
                moveController = GetComponent<EntityMoveMotorMB>();
            }

            if (playerMoveController == null)
            {
                playerMoveController = GetComponent<PlayerMoveController>();
            }

            if (moveController == null && playerMoveController != null)
            {
                moveController = playerMoveController.MoveMotor;
            }

            if (cameraController == null)
            {
                cameraController = GetComponentInChildren<ThirdPersonCameraController>(true);
            }
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
            ResolveReferences();

            if (ragdollController != null)
            {
                ragdollController.ExitRagdoll();
            }

            if (MoveController != null)
            {
                MoveController.ReviveFromCheckpoint();
            }


            PlayRespawnEffect();
        }

    }
}
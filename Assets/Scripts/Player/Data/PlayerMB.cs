using BC.Camera;
using BC.Gimmick;
using BC.Manager;
using BC.Utility;
using UnityEngine;
namespace BC.Base
{

    public interface IEntityHandleItemAnimationSource
    {
        bool IsHandlingItem { get; }
    }

    public class PlayerMB : MonoBehaviour, IGodHandCatchTarget
    {

        [SerializeField] private ParticleSystem respawnEffectPrefab; // スポーン(またはリスポーン)したときの演出
        [SerializeField] private PlayerRagdollControllerMB ragdollController;
        [SerializeField] private EntityMoveMotorMB moveController;
        [SerializeField] private PlayerMoveController playerMoveController;
        [SerializeField] private ThirdPersonCameraController cameraController;
        [SerializeField] private Rigidbody bodyRigidbody;

        private Transform godHandCatchTransform;
        private bool isCaughtByGodHand;
        private bool cachedMoveMotorEnabled;
        private bool cachedPlayerMoveControllerEnabled;
        private bool cachedRigidbodyIsKinematic;
        private bool cachedRigidbodyUseGravity;
        private bool cachedRigidbodyDetectCollisions;

        public PlayerRagdollControllerMB RagdollController => ragdollController;
        public EntityMoveMotorMB MoveController => moveController != null ? moveController : GetComponent<EntityMoveMotorMB>();
        public PlayerMoveController PlayerMoveController => playerMoveController != null ? playerMoveController : GetComponent<PlayerMoveController>();
        public ThirdPersonCameraController CameraController => cameraController != null ? cameraController : GetComponentInChildren<ThirdPersonCameraController>();
        public bool CanBeCaughtByGodHand => enabled && gameObject.activeInHierarchy;

        private void Awake()
        {
            ResolveReferences();
        }

        private void LateUpdate()
        {
            if (!isCaughtByGodHand || godHandCatchTransform == null)
                return;

            transform.SetPositionAndRotation(godHandCatchTransform.position, godHandCatchTransform.rotation);
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

            if (bodyRigidbody == null)
            {
                bodyRigidbody = GetComponent<Rigidbody>();
            }
        }

        public void OnCaughtByGodHand(Transform catchTransform)
        {
            if (!CanBeCaughtByGodHand || catchTransform == null)
                return;

            ResolveReferences();

            if (!isCaughtByGodHand)
            {
                CacheGodHandCatchState();
            }

            godHandCatchTransform = catchTransform;
            isCaughtByGodHand = true;

            if (playerMoveController != null)
            {
                playerMoveController.CancelAutoMove();
                playerMoveController.SetPlanarVelocity(Vector3.zero);
                playerMoveController.SetVerticalVelocity(0.0f);
                playerMoveController.ClearExternalVelocity();
                playerMoveController.enabled = false;
            }

            if (moveController != null)
            {
                moveController.CancelAutoMove();
                moveController.ClearMoveIntent();
                moveController.enabled = false;
            }

            if (bodyRigidbody != null)
            {
                bodyRigidbody.linearVelocity = Vector3.zero;
                bodyRigidbody.angularVelocity = Vector3.zero;
                bodyRigidbody.isKinematic = true;
                bodyRigidbody.useGravity = false;
                bodyRigidbody.detectCollisions = false;
            }

            transform.SetPositionAndRotation(catchTransform.position, catchTransform.rotation);
        }

        public void OnReleasedByGodHand()
        {
            if (!isCaughtByGodHand)
                return;

            if (bodyRigidbody != null)
            {
                bodyRigidbody.linearVelocity = Vector3.zero;
                bodyRigidbody.angularVelocity = Vector3.zero;
                bodyRigidbody.isKinematic = cachedRigidbodyIsKinematic;
                bodyRigidbody.useGravity = cachedRigidbodyUseGravity;
                bodyRigidbody.detectCollisions = cachedRigidbodyDetectCollisions;
            }

            if (moveController != null)
            {
                moveController.enabled = cachedMoveMotorEnabled;
            }

            if (playerMoveController != null)
            {
                playerMoveController.enabled = cachedPlayerMoveControllerEnabled;
            }

            godHandCatchTransform = null;
            isCaughtByGodHand = false;
        }

        private void CacheGodHandCatchState()
        {
            cachedMoveMotorEnabled = moveController != null && moveController.enabled;
            cachedPlayerMoveControllerEnabled = playerMoveController != null && playerMoveController.enabled;

            if (bodyRigidbody == null)
                return;

            cachedRigidbodyIsKinematic = bodyRigidbody.isKinematic;
            cachedRigidbodyUseGravity = bodyRigidbody.useGravity;
            cachedRigidbodyDetectCollisions = bodyRigidbody.detectCollisions;
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
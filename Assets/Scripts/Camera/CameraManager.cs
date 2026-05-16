using Unity.Cinemachine;
using BC.Base;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace BC.Camera
{
    [DisallowMultipleComponent]
    public sealed class CameraManager : MonoBehaviour
    {
        [Header("Scene Cameras")]
        [FormerlySerializedAs("introCamera")]
        [SerializeField] private CinemachineCamera pathCamera;
        [SerializeField] private CinemachineCamera thirdPersonCamera;

        [Header("Path Playback")]
        [SerializeField] private int pathPriority = 100;
        [SerializeField] private int thirdPersonPriority = 10;
        [SerializeField] private int inactivePriority = 0;

        private Transform currentThirdPersonTarget;
        private SceneKernel sceneKernel;

        public static CameraManager Instance { get; private set; }

        public CinemachineCamera PathCamera => pathCamera;
        [System.Obsolete("Use PathCamera instead.")]
        public CinemachineCamera IntroCamera => pathCamera;
        public CinemachineCamera ThirdPersonCamera => thirdPersonCamera;

        public UniTask PlayPathAsync(ICameraPathSequenceSource sequenceSource, EntityRef actor)
        {
            if (sequenceSource == null)
            {
                Debug.LogError($"{nameof(CameraManager)}: sequence source is null.", this);
                return UniTask.CompletedTask;
            }

            return PlayPathAsync(new CameraPathSequenceDefinition(sequenceSource.BuildSequence()), actor);
        }

        public UniTask PlayPathAsync(CameraPathSequenceDefinition sequence, EntityRef actor)
        {
            if (!TryResolveSceneKernel(out SceneKernel resolvedSceneKernel))
                return UniTask.CompletedTask;

            CameraPathPlayRequest request = new CameraPathPlayRequest(
                pathCamera,
                thirdPersonCamera,
                sequence,
                actor,
                pathPriority,
                thirdPersonPriority,
                inactivePriority);

            return resolvedSceneKernel.CameraPaths.PlayAsync(request);
        }

        public void CancelPath()
        {
            if (TryResolveSceneKernel(out SceneKernel resolvedSceneKernel))
                resolvedSceneKernel.CameraPaths.Cancel();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterThirdPersonTarget(Transform target)
        {
            if (thirdPersonCamera == null)
            {
                Debug.LogWarning($"{nameof(CameraManager)}: Third person camera is not assigned.", this);
                return;
            }

            currentThirdPersonTarget = target;
            thirdPersonCamera.Follow = target;
            thirdPersonCamera.LookAt = target;
        }

        public void UnregisterThirdPersonTarget(Transform target)
        {
            if (thirdPersonCamera == null || target == null || currentThirdPersonTarget != target)
                return;

            thirdPersonCamera.Follow = null;
            thirdPersonCamera.LookAt = null;
            currentThirdPersonTarget = null;
        }

        private bool TryResolveSceneKernel(out SceneKernel resolvedSceneKernel)
        {
            if (sceneKernel != null)
            {
                resolvedSceneKernel = sceneKernel;
                return true;
            }

            SceneKernelMB kernelMB = GetComponentInParent<SceneKernelMB>();

            if (kernelMB == null)
                kernelMB = Object.FindAnyObjectByType<SceneKernelMB>();

            sceneKernel = kernelMB != null ? kernelMB.Kernel : null;
            resolvedSceneKernel = sceneKernel;

            if (resolvedSceneKernel != null)
                return true;

            Debug.LogError($"{nameof(CameraManager)}: SceneKernelMB is not found.", this);
            return false;
        }
    }

}
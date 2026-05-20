using Unity.Cinemachine;
using BC.Base;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using System;

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

        [Header("Talk Camera")]
        [SerializeField, Min(0.0f)] private float talkOcclusionProbeRadius = 0.18f;
        [SerializeField] private LayerMask talkOcclusionMask = ~0;
        [SerializeField, Min(0.01f)] private float talkFacingSharpness = 20.0f;

        private Transform currentThirdPersonTarget;
        private SceneKernel sceneKernel;

        public static CameraManager Instance { get; private set; }

        public CinemachineCamera PathCamera => pathCamera;
        [System.Obsolete("Use PathCamera instead.")]
        public CinemachineCamera IntroCamera => pathCamera;
        public CinemachineCamera ThirdPersonCamera => thirdPersonCamera;
        public Transform CurrentThirdPersonTarget => currentThirdPersonTarget;
        public int PresentationPriority => pathPriority;
        public int ThirdPersonPriority => thirdPersonPriority;
        public int InactivePriority => inactivePriority;
        public float TalkOcclusionProbeRadius => talkOcclusionProbeRadius;
        public LayerMask TalkOcclusionMask => talkOcclusionMask;
        public float TalkFacingSharpness => talkFacingSharpness;

        public UniTask PlayPathAsync(ICameraPathSequenceSource sequenceSource, EntityRef actor, Func<UniTask> onCompletedBeforeCameraReset = null)
        {
            if (sequenceSource == null)
            {
                Debug.LogError($"{nameof(CameraManager)}: sequence source is null.", this);
                return UniTask.CompletedTask;
            }

            if (!TryResolveSceneKernel(out SceneKernel resolvedSceneKernel))
                return UniTask.CompletedTask;

            if (!CameraPathSequenceDefinition.TryCreate(sequenceSource.BuildSequence(), resolvedSceneKernel, actor, out CameraPathSequenceDefinition sequence))
                return UniTask.CompletedTask;

            return PlayPathAsync(sequence, actor, onCompletedBeforeCameraReset);
        }

        public UniTask PlayPathAsync(CameraPathSequenceDefinition sequence, EntityRef actor, Func<UniTask> onCompletedBeforeCameraReset = null)
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
                inactivePriority,
                onCompletedBeforeCameraReset);

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

        public bool TryGetThirdPersonRig(out CinemachineThirdPersonFollow thirdPersonFollow, out CinemachineRotateWithFollowTarget rotateWithFollowTarget)
        {
            thirdPersonFollow = null;
            rotateWithFollowTarget = null;

            if (thirdPersonCamera == null)
                return false;

            thirdPersonFollow = thirdPersonCamera.GetComponent<CinemachineThirdPersonFollow>();
            rotateWithFollowTarget = thirdPersonCamera.GetComponent<CinemachineRotateWithFollowTarget>();
            return thirdPersonFollow != null;
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
                kernelMB = UnityEngine.Object.FindAnyObjectByType<SceneKernelMB>();

            sceneKernel = kernelMB != null ? kernelMB.Kernel : null;
            resolvedSceneKernel = sceneKernel;

            if (resolvedSceneKernel != null)
                return true;

            Debug.LogError($"{nameof(CameraManager)}: SceneKernelMB is not found.", this);
            return false;
        }
    }

}
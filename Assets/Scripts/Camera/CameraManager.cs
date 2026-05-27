using Unity.Cinemachine;
using BC.Base;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using System;

namespace BC.Camera
{
    [DisallowMultipleComponent]
    // シーン上に置かれるカメラ実体の入口。
    // 実カメラ参照、path 再生の facade、三人称ターゲット登録を持つ MonoBehaviour 側のハブです。
    public sealed class CameraManager : MonoBehaviour
    {
        [Header("Scene Cameras")]
        // パス再生時に使う演出用カメラ。
        [FormerlySerializedAs("introCamera")]
        [SerializeField] private CinemachineCamera pathCamera;
        // 通常プレイ時の三人称カメラ。
        [SerializeField] private CinemachineCamera thirdPersonCamera;
        // 会話専用の三人称カメラ。通常 TPS とは分離して切り替えます。
        [SerializeField] private CinemachineCamera talkCamera;

        [Header("Path Playback")]
        // path camera を優先表示するときの基準 priority 群。
        [SerializeField] private int pathPriority = 100;
        [SerializeField] private int talkPriority = 30;
        [SerializeField] private int thirdPersonPriority = 10;
        [SerializeField] private int inactivePriority = 0;

        [Header("Focus Camera")]
        // 注視演出時に肩越しカメラの左右切り替えを決める遮蔽判定設定。
        [FormerlySerializedAs("talkOcclusionProbeRadius")]
        [SerializeField, Min(0.0f)] private float focusOcclusionProbeRadius = 0.18f;
        [FormerlySerializedAs("talkOcclusionMask")]
        [SerializeField] private LayerMask focusOcclusionMask = ~0;
        // observer が注視対象へ向くときの回転追従の鋭さ。
        [FormerlySerializedAs("talkFacingSharpness")]
        [SerializeField, Min(0.01f)] private float focusFacingSharpness = 20.0f;
        [SerializeField, Range(0.0f, 45.0f)] private float talkInitialYawBias = 14.0f;
        [SerializeField, Min(0.0f)] private float talkDistanceOffset = 1.35f;
        [SerializeField, Min(0.0f)] private float talkDistanceScale = 0.55f;
        [SerializeField, Min(0.0f)] private float talkMinCameraDistance = 2.2f;
        [SerializeField, Min(0.0f)] private float talkMaxCameraDistance = 4.75f;
        [SerializeField, Min(0.01f)] private float talkDistanceBlendSharpness = 10.0f;

        // 三人称 camera の現在の Follow/LookAt 先。
        private Transform currentThirdPersonTarget;
        private SceneKernel sceneKernel;

        public static CameraManager Instance { get; private set; }

        public CinemachineCamera PathCamera => pathCamera;
        [System.Obsolete("Use PathCamera instead.")]
        public CinemachineCamera IntroCamera => pathCamera;
        public CinemachineCamera TalkCamera => talkCamera;
        public CinemachineCamera ThirdPersonCamera => thirdPersonCamera;
        public Transform CurrentThirdPersonTarget => currentThirdPersonTarget;
        public int PresentationPriority => pathPriority;
        public int TalkPriority => talkPriority;
        public int ThirdPersonPriority => thirdPersonPriority;
        public int InactivePriority => inactivePriority;
        public float FocusOcclusionProbeRadius => focusOcclusionProbeRadius;
        public LayerMask FocusOcclusionMask => focusOcclusionMask;
        public float FocusFacingSharpness => focusFacingSharpness;
        public float TalkInitialYawBias => talkInitialYawBias;
        public float TalkDistanceOffset => talkDistanceOffset;
        public float TalkDistanceScale => talkDistanceScale;
        public float TalkMinCameraDistance => talkMinCameraDistance;
        public float TalkMaxCameraDistance => talkMaxCameraDistance;
        public float TalkDistanceBlendSharpness => talkDistanceBlendSharpness;

        // Editor preview や debug utility から path camera を直接同期したいときの入口。
        public bool SetPathCameraPosition(Vector3 position, Quaternion rotation)
        {
            if (pathCamera == null)
            {
                Debug.LogWarning($"{nameof(CameraManager)}: Path camera is not assigned.", this);
                return false;
            }

            pathCamera.transform.SetPositionAndRotation(position, rotation);
            return true;
        }

        // path sequence 再生前に先頭 point の pose を適用し、blend 開始位置のズレを防ぐ。
        public bool SetPathCameraPosition(ICameraPathSequenceSource sequenceSource, EntityRef actor)
        {
            if (sequenceSource == null)
            {
                Debug.LogError($"{nameof(CameraManager)}: sequence source is null.", this);
                return false;
            }

            if (!TryResolveSceneKernel(out SceneKernel resolvedSceneKernel))
                return false;

            if (!CameraPathSequenceDefinition.TryCreate(sequenceSource.BuildSequence(), resolvedSceneKernel, actor, out CameraPathSequenceDefinition sequence))
                return false;

            if (sequence.Count == 0)
            {
                Debug.LogWarning($"{nameof(CameraManager)}: camera path sequence is empty.", this);
                return false;
            }

            return SetPathCameraPosition(sequence);
        }

        // 実再生と同じ 1 点目の pose をそのまま適用して、開始フレームでの見た目差分をなくす。
        public bool SetPathCameraPosition(CameraPathSequenceDefinition sequence)
        {
            if (sequence == null || sequence.Count == 0)
            {
                Debug.LogError($"{nameof(CameraManager)}: camera path sequence is empty.", this);
                return false;
            }

            return ApplyPathCameraPoint(sequence.Points[0]);
        }

        public bool ApplyPathCameraPoint(in CameraPathResolvedPoint point)
        {
            if (pathCamera == null)
            {
                Debug.LogWarning($"{nameof(CameraManager)}: Path camera is not assigned.", this);
                return false;
            }

            CameraPathPlaybackUtility.ApplyPose(
                pathCamera.transform,
                pathCamera,
                CameraPathPlaybackUtility.BuildPose(point));
            return true;
        }

        public bool ApplyInterpolatedPathCameraPoint(in CameraPathResolvedPoint from, in CameraPathResolvedPoint to, float t)
        {
            if (pathCamera == null)
            {
                Debug.LogWarning($"{nameof(CameraManager)}: Path camera is not assigned.", this);
                return false;
            }

            CameraPathPlaybackUtility.ApplyPose(
                pathCamera.transform,
                pathCamera,
                CameraPathPlaybackUtility.BuildInterpolatedPose(
                    from,
                    to,
                    t,
                    CameraPathPlaybackUtility.GetFieldOfView(pathCamera)));
            return true;
        }

        // Authoring 定義から path sequence を構築して再生する簡易入口。
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

        // 解決済みの sequence を CameraPathPlayerService へそのまま引き渡す。
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

        // 進行中の path playback をキャンセルする facade。
        public void CancelPath()
        {
            if (TryResolveSceneKernel(out SceneKernel resolvedSceneKernel))
                resolvedSceneKernel.CameraPaths.Cancel();
        }

        private void Awake()
        {
            // シーン内 singleton として振る舞う。重複があれば後勝ちではなく破棄する。
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

        // プレイヤー側から camera target が登録されたら、third person camera の Follow/LookAt を同期する。
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

        // 登録した target 本人が外れるときだけ Follow/LookAt を解除する。
        public void UnregisterThirdPersonTarget(Transform target)
        {
            if (thirdPersonCamera == null || target == null || currentThirdPersonTarget != target)
                return;

            thirdPersonCamera.Follow = null;
            thirdPersonCamera.LookAt = null;
            currentThirdPersonTarget = null;
        }

        public void ClearThirdPersonTarget()
        {
            if (thirdPersonCamera == null)
                return;

            thirdPersonCamera.Follow = null;
            thirdPersonCamera.LookAt = null;
            currentThirdPersonTarget = null;
        }

        // SceneCameraService が third person rig を補正できるように、必要コンポーネントをまとめて取り出す。
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

        public bool TryGetTalkRig(out CinemachineThirdPersonFollow thirdPersonFollow, out CinemachineRotateWithFollowTarget rotateWithFollowTarget)
        {
            thirdPersonFollow = null;
            rotateWithFollowTarget = null;

            if (talkCamera == null)
                return false;

            thirdPersonFollow = talkCamera.GetComponent<CinemachineThirdPersonFollow>();
            rotateWithFollowTarget = talkCamera.GetComponent<CinemachineRotateWithFollowTarget>();
            return thirdPersonFollow != null;
        }

        // CameraManager は scene root の SceneKernelMB に依存するので、必要になった時点で 1 回だけ解決する。
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
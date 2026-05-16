using System;
using BC.Camera;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace BombCourier.CameraIntro
{
    /// <summary>
    /// 旧Intro用Runnerの移行口。
    /// 実際のカメラパス再生はCameraManager/SceneKernel側の汎用CameraPathPlayerServiceに委譲する。
    /// </summary>
    [Obsolete("Use BC.Camera.CameraManager.PlayPathAsync instead.")]
    public sealed class IntroCameraSequenceRunner : MonoBehaviour
    {
        [SerializeField] private IntroCameraPathAuthoring path;
        [SerializeField] private UnityEvent<bool> inputLockChanged;
        [SerializeField] private UnityEvent introCompleted;

        private bool skipRequested;

        public async UniTask Play(IntroCameraPathAuthoring path)
        {
            if (CameraManager.Instance == null)
            {
                Debug.LogError($"{nameof(IntroCameraSequenceRunner)}: CameraManager is not found.", this);
                return;
            }

            IntroCameraPathAuthoring targetPath = path != null ? path : this.path;

            if (targetPath == null)
            {
                Debug.LogError($"{nameof(IntroCameraSequenceRunner)}: path is null.", this);
                return;
            }

            skipRequested = false;
            inputLockChanged?.Invoke(true);

            try
            {
                if (!skipRequested)
                {
                    await CameraManager.Instance.PlayPathAsync(targetPath, default);
                }
            }
            finally
            {
                inputLockChanged?.Invoke(false);
                introCompleted?.Invoke();
            }
        }

        public void Skip()
        {
            skipRequested = true;
            CameraManager.Instance?.CancelPath();
        }

        private void OnDisable()
        {
            Skip();
        }
    }
}

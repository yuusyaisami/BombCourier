using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using BC.UI;
namespace BC.Managers
{
    public struct TextEffectData
    {
        public bool applyFontSize;
        public int fontSize;
    }
    public struct TalkRequestData
    {
        public string speakerName;
        public string dialogueText;
        public CinemachineCamera changeCinemachineCamera;

        public TextEffectData textEffectData;
    }
    public class TalkSystemManagerMB : MonoBehaviour
    {
        public static TalkSystemManagerMB Instance { get; private set; }
        [SerializeField] private UITalkSystemMB talkSystemUIManagerMB;

        // 会話中だけ前面に出すカメラと、通常時に戻すための優先度。
        [SerializeField] private int talkCameraPriority = 100;
        [SerializeField] private int inactiveCameraPriority = 0;

        private CinemachineCamera activeTalkCamera;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }

            SetCameraPriority(activeTalkCamera, inactiveCameraPriority);
            activeTalkCamera = null;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 重複命令が入った場合はcancellして新しい命令を実行する
        private CancellationTokenSource cancellationTokenSource;
        public async UniTask ShowTalk(TalkRequestData talkRequestData)
        {
            if (talkSystemUIManagerMB == null)
            {
                return;
            }

            // 連続で会話が来たら、前の待機処理を止めて新しい会話に切り替える。
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
            cancellationTokenSource = new CancellationTokenSource();

            // 会話用カメラを前面に出してからUIを表示する。
            SetCameraPriority(activeTalkCamera, inactiveCameraPriority);
            activeTalkCamera = talkRequestData.changeCinemachineCamera;
            SetCameraPriority(activeTalkCamera, talkCameraPriority);

            await talkSystemUIManagerMB.ShowTalk(talkRequestData, cancellationTokenSource.Token);
        }

        public async UniTask HideTalk(float duration = 0.3f)
        {
            // 非表示要求が来たら、待機中の入力処理を止める。
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }

            if (talkSystemUIManagerMB != null)
            {
                // UIを閉じるアニメーションを終えたあと、カメラを通常状態へ戻す。
                await talkSystemUIManagerMB.HideTalk(duration);
            }

            SetCameraPriority(activeTalkCamera, inactiveCameraPriority);
            activeTalkCamera = null;
        }

        private static void SetCameraPriority(CinemachineCamera camera, int priority)
        {
            if (camera == null)
            {
                return;
            }

            // Cinemachine 3系のPrioritySettingsを使って優先度だけ差し替える。
            PrioritySettings settings = camera.Priority;
            settings.Enabled = true;
            settings.Value = priority;
            camera.Priority = settings;
        }
    }
}
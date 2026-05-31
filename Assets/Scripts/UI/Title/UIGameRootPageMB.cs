using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BC.UI.Title
{
    // タイトルシーン起動直後の「Press Any Button」画面。
    // 任意入力を検知したら TitleSceneManagerMB.GoToMainPageAsync を呼ぶ。
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIGameRootPageMB : TitlePageBase
    {
        [Header("Animation")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.5f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.4f;

        private CanvasGroup canvasGroup;
        private bool waitingForInput;
        private bool inputReceived;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
        }

        public override async UniTask ShowAsync(CancellationToken ct)
        {
            IsShowing = true;
            gameObject.SetActive(true);

            await canvasGroup
                .DOFade(1f, fadeInDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .WithCancellation(ct);

            canvasGroup.interactable = true;
            waitingForInput = true;
        }

        public override async UniTask HideAsync(CancellationToken ct)
        {
            waitingForInput = false;
            canvasGroup.interactable = false;

            await canvasGroup
                .DOFade(0f, fadeOutDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .WithCancellation(ct);

            IsShowing = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!waitingForInput) return;
            if (inputReceived) return;

            // 任意入力検知 (キーボード・マウス・ゲームパッド すべて)
            bool anyKeyThisFrame = Keyboard.current?.anyKey.wasPressedThisFrame == true
                                || Mouse.current?.leftButton.wasPressedThisFrame == true
                                || Mouse.current?.rightButton.wasPressedThisFrame == true
                                || (Gamepad.current?.wasUpdatedThisFrame == true &&
                                    Gamepad.current.buttonSouth.wasPressedThisFrame);

            if (!anyKeyThisFrame) return;

            inputReceived = true;
            waitingForInput = false;
            OnAnyInputReceived().Forget();
        }

        private async UniTaskVoid OnAnyInputReceived()
        {
            TitleSceneManagerMB manager = TitleSceneManagerMB.Instance;
            if (manager == null) return;

            await manager.GoToMainPageAsync(true, destroyCancellationToken);
        }
    }
}

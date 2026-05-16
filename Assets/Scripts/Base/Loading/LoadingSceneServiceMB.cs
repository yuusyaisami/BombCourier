using BC.UI;
using UnityEngine;

namespace BC.Base
{
    // ApplicationKernel に loading 画面サービスを登録する installer。
    // 独自 DDOL Canvas と UIFadeEffectMB を受け取り、scene 遷移前後の表示制御をサービス化する。
    public sealed class LoadingSceneServiceMB : MonoBehaviour, IKernelInstaller
    {
        [SerializeField] private Canvas loadingCanvas;
        [SerializeField] private UIFadeEffectMB uiFadeEffectMB;
        [SerializeField] private FadeType fadeType = FadeType.Single;
        [SerializeField, Range(0f, 1f)] private float visibleAmount = 1f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.2f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.2f;

        public int Order => 5;

        private void Reset()
        {
            if (loadingCanvas == null)
            {
                loadingCanvas = GetComponentInChildren<Canvas>(true);
            }

            if (uiFadeEffectMB == null)
            {
                uiFadeEffectMB = GetComponentInChildren<UIFadeEffectMB>(true);
            }
        }

        private void Awake()
        {
            if (loadingCanvas != null)
            {
                loadingCanvas.enabled = false;

                if (Application.isPlaying)
                {
                    // Loading 用 Canvas は scene 切り替えでも残る前提なので DDOL に固定する。
                    DontDestroyOnLoad(loadingCanvas.transform.root.gameObject);
                }
            }
        }

        public void Setup<TKernel>(TKernel kernel) where TKernel : BaseKernel
        {
            if (kernel is not ApplicationKernel applicationKernel)
            {
                Debug.LogError("LoadingSceneServiceMB supports only ApplicationKernel.", this);
                return;
            }

            applicationKernel.LoadingScene = new LoadingSceneService(
                loadingCanvas,
                uiFadeEffectMB,
                fadeType,
                visibleAmount,
                fadeInDuration,
                fadeOutDuration);
        }
    }
}
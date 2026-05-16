using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using BC.UI;

namespace BC.Base
{
    // Application 常駐の loading 画面を管理するサービス。
    // Scene 遷移前後の見た目制御だけを担当し、実際の scene load は SceneManagerService 側で行う。
    public sealed class LoadingSceneService : IDisposable
    {
        private readonly Canvas loadingCanvas;
        private readonly UIFadeEffectMB fadeEffect;
        private readonly FadeType fadeType;
        private readonly float visibleAmount;
        private readonly float fadeInDuration;
        private readonly float fadeOutDuration;

        public LoadingSceneService(
            Canvas loadingCanvas,
            UIFadeEffectMB fadeEffect,
            FadeType fadeType,
            float visibleAmount,
            float fadeInDuration,
            float fadeOutDuration)
        {
            this.loadingCanvas = loadingCanvas;
            this.fadeEffect = fadeEffect;
            this.fadeType = fadeType;
            this.visibleAmount = Mathf.Clamp01(visibleAmount);
            this.fadeInDuration = Mathf.Max(0f, fadeInDuration);
            this.fadeOutDuration = Mathf.Max(0f, fadeOutDuration);

            SetCanvasEnabled(false);
        }

        public Canvas LoadingCanvas => loadingCanvas;
        public UIFadeEffectMB FadeEffect => fadeEffect;
        public bool IsVisible { get; private set; }

        public async UniTask ShowAsync()
        {
            SetCanvasEnabled(true);

            if (fadeEffect != null)
            {
                await fadeEffect.StartFadeAsync(fadeType, visibleAmount, fadeInDuration);
            }

            IsVisible = true;
        }

        public async UniTask HideAsync()
        {
            if (fadeEffect != null)
            {
                await fadeEffect.StartFadeAsync(fadeType, 0f, fadeOutDuration);
            }

            SetCanvasEnabled(false);
            IsVisible = false;
        }

        public void Dispose()
        {
            SetCanvasEnabled(false);
            IsVisible = false;
        }

        private void SetCanvasEnabled(bool isEnabled)
        {
            if (loadingCanvas != null)
            {
                loadingCanvas.enabled = isEnabled;
            }

            if (fadeEffect != null && fadeEffect.FadeCanvas != null)
            {
                fadeEffect.FadeCanvas.enabled = isEnabled;
            }
        }
    }
}
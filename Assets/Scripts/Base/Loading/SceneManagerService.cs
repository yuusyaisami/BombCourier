using System;
using System.Threading;
using BC.Rendering.Transition;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BC.Base
{
    // Application 常駐の scene 遷移サービス。
    // LoadingSceneService の表示完了を待ってから実際の scene load を開始する。
    public sealed class SceneManagerService
    {
        private readonly ApplicationKernel applicationKernel;

        public SceneManagerService(ApplicationKernel applicationKernel)
        {
            this.applicationKernel = applicationKernel;
        }

        private LoadingSceneService LoadingScene => applicationKernel?.LoadingScene;

        public async UniTask LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("SceneManagerService: sceneName is null or empty.");
                return;
            }

            if (LoadingScene != null)
            {
                await LoadingScene.ShowAsync();
            }

            AsyncOperation loadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            if (loadOperation == null)
            {
                Debug.LogError($"SceneManagerService: failed to create AsyncOperation for scene '{sceneName}'.");

                if (LoadingScene != null)
                {
                    await LoadingScene.HideAsync();
                }

                return;
            }

            while (!loadOperation.isDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (LoadingScene != null)
            {
                await LoadingScene.HideAsync();
            }
        }

        public async UniTask LoadSceneWithTransitionAsync(
            string sceneName,
            ScreenTransitionRequest transitionRequest,
            LoadSceneMode loadSceneMode = LoadSceneMode.Single,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("SceneManagerService: sceneName is null or empty.");
                return;
            }

            ScreenTransitionServiceMB transitionService = ScreenTransitionServiceMB.Instance;
            if (transitionService == null)
            {
                await LoadSceneAsync(sceneName, loadSceneMode);
                return;
            }

            ScreenTransitionRequest runtimeRequest = new(
                transitionRequest.Profile,
                transitionRequest.ExplicitToTexture,
                transitionRequest.OverrideDuration,
                captureFromCurrentFrame: transitionRequest.CaptureFromCurrentFrame,
                waitUntilToReady: true);

            UniTask transitionTask = transitionService.PlayAsync(runtimeRequest, ct);

            try
            {
                if (LoadingScene != null)
                    await LoadingScene.ShowAsync();

                AsyncOperation loadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
                if (loadOperation == null)
                {
                    Debug.LogError($"SceneManagerService: failed to create AsyncOperation for scene '{sceneName}'.");
                    transitionService.CancelCurrentTransition(ScreenTransitionCancelMode.HoldCurrentVisual);

                    if (LoadingScene != null)
                        await LoadingScene.HideAsync();

                    return;
                }

                while (!loadOperation.isDone)
                {
                    ct.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                if (LoadingScene != null)
                    await LoadingScene.HideAsync();

                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, ct);
                transitionService.SetToReady(true);

                await transitionTask;
            }
            catch (OperationCanceledException)
            {
                transitionService.CancelCurrentTransition(ScreenTransitionCancelMode.HoldCurrentVisual);
                throw;
            }
        }

        public UniTask LoadSceneAsync(int buildIndex, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            return LoadSceneAsync(SceneUtility.GetScenePathByBuildIndex(buildIndex), loadSceneMode, buildIndex);
        }

        public async UniTask LoadSceneWithTransitionAsync(
            int buildIndex,
            ScreenTransitionRequest transitionRequest,
            LoadSceneMode loadSceneMode = LoadSceneMode.Single,
            CancellationToken ct = default)
        {
            ScreenTransitionServiceMB transitionService = ScreenTransitionServiceMB.Instance;
            if (transitionService == null)
            {
                await LoadSceneAsync(buildIndex, loadSceneMode);
                return;
            }

            string sceneIdentifier = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            ScreenTransitionRequest runtimeRequest = new(
                transitionRequest.Profile,
                transitionRequest.ExplicitToTexture,
                transitionRequest.OverrideDuration,
                captureFromCurrentFrame: transitionRequest.CaptureFromCurrentFrame,
                waitUntilToReady: true);

            UniTask transitionTask = transitionService.PlayAsync(runtimeRequest, ct);

            try
            {
                if (LoadingScene != null)
                    await LoadingScene.ShowAsync();

                AsyncOperation loadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIndex, loadSceneMode);
                if (loadOperation == null)
                {
                    Debug.LogError($"SceneManagerService: failed to create AsyncOperation for buildIndex {buildIndex} ({sceneIdentifier}).");
                    transitionService.CancelCurrentTransition(ScreenTransitionCancelMode.HoldCurrentVisual);

                    if (LoadingScene != null)
                        await LoadingScene.HideAsync();

                    return;
                }

                while (!loadOperation.isDone)
                {
                    ct.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                if (LoadingScene != null)
                    await LoadingScene.HideAsync();

                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, ct);
                transitionService.SetToReady(true);

                await transitionTask;
            }
            catch (OperationCanceledException)
            {
                transitionService.CancelCurrentTransition(ScreenTransitionCancelMode.HoldCurrentVisual);
                throw;
            }
        }

        public UniTask ReloadActiveSceneAsync(LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            return LoadSceneAsync(activeScene.name, loadSceneMode);
        }

        private async UniTask LoadSceneAsync(string sceneIdentifier, LoadSceneMode loadSceneMode, int buildIndex)
        {
            if (LoadingScene != null)
            {
                await LoadingScene.ShowAsync();
            }

            AsyncOperation loadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIndex, loadSceneMode);
            if (loadOperation == null)
            {
                Debug.LogError($"SceneManagerService: failed to create AsyncOperation for buildIndex {buildIndex} ({sceneIdentifier}).");

                if (LoadingScene != null)
                {
                    await LoadingScene.HideAsync();
                }

                return;
            }

            while (!loadOperation.isDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (LoadingScene != null)
            {
                await LoadingScene.HideAsync();
            }
        }
    }
}
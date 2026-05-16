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

        public UniTask LoadSceneAsync(int buildIndex, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            return LoadSceneAsync(SceneUtility.GetScenePathByBuildIndex(buildIndex), loadSceneMode, buildIndex);
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
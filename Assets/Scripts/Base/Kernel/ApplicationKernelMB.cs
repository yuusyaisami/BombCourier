using System.Collections.Generic;
using UnityEngine;
namespace BC.Base
{
    [DefaultExecutionOrder(-10000)]
    public class ApplicationKernelMB : MonoBehaviour
    {
        public static ApplicationKernelMB Instance { get; private set; }
        private ApplicationKernel kernel;
        public List<GameObject> targetObjects; // Authoring対象者のObjectたち基本は自分だけ
        public ApplicationKernel Kernel => kernel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            KernelBuilder kernelBuilder = new KernelBuilder();
            kernel = kernelBuilder.Build<ApplicationKernel>(targetObjects.ToArray());
            EnsureCoreServices();
        }

        private void Update()
        {
            // 重複生成された 2 個目の ApplicationKernelMB は Awake で Destroy 予約され kernel を持たない。
            // 破棄が確定する前に Update が走っても NRE にならないよう null をガードする。
            kernel?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            kernel?.Dispose();
            kernel = null;

            if (Instance == this)
                Instance = null;
        }

        private void EnsureCoreServices()
        {
            if (kernel == null)
            {
                Debug.LogError($"{nameof(ApplicationKernelMB)}: failed to build {nameof(ApplicationKernel)}.", this);
                return;
            }

            // ValueStoreMB が targetObjects に未配線でも editor 反復中にアプリが落ちないよう、
            // 空の fallback store を生成して続行する。これは「失敗を隠す」fallback ではなく、
            // 明示的な警告つきの degraded mode として許容する domain contract である。
            // 本番配線では ValueStoreMB を必ず置き、この警告が出たら配線漏れとして扱う。
            if (kernel.KernelValueStore == null)
            {
                kernel.KernelValueStore = new KernelValueStoreService();
                Debug.LogWarning($"{nameof(ApplicationKernelMB)}: {nameof(ApplicationKernel.KernelValueStore)} was not installed. Auto-created fallback store. Add {nameof(ValueStoreMB)} to targetObjects for explicit wiring.", this);
            }
        }
    }
}
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
            kernel.Tick(Time.deltaTime);
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

            if (kernel.KernelValueStore == null)
            {
                kernel.KernelValueStore = new KernelValueStoreService();
                Debug.LogWarning($"{nameof(ApplicationKernelMB)}: {nameof(ApplicationKernel.KernelValueStore)} was not installed. Auto-created fallback store. Add {nameof(ValueStoreMB)} to targetObjects for explicit wiring.", this);
            }
        }
    }
}
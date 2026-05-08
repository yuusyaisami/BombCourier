using System.Collections.Generic;
using UnityEngine;
namespace BC.Base
{
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
        }

        private void Update()
        {
            kernel.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            kernel.Dispose();
        }
    }
}
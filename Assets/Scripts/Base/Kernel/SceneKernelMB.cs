using System.Collections.Generic;
using UnityEngine;
namespace BC.Base
{
    public class SceneKernelMB : MonoBehaviour
    {
        private SceneKernel kernel;
        public List<GameObject> targetObjects; // Authoring対象者のObjectたち基本は自分だけ
        public SceneKernel Kernel => kernel;

        private void Awake()
        {
            KernelBuilder kernelBuilder = new KernelBuilder();
            kernel = kernelBuilder.Build<SceneKernel>(targetObjects.ToArray());
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
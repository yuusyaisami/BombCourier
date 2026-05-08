using UnityEngine;
namespace BC.Base
{
    public class SceneKernelMB : MonoBehaviour
    {
        private SceneKernel kernel;
        public SceneKernel Kernel => kernel;

        private void Awake()
        {
            KernelBuilder kernelBuilder = new KernelBuilder();
            kernel = kernelBuilder.Build<SceneKernel>();
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
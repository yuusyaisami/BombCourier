using UnityEngine;

namespace BC.Base
{
    public class EventMB : MonoBehaviour, IKernelInstaller
    {
        // StoreやLifecycleが購読できるよう、Eventは先に初期化する。
        public int Order => -5;
        private EventService eventService;

        public void Setup<TKernel>(TKernel kernel) where TKernel : BaseKernel
        {
            if (kernel is SceneKernel sceneKernel)
            {
                eventService = new EventService();
                sceneKernel.Events = eventService;
            }
            else if (kernel is ApplicationKernel applicationKernel)
            {
                eventService = new EventService();
                applicationKernel.Events = eventService;
            }
            else
            {
                Debug.LogError("Unsupported kernel type for EventMB");
            }
        }
    }
}
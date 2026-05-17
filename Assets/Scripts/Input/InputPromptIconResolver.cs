using BC.Manager;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BC.Inputs
{
    public static class InputPromptIconResolver
    {
        public static Sprite ResolveIcon(InputManagerMB inputManager, InputActionReference actionReference, Sprite fallback = null)
        {
            return actionReference == null ? fallback : ResolveIcon(inputManager, actionReference.action, fallback);
        }

        public static Sprite ResolveIcon(InputManagerMB inputManager, InputAction action, Sprite fallback = null)
        {
            if (action == null)
            {
                return fallback;
            }

            InputManagerMB manager = inputManager != null ? inputManager : InputManagerMB.Instance;
            if (manager == null)
            {
                return fallback;
            }

            return manager.TryResolvePromptIcon(action, out Sprite icon) ? icon : fallback;
        }
    }
}
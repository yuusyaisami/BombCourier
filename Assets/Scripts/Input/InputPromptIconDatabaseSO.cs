using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BC.Inputs
{
    [CreateAssetMenu(fileName = "InputPromptIconDatabase", menuName = "BombCourier/Input/Prompt Icon Database")]
    public sealed class InputPromptIconDatabaseSO : ScriptableObject
    {
        [SerializeField] private Sprite fallbackKeyboardMouseIcon;
        [SerializeField] private Sprite fallbackGamepadIcon;
        [SerializeField] private List<ActionIconSet> actionIconSets = new();

        public bool TryResolveIcon(InputAction action, InputPromptDeviceKind deviceKind, string controlPath, out Sprite icon)
        {
            icon = ResolveGlobalFallback(deviceKind);
            if (action == null)
            {
                return icon != null;
            }

            ActionIconSet actionIconSet = FindActionIconSet(action);
            if (actionIconSet == null)
            {
                return icon != null;
            }

            if (actionIconSet.TryResolveBindingIcon(deviceKind, controlPath, out icon))
            {
                return true;
            }

            icon = actionIconSet.ResolveFallback(deviceKind) ?? ResolveGlobalFallback(deviceKind);
            return icon != null;
        }

        public Sprite ResolveGlobalFallback(InputPromptDeviceKind deviceKind)
        {
            return deviceKind switch
            {
                InputPromptDeviceKind.Gamepad => fallbackGamepadIcon,
                InputPromptDeviceKind.KeyboardMouse => fallbackKeyboardMouseIcon,
                _ => fallbackKeyboardMouseIcon != null ? fallbackKeyboardMouseIcon : fallbackGamepadIcon,
            };
        }

        private ActionIconSet FindActionIconSet(InputAction action)
        {
            Guid actionId = action.id;
            for (int i = 0; i < actionIconSets.Count; i++)
            {
                ActionIconSet iconSet = actionIconSets[i];
                if (iconSet == null || iconSet.ActionReference == null || iconSet.ActionReference.action == null)
                {
                    continue;
                }

                if (iconSet.ActionReference.action.id == actionId)
                {
                    return iconSet;
                }
            }

            return null;
        }

        [Serializable]
        private sealed class ActionIconSet
        {
            [SerializeField] private InputActionReference actionReference;
            [SerializeField] private Sprite fallbackKeyboardMouseIcon;
            [SerializeField] private Sprite fallbackGamepadIcon;
            [SerializeField] private List<BindingIconEntry> bindingIcons = new();

            public InputActionReference ActionReference => actionReference;

            public bool TryResolveBindingIcon(InputPromptDeviceKind deviceKind, string controlPath, out Sprite icon)
            {
                icon = null;
                if (string.IsNullOrWhiteSpace(controlPath))
                {
                    return false;
                }

                string normalizedPath = NormalizeControlPath(controlPath);
                for (int i = 0; i < bindingIcons.Count; i++)
                {
                    BindingIconEntry bindingIcon = bindingIcons[i];
                    if (bindingIcon == null || bindingIcon.DeviceKind != deviceKind)
                    {
                        continue;
                    }

                    if (!bindingIcon.Matches(normalizedPath))
                    {
                        continue;
                    }

                    icon = bindingIcon.Icon;
                    return icon != null;
                }

                return false;
            }

            public Sprite ResolveFallback(InputPromptDeviceKind deviceKind)
            {
                return deviceKind switch
                {
                    InputPromptDeviceKind.Gamepad => fallbackGamepadIcon,
                    InputPromptDeviceKind.KeyboardMouse => fallbackKeyboardMouseIcon,
                    _ => fallbackKeyboardMouseIcon != null ? fallbackKeyboardMouseIcon : fallbackGamepadIcon,
                };
            }
        }

        [Serializable]
        private sealed class BindingIconEntry
        {
            [SerializeField] private InputPromptDeviceKind deviceKind = InputPromptDeviceKind.KeyboardMouse;
            [SerializeField] private string controlPath;
            [SerializeField] private Sprite icon;

            public InputPromptDeviceKind DeviceKind => deviceKind;
            public Sprite Icon => icon;

            public bool Matches(string normalizedPath)
            {
                if (string.IsNullOrWhiteSpace(controlPath))
                {
                    return false;
                }

                return string.Equals(NormalizeControlPath(controlPath), normalizedPath, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string NormalizeControlPath(string controlPath)
        {
            return string.IsNullOrWhiteSpace(controlPath)
                ? string.Empty
                : controlPath.Trim();
        }
    }
}
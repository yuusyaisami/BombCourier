using System;
using BC.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace BC.Manager
{
    public sealed class InputManagerMB : MonoBehaviour
    {
        private const float MouseDeltaThresholdSqr = 0.01f;
        private const float ScrollThresholdSqr = 0.01f;
        private const float GamepadStickThreshold = 0.25f;
        private const float GamepadTriggerThreshold = 0.2f;

        public static InputManagerMB Instance { get; private set; }

        [Header("Cursor")]
        [SerializeField] private bool lockCursorOnStart = true;
        [SerializeField] private bool hideCursorWhenLocked = true;

        [Header("Prompt Icons")]
        [SerializeField] private InputPromptIconDatabaseSO promptIconDatabase;
        [SerializeField] private InputPromptDeviceKind defaultPromptDeviceKind = InputPromptDeviceKind.KeyboardMouse;

        private InputPromptDeviceKind lastUsedPromptDeviceKind = InputPromptDeviceKind.Unknown;

        public event Action<InputPromptDeviceKind> PromptDeviceKindChanged;

        public InputPromptDeviceKind CurrentPromptDeviceKind =>
            lastUsedPromptDeviceKind != InputPromptDeviceKind.Unknown ? lastUsedPromptDeviceKind : defaultPromptDeviceKind;

        public InputPromptIconDatabaseSO PromptIconDatabase => promptIconDatabase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            lastUsedPromptDeviceKind = defaultPromptDeviceKind;
            ApplyDefaultCursorState();
        }

        private void OnEnable()
        {
            InputSystem.onEvent += HandleInputEvent;
        }

        private void OnDisable()
        {
            InputSystem.onEvent -= HandleInputEvent;
        }

        private void OnDestroy()
        {
            InputSystem.onEvent -= HandleInputEvent;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static InputManagerMB EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            InputManagerMB found = UnityEngine.Object.FindAnyObjectByType<InputManagerMB>();
            if (found != null)
            {
                return found;
            }

            GameObject managerObject = new GameObject(nameof(InputManagerMB));
            return managerObject.AddComponent<InputManagerMB>();
        }

        public void ApplyDefaultCursorState()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL のポインタロックはユーザー操作（ジェスチャ）起点でしか成立せず、起動時の自動ロックは
            // "Pointer lock cannot be acquired immediately after the user has exited the lock" などの
            // SecurityError を誘発するだけで効かない。WebGL では常に解除状態で開始する
            // （ゲームプレイ開始時の明示 LockCursor は従来どおり呼ばれる）。
            SetCursorLocked(false);
#else
            SetCursorLocked(lockCursorOnStart);
#endif
        }


        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked || !hideCursorWhenLocked;
        }

        public void LockCursor()
        {
            SetCursorLocked(true);
        }

        public void UnlockCursor()
        {
            SetCursorLocked(false);
        }

        public InputPromptDeviceKind ResolvePromptDeviceKind(InputAction action)
        {
            // CurrentPromptDeviceKind は HandleInputEvent が全入力イベントを監視して管理する。
            // action.activeControl.device から更新すると、最後にそのアクションを起動した
            // デバイスで上書きされてしまい、デバイス切り替え検出が壊れる。
            return CurrentPromptDeviceKind;
        }

        public string ResolvePromptControlPath(InputAction action, InputPromptDeviceKind preferredDeviceKind)
        {
            if (action == null)
            {
                return null;
            }

            if (action.activeControl != null && InferPromptDeviceKind(action.activeControl.device) == preferredDeviceKind)
            {
                return action.activeControl.path;
            }

            ReadOnlyArray<InputBinding> bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                if (binding.isComposite || binding.isPartOfComposite || string.IsNullOrWhiteSpace(binding.effectivePath))
                {
                    continue;
                }

                if (MatchesDeviceKind(binding.effectivePath, preferredDeviceKind))
                {
                    return binding.effectivePath;
                }
            }

            for (int i = 0; i < action.controls.Count; i++)
            {
                InputControl control = action.controls[i];
                if (control == null || InferPromptDeviceKind(control.device) != preferredDeviceKind)
                {
                    continue;
                }

                return control.path;
            }

            return null;
        }

        public bool TryResolvePromptIcon(InputActionReference actionReference, out Sprite icon)
        {
            icon = null;
            return actionReference != null && TryResolvePromptIcon(actionReference.action, out icon);
        }

        public bool TryResolvePromptIcon(InputAction action, out Sprite icon)
        {
            InputPromptDeviceKind deviceKind = ResolvePromptDeviceKind(action);
            string controlPath = ResolvePromptControlPath(action, deviceKind);
            return TryResolvePromptIcon(action, deviceKind, controlPath, out icon);
        }

        public bool TryResolvePromptIcon(InputAction action, InputPromptDeviceKind deviceKind, string controlPath, out Sprite icon)
        {
            icon = null;
            return promptIconDatabase != null && promptIconDatabase.TryResolveIcon(action, deviceKind, controlPath, out icon);
        }

        private void HandleInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (device == null || !eventPtr.valid)
            {
                return;
            }

            if (!HasMeaningfulInput(device))
            {
                return;
            }

            UpdateLastUsedPromptDeviceKind(device);
        }

        private static bool HasMeaningfulInput(InputDevice device)
        {
            switch (device)
            {
                case Keyboard keyboard:
                    return keyboard.anyKey.isPressed;

                case Mouse mouse:
                    if (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed)
                        return true;

                    if (mouse.delta.ReadValue().sqrMagnitude > MouseDeltaThresholdSqr)
                        return true;

                    return mouse.scroll.ReadValue().sqrMagnitude > ScrollThresholdSqr;

                case Gamepad gamepad:
                    if (gamepad.buttonSouth.isPressed || gamepad.buttonNorth.isPressed ||
                        gamepad.buttonEast.isPressed || gamepad.buttonWest.isPressed ||
                        gamepad.startButton.isPressed || gamepad.selectButton.isPressed ||
                        gamepad.leftShoulder.isPressed || gamepad.rightShoulder.isPressed ||
                        gamepad.leftStickButton.isPressed || gamepad.rightStickButton.isPressed ||
                        gamepad.dpad.up.isPressed || gamepad.dpad.down.isPressed ||
                        gamepad.dpad.left.isPressed || gamepad.dpad.right.isPressed)
                    {
                        return true;
                    }

                    if (gamepad.leftTrigger.ReadValue() > GamepadTriggerThreshold ||
                        gamepad.rightTrigger.ReadValue() > GamepadTriggerThreshold)
                    {
                        return true;
                    }

                    return gamepad.leftStick.ReadValue().sqrMagnitude > GamepadStickThreshold * GamepadStickThreshold ||
                           gamepad.rightStick.ReadValue().sqrMagnitude > GamepadStickThreshold * GamepadStickThreshold;
            }

            return false;
        }

        private void UpdateLastUsedPromptDeviceKind(InputDevice device)
        {
            InputPromptDeviceKind deviceKind = InferPromptDeviceKind(device);
            if (deviceKind == InputPromptDeviceKind.Unknown || deviceKind == lastUsedPromptDeviceKind)
            {
                return;
            }

            lastUsedPromptDeviceKind = deviceKind;
            PromptDeviceKindChanged?.Invoke(lastUsedPromptDeviceKind);
        }

        private static InputPromptDeviceKind InferPromptDeviceKind(InputDevice device)
        {
            return device switch
            {
                Keyboard => InputPromptDeviceKind.KeyboardMouse,
                Mouse => InputPromptDeviceKind.KeyboardMouse,
                Gamepad => InputPromptDeviceKind.Gamepad,
                _ => InputPromptDeviceKind.Unknown,
            };
        }

        private static bool MatchesDeviceKind(string controlPath, InputPromptDeviceKind deviceKind)
        {
            if (string.IsNullOrWhiteSpace(controlPath))
            {
                return false;
            }

            return deviceKind switch
            {
                InputPromptDeviceKind.KeyboardMouse =>
                    controlPath.Contains("<Keyboard>", StringComparison.OrdinalIgnoreCase)
                    || controlPath.Contains("<Mouse>", StringComparison.OrdinalIgnoreCase),
                InputPromptDeviceKind.Gamepad => controlPath.Contains("<Gamepad>", StringComparison.OrdinalIgnoreCase),
                _ => false,
            };
        }
    }
}
using UnityEngine;

namespace BC.Manager
{
    public sealed class InputManagerMB : MonoBehaviour
    {
        public static InputManagerMB Instance { get; private set; }

        [Header("Cursor")]
        [SerializeField] private bool lockCursorOnStart = true;
        [SerializeField] private bool hideCursorWhenLocked = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyDefaultCursorState();
        }

        private void OnDestroy()
        {
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

            InputManagerMB found = Object.FindAnyObjectByType<InputManagerMB>();
            if (found != null)
            {
                return found;
            }

            GameObject managerObject = new GameObject(nameof(InputManagerMB));
            return managerObject.AddComponent<InputManagerMB>();
        }

        public void ApplyDefaultCursorState()
        {
            SetCursorLocked(lockCursorOnStart);
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
    }
}
using System;
using System.Threading;
using BC.UI.Effect;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BC.UI.Components
{
    /// <summary>
    /// UnityEngine.UI.Button をプロジェクト標準の挙動で包む軽量 Button コンポーネント。
    /// Focus / Select の視覚効果とクリック時 Flash をここに集約し、親 UI はボタンの意味だけを扱う。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonMB : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [Serializable]
        public sealed class UIButtonEvent : UnityEvent { }

        [Header("Core")]
        [SerializeField] private Button button;

        [Header("Effects")]
        [SerializeField] private UINoiseOutlineMB noiseOutline;
        [SerializeField] private UIButtonFlashMB buttonFlash;
        [SerializeField] private bool autoSelectOnPointerEnter = true;
        [SerializeField] private bool playFlashBeforeClick = true;

        [Header("Events")]
        [SerializeField] private UIButtonEvent onClick = new();

        private bool initialized;
        private bool focused;
        private bool invokingClick;

        public event Action<UIButtonMB> Focused;
        public event Action<UIButtonMB> Deselected;

        public Button UnityButton
        {
            get
            {
                EnsureInitialized();
                return button;
            }
        }

        public UIButtonEvent OnClick => onClick;

        public bool Interactable
        {
            get
            {
                EnsureInitialized();
                return button != null && button.interactable;
            }
            set
            {
                EnsureInitialized();
                if (button == null)
                    return;

                button.interactable = value;
                if (!value)
                    SetFocused(false, notify: true);
            }
        }

        public Navigation Navigation
        {
            get
            {
                EnsureInitialized();
                return button != null ? button.navigation : default;
            }
            set
            {
                EnsureInitialized();
                if (button != null)
                    button.navigation = value;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
            SetFocused(false, notify: false);
        }

        private void OnEnable()
        {
            EnsureInitialized();
            if (!IsSelectedByEventSystem())
                SetFocused(false, notify: false);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnUnityButtonClicked);
        }

        public void AddClickListener(UnityAction listener)
        {
            if (listener == null)
                return;

            onClick.RemoveListener(listener);
            onClick.AddListener(listener);
        }

        public void RemoveClickListener(UnityAction listener)
        {
            if (listener != null)
                onClick.RemoveListener(listener);
        }

        public void Select()
        {
            EnsureInitialized();
            if (button != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        public bool IsSelectionTarget(GameObject selectedObject)
        {
            EnsureInitialized();
            if (selectedObject == null)
                return false;

            Transform selectedTransform = selectedObject.transform;
            if (button != null)
            {
                if (selectedObject == button.gameObject)
                    return true;

                if (selectedTransform != null && selectedTransform.IsChildOf(button.transform))
                    return true;
            }

            return selectedTransform != null && selectedTransform.IsChildOf(transform);
        }

        public void SetFocusedImmediate(bool isFocused)
        {
            SetFocused(isFocused, notify: false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (autoSelectOnPointerEnter)
                Select();
            else
                SetFocused(true, notify: true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!IsSelectedByEventSystem())
                SetFocused(false, notify: true);
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetFocused(true, notify: true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetFocused(false, notify: true);
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            button ??= GetComponent<Button>();
            noiseOutline ??= GetComponentInChildren<UINoiseOutlineMB>(true);
            buttonFlash ??= GetComponent<UIButtonFlashMB>();
            if (buttonFlash == null)
                buttonFlash = GetComponentInChildren<UIButtonFlashMB>(true);

            if (button != null)
            {
                button.onClick.RemoveListener(OnUnityButtonClicked);
                button.onClick.AddListener(OnUnityButtonClicked);
            }

            initialized = true;
        }

        private void OnUnityButtonClicked()
        {
            if (invokingClick)
                return;

            if (!isActiveAndEnabled || !Interactable)
                return;

            InvokeClickAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid InvokeClickAsync(CancellationToken ct)
        {
            invokingClick = true;
            try
            {
                if (playFlashBeforeClick && buttonFlash != null)
                    await buttonFlash.PlayFlashAsync(ct);

                onClick.Invoke();
            }
            finally
            {
                invokingClick = false;
            }
        }

        private void SetFocused(bool isFocused, bool notify)
        {
            EnsureInitialized();
            if (focused == isFocused)
                return;

            focused = isFocused;
            noiseOutline?.SetFocused(isFocused);

            if (!notify)
                return;

            if (isFocused)
                Focused?.Invoke(this);
            else
                Deselected?.Invoke(this);
        }

        private bool IsSelectedByEventSystem()
        {
            return EventSystem.current != null && IsSelectionTarget(EventSystem.current.currentSelectedGameObject);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            initialized = false;
            EnsureInitialized();
        }

        private void OnValidate()
        {
            initialized = false;
            EnsureInitialized();
        }
#endif
    }
}

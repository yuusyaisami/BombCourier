using System;
using BC.UI.Effect;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BC.UI.Components
{
    /// <summary>
    /// Selectable に対する共通 focus 表示を担う。
    /// Button 以外の Slider / Toggle / TMP_Dropdown でも同じ UINoise 表示ルールを使う。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UISelectableFocusMB : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [Header("References")]
        [SerializeField] private Selectable selectable;
        [SerializeField] private UINoiseOutlineMB noiseOutline;

        [Header("Behavior")]
        [SerializeField] private bool autoSelectOnPointerEnter = true;

        private bool initialized;
        private bool focused;

        public event Action<UISelectableFocusMB> Focused;
        public event Action<UISelectableFocusMB> Deselected;

        public Selectable UnitySelectable
        {
            get
            {
                EnsureInitialized();
                return selectable;
            }
        }

        public UINoiseOutlineMB NoiseOutline
        {
            get
            {
                EnsureInitialized();
                return noiseOutline;
            }
        }

        public bool IsFocused => focused;

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

        public void Select()
        {
            EnsureInitialized();
            if (selectable != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }

        public bool IsSelectionTarget(GameObject selectedObject)
        {
            EnsureInitialized();
            if (selectedObject == null)
                return false;

            Transform selectedTransform = selectedObject.transform;
            if (selectedTransform == null)
                return false;

            if (selectable != null)
            {
                if (selectedObject == selectable.gameObject)
                    return true;

                if (selectedTransform.IsChildOf(selectable.transform))
                    return true;
            }

            return selectedTransform.IsChildOf(transform);
        }

        public void SetFocusedImmediate(bool isFocused)
        {
            EnsureInitialized();
            focused = isFocused;
            noiseOutline?.SetFocusedImmediate(isFocused);
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

            selectable ??= GetComponent<Selectable>();
            noiseOutline ??= GetComponent<UINoiseOutlineMB>();
            if (noiseOutline == null)
                noiseOutline = GetComponentInChildren<UINoiseOutlineMB>(true);

            initialized = true;
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

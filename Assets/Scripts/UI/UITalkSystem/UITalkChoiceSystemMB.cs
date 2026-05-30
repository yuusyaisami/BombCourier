using System;
using System.Collections.Generic;
using System.Threading;
using BC.Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BC.UI
{
    [DisallowMultipleComponent]
    public sealed class UITalkChoiceSystemMB : MonoBehaviour
    {
        [SerializeField] private RectTransform choiceRoot;
        [SerializeField] private UITalkChoiceItemMB choiceItemPrefab;
        [SerializeField] private InputActionReference moveSelectionInputAction;
        [SerializeField] private InputActionReference submitChoiceInputAction;
        [SerializeField, Min(1f)] private float itemMinHeight = 52f;
        [SerializeField, Min(0f)] private float itemSpacing = 10f;
        [SerializeField, Range(0.1f, 1.0f)] private float unselectedBackgroundAlphaMultiplier = 0.55f;
        [SerializeField] private Color selectedOutlineColor = new(1f, 0.95f, 0.35f, 1f);
        [SerializeField] private Vector2 selectedOutlineDistance = new(2f, -2f);
        [SerializeField, Range(0.1f, 1f)] private float navigationDeadZone = 0.5f;
        [SerializeField, Min(0.01f)] private float navigationInitialRepeatDelay = 0.22f;
        [SerializeField, Min(0.01f)] private float navigationRepeatInterval = 0.10f;

        private readonly List<UITalkChoiceItemMB> activeItems = new();
        private readonly List<UITalkChoiceItemMB> pooledItems = new();
        private VerticalLayoutGroup verticalLayoutGroup;
        private ContentSizeFitter contentSizeFitter;
        private UITalkChoiceItemMB runtimeChoiceTemplate;
        private int lastNavigationDirection;
        private float navigationRepeatTimer;
        private int clickedItemIndex = -1;

        private RectTransform ChoiceRoot => choiceRoot != null ? choiceRoot : transform as RectTransform;

        private void Awake()
        {
            EnsureStructure();
            ClearChoicesImmediate();
        }

        private void OnEnable()
        {
            moveSelectionInputAction?.action.Enable();
            submitChoiceInputAction?.action.Enable();
        }

        private void OnDisable()
        {
            moveSelectionInputAction?.action.Disable();
            submitChoiceInputAction?.action.Disable();
            ResetNavigationRepeatState();
        }

        private void OnDestroy()
        {
            moveSelectionInputAction?.action.Disable();
            submitChoiceInputAction?.action.Disable();
            ClearChoicesImmediate();
            DestroyPooledChoicesImmediate();
        }

        private void OnValidate()
        {
            EnsureStructure();
        }

        private void Reset()
        {
            EnsureStructure();
        }

        public async UniTask<TalkChoiceSelectionResult> ShowChoicesAsync(
            TalkChoiceRequestData requestData,
            InputAction fallbackSubmitAction,
            CancellationToken cancellationToken)
        {
            if (!requestData.HasOptions)
                return TalkChoiceSelectionResult.None;

            EnsureStructure();
            ClearChoicesImmediate();

            int selectedIndex = CreateItems(requestData);
            if (activeItems.Count == 0)
                return TalkChoiceSelectionResult.None;

            clickedItemIndex = -1;
            UpdateSelectionVisuals(selectedIndex);

            try
            {
                // 会話送りに使った同一フレームの入力をそのまま選択確定へ流さないようにする。
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                ResetNavigationRepeatState();

                while (!cancellationToken.IsCancellationRequested)
                {
                    int navigationStep = ReadNavigationStep();
                    if (navigationStep != 0)
                    {
                        selectedIndex = MoveSelection(selectedIndex, navigationStep, requestData.WrapSelection);
                        UpdateSelectionVisuals(selectedIndex);
                    }

                    if (clickedItemIndex >= 0)
                    {
                        selectedIndex = Mathf.Clamp(clickedItemIndex, 0, activeItems.Count - 1);
                        UpdateSelectionVisuals(selectedIndex);

                        TalkChoiceOptionRequestData clickedOption = requestData.Options[selectedIndex];
                        return new TalkChoiceSelectionResult(selectedIndex, clickedOption.DisplayText ?? string.Empty);
                    }

                    if (WasSubmitPressed(fallbackSubmitAction))
                    {
                        TalkChoiceOptionRequestData option = requestData.Options[selectedIndex];
                        return new TalkChoiceSelectionResult(selectedIndex, option.DisplayText ?? string.Empty);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            finally
            {
                ClearChoicesImmediate();
            }

            throw new OperationCanceledException(cancellationToken);
        }

        public void ClearChoicesImmediate()
        {
            for (int i = 0; i < activeItems.Count; i++)
            {
                UITalkChoiceItemMB item = activeItems[i];
                if (item == null)
                    continue;

                item.gameObject.SetActive(false);
                pooledItems.Add(item);
            }

            activeItems.Clear();
            ResetNavigationRepeatState();
            clickedItemIndex = -1;
        }

        private void EnsureStructure()
        {
            if (choiceRoot == null)
                choiceRoot = transform as RectTransform;

            if (choiceRoot == null)
                return;

            verticalLayoutGroup = choiceRoot.GetComponent<VerticalLayoutGroup>();
            if (verticalLayoutGroup == null)
                verticalLayoutGroup = choiceRoot.gameObject.AddComponent<VerticalLayoutGroup>();

            contentSizeFitter = choiceRoot.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
                contentSizeFitter = choiceRoot.gameObject.AddComponent<ContentSizeFitter>();

            verticalLayoutGroup.spacing = Mathf.Max(0f, itemSpacing);
            verticalLayoutGroup.childAlignment = TextAnchor.LowerCenter;
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childControlHeight = true;
            verticalLayoutGroup.childForceExpandWidth = true;
            verticalLayoutGroup.childForceExpandHeight = false;

            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private int CreateItems(TalkChoiceRequestData requestData)
        {
            UITalkChoiceItemMB template = EnsureChoiceTemplate();
            if (template == null)
                return 0;

            TalkChoiceOptionRequestData[] options = requestData.Options;
            for (int i = 0; i < options.Length; i++)
            {
                UITalkChoiceItemMB item = AcquireChoiceItem(template);
                item.gameObject.name = $"TalkChoiceItem_{i}";
                item.gameObject.SetActive(true);
                item.Initialize(
                    i,
                    itemMinHeight,
                    unselectedBackgroundAlphaMultiplier,
                    selectedOutlineColor,
                    selectedOutlineDistance,
                    HandleItemClicked);
                item.Apply(options[i].DisplayText);
                activeItems.Add(item);
            }

            if (activeItems.Count == 0)
                return 0;

            return Mathf.Clamp(requestData.DefaultSelectionIndex, 0, activeItems.Count - 1);
        }

        private UITalkChoiceItemMB AcquireChoiceItem(UITalkChoiceItemMB template)
        {
            for (int i = pooledItems.Count - 1; i >= 0; i--)
            {
                UITalkChoiceItemMB pooled = pooledItems[i];
                pooledItems.RemoveAt(i);

                if (pooled == null)
                    continue;

                pooled.transform.SetParent(ChoiceRoot, false);
                return pooled;
            }

            return Instantiate(template, ChoiceRoot, false);
        }

        private void DestroyPooledChoicesImmediate()
        {
            for (int i = 0; i < pooledItems.Count; i++)
            {
                if (pooledItems[i] != null)
                    Destroy(pooledItems[i].gameObject);
            }

            pooledItems.Clear();
        }

        private UITalkChoiceItemMB EnsureChoiceTemplate()
        {
            if (choiceItemPrefab != null)
                return choiceItemPrefab;

            if (runtimeChoiceTemplate != null)
                return runtimeChoiceTemplate;

            if (ChoiceRoot == null)
                return null;

            Transform existingTemplate = ChoiceRoot.Find("TalkChoiceItemTemplate");
            if (existingTemplate != null)
                runtimeChoiceTemplate = existingTemplate.GetComponent<UITalkChoiceItemMB>();

            if (runtimeChoiceTemplate == null)
            {
                GameObject templateObject = new GameObject("TalkChoiceItemTemplate", typeof(RectTransform), typeof(Image), typeof(UITalkChoiceItemMB));
                templateObject.transform.SetParent(ChoiceRoot, false);
                runtimeChoiceTemplate = templateObject.GetComponent<UITalkChoiceItemMB>();
            }

            runtimeChoiceTemplate.Initialize(
                -1,
                itemMinHeight,
                unselectedBackgroundAlphaMultiplier,
                selectedOutlineColor,
                selectedOutlineDistance,
                null);
            runtimeChoiceTemplate.gameObject.SetActive(false);
            return runtimeChoiceTemplate;
        }

        private void HandleItemClicked(int index)
        {
            if (index < 0 || index >= activeItems.Count)
                return;

            clickedItemIndex = index;
        }

        private int MoveSelection(int currentIndex, int navigationStep, bool wrapSelection)
        {
            if (activeItems.Count == 0)
                return 0;

            int nextIndex = currentIndex + navigationStep;

            if (wrapSelection)
            {
                if (nextIndex < 0)
                    nextIndex = activeItems.Count - 1;
                else if (nextIndex >= activeItems.Count)
                    nextIndex = 0;
            }
            else
            {
                nextIndex = Mathf.Clamp(nextIndex, 0, activeItems.Count - 1);
            }

            return nextIndex;
        }

        private void UpdateSelectionVisuals(int selectedIndex)
        {
            for (int i = 0; i < activeItems.Count; i++)
            {
                if (activeItems[i] != null)
                    activeItems[i].SetSelected(i == selectedIndex);
            }
        }

        private int ReadNavigationStep()
        {
            int direction = ReadNavigationDirection();
            if (direction == 0)
            {
                ResetNavigationRepeatState();
                return 0;
            }

            if (direction != lastNavigationDirection)
            {
                lastNavigationDirection = direction;
                navigationRepeatTimer = Mathf.Max(0.01f, navigationInitialRepeatDelay);
                return direction;
            }

            navigationRepeatTimer -= Time.unscaledDeltaTime;
            if (navigationRepeatTimer > 0f)
                return 0;

            navigationRepeatTimer = Mathf.Max(0.01f, navigationRepeatInterval);
            return direction;
        }

        private int ReadNavigationDirection()
        {
            InputAction action = moveSelectionInputAction?.action;
            if (action != null)
            {
                Vector2 input = action.ReadValue<Vector2>();
                int actionDirection = ResolveDirectionFromVector(input);
                if (actionDirection != 0)
                    return actionDirection;
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed)
                    return -1;
                if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
                    return 1;
            }

            if (Gamepad.current != null)
            {
                Gamepad gp = Gamepad.current;
                if (gp.dpad.up.isPressed)
                    return -1;
                if (gp.dpad.down.isPressed)
                    return 1;

                int stickDirection = ResolveDirectionFromVector(gp.leftStick.ReadValue());
                if (stickDirection != 0)
                    return stickDirection;
            }

            return 0;
        }

        private int ResolveDirectionFromVector(Vector2 input)
        {
            float verticalMagnitude = Mathf.Abs(input.y);
            float horizontalMagnitude = Mathf.Abs(input.x);

            if (verticalMagnitude >= navigationDeadZone)
                return input.y > 0f ? -1 : 1;

            if (horizontalMagnitude >= navigationDeadZone)
                return input.x > 0f ? 1 : -1;

            return 0;
        }

        private void ResetNavigationRepeatState()
        {
            lastNavigationDirection = 0;
            navigationRepeatTimer = 0f;
        }

        private bool WasSubmitPressed(InputAction fallbackSubmitAction)
        {
            InputAction submitAction = submitChoiceInputAction?.action ?? fallbackSubmitAction;
            return submitAction != null && submitAction.WasPressedThisFrame();
        }
    }
}
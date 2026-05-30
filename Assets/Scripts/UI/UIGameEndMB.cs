using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Febucci.TextAnimatorForUnity;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BC.UI
{
    [DisallowMultipleComponent]
    public sealed class UIGameEndMB : MonoBehaviour
    {
        [Serializable]
        public sealed class StoryLineData
        {
            [TextArea(2, 5)] public string Text = string.Empty;
            [Min(0f)] public float HoldDuration = 0.9f;
        }

        [Serializable]
        public sealed class CreditEntryData
        {
            [TextArea(1, 3)] public string Role = string.Empty;
            [TextArea(1, 3)] public string Name = string.Empty;
        }

        [Header("Root")]
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [SerializeField, Min(0f)] private float rootFadeDuration = 0.35f;

        [Header("Epilogue")]
        [SerializeField] private CanvasGroup epilogueCanvasGroup;
        [SerializeField] private RectTransform epilogueLineContainer;
        [SerializeField] private TypewriterComponent epilogueLinePrefab;
        [SerializeField] private StoryLineData[] epilogueLines = Array.Empty<StoryLineData>();
        [SerializeField] private InputActionReference nextEpilogueInputAction;
        [SerializeField, Min(0f)] private float epilogueAdvanceDelay = 1.25f;
        [SerializeField] private bool advanceOnSkipInput = true;

        [Header("Credits")]
        [SerializeField] private CanvasGroup creditsCanvasGroup;
        [SerializeField] private RectTransform creditsViewport;
        [SerializeField] private RectTransform creditsContent;
        [SerializeField] private TextMeshProUGUI creditsText;
        [SerializeField] private string creditsHeader = "STAFF ROLL";
        [SerializeField] private CreditEntryData[] credits = Array.Empty<CreditEntryData>();
        [SerializeField] private Button creditsSkipButton;
        [SerializeField, Min(0.1f)] private float creditsScrollDuration = 16f;
        [SerializeField, Min(0f)] private float creditsPadding = 80f;
        [SerializeField, Min(0f)] private float creditsEndHoldDuration = 0.75f;

        [Header("Final Greeting")]
        [SerializeField] private CanvasGroup finalGreetingCanvasGroup;
        [SerializeField] private TextMeshProUGUI finalGreetingText; // これは少し遅れて表示される。
        [SerializeField, Min(0f)] private float finalGreetingFadeInDuration = 0.75f;
        [SerializeField, Min(0f)] private float finalGreetingTextDelay = 0.3f;
        [SerializeField, Min(0f)] private float finalGreetingTextFadeDuration = 0.5f;
        [SerializeField, Min(0f)] private float finalGreetingFadeOutDuration = 1.25f;
        [SerializeField, Min(0f)] private float finalGreetingInputGraceDuration = 0.35f;

        private readonly List<TypewriterComponent> spawnedEpilogueLines = new();

        private void Awake()
        {
            HideImmediate();
        }

        private void OnEnable()
        {
            nextEpilogueInputAction?.action.Enable();
        }

        private void OnDisable()
        {
            nextEpilogueInputAction?.action.Disable();
        }

        private void OnDestroy()
        {
            nextEpilogueInputAction?.action.Disable();
        }

        public async UniTask ShowAsync(CancellationToken cancellationToken = default)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            try
            {
                PrepareRootForShow();

                if (rootCanvasGroup != null && rootFadeDuration > 0f)
                {
                    await rootCanvasGroup
                        .DOFade(1f, rootFadeDuration)
                        .SetLink(gameObject)
                        .AsyncWaitForCompletion()
                        .AsUniTask()
                        .AttachExternalCancellation(cancellationToken);
                }
                else if (rootCanvasGroup != null)
                {
                    rootCanvasGroup.alpha = 1f;
                }

                await ShowEpilogueAsync(cancellationToken);
                await ShowCreditsAsync(cancellationToken);
                await ShowFinalGreetingAndExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        public void HideImmediate()
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 0f;
                rootCanvasGroup.interactable = false;
                rootCanvasGroup.blocksRaycasts = false;
            }

            HideSectionImmediate(epilogueCanvasGroup);
            HideSectionImmediate(creditsCanvasGroup);
            HideSectionImmediate(finalGreetingCanvasGroup);

            ClearSpawnedEpilogueLines();

            if (creditsText != null)
                creditsText.text = string.Empty;

            if (finalGreetingText != null)
                SetTextAlpha(finalGreetingText, 0f);

            if (creditsContent != null)
                creditsContent.anchoredPosition = Vector2.zero;

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void PrepareRootForShow()
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 0f;
                rootCanvasGroup.interactable = true;
                rootCanvasGroup.blocksRaycasts = true;
            }

            ShowSectionImmediate(epilogueCanvasGroup, false);
            ShowSectionImmediate(creditsCanvasGroup, false);
            ShowSectionImmediate(finalGreetingCanvasGroup, false);

            if (finalGreetingText != null)
                SetTextAlpha(finalGreetingText, 0f);

            ClearSpawnedEpilogueLines();
        }

        private async UniTask ShowEpilogueAsync(CancellationToken cancellationToken)
        {
            if (epilogueCanvasGroup == null)
                return;

            ShowSectionImmediate(epilogueCanvasGroup, true);
            ClearSpawnedEpilogueLines();

            if (epilogueLineContainer == null || epilogueLinePrefab == null || epilogueLines == null || epilogueLines.Length == 0)
            {
                await WaitForEpilogueAdvanceAsync(epilogueAdvanceDelay, cancellationToken);
                HideSectionImmediate(epilogueCanvasGroup);
                return;
            }

            for (int i = 0; i < epilogueLines.Length; i++)
            {
                StoryLineData lineData = epilogueLines[i];
                TypewriterComponent line = Instantiate(epilogueLinePrefab, epilogueLineContainer);
                line.gameObject.SetActive(true);
                spawnedEpilogueLines.Add(line);

                string epilogueText = lineData != null ? lineData.Text ?? string.Empty : string.Empty;
                bool lineCompleted = string.IsNullOrEmpty(epilogueText);
                float autoAdvanceDelay = lineData != null ? Mathf.Max(0f, lineData.HoldDuration) : epilogueAdvanceDelay;
                float completedAt = lineCompleted ? Time.unscaledTime : -1f;
                bool waitForSubmitRelease = IsNextEpilogueActuated();

                UnityAction onTextShowed = () => lineCompleted = true;
                line.onTextShowed.AddListener(onTextShowed);

                try
                {
                    line.ShowText(epilogueText);
                    line.StartShowingText(true);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (waitForSubmitRelease)
                        {
                            if (IsNextEpilogueActuated())
                            {
                                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                                continue;
                            }

                            waitForSubmitRelease = false;
                        }

                        if (!lineCompleted)
                        {
                            if (WasNextEpiloguePressedThisFrame())
                            {
                                line.SkipTypewriter();
                                lineCompleted = true;
                                completedAt = Time.unscaledTime;
                                waitForSubmitRelease = IsNextEpilogueActuated();

                                if (advanceOnSkipInput)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (completedAt < 0f)
                            {
                                completedAt = Time.unscaledTime;
                            }

                            if (WasNextEpiloguePressedThisFrame())
                            {
                                break;
                            }

                            if (autoAdvanceDelay <= 0f || Time.unscaledTime - completedAt >= autoAdvanceDelay)
                            {
                                break;
                            }
                        }

                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    }
                }
                finally
                {
                    line.onTextShowed.RemoveListener(onTextShowed);
                }
            }

            HideSectionImmediate(epilogueCanvasGroup);
        }

        private async UniTask WaitForEpilogueAdvanceAsync(float fallbackDelaySeconds, CancellationToken cancellationToken)
        {
            bool waitForSubmitRelease = IsNextEpilogueActuated();
            float startedAt = Time.unscaledTime;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (waitForSubmitRelease)
                {
                    if (IsNextEpilogueActuated())
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                        continue;
                    }

                    waitForSubmitRelease = false;
                    startedAt = Time.unscaledTime;
                }

                if (WasNextEpiloguePressedThisFrame())
                {
                    break;
                }

                if (fallbackDelaySeconds <= 0f || Time.unscaledTime - startedAt >= fallbackDelaySeconds)
                {
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private async UniTask ShowCreditsAsync(CancellationToken cancellationToken)
        {
            if (creditsCanvasGroup == null)
                return;

            ShowSectionImmediate(creditsCanvasGroup, true);

            if (creditsText == null || creditsContent == null)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(creditsEndHoldDuration),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken);
                HideSectionImmediate(creditsCanvasGroup);
                return;
            }

            creditsText.text = BuildCreditsText();
            creditsText.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(creditsContent);

            float contentHeight = Mathf.Max(creditsText.preferredHeight, creditsContent.rect.height);
            float viewportHeight = creditsViewport != null ? creditsViewport.rect.height : creditsContent.rect.height;
            float travelDistance = viewportHeight * 0.5f + contentHeight + creditsPadding;
            float startY = travelDistance;
            float endY = -travelDistance;

            Vector2 anchoredPosition = creditsContent.anchoredPosition;
            creditsContent.anchoredPosition = new Vector2(anchoredPosition.x, startY);

            Tween scrollTween = creditsContent
                .DOAnchorPosY(endY, creditsScrollDuration)
                .SetEase(Ease.Linear)
                .SetLink(creditsContent.gameObject);

            if (creditsSkipButton != null)
            {
                UniTaskCompletionSource skipRequested = new();
                UnityAction onClick = () => skipRequested.TrySetResult();
                creditsSkipButton.onClick.AddListener(onClick);

                try
                {
                    int completedIndex = await UniTask.WhenAny(
                        scrollTween.AsyncWaitForCompletion().AsUniTask(),
                        skipRequested.Task);

                    if (completedIndex == 1)
                    {
                        scrollTween.Kill();
                        creditsContent.anchoredPosition = new Vector2(anchoredPosition.x, endY);
                    }
                }
                finally
                {
                    creditsSkipButton.onClick.RemoveListener(onClick);
                }
            }
            else
            {
                await scrollTween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(cancellationToken);
            }

            if (creditsEndHoldDuration > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(creditsEndHoldDuration),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken);
            }

            HideSectionImmediate(creditsCanvasGroup);
        }

        private async UniTask ShowFinalGreetingAndExitAsync(CancellationToken cancellationToken)
        {
            if (finalGreetingCanvasGroup != null)
            {
                ShowSectionImmediate(finalGreetingCanvasGroup, false);
                finalGreetingCanvasGroup.alpha = 0f;

                if (finalGreetingFadeInDuration > 0f)
                {
                    await finalGreetingCanvasGroup
                        .DOFade(1f, finalGreetingFadeInDuration)
                        .SetLink(finalGreetingCanvasGroup.gameObject)
                        .AsyncWaitForCompletion()
                        .AsUniTask()
                        .AttachExternalCancellation(cancellationToken);
                }
                else
                {
                    finalGreetingCanvasGroup.alpha = 1f;
                }
            }

            if (finalGreetingText != null)
            {
                SetTextAlpha(finalGreetingText, 0f);

                if (finalGreetingTextDelay > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(finalGreetingTextDelay),
                        DelayType.UnscaledDeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken);
                }

                if (finalGreetingTextFadeDuration > 0f)
                {
                    await finalGreetingText
                        .DOFade(1f, finalGreetingTextFadeDuration)
                        .SetLink(finalGreetingText.gameObject)
                        .AsyncWaitForCompletion()
                        .AsUniTask()
                        .AttachExternalCancellation(cancellationToken);
                }
                else
                {
                    SetTextAlpha(finalGreetingText, 1f);
                }
            }

            await WaitForAnyInputAsync(cancellationToken, finalGreetingInputGraceDuration);

            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.interactable = false;
                rootCanvasGroup.blocksRaycasts = false;

                if (finalGreetingFadeOutDuration > 0f)
                {
                    await rootCanvasGroup
                        .DOFade(0f, finalGreetingFadeOutDuration)
                        .SetLink(gameObject)
                        .AsyncWaitForCompletion()
                        .AsUniTask()
                        .AttachExternalCancellation(cancellationToken);
                }
                else
                {
                    rootCanvasGroup.alpha = 0f;
                }
            }
        }

        private string BuildCreditsText()
        {
            StringBuilder builder = new();

            if (!string.IsNullOrWhiteSpace(creditsHeader))
            {
                builder.AppendLine(creditsHeader.Trim());
                builder.AppendLine();
            }

            if (credits != null)
            {
                for (int i = 0; i < credits.Length; i++)
                {
                    CreditEntryData entry = credits[i];
                    if (entry == null)
                        continue;

                    bool wroteLine = false;
                    if (!string.IsNullOrWhiteSpace(entry.Role))
                    {
                        builder.AppendLine(entry.Role.Trim());
                        wroteLine = true;
                    }

                    if (!string.IsNullOrWhiteSpace(entry.Name))
                    {
                        builder.AppendLine(entry.Name.Trim());
                        wroteLine = true;
                    }

                    if (wroteLine)
                        builder.AppendLine();
                }
            }

            return builder.ToString().TrimEnd();
        }

        private static void SetTextAlpha(TextMeshProUGUI text, float alpha)
        {
            if (text == null)
                return;

            Color color = text.color;
            color.a = Mathf.Clamp01(alpha);
            text.color = color;
        }

        private static void ShowSectionImmediate(CanvasGroup group, bool interactable)
        {
            if (group == null)
                return;

            group.gameObject.SetActive(true);
            group.alpha = 1f;
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }

        private static void HideSectionImmediate(CanvasGroup group)
        {
            if (group == null)
                return;

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private bool WasNextEpiloguePressedThisFrame()
        {
            InputAction action = nextEpilogueInputAction != null ? nextEpilogueInputAction.action : null;
            return action != null && action.WasPressedThisFrame();
        }

        private bool IsNextEpilogueActuated()
        {
            InputAction action = nextEpilogueInputAction != null ? nextEpilogueInputAction.action : null;
            return action != null && action.IsPressed();
        }

        private async UniTask WaitForAnyInputAsync(CancellationToken cancellationToken, float inputGraceSeconds)
        {
            if (inputGraceSeconds > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(inputGraceSeconds),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                if (IsAnyInputPressed())
                {
                    return;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private static bool IsAnyInputPressed()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                return true;

            if (Mouse.current != null)
            {
                return Mouse.current.leftButton.wasPressedThisFrame
                    || Mouse.current.rightButton.wasPressedThisFrame
                    || Mouse.current.middleButton.wasPressedThisFrame;
            }

            if (Gamepad.current != null)
            {
                Gamepad gp = Gamepad.current;
                return gp.buttonSouth.wasPressedThisFrame
                    || gp.buttonNorth.wasPressedThisFrame
                    || gp.buttonEast.wasPressedThisFrame
                    || gp.buttonWest.wasPressedThisFrame
                    || gp.startButton.wasPressedThisFrame
                    || gp.selectButton.wasPressedThisFrame;
            }

            return false;
        }

        private void ClearSpawnedEpilogueLines()
        {
            if (spawnedEpilogueLines.Count == 0)
                return;

            for (int i = 0; i < spawnedEpilogueLines.Count; i++)
            {
                TypewriterComponent line = spawnedEpilogueLines[i];
                if (line != null)
                    Destroy(line.gameObject);
            }

            spawnedEpilogueLines.Clear();
        }

        private void ClearSpawnedLines(List<TextMeshProUGUI> spawnedLines)
        {
            if (spawnedLines == null)
                return;

            for (int i = 0; i < spawnedLines.Count; i++)
            {
                TextMeshProUGUI line = spawnedLines[i];
                if (line != null)
                    Destroy(line.gameObject);
            }

            spawnedLines.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
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
        [SerializeField] private Vector2 epilogueSpawnRange = new Vector2(220f, 120f);
        [SerializeField, Min(0f)] private float epiloguePreviousLineFadeOutDuration = 0.35f;

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
            HideSectionImmediate(finalGreetingCanvasGroup);

            ClearSpawnedEpilogueLines();

            if (finalGreetingText != null)
                SetTextAlpha(finalGreetingText, 0f);

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
            HideSectionImmediate(finalGreetingCanvasGroup);

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
                PositionEpilogueLine(line.transform as RectTransform);

                await FadeOutSpawnedLinesExceptAsync(line, cancellationToken);

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

            await FadeOutSpawnedLinesExceptAsync(null, cancellationToken);
            HideSectionImmediate(epilogueCanvasGroup);
            ClearSpawnedEpilogueLines();
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

            group.gameObject.SetActive(false);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private void PositionEpilogueLine(RectTransform lineRect)
        {
            if (lineRect == null)
                return;

            float x = UnityEngine.Random.Range(-Mathf.Abs(epilogueSpawnRange.x), Mathf.Abs(epilogueSpawnRange.x));
            float y = UnityEngine.Random.Range(-Mathf.Abs(epilogueSpawnRange.y), Mathf.Abs(epilogueSpawnRange.y));
            lineRect.anchoredPosition = new Vector2(x, y);
        }

        private async UniTask FadeOutSpawnedLinesExceptAsync(TypewriterComponent keepLine, CancellationToken cancellationToken)
        {
            if (spawnedEpilogueLines.Count == 0)
                return;

            float fadeDuration = Mathf.Max(0f, epiloguePreviousLineFadeOutDuration);

            for (int i = spawnedEpilogueLines.Count - 1; i >= 0; i--)
            {
                TypewriterComponent line = spawnedEpilogueLines[i];
                if (line == null)
                {
                    spawnedEpilogueLines.RemoveAt(i);
                    continue;
                }

                if (ReferenceEquals(line, keepLine))
                    continue;

                CanvasGroup lineCanvasGroup = line.GetComponent<CanvasGroup>() ?? line.gameObject.AddComponent<CanvasGroup>();

                if (fadeDuration > 0f)
                {
                    await lineCanvasGroup
                        .DOFade(0f, fadeDuration)
                        .SetLink(line.gameObject)
                        .AsyncWaitForCompletion()
                        .AsUniTask()
                        .AttachExternalCancellation(cancellationToken);
                }
                else
                {
                    lineCanvasGroup.alpha = 0f;
                }

                spawnedEpilogueLines.RemoveAt(i);
                Destroy(line.gameObject);
            }
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

    }
}

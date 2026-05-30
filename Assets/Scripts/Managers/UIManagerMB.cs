using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using BC.Manager;
using BC.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BC.Managers
{
    [DisallowMultipleComponent]
    public sealed class UIManagerMB : MonoBehaviour
    {
        public static UIManagerMB Instance { get; private set; }

        [Header("References")]
        [SerializeField] private UIFadeEffectMB fadeEffect;
        [SerializeField] private UIGameSceneManagerMB gameSceneManager;
        [SerializeField] private UIIntroPathSkipMB introPathSkipUI;
        [SerializeField] private UIGameEndMB gameEndUI;
        [SerializeField] private UIToastStackMB toastStackUI;
        [SerializeField] private UITalkSystemMB talkSystemUI;
        [SerializeField] private UITalkChoiceSystemMB talkChoiceSystemUI;

        public UIFadeEffectMB FadeEffect => fadeEffect;
        public UIGameSceneManagerMB GameSceneManager => gameSceneManager;
        public UIIntroPathSkipMB IntroPathSkipUI => introPathSkipUI;
        public UIGameEndMB GameEndUI => gameEndUI;
        public UIToastStackMB ToastStackUI => toastStackUI;
        public UITalkSystemMB TalkSystemUI => talkSystemUI;
        public UITalkChoiceSystemMB TalkChoiceSystemUI => talkChoiceSystemUI;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void ResolveReferences()
        {
            fadeEffect ??= GetComponentInChildren<UIFadeEffectMB>(true);
            gameSceneManager ??= GetComponentInChildren<UIGameSceneManagerMB>(true);
            introPathSkipUI ??= GetComponentInChildren<UIIntroPathSkipMB>(true);
            gameEndUI ??= GetComponentInChildren<UIGameEndMB>(true);
            toastStackUI ??= GetComponentInChildren<UIToastStackMB>(true);
            talkSystemUI ??= GetComponentInChildren<UITalkSystemMB>(true);
            talkChoiceSystemUI ??= GetComponentInChildren<UITalkChoiceSystemMB>(true);
        }
    }
}

namespace BC.UI
{
    [DisallowMultipleComponent]
    public sealed class UIGameEndMB : MonoBehaviour
    {
        [Serializable]
        public sealed class StoryLineData
        {
            [TextArea(2, 5)] public string Text = string.Empty;
            [Min(0f)] public float FadeInDuration = 0.35f;
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
        [SerializeField] private TextMeshProUGUI epilogueLinePrefab;
        [SerializeField] private StoryLineData[] epilogueLines = Array.Empty<StoryLineData>();
        [SerializeField] private Button epilogueContinueButton;
        [SerializeField, Min(0f)] private float epilogueAdvanceDelay = 1.25f;

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

        [Header("Thanks")]
        [SerializeField] private CanvasGroup thanksCanvasGroup;
        [SerializeField] private RectTransform thanksLineContainer;
        [SerializeField] private TextMeshProUGUI thanksLinePrefab;
        [SerializeField] private StoryLineData[] thanksLines = Array.Empty<StoryLineData>();
        [SerializeField] private Image logoImage;
        [SerializeField] private Sprite logoSprite;
        [SerializeField] private Button returnToTitleButton;

        private readonly List<TextMeshProUGUI> spawnedEpilogueLines = new();
        private readonly List<TextMeshProUGUI> spawnedThanksLines = new();

        private void Awake()
        {
            HideImmediate();
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
                await ShowThanksAsync(cancellationToken);
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
            HideSectionImmediate(thanksCanvasGroup);

            ClearSpawnedLines(spawnedEpilogueLines);
            ClearSpawnedLines(spawnedThanksLines);

            if (creditsText != null)
                creditsText.text = string.Empty;

            if (creditsContent != null)
                creditsContent.anchoredPosition = Vector2.zero;

            if (logoImage != null)
            {
                logoImage.sprite = logoSprite;
                logoImage.enabled = logoSprite != null;
            }

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
            ShowSectionImmediate(thanksCanvasGroup, false);

            ClearSpawnedLines(spawnedEpilogueLines);
            ClearSpawnedLines(spawnedThanksLines);
        }

        private async UniTask ShowEpilogueAsync(CancellationToken cancellationToken)
        {
            if (epilogueCanvasGroup == null)
                return;

            ShowSectionImmediate(epilogueCanvasGroup, true);
            ClearSpawnedLines(spawnedEpilogueLines);

            if (epilogueLineContainer == null || epilogueLinePrefab == null || epilogueLines == null || epilogueLines.Length == 0)
            {
                await WaitForContinueAsync(epilogueContinueButton, epilogueAdvanceDelay, cancellationToken);
                HideSectionImmediate(epilogueCanvasGroup);
                return;
            }

            for (int i = 0; i < epilogueLines.Length; i++)
            {
                StoryLineData lineData = epilogueLines[i];
                TextMeshProUGUI line = Instantiate(epilogueLinePrefab, epilogueLineContainer);
                line.text = lineData != null ? lineData.Text : string.Empty;
                SetTextAlpha(line, 0f);
                spawnedEpilogueLines.Add(line);

                float fadeInDuration = lineData != null ? Mathf.Max(0f, lineData.FadeInDuration) : 0f;
                if (fadeInDuration > 0f)
                {
                    await line
                        .DOFade(1f, fadeInDuration)
                        .SetLink(line.gameObject)
                        .AsyncWaitForCompletion()
                        .AsUniTask()
                        .AttachExternalCancellation(cancellationToken);
                }
                else
                {
                    SetTextAlpha(line, 1f);
                }

                float holdDuration = lineData != null ? Mathf.Max(0f, lineData.HoldDuration) : 0f;
                if (holdDuration > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(holdDuration),
                        DelayType.UnscaledDeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken);
                }
            }

            await WaitForContinueAsync(epilogueContinueButton, epilogueAdvanceDelay, cancellationToken);
            HideSectionImmediate(epilogueCanvasGroup);
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

        private async UniTask ShowThanksAsync(CancellationToken cancellationToken)
        {
            if (thanksCanvasGroup == null)
                return;

            ShowSectionImmediate(thanksCanvasGroup, true);
            ClearSpawnedLines(spawnedThanksLines);

            if (logoImage != null)
            {
                logoImage.sprite = logoSprite;
                logoImage.enabled = logoSprite != null;
            }

            if (thanksLineContainer != null && thanksLinePrefab != null && thanksLines != null)
            {
                for (int i = 0; i < thanksLines.Length; i++)
                {
                    StoryLineData lineData = thanksLines[i];
                    TextMeshProUGUI line = Instantiate(thanksLinePrefab, thanksLineContainer);
                    line.text = lineData != null ? lineData.Text : string.Empty;
                    SetTextAlpha(line, 0f);
                    spawnedThanksLines.Add(line);

                    float fadeInDuration = lineData != null ? Mathf.Max(0f, lineData.FadeInDuration) : 0f;
                    if (fadeInDuration > 0f)
                    {
                        await line
                            .DOFade(1f, fadeInDuration)
                            .SetLink(line.gameObject)
                            .AsyncWaitForCompletion()
                            .AsUniTask()
                            .AttachExternalCancellation(cancellationToken);
                    }
                    else
                    {
                        SetTextAlpha(line, 1f);
                    }

                    float holdDuration = lineData != null ? Mathf.Max(0f, lineData.HoldDuration) : 0f;
                    if (holdDuration > 0f)
                    {
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(holdDuration),
                            DelayType.UnscaledDeltaTime,
                            PlayerLoopTiming.Update,
                            cancellationToken);
                    }
                }
            }

            // エンド演出終了後はクリックまたは任意の入力でタイトルへ戻れるようにする。
            await WaitForContinueAsync(returnToTitleButton, 0f, cancellationToken, acceptAnyInput: true);
        }

        private async UniTask WaitForContinueAsync(Button button, float fallbackDelaySeconds, CancellationToken cancellationToken, bool acceptAnyInput = false)
        {
            if (button == null)
            {
                if (fallbackDelaySeconds > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(fallbackDelaySeconds),
                        DelayType.UnscaledDeltaTime,
                        PlayerLoopTiming.Update,
                        cancellationToken);
                }

                if (acceptAnyInput)
                {
                    await UniTask.WaitUntil(IsAnyInputPressed, PlayerLoopTiming.Update, cancellationToken);
                }

                return;
            }

            // acceptAnyInput かつ fallbackDelay がない場合、前フレームで押されていた入力が
            // 即座に拾われないよう、短いグレースピリオドを挿入する。
            if (acceptAnyInput && fallbackDelaySeconds <= 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(0.5f),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken);
            }

            UniTaskCompletionSource completionSource = new();
            UnityAction onClick = () => completionSource.TrySetResult();
            button.onClick.AddListener(onClick);

            try
            {
                if (acceptAnyInput)
                {
                    await UniTask.WhenAny(
                        completionSource.Task,
                        UniTask.WaitUntil(IsAnyInputPressed, PlayerLoopTiming.Update, cancellationToken));
                }
                else if (fallbackDelaySeconds > 0f)
                {
                    await UniTask.WhenAny(
                        completionSource.Task,
                        UniTask.Delay(
                            TimeSpan.FromSeconds(fallbackDelaySeconds),
                            DelayType.UnscaledDeltaTime,
                            PlayerLoopTiming.Update,
                            cancellationToken));
                }
                else
                {
                    await completionSource.Task;
                }
            }
            finally
            {
                button.onClick.RemoveListener(onClick);
            }
        }

        private static bool IsAnyInputPressed()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (Gamepad.current != null)
            {
                Gamepad gp = Gamepad.current;
                return gp.buttonSouth.wasPressedThisFrame ||
                       gp.buttonNorth.wasPressedThisFrame ||
                       gp.buttonEast.wasPressedThisFrame  ||
                       gp.buttonWest.wasPressedThisFrame  ||
                       gp.startButton.wasPressedThisFrame ||
                       gp.selectButton.wasPressedThisFrame;
            }
            return false;
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

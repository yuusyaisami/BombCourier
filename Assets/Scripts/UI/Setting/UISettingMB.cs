using System.Collections.Generic;
using System.Threading;
using BC.Audio;
using BC.Base;
using BC.Manager;
using BC.Managers;
using BC.UI.Title;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Localization;
using BC.UI.Effect;
using BC.UI.Components;
using Sirenix.OdinInspector;

namespace BC.UI
{
    // ゲームの設定画面を管理するクラス。
    // InputActionReference で指定されたキーでトグル表示し、
    // 設定を開いたときにゲーム時間を止めるかどうかは Inspector で切り替えられる。
    // 設定値は ApplicationKernel.KernelValueStore (ValueKeys.AppSettings) を唯一の真実源とする。
    [DisallowMultipleComponent]
    public class UISettingMB : MonoBehaviour
    {
        private static readonly ValueModifierTagId SettingsInputLockTag = new ValueModifierTagId(14001);
        private const string KeyMusicVolume = "Settings.MusicVolume";
        private const string KeySFXVolume = "Settings.SFXVolume";
        private const string KeyCameraSensitivity = "Settings.CameraSensitivity";
        private const string KeyInvertYAxis = "Settings.InvertYAxis";

        [Header("Open/Close Input")]
        [SerializeField] private InputActionReference openSettingAction;
        [Tooltip("このフラグがオンのとき、openSettingAction で設定画面のトグルを有効化します。")]
        [SerializeField] private bool applyOpenSettingActionEnabled = true;

        [Header("General")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;
        [SerializeField] private Selectable defaultSelectedOnOpen;
        [SerializeField] private UISelectableNavigationMapMB navigationMap;
        [Tooltip("設定画面を開いたときに Time.timeScale を 0 にするかどうかを切り替えます。")]
        [SerializeField] private bool pauseTimeScaleOnOpen = true;
        [SerializeField] private UIButtonMB closeButton;
        [SerializeField] private bool applyReturnToTitleButton = true;
        [SerializeField, ShowIf("applyReturnToTitleButton")] private UIButtonMB returnToTitleButton;

        [Header("Sound")]
        [Tooltip("設定画面を開いたときに再生するサウンドです。")]
        [SerializeField] private AudioDataSO openSound;

        [Header("Mouse Settings")]
        [SerializeField] private Toggle invertYAxisToggle;
        [SerializeField] private Slider cameraSensitivitySlider;
        [SerializeField] private TextMeshProUGUI cameraSensitivityValueText;

        [Header("Audio Settings")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Performance Settings")]
        [SerializeField] private Toggle vSyncToggle;
        [SerializeField] private TMP_Dropdown qualityLevelDropdown;

        [Header("Screen Settings")]
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown resolutionDropdown;

        [Header("Language Settings")]
        [Tooltip("言語選択ドロップダウン。LanguageManagerMB 経由で Unity Localization を切り替えます。")]
        [SerializeField] private TMP_Dropdown languageDropdown;

        [Header("Save Data")]
        [Tooltip("セーブデータ（ステージ進捗）削除ボタン。押すと二段階確認ダイアログを経て削除する。")]
        [SerializeField] private UIButtonMB deleteSaveDataButton;
        [Tooltip("削除の最終確認を行う二段階確認ダイアログ。設定パネルの上に重ねて表示する。")]
        [SerializeField] private UIConfirmDialogMB deleteSaveDataConfirmDialog;

        private bool isShowing = false;
        private bool isInitializing = false;
        private bool gameplayInputLockedBySettings;
        private bool hasLoggedMissingAppKernelStore;
        private bool hasLoggedBlockedByGameState;
        private bool hasStoredTimeScale;
        private float previousTimeScale = 1f;
        private bool isLockScene;
        private CancellationTokenSource toggleCts;
        // resolutionDropdown に表示している解像度の実リストを保持する。
        private readonly List<Resolution> availableResolutions = new();

        private void Awake()
        {
            // gameObject を非アクティブにするのではなく CanvasGroup で隠す。
            // Start() は常に実行されるため、初期化順序のバグを回避できる。
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            BuildResolutionDropdown();
            BuildQualityDropdown();
            if (applyReturnToTitleButton)
                EnsureReturnToTitleButton();
            EnsureSettingFocusWiring();
            ConfigureSettingNavigation();
            EnsureInitialSelectionConfigured();
        }

        private void Start()
        {
            isInitializing = true;
            LoadSettingsToUI(logMissingStore: false);
            isInitializing = false;

            RegisterListeners();
            RegisterCloseButton();
            RegisterDeleteSaveDataButton();
            ApplyOpenSettingActionEnabled();
        }

        private void Update()
        {
            if (isShowing && IsBlockedByCurrentGameState())
            {
                HidePanelAsync().Forget();
                return;
            }

            // applyOpenSettingActionEnabled=false のときはトグル入力を一切処理しない。
            // action.Disable() だけに頼らないのは、UINavigationBootstrap が「UI」アクションマップごと
            // 再有効化して openSettingAction（プロジェクト共通の Cancel 等）が復活し得るため。
            // 併せて、確認ダイアログ表示中もトグルを無視する（ダイアログ外への遷移漏れ・不整合状態を防ぐ）。
            if (applyOpenSettingActionEnabled
                && openSettingAction != null && openSettingAction.action != null
                && openSettingAction.action.WasPressedThisFrame()
                && (deleteSaveDataConfirmDialog == null || !deleteSaveDataConfirmDialog.IsOpen))
                ToggleSettingAsync().Forget();

        }

        private void OnDestroy()
        {
            UnregisterListeners();
            UnregisterCloseButton();
            UnregisterDeleteSaveDataButton();
            toggleCts?.Cancel();
            toggleCts?.Dispose();
            toggleCts = null;
            if (pauseTimeScaleOnOpen && hasStoredTimeScale)
                Time.timeScale = previousTimeScale;
            ApplyGameplayInputLock(false);
            if (modalGatePushed)
            {
                modalGatePushed = false;
                UiModalGate.Pop();
            }
        }

        private void EnsureReturnToTitleButton()
        {
            if (!applyReturnToTitleButton)
            {
                if (returnToTitleButton != null)
                    returnToTitleButton.UnityButton.gameObject.SetActive(false);
                return;
            }

            if (returnToTitleButton == null)
                returnToTitleButton = FindNamedButtonInChildren("ReturnTitleBtn");

            if (returnToTitleButton == null)
                return;

            returnToTitleButton.UnityButton.gameObject.SetActive(true);

            isLockScene = false;
            returnToTitleButton.RemoveClickListener(OnClickReturnToTitle);
            returnToTitleButton.AddClickListener(OnClickReturnToTitle);
        }

        private void OnClickReturnToTitle()
        {
            if (isLockScene)
                return;
            if (TryResolveAppKernelSceneManager(out SceneManagerService sceneManager))
            {
                sceneManager.LoadSceneAsync(0).Forget();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // トグル・表示制御
        // ─────────────────────────────────────────────────────────────────

        private async UniTaskVoid ToggleSettingAsync()
        {
            if (isShowing)
                await HidePanelAsync();
            else
                await ShowPanelAsync();
        }

        private bool modalGatePushed;

        public async UniTask ShowPanelAsync()
        {
            if (isShowing)
                return;

            if (IsBlockedByCurrentGameState())
                return;

            isShowing = true;
            modalGatePushed = true;
            UiModalGate.Push();

            UINavigationBootstrap.EnsureConfigured();
            EnsureCanvasRaycaster();
            transform.SetAsLastSibling();

            if (openSound != null && openSound.Clip != null)
                AudioSystemMB.Instance?.PlaySE(openSound);

            // 前の操作を中断して新しいトークンで開始する。
            toggleCts?.Cancel();
            toggleCts?.Dispose();
            toggleCts = new CancellationTokenSource();
            CancellationToken ct = toggleCts.Token;

            if (pauseTimeScaleOnOpen)
            {
                previousTimeScale = Time.timeScale;
                hasStoredTimeScale = true;
                Time.timeScale = 0f;
            }
            InputManagerMB.EnsureInstance().UnlockCursor();
            ApplyGameplayInputLock(true);

            if (canvasGroup == null)
                return;

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            Selectable initialSelection = defaultSelectedOnOpen != null
                ? defaultSelectedOnOpen
                : ResolveInitialSelection();

            if (initialSelection != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(initialSelection.gameObject);

            if (fadeDuration > 0f)
                await canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true).AsyncWaitForCompletion().AsUniTask()
                    .AttachExternalCancellation(ct);
            else
                canvasGroup.alpha = 1f;
        }

        public async UniTask HidePanelAsync()
        {
            if (!isShowing)
                return;
            isShowing = false;
            if (modalGatePushed)
            {
                modalGatePushed = false;
                UiModalGate.Pop();
            }

            toggleCts?.Cancel();
            toggleCts?.Dispose();
            toggleCts = new CancellationTokenSource();
            CancellationToken ct = toggleCts.Token;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                if (fadeDuration > 0f)
                    await canvasGroup.DOFade(0f, fadeDuration).SetUpdate(true).AsyncWaitForCompletion().AsUniTask()
                        .AttachExternalCancellation(ct);
                else
                    canvasGroup.alpha = 0f;
            }

            if (pauseTimeScaleOnOpen && hasStoredTimeScale)
            {
                Time.timeScale = previousTimeScale;
                hasStoredTimeScale = false;
            }
            InputManagerMB.EnsureInstance().LockCursor();
            ApplyGameplayInputLock(false);
        }

        public void ClosePanel()
        {
            // [SettingsDiag] 戻るボタンのクリックが C# に届いているかの確認用（WebGL 切り分け、後で削除可）。
            Debug.Log("[SettingsDiag] ClosePanel() invoked (back button click reached C#).", this);
            ClosePanelAndReturnAsync().Forget();
        }

        private async UniTaskVoid ClosePanelAndReturnAsync()
        {
            await HidePanelAsync();
            Debug.Log("[SettingsDiag] HidePanelAsync completed.", this);

            TitleSceneManagerMB titleSceneManager = TitleSceneManagerMB.Instance;
            if (titleSceneManager == null)
            {
                Debug.LogWarning("[SettingsDiag] TitleSceneManagerMB.Instance is null; cannot return to title.", this);
                return;
            }

            await titleSceneManager.ReturnToTitleMainFromSettingsAsync(destroyCancellationToken);
            Debug.Log("[SettingsDiag] ReturnToTitleMainFromSettingsAsync completed.", this);
        }

        // ─────────────────────────────────────────────────────────────────
        // 設定値の読み込み → UI への反映
        // ─────────────────────────────────────────────────────────────────

        private void LoadSettingsToUI(bool logMissingStore = true)
        {
            TryResolveAppKernelValueStore(out KernelValueStoreService store, logMissingStore);

            float musicVolume = store?.Get(ValueKeys.AppSettings.MusicVolume)
                                                                ?? PlayerPrefs.GetFloat(KeyMusicVolume, ValueKeys.AppSettings.MusicVolume.DefaultValue);
            float sfxVolume = store?.Get(ValueKeys.AppSettings.SFXVolume)
                                                            ?? PlayerPrefs.GetFloat(KeySFXVolume, ValueKeys.AppSettings.SFXVolume.DefaultValue);
            float sensitivity = store?.Get(ValueKeys.AppSettings.CameraSensitivity)
                                                                ?? PlayerPrefs.GetFloat(KeyCameraSensitivity, ValueKeys.AppSettings.CameraSensitivity.DefaultValue);
            bool invertY = store?.Get(ValueKeys.AppSettings.InvertYAxis)
                                                     ?? (PlayerPrefs.GetInt(KeyInvertYAxis, ValueKeys.AppSettings.InvertYAxis.DefaultValue ? 1 : 0) == 1);

            if (musicVolumeSlider != null)
                musicVolumeSlider.value = musicVolume;
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = sfxVolume;
            if (cameraSensitivitySlider != null)
                cameraSensitivitySlider.value = sensitivity;
            if (invertYAxisToggle != null)
                invertYAxisToggle.isOn = invertY;

            UpdateCameraSensitivityValueText(sensitivity);

            // 映像設定の現在値を反映する。
            if (vSyncToggle != null)
                vSyncToggle.isOn = QualitySettings.vSyncCount > 0;
            if (qualityLevelDropdown != null)
                qualityLevelDropdown.value = QualitySettings.GetQualityLevel();
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = Screen.fullScreen;
            RefreshResolutionDropdownSelection();
            BuildLanguageDropdown();
        }

        // ─────────────────────────────────────────────────────────────────
        // リスナー登録・解除
        // ─────────────────────────────────────────────────────────────────

        private void RegisterListeners()
        {
            if (invertYAxisToggle != null)
                invertYAxisToggle.onValueChanged.AddListener(OnInvertYAxisToggleChanged);
            if (cameraSensitivitySlider != null)
                cameraSensitivitySlider.onValueChanged.AddListener(OnCameraSensitivitySliderChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeSliderChanged);
            if (vSyncToggle != null)
                vSyncToggle.onValueChanged.AddListener(OnVSyncToggleChanged);
            if (qualityLevelDropdown != null)
                qualityLevelDropdown.onValueChanged.AddListener(OnQualityLevelChanged);
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            if (languageDropdown != null)
                languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);

            LanguageManagerMB languageManager = LanguageManagerMB.Instance;
            if (languageManager != null)
                languageManager.LanguageChanged += OnLanguageManagerChanged;
        }

        private void UnregisterListeners()
        {
            if (invertYAxisToggle != null)
                invertYAxisToggle.onValueChanged.RemoveListener(OnInvertYAxisToggleChanged);
            if (cameraSensitivitySlider != null)
                cameraSensitivitySlider.onValueChanged.RemoveListener(OnCameraSensitivitySliderChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeSliderChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeSliderChanged);
            if (vSyncToggle != null)
                vSyncToggle.onValueChanged.RemoveListener(OnVSyncToggleChanged);
            if (qualityLevelDropdown != null)
                qualityLevelDropdown.onValueChanged.RemoveListener(OnQualityLevelChanged);
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggleChanged);
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            if (languageDropdown != null)
                languageDropdown.onValueChanged.RemoveListener(OnLanguageDropdownChanged);

            LanguageManagerMB languageManager = LanguageManagerMB.Instance;
            if (languageManager != null)
                languageManager.LanguageChanged -= OnLanguageManagerChanged;
        }

        private void RegisterCloseButton()
        {
            if (closeButton == null)
                closeButton = FindNamedButtonInChildren("BackBtn");

            if (closeButton == null)
            {
                // [SettingsDiag] 戻るボタンの参照解決失敗（後で削除可）。
                Debug.LogWarning("[SettingsDiag] closeButton (BackBtn) not found; back button will do nothing.", this);
                return;
            }

            // [SettingsDiag] 戻るボタンが見つかり、クリックリスナを登録したことの確認用（後で削除可）。
            Debug.Log($"[SettingsDiag] closeButton registered: {closeButton.name} (interactable={closeButton.Interactable})", this);
            closeButton.RemoveClickListener(ClosePanel);
            closeButton.AddClickListener(ClosePanel);
        }

        private void UnregisterCloseButton()
        {
            if (closeButton != null)
                closeButton.RemoveClickListener(ClosePanel);
        }

        private void RegisterDeleteSaveDataButton()
        {
            if (deleteSaveDataButton == null)
                return;

            deleteSaveDataButton.RemoveClickListener(OnDeleteSaveDataClicked);
            deleteSaveDataButton.AddClickListener(OnDeleteSaveDataClicked);
        }

        private void UnregisterDeleteSaveDataButton()
        {
            if (deleteSaveDataButton != null)
                deleteSaveDataButton.RemoveClickListener(OnDeleteSaveDataClicked);
        }

        private void OnDeleteSaveDataClicked()
        {
            ConfirmAndDeleteSaveDataAsync().Forget();
        }

        // セーブデータ（ステージ進捗）の削除を二段階確認してから実行する。
        // 確認中は設定パネル本体を一時的に非 interactable にして、ダイアログ外への操作漏れを防ぐ
        // （ダイアログは ignoreParentGroups=true なので、その間も独立して操作できる）。
        private async UniTaskVoid ConfirmAndDeleteSaveDataAsync()
        {
            if (deleteSaveDataConfirmDialog == null)
            {
                Debug.LogWarning($"{nameof(UISettingMB)}: deleteSaveDataConfirmDialog is not assigned.", this);
                return;
            }

            if (canvasGroup != null)
                canvasGroup.interactable = false;

            try
            {
                bool confirmed = await deleteSaveDataConfirmDialog.ShowConfirmAsync(destroyCancellationToken);
                if (confirmed)
                {
                    // 進捗のみ削除（設定系キーは保持）。シーン再読込はせず、タイトル/ステージ選択の
                    // 次回表示時に星リセット・特典ボタン非表示が反映される。
                    TitleStageProgressServiceMB.DeleteAllProgress();
                }
            }
            finally
            {
                // パネルがまだ表示中なら操作を復帰し、フォーカスを削除ボタンへ戻す。
                if (canvasGroup != null)
                    canvasGroup.interactable = isShowing;
                if (isShowing && deleteSaveDataButton != null)
                    deleteSaveDataButton.Select();
            }
        }

        private void ApplyOpenSettingActionEnabled()
        {
            if (openSettingAction == null || openSettingAction.action == null)
                return;

            if (applyOpenSettingActionEnabled)
                openSettingAction.action.Enable();
            else
                openSettingAction.action.Disable();
        }

        // ─────────────────────────────────────────────────────────────────
        // 音声・カメラ設定 → KernelValueStore
        // ─────────────────────────────────────────────────────────────────

        private void OnMusicVolumeSliderChanged(float value)
        {
            if (isInitializing) return;
            float clamped = Mathf.Clamp01(value);
            if (TryResolveAppKernelValueStore(out KernelValueStoreService store))
                store.Set(ValueKeys.AppSettings.MusicVolume, clamped);
            PlayerPrefs.SetFloat(KeyMusicVolume, clamped);
        }

        private void OnSFXVolumeSliderChanged(float value)
        {
            if (isInitializing) return;
            float clamped = Mathf.Clamp01(value);
            if (TryResolveAppKernelValueStore(out KernelValueStoreService store))
                store.Set(ValueKeys.AppSettings.SFXVolume, clamped);
            PlayerPrefs.SetFloat(KeySFXVolume, clamped);
        }

        private void OnCameraSensitivitySliderChanged(float value)
        {
            if (isInitializing) return;
            float clamped = Mathf.Max(0.001f, value);
            if (TryResolveAppKernelValueStore(out KernelValueStoreService store))
                store.Set(ValueKeys.AppSettings.CameraSensitivity, clamped);
            PlayerPrefs.SetFloat(KeyCameraSensitivity, clamped);
            UpdateCameraSensitivityValueText(clamped);
        }

        private void OnInvertYAxisToggleChanged(bool isOn)
        {
            if (isInitializing) return;
            if (TryResolveAppKernelValueStore(out KernelValueStoreService store))
                store.Set(ValueKeys.AppSettings.InvertYAxis, isOn);
            PlayerPrefs.SetInt(KeyInvertYAxis, isOn ? 1 : 0);
        }

        // ─────────────────────────────────────────────────────────────────
        // 映像設定 → Unity API
        // ─────────────────────────────────────────────────────────────────

        private void OnVSyncToggleChanged(bool isOn)
        {
            if (isInitializing) return;
            QualitySettings.vSyncCount = isOn ? 1 : 0;
        }

        private void OnQualityLevelChanged(int index)
        {
            if (isInitializing) return;
            QualitySettings.SetQualityLevel(index, applyExpensiveChanges: true);
        }

        private void OnFullscreenToggleChanged(bool isOn)
        {
            if (isInitializing) return;
            Screen.fullScreen = isOn;
        }

        private void OnResolutionChanged(int index)
        {
            if (isInitializing) return;
            if (index < 0 || index >= availableResolutions.Count) return;
            Resolution res = availableResolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
        }

        // ─────────────────────────────────────────────────────────────────
        // Dropdown 生成ヘルパー
        // ─────────────────────────────────────────────────────────────────

        private void BuildResolutionDropdown()
        {
            if (resolutionDropdown == null) return;

            availableResolutions.Clear();
            resolutionDropdown.ClearOptions();

            Resolution[] all = Screen.resolutions;

            // WebGL ではブラウザが解像度を管理し、Screen.resolutions は空・Screen.SetResolution も実質無視される。
            // 解像度候補が無い環境では選択肢を作れないため、空ドロップダウンを出さず解像度設定 UI ごと隠す。
            if (all == null || all.Length == 0)
            {
                resolutionDropdown.gameObject.SetActive(false);
                return;
            }

            var options = new List<string>();

            foreach (Resolution res in all)
            {
                double hz = res.refreshRateRatio.numerator / (double)res.refreshRateRatio.denominator;
                options.Add($"{res.width} x {res.height} @ {hz:F0}Hz");
                availableResolutions.Add(res);
            }

            resolutionDropdown.AddOptions(options);
        }

        private void BuildQualityDropdown()
        {
            if (qualityLevelDropdown == null) return;

            qualityLevelDropdown.ClearOptions();
            qualityLevelDropdown.AddOptions(new List<string>(QualitySettings.names));
        }

        // ─────────────────────────────────────────────────────────────────
        // 言語設定 Dropdown
        // ─────────────────────────────────────────────────────────────────

        private void BuildLanguageDropdown()
        {
            if (languageDropdown == null)
                return;

            languageDropdown.ClearOptions();

            LanguageManagerMB manager = LanguageManagerMB.Instance;
            if (manager == null)
                return;

            // ローカライズ初期化前に AvailableLocales へ同期アクセスすると、WebGL では Localization が
            // 同期 WaitForCompletion を呼んで例外（WebGL は同期 Addressable 不可）を投げ、初期化自体を壊す。
            // 準備完了まで触らない。完了時に LanguageChanged → OnLanguageManagerChanged 経由で再構築される。
            if (!manager.IsReady)
                return;

            IReadOnlyList<Locale> locales = manager.AvailableLocales;
            var options = new List<string>(locales.Count);
            for (int i = 0; i < locales.Count; i++)
                options.Add(LanguageManagerMB.GetDisplayName(locales[i]));

            languageDropdown.AddOptions(options);
            RefreshLanguageDropdownSelection();
        }

        private void RefreshLanguageDropdownSelection()
        {
            if (languageDropdown == null)
                return;

            LanguageManagerMB manager = LanguageManagerMB.Instance;
            if (manager == null)
                return;

            // 初期化前の SelectedLocale / AvailableLocales 同期アクセスを避ける（WebGL の WaitForCompletion 対策）。
            if (!manager.IsReady)
                return;

            int index = manager.CurrentLocaleIndex;
            if (index >= 0 && index < languageDropdown.options.Count)
                languageDropdown.SetValueWithoutNotify(index);
        }

        private void OnLanguageDropdownChanged(int index)
        {
            if (isInitializing) return;
            LanguageManagerMB.Instance?.SetLanguageByIndex(index);
        }

        private void OnLanguageManagerChanged(Locale locale)
        {
            // マネージャ初期化前に UI が構築され、言語リストが空のままなら作り直す。
            if (languageDropdown != null && languageDropdown.options.Count == 0)
                BuildLanguageDropdown();
            else
                RefreshLanguageDropdownSelection();
        }

        private void RefreshResolutionDropdownSelection()
        {
            if (resolutionDropdown == null) return;

            Resolution current = Screen.currentResolution;
            for (int i = 0; i < availableResolutions.Count; i++)
            {
                Resolution res = availableResolutions[i];
                if (res.width == current.width && res.height == current.height)
                {
                    resolutionDropdown.value = i;
                    break;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // テキスト更新ヘルパー
        // ─────────────────────────────────────────────────────────────────

        private void UpdateCameraSensitivityValueText(float value)
        {
            if (cameraSensitivityValueText != null)
                cameraSensitivityValueText.text = value.ToString("0.000");
        }

        private void EnsureSettingFocusWiring()
        {
            EnsureToggleFocus(invertYAxisToggle);
            EnsureSliderFocus(cameraSensitivitySlider);
            EnsureSliderFocus(musicVolumeSlider);
            EnsureSliderFocus(sfxVolumeSlider);
            EnsureToggleFocus(vSyncToggle);
            EnsureToggleFocus(fullscreenToggle);
            EnsureDropdownFocus(qualityLevelDropdown);
            EnsureDropdownFocus(resolutionDropdown);
            EnsureDropdownFocus(languageDropdown);
        }

        private void ConfigureSettingNavigation()
        {
            if (closeButton == null)
                closeButton = FindNamedButtonInChildren("BackBtn");

            if (applyReturnToTitleButton && returnToTitleButton == null)
                returnToTitleButton = FindNamedButtonInChildren("ReturnTitleBtn");

            navigationMap ??= GetComponent<UISelectableNavigationMapMB>();
            if (navigationMap == null)
                navigationMap = gameObject.AddComponent<UISelectableNavigationMapMB>();

            var orderedSelectables = new List<Selectable>(10);
            AppendSelectable(orderedSelectables, invertYAxisToggle);
            AppendSelectable(orderedSelectables, cameraSensitivitySlider);
            AppendSelectable(orderedSelectables, musicVolumeSlider);
            AppendSelectable(orderedSelectables, sfxVolumeSlider);
            AppendSelectable(orderedSelectables, fullscreenToggle);
            AppendSelectable(orderedSelectables, resolutionDropdown);
            AppendSelectable(orderedSelectables, vSyncToggle);
            AppendSelectable(orderedSelectables, qualityLevelDropdown);
            AppendSelectable(orderedSelectables, languageDropdown);
            AppendSelectable(orderedSelectables, deleteSaveDataButton != null ? deleteSaveDataButton.UnityButton : null);
            AppendSelectable(orderedSelectables, closeButton != null ? closeButton.UnityButton : null);
            AppendSelectable(orderedSelectables, returnToTitleButton != null ? returnToTitleButton.UnityButton : null);

            var entries = new List<UISelectableNavigationMapMB.NavigationEntry>(orderedSelectables.Count);
            for (int i = 0; i < orderedSelectables.Count; i++)
            {
                Selectable selectable = orderedSelectables[i];
                if (selectable == null)
                    continue;

                entries.Add(new UISelectableNavigationMapMB.NavigationEntry
                {
                    selectable = selectable,
                    up = i > 0 ? orderedSelectables[i - 1] : null,
                    down = i + 1 < orderedSelectables.Count ? orderedSelectables[i + 1] : null,
                });
            }

            navigationMap.SetEntries(entries);
            navigationMap.Apply();
        }

        private void EnsureInitialSelectionConfigured()
        {
            defaultSelectedOnOpen = ResolveInitialSelection();
        }

        private Selectable ResolveInitialSelection()
        {
            // 開いた瞬間に必ず選択が入るよう、存在するものを順にフォールバックする(最後はボタンも候補)。
            if (cameraSensitivitySlider != null)
                return cameraSensitivitySlider;

            if (musicVolumeSlider != null)
                return musicVolumeSlider;

            if (sfxVolumeSlider != null)
                return sfxVolumeSlider;

            if (invertYAxisToggle != null)
                return invertYAxisToggle;

            if (fullscreenToggle != null)
                return fullscreenToggle;

            if (vSyncToggle != null)
                return vSyncToggle;

            if (resolutionDropdown != null)
                return resolutionDropdown;

            if (qualityLevelDropdown != null)
                return qualityLevelDropdown;

            if (closeButton != null && closeButton.UnityButton != null)
                return closeButton.UnityButton;

            if (returnToTitleButton != null && returnToTitleButton.UnityButton != null)
                return returnToTitleButton.UnityButton;

            return defaultSelectedOnOpen;
        }

        private void EnsureToggleFocus(Toggle toggle)
        {
            if (toggle == null)
                return;

            EnsureFocusComponent(toggle, toggle.transform as RectTransform, new Vector2(6f, 6f));
        }

        private void EnsureSliderFocus(Slider slider)
        {
            if (slider == null)
                return;

            RectTransform outlineTarget = slider.handleRect != null
                ? slider.handleRect
                : slider.transform as RectTransform;

            EnsureFocusComponent(slider, outlineTarget, new Vector2(2f, 2f));
        }

        private void EnsureDropdownFocus(TMP_Dropdown dropdown)
        {
            if (dropdown == null)
                return;

            EnsureFocusComponent(dropdown, dropdown.transform as RectTransform, new Vector2(6f, 6f));

            UITMPDropdownNavigationBridgeMB bridge = dropdown.GetComponent<UITMPDropdownNavigationBridgeMB>();
            if (bridge == null)
                bridge = dropdown.gameObject.AddComponent<UITMPDropdownNavigationBridgeMB>();

            if (dropdown.template == null)
                return;

            Toggle templateToggle = dropdown.template.GetComponentInChildren<Toggle>(true);
            if (templateToggle == null)
                return;

            EnsureFocusComponent(templateToggle, templateToggle.transform as RectTransform, new Vector2(6f, 3f));
        }

        private void EnsureFocusComponent(Selectable selectable, RectTransform outlineTarget, Vector2 padding)
        {
            if (selectable == null)
                return;

            UINoiseOutlineMB noiseOutline = selectable.GetComponent<UINoiseOutlineMB>();
            if (noiseOutline == null)
                noiseOutline = selectable.gameObject.AddComponent<UINoiseOutlineMB>();

            noiseOutline.SetTargetRect(outlineTarget);
            noiseOutline.SetPadding(padding);
            noiseOutline.SetFocusedImmediate(false);

            UISelectableFocusMB focus = selectable.GetComponent<UISelectableFocusMB>();
            if (focus == null)
                focus = selectable.gameObject.AddComponent<UISelectableFocusMB>();

            focus.SetFocusedImmediate(false);
        }

        private UIButtonMB FindNamedButtonInChildren(string buttonName)
        {
            if (string.IsNullOrWhiteSpace(buttonName))
                return null;

            UIButtonMB[] buttons = GetComponentsInChildren<UIButtonMB>(includeInactive: true);
            for (int i = 0; i < buttons.Length; i++)
            {
                UIButtonMB button = buttons[i];
                if (button != null && button.name == buttonName)
                    return button;
            }

            return null;
        }

        private static void AppendSelectable(List<Selectable> selectables, Selectable selectable)
        {
            // 非アクティブな選択肢（例: WebGL で隠した解像度ドロップダウン）はナビ経路に含めない。
            // 含めると上下ナビが「選択できない要素」で詰まってしまう。
            if (selectable == null || !selectable.gameObject.activeInHierarchy || selectables.Contains(selectable))
                return;

            selectables.Add(selectable);
        }

        private void ApplyGameplayInputLock(bool locked)
        {
            GameLogicManagerMB gameLogicManager = GameLogicManagerMB.Instance;
            PlayerMB player = gameLogicManager != null ? gameLogicManager.PlayerInstance : null;
            if (player == null)
            {
                gameplayInputLockedBySettings = false;
                return;
            }

            EntityMB entityMB = player.GetComponent<EntityMB>();
            SceneKernelMB sceneKernelMB = player.GetComponentInParent<SceneKernelMB>();

            if (entityMB == null || !entityMB.HasEntity || sceneKernelMB?.Kernel?.ValueStore == null)
                return;

            ValueStoreService store = sceneKernelMB.Kernel.ValueStore;
            EntityRef playerEntity = entityMB.Entity;

            if (locked)
            {
                store.SetBoolModifier(playerEntity, ValueKeys.Move.CanMoveByInput, SettingsInputLockTag, false);
                store.SetBoolModifier(playerEntity, ValueKeys.Camera.CanLookByInput, SettingsInputLockTag, false);
                store.SetBoolModifier(playerEntity, ValueKeys.Interaction.CanInteract, SettingsInputLockTag, false);
                gameplayInputLockedBySettings = true;
            }
            else if (gameplayInputLockedBySettings)
            {
                store.RemoveBoolModifier(playerEntity, ValueKeys.Move.CanMoveByInput, SettingsInputLockTag);
                store.RemoveBoolModifier(playerEntity, ValueKeys.Camera.CanLookByInput, SettingsInputLockTag);
                store.RemoveBoolModifier(playerEntity, ValueKeys.Interaction.CanInteract, SettingsInputLockTag);
                gameplayInputLockedBySettings = false;
            }
        }

        private void EnsureCanvasRaycaster()
        {
            Canvas targetCanvas = GetComponentInParent<Canvas>();
            if (targetCanvas == null)
                return;

            if (targetCanvas.GetComponent<GraphicRaycaster>() == null)
                targetCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        private bool TryResolveAppKernelValueStore(out KernelValueStoreService store, bool logIfMissing = true)
        {
            ApplicationKernelMB appKernelMB = ApplicationKernelMB.Instance;

            if (appKernelMB == null)
                appKernelMB = UnityEngine.Object.FindAnyObjectByType<ApplicationKernelMB>();

            store = appKernelMB != null ? appKernelMB.Kernel?.KernelValueStore : null;
            if (store != null)
                return true;

            if (logIfMissing && !hasLoggedMissingAppKernelStore)
            {
                Debug.LogWarning($"{nameof(UISettingMB)}: ApplicationKernel.KernelValueStore is not available yet. Check ApplicationKernelMB bootstrap and targetObjects wiring if this persists.", this);
                hasLoggedMissingAppKernelStore = true;
            }

            return false;
        }
        private bool TryResolveAppKernelSceneManager(out SceneManagerService sceneManager, bool logIfMissing = true)
        {
            ApplicationKernelMB appKernelMB = ApplicationKernelMB.Instance;

            if (appKernelMB == null)
                appKernelMB = UnityEngine.Object.FindAnyObjectByType<ApplicationKernelMB>();

            sceneManager = appKernelMB != null ? appKernelMB.Kernel?.SceneManager : null;
            if (sceneManager != null)
                return true;

            if (logIfMissing && !hasLoggedMissingAppKernelStore)
            {
                Debug.LogWarning($"{nameof(UISettingMB)}: ApplicationKernel.SceneManager is not available yet. Check ApplicationKernelMB bootstrap and targetObjects wiring if this persists.", this);
                hasLoggedMissingAppKernelStore = true;
            }

            return false;
        }

        private bool IsBlockedByCurrentGameState()
        {
            GameStateManagerMB stateManager = GameStateManagerMB.Instance;
            if (stateManager == null)
            {
                hasLoggedBlockedByGameState = false;
                return false;
            }

            bool blocked = stateManager.CurrentState == GameState.Loading ||
                           stateManager.CurrentState == GameState.Starting ||
                           stateManager.CurrentState == GameState.Intro ||
                           stateManager.CurrentState == GameState.Goaling ||
                           stateManager.CurrentState == GameState.NextStage ||
                           stateManager.CurrentState == GameState.ReturnToTitle;

            if (!blocked)
            {
                hasLoggedBlockedByGameState = false;
                return false;
            }

            if (!hasLoggedBlockedByGameState)
            {
                Debug.Log($"{nameof(UISettingMB)}: blocked opening settings while state is {stateManager.CurrentState}.", this);
                hasLoggedBlockedByGameState = true;
            }

            return true;
        }

    }
}

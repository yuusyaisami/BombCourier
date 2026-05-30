using System.Collections.Generic;
using System.Threading;
using BC.Audio;
using BC.Base;
using BC.Manager;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BC.UI
{
    // ゲームの設定画面を管理するクラス。
    // InputActionReference で指定されたキーでトグル表示し、
    // 開いた際は Time.timeScale = 0 でゲームを一時停止する。
    // 設定値は ApplicationKernel.KernelValueStore (ValueKeys.AppSettings) を唯一の真実源とする。
    [DisallowMultipleComponent]
    public class UISettingMB : MonoBehaviour
    {
        [Header("Open/Close Input")]
        [SerializeField] private InputActionReference openSettingAction;

        [Header("General")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

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

        private bool isShowing = false;
        private bool isInitializing = false;
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
        }

        private void Start()
        {
            isInitializing = true;
            LoadSettingsToUI();
            isInitializing = false;

            RegisterListeners();

            if (openSettingAction != null)
                openSettingAction.action?.Enable();
        }

        private void Update()
        {
            if (openSettingAction?.action?.WasPressedThisFrame() == true)
                ToggleSettingAsync().Forget();
        }

        private void OnDestroy()
        {
            UnregisterListeners();
            toggleCts?.Cancel();
            toggleCts?.Dispose();
            toggleCts = null;
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

        public async UniTask ShowPanelAsync()
        {
            if (isShowing)
                return;
            isShowing = true;

            if (openSound != null && openSound.Clip != null)
                AudioSystemMB.Instance?.PlaySE(openSound);

            // 前の操作を中断して新しいトークンで開始する。
            toggleCts?.Cancel();
            toggleCts?.Dispose();
            toggleCts = new CancellationTokenSource();
            CancellationToken ct = toggleCts.Token;

            Time.timeScale = 0f;
            InputManagerMB.EnsureInstance().UnlockCursor();

            if (canvasGroup == null)
                return;

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

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

            Time.timeScale = 1f;
            InputManagerMB.EnsureInstance().LockCursor();
        }

        // ─────────────────────────────────────────────────────────────────
        // 設定値の読み込み → UI への反映
        // ─────────────────────────────────────────────────────────────────

        private void LoadSettingsToUI()
        {
            KernelValueStoreService store = ApplicationKernelMB.Instance?.Kernel?.KernelValueStore;

            float musicVolume = store?.Get(ValueKeys.AppSettings.MusicVolume)
                                ?? ValueKeys.AppSettings.MusicVolume.DefaultValue;
            float sfxVolume = store?.Get(ValueKeys.AppSettings.SFXVolume)
                              ?? ValueKeys.AppSettings.SFXVolume.DefaultValue;
            float sensitivity = store?.Get(ValueKeys.AppSettings.CameraSensitivity)
                                ?? ValueKeys.AppSettings.CameraSensitivity.DefaultValue;
            bool invertY = store?.Get(ValueKeys.AppSettings.InvertYAxis)
                           ?? ValueKeys.AppSettings.InvertYAxis.DefaultValue;

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
        }

        // ─────────────────────────────────────────────────────────────────
        // 音声・カメラ設定 → KernelValueStore
        // ─────────────────────────────────────────────────────────────────

        private void OnMusicVolumeSliderChanged(float value)
        {
            if (isInitializing) return;
            ApplicationKernelMB.Instance?.Kernel?.KernelValueStore
                .Set(ValueKeys.AppSettings.MusicVolume, Mathf.Clamp01(value));
        }

        private void OnSFXVolumeSliderChanged(float value)
        {
            if (isInitializing) return;
            ApplicationKernelMB.Instance?.Kernel?.KernelValueStore
                .Set(ValueKeys.AppSettings.SFXVolume, Mathf.Clamp01(value));
        }

        private void OnCameraSensitivitySliderChanged(float value)
        {
            if (isInitializing) return;
            ApplicationKernelMB.Instance?.Kernel?.KernelValueStore
                .Set(ValueKeys.AppSettings.CameraSensitivity, Mathf.Max(0.001f, value));
            UpdateCameraSensitivityValueText(value);
        }

        private void OnInvertYAxisToggleChanged(bool isOn)
        {
            if (isInitializing) return;
            ApplicationKernelMB.Instance?.Kernel?.KernelValueStore
                .Set(ValueKeys.AppSettings.InvertYAxis, isOn);
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
    }
}

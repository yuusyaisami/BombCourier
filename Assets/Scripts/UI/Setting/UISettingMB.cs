using BC.Managers;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BC.UI
{
    // ゲームの設定画面を管理するクラス。
    // 画面上のUI要素と、ゲーム全体の設定を同期させる役割を持つ。
    public class UISettingMB : MonoBehaviour
    {
        [Header("General")]
        [SerializeField] private CanvasGroup canvasGroup; // パネル全体のCanvasGroupコンポーネント（フェードイン/アウト用）
        [Header("Mouse Settings")]
        [SerializeField] private Toggle invertYAxisToggle; // Y軸反転のオンオフを切り替えるトグル
        [SerializeField] private Slider cameraSensitivitySlider; // カメラ感度を調整するスライダー
        [SerializeField] private TextMeshProUGUI cameraSensitivityValueText; // カメラ感度の数値を表示するテキスト

        [Header("Audio Settings")]
        [SerializeField] private Slider musicVolumeSlider; // 音楽の音量を調整するスライダー
        [SerializeField] private Slider sfxVolumeSlider; // 効果音の音量を調整するスライダー

        [Header("Performance Settings")]
        // ここにパフォーマンス設定のUI要素を追加していく
        [SerializeField] private Toggle vSyncToggle; // 垂直同期のオンオフを切り替えるトグル
        [SerializeField] private TMP_Dropdown qualityLevelDropdown; // グラフィック品質のレベルを選択するドロップダウン

        private bool isInitialized = false;

        private bool isShowing = false;
        private void Awake()
        {
            // パネルを初期状態で非表示にする
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            gameObject.SetActive(false);
        }

        private void Start()
        {
            // ゲーム全体の設定から初期値を読み込んでUIに反映させる
            LoadSettingsToUI();

            // UI要素のイベントリスナーを登録する
            if (invertYAxisToggle != null)
                invertYAxisToggle.onValueChanged.AddListener(OnInvertYAxisToggleChanged);

            if (cameraSensitivitySlider != null)
                cameraSensitivitySlider.onValueChanged.AddListener(OnCameraSensitivitySliderChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeSliderChanged);
        }

        private void OnDestroy()
        {
            // イベントリスナーを解除してメモリリークを防止する
            if (invertYAxisToggle != null)
                invertYAxisToggle.onValueChanged.RemoveListener(OnInvertYAxisToggleChanged);

            if (cameraSensitivitySlider != null)
                cameraSensitivitySlider.onValueChanged.RemoveListener(OnCameraSensitivitySliderChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeSliderChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeSliderChanged);
        }

        public async UniTask ShowPanelAsync()
        {
            // パネルを表示するアニメーションやエフェクトをここで実装することができます。
            canvasGroup.alpha = 1f; // フェードインの例
            canvasGroup.interactable = true;
            gameObject.SetActive(true);
            isShowing = true;

        }
        public async UniTask HidePanelAsync()
        {
            isShowing = false;
            canvasGroup.interactable = false;
            await canvasGroup.DOFade(0f, 0.1f).OnComplete(() => gameObject.SetActive(false)); // フェードアウトの例

        }

        private void LoadSettingsToUI()
        {
            // SettingManagerMBから現在の設定を取得してUIに反映させる
            bool isInvertY = SettingManagerMB.Instance != null ? SettingManagerMB.Instance.IsInvertYAxis : false;
            float cameraSensitivity = SettingManagerMB.Instance != null ? SettingManagerMB.Instance.CameraSensitivity : 1f;
            float musicVolume = SettingManagerMB.Instance != null ? SettingManagerMB.Instance.MusicVolume : 1f;
            float sfxVolume = SettingManagerMB.Instance != null ? SettingManagerMB.Instance.SFXVolume : 1f;

            if (invertYAxisToggle != null)
                invertYAxisToggle.isOn = isInvertY;

            if (cameraSensitivitySlider != null)
                cameraSensitivitySlider.value = cameraSensitivity;

            if (musicVolumeSlider != null)
                musicVolumeSlider.value = musicVolume;

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = sfxVolume;

            isInitialized = true;

            UpdateCameraSensitivityValueText(cameraSensitivity);
        }

        private void OnInvertYAxisToggleChanged(bool isOn)
        {
            // ゲーム全体の設定にY軸反転の変更を反映させる
            if (SettingManagerMB.Instance != null)
            {
                SettingManagerMB.Instance.SetInvertYAxis(isOn);
            }
        }
        private void OnCameraSensitivitySliderChanged(float value)
        {
            // ゲーム全体の設定にカメラ感度の変更を反映させる
            if (SettingManagerMB.Instance != null)
            {
                SettingManagerMB.Instance.SetCameraSensitivity(value);
            }
            UpdateCameraSensitivityValueText(value);
        }
        private void OnMusicVolumeSliderChanged(float value)
        {
            // ゲーム全体の設定に音楽の音量の変更を反映させる
            if (SettingManagerMB.Instance != null)
            {
                SettingManagerMB.Instance.SetMusicVolume(value);
            }
        }
        private void OnSFXVolumeSliderChanged(float value)
        {
            // ゲーム全体の設定に効果音の音量の変更を反映させる
            if (SettingManagerMB.Instance != null)
            {
                SettingManagerMB.Instance.SetSFXVolume(value);
            }
        }

        private void UpdateCameraSensitivityValueText(float value)
        {
            if (cameraSensitivityValueText != null)
            {
                cameraSensitivityValueText.text = value.ToString("0.00");
            }
        }
    }

}
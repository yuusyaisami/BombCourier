using System;
using BC.Base;
using UnityEngine;

namespace BC.Managers
{
    // アプリケーション設定の永続化担当。
    // 起動時に PlayerPrefs から設定を読み込んで ApplicationKernel.KernelValueStore に書き込む。
    // KernelValueStore の変化を Watch して PlayerPrefs に自動保存する。
    // 実際の設定値は KernelValueStore (ValueKeys.AppSettings) を唯一の真実源とする。
    public class SettingManagerMB : MonoBehaviour
    {
        private const string KeyMusicVolume = "Settings.MusicVolume";
        private const string KeySFXVolume = "Settings.SFXVolume";
        private const string KeyCameraSensitivity = "Settings.CameraSensitivity";
        private const string KeyInvertYAxis = "Settings.InvertYAxis";

        public static SettingManagerMB Instance { get; private set; }

        private EventSubscription musicVolumeSub;
        private EventSubscription sfxVolumeSub;
        private EventSubscription cameraSensitivitySub;
        private EventSubscription invertYAxisSub;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Multiple instances of {nameof(SettingManagerMB)} detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            KernelValueStoreService store = ApplicationKernelMB.Instance?.Kernel?.KernelValueStore;
            if (store == null)
            {
                Debug.LogError($"{nameof(SettingManagerMB)}: ApplicationKernel.KernelValueStore is not available.", this);
                return;
            }

            // PlayerPrefs から読み込んで KernelValueStore に反映する。
            store.Set(ValueKeys.AppSettings.MusicVolume,
                PlayerPrefs.GetFloat(KeyMusicVolume, ValueKeys.AppSettings.MusicVolume.DefaultValue));
            store.Set(ValueKeys.AppSettings.SFXVolume,
                PlayerPrefs.GetFloat(KeySFXVolume, ValueKeys.AppSettings.SFXVolume.DefaultValue));
            store.Set(ValueKeys.AppSettings.CameraSensitivity,
                PlayerPrefs.GetFloat(KeyCameraSensitivity, ValueKeys.AppSettings.CameraSensitivity.DefaultValue));
            store.Set(ValueKeys.AppSettings.InvertYAxis,
                PlayerPrefs.GetInt(KeyInvertYAxis, ValueKeys.AppSettings.InvertYAxis.DefaultValue ? 1 : 0) == 1);

            // 変化を Watch して PlayerPrefs に自動保存する。
            musicVolumeSub = store.GetHandle(ValueKeys.AppSettings.MusicVolume)
                .Subscribe(v => PlayerPrefs.SetFloat(KeyMusicVolume, v));
            sfxVolumeSub = store.GetHandle(ValueKeys.AppSettings.SFXVolume)
                .Subscribe(v => PlayerPrefs.SetFloat(KeySFXVolume, v));
            cameraSensitivitySub = store.GetHandle(ValueKeys.AppSettings.CameraSensitivity)
                .Subscribe(v => PlayerPrefs.SetFloat(KeyCameraSensitivity, v));
            invertYAxisSub = store.GetHandle(ValueKeys.AppSettings.InvertYAxis)
                .Subscribe(v => PlayerPrefs.SetInt(KeyInvertYAxis, v ? 1 : 0));
        }

        private void OnDestroy()
        {
            musicVolumeSub?.Dispose();
            sfxVolumeSub?.Dispose();
            cameraSensitivitySub?.Dispose();
            invertYAxisSub?.Dispose();

            if (Instance == this)
                Instance = null;
        }
    }
}
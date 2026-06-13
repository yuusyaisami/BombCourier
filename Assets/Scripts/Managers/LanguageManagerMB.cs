using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace BC.Managers
{
    // 言語設定の永続化と適用を担当するアプリ全体マネージャ。
    // - 外部から言語指定（Locale / コード / index）を受けたら Unity Localization の SelectedLocale を切り替える。
    // - セットアップ時、保存済み言語が無ければ PC の systemLanguage から自動推論する。
    // - 選択言語は PlayerPrefs("Settings.Language") に locale コードで永続化する。
    public class LanguageManagerMB : MonoBehaviour
    {
        private const string KeyLanguage = "Settings.Language";

        public static LanguageManagerMB Instance { get; private set; }

        // 言語が切り替わったとき（起動時の初期適用含む）に発火する。UI のドロップダウン更新等に使う。
        public event Action<Locale> LanguageChanged;

        // Localization 初期化と初期言語適用が完了したか。
        public bool IsReady { get; private set; }

        public IReadOnlyList<Locale> AvailableLocales
        {
            get
            {
                // 初期化未完了で provider.Locales に触れると、WebGL では Localization が内部で同期
                // WaitForCompletion を呼んで例外（WebGL は同期 Addressable 不可）を投げ、初期化自体を壊す。
                // 完了前は空を返し、同期アクセスを発生源で断つ（呼び出し側に依存しない安全策）。
                if (!LocalizationSettings.InitializationOperation.IsDone)
                    return Array.Empty<Locale>();

                ILocalesProvider provider = LocalizationSettings.AvailableLocales;
                return provider != null ? provider.Locales : (IReadOnlyList<Locale>)Array.Empty<Locale>();
            }
        }

        // 初期化未完了の SelectedLocale 取得も同期ロードを誘発し得るため、完了前は null を返す。
        public Locale CurrentLocale =>
            LocalizationSettings.InitializationOperation.IsDone ? LocalizationSettings.SelectedLocale : null;

        public int CurrentLocaleIndex
        {
            get
            {
                IReadOnlyList<Locale> locales = AvailableLocales;
                Locale current = CurrentLocale;
                for (int i = 0; i < locales.Count; i++)
                {
                    if (locales[i] == current)
                        return i;
                }

                return -1;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            // 重複により破棄予定の場合は何もしない。
            if (Instance != this)
                yield break;

            // AvailableLocales がロードされるまで Localization の初期化を待つ。
            yield return LocalizationSettings.InitializationOperation;

            ApplyInitialLocale();
            IsReady = true;

            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;

            // 初期言語が確定したことを購読者（UI 等）へ通知する。
            LanguageChanged?.Invoke(CurrentLocale);
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            Instance = null;
        }

        // 起動時の言語決定: 保存済み > systemLanguage 推論 > ProjectLocale/先頭。
        private void ApplyInitialLocale()
        {
            if (AvailableLocales.Count == 0)
            {
                Debug.LogWarning($"{nameof(LanguageManagerMB)}: 利用可能な Locale がありません。Localization 設定を確認してください。", this);
                return;
            }

            Locale locale = ResolveSavedLocale();
            bool inferred = false;
            if (locale == null)
            {
                locale = InferSystemLocale();
                inferred = true;
            }

            if (locale == null)
                return;

            LocalizationSettings.SelectedLocale = locale;

            // 推論で決めた場合のみ保存する（以後は保存済みを優先）。
            if (inferred)
                SaveLocale(locale);
        }

        // ─────────────────────────────────────────────
        // 外部公開API（言語指定を受けて切り替えを起動する）
        // ─────────────────────────────────────────────

        public void SetLanguage(Locale locale)
        {
            if (locale == null || !IsLocaleAvailable(locale))
                return;

            // Unity Localization の言語切り替えを起動する。
            LocalizationSettings.SelectedLocale = locale;
            SaveLocale(locale);
        }

        public void SetLanguage(string localeCode)
        {
            if (string.IsNullOrEmpty(localeCode) || LocalizationSettings.AvailableLocales == null)
                return;

            SetLanguage(LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(localeCode)));
        }

        public void SetLanguageByIndex(int index)
        {
            IReadOnlyList<Locale> locales = AvailableLocales;
            if (index < 0 || index >= locales.Count)
                return;

            SetLanguage(locales[index]);
        }

        // ─────────────────────────────────────────────
        // ヘルパー
        // ─────────────────────────────────────────────

        private Locale ResolveSavedLocale()
        {
            string code = PlayerPrefs.GetString(KeyLanguage, string.Empty);
            if (string.IsNullOrEmpty(code) || LocalizationSettings.AvailableLocales == null)
                return null;

            return LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
        }

        private Locale InferSystemLocale()
        {
            ILocalesProvider provider = LocalizationSettings.AvailableLocales;
            if (provider == null)
                return null;

            Locale locale = provider.GetLocale(new LocaleIdentifier(Application.systemLanguage));
            if (locale != null)
                return locale;

            if (LocalizationSettings.ProjectLocale != null && IsLocaleAvailable(LocalizationSettings.ProjectLocale))
                return LocalizationSettings.ProjectLocale;

            return AvailableLocales.Count > 0 ? AvailableLocales[0] : null;
        }

        private bool IsLocaleAvailable(Locale locale)
        {
            if (locale == null)
                return false;

            IReadOnlyList<Locale> locales = AvailableLocales;
            for (int i = 0; i < locales.Count; i++)
            {
                if (locales[i] == locale)
                    return true;
            }

            return false;
        }

        private void SaveLocale(Locale locale)
        {
            if (locale == null)
                return;

            PlayerPrefs.SetString(KeyLanguage, locale.Identifier.Code);
            PlayerPrefs.Save();
        }

        private void OnSelectedLocaleChanged(Locale newLocale)
        {
            // SelectedLocale が（外部要因含め）変わったら永続化し、購読者へ通知する。
            SaveLocale(newLocale);
            LanguageChanged?.Invoke(newLocale);
        }

        // 言語名の表示用文字列（ドロップダウン等で利用）。ネイティブ名 > locale コード。
        public static string GetDisplayName(Locale locale)
        {
            if (locale == null)
                return string.Empty;

            var culture = locale.Identifier.CultureInfo;
            if (culture != null && !string.IsNullOrEmpty(culture.NativeName))
                return culture.NativeName;

            return locale.Identifier.Code;
        }
    }
}

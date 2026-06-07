using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace BC.Localization
{
    /// <summary>
    /// String Table を (table, key) から現在ロケールで解決する共有ユーティリティ。
    /// 見つからない場合は fallback を返す。WaitForCompletion を避け非同期取得（WebGL 安全）。
    /// </summary>
    public static class LocalizedStringResolver
    {
        public static UniTask<string> ResolveAsync(LocalizedStringTable tableField, string key, string fallback)
        {
            TableReference table = tableField != null ? tableField.TableReference : default;
            return ResolveAsync(table, key, fallback);
        }

        public static async UniTask<string> ResolveAsync(TableReference table, string key, string fallback)
        {
            fallback ??= string.Empty;

            if (table.ReferenceType == TableReference.Type.Empty || string.IsNullOrWhiteSpace(key))
                return fallback;

            try
            {
                var handle = LocalizationSettings.StringDatabase.GetTableAsync(table);
                StringTable stringTable = handle.IsDone ? handle.Result : await handle.Task;
                if (stringTable == null)
                    return fallback;

                StringTableEntry entry = stringTable.GetEntry(key);
                if (entry == null)
                    return fallback;

                string localized = entry.GetLocalizedString();
                return string.IsNullOrEmpty(localized) ? fallback : localized;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{nameof(LocalizedStringResolver)}: resolve failed (key={key}): {exception.Message}");
                return fallback;
            }
        }
    }
}

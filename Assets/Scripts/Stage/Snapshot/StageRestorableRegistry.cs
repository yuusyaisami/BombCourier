using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.Stage.Snapshot
{
    /// <summary>
    /// 復元対象（<see cref="StageRestorableMB"/>）の能動的レジストリ。
    /// 各対象が OnEnable/OnDisable/OnDestroy で自己登録/解除するため、
    /// シーンスキャンや stageRoot 配下探索に依存せず、Player所有オブジェクトなど
    /// 階層外の対象も確実に拾える（旧システムの収集漏れを解消）。
    ///
    /// 本ゲームはゲームプレイが単一アクティブシーンで進行するためグローバル静的で十分。
    /// （additive で複数ゲームプレイシーンを同時運用する場合はシーン別分割が必要。）
    /// </summary>
    public static class StageRestorableRegistry
    {
        private static readonly List<StageRestorableMB> orderedActive = new List<StageRestorableMB>(64);
        private static readonly Dictionary<string, StageRestorableMB> activeByKey = new Dictionary<string, StageRestorableMB>(StringComparer.Ordinal);
        private static readonly Dictionary<string, StageRestorableMB> inactiveByKey = new Dictionary<string, StageRestorableMB>(StringComparer.Ordinal);

        /// <summary>登録順（=キャプチャ/復元の決定的な反復順）のアクティブ対象一覧。</summary>
        public static IReadOnlyList<StageRestorableMB> OrderedActive => orderedActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            orderedActive.Clear();
            activeByKey.Clear();
            inactiveByKey.Clear();
        }

        public static void Register(StageRestorableMB restorable)
        {
            if (restorable == null)
                return;

            string key = restorable.RuntimeKey;
            if (string.IsNullOrEmpty(key))
                key = ComposeKey(restorable);

            // 別の生存インスタンスが同キーを既に使っている場合のみ序数で退避（基本起きない安全網）。
            if (activeByKey.TryGetValue(key, out StageRestorableMB existing) && existing != null && existing != restorable)
                key = ComposeUnique(key, restorable);

            restorable.SetRuntimeKey(key);
            inactiveByKey.Remove(key);
            activeByKey[key] = restorable;
            if (!orderedActive.Contains(restorable))
                orderedActive.Add(restorable);
        }

        public static void MoveToInactive(StageRestorableMB restorable)
        {
            if (restorable == null)
                return;

            string key = restorable.RuntimeKey;
            if (string.IsNullOrEmpty(key))
                return;

            activeByKey.Remove(key);
            orderedActive.Remove(restorable);
            inactiveByKey[key] = restorable;
        }

        public static void Remove(StageRestorableMB restorable)
        {
            if (restorable == null)
                return;

            string key = restorable.RuntimeKey;
            if (!string.IsNullOrEmpty(key))
            {
                if (activeByKey.TryGetValue(key, out StageRestorableMB active) && active == restorable)
                    activeByKey.Remove(key);
                if (inactiveByKey.TryGetValue(key, out StageRestorableMB inactive) && inactive == restorable)
                    inactiveByKey.Remove(key);
            }

            orderedActive.Remove(restorable);
        }

        /// <summary>キーから生存インスタンスを解決する（アクティブ→非アクティブの順）。</summary>
        public static bool TryResolve(string key, out StageRestorableMB restorable)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (activeByKey.TryGetValue(key, out restorable) && restorable != null)
                    return true;
                if (inactiveByKey.TryGetValue(key, out restorable) && restorable != null)
                    return true;
            }

            restorable = null;
            return false;
        }

        private static string ComposeKey(StageRestorableMB restorable)
        {
            string baseId = restorable.RawId;
            if (string.IsNullOrEmpty(baseId))
            {
                baseId = Guid.NewGuid().ToString("N");
                Debug.LogWarning(
                    $"StageSnapshot: '{GetHierarchyPath(restorable.transform)}' に stableId がありません。実行時IDを割り当てました。Stage Snapshot 検証メニューで修正してください。",
                    restorable);
            }

            return ComposeUnique(baseId, restorable);
        }

        private static string ComposeUnique(string baseKey, StageRestorableMB restorable)
        {
            if (!activeByKey.ContainsKey(baseKey) && !inactiveByKey.ContainsKey(baseKey))
                return baseKey;

            int ordinal = 1;
            string candidate;
            do
            {
                candidate = baseKey + ":" + ordinal.ToString();
                ordinal++;
            }
            while (activeByKey.ContainsKey(candidate) || inactiveByKey.ContainsKey(candidate));

            Debug.LogWarning(
                $"StageSnapshot: stableId '{baseKey}' が重複しています（'{GetHierarchyPath(restorable.transform)}'）。'{candidate}' として退避しました。検証メニューで一意化してください。",
                restorable);
            return candidate;
        }

        internal static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return "<null>";

            var stack = new Stack<string>();
            Transform cursor = transform;
            while (cursor != null)
            {
                stack.Push(cursor.name);
                cursor = cursor.parent;
            }

            return string.Join("/", stack);
        }
    }
}

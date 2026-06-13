using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BC.ActionSystem;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace BC.Editor
{
    /// <summary>
    /// 既存 InlineAction（NPCObjectMB.interactionAction / TutorialStageAuthoringMB の Steps）内の
    /// ShowTalk / ShowDialogue / Choice について、フォールバック(=日本語)テキストと一致する String Table の
    /// エントリを探し、Table / Key / Apply Set Table を自動で流し込むエディタツール。
    ///
    /// マッチは「フォールバック文字列 == その Stage の String Table の ja 値」で行う。
    /// 既に Key が入っているもの・空テキストはスキップする（再実行しても安全）。
    /// speakerName 系（話者名）は対象外（別途）。
    /// </summary>
    public static class TalkLocalizationAutoFillTool
    {
        private struct Target
        {
            public string PrefabName;
            public string Collection;
        }

        // 対象プレハブと、使用する String Table コレクション。
        private static readonly Target[] Targets =
        {
            new Target { PrefabName = "MapInstanceLv1",  Collection = "Tutorial_Stage01" },
            new Target { PrefabName = "MapInstanceLv2",  Collection = "Talk_Stage02" },
            new Target { PrefabName = "MapInstanceLv5",  Collection = "Talk_Stage05" },
            new Target { PrefabName = "MapInstanceLv6",  Collection = "Talk_Stage06" },
            new Target { PrefabName = "MapInstanceLv8",  Collection = "Talk_Stage08" },
            new Target { PrefabName = "MapInstanceLv9",  Collection = "Talk_Stage09" },
            new Target { PrefabName = "MapInstanceLv11", Collection = "Talk_Stage11" },
            new Target { PrefabName = "MapInstanceLv12", Collection = "Talk_Stage12" },
        };

        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [MenuItem("Tools/Localization/Auto-Fill Talk Localization (Dry Run)")]
        public static void DryRun() => Run(apply: false);

        [MenuItem("Tools/Localization/Auto-Fill Talk Localization (Apply)")]
        public static void Apply()
        {
            bool ok = EditorUtility.DisplayDialog(
                "Auto-Fill Talk Localization",
                "対象8プレハブの ShowTalk / ShowDialogue / Choice に、フォールバック日本語と一致する Table/Key を流し込みます。\n" +
                "・既に Key が入っているものはスキップ\n" +
                "・話者名(speakerName)は対象外\n\n実行しますか？（先に Dry Run での確認を推奨）",
                "実行", "キャンセル");
            if (ok)
                Run(apply: true);
        }

        private static void Run(bool apply)
        {
            var report = new StringBuilder();
            report.AppendLine(apply ? "=== Talk Localization Auto-Fill (APPLY) ===" : "=== Talk Localization Auto-Fill (DRY RUN) ===");

            int grandMatched = 0, grandSkipped = 0, grandUnmatched = 0;

            foreach (Target target in Targets)
            {
                string prefabPath = FindPrefabPath(target.PrefabName);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    report.AppendLine($"[SKIP] {target.PrefabName}: prefab が見つかりません。");
                    continue;
                }

                StringTableCollection collection = FindCollection(target.Collection);
                if (collection == null)
                {
                    report.AppendLine($"[SKIP] {target.PrefabName}: String Table collection '{target.Collection}' が見つかりません。");
                    continue;
                }

                var matcher = new JaMatcher(collection);
                if (matcher.EntryCount == 0)
                {
                    report.AppendLine($"[WARN] {target.PrefabName}: '{target.Collection}' に ja エントリがありません。");
                }

                var ctx = new Context
                {
                    Matcher = matcher,
                    Collection = collection,
                    Apply = apply,
                    Report = report,
                };

                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (MonoBehaviour mb in behaviours)
                    {
                        if (mb == null)
                            continue;

                        string typeName = mb.GetType().Name;
                        if (typeName == "NPCObjectMB")
                        {
                            var interaction = GetFieldValue(mb, "interactionAction") as InlineAction;
                            ctx.OwnerName = mb.gameObject.name;
                            WalkInlineAction(interaction, ctx);
                        }
                        else if (typeName == "TutorialStageAuthoringMB")
                        {
                            if (GetFieldValue(mb, "steps") is IEnumerable steps)
                            {
                                ctx.OwnerName = mb.gameObject.name;
                                foreach (object tutorialStep in steps)
                                {
                                    if (tutorialStep == null)
                                        continue;

                                    WalkInlineAction(GetFieldValue(tutorialStep, "onEnter") as InlineAction, ctx);
                                    WalkInlineAction(GetFieldValue(tutorialStep, "onComplete") as InlineAction, ctx);
                                }
                            }
                        }
                    }

                    if (apply && ctx.Changed)
                        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                report.AppendLine(
                    $"[{target.PrefabName}] table={target.Collection} matched={ctx.Matched} skipped(existing)={ctx.Skipped} unmatched={ctx.Unmatched.Count}{(apply && ctx.Changed ? " (saved)" : string.Empty)}");

                for (int i = 0; i < ctx.Unmatched.Count; i++)
                    report.AppendLine($"    未一致: \"{Shorten(ctx.Unmatched[i])}\"");

                grandMatched += ctx.Matched;
                grandSkipped += ctx.Skipped;
                grandUnmatched += ctx.Unmatched.Count;
            }

            report.AppendLine($"--- 合計 matched={grandMatched} skipped={grandSkipped} unmatched={grandUnmatched} ---");
            if (!apply)
                report.AppendLine("Dry Run のため保存していません。Apply メニューで実際に流し込みます。");

            Debug.Log(report.ToString());
            if (apply)
                AssetDatabase.SaveAssets();
        }

        // ── walker ───────────────────────────────────────────────

        private static void WalkInlineAction(InlineAction action, Context ctx)
        {
            if (action == null)
                return;

            if (!ctx.Visited.Add(action))
                return;

            if (GetFieldValue(action, "_steps") is not IList steps)
                return;

            foreach (object step in steps)
            {
                if (step == null)
                    continue;

                switch (step)
                {
                    case ShowDialogueStepAuthoring _:
                        ProcessRequest(step, "dialogueRequestData", "onStartDialogueAction", "onCompleteDialogueAction", ctx);
                        break;

                    case ShowTalkStepAuthoring _:
                        ProcessRequest(step, "talkRequestData", "onStartTalkAction", "onCompleteTalkAction", ctx);
                        break;

                    case ShowTalkChoiceStepAuthoring _:
                        ProcessChoice(step, ctx);
                        break;

                    case IfStepAuthoring _:
                        WalkInlineAction(GetFieldValue(step, "whenTrue") as InlineAction, ctx);
                        WalkInlineAction(GetFieldValue(step, "whenFalse") as InlineAction, ctx);
                        break;

                    case SubActionStepAuthoring _:
                        WalkInlineAction(GetFieldValue(step, "action") as InlineAction, ctx);
                        break;
                }
            }
        }

        // ShowDialogue / ShowTalk の本文テキストを処理する（struct フィールド）。
        private static void ProcessRequest(object step, string requestFieldName, string onStartName, string onCompleteName, Context ctx)
        {
            FieldInfo requestField = step.GetType().GetField(requestFieldName, FieldFlags);
            if (requestField == null)
                return;

            object boxed = requestField.GetValue(step); // boxed TalkRequestData / DialogueRequestData
            if (boxed == null)
                return;

            Type structType = boxed.GetType();
            string fallback = structType.GetField("dialogueText", FieldFlags)?.GetValue(boxed) as string;
            string existingEntry = structType.GetField("entry", FieldFlags)?.GetValue(boxed) as string;

            if (string.IsNullOrEmpty(existingEntry))
            {
                if (!string.IsNullOrWhiteSpace(fallback) && ctx.Matcher.TryMatch(fallback, out string key))
                {
                    if (ctx.Apply)
                    {
                        structType.GetField("table", FieldFlags)?.SetValue(boxed, ctx.MakeLocalizedStringTable());
                        structType.GetField("entry", FieldFlags)?.SetValue(boxed, key);
                        structType.GetField("applySetTable", FieldFlags)?.SetValue(boxed, true);
                        requestField.SetValue(step, boxed); // struct を書き戻す
                        ctx.Changed = true;
                    }
                    ctx.Matched++;
                }
                else if (!string.IsNullOrWhiteSpace(fallback))
                {
                    ctx.Unmatched.Add(fallback);
                }
            }
            else
            {
                ctx.Skipped++;
            }

            // ネストした InlineAction（開始/終了アクション）も辿る。
            WalkInlineAction(structType.GetField(onStartName, FieldFlags)?.GetValue(boxed) as InlineAction, ctx);
            WalkInlineAction(structType.GetField(onCompleteName, FieldFlags)?.GetValue(boxed) as InlineAction, ctx);
        }

        // Choice の各オプション（displayText）を処理する（class フィールド）。
        private static void ProcessChoice(object step, Context ctx)
        {
            if (GetFieldValue(step, "options") is not Array options)
                return;

            foreach (object option in options)
            {
                if (option == null)
                    continue;

                Type optType = option.GetType();
                string fallback = optType.GetField("displayText", FieldFlags)?.GetValue(option) as string;
                string existingEntry = optType.GetField("entry", FieldFlags)?.GetValue(option) as string;

                if (string.IsNullOrEmpty(existingEntry))
                {
                    if (!string.IsNullOrWhiteSpace(fallback) && ctx.Matcher.TryMatch(fallback, out string key))
                    {
                        if (ctx.Apply)
                        {
                            optType.GetField("table", FieldFlags)?.SetValue(option, ctx.MakeLocalizedStringTable());
                            optType.GetField("entry", FieldFlags)?.SetValue(option, key);
                            optType.GetField("applySetTable", FieldFlags)?.SetValue(option, true);
                            ctx.Changed = true;
                        }
                        ctx.Matched++;
                    }
                    else if (!string.IsNullOrWhiteSpace(fallback))
                    {
                        ctx.Unmatched.Add(fallback);
                    }
                }
                else
                {
                    ctx.Skipped++;
                }

                // 選択肢の outcome InlineAction も辿る。
                WalkInlineAction(optType.GetField("inlineAction", FieldFlags)?.GetValue(option) as InlineAction, ctx);
            }
        }

        // ── helpers ──────────────────────────────────────────────

        private static object GetFieldValue(object obj, string fieldName)
        {
            if (obj == null)
                return null;

            FieldInfo field = obj.GetType().GetField(fieldName, FieldFlags);
            return field?.GetValue(obj);
        }

        private static string FindPrefabPath(string prefabName)
        {
            string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == prefabName)
                    return path;
            }

            return null;
        }

        private static StringTableCollection FindCollection(string collectionName)
        {
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                if (collection is StringTableCollection stringCollection &&
                    stringCollection.TableCollectionName == collectionName)
                    return stringCollection;
            }

            return null;
        }

        private static string Shorten(string text)
        {
            string single = text.Replace("\r", " ").Replace("\n", " ").Trim();
            return single.Length > 40 ? single.Substring(0, 40) + "…" : single;
        }

        // ── context ──────────────────────────────────────────────

        private sealed class Context
        {
            public JaMatcher Matcher;
            public StringTableCollection Collection;
            public bool Apply;
            public StringBuilder Report;
            public string OwnerName;

            public bool Changed;
            public int Matched;
            public int Skipped;
            public readonly List<string> Unmatched = new();
            public readonly HashSet<object> Visited = new();

            public LocalizedStringTable MakeLocalizedStringTable()
            {
                return new LocalizedStringTable { TableReference = Collection.TableCollectionNameReference };
            }
        }

        // ── ja マッチャ ───────────────────────────────────────────

        private sealed class JaMatcher
        {
            private readonly Dictionary<string, List<string>> _exact = new();
            private readonly Dictionary<string, List<string>> _loose = new();
            private readonly HashSet<string> _usedKeys = new();

            public int EntryCount { get; }

            public JaMatcher(StringTableCollection collection)
            {
                var jaTable = collection.GetTable(new LocaleIdentifier("ja")) as StringTable;
                SharedTableData shared = collection.SharedData;
                if (shared == null)
                    return;

                foreach (SharedTableData.SharedTableEntry entry in shared.Entries)
                {
                    if (entry == null)
                        continue;

                    string ja = jaTable?.GetEntry(entry.Id)?.Value;
                    if (string.IsNullOrEmpty(ja))
                        continue;

                    EntryCount++;
                    Add(_exact, Normalize(ja), entry.Key);
                    Add(_loose, NormalizeLoose(ja), entry.Key);
                }
            }

            public bool TryMatch(string fallback, out string key)
            {
                key = null;
                if (string.IsNullOrWhiteSpace(fallback))
                    return false;

                return Pick(_exact, Normalize(fallback), out key)
                       || Pick(_loose, NormalizeLoose(fallback), out key);
            }

            private bool Pick(Dictionary<string, List<string>> map, string normalized, out string key)
            {
                key = null;
                if (string.IsNullOrEmpty(normalized) || !map.TryGetValue(normalized, out List<string> keys) || keys.Count == 0)
                    return false;

                // 同一日本語が複数キーに存在する場合は、まだ使っていないキーを優先的に割り当てる。
                for (int i = 0; i < keys.Count; i++)
                {
                    if (_usedKeys.Add(keys[i]))
                    {
                        key = keys[i];
                        return true;
                    }
                }

                key = keys[keys.Count - 1];
                return true;
            }

            private static void Add(Dictionary<string, List<string>> map, string normalized, string key)
            {
                if (string.IsNullOrEmpty(normalized))
                    return;

                if (!map.TryGetValue(normalized, out List<string> keys))
                {
                    keys = new List<string>();
                    map[normalized] = keys;
                }

                if (!keys.Contains(key))
                    keys.Add(key);
            }

            private static string Normalize(string value)
            {
                if (string.IsNullOrEmpty(value))
                    return string.Empty;

                return value.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
            }

            private static string NormalizeLoose(string value)
            {
                return Regex.Replace(Normalize(value), @"\s+", string.Empty);
            }
        }
    }
}

using System.Collections.Generic;
using BC.Gimmick.MovingPlatform;
using UnityEditor;
using UnityEngine;

namespace BC.Editor.Gimmick.MovingPlatformTools
{
    internal static class MovingPlatformTreeMigrationCommands
    {
        private const string Level1PrefabFolder = "Assets/Art/Prefab/Map/Level1";

        [MenuItem("Tools/BombCourier/MovingPlatform/Migrate Level1 Prefabs")]
        private static void MigrateLevel1Prefabs()
        {
            // この操作は prefab を直接書き換えて保存し、Undo できない。誤クリックでの
            // 一括書き換えを防ぐため、実行前に明示的な確認を取る。
            // (既に migration 済みの platform は TryApplyLegacyMigration 側で上書き拒否されるため、
            //  再実行しても hand-authored tree は破壊されない。)
            if (!EditorUtility.DisplayDialog(
                "MovingPlatform Migration",
                $"'{Level1PrefabFolder}' 配下の全 prefab を走査し、未 migration の MovingPlatform を tree authoring へ変換して上書き保存します。\nこの操作は Undo できません。続行しますか？",
                "実行",
                "キャンセル"))
            {
                return;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { Level1PrefabFolder });
            var failures = new List<string>();
            int migratedPrefabCount = 0;

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (string.IsNullOrWhiteSpace(prefabPath))
                    continue;

                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefabRoot == null)
                {
                    failures.Add($"{prefabPath}: failed to load prefab contents");
                    continue;
                }

                try
                {
                    bool prefabChanged = false;
                    MovingPlatformMB[] movingPlatforms = prefabRoot.GetComponentsInChildren<MovingPlatformMB>(true);
                    for (int platformIndex = 0; platformIndex < movingPlatforms.Length; platformIndex++)
                    {
                        MovingPlatformMB movingPlatform = movingPlatforms[platformIndex];
                        if (movingPlatform == null)
                            continue;

                        if (!movingPlatform.TryApplyLegacyMigration(out string failureReason))
                        {
                            failures.Add($"{prefabPath}::{movingPlatform.name}: {failureReason}");
                            continue;
                        }

                        EditorUtility.SetDirty(movingPlatform);
                        prefabChanged = true;
                    }

                    if (!prefabChanged)
                        continue;

                    // 保存失敗(ロック/読み取り専用等)を握りつぶさず、失敗として記録する。
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath, out bool savedSuccessfully);
                    if (!savedSuccessfully)
                    {
                        failures.Add($"{prefabPath}: SaveAsPrefabAsset failed");
                        continue;
                    }

                    migratedPrefabCount++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            AssetDatabase.SaveAssets();

            if (failures.Count > 0)
            {
                Debug.LogWarning(
                    $"MovingPlatform tree migration completed with failures. Migrated prefabs: {migratedPrefabCount}\n" +
                    string.Join("\n", failures));
                EditorUtility.DisplayDialog(
                    "MovingPlatform Migration",
                    $"Migrated prefabs: {migratedPrefabCount}\nFailures: {failures.Count}\nSee Console for details.",
                    "Close");
                return;
            }

            EditorUtility.DisplayDialog(
                "MovingPlatform Migration",
                $"Migrated prefabs: {migratedPrefabCount}",
                "Close");
        }
    }
}

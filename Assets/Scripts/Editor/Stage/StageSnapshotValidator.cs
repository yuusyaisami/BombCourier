#if UNITY_EDITOR
using BC.Stage.Snapshot;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BC.EditorTools.Stage
{
    /// <summary>
    /// スナップショット対象の取りこぼしを構造的に検出するエディタ検証。
    /// - 独自状態を持つギミック（IStageStateRestorable）なのに StageRestorableMB が無いオブジェクト
    /// - 安定ID(stableId)が未採番の StageRestorableMB
    /// を警告する（BreakableGate のような「マーク忘れで無言非対象」を防ぐ）。
    /// </summary>
    public static class StageSnapshotValidator
    {
        [MenuItem("Tools/Stage Snapshot/Validate Prefabs + Open Scenes")]
        public static void Validate()
        {
            int issues = 0;

            // --- プレハブアセット（読み取り専用で検査） ---
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (string.IsNullOrEmpty(path) || path.StartsWith("Packages/"))
                    continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    issues += ValidateHierarchy(prefab, $"Prefab: {path}");
            }

            // --- 現在開いているシーン ---
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                    issues += ValidateHierarchy(roots[r], $"Scene: {scene.name}");
            }

            if (issues == 0)
                Debug.Log("Stage Snapshot validation: 問題は見つかりませんでした。");
            else
                Debug.LogWarning($"Stage Snapshot validation: {issues} 件の問題が見つかりました（上のログ参照）。");
        }

        private static int ValidateHierarchy(GameObject root, string context)
        {
            int issues = 0;

            // 1) IStageStateRestorable を持つが StageRestorableMB が無いオブジェクト。
            IStageStateRestorable[] participants = root.GetComponentsInChildren<IStageStateRestorable>(true);
            for (int i = 0; i < participants.Length; i++)
            {
                if (!(participants[i] is Component component) || component == null)
                    continue;

                if (component.GetComponent<StageRestorableMB>() == null)
                {
                    issues++;
                    Debug.LogWarning(
                        $"[StageSnapshot] {context}: '{GetPath(component.transform)}' は独自状態({component.GetType().Name})を持つが " +
                        $"{nameof(StageRestorableMB)} がありません。リロードで復元されません。マークを付与してください。",
                        component);
                }
            }

            // 2) 安定ID 未採番の StageRestorableMB。
            StageRestorableMB[] restorables = root.GetComponentsInChildren<StageRestorableMB>(true);
            for (int i = 0; i < restorables.Length; i++)
            {
                StageRestorableMB restorable = restorables[i];
                if (restorable != null && string.IsNullOrEmpty(restorable.RawId))
                {
                    issues++;
                    Debug.LogWarning(
                        $"[StageSnapshot] {context}: '{GetPath(restorable.transform)}' の {nameof(StageRestorableMB)} に stableId がありません。" +
                        " 移行メニュー実行、またはオブジェクト選択でエディタが自動採番します。",
                        restorable);
                }
            }

            return issues;
        }

        private static string GetPath(Transform transform)
        {
            if (transform == null)
                return "<null>";

            string path = transform.name;
            Transform cursor = transform.parent;
            while (cursor != null)
            {
                path = cursor.name + "/" + path;
                cursor = cursor.parent;
            }

            return path;
        }
    }
}
#endif

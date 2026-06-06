#if UNITY_EDITOR
using System.Collections.Generic;
using BC.Stage;
using BC.Stage.Snapshot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BC.EditorTools.Stage
{
    /// <summary>
    /// 旧 <see cref="StageSaveMarkMB"/> を新 <see cref="StageRestorableMB"/> へ移行するエディタ専用ツール。
    /// フラグを複写し、安定IDを採番し、独自状態を持つギミック（BreakableGate 等）にもマークを付与する。
    /// 一度実行すれば良い。実行後は旧型2本と本ツールを削除してよい。
    /// </summary>
    public static class StageSnapshotMigrationTool
    {
        [MenuItem("Tools/Stage Snapshot/Migrate StageSaveMark -> StageRestorable")]
        public static void Migrate()
        {
            int convertedPrefabs = 0;
            int convertedScenes = 0;

            // --- プレハブアセット ---
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            try
            {
                for (int i = 0; i < prefabGuids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                    if (string.IsNullOrEmpty(path) || path.StartsWith("Packages/"))
                        continue;

                    EditorUtility.DisplayProgressBar("Stage Snapshot Migration", path, (float)i / Mathf.Max(1, prefabGuids.Length));

                    GameObject root = PrefabUtility.LoadPrefabContents(path);
                    bool changed = ConvertHierarchy(root);
                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        convertedPrefabs++;
                    }

                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // --- 現在開いているシーン ---
            bool anySceneDirty = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                bool sceneChanged = false;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                    sceneChanged |= ConvertHierarchy(roots[r]);

                if (sceneChanged)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    anySceneDirty = true;
                    convertedScenes++;
                }
            }

            if (anySceneDirty)
                EditorSceneManager.SaveOpenScenes();

            AssetDatabase.SaveAssets();
            Debug.Log($"Stage Snapshot migration: {convertedPrefabs} prefab(s), {convertedScenes} open scene(s) を変換しました。" +
                      " 検証メニューで漏れ/重複を確認してください。");
        }

        // 1階層を変換: 旧マーク→新マーク、独自状態ギミックへの付与、ID採番。変更があれば true。
        private static bool ConvertHierarchy(GameObject root)
        {
            bool changed = false;

            // 1) 旧 StageSaveMarkMB を StageRestorableMB へ変換。
            StageSaveMarkMB[] oldMarks = root.GetComponentsInChildren<StageSaveMarkMB>(true);
            for (int i = 0; i < oldMarks.Length; i++)
            {
                StageSaveMarkMB oldMark = oldMarks[i];
                if (oldMark == null)
                    continue;

                GameObject go = oldMark.gameObject;
                StageRestorableMB restorable = go.GetComponent<StageRestorableMB>();
                if (restorable == null)
                    restorable = go.AddComponent<StageRestorableMB>();

                CopyFlags(oldMark, restorable);
                EnsureId(restorable);
                Object.DestroyImmediate(oldMark);
                changed = true;
            }

            // 2) 独自状態を持つギミック（BreakableGate 等）にマークが無ければ付与。
            IStageStateRestorable[] participants = root.GetComponentsInChildren<IStageStateRestorable>(true);
            for (int i = 0; i < participants.Length; i++)
            {
                if (!(participants[i] is Component component) || component == null)
                    continue;

                GameObject go = component.gameObject;
                StageRestorableMB restorable = go.GetComponent<StageRestorableMB>();
                if (restorable == null)
                {
                    restorable = go.AddComponent<StageRestorableMB>();
                    changed = true;
                }

                if (EnsureId(restorable))
                    changed = true;
            }

            // 3) 既存の StageRestorableMB で ID 未採番のものを採番。
            StageRestorableMB[] restorables = root.GetComponentsInChildren<StageRestorableMB>(true);
            for (int i = 0; i < restorables.Length; i++)
            {
                if (EnsureId(restorables[i]))
                    changed = true;
            }

            return changed;
        }

        private static void CopyFlags(StageSaveMarkMB oldMark, StageRestorableMB newMark)
        {
            var oldSo = new SerializedObject(oldMark);
            var newSo = new SerializedObject(newMark);

            SetBool(newSo, "excludeFromSnapshot", GetBool(oldSo, "excludeFromCheckpoint"));
            SetBool(newSo, "saveActiveSelf", GetBool(oldSo, "saveActiveSelf"));
            SetBool(newSo, "saveTransform", GetBool(oldSo, "saveTransform"));
            SetBool(newSo, "saveRigidbody", GetBool(oldSo, "saveRigidbody"));

            newSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // 安定IDが空なら採番する。採番したら true。
        private static bool EnsureId(StageRestorableMB restorable)
        {
            if (restorable == null)
                return false;

            var so = new SerializedObject(restorable);
            SerializedProperty idProp = so.FindProperty("stableId");
            if (idProp == null)
                return false;

            if (string.IsNullOrEmpty(idProp.stringValue))
            {
                idProp.stringValue = System.Guid.NewGuid().ToString("N");
                so.ApplyModifiedPropertiesWithoutUndo();
                return true;
            }

            return false;
        }

        private static bool GetBool(SerializedObject so, string propertyName)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            return prop != null && prop.boolValue;
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null)
                prop.boolValue = value;
        }
    }
}
#endif

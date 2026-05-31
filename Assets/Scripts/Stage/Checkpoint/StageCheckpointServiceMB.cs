using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace BC.Stage
{
    /// <summary>
    /// 爆弾運搬開始前の一時チェックポイントを管理する。
    /// SaveMark が付いた対象だけを保存・復元する。
    /// </summary>
    public sealed class StageCheckpointServiceMB : MonoBehaviour
    {
        [SerializeField] private Transform stageRoot;

        private StageCheckpointSnapshot latestSnapshot;

        public bool HasCheckpoint => latestSnapshot.IsValid;

        private Transform Root => stageRoot != null ? stageRoot : transform;

        public void Capture()
        {
            latestSnapshot = CaptureSnapshot();
        }

        public StageCheckpointSnapshot CaptureSnapshot()
        {
            StageSaveMarkMB[] marks = CollectMarksInScene();
            var snapshotEntries = new List<StageObjectSnapshot>(marks.Length);

            for (int i = 0; i < marks.Length; i++)
            {
                StageSaveMarkMB mark = marks[i];

                if (mark == null || mark.ExcludeFromCheckpoint)
                    continue;

                snapshotEntries.Add(mark.Capture());
            }

            return new StageCheckpointSnapshot(snapshotEntries.ToArray());
        }

        private StageSaveMarkMB[] CollectMarksInScene()
        {
            Scene targetScene = Root.gameObject.scene;
            if (!targetScene.IsValid())
                return System.Array.Empty<StageSaveMarkMB>();

            GameObject[] rootObjects = targetScene.GetRootGameObjects();
            var marks = new List<StageSaveMarkMB>(32);
            var seen = new HashSet<StageSaveMarkMB>();

            for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
            {
                StageSaveMarkMB[] rootMarks = rootObjects[rootIndex].GetComponentsInChildren<StageSaveMarkMB>(true);
                for (int markIndex = 0; markIndex < rootMarks.Length; markIndex++)
                {
                    StageSaveMarkMB mark = rootMarks[markIndex];
                    if (mark == null || !seen.Add(mark))
                        continue;

                    marks.Add(mark);
                }
            }

            return marks.ToArray();
        }

        public void Restore()
        {
            RestoreSnapshot(latestSnapshot);
        }

        public void RestoreSnapshot(StageCheckpointSnapshot snapshot)
        {
            if (!snapshot.IsValid)
            {
                Debug.LogError($"{nameof(StageCheckpointServiceMB)}: checkpoint does not exist.", this);
                return;
            }

            // CharacterController は transform ワープと相性が悪いので一時的に止める。
            var disabledControllers = new List<CharacterController>();
            IReadOnlyList<StageObjectSnapshot> snapshotEntries = snapshot.Entries;

            for (int i = 0; i < snapshotEntries.Count; i++)
            {
                var target = snapshotEntries[i].Target;

                if (target != null && target.TryGetComponent(out CharacterController controller) && controller.enabled)
                {
                    controller.enabled = false;
                    disabledControllers.Add(controller);
                }
            }

            for (int i = 0; i < snapshotEntries.Count; i++)
            {
                StageObjectSnapshot snapshotEntry = snapshotEntries[i];

                if (snapshotEntry.Target == null)
                    continue;

                snapshotEntry.Target.Restore(snapshotEntry);
            }

            for (int i = 0; i < disabledControllers.Count; i++)
            {
                if (disabledControllers[i] != null)
                    disabledControllers[i].enabled = true;
            }
        }

        public void Clear()
        {
            latestSnapshot = default;
        }
    }

    public readonly struct StageCheckpointSnapshot
    {
        private readonly StageObjectSnapshot[] entries;

        internal StageCheckpointSnapshot(StageObjectSnapshot[] entries)
        {
            this.entries = entries ?? System.Array.Empty<StageObjectSnapshot>();
        }

        public bool IsValid => entries != null;
        internal IReadOnlyList<StageObjectSnapshot> Entries => entries ?? System.Array.Empty<StageObjectSnapshot>();
    }
}

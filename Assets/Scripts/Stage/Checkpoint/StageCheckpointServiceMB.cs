using System.Collections.Generic;
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

        private readonly List<StageObjectSnapshot> latestSnapshot = new();

        public bool HasCheckpoint => latestSnapshot.Count > 0;

        private Transform Root => stageRoot != null ? stageRoot : transform;

        public void Capture()
        {
            latestSnapshot.Clear();

            StageSaveMarkMB[] marks = Root.GetComponentsInChildren<StageSaveMarkMB>(true);

            for (int i = 0; i < marks.Length; i++)
            {
                StageSaveMarkMB mark = marks[i];

                if (mark == null || mark.ExcludeFromCheckpoint)
                    continue;

                latestSnapshot.Add(mark.Capture());
            }
        }

        public void Restore()
        {
            if (!HasCheckpoint)
            {
                Debug.LogError($"{nameof(StageCheckpointServiceMB)}: checkpoint does not exist.", this);
                return;
            }

            // CharacterController は transform ワープと相性が悪いので一時的に止める。
            var disabledControllers = new List<CharacterController>();

            for (int i = 0; i < latestSnapshot.Count; i++)
            {
                var target = latestSnapshot[i].Target;

                if (target != null && target.TryGetComponent(out CharacterController controller) && controller.enabled)
                {
                    controller.enabled = false;
                    disabledControllers.Add(controller);
                }
            }

            for (int i = 0; i < latestSnapshot.Count; i++)
            {
                StageObjectSnapshot snapshot = latestSnapshot[i];

                if (snapshot.Target == null)
                    continue;

                snapshot.Target.Restore(snapshot);
            }

            for (int i = 0; i < disabledControllers.Count; i++)
            {
                if (disabledControllers[i] != null)
                    disabledControllers[i].enabled = true;
            }
        }

        public void Clear()
        {
            latestSnapshot.Clear();
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace BC.Stage.Snapshot
{
    /// <summary>
    /// 「ステージ開始時状態」ベースラインを1つ保持し、Reload時にそこへ復元するサービス。
    /// 列挙は <see cref="StageRestorableRegistry"/> 由来（シーンスキャン/stageRoot依存なし）。
    /// 旧 StageCheckpointServiceMB の後継。
    /// </summary>
    public sealed class StageSnapshotServiceMB : MonoBehaviour
    {
        private StageStartSnapshot baseline;

        public bool HasBaseline => baseline.IsValid;

        /// <summary>登録済み全対象から開始ベースラインを取得する（ステージ毎に1回）。</summary>
        public void CaptureBaseline()
        {
            IReadOnlyList<StageRestorableMB> active = StageRestorableRegistry.OrderedActive;
            var entries = new List<RestorableSnapshot>(active.Count);

            for (int i = 0; i < active.Count; i++)
            {
                StageRestorableMB restorable = active[i];
                if (restorable == null || restorable.ExcludeFromSnapshot)
                    continue;

                entries.Add(restorable.CaptureSnapshot());
            }

            baseline = new StageStartSnapshot(entries.ToArray());
        }

        public void ClearBaseline()
        {
            baseline = default;
        }

        /// <summary>開始ベースラインへ全対象を復元する。</summary>
        public void RestoreBaseline()
        {
            if (!baseline.IsValid)
            {
                Debug.LogWarning($"{nameof(StageSnapshotServiceMB)}: 開始ベースラインが未取得のため復元をスキップします。", this);
                return;
            }

            IReadOnlyList<RestorableSnapshot> entries = baseline.Entries;
            var disabledControllers = new List<CharacterController>(8);

            // --- パス1: 再活性化 → CharacterController停止 → kinematic固定/速度0 → 親復元 → ワールド姿勢 ---
            for (int i = 0; i < entries.Count; i++)
            {
                RestorableSnapshot entry = entries[i];
                StageRestorableMB target = entry.Target;
                if (target == null)
                {
                    Debug.LogWarning(
                        $"StageSnapshot: 復元漏れ id='{entry.Key}' (capture時 '{entry.DebugPath}') — 破棄済みか未再登録のため復元できません。",
                        this);
                    continue;
                }

                GameObject go = target.gameObject;

                // OnEnable の再初期化を先に走らせるため、参加者復元より前に活性化する。
                if (entry.SaveActiveSelf && entry.ActiveSelf && !go.activeSelf)
                    go.SetActive(true);

                if (go.TryGetComponent(out CharacterController controller) && controller.enabled)
                {
                    controller.enabled = false;
                    disabledControllers.Add(controller);
                }

                Rigidbody rb = ResolveRigidbody(entry, go);
                if (rb != null)
                {
                    // teleport で物理が暴れないよう、移動前に kinematic 固定＋速度ゼロ化。
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                if (entry.SaveTransform)
                {
                    Transform t = target.transform;
                    if (t.parent != entry.Parent)
                        t.SetParent(entry.Parent, true);

                    if (rb != null)
                    {
                        rb.position = entry.WorldPosition;
                        rb.rotation = entry.WorldRotation;
                    }
                    else
                    {
                        t.SetPositionAndRotation(entry.WorldPosition, entry.WorldRotation);
                    }

                    t.localScale = entry.LocalScale;
                }
            }

            // 全姿勢書き込み後に一括で物理同期（per-object にしない）。
            Physics.SyncTransforms();

            // --- パス2: RBフラグ/速度/sleep 復元 → 参加者状態復元 → 非アクティブ化 ---
            for (int i = 0; i < entries.Count; i++)
            {
                RestorableSnapshot entry = entries[i];
                StageRestorableMB target = entry.Target;
                if (target == null)
                    continue;

                GameObject go = target.gameObject;

                Rigidbody rb = ResolveRigidbody(entry, go);
                if (rb != null)
                {
                    rb.isKinematic = entry.IsKinematic;
                    rb.useGravity = entry.UseGravity;
                    rb.detectCollisions = entry.DetectCollisions;

                    if (!entry.IsKinematic)
                    {
                        rb.linearVelocity = entry.LinearVelocity;
                        rb.angularVelocity = entry.AngularVelocity;
                    }
                    else
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    if (entry.IsSleeping || entry.IsKinematic)
                        rb.Sleep();
                    else
                        rb.WakeUp();
                }

                if (entry.ParticipantStates.Length > 0)
                {
                    IStageStateRestorable[] participants = go.GetComponents<IStageStateRestorable>();
                    int count = Mathf.Min(participants.Length, entry.ParticipantStates.Length);
                    for (int p = 0; p < count; p++)
                        participants[p].RestoreStageState(entry.ParticipantStates[p]);
                }

                if (entry.SaveActiveSelf && !entry.ActiveSelf && go.activeSelf)
                    go.SetActive(false);
            }

            for (int i = 0; i < disabledControllers.Count; i++)
            {
                if (disabledControllers[i] != null)
                    disabledControllers[i].enabled = true;
            }

            Physics.SyncTransforms();

            DespawnNotInBaseline(entries);
        }

        private static Rigidbody ResolveRigidbody(in RestorableSnapshot entry, GameObject go)
        {
            if (!entry.SaveRigidbody || !entry.HasRigidbody)
                return null;

            return go.TryGetComponent(out Rigidbody rb) ? rb : null;
        }

        // ベースライン非掲載（取得後に生成された）対象は、開始状態へ決定的に戻すため破棄する。
        private void DespawnNotInBaseline(IReadOnlyList<RestorableSnapshot> entries)
        {
            var baselineKeys = new HashSet<string>(System.StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                if (!string.IsNullOrEmpty(entries[i].Key))
                    baselineKeys.Add(entries[i].Key);
            }

            // 安全装置: ベースラインが空（＝未捕捉の疑い）の場合、全アクティブ対象を破棄しない。
            if (baselineKeys.Count == 0)
                return;

            // Destroy が OnDestroy 経由でレジストリを変更するため、コピーを反復する。
            var current = new List<StageRestorableMB>(StageRestorableRegistry.OrderedActive);
            for (int i = 0; i < current.Count; i++)
            {
                StageRestorableMB restorable = current[i];
                if (restorable == null || restorable.PersistAcrossRestore)
                    continue;

                if (!baselineKeys.Contains(restorable.RuntimeKey))
                {
                    Debug.Log(
                        $"StageSnapshot: ベースライン非掲載のため破棄 '{StageRestorableRegistry.GetHierarchyPath(restorable.transform)}'。",
                        this);
                    Destroy(restorable.gameObject);
                }
            }
        }
    }

    /// <summary>取得済みの開始ベースライン（不変）。</summary>
    public readonly struct StageStartSnapshot
    {
        private readonly RestorableSnapshot[] entries;

        internal StageStartSnapshot(RestorableSnapshot[] entries)
        {
            this.entries = entries ?? System.Array.Empty<RestorableSnapshot>();
        }

        public bool IsValid => entries != null;

        internal IReadOnlyList<RestorableSnapshot> Entries => entries ?? System.Array.Empty<RestorableSnapshot>();
    }
}

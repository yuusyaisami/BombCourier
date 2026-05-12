using System;
using System.Collections.Generic;
using UnityEngine;

namespace BC.Stage
{
    /// <summary>
    /// 爆弾運搬中の一時チェックポイント対象にするためのマーカー。
    /// これは通常の永続セーブではなく、ステージ内メモリ復元用。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageSaveMarkMB : MonoBehaviour
    {
        [Header("Save Target")]
        [SerializeField] private bool excludeFromCheckpoint;
        [SerializeField] private bool saveActiveSelf = true;
        [SerializeField] private bool saveTransform = true;
        [SerializeField] private bool saveRigidbody = true;

        public bool ExcludeFromCheckpoint => excludeFromCheckpoint;

        internal StageObjectSnapshot Capture()
        {
            var rb = saveRigidbody ? GetComponent<Rigidbody>() : null;

            var participantSnapshots = new List<StageParticipantSnapshot>();
            var participants = GetComponents<IStageCheckpointParticipant>();

            for (int i = 0; i < participants.Length; i++)
            {
                participantSnapshots.Add(new StageParticipantSnapshot(
                    participants[i],
                    participants[i].CaptureCheckpointState()
                ));
            }

            return new StageObjectSnapshot(
                this,
                gameObject.activeSelf,
                transform.localPosition,
                transform.localRotation,
                transform.localScale,
                rb != null,
                rb != null ? rb.linearVelocity : Vector3.zero,
                rb != null ? rb.angularVelocity : Vector3.zero,
                participantSnapshots.ToArray()
            );
        }

        internal void Restore(StageObjectSnapshot snapshot)
        {
            Rigidbody rb = saveRigidbody && snapshot.HasRigidbody && TryGetComponent(out Rigidbody cachedRigidbody)
                ? cachedRigidbody
                : null;

            if (saveTransform)
            {
                if (rb != null)
                {
                    Transform parent = transform.parent;
                    Vector3 worldPosition = parent != null
                        ? parent.TransformPoint(snapshot.LocalPosition)
                        : snapshot.LocalPosition;

                    Quaternion worldRotation = parent != null
                        ? parent.rotation * snapshot.LocalRotation
                        : snapshot.LocalRotation;

                    rb.position = worldPosition;
                    rb.rotation = worldRotation;
                }
                else
                {
                    transform.localPosition = snapshot.LocalPosition;
                    transform.localRotation = snapshot.LocalRotation;
                }

                transform.localScale = snapshot.LocalScale;
            }

            if (rb != null)
            {
                rb.linearVelocity = snapshot.LinearVelocity;
                rb.angularVelocity = snapshot.AngularVelocity;
                rb.Sleep();
            }

            for (int i = 0; i < snapshot.ParticipantSnapshots.Length; i++)
            {
                var participantSnapshot = snapshot.ParticipantSnapshots[i];

                if (participantSnapshot.Target != null)
                {
                    participantSnapshot.Target.RestoreCheckpointState(participantSnapshot.State);
                }
            }

            if (saveActiveSelf)
            {
                gameObject.SetActive(snapshot.ActiveSelf);
            }
        }
    }

    /// <summary>
    /// 個別ギミックが独自状態を保存したい場合に実装する。
    /// 例: スイッチON/OFF、扉の開閉、アイテム取得済み状態など。
    /// </summary>
    public interface IStageCheckpointParticipant
    {
        object CaptureCheckpointState();
        void RestoreCheckpointState(object state);
    }

    internal readonly struct StageObjectSnapshot
    {
        public readonly StageSaveMarkMB Target;
        public readonly bool ActiveSelf;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;
        public readonly bool HasRigidbody;
        public readonly Vector3 LinearVelocity;
        public readonly Vector3 AngularVelocity;
        public readonly StageParticipantSnapshot[] ParticipantSnapshots;

        public StageObjectSnapshot(
            StageSaveMarkMB target,
            bool activeSelf,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            bool hasRigidbody,
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            StageParticipantSnapshot[] participantSnapshots)
        {
            Target = target;
            ActiveSelf = activeSelf;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
            HasRigidbody = hasRigidbody;
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            ParticipantSnapshots = participantSnapshots ?? Array.Empty<StageParticipantSnapshot>();
        }
    }

    internal readonly struct StageParticipantSnapshot
    {
        public readonly IStageCheckpointParticipant Target;
        public readonly object State;

        public StageParticipantSnapshot(IStageCheckpointParticipant target, object state)
        {
            Target = target;
            State = state;
        }
    }
}
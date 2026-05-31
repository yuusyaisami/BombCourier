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
                transform.parent,
                transform.localPosition,
                transform.localRotation,
                transform.localScale,
                rb != null,
                rb != null && rb.isKinematic,
                rb == null || rb.useGravity,
                rb == null || rb.detectCollisions,
                rb != null ? rb.linearVelocity : Vector3.zero,
                rb != null ? rb.angularVelocity : Vector3.zero,
                rb != null && rb.IsSleeping(),
                participantSnapshots.ToArray()
            );
        }

        internal void Restore(StageObjectSnapshot snapshot)
        {
            bool shouldActivateBeforeRestore = saveActiveSelf && snapshot.ActiveSelf && !gameObject.activeSelf;
            if (shouldActivateBeforeRestore)
                gameObject.SetActive(true);

            Rigidbody rb = saveRigidbody && snapshot.HasRigidbody && TryGetComponent(out Rigidbody cachedRigidbody)
                ? cachedRigidbody
                : null;

            if (transform.parent != snapshot.Parent)
                transform.SetParent(snapshot.Parent, false);

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
                rb.isKinematic = snapshot.IsKinematic;
                rb.useGravity = snapshot.UseGravity;
                rb.detectCollisions = snapshot.DetectCollisions;

                if (!snapshot.IsKinematic)
                {
                    rb.linearVelocity = snapshot.LinearVelocity;
                    rb.angularVelocity = snapshot.AngularVelocity;
                }
                else
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                if (snapshot.IsSleeping || snapshot.IsKinematic)
                    rb.Sleep();
                else
                    rb.WakeUp();
            }

            for (int i = 0; i < snapshot.ParticipantSnapshots.Length; i++)
            {
                var participantSnapshot = snapshot.ParticipantSnapshots[i];

                if (participantSnapshot.Target != null)
                {
                    participantSnapshot.Target.RestoreCheckpointState(participantSnapshot.State);
                }
            }

            if (!saveActiveSelf)
                return;

            if (!snapshot.ActiveSelf && gameObject.activeSelf)
                gameObject.SetActive(false);
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
        public readonly Transform Parent;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;
        public readonly bool HasRigidbody;
        public readonly bool IsKinematic;
        public readonly bool UseGravity;
        public readonly bool DetectCollisions;
        public readonly Vector3 LinearVelocity;
        public readonly Vector3 AngularVelocity;
        public readonly bool IsSleeping;
        public readonly StageParticipantSnapshot[] ParticipantSnapshots;

        public StageObjectSnapshot(
            StageSaveMarkMB target,
            bool activeSelf,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            bool hasRigidbody,
            bool isKinematic,
            bool useGravity,
            bool detectCollisions,
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            bool isSleeping,
            StageParticipantSnapshot[] participantSnapshots)
        {
            Target = target;
            ActiveSelf = activeSelf;
            Parent = parent;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
            HasRigidbody = hasRigidbody;
            IsKinematic = isKinematic;
            UseGravity = useGravity;
            DetectCollisions = detectCollisions;
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            IsSleeping = isSleeping;
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

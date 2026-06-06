using System;
using UnityEngine;

namespace BC.Stage.Snapshot
{
    /// <summary>
    /// 個別ギミックが独自状態（スイッチON/OFF、扉開閉、破壊フラグ等）を
    /// 開始ベースラインへ保存・復元したい場合に実装するインターフェース。
    /// 旧 <c>IStageCheckpointParticipant</c> の後継。
    /// </summary>
    public interface IStageStateRestorable
    {
        object CaptureStageState();
        void RestoreStageState(object state);
    }

    /// <summary>
    /// 「ステージ開始時状態へ戻す」スナップショットの対象マーカー（旧 StageSaveMarkMB の後継）。
    /// OnEnable で <see cref="StageRestorableRegistry"/> に自己登録し、ID基準で復元される。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageRestorableMB : MonoBehaviour
    {
        [Header("Snapshot Target")]
        [Tooltip("チェックされたオブジェクトはスナップショット対象から除外されます。")]
        [SerializeField] private bool excludeFromSnapshot;
        [Tooltip("gameObject.activeSelf を保存・復元します。")]
        [SerializeField] private bool saveActiveSelf = true;
        [Tooltip("Transform（ワールド位置/回転・ローカルスケール）を保存・復元します。")]
        [SerializeField] private bool saveTransform = true;
        [Tooltip("Rigidbody の状態（kinematic/重力/速度/sleep）を保存・復元します。")]
        [SerializeField] private bool saveRigidbody = true;
        [Tooltip("ベースライン取得後に生成されたこのオブジェクトを、復元時に破棄せず残します。")]
        [SerializeField] private bool persistAcrossRestore;

        [SerializeField, HideInInspector] private string stableId;

        private string runtimeKey;

        public bool ExcludeFromSnapshot => excludeFromSnapshot;
        public bool PersistAcrossRestore => persistAcrossRestore;

        /// <summary>エディタで採番された安定ID（未採番なら空）。</summary>
        public string RawId => string.IsNullOrEmpty(stableId) ? string.Empty : stableId;

        /// <summary>レジストリ登録時に確定した実行時キー（重複時は序数付き）。</summary>
        public string RuntimeKey => runtimeKey;

        public StableObjectId Id => new StableObjectId(stableId);

        internal void SetRuntimeKey(string key)
        {
            runtimeKey = key;
        }

        private void OnEnable()
        {
            StageRestorableRegistry.Register(this);
        }

        private void OnDisable()
        {
            StageRestorableRegistry.MoveToInactive(this);
        }

        private void OnDestroy()
        {
            StageRestorableRegistry.Remove(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // エディタ編集時のみ、未採番なら安定IDを自動付与する。
            if (string.IsNullOrEmpty(stableId) && !Application.isPlaying)
            {
                stableId = Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        /// <summary>
        /// 現在状態を <see cref="RestorableSnapshot"/> として取得する（フラグに従いワールド姿勢/RB/参加者状態を収集）。
        /// </summary>
        internal RestorableSnapshot CaptureSnapshot()
        {
            Rigidbody rb = saveRigidbody ? GetComponent<Rigidbody>() : null;

            IStageStateRestorable[] participants = GetComponents<IStageStateRestorable>();
            object[] participantStates = participants.Length > 0 ? new object[participants.Length] : Array.Empty<object>();
            for (int i = 0; i < participants.Length; i++)
                participantStates[i] = participants[i].CaptureStageState();

            Transform t = transform;
            return new RestorableSnapshot(
                this,
                runtimeKey,
                StageRestorableRegistry.GetHierarchyPath(t),
                saveActiveSelf,
                saveTransform,
                saveRigidbody,
                gameObject.activeSelf,
                t.parent,
                t.position,
                t.rotation,
                t.localScale,
                rb != null,
                rb != null && rb.isKinematic,
                rb == null || rb.useGravity,
                rb == null || rb.detectCollisions,
                rb != null ? rb.linearVelocity : Vector3.zero,
                rb != null ? rb.angularVelocity : Vector3.zero,
                rb != null && rb.IsSleeping(),
                participantStates);
        }
    }

    /// <summary>
    /// 1オブジェクトの開始時スナップショット。参照に依存せず復元できるよう
    /// 値（ワールド姿勢/RB状態/参加者状態）と、診断用のキー・パスを保持する。
    /// Target/Parent は「同一インスタンスをその場で復元する」リロードのために生参照を持つ
    /// （別シーン再生成は本システムの対象外。破棄済みは Target==null として漏れログ対象）。
    /// </summary>
    internal readonly struct RestorableSnapshot
    {
        public readonly StageRestorableMB Target;
        public readonly string Key;
        public readonly string DebugPath;

        public readonly bool SaveActiveSelf;
        public readonly bool SaveTransform;
        public readonly bool SaveRigidbody;

        public readonly bool ActiveSelf;
        public readonly Transform Parent;
        public readonly Vector3 WorldPosition;
        public readonly Quaternion WorldRotation;
        public readonly Vector3 LocalScale;

        public readonly bool HasRigidbody;
        public readonly bool IsKinematic;
        public readonly bool UseGravity;
        public readonly bool DetectCollisions;
        public readonly Vector3 LinearVelocity;
        public readonly Vector3 AngularVelocity;
        public readonly bool IsSleeping;

        public readonly object[] ParticipantStates;

        public RestorableSnapshot(
            StageRestorableMB target,
            string key,
            string debugPath,
            bool saveActiveSelf,
            bool saveTransform,
            bool saveRigidbody,
            bool activeSelf,
            Transform parent,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector3 localScale,
            bool hasRigidbody,
            bool isKinematic,
            bool useGravity,
            bool detectCollisions,
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            bool isSleeping,
            object[] participantStates)
        {
            Target = target;
            Key = key;
            DebugPath = debugPath;
            SaveActiveSelf = saveActiveSelf;
            SaveTransform = saveTransform;
            SaveRigidbody = saveRigidbody;
            ActiveSelf = activeSelf;
            Parent = parent;
            WorldPosition = worldPosition;
            WorldRotation = worldRotation;
            LocalScale = localScale;
            HasRigidbody = hasRigidbody;
            IsKinematic = isKinematic;
            UseGravity = useGravity;
            DetectCollisions = detectCollisions;
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            IsSleeping = isSleeping;
            ParticipantStates = participantStates ?? Array.Empty<object>();
        }
    }
}

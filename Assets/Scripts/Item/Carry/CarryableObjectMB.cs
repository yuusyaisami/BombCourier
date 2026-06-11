using System.Collections.Generic;
using BC.Base;
using BC.Player;
using BC.Stage;
using UnityEngine;

namespace BC.Item
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public sealed class CarryableObjectMB : MonoBehaviour, ICarryableItem, ICarryReleaseOwnerCollisionGuard, IInteractionPromptDetailTextProvider, BC.Stage.Snapshot.IStageStateRestorable
    {
        [Header("Carry")]
        [Tooltip("このオブジェクトを持ち運び可能にするかを指定します。")]
        [SerializeField] private bool canBeCarried = true;

        [Header("Prompt")]
        [Tooltip("プロンプト詳細表示に使う任意テキストです。空なら詳細表示は出しません。")]
        [SerializeField, TextArea] private string promptDetailText = string.Empty;

        private Rigidbody rb;
        private Collider objectCollider;
        private bool isHandled;
        private Transform releaseParent; // 拾う前の親（Map 内）。リリース時にここへ戻す。
        private float ignoreOwnerCollisionUntilTime;
        private float ignoreOwnerCollisionHardDeadline;
        private readonly List<Collider> ignoredHolderColliders = new(16);

        // 投擲後、重なりが解消しない場合でもこの猶予を超えたら無視を強制解除する安全上限。
        private const float OwnerCollisionOverlapHardCapSeconds = 2.0f;

        public Transform ItemTransform => transform;
        public bool IsHandled => isHandled;
        public bool CanBeCarried => canBeCarried && enabled && gameObject.activeInHierarchy;
        public string PromptDetailText => promptDetailText ?? string.Empty;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            objectCollider = GetComponent<Collider>();
        }

        private void OnDisable()
        {
            ignoreOwnerCollisionUntilTime = 0f;
            ClearIgnoredHolderCollisions();
        }

        private void Update()
        {
            if (ignoreOwnerCollisionUntilTime <= 0f)
                return;

            if (Time.time < ignoreOwnerCollisionUntilTime)
                return;

            // タイマー満了後は、重なっているペアを分離するまで無視を維持し、めり込み解消による吹き飛びを防ぐ。
            // ハードキャップを超えたら強制解除する。
            bool hardCapReached = Time.time >= ignoreOwnerCollisionHardDeadline;
            bool allReleased = CarryCollisionUtility.ReleaseSeparatedIgnoredColliders(objectCollider, ignoredHolderColliders);

            if (allReleased || hardCapReached)
            {
                ignoreOwnerCollisionUntilTime = 0f;
                ignoreOwnerCollisionHardDeadline = 0f;
                ClearIgnoredHolderCollisions();
            }
        }

        public void OnHandle(Transform handlePoint)
        {
            if (!CanBeCarried || handlePoint == null)
                return;

            // 拾う前の親（Map 内）を覚えておき、リリース時にそこへ戻す（GameScene ルートに残さない）。
            if (!isHandled)
                releaseParent = transform.parent;

            isHandled = true;

            // 掴んでいる間は物理挙動を止め、ハンドル位置に追従させる。
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.detectCollisions = true;
            rb.useGravity = false;

            ConfigureHeldHolderCollisionIgnore(handlePoint);

            // 持っている間は当たり判定を消す（Player・壁ともに干渉させない）。
            if (objectCollider != null)
                objectCollider.enabled = false;

            transform.SetParent(handlePoint, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void OnRelease(Vector3 throwVelocity)
        {
            if (!isHandled)
                return;

            isHandled = false;
            ignoreOwnerCollisionUntilTime = 0f;
            // 先にコライダーを有効化してから無視解除する（無効状態での解除漏れを避ける）。
            if (objectCollider != null)
                objectCollider.enabled = true;
            ClearIgnoredHolderCollisions();
            transform.SetParent(CarryReleaseUtility.ResolveReleaseParent(releaseParent), true);

            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(throwVelocity, ForceMode.VelocityChange);
        }

        public void IgnoreOwnerCollisionAfterRelease(Transform ownerRoot, float durationSeconds)
        {
            if (ownerRoot == null || objectCollider == null || durationSeconds <= 0f)
                return;

            ConfigureHeldHolderCollisionIgnore(ownerRoot);
            ignoreOwnerCollisionUntilTime = Mathf.Max(ignoreOwnerCollisionUntilTime, Time.time + durationSeconds);
            ignoreOwnerCollisionHardDeadline = Mathf.Max(ignoreOwnerCollisionHardDeadline, Time.time + durationSeconds + OwnerCollisionOverlapHardCapSeconds);
        }

        public object CaptureStageState()
        {
            return new CarryableCheckpointState(isHandled);
        }

        public void RestoreStageState(object state)
        {
            if (state is not CarryableCheckpointState checkpoint)
                return;

            ignoreOwnerCollisionUntilTime = 0f;
            ClearIgnoredHolderCollisions();
            isHandled = checkpoint.IsHandled;

            if (objectCollider != null)
                objectCollider.enabled = !isHandled;

            if (rb != null)
            {
                rb.isKinematic = isHandled;
                rb.detectCollisions = true;
                rb.useGravity = !isHandled;

                if (isHandled)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            if (isHandled && transform.parent != null)
                ConfigureHeldHolderCollisionIgnore(transform.parent);
        }

        private void ConfigureHeldHolderCollisionIgnore(Transform handlePoint)
        {
            ClearIgnoredHolderCollisions();

            if (handlePoint == null || objectCollider == null)
                return;

            EntityMB ownerEntity = handlePoint.GetComponentInParent<EntityMB>();
            Transform ownerRoot = ownerEntity != null ? ownerEntity.transform : handlePoint.root;

            if (ownerRoot == null)
                return;

            Collider[] ownerColliders = ownerRoot.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < ownerColliders.Length; i++)
            {
                Collider ownerCollider = ownerColliders[i];

                if (!CanIgnoreHolderCollider(ownerCollider))
                    continue;

                Physics.IgnoreCollision(objectCollider, ownerCollider, true);
                ignoredHolderColliders.Add(ownerCollider);
            }
        }

        private bool CanIgnoreHolderCollider(Collider ownerCollider)
        {
            if (ownerCollider == null ||
                ownerCollider == objectCollider ||
                !ownerCollider.enabled ||
                !ownerCollider.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (ownerCollider.transform.IsChildOf(transform))
                return false;

            return !ignoredHolderColliders.Contains(ownerCollider);
        }

        private void ClearIgnoredHolderCollisions()
        {
            if (objectCollider != null)
            {
                for (int i = 0; i < ignoredHolderColliders.Count; i++)
                {
                    Collider ignored = ignoredHolderColliders[i];

                    if (ignored != null)
                        Physics.IgnoreCollision(objectCollider, ignored, false);
                }
            }

            ignoredHolderColliders.Clear();
        }

        private sealed class CarryableCheckpointState
        {
            public CarryableCheckpointState(bool isHandled)
            {
                IsHandled = isHandled;
            }

            public bool IsHandled { get; }
        }
    }
}

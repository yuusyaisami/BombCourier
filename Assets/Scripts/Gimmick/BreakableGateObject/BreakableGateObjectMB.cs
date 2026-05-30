using System.Collections;
using System.Collections.Generic;
using BC.Audio;
using BC.Bomb;
using BC.Manager;
using Sirenix.OdinInspector;
using UnityEngine;
namespace BC.Gimmick
{
    // このクラスは、外部からBombなどの衝撃を一定以上受けると、自身の子オブジェクトのRigidbodyにBombの衝撃に加え、設定された力を加えて破壊するギミックの実装です。
    public class BreakableGateObjectMB : MonoBehaviour, IBombImpactReceiver, IExplosionImpactReceiver
    {
        [Header("Breakable Gate Settings")]
        [SerializeField] private ParticleSystem breakEffectPrefab;
        [SerializeField] private List<Rigidbody> breakableParts = new List<Rigidbody>(); // 自分の子オブジェクトのRigidbodyをリストで管理
        [SerializeField] private bool autoCollectChildRigidbodies = false;
        [SerializeField, Min(-1)] private int maxBreakablePartsToActivate = -1;
        [SerializeField] private float breakForceThreshold = 200f; // 壊れるときの力の大きさ
        [SerializeField] private float explosionForce = 1000f; // 爆風の力の大きさ
        [SerializeField, Min(0f)] private float partCollisionEnableDelay = 0.08f;
        [SerializeField, Min(0f)] private float breakStabilizeDuration = 0.75f;
        [SerializeField, Min(0f)] private float maxPartLinearSpeed = 8f;
        [SerializeField, Min(0f)] private float maxPartAngularSpeed = 20f;
        [SerializeField, Min(0f)] private float maxPartDepenetrationVelocity = 2f;
        [SerializeField] private Vector3 breakForceDirection = Vector3.up; // 力の方向
        [SerializeField] private Transform breakForceOrigin; // 力の発生源
        [SerializeField] private Collider gateCollider; // 壊れる前の当たり判定。壊れた後は無効にする。

        [Header("Sound")]
        [Tooltip("ゲートが破壊されたときに再生するサウンドです。")]
        [SerializeField] private AudioDataSO breakSound;
        [Header("Goal Gate Settings")]
        [SerializeField] private bool isGoalGate = false;
        [SerializeField, ShowIf("isGoalGate")] private EntityTriggerObjectMB goalTriggerObject; // ゴールルームのトリガーオブジェクト

        [SerializeField, ShowIf("isGoalGate")] private GoalData goalData; // ゴールルームのデータ。ゴールカメラやゴールの位置などを管理するために使用する。

        private bool isBroken = false;
        private bool pendingInitialPhysicsSync;
        private readonly List<PartStabilizeState> activeBrokenParts = new List<PartStabilizeState>(16);
        public bool IsBroken => isBroken;
        public GoalData GoalData => goalData;
        public bool IsGoalGate => isGoalGate;
        public Transform BreakForceOrigin => breakForceOrigin;
        public Vector3 BreakForceDirection => breakForceDirection;
        public Vector3 TargetPoint => isGoalGate && goalData != null ? goalData.Target : transform.position;
        private void Awake()
        {
            CollectBreakablePartsIfNeeded();
            InitializeBreakableParts();
            pendingInitialPhysicsSync = true;

            if (isGoalGate && goalData != null)
            {
                goalData.goalTransform = this.transform;
            }
        }

        private void OnEnable()
        {
            if (isBroken)
                return;

            CollectBreakablePartsIfNeeded();
            InitializeBreakableParts();
            pendingInitialPhysicsSync = true;
        }

        private void Start()
        {
            if (!isBroken)
            {
                CollectBreakablePartsIfNeeded();
                InitializeBreakableParts();
                pendingInitialPhysicsSync = true;
            }

            // event登録
            if (isGoalGate && goalTriggerObject != null)
            {
                goalTriggerObject.OnTrigger += OnGoalTriggered;
            }
        }

        private void LateUpdate()
        {
            if (!pendingInitialPhysicsSync || isBroken)
                return;

            // 初期フレームで他コンポーネントに上書きされても、最後に固定状態へ戻す。
            InitializeBreakableParts();
            pendingInitialPhysicsSync = false;
        }

        private void FixedUpdate()
        {
            if (activeBrokenParts.Count == 0)
                return;

            for (int i = activeBrokenParts.Count - 1; i >= 0; i--)
            {
                PartStabilizeState state = activeBrokenParts[i];
                Rigidbody part = state.Rigidbody;

                if (part == null)
                {
                    activeBrokenParts.RemoveAt(i);
                    continue;
                }

                if (Time.time > state.StabilizeUntil)
                {
                    activeBrokenParts.RemoveAt(i);
                    continue;
                }

                if (maxPartLinearSpeed > 0f)
                    part.linearVelocity = Vector3.ClampMagnitude(part.linearVelocity, maxPartLinearSpeed);

                if (maxPartAngularSpeed > 0f)
                    part.angularVelocity = Vector3.ClampMagnitude(part.angularVelocity, maxPartAngularSpeed);
            }
        }
        private void OnDestroy()
        {
            // event解除
            if (isGoalGate && goalTriggerObject != null)
            {
                goalTriggerObject.OnTrigger -= OnGoalTriggered;
            }
        }
        private void OnGoalTriggered()
        {
            GameStateManagerMB.Instance.ChangeState(GameState.Goaling);
        }

        public void OnBombImpactReceived(Vector3 direction, float impactForce)
        {
            if (isBroken) return; // 既に壊れている場合は何もしない
            Debug.Log("BreakableGateObjectMB: Bomb impact received. Force: " + impactForce);

            if (impactForce >= breakForceThreshold)
            {
                BreakGate(direction, impactForce);
            }
        }

        public void OnExplosionImpactReceived(Vector3 direction, float impactForce)
        {
            OnBombImpactReceived(direction, impactForce);
        }

        private void BreakGate(Vector3 impactDirection, float impactForce)
        {
            isBroken = true;

            // 先にゲート本体の当たり判定を消して、破片が内側で押し出されるのを防ぐ。
            if (gateCollider != null)
            {
                gateCollider.enabled = false;
            }

            // 破壊サウンドの再生
            if (breakSound != null && breakSound.Clip != null)
                AudioSource.PlayClipAtPoint(breakSound.Clip, transform.position, breakSound.BaseVolume);

            // 破壊エフェクトの生成
            if (breakEffectPrefab != null)
            {
                Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            }

            // 子オブジェクトのRigidbodyに力を加える
            int activePartCount = 0;
            for (int i = 0; i < breakableParts.Count; i++)
            {
                if (maxBreakablePartsToActivate >= 0 && activePartCount >= maxBreakablePartsToActivate)
                    break;

                Rigidbody part = breakableParts[i];
                if (part != null)
                {
                    Vector3 normalizedImpactDirection = impactDirection.sqrMagnitude > 0.0001f
                        ? impactDirection.normalized
                        : transform.forward;

                    // 爆発方向と演出用固定方向を合成して、破片の飛散方向を安定させる。
                    float maxSumImpact = impactForce * 0.25f + explosionForce;
                    Vector3 forceDirection = normalizedImpactDirection * (impactForce / maxSumImpact) + breakForceDirection.normalized * (explosionForce / maxSumImpact);
                    Vector3 launchImpulse = forceDirection.normalized * explosionForce;
                    ActivateBrokenPart(part, launchImpulse);
                    activePartCount++;
                }
            }

            // ゴールゲート
            if (isGoalGate)
            {

            }
        }

# if UNITY_EDITOR
        private void OnValidate()
        {
            if (isGoalGate && goalData != null)
            {
                goalData.goalTransform = this.transform;
            }
        }
# endif

        private void CollectBreakablePartsIfNeeded()
        {
            if (!autoCollectChildRigidbodies && breakableParts.Count > 0)
                return;

            Rigidbody[] descendants = GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Rigidbody rb = descendants[i];
                if (rb == null || rb.transform == transform)
                    continue;

                if (rb != null && !breakableParts.Contains(rb))
                {
                    breakableParts.Add(rb);
                }
            }
        }

        private void InitializeBreakableParts()
        {
            foreach (Rigidbody part in breakableParts)
            {
                if (part == null)
                    continue;

                SetPartPhysicsEnabled(part, false);
            }
        }

        private static void SetPartPhysicsEnabled(Rigidbody part, bool enabled)
        {
            part.isKinematic = !enabled;
            part.useGravity = enabled;
            part.detectCollisions = enabled;

            if (!enabled)
            {
                part.linearVelocity = Vector3.zero;
                part.angularVelocity = Vector3.zero;
                part.Sleep();
            }

            Collider[] colliders = part.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = enabled;
            }
        }

        private void ActivateBrokenPart(Rigidbody part, Vector3 launchImpulse)
        {
            if (part == null)
                return;

            part.isKinematic = false;
            part.useGravity = true;
            part.detectCollisions = false;
            part.maxDepenetrationVelocity = Mathf.Max(0f, maxPartDepenetrationVelocity);
            part.linearVelocity = Vector3.zero;
            part.angularVelocity = Vector3.zero;
            part.AddForce(launchImpulse, ForceMode.Impulse);

            if (maxPartLinearSpeed > 0f)
                part.linearVelocity = Vector3.ClampMagnitude(part.linearVelocity, maxPartLinearSpeed);

            if (maxPartAngularSpeed > 0f)
                part.angularVelocity = Vector3.ClampMagnitude(part.angularVelocity, maxPartAngularSpeed);

            Collider[] colliders = part.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            if (breakStabilizeDuration > 0f)
                activeBrokenParts.Add(new PartStabilizeState(part, Time.time + breakStabilizeDuration));

            StartCoroutine(EnablePartCollisionsAfterDelay(part, colliders));
        }

        private IEnumerator EnablePartCollisionsAfterDelay(Rigidbody part, Collider[] colliders)
        {
            if (partCollisionEnableDelay > 0f)
                yield return new WaitForSeconds(partCollisionEnableDelay);
            else
                yield return new WaitForFixedUpdate();

            if (part == null)
                yield break;

            part.detectCollisions = true;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = true;
            }
        }

        private readonly struct PartStabilizeState
        {
            public PartStabilizeState(Rigidbody rigidbody, float stabilizeUntil)
            {
                Rigidbody = rigidbody;
                StabilizeUntil = stabilizeUntil;
            }

            public Rigidbody Rigidbody { get; }
            public float StabilizeUntil { get; }
        }

    }
}
using System.Collections;
using System.Collections.Generic;
using BC.Audio;
using BC.Bomb;
using BC.Manager;
using BC.Stage;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Gimmick
{
    // このクラスは、外部からBombなどの衝撃を一定以上受けると、自身の子オブジェクトのRigidbodyにBombの衝撃に加え、設定された力を加えて破壊するギミックの実装です。
    [RequireComponent(typeof(BC.Stage.Snapshot.StageRestorableMB))]
    public class BreakableGateObjectMB : MonoBehaviour, IBombImpactReceiver, IExplosionImpactReceiver, BC.Stage.Snapshot.IStageStateRestorable
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
        private readonly List<PartStabilizeState> activeBrokenParts = new List<PartStabilizeState>(16);

        public bool IsBroken => isBroken;
        public GoalData GoalData => goalData;
        public bool IsGoalGate => isGoalGate;
        public Transform BreakForceOrigin => breakForceOrigin;
        public Vector3 BreakForceDirection => breakForceDirection;
        public Vector3 TargetPoint => isGoalGate && goalData != null ? goalData.Target : transform.position;

        private void Awake()
        {
            InitializeAuthoringState();
            if (isGoalGate && goalData != null)
                goalData.goalTransform = this.transform;
        }

        private void OnEnable()
        {
            if (!isBroken)
                InitializeAuthoringState();

            if (isGoalGate && goalTriggerObject != null)
                goalTriggerObject.OnTrigger += OnGoalTriggered;
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

        private void OnDisable()
        {
            if (isGoalGate && goalTriggerObject != null)
                goalTriggerObject.OnTrigger -= OnGoalTriggered;
        }

        private void OnGoalTriggered()
        {
            GameStateManagerMB.Instance.ChangeState(GameState.Goaling);
        }

        public void OnBombImpactReceived(Vector3 direction, float impactForce)
        {
            if (isBroken)
                return;

            if (impactForce >= breakForceThreshold)
                BreakGate(direction, impactForce);
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
                gateCollider.enabled = false;

            TryPlayBreakSound();

            if (breakEffectPrefab != null)
            {
                Transform stageRoot = GetComponentInParent<MapRuntimeMB>(true)?.transform;
                SpawnTransientParticleEffect(breakEffectPrefab, transform.position, Quaternion.identity, stageRoot);
            }

            int activePartCount = 0;
            for (int i = 0; i < breakableParts.Count; i++)
            {
                if (maxBreakablePartsToActivate >= 0 && activePartCount >= maxBreakablePartsToActivate)
                    break;

                Rigidbody part = breakableParts[i];
                if (part == null)
                    continue;

                Vector3 normalizedImpactDirection = impactDirection.sqrMagnitude > 0.0001f
                    ? impactDirection.normalized
                    : transform.forward;

                Vector3 directionalBias = ResolveDirectionalBias(part);
                Vector3 forceDirection = normalizedImpactDirection + directionalBias;
                if (forceDirection.sqrMagnitude <= 0.0001f)
                    forceDirection = normalizedImpactDirection;

                float launchMagnitude = Mathf.Max(0f, explosionForce) + Mathf.Max(0f, impactForce * 0.25f);
                Vector3 launchImpulse = forceDirection.normalized * launchMagnitude;
                ActivateBrokenPart(part, launchImpulse);
                activePartCount++;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (isGoalGate && goalData != null)
                goalData.goalTransform = this.transform;
        }
#endif

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

                if (!breakableParts.Contains(rb))
                    breakableParts.Add(rb);
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
                // kinematic のまま速度を書き込むと warning が出るため、一時的に dynamic にしてからゼロ化する。
                if (part.isKinematic)
                    part.isKinematic = false;

                part.linearVelocity = Vector3.zero;
                part.angularVelocity = Vector3.zero;

                part.isKinematic = true;
                part.Sleep();
            }

            Collider[] colliders = part.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = enabled;
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
                colliders[i].enabled = false;

            if (breakStabilizeDuration > 0f)
                activeBrokenParts.Add(new PartStabilizeState(part, Time.time + breakStabilizeDuration));

            StartCoroutine(EnablePartCollisionsAfterDelay(part, colliders));
        }

        private void InitializeAuthoringState()
        {
            CollectBreakablePartsIfNeeded();
            InitializeBreakableParts();
        }

        // ステージ開始時状態（閉じた状態・破片の初期ローカル姿勢・ゲート当たり判定の有効状態）を保存する。
        public object CaptureStageState()
        {
            CollectBreakablePartsIfNeeded();
            var partPoses = new PartPose[breakableParts.Count];
            for (int i = 0; i < breakableParts.Count; i++)
            {
                Rigidbody part = breakableParts[i];
                if (part == null)
                {
                    partPoses[i] = default;
                    continue;
                }

                Transform t = part.transform;
                partPoses[i] = new PartPose(t.localPosition, t.localRotation, t.localScale);
            }

            return new GateStageState(isBroken, gateCollider != null && gateCollider.enabled, partPoses);
        }

        // 破壊状態を巻き戻し、破片を初期ローカル姿勢＋kinematic/sleep（閉じた authored 状態）へ戻す。
        public void RestoreStageState(object state)
        {
            if (!(state is GateStageState gateState))
                return;

            // 進行中の安定化/コリジョン有効化コルーチンを停止（このコンポーネントは本コルーチンのみ起動する）。
            StopAllCoroutines();
            activeBrokenParts.Clear();

            CollectBreakablePartsIfNeeded();
            PartPose[] partPoses = gateState.Parts;
            for (int i = 0; i < breakableParts.Count; i++)
            {
                Rigidbody part = breakableParts[i];
                if (part == null || partPoses == null || i >= partPoses.Length)
                    continue;

                Transform t = part.transform;
                t.localPosition = partPoses[i].LocalPosition;
                t.localRotation = partPoses[i].LocalRotation;
                t.localScale = partPoses[i].LocalScale;
            }

            isBroken = gateState.IsBroken;
            if (gateCollider != null)
                gateCollider.enabled = gateState.GateColliderEnabled;

            // 破片を kinematic/sleep・子Collider無効（＝閉じた状態）へ再初期化する。
            InitializeBreakableParts();
        }

        private readonly struct PartPose
        {
            public PartPose(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }

            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
        }

        private readonly struct GateStageState
        {
            public GateStageState(bool isBroken, bool gateColliderEnabled, PartPose[] parts)
            {
                IsBroken = isBroken;
                GateColliderEnabled = gateColliderEnabled;
                Parts = parts;
            }

            public bool IsBroken { get; }
            public bool GateColliderEnabled { get; }
            public PartPose[] Parts { get; }
        }

        private void TryPlayBreakSound()
        {
            if (breakSound == null || breakSound.Clip == null)
                return;

            if (AudioSystemMB.Instance == null)
            {
                Debug.LogWarning($"{nameof(BreakableGateObjectMB)}: Break sound is configured but {nameof(AudioSystemMB)} is unavailable.", this);
                return;
            }

            if (!AudioSystemMB.Instance.TryPlaySE(breakSound))
            {
                Debug.LogWarning($"{nameof(BreakableGateObjectMB)}: Failed to play configured break sound '{breakSound.Clip.name}'.", this);
            }
        }

        private Vector3 ResolveDirectionalBias(Rigidbody part)
        {
            Vector3 authoringDirection = breakForceDirection.sqrMagnitude > 0.0001f
                ? breakForceDirection.normalized
                : Vector3.zero;

            if (breakForceOrigin == null || part == null)
                return authoringDirection;

            Vector3 radialDirection = part.worldCenterOfMass - breakForceOrigin.position;
            if (radialDirection.sqrMagnitude <= 0.0001f)
                return authoringDirection;

            radialDirection.Normalize();
            return authoringDirection.sqrMagnitude > 0.0001f
                ? (radialDirection + authoringDirection)
                : radialDirection;
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

        private static void SpawnTransientParticleEffect(ParticleSystem effectPrefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (effectPrefab == null)
                return;

            ParticleSystem instance = parent != null
                ? Instantiate(effectPrefab, position, rotation, parent)
                : Instantiate(effectPrefab, position, rotation);
            if (instance == null)
                return;

            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            float maxLifetime = 0.5f;

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                ParticleSystem.MainModule main = system.main;
                float startLifetimeMax = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                    ? main.startLifetime.constantMax
                    : main.startLifetime.constant;
                maxLifetime = Mathf.Max(maxLifetime, main.duration + startLifetimeMax + 0.5f);
            }

            instance.Play(true);
            Destroy(instance.gameObject, maxLifetime);
        }
    }
}

using System.Collections.Generic;
using BC.Bomb;
using BC.Manager;
using Sirenix.OdinInspector;
using UnityEngine;
namespace BC.Gimmick
{
    // このクラスは、外部からBombなどの衝撃を一定以上受けると、自身の子オブジェクトのRigidbodyにBombの衝撃に加え、設定された力を加えて破壊するギミックの実装です。
    public class BreakableGateObjectMB : MonoBehaviour, IBombImpactReceiver
    {
        [Header("Breakable Gate Settings")]
        [SerializeField] private ParticleSystem breakEffectPrefab;
        [SerializeField] private List<Rigidbody> breakableParts = new List<Rigidbody>(); // 自分の子オブジェクトのRigidbodyをリストで管理
        [SerializeField] private float breakForceThreshold = 200f; // 壊れるときの力の大きさ
        [SerializeField] private float explosionForce = 1000f; // 爆風の力の大きさ
        [SerializeField] private Vector3 breakForceDirection = Vector3.up; // 力の方向
        [SerializeField] private Transform breakForceOrigin; // 力の発生源
        [Header("Goal Gate Settings")]
        [SerializeField] private bool isGoalGate = false;
        [SerializeField, ShowIf("isGoalGate")] private EntityTriggerObjectMB goalTriggerObject; // ゴールルームのトリガーオブジェクト

        [SerializeField, ShowIf("isGoalGate")] private GoalData goalData; // ゴールルームのデータ。ゴールカメラやゴールの位置などを管理するために使用する。

        private bool isBroken = false;
        public GoalData GoalData => goalData;

        private void Start()
        {
            // Rigidbodyがアタッチされていない子オブジェクトを自動的にリストに追加
            foreach (Transform child in transform)
            {
                Rigidbody rb = child.GetComponent<Rigidbody>();
                if (rb != null && !breakableParts.Contains(rb))
                {
                    breakableParts.Add(rb);
                }
            }
            // 最初は全てのRigidbodyをKinematicにして物理挙動を無効化
            foreach (var part in breakableParts)
            {
                if (part != null)
                {
                    part.isKinematic = true;
                }
            }
            // event登録
            if (isGoalGate && goalTriggerObject != null)
            {
                goalTriggerObject.OnTrigger += OnGoalTriggered;
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

        public void OnBombImpactReceived(Vector3 impactPoint, float impactForce)
        {
            if (isBroken) return; // 既に壊れている場合は何もしない

            if (impactForce >= breakForceThreshold)
            {
                BreakGate(impactPoint, impactForce);
            }
        }

        private void BreakGate(Vector3 impactPoint, float impactForce)
        {
            isBroken = true;

            // 破壊エフェクトの生成
            if (breakEffectPrefab != null)
            {
                Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            }

            // 子オブジェクトのRigidbodyに力を加える
            foreach (var part in breakableParts)
            {
                if (part != null)
                {
                    part.isKinematic = false; // Rigidbodyを物理挙動させる
                    // 方向はimpactPointとbreakForceOriginとRBの位置の二つを考慮して決定する
                    // どれぐらいの力かで方向の重みを変える
                    float maxSumImpact = impactForce + explosionForce;
                    Vector3 forceDirection = (impactPoint - breakForceOrigin.position).normalized * (impactForce / maxSumImpact) + breakForceDirection.normalized * (explosionForce / maxSumImpact);
                    part.AddForce(forceDirection, ForceMode.Impulse);
                }
            }

            // ゴールゲート
            if (isGoalGate)
            {

            }
        }

# if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (breakForceOrigin != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(breakForceOrigin.position, breakForceOrigin.position + breakForceDirection.normalized * 2f);
                Gizmos.DrawWireSphere(breakForceOrigin.position, 0.2f);
            }
        }
# endif

    }
}
using BC.Base;
using UnityEngine;
namespace BC.Bomb
{
    [System.Serializable]
    public struct BombExplosionThresholdData
    {
        public string UnityTag { get; }
        public float ExplosionThresholdMul { get; }
    }
    [System.Serializable]
    public struct BombExplosionThresholdDataset
    {
        public BombExplosionThresholdData[] Thresholds { get; }
    }
    // 爆弾の衝撃を検出するインターフェース
    public interface IBombImpactDetector
    {
        void OnBombImpact(Vector2 direction, float impactForce);
    }
    // rigidbodyを持っていないが爆弾の衝撃を検出したいオブジェクトはこのインターフェースを実装する
    public interface IBombImpactReceiver
    {
        void OnBombImpactReceived(Vector2 direction, float impactForce);
    }
    public class BombMB : UnityEngine.MonoBehaviour
    {
        // 特定の衝撃で爆発するための閾値
        [SerializeField] private float explosionThreshold = 10f;
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private float explosionForce = 1000f;
        [SerializeField] private GameObject explosionEffectPrefab; // 爆発エフェクトのプレハブ
        [SerializeField]
        private BombExplosionThresholdDataset thresholdDataset;

        private Rigidbody rb;
        private Collider bombCollider;
        private SceneKernelMB kernelMB;
        private EntityRef entityRef;
        private float lastImpactForce;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            bombCollider = GetComponent<Collider>();
            kernelMB = GetComponentInParent<SceneKernelMB>();
            entityRef = GetComponentInParent<EntityMB>().Entity;

            if (rb == null)
            {
                Debug.LogError("BombMB: Rigidbody component is missing.", this);
            }

            if (bombCollider == null)
            {
                Debug.LogError("BombMB: Collider component is missing.", this);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (rb == null || bombCollider == null) return;
            // タグに基づいて閾値を取得
            float explosionThreshold = this.explosionThreshold; // デフォルトの閾値
            foreach (var thresholdData in thresholdDataset.Thresholds)
            {
                if (collision.gameObject.CompareTag(thresholdData.UnityTag))
                {
                    explosionThreshold *= thresholdData.ExplosionThresholdMul;
                    break;
                }
            }

            // 衝撃の強さを計算
            float impactForce = collision.impulse.magnitude / Time.fixedDeltaTime;
            lastImpactForce = impactForce;

            // 周りにRigidbodyがある場合は、衝撃を与える
            foreach (var hit in Physics.OverlapSphere(transform.position, explosionRadius))
            {
                Rigidbody hitRb = hit.GetComponent<Rigidbody>();
                if (hitRb != null && hitRb != rb)
                {
                    Vector3 direction = (hit.transform.position - transform.position).normalized;
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    float forceMagnitude = Mathf.Clamp(explosionForce / (distance * distance), 0, explosionForce);
                    if (hitRb.TryGetComponent<IBombImpactDetector>(out var impactDetector))
                    {
                        impactDetector.OnBombImpact(direction, forceMagnitude);
                    }
                    hitRb.AddForce(direction * forceMagnitude);
                }
                else if (hit.TryGetComponent<IBombImpactReceiver>(out var impactReceiver))
                {
                    Vector3 direction = (hit.transform.position - transform.position).normalized;
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    float forceMagnitude = Mathf.Clamp(explosionForce / (distance * distance), 0, explosionForce);
                    impactReceiver.OnBombImpactReceived(direction, forceMagnitude);
                }
            }

            // 閾値を超える衝撃があった場合に爆発
            if (impactForce >= explosionThreshold)
            {
                Explode();
            }
        }
        private void Update()
        {
            // lastImpactForceの数値を落とす(時間経過で衝撃の強さが減少するようにする)
            if (lastImpactForce > 0) lastImpactForce = Mathf.Lerp(lastImpactForce, 0, Time.deltaTime * 5f);
        }

        private void Explode()
        {
            // 爆発エフェクトを生成
            if (explosionEffectPrefab != null)
            {
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            }

            // ここで爆発のダメージ処理などを行うことができます

            // ボムオブジェクトを破壊
            kernelMB.Kernel.Spawner.Despawn(entityRef);
        }
    }
}
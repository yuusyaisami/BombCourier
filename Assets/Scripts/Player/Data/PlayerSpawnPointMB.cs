using UnityEngine;

namespace BC.Bomb
{
    // これはRestoreのときではなく、ステージ初期ロードのときにスポーンポイントを管理するためのクラスです。1
    public class PlayerSpawnPointMB : UnityEngine.MonoBehaviour
    {
        [SerializeField] private int spawnPointID; // スポーンポイントのID

        [Header("Spawn Orientation")]
        [Tooltip("スポーン時の向き（ローカル空間）。ゼロベクトルのときは transform.forward に fallback する。")]
        [SerializeField] private Vector3 localSpawnDirection = Vector3.forward;

        public int SpawnPointID => spawnPointID;

        /// <summary>
        /// ローカル向きをワールド空間に変換して返す。
        /// ゼロベクトルのときは transform.forward を返す。
        /// </summary>
        public Vector3 GetWorldSpawnDirection()
        {
            if (localSpawnDirection.sqrMagnitude <= 0.0001f)
                return transform.forward;

            return transform.TransformDirection(localSpawnDirection.normalized);
        }

        /// <summary>
        /// プレイヤー body 用の初期回転を返す（水平成分のみ、yaw のみの回転）。
        /// ゼロベクトルのときは Quaternion.identity を返す。
        /// </summary>
        public Quaternion GetSpawnBodyRotation()
        {
            Vector3 world = GetWorldSpawnDirection();
            world.y = 0f;

            if (world.sqrMagnitude <= 0.0001f)
                return Quaternion.identity;

            return Quaternion.LookRotation(world.normalized);
        }
    }
}
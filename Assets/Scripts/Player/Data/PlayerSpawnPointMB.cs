using UnityEngine;
namespace BC.Bomb
{
    public class PlayerSpawnPointMB : UnityEngine.MonoBehaviour
    {
        [SerializeField] private int spawnPointID; // スポーンポイントのID
        public int SpawnPointID => spawnPointID; // スポーンポイントのIDを外部から取得できるようにするプロパティ
    }
}
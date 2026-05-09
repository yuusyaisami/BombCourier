using UnityEngine;
namespace BC.Bomb
{
    // これはRestoreのときではなく、ステージ初期ロードのときにスポーンポイントを管理するためのクラスです。1
    public class PlayerSpawnPointMB : UnityEngine.MonoBehaviour
    {
        [SerializeField] private int spawnPointID; // スポーンポイントのID
        public int SpawnPointID => spawnPointID; // スポーンポイントのIDを外部から取得できるようにするプロパティ
    }
}
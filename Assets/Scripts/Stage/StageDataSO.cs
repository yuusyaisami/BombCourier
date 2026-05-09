using UnityEngine;
namespace BC.Stage
{
    [CreateAssetMenu(fileName = "StageData", menuName = "BombCourier/StageData", order = 1)]
    public class StageDataSO : ScriptableObject
    {
        // ステージのデータを管理するクラス
        public string stageName; // ステージの名前
        public string stageDescription; // ステージの説明
        public int stageDifficulty; // ステージの難易度
        public GameObject stagePrefab; // ステージのプレハブ
    }
}
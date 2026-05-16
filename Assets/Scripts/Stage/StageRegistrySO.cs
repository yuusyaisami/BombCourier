using System.Collections.Generic;
using UnityEngine;
namespace BC.Stage
{
    [System.Serializable]
    public class StageData
    {
        // ステージのデータを管理するクラス
        public string stageName; // ステージの名前
        public string stageDescription; // ステージの説明
        public int stageDifficulty; // ステージの難易度
        public float clearTimeThreshold; // ゴールデータにクリアタイムの閾値がある場合はそれを使用し、ない場合はデフォルト値を返す
        public GameObject stagePrefab; // ステージのプレハブ
    }
    [CreateAssetMenu(fileName = "StageRegistry", menuName = "BombCourier/StageRegistry", order = 1)]
    public class StageRegistrySO : ScriptableObject
    {
        [SerializeField] private List<StageData> stageData; // ステージのデータ
        public List<StageData> StageData => stageData; // ステージのデータへの公開
    }
}
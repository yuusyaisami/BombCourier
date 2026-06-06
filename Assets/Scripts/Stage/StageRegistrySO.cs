using System.Collections.Generic;
using BC.Localization;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;

namespace BC.Stage
{
    [System.Serializable]
    public class StageData
    {
        // ステージのデータを管理するクラス
        public string stageName; // ステージ名のフォールバック（Key が見つからない場合に表示）
        public string stageNameKey; // ステージ名のローカライズ Key（StageRegistrySO.stageNameTable から解決）
        public int stageDifficulty; // ステージの難易度
        public float clearTimeThreshold; // ゴールデータにクリアタイムの閾値がある場合はそれを使用し、ない場合はデフォルト値を返す
        public GameObject stagePrefab; // ステージのプレハブ
        // タイトル画面ステージセレクト用ビジュアル (null 許容)
        public Sprite backgroundSprite; // ステージセレクト背景画像
        public Sprite previewSprite;    // ステージセレクトのサムネイル (未使用は null でも可)
    }

    [CreateAssetMenu(fileName = "StageRegistry", menuName = "BombCourier/StageRegistry", order = 1)]
    public class StageRegistrySO : ScriptableObject
    {
        [SerializeField] private List<StageData> stageData; // ステージのデータ
        public List<StageData> StageData => stageData; // ステージのデータへの公開

        [Header("Localization")]
        [Tooltip("ステージ名を解決する統一 String Table。各 StageData の stageNameKey で引く。見つからなければ stageName。")]
        public LocalizedStringTable stageNameTable;

        // StageData のステージ名を現在ロケールで解決する。Key 未指定/未発見なら stageName をフォールバック表示する。
        public UniTask<string> ResolveStageNameAsync(StageData data)
        {
            if (data == null)
                return UniTask.FromResult(string.Empty);

            return ResolveStageNameAsync(data.stageNameKey, data.stageName);
        }

        public UniTask<string> ResolveStageNameAsync(string key, string fallback)
        {
            return LocalizedStringResolver.ResolveAsync(stageNameTable, key, fallback);
        }
    }
}

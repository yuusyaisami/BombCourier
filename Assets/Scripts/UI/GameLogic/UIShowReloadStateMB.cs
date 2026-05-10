using BC.Base;
using BC.Manager;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace BC.UI
{
    // 爆弾が爆発して、プレイヤーにリロードを促すためのUI
    public class UIShowReloadStateMB : MonoBehaviour
    {
        [SerializeField][SerializeReference] private IAnimationSpriteClipSource RetryTextAnimClipSource; // 爆発アニメーションのクリップソース
        [SerializeField] private SpriteAnimationPlayerMB spriteAnimationPlayer; // 爆発アニメーションを再生するためのコンポーネント
        [SerializeField] private InputActionReference reloadInputActionReference; // リロードの入力アクションリファレンス
        [SerializeField] private Slider reloadProgressSlider; // リロードの進行状況を表示するスライダー

        private float reloadInputHoldTime; // リロード入力のホールド時間
        private float requiredHoldTime = 1.5f; // リロード入力をホールドする必要がある時間
        private void Start()
        {
            // 最初は非表示にしておく
            gameObject.SetActive(false);
            // GameLogicManagerMBのExplodedStateイベントにリスナーを登録
            GameLogicManagerMB.Instance.ExplodedState += OnExplodedState;
        }

        public void OnExplodedState()
        {
            // 爆弾が爆発したときにUIを表示する
            gameObject.SetActive(true);
            // 爆発アニメーションを再生する
            spriteAnimationPlayer.Play(RetryTextAnimClipSource, SpriteAnimationPlayMode.Once);
        }
        private void Update()
        {
            // 入力があったときにリロードする
            if (gameObject.activeSelf)
            {
                if (reloadInputActionReference.action.IsPressed())
                {
                    reloadInputHoldTime += Time.deltaTime;
                    if (reloadInputHoldTime >= requiredHoldTime)
                    {
                        GameStateManagerMB.Instance.ChangeState(GameState.Reload);
                    }
                }
                else
                {
                    reloadInputHoldTime = 0f; // 入力が離されたらホールド時間をリセットする
                }
                // リロードの進行状況をスライダーに反映する
                if (reloadProgressSlider != null)
                {
                    float progress = Mathf.Clamp01(reloadInputHoldTime / requiredHoldTime);
                    float displayProgress = Mathf.SmoothStep(0f, 1f, progress); // スムースステップで進行状況を計算する
                    reloadProgressSlider.value = displayProgress;
                }
            }
        }
    }
}
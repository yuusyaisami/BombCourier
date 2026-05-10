using BC.Base;
using BC.Bomb;
using BC.Manager;
using UnityEngine;
using UnityEngine.UI;
namespace BC.UI
{
    public class UIBombStateMB : MonoBehaviour
    {
        [SerializeField] private Slider bombTimerSlider;
        [SerializeField] private Slider bombImpactExplosionSlider; // 爆弾が受けた衝撃の大きさを表示するスライダー。衝撃が大きいほど、爆発範囲が広がる。
        [SerializeField][SerializeReference] private IAnimationSpriteClipSource TimerIconStartClipSource;
        [SerializeField][SerializeReference] private IAnimationSpriteClipSource TimerIconStoppedClipSource;
        [SerializeField] private SpriteAnimationPlayerMB timerIconAnimationPlayer;

        private BombMB bomb;
        private bool isFuseStarted;
        private void Start()
        {
            GameLogicManagerMB.Instance.OnCurrentBombChanged += HandleCurrentBombChanged;
        }
        private void OnDestroy()
        {
            GameLogicManagerMB.Instance.OnCurrentBombChanged -= HandleCurrentBombChanged;
        }

        private void HandleCurrentBombChanged(BombMB newBomb)
        {
            bomb = newBomb;
        }

        private void Update()
        {
            if (bomb == null)
            {
                bombTimerSlider.value = 1f;
                bombImpactExplosionSlider.value = 0f;
                return;
            }
            // timer設定
            if (bomb.FuseStarted)
            {
                bombTimerSlider.value = bomb.RemainingFuseTime / bomb.TotalFuseTime;
                // アイコンのアニメーションも進める
                if (TimerIconStartClipSource != null && timerIconAnimationPlayer != null && !isFuseStarted)
                {
                    timerIconAnimationPlayer.Play(TimerIconStartClipSource, SpriteAnimationPlayMode.Loop);
                    isFuseStarted = true;
                }
            }
            else
            {
                bombTimerSlider.value = 1f;
                if (TimerIconStoppedClipSource != null && timerIconAnimationPlayer != null && isFuseStarted)
                {
                    timerIconAnimationPlayer.Play(TimerIconStoppedClipSource, SpriteAnimationPlayMode.Loop);
                    isFuseStarted = false;
                }
            }

            // 爆発範囲設定
            bombImpactExplosionSlider.value = bomb.ImpactExplosionRatio;
        }
    }
}
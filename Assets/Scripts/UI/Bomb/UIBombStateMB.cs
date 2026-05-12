using BC.Base;
using BC.Bomb;
using BC.Manager;
using UnityEngine;
using UnityEngine.UI;
namespace BC.UI
{
    public class UIBombStateMB : MonoBehaviour
    {
        private const float PostExplodeImpactHoldTime = 0.35f;
        private const float ImpactGaugeFadeSpeed = 0.2f;

        [SerializeField] private Slider bombTimerSlider;
        [SerializeField] private Slider bombImpactExplosionSlider; // 爆弾が受けた衝撃の大きさを表示するスライダー。衝撃が大きいほど、爆発範囲が広がる。
        [SerializeField][SerializeReference] private IAnimationSpriteClipSource TimerIconStartClipSource;
        [SerializeField][SerializeReference] private IAnimationSpriteClipSource TimerIconStoppedClipSource;
        [SerializeField] private SpriteAnimationPlayerMB timerIconAnimationPlayer;

        private BombMB bomb;
        private bool isFuseStarted;
        private bool hadBombLastFrame;
        private float displayedImpactExplosionRatio;
        private float postExplodeImpactHoldRemaining;

        private void OnEnable()
        {
            if (GameLogicManagerMB.Instance == null)
                return;

            GameLogicManagerMB.Instance.OnCurrentBombChanged += HandleCurrentBombChanged;
            HandleCurrentBombChanged(GameLogicManagerMB.Instance.CurrentBomb);
        }
        private void OnDisable()
        {
            if (GameLogicManagerMB.Instance == null)
                return;

            GameLogicManagerMB.Instance.OnCurrentBombChanged -= HandleCurrentBombChanged;
        }

        private void HandleCurrentBombChanged(BombMB newBomb)
        {
            if (newBomb == null && bomb != null)
                HoldCurrentImpactGauge();

            bomb = newBomb;

            if (bomb != null)
            {
                displayedImpactExplosionRatio = bomb.ImpactExplosionRatio;
                hadBombLastFrame = true;
            }
        }

        private void Update()
        {
            if (bomb == null)
            {
                if (hadBombLastFrame)
                    HoldCurrentImpactGauge();

                bombTimerSlider.value = 0f;

                if (TimerIconStoppedClipSource != null && timerIconAnimationPlayer != null && isFuseStarted)
                {
                    timerIconAnimationPlayer.Play(TimerIconStoppedClipSource, SpriteAnimationPlayMode.Loop);
                    isFuseStarted = false;
                }

                if (postExplodeImpactHoldRemaining > 0.0f)
                {
                    postExplodeImpactHoldRemaining = Mathf.Max(0.0f, postExplodeImpactHoldRemaining - Time.deltaTime);
                }
                else
                {
                    displayedImpactExplosionRatio = Mathf.MoveTowards(displayedImpactExplosionRatio, 0.0f, ImpactGaugeFadeSpeed * Time.deltaTime);
                }

                bombImpactExplosionSlider.value = displayedImpactExplosionRatio;
                hadBombLastFrame = false;
                return;
            }

            hadBombLastFrame = true;
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
            displayedImpactExplosionRatio = bomb.ImpactExplosionRatio;
            bombImpactExplosionSlider.value = displayedImpactExplosionRatio;
        }

        private void HoldCurrentImpactGauge()
        {
            displayedImpactExplosionRatio = bomb != null
                ? bomb.ImpactExplosionRatio
                : displayedImpactExplosionRatio;
            postExplodeImpactHoldRemaining = PostExplodeImpactHoldTime;
        }
    }
}
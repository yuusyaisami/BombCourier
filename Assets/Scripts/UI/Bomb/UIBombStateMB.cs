using BC.Base;
using BC.Bomb;
using BC.Item;
using BC.Manager;
using BC.Player;
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
        private GameLogicManagerMB gameLogicManager;
        private PlayerItemHandleStateMB itemHandleState;
        private BombMB lastKnownBomb;
        private bool isFuseStarted;
        private bool hadBombLastFrame;
        private float displayedImpactExplosionRatio;
        private float postExplodeImpactHoldRemaining;

        private void OnEnable()
        {
            TryBindGameLogic();
        }

        private void OnDisable()
        {
            UnbindGameLogic();
            UnbindItemHandleState();
        }

        private void OnDestroy()
        {
            UnbindGameLogic();
            UnbindItemHandleState();
        }

        private void HandleCurrentBombChanged(BombMB newBomb)
        {
            if (newBomb == null && bomb != null)
                HoldCurrentImpactGauge();

            if (newBomb != null)
                lastKnownBomb = newBomb;

            bomb = newBomb != null ? newBomb : lastKnownBomb;

            if (bomb != null)
            {
                displayedImpactExplosionRatio = bomb.ImpactExplosionRatio;
                hadBombLastFrame = true;
            }
        }

        private void Update()
        {
            if (gameLogicManager == null)
                TryBindGameLogic();

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

        private void HandlePlayerSpawned(PlayerMB player)
        {
            BindItemHandleState(player != null ? player.GetComponent<PlayerItemHandleStateMB>() : null);
        }

        private void HandleCurrentHandledItemChanged(ICarryableItem handledItem)
        {
            HandleCurrentBombChanged(ResolveBombFromHandledItem(handledItem));
        }

        private void BindItemHandleState(PlayerItemHandleStateMB newItemHandleState)
        {
            if (ReferenceEquals(itemHandleState, newItemHandleState))
            {
                HandleCurrentHandledItemChanged(itemHandleState != null ? itemHandleState.CurrentHandledItem : null);
                return;
            }

            UnbindItemHandleState();
            itemHandleState = newItemHandleState;

            if (itemHandleState != null)
                itemHandleState.CurrentHandledItemChanged += HandleCurrentHandledItemChanged;

            HandleCurrentHandledItemChanged(itemHandleState != null ? itemHandleState.CurrentHandledItem : null);
        }

        private void UnbindItemHandleState()
        {
            if (itemHandleState != null)
                itemHandleState.CurrentHandledItemChanged -= HandleCurrentHandledItemChanged;

            itemHandleState = null;
        }

        private void TryBindGameLogic()
        {
            GameLogicManagerMB manager = GameLogicManagerMB.Instance;

            if (manager == null)
                return;

            if (ReferenceEquals(gameLogicManager, manager))
            {
                HandlePlayerSpawned(gameLogicManager.PlayerInstance);
                return;
            }

            UnbindGameLogic();
            gameLogicManager = manager;
            gameLogicManager.OnPlayerSpawned += HandlePlayerSpawned;
            HandlePlayerSpawned(gameLogicManager.PlayerInstance);
        }

        private void UnbindGameLogic()
        {
            if (gameLogicManager != null)
                gameLogicManager.OnPlayerSpawned -= HandlePlayerSpawned;

            gameLogicManager = null;
        }

        private static BombMB ResolveBombFromHandledItem(ICarryableItem handledItem)
        {
            if (handledItem == null)
                return null;

            if (handledItem is BombMB handledBomb)
                return handledBomb;

            if (handledItem is Component handledComponent)
            {
                if (handledComponent.TryGetComponent(out BombMB componentBomb))
                    return componentBomb;

                return handledComponent.GetComponentInParent<BombMB>();
            }

            Transform itemTransform = handledItem.ItemTransform;

            if (itemTransform == null)
                return null;

            if (itemTransform.TryGetComponent(out BombMB itemTransformBomb))
                return itemTransformBomb;

            return itemTransform.GetComponentInParent<BombMB>();
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
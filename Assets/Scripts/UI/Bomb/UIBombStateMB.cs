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

        [Header("Time Bonus Mark")]
        [Tooltip("タイムボーナス境界線として TimerSlider 上に表示する Mark の Sprite。")]
        [SerializeField] private Sprite timeBonusMarkSprite;
        [Tooltip("Mark の縦(=スライダー高さ)に対する横幅の割合。")]
        [SerializeField, Min(0f)] private float timeBonusMarkWidthToHeightRatio = 0.15f;
        [Tooltip("Slider が境界線を過ぎた(ボーナス不可になった)後の Mark の明度倍率。1=変化なし、小さいほど暗い。")]
        [SerializeField, Range(0f, 1f)] private float timeBonusMarkPassedBrightness = 0.5f;

        private BombMB bomb;
        private GameLogicManagerMB gameLogicManager;
        private GameStateManagerMB gameStateManager;
        private PlayerItemHandleStateMB itemHandleState;
        private BombMB lastKnownBomb;
        private bool isFuseStarted;
        private bool hadBombLastFrame;
        private float displayedImpactExplosionRatio;
        private float postExplodeImpactHoldRemaining;

        // 爆弾が爆発したあと、次の爆弾を持つ or リセットが入るまで Impact を MAX 固定する。
        private bool holdImpactMaxAfterExplode;

        // 自動生成する Mark。生成後は非表示にしておき、必要時に位置決め+表示する。
        private RectTransform timeBonusMarkRect;
        private Image timeBonusMarkImage;
        private bool timeBonusMarkInitialized;
        private float timeBonusMarkNormalized = -1f; // 表示中の境界線位置(Slider 値基準)。未表示は -1。
        private Color timeBonusMarkBaseColor = Color.white; // Mark Image の基準色。生成時にキャプチャする。

        private void OnEnable()
        {
            EnsureTimeBonusMarkCreated();
            TryBindGameLogic();
            TryBindGameState();
        }

        private void OnDisable()
        {
            UnbindGameLogic();
            UnbindGameState();
            UnbindItemHandleState();
        }

        private void OnDestroy()
        {
            UnbindGameLogic();
            UnbindGameState();
            UnbindItemHandleState();
        }

        private void HandleCurrentBombChanged(BombMB newBomb)
        {
            if (newBomb == null && bomb != null)
                HoldCurrentImpactGauge();

            if (newBomb != null)
            {
                lastKnownBomb = newBomb;
                // 次の爆弾を持ったので、爆発後の Impact MAX 固定を解除する。
                holdImpactMaxAfterExplode = false;
                // 持っている爆弾と現在ステージの閾値から、タイムボーナス境界線に Mark を置く。
                ShowTimeBonusMarkForBomb(newBomb);
            }

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

            if (gameStateManager == null)
                TryBindGameState();

            // 爆発後は、次の爆弾取得 or リセットが入るまで Impact を MAX で固定表示する。
            if (holdImpactMaxAfterExplode)
            {
                HoldPostExplodeImpactMax();
                return;
            }

            if (gameLogicManager != null && !gameLogicManager.HasAnyActiveSceneBomb())
            {
                ResetToInitialDisplayState();
                return;
            }

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

            // Slider が境界線を過ぎたら Mark を暗くする。
            RefreshTimeBonusMarkBrightness();
        }

        // 爆発後の Impact MAX 固定表示。
        private void HoldPostExplodeImpactMax()
        {
            displayedImpactExplosionRatio = 1.0f;

            if (bombImpactExplosionSlider != null)
                bombImpactExplosionSlider.value = bombImpactExplosionSlider.maxValue;

            if (bombTimerSlider != null)
                bombTimerSlider.value = 0f;

            if (TimerIconStoppedClipSource != null && timerIconAnimationPlayer != null && isFuseStarted)
            {
                timerIconAnimationPlayer.Play(TimerIconStoppedClipSource, SpriteAnimationPlayMode.Loop);
                isFuseStarted = false;
            }

            bomb = null;
            hadBombLastFrame = false;

            // 爆発後は通過扱い。Mark が表示中なら暗いまま保つ。
            RefreshTimeBonusMarkBrightness();
        }

        private void ResetToInitialDisplayState()
        {
            bomb = null;
            lastKnownBomb = null;
            hadBombLastFrame = false;
            postExplodeImpactHoldRemaining = 0.0f;
            displayedImpactExplosionRatio = 0.0f;

            if (bombTimerSlider != null)
                bombTimerSlider.value = 1.0f;

            if (bombImpactExplosionSlider != null)
                bombImpactExplosionSlider.value = 0.0f;

            if (TimerIconStoppedClipSource != null && timerIconAnimationPlayer != null)
                timerIconAnimationPlayer.Play(TimerIconStoppedClipSource, SpriteAnimationPlayMode.Loop);

            isFuseStarted = false;
        }

        private void HandlePlayerSpawned(PlayerMB player)
        {
            BindItemHandleState(player != null ? player.GetComponent<PlayerItemHandleStateMB>() : null);
        }

        private void HandleCurrentHandledItemChanged(ICarryableItem handledItem)
        {
            HandleCurrentBombChanged(ResolveBombFromHandledItem(handledItem));
        }

        // 現在持っている爆弾が爆発した。次の爆弾取得 or リセットまで Impact を MAX 固定にする。
        private void HandleCurrentBombExploded()
        {
            holdImpactMaxAfterExplode = true;
        }

        // ステージクリア / リロード / リセット時には Mark と Impact 固定を一度リセットする。
        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Reload:
                case GameState.ResetStage:
                case GameState.Goaling:
                case GameState.NextStage:
                case GameState.Starting:
                case GameState.Loading:
                case GameState.ReturnToTitle:
                    HideTimeBonusMark();
                    holdImpactMaxAfterExplode = false;
                    break;
            }
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
            gameLogicManager.ExplodedState += HandleCurrentBombExploded;
            HandlePlayerSpawned(gameLogicManager.PlayerInstance);
        }

        private void UnbindGameLogic()
        {
            if (gameLogicManager != null)
            {
                gameLogicManager.OnPlayerSpawned -= HandlePlayerSpawned;
                gameLogicManager.ExplodedState -= HandleCurrentBombExploded;
            }

            gameLogicManager = null;
        }

        private void TryBindGameState()
        {
            GameStateManagerMB manager = GameStateManagerMB.Instance;

            if (manager == null || ReferenceEquals(gameStateManager, manager))
                return;

            UnbindGameState();
            gameStateManager = manager;
            gameStateManager.StateMachine.Subscribe(HandleGameStateChanged);
        }

        private void UnbindGameState()
        {
            if (gameStateManager != null)
                gameStateManager.StateMachine.Unsubscribe(HandleGameStateChanged);

            gameStateManager = null;
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

        // Sprite と幅割合から Mark の GameObject(RectTransform)+Image を一度だけ生成し、非表示にしておく。
        private void EnsureTimeBonusMarkCreated()
        {
            if (timeBonusMarkInitialized)
                return;

            if (bombTimerSlider == null)
                return;

            RectTransform sliderRect = bombTimerSlider.transform as RectTransform;
            if (sliderRect == null)
                return;

            timeBonusMarkInitialized = true;

            var markObject = new GameObject("TimeBonusMark", typeof(RectTransform), typeof(Image));
            timeBonusMarkRect = markObject.GetComponent<RectTransform>();
            timeBonusMarkRect.SetParent(sliderRect, false);

            timeBonusMarkImage = markObject.GetComponent<Image>();
            timeBonusMarkImage.sprite = timeBonusMarkSprite;
            timeBonusMarkImage.raycastTarget = false;
            timeBonusMarkImage.enabled = timeBonusMarkSprite != null;
            timeBonusMarkBaseColor = timeBonusMarkImage.color; // 既定色(白)を基準として控える。

            // 縦はスライダー高さにストレッチ、横は割合で決める。位置は表示時に上書きする。
            timeBonusMarkRect.pivot = new Vector2(0.5f, 0.5f);
            timeBonusMarkRect.anchorMin = new Vector2(0.5f, 0f);
            timeBonusMarkRect.anchorMax = new Vector2(0.5f, 1f);
            timeBonusMarkRect.anchoredPosition = Vector2.zero;
            RefreshTimeBonusMarkSize();

            // 生成直後は非表示。必要時に位置決め+表示する。
            markObject.SetActive(false);
        }

        // 持っている爆弾の TotalFuseTime と現在ステージの閾値から境界線位置を求めて Mark を表示する。
        private void ShowTimeBonusMarkForBomb(BombMB targetBomb)
        {
            EnsureTimeBonusMarkCreated();

            if (timeBonusMarkRect == null || timeBonusMarkSprite == null || targetBomb == null || gameLogicManager == null)
                return;

            float totalFuseTime = targetBomb.TotalFuseTime;
            if (totalFuseTime <= 0f)
                return;

            // TimerSlider = RemainingFuseTime / TotalFuseTime。
            // 経過時間が閾値ちょうどの地点 = (TotalFuseTime - 閾値) / TotalFuseTime。
            float threshold = gameLogicManager.CurrentClearTimeThreshold;
            float normalized = Mathf.Clamp01((totalFuseTime - threshold) / totalFuseTime);

            PositionTimeBonusMark(normalized);
            timeBonusMarkNormalized = normalized;

            if (timeBonusMarkImage != null)
            {
                timeBonusMarkImage.enabled = true;
                // 新しい爆弾はフューズ満タン=未通過なので基準色に戻す。
                timeBonusMarkImage.color = timeBonusMarkBaseColor;
            }

            timeBonusMarkRect.gameObject.SetActive(true);
        }

        private void PositionTimeBonusMark(float normalized)
        {
            if (timeBonusMarkRect == null || bombTimerSlider == null)
                return;

            normalized = Mathf.Clamp01(normalized);

            // スライダーの向きに合わせて 0..1 を反転する（横向きスライダー前提）。
            float t = bombTimerSlider.direction == Slider.Direction.RightToLeft
                ? 1f - normalized
                : normalized;

            timeBonusMarkRect.anchorMin = new Vector2(t, 0f);
            timeBonusMarkRect.anchorMax = new Vector2(t, 1f);
            timeBonusMarkRect.pivot = new Vector2(0.5f, 0.5f);
            timeBonusMarkRect.anchoredPosition = Vector2.zero;

            RefreshTimeBonusMarkSize();
        }

        private void RefreshTimeBonusMarkSize()
        {
            if (timeBonusMarkRect == null || bombTimerSlider == null)
                return;

            RectTransform sliderRect = bombTimerSlider.transform as RectTransform;
            if (sliderRect == null)
                return;

            float sliderHeight = sliderRect.rect.height;
            float width = Mathf.Max(0f, sliderHeight * Mathf.Max(0f, timeBonusMarkWidthToHeightRatio));

            // 縦は anchor ストレッチに任せ、横幅だけ指定する。
            timeBonusMarkRect.sizeDelta = new Vector2(width, 0f);
        }

        private void HideTimeBonusMark()
        {
            if (timeBonusMarkRect != null)
                timeBonusMarkRect.gameObject.SetActive(false);

            timeBonusMarkNormalized = -1f;
        }

        // Slider が境界線を過ぎた(ボーナス不可になった)あとは Mark の明度を下げる。
        private void RefreshTimeBonusMarkBrightness()
        {
            if (timeBonusMarkImage == null || timeBonusMarkRect == null || timeBonusMarkNormalized < 0f)
                return;

            if (!timeBonusMarkRect.gameObject.activeSelf)
                return;

            float value = bombTimerSlider != null ? bombTimerSlider.value : 1f;
            bool passed = value < timeBonusMarkNormalized;

            if (passed)
            {
                // RGB を一律スカラー倍 = HSV の明度(Value)を下げる。色相・彩度は維持する。
                float brightness = Mathf.Clamp01(timeBonusMarkPassedBrightness);
                timeBonusMarkImage.color = new Color(
                    timeBonusMarkBaseColor.r * brightness,
                    timeBonusMarkBaseColor.g * brightness,
                    timeBonusMarkBaseColor.b * brightness,
                    timeBonusMarkBaseColor.a);
            }
            else
            {
                timeBonusMarkImage.color = timeBonusMarkBaseColor;
            }
        }
    }
}

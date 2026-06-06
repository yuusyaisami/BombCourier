// 爆弾が起動中(爆弾が存在しており、かつそれがカウントダウン中)の状態でプレイヤーがこのオブジェクトに触れると、アイテムを取得できます。
// 触れたら取得となります。

using System;
using BC.Base;
using BC.Bomb;
using BC.Manager;
using BC.Utility;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace BC.Item
{
    public sealed class BonusObjectMB : MonoBehaviour
    {
        [SerializeField] private BonusItemData itemData;
        [SerializeField] private ParticleSystem collectEffectPrefab; // アイテム取得時に再生するエフェクト
        [SerializeField] private MeshMaterialControllerMB materialController; // アイテムのマテリアルを制御するためのコンポーネント

        [EntityTagDropdown]
        [SerializeField] private EntityTagReference requiredPlayerTag = new EntityTagReference(); // このタグを持つEntityMBに触れたときのみアイテムを取得できるようにするためのフィールド
        private bool isCollected = false; // アイテムが既に取得されたかどうかを管理するフラグ
        // ゲットできる状態
        private bool canCollect;
        public BonusItemData ItemData => itemData;
        private Collider[] _colliders; // BonusObjectにアタッチされている全てのコライダーへの参照を保持する配列
        [SerializeField, Min(0f)] private float spinBaseSpeed = 90f; // アイテムが通常時に回転する速度 (度/秒)
        [SerializeField, Min(0f)] private float deniedCollectSpinBoost = 45f; // 取得できない状態で触れた時に一時的に加える速度
        [SerializeField, Min(0.01f)] private float spinReturnConvergence = 180f; // 一時加速した回転を基準速度へ戻す速さ
        private float targetSpinSpeed; // 触れたときに一時的に上がる回転速度の目標値
        private float currentSpinSpeed; // 現在の回転速度
        public event Action<BonusObjectMB> OnCollected; // アイテムが取得されたときに発火するイベント

        private Sequence collectSequence; // 取得演出のトゥイーン。リロードで途中中断できるよう参照を保持する。
        // リロード時に巻き戻すための初期スポーン状態 (もともとどこにあったのか) を保持する。
        private Transform originalParent;
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private Vector3 originalLocalScale;
        private float initialSpinBaseSpeed; // 取得時に 0 へ落とすため、初期回転速度を別途覚えておく。
        private bool hasCapturedOriginalState;

        public bool IsCollected => isCollected; // アイテムが既に取得されたかどうかを外部から参照できるようにするプロパティ
        private void Awake()
        {
            // BonusObjectにアタッチされている全てのコライダーへの参照を取得して保存する
            _colliders = GetComponentsInChildren<Collider>(true);

            // 起動時は通常の回転速度から開始して、常にその速度へ戻るようにする。
            targetSpinSpeed = spinBaseSpeed;
            currentSpinSpeed = spinBaseSpeed;

            // リロードで戻すために、初期スポーン位置と初期回転速度を控えておく。
            CaptureOriginalState();
        }
        private void Start()
        {
            RefreshCanCollectFromSceneBombState();

            // 爆弾のカウントダウン開始と終了のイベントにリスナーを登録する
            if (GameLogicManagerMB.Instance != null)
            {
                GameLogicManagerMB.Instance.OnStartBombFuse += OnStartBombFuse;
                GameLogicManagerMB.Instance.OnEndBombFuse += OnEndBombFuse;
            }
        }


        private void OnDestroy()
        {
            if (GameLogicManagerMB.Instance == null)
                return;

            GameLogicManagerMB.Instance.OnStartBombFuse -= OnStartBombFuse;
            GameLogicManagerMB.Instance.OnEndBombFuse -= OnEndBombFuse;
        }

        private void OnStartBombFuse(BombMB _)
        {
            RefreshCanCollectFromSceneBombState();
        }

        private void OnEndBombFuse()
        {
            RefreshCanCollectFromSceneBombState();
        }

        // シーン内の Bomb 状態から、取得可能フラグを毎回再計算する。
        private void RefreshCanCollectFromSceneBombState()
        {
            bool hasAnyFusingBomb = GameLogicManagerMB.Instance != null && GameLogicManagerMB.Instance.HasAnyFusingSceneBomb();
            SetCanCollect(hasAnyFusingBomb);
        }

        private void SetCanCollect(bool value)
        {
            canCollect = value;

            if (!isCollected)
            {
                // アイテムが取得できないときはAlpha値を0.5にする
                float targetAlpha = canCollect ? 1f : 0.5f;
                materialController.SetAlpha(targetAlpha);
            }
        }
        private void Update()
        {
            // 複数 Bomb でも正しい取得可否になるよう、毎フレーム同期する。
            RefreshCanCollectFromSceneBombState();

            // 一時的な加速が入っていても、毎フレーム基準速度へ戻していく。
            currentSpinSpeed = Mathf.MoveTowards(currentSpinSpeed, targetSpinSpeed, spinReturnConvergence * Time.deltaTime);

            // アイテムを回転させる
            transform.Rotate(Vector3.up, currentSpinSpeed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected || !canCollect)
            {
                // 取得不可の接触時だけ少しだけ加速させ、Update側で基準速度へ戻す。
                currentSpinSpeed += deniedCollectSpinBoost;
                targetSpinSpeed = spinBaseSpeed;
                return; // 既に取得されている場合は何もしない
            }

            if (other.TryGetComponent<EntityMB>(out var entityMB))
            {
                // プレイヤーのEntityMBに触れた場合のみアイテムを取得できるようにする
                if (entityMB.Tag == requiredPlayerTag.Id)
                {
                    // 取得！
                    Collect().Forget();
                    for (int i = 0; i < _colliders.Length; i++)
                    {
                        _colliders[i].enabled = false; // アイテムが取得された後は全てのコライダーを無効にして、再度触れないようにする
                    }
                }
            }
        }
        private async UniTask Collect()
        {
            isCollected = true;
            canCollect = false;
            spinBaseSpeed = 0f; // アイテムが取得されたら回転を止める
            targetSpinSpeed = 0f; // アイテムが取得されたら回転速度の目標値も0にする
            currentSpinSpeed = 0f; // アイテムが取得されたら現在の回転速度も0にする

            // エフェクト再生
            if (collectEffectPrefab != null)
            {
                var collectEffect = Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
                collectEffect.Play();
            }

            // 取得演出は 1 本の Sequence にまとめる。リロードで中断された際に
            // 「2つ目の await が新しいトゥイーンを生成して巻き戻し後のスケールを潰す」事故を防ぐ。
            collectSequence?.Kill();
            collectSequence = DOTween.Sequence();
            collectSequence.Append(transform.DOMoveY(transform.position.y + 2f, 0.5f).SetEase(Ease.OutQuad));
            collectSequence.Append(transform.DOScale(new Vector3(1, 0, 1), 0.5f).SetEase(Ease.InQuad));
            await collectSequence.AsyncWaitForCompletion();
            collectSequence = null;

            // リロードなどで取得状態が巻き戻された場合は取得通知を出さない。
            if (!isCollected)
                return;

            OnCollected?.Invoke(this); // アイテムが取得されたことを通知する
        }

        // 初期スポーン状態を一度だけ控える。これがリロード時に戻す「もともとの位置」になる。
        private void CaptureOriginalState()
        {
            if (hasCapturedOriginalState)
                return;

            originalParent = transform.parent;
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            originalLocalScale = transform.localScale;
            initialSpinBaseSpeed = spinBaseSpeed;
            hasCapturedOriginalState = true;
        }

        // リロード用に「取得済みかどうか」を保存する。
        // チェックポイントは爆弾を持つ前に取られるため、通常この値は未取得 (false) になる。
        public object CaptureRetryCheckpointState()
        {
            return new BonusCheckpointState(isCollected);
        }

        // 保存した取得状態と初期スポーン位置へ巻き戻す。
        public void RestoreRetryCheckpointState(object state)
        {
            CaptureOriginalState();

            bool wasCollected = state is BonusCheckpointState bonusState && bonusState.IsCollected;

            // 進行中の取得演出を止めてから初期状態へ戻す。
            if (collectSequence != null)
            {
                collectSequence.Kill();
                collectSequence = null;
            }

            // 初期スポーン位置 (もともとどこにあったのか) へ戻す。
            if (originalParent != null && transform.parent != originalParent)
                transform.SetParent(originalParent, false);

            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
            transform.localScale = wasCollected
                ? new Vector3(originalLocalScale.x, 0f, originalLocalScale.z) // 取得済みなら潰したまま隠す。
                : originalLocalScale;

            isCollected = wasCollected;
            spinBaseSpeed = wasCollected ? 0f : initialSpinBaseSpeed;
            targetSpinSpeed = spinBaseSpeed;
            currentSpinSpeed = spinBaseSpeed;

            if (_colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i] != null)
                        _colliders[i].enabled = !wasCollected; // 取得済みでなければ再度触れられるように戻す。
                }
            }

            if (wasCollected)
            {
                canCollect = false;
            }
            else
            {
                // 現在の爆弾状態から取得可否とアルファ表示を再計算する。
                RefreshCanCollectFromSceneBombState();
            }
        }

        private sealed class BonusCheckpointState
        {
            public BonusCheckpointState(bool isCollected)
            {
                IsCollected = isCollected;
            }

            public bool IsCollected { get; }
        }
    }
}

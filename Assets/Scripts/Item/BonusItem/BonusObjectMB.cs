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

        public bool IsCollected => isCollected; // アイテムが既に取得されたかどうかを外部から参照できるようにするプロパティ
        private void Awake()
        {
            // BonusObjectにアタッチされている全てのコライダーへの参照を取得して保存する
            _colliders = GetComponentsInChildren<Collider>(true);

            // 起動時は通常の回転速度から開始して、常にその速度へ戻るようにする。
            targetSpinSpeed = spinBaseSpeed;
            currentSpinSpeed = spinBaseSpeed;
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
            await transform.DOMoveY(transform.position.y + 2f, 0.5f).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
            await transform.DOScale(new Vector3(1, 0, 1), 0.5f).SetEase(Ease.InQuad).AsyncWaitForCompletion();
            OnCollected?.Invoke(this); // アイテムが取得されたことを通知する
        }
    }
}

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
        private float spinBaseSpeed = 90f; // アイテムが回転する速度 (度/秒)
        private float targetSpinConvergence = 3f; // 回転速度がBaseに収束するセンシティブ
        private float currentsSpinConvergence = 0f; // 回転速度がBaseに収束する
        private float targetSpinSpeed; // 触れたときに一時的に上がる回転速度の目標値
        private float currentSpinSpeed; // 現在の回転速度
        public event Action<BonusObjectMB> OnCollected; // アイテムが取得されたときに発火するイベント

        public bool IsCollected => isCollected; // アイテムが既に取得されたかどうかを外部から参照できるようにするプロパティ
        private void Awake()
        {
            // BonusObjectにアタッチされている全てのコライダーへの参照を取得して保存する
            _colliders = GetComponentsInChildren<Collider>(true);
        }
        private void Start()
        {
            SetCanCollect(GameStateManagerMB.Instance != null && GameStateManagerMB.Instance.CurrentState == GameState.FusePlaying);

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
            SetCanCollect(true); // 爆弾のカウントダウンが開始されたらアイテムを取得できる状態にする
        }

        private void OnEndBombFuse()
        {
            SetCanCollect(false); // 爆弾のカウントダウンが終了したらアイテムを取得できない状態にする
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
            // 回転速度を目標値に向かって徐々に変化させる
            if (currentSpinSpeed != targetSpinSpeed)
            {
                currentSpinSpeed = Mathf.MoveTowards(currentSpinSpeed, targetSpinSpeed, currentsSpinConvergence * Time.deltaTime);
                if (currentSpinSpeed == targetSpinSpeed)
                {
                    currentsSpinConvergence = 0f; // 目標値に到達したら収束速度をリセットする
                }
            }
            else
            {
                currentsSpinConvergence = Mathf.Lerp(currentsSpinConvergence, targetSpinConvergence, 1f * Time.deltaTime); // 目標値に到達していないときは収束速度を設定する
            }

            // アイテムを回転させる
            transform.Rotate(Vector3.up, currentSpinSpeed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected || !canCollect)
            {
                currentSpinSpeed += spinBaseSpeed / 2; // 既に取得されている場合や取得できない状態のときは、触れたら回転速度が一時的に上がるようにする
                currentsSpinConvergence = 0f; // 回転速度がBaseに収束する速度を速くする
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

            // カメラ取得
            var mainCamera = UnityEngine.Camera.main;

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

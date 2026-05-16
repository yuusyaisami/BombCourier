using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
namespace BC.UI
{
    // 落下エフェクトのUIロジックを管理するクラス
    public class UIFallEffectMB : MonoBehaviour, IUIFallEffect
    {
        [SerializeField] private CanvasGroup fallEffectCanvasGroup;
        [SerializeField] private FallEffectSettings settings = new FallEffectSettings();
        [SerializeField] private GameObject fallEffectPrefab;
        [SerializeField] private float spawnInterval = 0.1f; // 落下エフェクトのスポーン間隔
        [InlineProperty]
        [SerializeField] private FallEffectElementData[] fallEffectElements;
        [SerializeField] private Vector2 spawnAreaOffset; // local spaceを基準にしている
        [SerializeField] private Vector2 spawnAreaSize; // local spaceを基準にしている
        // pool管理
        private ObjectPool<Image> fallEffectPool;
        private float totalWeight;
        private float effectTimer;
        private bool isPlaying;
        private float endTimer; // 一回再生モードの終了タイマー

        private void Awake()
        {
            DisableInputBlocking();
        }

        void Start()
        {
            // 初期化処理
            // pool 初期化
            fallEffectPool = new ObjectPool<Image>(
                createFunc: () =>
                {
                    Image image = Instantiate(fallEffectPrefab, transform).GetComponent<Image>();
                    if (image != null)
                    {
                        image.raycastTarget = false;
                    }

                    return image;
                },
                actionOnGet: obj =>
                {
                    if (obj != null)
                    {
                        obj.raycastTarget = false;
                        obj.gameObject.SetActive(true);
                    }
                },
                actionOnRelease: obj => obj.gameObject.SetActive(false),
                actionOnDestroy: obj => Destroy(obj.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 200
            );
            // 総重量の計算
            totalWeight = 0;
            foreach (var element in fallEffectElements)
            {
                totalWeight += element.weight;
            }
        }
        Image SpawnFallEffect()
        {
            // プールから落下エフェクトを取得
            var effectObj = fallEffectPool.Get();
            // ランダムな位置に配置
            Vector2 randomPos = new Vector2(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2)
            );
            randomPos += spawnAreaOffset;
            effectObj.rectTransform.anchoredPosition = randomPos;
            // ランダムなスプライトを選択
            float randomValue = Random.Range(0, totalWeight);
            float cumulativeWeight = 0f;
            foreach (var element in fallEffectElements)
            {
                cumulativeWeight += element.weight;
                if (randomValue <= cumulativeWeight)
                {
                    effectObj.sprite = element.sprite;
                    break;
                }
            }
            return effectObj;
        }

        // Update is called once per frame
        void Update()
        {
            if (isPlaying)
            {
                effectTimer += Time.deltaTime;
                if (effectTimer >= spawnInterval)
                {
                    effectTimer = 0f;
                    SpawnFallEffect();
                }
                if (endTimer > 0f && endTimer != -1f)
                {
                    endTimer -= Time.deltaTime;
                    if (endTimer <= 0f)
                    {
                        EndFallEffect();
                    }
                }
            }
        }
        public void StartFallEffect(FallEffectPlayMode playMode = FallEffectPlayMode.Loop)
        {
            // 落下エフェクトの開始処理
            if (playMode == FallEffectPlayMode.Loop)
            {
                isPlaying = true;
                endTimer = -1f; // 終了タイマーは無効
            }
            else if (playMode == FallEffectPlayMode.Once)
            {
                // 一回再生の場合は、一定時間後に停止する
                isPlaying = true;
                endTimer = settings.Duration;
            }
        }

        public void EndFallEffect()
        {
            // 落下エフェクトの終了処理
            isPlaying = false;
            endTimer = -1f;
        }

        private void DisableInputBlocking()
        {
            if (fallEffectCanvasGroup == null)
            {
                return;
            }

            // 画面演出であり UI 操作対象ではないので、常に raycast を通す。
            fallEffectCanvasGroup.interactable = false;
            fallEffectCanvasGroup.blocksRaycasts = false;
        }

#if UNITY_EDITOR
        // Area Gizmoの表示
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            RectTransform rectTransform = transform as RectTransform;

            if (rectTransform != null)
            {
                // anchoredPosition を基準に、親ローカル空間へ矩形を描画する。
                Matrix4x4 previousMatrix = Gizmos.matrix;
                Transform parentTransform = rectTransform.parent;
                Gizmos.matrix = parentTransform != null ? parentTransform.localToWorldMatrix : Matrix4x4.identity;

                Vector2 center = rectTransform.anchoredPosition + spawnAreaOffset;
                Vector3 size = new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0.01f);
                Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0f), size);

                Gizmos.matrix = previousMatrix;
                return;
            }

            // RectTransform ではない場合は従来のワールド座標で描画する。
            Vector3 fallbackCenter = transform.position + (Vector3)spawnAreaOffset;
            Vector3 fallbackSize = new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0.01f);
            Gizmos.DrawWireCube(fallbackCenter, fallbackSize);
        }
#endif
    }
}
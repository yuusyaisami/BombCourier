using BC.Audio;
using BC.Base;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BC.Player
{
    // プレイヤーの足音を再生するコンポーネント。
    //
    // ── セットアップ方法 ──────────────────────────────────────────────────
    //
    // 1. このコンポーネントをプレイヤー Prefab の "Armature" など
    //    Animator を持つ GameObject に AddComponent する。
    //
    // 2. Inspector で footstepClips に足音クリップを 1 つ以上設定する
    //    (複数登録するとランダムに選ばれるので単調にならない)。
    //
    // 3. Unity Editor のアニメーションウィンドウで Walk / Run クリップを開き、
    //    足が地面に着地するフレームに Animation Event を追加する。
    //    Event の Function に「OnFootstep」と入力して保存する。
    //
    // 4. 走り足音を歩きと別にしたい場合は walkClips / runClips を使う。
    //    isSpeedSplitEnabled = true にして speedSplitThreshold で閾値を設定する。
    //
    // ────────────────────────────────────────────────────────────────────
    [DisallowMultipleComponent]
    public sealed class PlayerFootstepMB : MonoBehaviour
    {
        [Title("歩き足音")]
        [SerializeField] private AudioDataSO[] walkClips = System.Array.Empty<AudioDataSO>();

        [Title("走り足音 (空の場合は歩きクリップを共用)")]
        [SerializeField] private AudioDataSO[] runClips = System.Array.Empty<AudioDataSO>();

        [Title("速度分岐")]
        [Tooltip("true にすると NormalizedSpeed がしきい値以上のとき runClips を使う。")]
        [SerializeField] private bool isSpeedSplitEnabled = false;

        [SerializeField, ShowIf(nameof(isSpeedSplitEnabled)), Range(0f, 1f)]
        private float speedSplitThreshold = 0.7f;

        // Animator の同一 GameObject か親に PlayerAnimationMB があれば自動解決する。
        // 手動で指定したい場合は Inspector から設定する。
        [Title("参照 (任意)")]
        [SerializeField] private PlayerAnimationMB playerAnimation;

        private Animator cachedAnimator;

        private void Awake()
        {
            cachedAnimator = GetComponent<Animator>();
            if (cachedAnimator == null)
                cachedAnimator = GetComponentInParent<Animator>(true);

            if (playerAnimation == null)
                playerAnimation = GetComponentInParent<PlayerAnimationMB>(true);
        }

        // ─────────────────────────────────────────────────────────────────
        // Animation Event から呼ばれるメソッド
        // ─────────────────────────────────────────────────────────────────

        // アニメーションクリップ上の Animation Event の Function に
        // 「OnFootstep」と入力することで、着地フレームで自動的に呼ばれる。
        public void OnFootstep()
        {
            AudioDataSO clip = PickClip();
            if (clip == null)
                return;

            if (AudioSystemMB.Instance != null)
                AudioSystemMB.Instance.PlaySE(clip);
        }

        // ─────────────────────────────────────────────────────────────────
        // 内部ロジック
        // ─────────────────────────────────────────────────────────────────

        private AudioDataSO PickClip()
        {
            AudioDataSO[] pool = ResolvePool();
            if (pool == null || pool.Length == 0)
                return null;

            // 複数登録されていればランダムに選ぶ。
            return pool[Random.Range(0, pool.Length)];
        }

        private AudioDataSO[] ResolvePool()
        {
            if (!isSpeedSplitEnabled)
                return walkClips.Length > 0 ? walkClips : null;

            // 現在の Animator の CurrentSpeed パラメーターで走り/歩きを判定する。
            float speed = GetNormalizedSpeed();
            bool isSprinting = speed >= speedSplitThreshold;

            if (isSprinting && runClips.Length > 0)
                return runClips;

            return walkClips.Length > 0 ? walkClips : null;
        }

        private float GetNormalizedSpeed()
        {
            if (cachedAnimator == null)
                return 0f;

            // PlayerAnimationMB と同じパラメーター名 "CurrentSpeed" を参照する。
            return cachedAnimator.GetFloat("CurrentSpeed");
        }
    }
}

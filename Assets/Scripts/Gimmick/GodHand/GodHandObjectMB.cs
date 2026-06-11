using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
namespace BC.Gimmick
{
    public interface IGodHandCatchTarget
    {
        bool CanBeCaughtByGodHand { get; }

        void OnCaughtByGodHand(Transform catchTransform);
        void OnReleasedByGodHand();
    }
    public class GodHandObjectMB : MonoBehaviour
    {
        [SerializeField] private Transform catchTransform; // GodHandにつかまったときParent位置
        [SerializeField] private Vector3 originalPosition; // 初期位置
        [SerializeField] private Vector3 targetPosition; // 移動目標位置 (デフォルト値)

        private IGodHandCatchTarget catchTarget; // つかまる対象のEntity契約

        // 進行中の移動 Tween を保持する。scene reload などで本オブジェクトが
        // 破棄されたとき、await 継続が破棄済み Transform に触れて
        // MissingReferenceException を出す前に、OnDestroy で確実に止めるための参照。
        private Tween activeMoveTween;

        public Vector3 TargetPosition => targetPosition;
        public Vector3 OriginalPosition => originalPosition;
        public void Catch(IGodHandCatchTarget target)
        {
            if (catchTransform == null)
            {
                Debug.LogError($"{nameof(GodHandObjectMB)}: catchTransform is not assigned.", this);
                return;
            }

            if (target == null || !target.CanBeCaughtByGodHand)
                return;

            // すでにつかまっている対象がある場合はリリースしてから新しい対象をつかむ
            if (catchTarget != null)
            {
                Release();
            }

            catchTarget = target;
            catchTarget.OnCaughtByGodHand(catchTransform);
            Debug.Log($"{nameof(GodHandObjectMB)}: Caught {target}.", this);
        }

        public void Release()
        {
            if (catchTarget != null)
            {
                catchTarget.OnReleasedByGodHand();
                catchTarget = null;
            }
        }
        public void SetOriginalPosition()
        {
            transform.position = originalPosition;
        }
        public void SetTargetPosition()
        {
            transform.position = targetPosition;
        }

        private void OnDestroy()
        {
            // 進行中の移動 Tween を即時停止し、await 継続が破棄済み Transform を触る前に止める。
            activeMoveTween?.Kill();
            activeMoveTween = null;

            // GodHand 破棄後に対象が「つかまれた」状態の参照を握ったままにならないよう手放す。
            // ただし scene teardown では対象側も破棄され得るため、ここでは OnReleasedByGodHand を
            // 呼ばず参照を切るだけにする（通常の解放は Release() 経路に委ねる）。
            catchTarget = null;
        }
        public async UniTask MoveToAsync(Vector3 targetPosition, float duration)
        {
            // 直前の移動 Tween が残っていれば畳んでから始める（同時2本走行で位置が競合するのを防ぐ）。
            activeMoveTween?.Kill();
            activeMoveTween = transform.DOMove(targetPosition, duration).SetEase(Ease.InOutSine);
            await activeMoveTween.AsyncWaitForCompletion();
            activeMoveTween = null;

            // await 中に GameObject が Destroy された場合（stage reload / シーン遷移）、
            // 破棄済み Transform へ書き込むと例外になる。Unity の fake-null を見て安全に打ち切る。
            // 呼び出し側の await 契約を変えないため、ここでは throw せず静かに return する。
            if (this == null)
                return;

            transform.position = targetPosition;
        }
        public async UniTask MoveToAsync(float duration)
        {
            await MoveToAsync(targetPosition, duration);
        }

#if UNITY_EDITOR
        [ContextMenu("Set Original Position"), Button("Set Original Position")]
        private void SetEditorOriginalPosition()
        {
            originalPosition = transform.position;
        }
        [ContextMenu("Set Target Position"), Button("Set Target Position")]
        private void SetEditorTargetPosition()
        {
            targetPosition = transform.position;
        }
# endif
    }
}
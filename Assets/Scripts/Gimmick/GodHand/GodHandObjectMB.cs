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
        public async UniTask MoveToAsync(Vector3 targetPosition, float duration)
        {
            Tween moveTween = transform.DOMove(targetPosition, duration).SetEase(Ease.InOutSine);
            await moveTween.AsyncWaitForCompletion();
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
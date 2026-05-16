using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
namespace BC.Gimmick
{
    public class GodHandObjectMB : MonoBehaviour
    {
        [SerializeField] private Transform catchTransform; // GodHandにつかまったときParent位置
        [SerializeField] private Vector3 originalPosition; // 初期位置
        [SerializeField] private Vector3 targetPosition; // 移動目標位置 (デフォルト値)

        private Transform targetTransform; // つかまる対象のTransform
        private Transform releaseTargetParent; // つかまる対象の元の親Transform
        public void Catch(Transform targetTransform)
        {
            // すでにつかまっている対象がある場合はリリースしてから新しい対象をつかむ
            if (this.targetTransform != null)
            {
                Release();
            }
            this.targetTransform = targetTransform;
            this.releaseTargetParent = targetTransform.parent;
            targetTransform.SetParent(catchTransform);
            targetTransform.localPosition = Vector3.zero;
            targetTransform.localRotation = Quaternion.identity;
        }

        public void Release()
        {
            if (targetTransform != null)
            {
                targetTransform.SetParent(releaseTargetParent);
                targetTransform = null;
                releaseTargetParent = null;
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
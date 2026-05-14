using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
namespace BC.Gimmick
{
    public class GodHandObjectMB : MonoBehaviour
    {
        [SerializeField] private Transform catchTransform; // GodHandにつかまったときParent位置
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
        public async UniTask MoveToAsync(Vector3 targetPosition, float duration)
        {
            Tween moveTween = transform.DOMove(targetPosition, duration).SetEase(Ease.InOutSine);
            await moveTween.AsyncWaitForCompletion();
            transform.position = targetPosition;
        }
    }
}
using UnityEngine;

namespace BC.Player
{
    // アイテムを持つときのソケット（親オブジェクト）を、手の動きに追従させるためのクラス。
    public sealed class ItemHoldSocketFollower : MonoBehaviour
    {
        [SerializeField] private Transform handBone;
        [SerializeField] private Transform socket;

        [Header("Offset From Hand")]
        [SerializeField] private Vector3 localPositionOffset;
        [SerializeField] private Vector3 localEulerOffset;

        public Transform Socket => socket;

        private void LateUpdate()
        {
            if (handBone == null || socket == null)
                return;

            Quaternion offsetRotation = Quaternion.Euler(localEulerOffset);

            socket.position = handBone.TransformPoint(localPositionOffset);
            socket.rotation = handBone.rotation * offsetRotation;

            // 重要。socket は絶対に scale を継承させない。
            socket.localScale = Vector3.one;
        }
    }
}
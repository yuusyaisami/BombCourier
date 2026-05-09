using BC.Base;
using UnityEngine;
namespace BC.Player
{
    public sealed class PlayerItemHandleStateMB : MonoBehaviour, IEntityHandleItemAnimationSource
    {
        [SerializeField] private float handleItemDistance = 1.5f; // アイテムを扱える距離
        [SerializeField] private float handleItemAngleThreshold = 45f; // アイテムを扱える角度の閾値
        [SerializeField] private bool isHandlingItem;

        public bool IsHandlingItem => isHandlingItem;

        public void SetHandlingItem(bool value)
        {
            isHandlingItem = value;
        }
    }
}
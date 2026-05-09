using BC.Base;
using UnityEngine;

public sealed class PlayerItemHandleStateMB : MonoBehaviour, IEntityHandleItemAnimationSource
{
    [SerializeField] private bool isHandlingItem;
    [SerializeField]
    private

    public bool IsHandlingItem => isHandlingItem;

    public void SetHandlingItem(bool value)
    {
        isHandlingItem = value;
    }
}
using BC.Utility;
using UnityEngine;
namespace BC.Base
{

    public interface IEntityHandleItemAnimationSource
    {
        bool IsHandlingItem { get; }
    }

    public class PlayerMB : MonoBehaviour
    {

        public void TeleportToSpawnPoint(Vector3 position = default, Quaternion rotation = default)
        {

        }
    }
}
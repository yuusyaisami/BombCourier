using BC.Base;
using Unity.Cinemachine;
using UnityEngine;

namespace BC.Gimmick
{
    public interface IEntityTrigger
    {
        // ゴールルーム内のオブジェクトが特定の条件を満たしたときに呼び出されるイベント
        public event System.Action OnTrigger;
    }
    // このクラスは、ゴールルーム内のオブジェクトの挙動を管理するためのMonoBehaviourです。現在は空ですが、将来的にゴールルーム内の特定のオブジェクトに対して特別な挙動を追加するために使用される予定です。
    public class EntityTriggerObjectMB : MonoBehaviour, IEntityTrigger
    {
        public EntityTagId filterTag;
        public event System.Action OnTrigger;

        // OnTriggerEnterなどのUnityのイベント関数を使用して、特定の条件が満たされたときにOnTriggerイベントを呼び出すことができます。例えば、プレイヤーが特定のエリアに入ったときや、特定のオブジェクトと接触したときなどにイベントを発火させることができます。
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out EntityMB entity) && entity.Tag == filterTag)
            {
                OnTrigger?.Invoke();
            }
        }
    }
}
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
    // filterTag に一致する Entity がトリガーゾーンに入ったことを検出し、OnTrigger を発火する。
    // 主にゴール検出（BreakableGate の OnGoalTriggered 等）に使われる。
    public class EntityTriggerObjectMB : MonoBehaviour, IEntityTrigger
    {
        [EntityTagDropdown]
        public EntityTagReference filterTag;
        public event System.Action OnTrigger;

        // ゾーン内に存在する「資格を満たすコライダー」の数。
        // プレイヤーが複数コライダー（体・足など）を持つ場合や複数エンティティが同時侵入する場合に
        // OnTrigger が多重発火しないよう、0→1 の遷移時にだけ発火させるためのカウンタ。
        // 購読側が ChangeState(Goaling) を呼ぶが StateMachine.ChangeState は冪等でないため、
        // 多重発火するとゴール演出などが重複実行されてしまう。それを防ぐのが目的。
        private int qualifyingContactCount;

        private void OnTriggerEnter(Collider other)
        {
            if (!IsQualifyingEntity(other))
                return;

            qualifyingContactCount++;

            // ゾーンが「空」から「在室」へ変わった最初の1回だけ通知する。
            if (qualifyingContactCount == 1)
                OnTrigger?.Invoke();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsQualifyingEntity(other))
                return;

            // 破棄などで Exit を取りこぼしてもカウンタが負にならないようガードする
            // （負になると再侵入時の 0→1 判定がずれて発火しなくなる）。
            qualifyingContactCount = Mathf.Max(0, qualifyingContactCount - 1);
        }

        private bool IsQualifyingEntity(Collider other)
        {
            // OnTriggerEnter/Exit の other は Unity が常に有効値を渡すため null 判定は不要。
            return other.TryGetComponent(out EntityMB entity) && entity.Tag == filterTag.Id;
        }
    }
}
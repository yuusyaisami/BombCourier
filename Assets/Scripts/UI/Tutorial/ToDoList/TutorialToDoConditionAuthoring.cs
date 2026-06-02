using System;
using BC.Base;
using UnityEngine;

namespace BC.UI
{
    // チュートリアル ToDoList アイテムの達成条件の基底クラス。
    // [SerializeReference] でポリモーフィックにオーサリングする。
    // 達成されたら onComplete(index) を呼んで購読を終了する。
    [Serializable]
    public abstract class TutorialToDoConditionAuthoring
    {
        /// <summary>
        /// 条件監視を開始する。達成時に onComplete(index) を1度だけ呼ぶ。
        /// store または playerEntity が無効な場合はバインドしない。
        /// </summary>
        public abstract void Subscribe(ValueStoreService store, EntityRef playerEntity,
                                       Action<int> onComplete, int index);

        /// <summary>監視を解除し、内部リソースを破棄する。</summary>
        public abstract void Unsubscribe();
    }

    // ------------------------------------------------------------------
    // 自動監視なし。外部から UITutorialToDoListMB.SetItemCompleted(i) を手動で呼ぶ。
    // ------------------------------------------------------------------
    [Serializable]
    public sealed class ManualOnlyConditionAuthoring : TutorialToDoConditionAuthoring
    {
        public override void Subscribe(ValueStoreService store, EntityRef playerEntity,
                                       Action<int> onComplete, int index)
        { }
        public override void Unsubscribe() { }
    }

    // ------------------------------------------------------------------
    // 指定した ValueKey<bool> が targetValue と一致したとき達成する。
    // ------------------------------------------------------------------
    [Serializable]
    public sealed class WatchValueKeyBoolConditionAuthoring : TutorialToDoConditionAuthoring
    {
        [ValueKeyDropdown(typeof(bool))]
        [SerializeField] private ValueKeyReference keyRef;

        [Tooltip("この値になったとき達成とみなす。")]
        [SerializeField] private bool targetValue = true;

        private EventSubscription subscription;

        public override void Subscribe(ValueStoreService store, EntityRef playerEntity,
                                       Action<int> onComplete, int index)
        {
            if (store == null || !keyRef.IsAssigned) return;

            ValueWatchHandle<bool> handle = store.GetHandle<bool>(playerEntity, keyRef);

            // 現在値が既に条件を満たしている場合は即完了
            if (handle.CurrentValue == targetValue)
            {
                onComplete(index);
                return;
            }

            subscription = handle.Subscribe(value =>
            {
                if (value != targetValue) return;
                subscription?.Dispose();
                subscription = null;
                onComplete(index);
            });
        }

        public override void Unsubscribe()
        {
            subscription?.Dispose();
            subscription = null;
        }
    }

    // ------------------------------------------------------------------
    // 整数値比較演算子
    // ------------------------------------------------------------------
    public enum ToDoCompareOp
    {
        Equal,
        GreaterOrEqual,
        LessOrEqual,
    }

    // ------------------------------------------------------------------
    // 指定した ValueKey<int> が条件を満たしたとき達成する。
    // ------------------------------------------------------------------
    [Serializable]
    public sealed class WatchValueKeyIntConditionAuthoring : TutorialToDoConditionAuthoring
    {
        [ValueKeyDropdown(typeof(int))]
        [SerializeField] private ValueKeyReference keyRef;

        [SerializeField] private int compareValue;
        [SerializeField] private ToDoCompareOp op = ToDoCompareOp.GreaterOrEqual;

        private EventSubscription subscription;

        public override void Subscribe(ValueStoreService store, EntityRef playerEntity,
                                       Action<int> onComplete, int index)
        {
            if (store == null || !keyRef.IsAssigned) return;

            ValueWatchHandle<int> handle = store.GetHandle<int>(playerEntity, keyRef);

            if (Matches(handle.CurrentValue))
            {
                onComplete(index);
                return;
            }

            subscription = handle.Subscribe(value =>
            {
                if (!Matches(value)) return;
                subscription?.Dispose();
                subscription = null;
                onComplete(index);
            });
        }

        public override void Unsubscribe()
        {
            subscription?.Dispose();
            subscription = null;
        }

        private bool Matches(int value) => op switch
        {
            ToDoCompareOp.Equal => value == compareValue,
            ToDoCompareOp.GreaterOrEqual => value >= compareValue,
            ToDoCompareOp.LessOrEqual => value <= compareValue,
            _ => false,
        };
    }
}

using System;
using System.Collections.Generic;
using BC.Base;
using UnityEngine;

namespace BC.UI
{
    // 操作ガイドのエントリが表示されるべき条件の基底クラス。
    // UIOperationGuideMB の Update ごとに Evaluate が呼ばれる。軽量に保つこと。
    [Serializable]
    public abstract class OperationVisibilityConditionAuthoring
    {
        /// <summary>
        /// プレイヤーエンティティの現在の ValueStore 状態からこのエントリを表示すべきかを返す。
        /// store または playerEntity が無効な場合は false を返すこと。
        /// </summary>
        public abstract bool Evaluate(ValueStoreService store, EntityRef playerEntity);
    }

    // 常に表示する
    [Serializable]
    public sealed class AlwaysVisibleCondition : OperationVisibilityConditionAuthoring
    {
        public override bool Evaluate(ValueStoreService store, EntityRef playerEntity) => true;
    }

    // 移動入力が許可されているとき
    [Serializable]
    public sealed class CanMoveCondition : OperationVisibilityConditionAuthoring
    {
        public override bool Evaluate(ValueStoreService store, EntityRef playerEntity)
        {
            if (store == null) return false;
            return store.Get(playerEntity, ValueKeys.Move.CanMoveByInput);
        }
    }

    // 移動入力が許可されており、かつ接地しているとき（ジャンプ可能）
    [Serializable]
    public sealed class CanJumpCondition : OperationVisibilityConditionAuthoring
    {
        public override bool Evaluate(ValueStoreService store, EntityRef playerEntity)
        {
            if (store == null) return false;
            return store.Get(playerEntity, ValueKeys.Move.CanMoveByInput)
                && store.Get(playerEntity, ValueKeys.Runtime.IsGrounded);
        }
    }

    // アイテムを手に持っているとき
    [Serializable]
    public sealed class IsHoldingItemCondition : OperationVisibilityConditionAuthoring
    {
        public override bool Evaluate(ValueStoreService store, EntityRef playerEntity)
        {
            if (store == null) return false;
            return store.Get(playerEntity, ValueKeys.Runtime.IsHandlingItem);
        }
    }

    // アイテムを拾える状態のとき（CanCarry が true かつ アイテム未保持）
    [Serializable]
    public sealed class CanCarryCondition : OperationVisibilityConditionAuthoring
    {
        public override bool Evaluate(ValueStoreService store, EntityRef playerEntity)
        {
            if (store == null) return false;
            return store.Get(playerEntity, ValueKeys.Item.CanCarry)
                && !store.Get(playerEntity, ValueKeys.Runtime.IsHandlingItem);
        }
    }

    // インタラクト可能なとき
    [Serializable]
    public sealed class CanInteractCondition : OperationVisibilityConditionAuthoring
    {
        public override bool Evaluate(ValueStoreService store, EntityRef playerEntity)
        {
            if (store == null) return false;
            return store.Get(playerEntity, ValueKeys.Interaction.CanInteract);
        }
    }

    // 複数の bool ValueKey がすべて true のとき表示するカスタム条件
    [Serializable]
    public sealed class ValueKeyBoolAndCondition : OperationVisibilityConditionAuthoring
    {
        [Tooltip("すべてが true のとき表示する ValueKey の一覧。")]
        [ValueKeyDropdown(typeof(bool))]
        [SerializeField] private List<ValueKeyReference> keys = new();

        public override bool Evaluate(ValueStoreService store, EntityRef playerEntity)
        {
            if (store == null || keys == null) return false;
            foreach (ValueKeyReference key in keys)
            {
                if (!key.IsAssigned) continue;
                if (!store.Get<bool>(playerEntity, key))
                    return false;
            }
            return true;
        }
    }
}

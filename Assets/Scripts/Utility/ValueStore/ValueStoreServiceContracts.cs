namespace BC.Base
{
    // Entity単位のStore。EntityRefごとに独立したValueStoreScopeを持つ。
    public interface IEntityValueStoreService
    {
        T Get<T>(EntityRef entity, ValueKeyReference key);
        T Get<T>(EntityRef entity, ValueKey<T> key);

        // 毎フレーム監視する値はGetを繰り返さず、HandleのCurrentValue/Versionを読む。
        ValueWatchHandle<T> GetHandle<T>(EntityRef entity, ValueKeyReference key);
        ValueWatchHandle<T> GetHandle<T>(EntityRef entity, ValueKey<T> key);
        bool Set<T>(EntityRef entity, ValueKeyReference key, T value);
        bool Set<T>(EntityRef entity, ValueKey<T> key, T value);
        bool SetAdd(EntityRef entity, ValueKeyReference key, ValueModifierTagId tag, float value);
        bool SetAdd(EntityRef entity, ValueKey<float> key, ValueModifierTagId tag, float value);
        bool SetAdd(EntityRef entity, ValueKey<int> key, ValueModifierTagId tag, float value);
        bool SetMul(EntityRef entity, ValueKeyReference key, ValueModifierTagId tag, float value);
        bool SetMul(EntityRef entity, ValueKey<float> key, ValueModifierTagId tag, float value);
        bool SetMul(EntityRef entity, ValueKey<int> key, ValueModifierTagId tag, float value);
        bool RemoveAdd(EntityRef entity, ValueKeyReference key, ValueModifierTagId tag);
        bool RemoveAdd(EntityRef entity, ValueKey<float> key, ValueModifierTagId tag);
        bool RemoveAdd(EntityRef entity, ValueKey<int> key, ValueModifierTagId tag);
        bool RemoveMul(EntityRef entity, ValueKeyReference key, ValueModifierTagId tag);
        bool RemoveMul(EntityRef entity, ValueKey<float> key, ValueModifierTagId tag);
        bool RemoveMul(EntityRef entity, ValueKey<int> key, ValueModifierTagId tag);
        bool SetBoolModifier(EntityRef entity, ValueKeyReference key, ValueModifierTagId tag, bool value);
        bool SetBoolModifier(EntityRef entity, ValueKey<bool> key, ValueModifierTagId tag, bool value);
        bool RemoveBoolModifier(EntityRef entity, ValueKeyReference key, ValueModifierTagId tag);
        bool RemoveBoolModifier(EntityRef entity, ValueKey<bool> key, ValueModifierTagId tag);
        void ClearEntity(EntityRef entity);
        void Clear();
    }

    // Kernel単位のStore。Scene全体やApplication全体の共有状態を表す。
    public interface IKernelValueStoreService
    {
        T Get<T>(ValueKeyReference key);
        T Get<T>(ValueKey<T> key);

        // Gimmickの条件監視はKernelValueStoreのHandleを標準入口にする。
        ValueWatchHandle<T> GetHandle<T>(ValueKeyReference key);
        ValueWatchHandle<T> GetHandle<T>(ValueKey<T> key);
        bool Set<T>(ValueKeyReference key, T value);
        bool Set<T>(ValueKey<T> key, T value);
        bool SetAdd(ValueKeyReference key, ValueModifierTagId tag, float value);
        bool SetAdd(ValueKey<float> key, ValueModifierTagId tag, float value);
        bool SetAdd(ValueKey<int> key, ValueModifierTagId tag, float value);
        bool SetMul(ValueKeyReference key, ValueModifierTagId tag, float value);
        bool SetMul(ValueKey<float> key, ValueModifierTagId tag, float value);
        bool SetMul(ValueKey<int> key, ValueModifierTagId tag, float value);
        bool RemoveAdd(ValueKeyReference key, ValueModifierTagId tag);
        bool RemoveAdd(ValueKey<float> key, ValueModifierTagId tag);
        bool RemoveAdd(ValueKey<int> key, ValueModifierTagId tag);
        bool RemoveMul(ValueKeyReference key, ValueModifierTagId tag);
        bool RemoveMul(ValueKey<float> key, ValueModifierTagId tag);
        bool RemoveMul(ValueKey<int> key, ValueModifierTagId tag);
        bool SetBoolModifier(ValueKeyReference key, ValueModifierTagId tag, bool value);
        bool SetBoolModifier(ValueKey<bool> key, ValueModifierTagId tag, bool value);
        bool RemoveBoolModifier(ValueKeyReference key, ValueModifierTagId tag);
        bool RemoveBoolModifier(ValueKey<bool> key, ValueModifierTagId tag);
        void Clear();
    }

    // Action 内だけで有効な一時 Store。Scene の Entity/Kernel 状態を汚さず分岐や選択結果を保持する。
    public interface ILocalValueStoreService
    {
        T Get<T>(ValueKeyReference key);
        T Get<T>(ValueKey<T> key);
        ValueWatchHandle<T> GetHandle<T>(ValueKeyReference key);
        ValueWatchHandle<T> GetHandle<T>(ValueKey<T> key);
        bool Set<T>(ValueKeyReference key, T value);
        bool Set<T>(ValueKey<T> key, T value);
        bool SetAdd(ValueKeyReference key, ValueModifierTagId tag, float value);
        bool SetAdd(ValueKey<float> key, ValueModifierTagId tag, float value);
        bool SetAdd(ValueKey<int> key, ValueModifierTagId tag, float value);
        bool SetMul(ValueKeyReference key, ValueModifierTagId tag, float value);
        bool SetMul(ValueKey<float> key, ValueModifierTagId tag, float value);
        bool SetMul(ValueKey<int> key, ValueModifierTagId tag, float value);
        bool RemoveAdd(ValueKeyReference key, ValueModifierTagId tag);
        bool RemoveAdd(ValueKey<float> key, ValueModifierTagId tag);
        bool RemoveAdd(ValueKey<int> key, ValueModifierTagId tag);
        bool RemoveMul(ValueKeyReference key, ValueModifierTagId tag);
        bool RemoveMul(ValueKey<float> key, ValueModifierTagId tag);
        bool RemoveMul(ValueKey<int> key, ValueModifierTagId tag);
        bool SetBoolModifier(ValueKeyReference key, ValueModifierTagId tag, bool value);
        bool SetBoolModifier(ValueKey<bool> key, ValueModifierTagId tag, bool value);
        bool RemoveBoolModifier(ValueKeyReference key, ValueModifierTagId tag);
        bool RemoveBoolModifier(ValueKey<bool> key, ValueModifierTagId tag);
        void Clear();
    }
}
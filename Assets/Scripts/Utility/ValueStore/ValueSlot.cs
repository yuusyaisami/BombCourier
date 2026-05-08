using System;

namespace BC.Base
{
    internal sealed class ValueSlot
    {
        public ValueKeyId KeyId { get; }
        public Type ValueType { get; }
        public object Value { get; private set; }
        public int Revision { get; private set; }

        public ValueSlot(ValueKeyId keyId, Type valueType, object initialValue)
        {
            KeyId = keyId;
            ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
            Value = initialValue;
            Revision = 1;
        }

        public bool Set<T>(T value)
        {
            // 型の安全性を確保するため、ValueTypeとTが一致するか確認する
            if (ValueType != typeof(T))
            {
                // cast を試みる
                if (TryCast(out T castedValue))
                {
                    value = castedValue;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Value type mismatch. Key={KeyId}, Expected={ValueType.Name}, Actual={typeof(T).Name}");
                }
            }

            if (Equals(Value, value))
                return false;

            Value = value;
            Revision++;
            return true;
        }

        public T Get<T>()
        {
            // 型の安全性を確保するため、ValueTypeとTが一致するか確認する
            if (ValueType != typeof(T))
            {
                // cast を試みる
                if (TryCast(out T castedValue))
                {
                    return castedValue;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Value type mismatch. Key={KeyId}, Expected={ValueType.Name}, Actual={typeof(T).Name}");
                }
            }

            return (T)Value;
        }

        // 一部の型間での変換を許可するためのキャスト試行メソッド
        public bool TryCast<T>(out T result)
        {
            // 一応 int <-> float などの変換は許可する
            if (ValueType == typeof(T))
            {
                result = (T)Value;
                return true;
            }
            // int と float の相互変換を許可
            else if (typeof(T) == typeof(float) && ValueType == typeof(int))
            {
                result = (T)(object)(float)(int)Value;
                return true;
            }
            else if (typeof(T) == typeof(int) && ValueType == typeof(float))
            {
                result = (T)(object)(int)(float)Value;
                return true;
            }
            // bool と int の相互変換を許可 (0 = false, 0以外 = true)
            else if (typeof(T) == typeof(bool) && ValueType == typeof(int))
            {
                result = (T)(object)((int)Value != 0);
                return true;
            }
            else if (typeof(T) == typeof(int) && ValueType == typeof(bool))
            {
                result = (T)(object)((bool)Value ? 1 : 0);
                return true;
            }
            // bool と float の相互変換を許可 (0.0f = false, 0.0f以外 = true)
            else if (typeof(T) == typeof(bool) && ValueType == typeof(float))
            {
                result = (T)(object)((float)Value != 0.0f);
                return true;
            }
            else if (typeof(T) == typeof(float) && ValueType == typeof(bool))
            {
                result = (T)(object)((bool)Value ? 1.0f : 0.0f);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
    }
}
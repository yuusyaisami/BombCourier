using UnityEngine;

namespace BC.UI
{
    /// <summary>
    /// モーダルUI（設定画面など）が開いている間、裏側のゲーム入力（Talk の送りなど）を
    /// 生入力経由で誤って進めないように抑制するための共有ゲート。
    /// モーダルを開いたら <see cref="Push"/>、閉じたら <see cref="Pop"/> を呼ぶ。
    /// </summary>
    public static class UiModalGate
    {
        private static int openCount;

        public static bool IsAnyOpen => openCount > 0;

        public static void Push()
        {
            openCount++;
        }

        public static void Pop()
        {
            openCount = Mathf.Max(0, openCount - 1);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            openCount = 0;
        }
    }
}

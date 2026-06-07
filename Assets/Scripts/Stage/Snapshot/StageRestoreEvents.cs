using System;
using UnityEngine;

namespace BC.Stage.Snapshot
{
    /// <summary>
    /// ステージ復元（スナップショット復元 ＋ ValueStore 復元）が完了した後に1回だけ発火するイベント。
    /// ConditionDrivenColliderObject のように「ValueStore 条件で状態が決まる」ものが、
    /// 全状態が戻った後に一度だけ再評価するために購読する。
    /// </summary>
    public static class StageRestoreEvents
    {
        public static event Action PostRestore;

        public static void RaisePostRestore()
        {
            PostRestore?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            PostRestore = null;
        }
    }
}

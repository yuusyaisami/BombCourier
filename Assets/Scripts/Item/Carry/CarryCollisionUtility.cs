using System.Collections.Generic;
using UnityEngine;

namespace BC.Item
{
    /// <summary>
    /// 持ち運び/投擲アイテムと所有者(Player)コライダー間の IgnoreCollision を安全に戻すためのユーティリティ。
    /// 投擲直後にタイマーだけで一括解除すると、足元などに重なったまま解除され、
    /// 物理エンジンのめり込み解消で Player が吹き飛ぶ。重なっている間は無視を維持し、分離してから解除する。
    /// </summary>
    public static class CarryCollisionUtility
    {
        // 重なっていないペアのみ IgnoreCollision を解除し list から除外する。重なっているペアは維持する。
        // すべて解除し終えたら true を返す。
        public static bool ReleaseSeparatedIgnoredColliders(Collider self, List<Collider> ignored)
        {
            if (ignored == null)
                return true;

            if (self == null)
            {
                ignored.Clear();
                return true;
            }

            for (int i = ignored.Count - 1; i >= 0; i--)
            {
                Collider owner = ignored[i];
                if (owner == null)
                {
                    ignored.RemoveAt(i);
                    continue;
                }

                if (CollidersOverlap(self, owner))
                    continue;

                Physics.IgnoreCollision(self, owner, false);
                ignored.RemoveAt(i);
            }

            return ignored.Count == 0;
        }

        // 2つのコライダーが現在めり込んでいるかを判定する。
        public static bool CollidersOverlap(Collider a, Collider b)
        {
            if (a == null || b == null || !a.enabled || !b.enabled)
                return false;

            if (!a.gameObject.activeInHierarchy || !b.gameObject.activeInHierarchy)
                return false;

            return Physics.ComputePenetration(
                a, a.transform.position, a.transform.rotation,
                b, b.transform.position, b.transform.rotation,
                out _, out _);
        }
    }
}

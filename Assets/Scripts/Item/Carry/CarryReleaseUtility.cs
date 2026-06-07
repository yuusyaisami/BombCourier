using BC.Stage;
using UnityEngine;

namespace BC.Item
{
    /// <summary>
    /// 持ち運びアイテムをリリースする際に戻す親 Transform を決めるユーティリティ。
    /// SetParent(null)（GameScene ルート直下）にすると、マップ破棄時にアイテムが残り、
    /// 次のマップへ持ち越されてしまう。これを避けるため、
    /// 「拾う前の親（Map 内）→ 現在アクティブな Map ルート → 最終手段 null」の順で解決する。
    /// </summary>
    public static class CarryReleaseUtility
    {
        public static Transform ResolveReleaseParent(Transform capturedParent)
        {
            // capturedParent が破棄済みなら Unity の == オーバーロードで null 扱いになる。
            if (capturedParent != null)
                return capturedParent;

            MapRuntimeMB activeMap = MapRuntimeMB.Active;
            return activeMap != null ? activeMap.transform : null;
        }
    }
}

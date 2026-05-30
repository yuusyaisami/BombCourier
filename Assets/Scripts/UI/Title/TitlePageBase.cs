using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BC.UI.Title
{
    // タイトルシーンの各ページが継承する抽象基底クラス。
    // ShowAsync / HideAsync でページの表示・非表示アニメーションを統一する。
    public abstract class TitlePageBase : MonoBehaviour
    {
        /// <summary>このページが現在表示されているかどうか。</summary>
        public bool IsShowing { get; protected set; }

        /// <summary>ページを表示してアニメーション完了まで待つ。</summary>
        public abstract UniTask ShowAsync(CancellationToken ct);

        /// <summary>ページを非表示にしてアニメーション完了まで待つ。</summary>
        public abstract UniTask HideAsync(CancellationToken ct);
    }
}

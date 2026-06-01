using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BC.UI
{
    // ScrollRect の Content 内に Slider を配置したとき、
    // ScrollRect がドラッグイベントを横取りして Slider が動かなくなる問題を修正するコンポーネント。
    //
    // 原因:
    //   Unity の Slider は IBeginDragHandler を実装していない。
    //   そのため IBeginDragHandler が親の ScrollRect へ伝播し、
    //   ScrollRect がドラッグターゲットになって以後の IDragHandler も奪われる。
    //
    // 対処:
    //   このコンポーネントを Slider と同じ GameObject にアタッチする。
    //   IBeginDragHandler を実装して伝播を止め、
    //   ドラッグ方向が Slider の軸と一致するときは Slider に処理させ、
    //   一致しないときだけ親 ScrollRect へ転送する。
    [RequireComponent(typeof(Slider))]
    [DisallowMultipleComponent]
    public sealed class ScrollRectSliderFix : MonoBehaviour,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private Slider _slider;
        private ScrollRect _parentScrollRect;

        // true のとき、このドラッグは Slider が担当する。
        private bool _handledBySlider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _parentScrollRect = GetComponentInParent<ScrollRect>(includeInactive: true);
        }

        // ─── IInitializePotentialDragHandler ───────────────────────────
        // ScrollRect にも慣性リセット等を行わせるために転送する。
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            (_parentScrollRect as IInitializePotentialDragHandler)?.OnInitializePotentialDrag(eventData);
        }

        // ─── IBeginDragHandler ─────────────────────────────────────────
        // ここで伝播を受け止め、方向判定を行う。
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_slider == null)
            {
                ForwardBeginDragToScrollRect(eventData);
                return;
            }

            bool sliderIsHorizontal = _slider.direction == Slider.Direction.LeftToRight
                                   || _slider.direction == Slider.Direction.RightToLeft;

            float absX = Mathf.Abs(eventData.delta.x);
            float absY = Mathf.Abs(eventData.delta.y);

            // ドラッグ量が非常に小さい場合は Slider 側を優先する
            // (ユーザーが Slider を操作しようとしている可能性が高い)。
            if (absX == 0f && absY == 0f)
            {
                _handledBySlider = true;
                return;
            }

            bool dragIsHorizontal = absX >= absY;

            // Slider の軸とドラッグ方向が合っていれば Slider に任せる。
            _handledBySlider = (sliderIsHorizontal == dragIsHorizontal);

            if (!_handledBySlider)
                ForwardBeginDragToScrollRect(eventData);
        }

        // ─── IDragHandler ──────────────────────────────────────────────
        public void OnDrag(PointerEventData eventData)
        {
            if (_handledBySlider)
            {
                // Slider 自身の IDragHandler を呼び出す。
                ExecuteEvents.Execute(_slider.gameObject, eventData, ExecuteEvents.dragHandler);
            }
            else if (_parentScrollRect != null)
            {
                ExecuteEvents.Execute(
                    _parentScrollRect.gameObject, eventData,
                    ExecuteEvents.dragHandler);
            }
        }

        // ─── IEndDragHandler ───────────────────────────────────────────
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_handledBySlider && _parentScrollRect != null)
            {
                ExecuteEvents.Execute(
                    _parentScrollRect.gameObject, eventData,
                    ExecuteEvents.endDragHandler);
            }
            _handledBySlider = false;
        }

        // ──────────────────────────────────────────────────────────────
        private void ForwardBeginDragToScrollRect(PointerEventData eventData)
        {
            if (_parentScrollRect != null)
                ExecuteEvents.Execute(
                    _parentScrollRect.gameObject, eventData,
                    ExecuteEvents.beginDragHandler);
        }
    }
}

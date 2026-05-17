using DG.Tweening;
using UnityEngine;
namespace BC.Manager
{
    // ゲームシーンのUIを管理するクラス。
    // 画面上のUI要素の表示/非表示や、UI要素とゲーム全体の状態を同期させる役割を持つ。
    public class UIGameSceneManagerMB : MonoBehaviour
    {
        [SerializeField] private RectTransform topPanel; // 画面上部のUIパネル
        [SerializeField] private Vector3 topPanelHiddenPosition = new Vector3(0, 1000, 0); // 上部パネルを隠す位置
        [SerializeField] private Vector3 topPanelVisiblePosition = Vector3.zero; // 上部パネルを表示する位置
        [SerializeField] private RectTransform bottomPanel; // 画面下部のUIパネル
        [SerializeField] private Vector3 bottomPanelHiddenPosition = new Vector3(0, -1000, 0); // 下部パネルを隠す位置
        [SerializeField] private Vector3 bottomPanelVisiblePosition = Vector3.zero; // 下部パネルを表示する位置

        private Tween topPanelTween;
        private Tween bottomPanelTween;

        public void ShowTopPanel(bool show, float duration = 0.5f)
        {
            Debug.Log("ShowTopPanel: " + show);
            if (topPanel == null)
                return;

            topPanelTween?.Kill();
            Vector3 targetPosition = show ? topPanelVisiblePosition : topPanelHiddenPosition;
            if (duration <= 0f)
            {
                topPanel.anchoredPosition = targetPosition;
                return;
            }
            topPanelTween = topPanel.DOAnchorPos(targetPosition, duration).SetEase(Ease.OutCubic);
        }

        public void ShowBottomPanel(bool show, float duration = 0.5f)
        {
            Debug.Log("ShowBottomPanel: " + show);
            if (bottomPanel == null)
                return;

            bottomPanelTween?.Kill();
            if (duration <= 0f)
            {
                bottomPanel.anchoredPosition = show ? bottomPanelVisiblePosition : bottomPanelHiddenPosition;
                return;
            }
            Vector3 targetPosition = show ? bottomPanelVisiblePosition : bottomPanelHiddenPosition;
            bottomPanelTween = bottomPanel.DOAnchorPos(targetPosition, duration).SetEase(Ease.OutCubic);
        }
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using System;


public class PartySwipePager : MonoBehaviour, IEndDragHandler
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private float snapDuration = 0.2f;
    [SerializeField] private float swipeThreshold = 0.3f;

    private float pageWidth;   // 1ページ分のスクロール量（Viewport幅）

    private Action onRefresh;

    public void Setup(Action onRefresh)
    {
        var viewport = scrollRect.viewport.rect;
        pageWidth = viewport.width;
        this.onRefresh = onRefresh;
    }

    public void SetPagePosition()
    {
        content.anchoredPosition = new Vector2(0, content.anchoredPosition.y);
    }

    // ドラッグ終了時：開始位置との差分でスワイプ判定
    public void OnEndDrag(PointerEventData eventData)
    {
        // 現在のページを計算
        float currentX = content.anchoredPosition.x;
        int targetIndex = 0;

        // ★ 一定以上動いていたらスワイプとみなしてページ変更
        if (Mathf.Abs(currentX) > swipeThreshold)
        {
            if (currentX > 0)
            {
                // 右へ大きくスワイプ → 前のページへ
                targetIndex = - 1;
            }
            else
            {
                // 左へ大きくスワイプ → 次のページへ
                targetIndex = 1;
            }
        }

        // 対象ページ位置にスナップ
        float targetX = -pageWidth * targetIndex;
        content.DOAnchorPosX(targetX, snapDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                GameContext.Instance.SetCurrentPartyIndex(GameContext.Instance.CurrentPartyIndex + targetIndex);
                onRefresh?.Invoke();
            });
    }
}

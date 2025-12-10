using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using System;


public class DetailSwipePager : MonoBehaviour, IEndDragHandler
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private float snapDuration = 0.2f;
    [SerializeField] private float swipeThreshold = 0.3f;

    private float pageWidth;   // 1ページ分のスクロール量（Viewport幅）
    private int cardIndex;
    private int partyIndex;

    private Action<int, int> onSwipe;

    public void Setup(Action<int, int> onSwipe)
    {
        var viewport = scrollRect.viewport.rect;
        pageWidth = viewport.width;
        this.onSwipe = onSwipe;
    }

    public void SetPagePosition()
    {
        content.anchoredPosition = new Vector2(0, content.anchoredPosition.y);
    }

    public void SetCardIndex(int cardIndex, int partyIndex)
    {
        this.cardIndex = cardIndex;
        this.partyIndex = partyIndex;
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
                onSwipe?.Invoke(cardIndex + targetIndex, partyIndex);
            });
    }
}

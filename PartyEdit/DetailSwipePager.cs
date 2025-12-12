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

    private Action<int> onSwipeDirection;
    private Action onSwipeEnd;

    public void Setup(Action<int> onSwipeDirection, Action onSwipeEnd)
    {
        var viewport = scrollRect.viewport.rect;
        pageWidth = viewport.width;
        this.onSwipeDirection = onSwipeDirection;
        this.onSwipeEnd = onSwipeEnd;
    }

    public void SetPagePosition()
    {
        // 常に中央カード（index=0位置）に戻す
        content.anchoredPosition = new Vector2(0, content.anchoredPosition.y);
    }

    // ドラッグ終了時：開始位置との差分でスワイプ判定
    public void OnEndDrag(PointerEventData eventData)
    {
        float currentX   = content.anchoredPosition.x;
        int   swipeDirection = 0;

        // ★ 一定以上動いていたらスワイプとみなす
        if (Mathf.Abs(currentX) > swipeThreshold)
        {
            if (currentX > 0)
            {
                // 右へスワイプ → 前のモンスターへ
                swipeDirection = -1;
            }
            else
            {
                // 左へスワイプ → 次のモンスターへ
                swipeDirection = 1;
            }
        }

        if (swipeDirection != 0)
        {
            scrollRect.enabled = false;
            onSwipeDirection?.Invoke(swipeDirection);
        }

        // UI位置をスナップ
        float targetX = -pageWidth * swipeDirection;
        content.DOAnchorPosX(targetX, snapDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // ★ 見た目が移動し終わったタイミングでコールバック
                // targetMove が 0 の場合は同じ index を再表示（位置だけ戻す）
                onSwipeEnd?.Invoke();
                scrollRect.enabled = true;
            });
    }

    public void OnClickRightArrow()
    {
        int swipeDirection = 1;
        scrollRect.enabled = false;
        onSwipeDirection?.Invoke(swipeDirection);

        // UI位置をスナップ
        float targetX = -pageWidth * swipeDirection;
        content.DOAnchorPosX(targetX, snapDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // ★ 見た目が移動し終わったタイミングでコールバック
                // targetMove が 0 の場合は同じ index を再表示（位置だけ戻す）
                onSwipeEnd?.Invoke();
                scrollRect.enabled = true;
            });
    }


    public void OnClickLeftArrow()
    {
        int swipeDirection = -1;
        scrollRect.enabled = false;
        onSwipeDirection?.Invoke(swipeDirection);

        // UI位置をスナップ
        float targetX = -pageWidth * swipeDirection;
        content.DOAnchorPosX(targetX, snapDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // ★ 見た目が移動し終わったタイミングでコールバック
                // targetMove が 0 の場合は同じ index を再表示（位置だけ戻す）
                onSwipeEnd?.Invoke();
                scrollRect.enabled = true;
            });
    }
}

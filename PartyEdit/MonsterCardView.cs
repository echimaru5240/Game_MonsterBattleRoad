using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using CandyCoded.HapticFeedback;
using DG.Tweening;

public class MonsterCardView : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI atkText;
    [SerializeField] private TextMeshProUGUI mgcText;
    [SerializeField] private TextMeshProUGUI defText;
    [SerializeField] private TextMeshProUGUI agiText;
    [SerializeField] private Image isPartyCover;
    [SerializeField] private GameObject partyRemoveBtn;

    [Header("Long Press")]
    [SerializeField] private float longPressThreshold = 0.6f; // 0.6秒長押しで発火

    [Header("Click判定")]
    [SerializeField] private float clickMoveThreshold = 20f; // 何pxまでならタップ扱いか
    [SerializeField] private float clickTimeThreshold = 0.1f; // 何秒までならタップ扱いか

    [Header("Press Animation")]
    [SerializeField] private float pressedScale = 0.95f;         // 押してる間の縮小倍率
    [SerializeField] private float pressScaleDuration = 0.08f;   // 縮小にかける時間
    [SerializeField] private float releasePopScale = 1.05f;      // 離したときに一瞬膨らむ倍率
    [SerializeField] private float releasePopDuration = 0.12f;   // 膨らみ→戻りにかける合計時間

    public int Index { get; private set; }
    public int PartyIndex { get; private set; }
    private OwnedMonster ownedData;
    private Action<MonsterCardView> onClicked;
    private Action<MonsterCardView> onLongPress;
    private Action<MonsterCardView> onPartyRemove;

    private bool isPressing;
    private bool isDetailView;
    private float pressTime;
    private bool longPressTriggered;
    private Vector2 pointerDownPos;

    // ★ 元のスケールと現在のTweenを保持
    private Vector3 defaultScale;
    private Tween scaleTween;

    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    public void Setup(OwnedMonster data, int partyIndex, Action<MonsterCardView> onClicked, Action<MonsterCardView> onLongPress, Action<MonsterCardView> onPartyRemove = null)
    {
        // Debug.Log("Card SetUp");
        ownedData = data;
        PartyIndex = partyIndex;
        this.onClicked = onClicked;
        this.onLongPress = onLongPress;
        this.onPartyRemove = onPartyRemove;
        isPartyCover.gameObject.SetActive(false);
        partyRemoveBtn.SetActive(false);
        isDetailView = false;

        if (ownedData != null && ownedData.master != null)
        {
            // Debug.Log("Card DataSet");
            iconImage.gameObject.SetActive(true);
            iconImage.sprite = ownedData.monsterFarSprite;
            nameText.text    = ownedData.Name;
            hpText.text      = ownedData.hp.ToString();
            atkText.text     = ownedData.atk.ToString();
            mgcText.text     = ownedData.mgc.ToString();
            defText.text     = ownedData.def.ToString();
            agiText.text     = ownedData.agi.ToString();
            if (ownedData.isParty && partyIndex == -1)
            {
                isPartyCover.gameObject.SetActive(true);
                partyRemoveBtn.SetActive(true);
            }
        }
        else
        {
            // Debug.Log("Card Data Null");
            // 空スロット表示など、必要ならここで
            iconImage.gameObject.SetActive(false);
            nameText.text    = "－－－";
            hpText.text = atkText.text = mgcText.text = defText.text = agiText.text = "-";
        }

        // 念のため毎回リセット
        transform.localScale = defaultScale;
    }

    public void SetCardIndex(int index)
    {
        Index = index;
    }

    public void SetupDetailDataView(MonsterCardView card)
    {
        Setup(card.GetOwnedMonsterData(), -1, null, null);
        isPartyCover.gameObject.SetActive(false);
        partyRemoveBtn.SetActive(false);
        isDetailView = true;;
    }

    private void Update()
    {
        if (!isPressing)
        {
            return;
        }

        if (ownedData == null || isDetailView)
        {
            return;
        }

        if (!longPressTriggered)
        {
            pressTime += Time.deltaTime;

            if (pressTime >= longPressThreshold)
            {
                Debug.Log("onLongPress Card");
                longPressTriggered = true;
                HapticFeedback.LightFeedback();
                onLongPress?.Invoke(this);
            }
        }
    }


    public void SetIsParty(bool isParty)
    {
        ownedData.isParty = isParty;
    }

    public OwnedMonster GetOwnedMonsterData()
    {
        return ownedData;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("OnPointerDown Card");
        isPressing = true;
        pressTime = 0f;
        longPressTriggered = false;
        // ★ 押したときの位置を記録
        pointerDownPos = eventData.position;
        Debug.Log($"pointerDownPos {pointerDownPos}");

        // ★ 詳細ビュー用カードはスケール演出しない
        if (isDetailView) return;

        // ★ 押したときに少し縮小
        scaleTween?.Kill();
        transform.localScale = defaultScale;
        scaleTween = transform
            .DOScale(defaultScale * pressedScale, pressScaleDuration)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("OnPointerUp Card");
        isPressing = false;

        // 指を離した位置
        Vector2 pointerUpPos = eventData.position;
        Debug.Log($"pointerUpPos {pointerUpPos}");
        float moveDist = Vector2.Distance(pointerUpPos, pointerDownPos);

        // ★ 移動量が大きければ「スクロールしただけ」と判断してクリック処理しない
        if (moveDist > clickMoveThreshold)
        {
            Debug.Log($"Ignore click: moved {moveDist}px (threshold {clickMoveThreshold})");
            // スクロールだった場合はスケールだけ元に戻す
            scaleTween?.Kill();
            transform.localScale = defaultScale;
            return;
        }

        // ★ 離したときに「ポン」と膨らんで戻る
        if (!isDetailView)
        {
            scaleTween?.Kill();
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOScale(defaultScale * releasePopScale, releasePopDuration * 0.5f)
                .SetEase(Ease.OutQuad));
            seq.Append(transform.DOScale(defaultScale, releasePopDuration * 0.5f)
                .SetEase(Ease.OutQuad));
            scaleTween = seq;
        }

        // 長押しが発生していないときだけ「通常タップ」として扱う
        if (!longPressTriggered)
        {
            if (pressTime < clickTimeThreshold)
            {
                Debug.Log($"onClicked Card [{Index}]");
                onClicked?.Invoke(this);
            }
            else if (pressTime >= clickTimeThreshold) {
                Debug.Log("onLongPress Card");
                longPressTriggered = true;
                onLongPress?.Invoke(this);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 指が外に出たらキャンセル
        isPressing = false;

        // スケールも元に戻す
        if (!isDetailView)
        {
            scaleTween?.Kill();
            transform.localScale = defaultScale;
        }
    }

    public void OnPartyRemoveButtonClicked()
    {
        Debug.Log("OnPartyRemoveButtonClicked");
        onPartyRemove?.Invoke(this);
    }


    private void OnDisable()
    {
        // カードが非表示になったときなどにTweenを止めてスケールリセット
        scaleTween?.Kill();
        transform.localScale = defaultScale;
    }
}

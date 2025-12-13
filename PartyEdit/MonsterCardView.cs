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
    [SerializeField] private TextMeshProUGUI coverText;
    [SerializeField] private GameObject cardButton;
    [SerializeField] private TextMeshProUGUI cardButtonText;
    [SerializeField] private Color isPartyColor;
    [SerializeField] private Color isSelectedColor;

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
    private Action<MonsterCardView> onCardButtonClick;

    private bool isPressing;
    private float pressTime;
    private bool longPressTriggered;
    private Vector2 pointerDownPos;

    private Vector3 defaultScale;
    private Tween scaleTween;

    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    public void Setup(
        OwnedMonster data,
        int partyIndex,
        Action<MonsterCardView> onClicked,
        Action<MonsterCardView> onLongPress,
        Action<MonsterCardView> onPartyRemoveOrButton = null)
    {
        ownedData = data;
        PartyIndex = partyIndex;
        this.onClicked = onClicked;
        this.onLongPress = onLongPress;
        this.onCardButtonClick = onPartyRemoveOrButton;

        isPartyCover.gameObject.SetActive(false);
        coverText.gameObject.SetActive(false);
        cardButton.SetActive(false);

        if (ownedData != null && ownedData.master != null)
        {
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
                isPartyCover.color = isPartyColor;
                isPartyCover.gameObject.SetActive(true);
                coverText.text = "パーティーメンバー";
                coverText.gameObject.SetActive(true);
                cardButtonText.text = "はずす";
                cardButton.SetActive(true);
            }
        }
        else
        {
            iconImage.gameObject.SetActive(false);
            nameText.text = "－－－";
            hpText.text = atkText.text = mgcText.text = defText.text = agiText.text = "-";
        }

        // 念のため毎回リセット
        scaleTween?.Kill();
        transform.localScale = defaultScale;
    }

    public void SetCardIndex(int index) => Index = index;

    public OwnedMonster GetOwnedMonsterData() => ownedData;

    public void SetIsParty(bool isParty)
    {
        if (ownedData != null) ownedData.isParty = isParty;
    }

    private void Update()
    {
        if (!isPressing) return;
        if (ownedData == null) return;

        if (!longPressTriggered)
        {
            pressTime += Time.deltaTime;

            if (pressTime >= longPressThreshold)
            {
                longPressTriggered = true;
                HapticFeedback.LightFeedback();
                onLongPress?.Invoke(this);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("OnPointerDown Card");
        isPressing = true;
        pressTime = 0f;
        longPressTriggered = false;
        // ★ 押したときの位置を記録
        pointerDownPos = eventData.position;

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

        Vector2 pointerUpPos = eventData.position;
        float moveDist = Vector2.Distance(pointerUpPos, pointerDownPos);

        // スクロール判定
        if (moveDist > clickMoveThreshold)
        {
            scaleTween?.Kill();
            transform.localScale = defaultScale;
            return;
        }

        // ★ 離したときに「ポン」と膨らんで戻る
        scaleTween?.Kill();
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(defaultScale * releasePopScale, releasePopDuration * 0.5f)
            .SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(defaultScale, releasePopDuration * 0.5f)
            .SetEase(Ease.OutQuad));
        scaleTween = seq;

        // 長押しが発生していないときだけ「通常タップ」として扱う
        if (!longPressTriggered)
        {
            if (pressTime < clickTimeThreshold)
            {
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
        scaleTween?.Kill();
        transform.localScale = defaultScale;
    }

    public void OnCardButtonClicked()
    {
        onCardButtonClick?.Invoke(this);
    }

    private void OnDisable()
    {
        // カードが非表示になったときなどにTweenを止めてスケールリセット
        scaleTween?.Kill();
        transform.localScale = defaultScale;
    }

    /// <summary>
    /// 入れ替え表示（候補/対象）制御
    /// - showButton=true  : 所持リストで「入れ替え」ボタン表示
    /// - showButton=false : ハイライトだけ（SelectingTarget中用）
    /// </summary>
    public void SetReplaceState(
        bool isSelected,
        bool isOwnedList = false,
        Action<MonsterCardView> onReplace = null,
        bool showButton = true)
    {
        if (ownedData == null) return;

        // パーティ所属で、所持リスト側で「はずす」以外を出したくないなら弾く（元仕様踏襲）
        if (ownedData.isParty && PartyIndex == -1) return;

        // onReplace が渡されたときだけ更新（nullで既存を消さない）
        if (onReplace != null) onCardButtonClick = onReplace;

        isPartyCover.color = isSelectedColor;
        isPartyCover.gameObject.SetActive(isSelected);
        coverText.text = isOwnedList ? "選択中" : "入れ替え対象";
        coverText.gameObject.SetActive(isSelected);

        cardButtonText.text = "入れ替え";
        cardButton.SetActive(isSelected && isOwnedList && showButton);
    }
}

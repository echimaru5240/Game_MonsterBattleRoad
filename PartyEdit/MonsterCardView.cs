using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using CandyCoded.HapticFeedback;
using DG.Tweening;

public enum MonsterCardEventType
{
    Click,
    LongPress,
    CardButton,
    LevelUpButton,
}

public class MonsterCardView : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI atkText;
    [SerializeField] private TextMeshProUGUI mgcText;
    [SerializeField] private TextMeshProUGUI defText;
    [SerializeField] private TextMeshProUGUI agiText;
    [SerializeField] private Image isPartyCover;
    [SerializeField] private TextMeshProUGUI coverText;
    [SerializeField] private GameObject levelUpButton;
    [SerializeField] private GameObject cardButton;
    [SerializeField] private TextMeshProUGUI cardButtonText;
    [SerializeField] private Color isPartyColor;
    [SerializeField] private Color isSelectedColor;

    [Header("Long Press")]
    [SerializeField] private float longPressThreshold = 0.6f;

    [Header("Click判定")]
    [SerializeField] private float clickMoveThreshold = 20f;
    [SerializeField] private float clickTimeThreshold = 0.1f;

    [Header("Press Animation")]
    [SerializeField] private float pressedScale = 0.95f;
    [SerializeField] private float pressScaleDuration = 0.08f;
    [SerializeField] private float releasePopScale = 1.05f;
    [SerializeField] private float releasePopDuration = 0.12f;

    public int Index { get; private set; }
    public int PartyIndex { get; private set; }

    private OwnedMonster ownedData;

    // ★ Actionを1本に統合
    private Action<MonsterCardView, MonsterCardEventType> onEvent;

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
        Action<MonsterCardView, MonsterCardEventType> onEvent)
    {
        ownedData = data;
        PartyIndex = partyIndex;
        this.onEvent = onEvent;

        isPartyCover.gameObject.SetActive(false);
        coverText.gameObject.SetActive(false);
        cardButton.SetActive(false);
        levelUpButton.SetActive(false);

        if (ownedData != null && ownedData.monsterId != 0)
        {
            iconImage.gameObject.SetActive(true);
            iconImage.sprite = ownedData.monsterFarSprite;
            nameText.text    = ownedData.Name;
            levelText.text   = ownedData.level.ToString();
            hpText.text      = ownedData.hp.ToString();
            atkText.text     = ownedData.atk.ToString();
            mgcText.text     = ownedData.mgc.ToString();
            defText.text     = ownedData.def.ToString();
            agiText.text     = ownedData.agi.ToString();

            // 所持リスト（partyIndex==-1）でパーティ所属なら「はずす」表示
            if (ownedData.isParty && partyIndex == -1)
            {
                isPartyCover.color = isPartyColor;
                isPartyCover.gameObject.SetActive(true);
                coverText.text = "パーティーメンバー";
                coverText.gameObject.SetActive(true);

                cardButtonText.text = "はずす";
                cardButton.SetActive(true);
            }
            else
            {
                // ★ 未振りポイントがあるときだけレベルアップボタン
                levelUpButton.SetActive(ownedData.unspentStatPoints > 0);
            }
        }
        else
        {
            iconImage.gameObject.SetActive(false);
            nameText.text = "－－－";
            levelText.text = hpText.text = atkText.text = mgcText.text = defText.text = agiText.text = "-";
        }

        scaleTween?.Kill();
        transform.localScale = defaultScale;
    }

    public void SetCardIndex(int index) => Index = index;
    public OwnedMonster GetOwnedMonsterData() => ownedData;
    public void SetIsParty(bool isParty) { if (ownedData != null) ownedData.isParty = isParty; }

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
                onEvent?.Invoke(this, MonsterCardEventType.LongPress);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressing = true;
        pressTime = 0f;
        longPressTriggered = false;
        pointerDownPos = eventData.position;

        scaleTween?.Kill();
        transform.localScale = defaultScale;
        scaleTween = transform
            .DOScale(defaultScale * pressedScale, pressScaleDuration)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressing = false;

        Vector2 pointerUpPos = eventData.position;
        float moveDist = Vector2.Distance(pointerUpPos, pointerDownPos);

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
                onEvent?.Invoke(this, MonsterCardEventType.Click);
            }
            else
            {
                longPressTriggered = true;
                onEvent?.Invoke(this, MonsterCardEventType.LongPress);
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

    public void OnLevelUpButtonClicked()
    {
        onEvent?.Invoke(this, MonsterCardEventType.LevelUpButton);
    }

    public void OnCardButtonClicked()
    {
        onEvent?.Invoke(this, MonsterCardEventType.CardButton);
    }

    private void OnDisable()
    {
        // カードが非表示になったときなどにTweenを止めてスケールリセット
        scaleTween?.Kill();
        transform.localScale = defaultScale;
    }

    public void SetReplaceState(
        bool isSelected,
        bool isOwnedList = false,
        bool showButton = true)
    {
        if (ownedData == null) return;
        if (ownedData.isParty && PartyIndex == -1) return;

        isPartyCover.color = isSelectedColor;
        isPartyCover.gameObject.SetActive(isSelected);
        coverText.text = isOwnedList ? "選択中" : "入れ替え対象";
        coverText.gameObject.SetActive(isSelected);

        cardButtonText.text = "入れ替え";

        if (isSelected && isOwnedList && showButton)
        {
            cardButton.SetActive(true);
            levelUpButton.SetActive(false);
        }
        else
        {
            cardButton.SetActive(false);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

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
    [SerializeField] private Image longPressGauge;            // ← 円形ゲージ
    [Header("Click判定")]
    [SerializeField] private float clickMoveThreshold = 20f; // 何pxまでならタップ扱いか
    [SerializeField] private float clickTimeThreshold = 0.1f; // 何pxまでならタップ扱いか

    private OwnedMonster ownedData;
    private int partyNum;
    private Action<MonsterCardView> onClicked;
    private Action<MonsterCardView> onLongPress;
    private Action<MonsterCardView> onPartyRemove;

    private bool isPressing;
    private bool isDetailView;
    private float pressTime;
    private bool longPressTriggered;
    private Vector2 pointerDownPos;

    public void Setup(OwnedMonster data, int partyNum, Action<MonsterCardView> onClicked, Action<MonsterCardView> onLongPress, Action<MonsterCardView> onPartyRemove = null)
    {
        // Debug.Log("Card SetUp");
        ownedData = data;
        this.partyNum = partyNum;
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
            if (ownedData.isParty && partyNum == -1)
            {
                isPartyCover.gameObject.SetActive(true);
                partyRemoveBtn.SetActive(true);
            }
            if (partyNum == -1)
            {
                clickMoveThreshold = 50f;
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
            // 押してないときはゲージを 0 にして非表示に
            if (longPressGauge != null)
            {
                longPressGauge.fillAmount = 0f;
                longPressGauge.gameObject.SetActive(false);
            }
            return;
        }

        if (ownedData == null || isDetailView)
        {
            return;
        }

        // ここから「押している間」
        if (longPressGauge != null && pressTime >= 0.05f)
        {
            longPressGauge.gameObject.SetActive(true);
        }

        if (!longPressTriggered)
        {
            pressTime += Time.deltaTime;

            // 閾値に対する割合 0.0?1.0
            float ratio = Mathf.Clamp01(pressTime / longPressThreshold);

            if (longPressGauge != null)
            {
                longPressGauge.fillAmount = ratio;   // ★ ここで円形ゲージが溜まる
            }

            if (pressTime >= longPressThreshold)
            {
                Debug.Log("onLongPress Card");
                longPressTriggered = true;
                onLongPress?.Invoke(this);

                // 閾値に達したらゲージいっぱいにしてもOK
                if (longPressGauge != null)
                {
                    longPressGauge.fillAmount = 1f;
                }
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

    public int GetPartyNum()
    {
        return partyNum;
    }

    // private void OnClick()
    // {
    //     Debug.Log("OnClick Card");
    //     onClicked?.Invoke(this);
    // }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("OnPointerDown Card");
        isPressing = true;
        pressTime = 0f;
        longPressTriggered = false;
        // ★ 押したときの位置を記録
        pointerDownPos = eventData.position;
        Debug.Log($"pointerDownPos {pointerDownPos}");
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
            return;
        }
        // 長押しが発生していないときだけ「通常タップ」として扱う
        if (!longPressTriggered && pressTime < clickTimeThreshold)
        {
            Debug.Log("onClicked Card");
            onClicked?.Invoke(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 指が外に出たらキャンセル
        isPressing = false;
    }

    public void OnPartyRemoveButtonClicked()
    {
        Debug.Log("OnPartyRemoveButtonClicked");
        onPartyRemove?.Invoke(this);
    }
}

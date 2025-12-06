using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MonsterCardView : MonoBehaviour
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
    [SerializeField] private Button button;

    private OwnedMonster ownedData;
    private int partyNum;
    private Action<MonsterCardView> onClicked;

    public void Setup(OwnedMonster data, int partyNum, Action<MonsterCardView> onClicked)
    {
        // Debug.Log("Card SetUp");
        ownedData = data;
        this.partyNum = partyNum;
        this.onClicked = onClicked;
        isPartyCover.gameObject.SetActive(false);

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

        if (button != null)
        {
            // Debug.Log("Button != Null");
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
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

    private void OnClick()
    {
        Debug.Log("OnClick Card");
        onClicked?.Invoke(this);
    }
}

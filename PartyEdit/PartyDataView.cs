using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PartyDataView : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private int partySize = 3;

    [Header("UI - Party")]
    [SerializeField] private MonsterCardView[] partySlots; // 上の3枚
    [SerializeField] private TextMeshProUGUI partyNameText;
    [SerializeField] private TextMeshProUGUI teamHpText;
    [SerializeField] private Image viewFrame;

    private PartyData partyData;
    private bool isCurrentParty;
    private Action onRefresh;
    private Action<MonsterCardView> onMonsterCardViewLongPressed;

    public void Setup(Action onRefresh, Action<MonsterCardView> onMonsterCardViewLongPressed)
    {
        partyData = new PartyData();
        this.onRefresh = onRefresh;
        this.onMonsterCardViewLongPressed = onMonsterCardViewLongPressed;
    }

    public void RefreshPartyData(PartyData partyData, bool isCurrentParty)
    {
        int totalHp = 0;

        this.partyData = partyData;
        this.isCurrentParty = isCurrentParty;

        for (int i = 0; i < partySize; i++)
        {
            // 上のスロットにも MonsterCardView を使う
            var monster = partyData.members[i];
            partySlots[i].Setup(monster, i, OnPartyMonsterClicked, onMonsterCardViewLongPressed); // 上はクリック無効なら null
            if (monster != null) {
                totalHp += monster.hp;
                monster.isParty = isCurrentParty ?  true : false;
            }
        }

        partyNameText.text = partyData.partyName;
        teamHpText.text    = totalHp.ToString();
        viewFrame.gameObject.SetActive(isCurrentParty);
    }

    public bool IsEmptyParty()
    {
        for (int i = 0; i < partySize; i++)
        {
            if (partyData.members[i] != null)
            {
                return false;
            }
        }
        return true;
    }

    public void OnPartyRemoveButtonClicked(MonsterCardView card)
    {
        Debug.Log($"OnPartyRemoveButtonClicked dataview: {card.GetOwnedMonsterData().Name}");
        for (int i = 0; i < partySize; i++)
        {
            Debug.Log($"OnPartyRemoveButtonClicked {i}");
            if (partyData.members[i] == card.GetOwnedMonsterData())
            {
                partyData.members[i] = null;
                card.SetIsParty(false);
                RefreshPartyData(partyData, isCurrentParty);
                onRefresh?.Invoke();
                return;
            }
        }
    }

    // 一覧から選択された
    private void OnPartyMonsterClicked(MonsterCardView card)
    {
        if (!isCurrentParty || card.GetOwnedMonsterData() == null) {
            return;
        }
        Debug.Log("OnClick Card");
        partyData.members[card.GetPartyNum()] = null;
        card.SetIsParty(false);
        RefreshPartyData(partyData, isCurrentParty);
        onRefresh?.Invoke();
    }


    private void OnClickPartyName()
    {
        Debug.Log("OnClick PartyName");
    }
}

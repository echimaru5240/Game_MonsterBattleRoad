using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PartyDataView : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private int partySize = 3;

    [Header("UI - Party")]
    [SerializeField] private MonsterCardView[] partySlots;
    [SerializeField] private TextMeshProUGUI partyNameText;
    [SerializeField] private TextMeshProUGUI teamHpText;
    [SerializeField] private Image viewFrame;

    private PartyData partyData;
    private bool isCurrentParty;

    // 長押しはPartyEditManagerへ（詳細表示）
    private Action<MonsterCardView> onMonsterCardViewLongPressed;

    // ★ slotタップ通知（PartyEditManagerへ）
    private Action<int> onPartySlotClicked;

    public void Setup(
        Action<MonsterCardView> onMonsterCardViewLongPressed,
        Action<int> onPartySlotClicked)
    {
        this.onMonsterCardViewLongPressed = onMonsterCardViewLongPressed;
        this.onPartySlotClicked = onPartySlotClicked;
    }

    public void RefreshPartyData(PartyData partyData, bool isCurrentParty)
    {
        int totalHp = 0;

        this.partyData = partyData;
        this.isCurrentParty = isCurrentParty;

        for (int i = 0; i < partySize; i++)
        {
            var monster = partyData.members[i];

            // ★ onClicked は PartyDataView 内のハンドラにして、slotIndex を上に通知
            partySlots[i].Setup(monster, i, OnPartyMonsterClicked, onMonsterCardViewLongPressed);
            partySlots[i].SetCardIndex(i);

            if (monster != null)
            {
                totalHp += monster.hp;

                // ※本当はManagerだけで管理したいが、現状のUI（所持側の「パーティーメンバー」表示）
                // を維持するために踏襲。将来的には partyId/slotIndex 化推奨。
                monster.isParty = isCurrentParty ? true : monster.isParty;
            }
        }

        partyNameText.text = partyData.partyName;
        teamHpText.text = totalHp.ToString();
        viewFrame.gameObject.SetActive(isCurrentParty);
    }

    public void SetReplaceState(bool enable)
    {
        for (int i = 0; i < partySize; i++)
        {
            if (partyData.members[i] != null)
            {
                partySlots[i].SetReplaceState(enable);
            }
        }
    }

    public bool IsEmptyParty()
    {
        for (int i = 0; i < partySize; i++)
        {
            if (partyData.members[i] != null) return false;
        }
        return true;
    }

    private void OnPartyMonsterClicked(MonsterCardView card)
    {
        if (!isCurrentParty) return;
        onPartySlotClicked?.Invoke(card.PartyIndex);
        // 必要なら onRefresh?.Invoke();（いまはManager側で RefreshAllView する想定）
    }

    private void OnClickPartyName()
    {
        Debug.Log("OnClick PartyName");
    }
}

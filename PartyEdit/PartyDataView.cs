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

    // ★ これ1本だけ
    private Action<MonsterCardView, MonsterCardEventType> onCardEvent;

    public void Setup(Action<MonsterCardView, MonsterCardEventType> onCardEvent)
    {
        this.onCardEvent = onCardEvent;
    }

    public void RefreshPartyData(PartyData partyData, bool isCurrentParty)
    {
        float totalHp = 0;

        this.partyData = partyData;
        this.isCurrentParty = isCurrentParty;

        for (int i = 0; i < partySize; i++)
        {
            var monster = partyData.members[i];

            // ★ Party slotも同じイベント1本を渡す
            partySlots[i].Setup(monster, i, OnPartySlotEvent);
            partySlots[i].SetCardIndex(i);

            if (monster != null)
            {
                totalHp += monster.hp;

                // 現状踏襲（将来的にpartyId/slotIndex化推奨）
                monster.isParty = isCurrentParty ? true : monster.isParty;
            }
        }

        partyNameText.text = partyData.partyName;
        teamHpText.text = Mathf.FloorToInt(totalHp).ToString();
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

    private void OnPartySlotEvent(MonsterCardView card, MonsterCardEventType type)
    {
        // ★ 現在パーティ以外は無視（これまで通り）
        if (!isCurrentParty) return;

        // Party側で許可するイベントはここで制限できる
        // 例：Party slotにLevelUpButtonがあっても意味ないなら弾く
        // if (type == MonsterCardEventType.LevelUpButton)
        //     return;

        // そのまま上へ
        onCardEvent?.Invoke(card, type);
    }

    private void OnClickPartyName()
    {
        Debug.Log("OnClick PartyName");
    }
}

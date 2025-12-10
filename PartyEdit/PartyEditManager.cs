using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

// ================================
// パーティ編成メニュー状態
// ================================
public enum PartyEditState
{
    NONE,
    LIST,
    DETAIL,
}

public class PartyEditManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private int partyDataSize = 3;
    [SerializeField] private int partySize = 3;

    [Header("UI - Party")]
    [SerializeField] private PartySwipePager partySwipePager;
    [SerializeField] private PartyDataView[] partyDataSlots; // 上の3枚
    [SerializeField] private GameObject[] partyPointerObjs; // 上の3枚

    [Header("UI - Owned List")]
    [SerializeField] private Transform listContentParent; // ScrollRect/Viewport/Content
    [SerializeField] private MonsterCardView listItemPrefab;

    [Header("UI - Detail Data")]
    [SerializeField] private DetailSwipePager detailSwipePager;
    [SerializeField] private MonsterDetailDataView monsterDetailDataView;

    [Header("UI - Popup")]
    [SerializeField] private GameObject popupObj;

    private PartyData[] partyDatas;
    private int currentPartyIndex;
    private PlayerMonsterInventory inventoryMonsters;

    // 状態管理
    private PartyEditState state = PartyEditState.NONE;


    private void Start()
    {
        state = PartyEditState.LIST;

        // 戦闘BGM再生
        AudioManager.Instance.PlayHomeBGM();
        popupObj.gameObject.SetActive(false);
        monsterDetailDataView.Setup();

        partyDatas = new PartyData[partyDataSize];

        inventoryMonsters   = GameContext.Instance.inventory;
        partyDatas          = GameContext.Instance.partyList;
        currentPartyIndex   = GameContext.Instance.CurrentPartyIndex;

        partySwipePager.Setup(RefreshAllView);
        for (int i = 0; i < partyDataSize; i++) {
            partyDataSlots[i].Setup(RefreshOwnedMonsterListView, OnMonsterCardViewLongPressed);
        }
        // RefreshOwnedMonsterListView();
        // RefreshPartyDataView();
        RefreshAllView();
    }

    private void ChangeState(PartyEditState newState)
    {
        state = newState;

        switch (state)
        {
            case PartyEditState.LIST:
                for (int i = 0; i < partyDataSlots.Length; i++)
                {
                    partyDataSlots[i].gameObject.SetActive(true);
                }
                listContentParent.gameObject.SetActive(true);

                break;
            case PartyEditState.DETAIL:
                // ShowMonsterDetailDataView(card);
                for (int i = 0; i < partyDataSlots.Length; i++)
                {
                    partyDataSlots[i].gameObject.SetActive(false);
                }
                listContentParent.gameObject.SetActive(false);
                break;
        }
    }

    // 下の一覧を生成
    private void RefreshOwnedMonsterListView()
    {
        foreach (Transform child in listContentParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < inventoryMonsters.ownedMonsters.Count; i++)
        {
            var owned = inventoryMonsters.ownedMonsters[i];
            var item = Instantiate(listItemPrefab, listContentParent);
            // 一覧のカードを押したらパーティに入れる
            item.Setup(owned, -1, OnOwnedMonsterClicked, OnMonsterCardViewLongPressed, OnPartyRemoveButtonClicked);
            item.SetCardIndex(i);
        }
    }

    // 一覧から選択された
    private void OnOwnedMonsterClicked(MonsterCardView card)
    {
        Debug.Log($"OnOwnedMonsterClicked editmanager");
        var monster = card.GetOwnedMonsterData();

        if (!monster.isParty)
        {
            // 1. 空きスロットを探す
            for (int i = 0; i < partySize; i++)
            {
                if (partyDatas[currentPartyIndex].members[i] == null)
                {
                    partyDatas[currentPartyIndex].members[i] = monster;//card.GetOwnedMonsterData();
                    GameContext.Instance.SetCurrentParty(partyDatas[currentPartyIndex].members);
                    RefreshPartyDataView();
                    card.Setup(monster, -1, OnOwnedMonsterClicked, OnMonsterCardViewLongPressed, OnPartyRemoveButtonClicked);
                    return;
                }
            }
        }
    }

    private void OnPartyRemoveButtonClicked(MonsterCardView card)
    {
        Debug.Log($"OnPartyRemoveButtonClicked editmanager");
        currentPartyIndex = GameContext.Instance.CurrentPartyIndex;
        partyDataSlots[1].OnPartyRemoveButtonClicked(card);
    }

    // パーティ上部の3枚を更新
    private void RefreshPartyDataView()
    {
        RefreshPrivatePartyData();

        int leftPartyDataSlotIndex = currentPartyIndex - 1;
        int rightPartyDataSlotIndex = currentPartyIndex + 1;
        if (leftPartyDataSlotIndex == -1) {
            leftPartyDataSlotIndex = partyDatas.Length -1;
        }
        if (rightPartyDataSlotIndex == partyDatas.Length) {
            rightPartyDataSlotIndex = 0;
        }

        partyDataSlots[0].RefreshPartyData(partyDatas[leftPartyDataSlotIndex], false);
        partyDataSlots[2].RefreshPartyData(partyDatas[rightPartyDataSlotIndex], false);
        partyDataSlots[1].RefreshPartyData(partyDatas[currentPartyIndex], true);
        GameContext.Instance.SetCurrentParty(partyDatas[currentPartyIndex].members);
        SetPartyPointer();
        // RefreshOwnedMonsterListView();
        partySwipePager.SetPagePosition();
    }

    private void RefreshAllView()
    {
        RefreshPartyDataView();
        RefreshOwnedMonsterListView();
    }

    private void RefreshPrivatePartyData()
    {
        partyDatas = GameContext.Instance.partyList;
        currentPartyIndex = GameContext.Instance.CurrentPartyIndex;
        Debug.Log($"currentPartyIndex: {currentPartyIndex}");
    }

    private void SetPartyPointer()
    {
        for (int i = 0; i < partyPointerObjs.Length; i++)
        {
            if (currentPartyIndex == i)
            {
                partyPointerObjs[i].SetActive(true);
            }
            else {
                partyPointerObjs[i].SetActive(false);
            }
        }
    }

    private void SetPartyData()
    {
        // partyMonsters の内容を、どこかの PartyManager やセーブデータに反映
        Debug.Log("パーティ決定！");
        // ① 現在のパーティを GameContext に保存
        if (GameContext.Instance != null)
        {
            GameContext.Instance.SetPartyData(partyDatas, currentPartyIndex);
        }
        else
        {
            Debug.LogWarning("GameContext が見つかりませんでした。");
        }

    }

    private void ShowMonsterDetailDataView(int cardIndex, int partyIndex)
    {
        RefreshPrivatePartyData();
        int monsterNum = inventoryMonsters.ownedMonsters.Count;

        int currentIndex = cardIndex;
        int prevIndex = currentIndex - 1;
        int nextIndex = currentIndex + 1;

        if (partyIndex == -1)
        {
            if (prevIndex < 0)
                prevIndex = monsterNum - 1;

            if (nextIndex > monsterNum - 1)
                nextIndex = 0;
        }
        else {
            monsterNum = 3;
            if (prevIndex < 0)
                prevIndex = 2;

            if (nextIndex > 2)
                nextIndex = 0;
        }

        Debug.Log($"prev: {prevIndex}, next: {nextIndex}");

        monsterDetailDataView.Show(inventoryMonsters.ownedMonsters[currentIndex], currentIndex, monsterNum, inventoryMonsters.ownedMonsters[prevIndex], inventoryMonsters.ownedMonsters[nextIndex]);
        detailSwipePager.SetCardIndex(cardIndex, partyIndex);
        detailSwipePager.SetPagePosition();
    }

    // インデックスを 0 ～ count-1 の範囲に丸める（-1 → 最後、count → 0）
    private int WrapIndex(int index, int count)
    {
        if (count <= 0) return 0;

        index %= count;
        if (index < 0)
            index += count;

        return index;
    }

    private void RefreshMonsterDetailDataView(int cardIndex, int partyIndex)
    {
        RefreshPrivatePartyData();

        int currentIndex;
        int prevIndex;
        int nextIndex;

        // ==========
        // インベントリ側（所持モンスター一覧）
        // ==========
        if (partyIndex == -1)
        {
            int monsterNum = inventoryMonsters.ownedMonsters.Count;
            if (monsterNum == 0)
            {
                Debug.LogWarning("No monsters in inventory.");
                return;
            }

            currentIndex = WrapIndex(cardIndex, monsterNum);
            prevIndex    = WrapIndex(currentIndex - 1, monsterNum);
            nextIndex    = WrapIndex(currentIndex + 1, monsterNum);

            Debug.Log($"[Inventory] current: {currentIndex}, prev: {prevIndex}, next: {nextIndex}");

            monsterDetailDataView.Show(
                inventoryMonsters.ownedMonsters[currentIndex],
                currentIndex,
                monsterNum,
                inventoryMonsters.ownedMonsters[prevIndex],
                inventoryMonsters.ownedMonsters[nextIndex]
            );
        }
        // ==========
        // パーティ側（3体固定）
        // ==========
        else
        {
            var members = partyDatas[currentPartyIndex].members;
            int memberCount = members.Length; // 想定: 3

            if (memberCount == 0)
            {
                Debug.LogWarning("No members in party.");
                return;
            }

            currentIndex = WrapIndex(cardIndex, memberCount);
            prevIndex    = WrapIndex(currentIndex - 1, memberCount);
            nextIndex    = WrapIndex(currentIndex + 1, memberCount);

            Debug.Log($"[Party] current: {currentIndex}, prev: {prevIndex}, next: {nextIndex}");

            monsterDetailDataView.Show(
                members[currentIndex],
                currentIndex,
                memberCount,
                members[prevIndex],
                members[nextIndex]
            );
        }

        // 共通処理
        Debug.Log($"prev: {prevIndex}, next: {nextIndex}");

        detailSwipePager.SetCardIndex(currentIndex, partyIndex);
        detailSwipePager.SetPagePosition();
    }

    private void OnMonsterCardViewLongPressed(MonsterCardView card)
    {
        detailSwipePager.Setup(RefreshMonsterDetailDataView);
        ShowMonsterDetailDataView(card.Index, card.PartyIndex);
    }

    // OKボタンから呼ぶ
    public void OnClickOk()
    {
        // パーティが空か判定
        if (partyDataSlots[1].IsEmptyParty()) {
            StartCoroutine(ShowPopup());
            return;
        }
        // partyMonsters の内容を、どこかの PartyManager やセーブデータに反映
        Debug.Log("パーティ決定！");
        // ① 現在のパーティを GameContext に保存
        SetPartyData();

        AudioManager.Instance.PlayButtonSE();
        SceneManager.LoadScene("HomeScene");
    }

    private IEnumerator ShowPopup()
    {
        Debug.Log("パーティメンバーがいません");
        popupObj.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        popupObj.gameObject.SetActive(false);
    }

}

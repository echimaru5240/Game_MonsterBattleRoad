using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

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
    [SerializeField] private MonsterDetailDataView monsterDetailDataView;

    [Header("UI - Popup")]
    [SerializeField] private GameObject popupObj;

    private PartyData[] partyDatas;
    private int currentPartyIndex;
    private PlayerMonsterInventory inventoryMonsters;


    private void Start()
    {
        // 戦闘BGM再生
        AudioManager.Instance.PlayHomeBGM();
        popupObj.gameObject.SetActive(false);

        partyDatas = new PartyData[partyDataSize];

        inventoryMonsters   = GameContext.Instance.inventory;
        partyDatas          = GameContext.Instance.partyList;
        currentPartyIndex   = GameContext.Instance.CurrentPartyIndex;

        partySwipePager.Setup(RefreshPartyDataView);
        for (int i = 0; i < partyDataSize; i++) {
            partyDataSlots[i].Setup(RefreshOwnedMonsterListView, OnMonsterCardViewLongPressed);
        }
        RefreshOwnedMonsterListView();
        RefreshPartyDataView();
    }

    // 下の一覧を生成
    private void RefreshOwnedMonsterListView()
    {
        foreach (Transform child in listContentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var owned in inventoryMonsters.ownedMonsters)
        {
            var item = Instantiate(listItemPrefab, listContentParent);
            // 一覧のカードを押したらパーティに入れる
            item.Setup(owned, -1, OnOwnedMonsterClicked, OnMonsterCardViewLongPressed, OnPartyRemoveButtonClicked);
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
                    RefreshOwnedMonsterListView();
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
        partyDatas = GameContext.Instance.partyList;
        currentPartyIndex = GameContext.Instance.CurrentPartyIndex;
        Debug.Log($"currentPartyIndex: {currentPartyIndex}");

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
        RefreshOwnedMonsterListView();
        partySwipePager.SetPagePosition();
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

    private void ShowMonsterDetailDataView(MonsterCardView card)
    {
        monsterDetailDataView.Show(card);
    }

    private void OnMonsterCardViewLongPressed(MonsterCardView card)
    {
        ShowMonsterDetailDataView(card);
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

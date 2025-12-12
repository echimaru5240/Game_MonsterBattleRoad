using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using System;

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
    [SerializeField] private int partyDataSize = 3; // パーティ数
    [SerializeField] private int partySize     = 3; // 1パーティの枠数

    [Header("UI - Party")]
    [SerializeField] private PartySwipePager   partySwipePager;
    [SerializeField] private PartyDataView[]   partyDataSlots;   // 上の3枚
    [SerializeField] private GameObject[]      partyPointerObjs; // 下のインジケータ

    [Header("UI - Owned List")]
    [SerializeField] private Transform         listContentParent; // ScrollRect/Viewport/Content
    [SerializeField] private MonsterCardView   listItemPrefab;

    [Header("UI - Detail Data")]
    [SerializeField] private DetailSwipePager      detailSwipePager;
    [SerializeField] private MonsterDetailDataView monsterDetailDataView;

    [Header("UI - Popup")]
    [SerializeField] private GameObject popupObj;

    private PartyData[]            partyDatas;
    private int                    currentPartyIndex;
    private PlayerMonsterInventory inventoryMonsters;

    // 状態管理
    private PartyEditState state = PartyEditState.NONE;

    // 詳細画面用の状態（PartyEditManager が唯一の持ち主）
    private int detailPartyIndex;   // -1 or 0.. (カード側の partyIndex)
    private int detailIndex;        // 現在表示中
    private int detailTotal;

    private Func<int, OwnedMonster> detailGetter;

    private void Start()
    {
        state = PartyEditState.LIST;

        AudioManager.Instance.PlayHomeBGM();
        popupObj.gameObject.SetActive(false);

        // ★ 閉じた時に呼ばれるコールバックを渡す
        monsterDetailDataView.Setup(OnDetailClosed);

        var ctx = GameContext.Instance;
        if (ctx != null)
        {
            inventoryMonsters = ctx.inventory; // 参照コピー
            partyDatas        = ctx.partyList; // 参照コピー
            currentPartyIndex = ctx.CurrentPartyIndex;
        }
        else
        {
            Debug.LogError("GameContext が見つかりませんでした。");
        }

        partySwipePager.Setup(RefreshAllView);

        for (int i = 0; i < partyDataSize; i++)
        {
            partyDataSlots[i].Setup(RefreshOwnedMonsterListView, OnMonsterCardViewLongPressed);
        }

        RefreshAllView();
    }


    private void ChangeState(PartyEditState newState)
    {
        state = newState;

        switch (state)
        {
            case PartyEditState.LIST:
                foreach (var slot in partyDataSlots)
                    slot.gameObject.SetActive(true);
                listContentParent.gameObject.SetActive(true);
                break;

            case PartyEditState.DETAIL:
                foreach (var slot in partyDataSlots)
                    slot.gameObject.SetActive(false);
                listContentParent.gameObject.SetActive(false);
                break;
        }
    }

    /// <summary>
    /// PartySwipePager が GameContext.CurrentPartyIndex を更新する前提で、
    /// ここからローカルの currentPartyIndex を同期する。
    /// </summary>
    private void SyncPartyIndexFromContext()
    {
        var ctx = GameContext.Instance;
        if (ctx == null) return;

        currentPartyIndex = ctx.CurrentPartyIndex;
    }

    // ================================
    // 下の一覧（所持モンスター）
    // ================================
    private void RefreshOwnedMonsterListView()
    {
        foreach (Transform child in listContentParent)
        {
            Destroy(child.gameObject);
        }

        if (inventoryMonsters == null || inventoryMonsters.ownedMonsters == null)
        {
            Debug.LogWarning("inventoryMonsters が未設定です。");
            return;
        }

        for (int i = 0; i < inventoryMonsters.ownedMonsters.Count; i++)
        {
            var owned = inventoryMonsters.ownedMonsters[i];
            var item  = Instantiate(listItemPrefab, listContentParent);

            // 一覧のカードを押したらパーティに入れる
            item.Setup(owned, -1, OnOwnedMonsterClicked, OnMonsterCardViewLongPressed, OnPartyRemoveButtonClicked);
            item.SetCardIndex(i);
        }
    }

    // 一覧から選択された（パーティに追加）
    private void OnOwnedMonsterClicked(MonsterCardView card)
    {
        Debug.Log($"OnOwnedMonsterClicked PartyEditManager");
        var monster = card.GetOwnedMonsterData();
        if (monster == null) return;

        if (!monster.isParty)
        {
            // 空きスロットを探す
            var members = partyDatas[currentPartyIndex].members;
            for (int i = 0; i < partySize; i++)
            {
                if (members[i] == null)
                {
                    members[i]    = monster;
                    monster.isParty = true;

                    RefreshPartyDataView();
                    // カード表示も更新（isParty フラグなど反映させたい場合）
                    card.Setup(monster, -1, OnOwnedMonsterClicked, OnMonsterCardViewLongPressed, OnPartyRemoveButtonClicked);
                    return;
                }
            }
        }
    }

    private void OnPartyRemoveButtonClicked(MonsterCardView card)
    {
        Debug.Log($"OnPartyRemoveButtonClicked PartyEditManager");

        // 念のためパーティインデックスを同期（スワイプ直後などを想定）
        SyncPartyIndexFromContext();

        // パーティスロット側で、該当モンスターを外す処理をしている想定
        partyDataSlots[1].OnPartyRemoveButtonClicked(card);

        // 所持リスト & 上部表示を更新
        RefreshPartyDataView();
        RefreshOwnedMonsterListView();
    }

    // ================================
    // 上部のパーティ 3 枚
    // ================================
    private void RefreshPartyDataView()
    {
        // PartySwipePager によって GameContext.CurrentPartyIndex が変わっている可能性があるので同期
        SyncPartyIndexFromContext();

        if (partyDatas == null || partyDatas.Length == 0)
        {
            Debug.LogWarning("partyDatas が未設定です。");
            return;
        }

        int leftPartyDataSlotIndex  = currentPartyIndex - 1;
        int rightPartyDataSlotIndex = currentPartyIndex + 1;

        if (leftPartyDataSlotIndex < 0)
            leftPartyDataSlotIndex = partyDatas.Length - 1;

        if (rightPartyDataSlotIndex >= partyDatas.Length)
            rightPartyDataSlotIndex = 0;

        partyDataSlots[0].RefreshPartyData(partyDatas[leftPartyDataSlotIndex],  false);
        partyDataSlots[2].RefreshPartyData(partyDatas[rightPartyDataSlotIndex], false);
        partyDataSlots[1].RefreshPartyData(partyDatas[currentPartyIndex],       true);

        SetPartyPointer();
        partySwipePager.SetPagePosition();
    }

    private void RefreshAllView()
    {
        RefreshPartyDataView();
        RefreshOwnedMonsterListView();
    }

    private void SetPartyPointer()
    {
        for (int i = 0; i < partyPointerObjs.Length; i++)
        {
            partyPointerObjs[i].SetActive(currentPartyIndex == i);
        }
    }

    // ================================
    // 詳細画面（3D表示用）
    // ================================

    // インデックスを 0 ～ count-1 の範囲に丸める（-1 → 最後、count → 0）
    private int WrapIndex(int index, int count)
    {
        if (count <= 0) return 0;

        index %= count;
        if (index < 0)
            index += count;

        return index;
    }

    /// <summary>
    /// 詳細画面の内容を更新（初回表示・スワイプ更新どちらも）
    /// </summary>
    // private void UpdateMonsterDetail(int cardIndex, int partyIndex)
    // {
    //     int currentIndex;
    //     int prevIndex;
    //     int nextIndex;

    //     // ========== インベントリ側 ==========
    //     if (partyIndex == -1)
    //     {
    //         int monsterNum = inventoryMonsters.ownedMonsters.Count;
    //         if (monsterNum == 0)
    //         {
    //             Debug.LogWarning("No monsters in inventory.");
    //             return;
    //         }

    //         currentIndex = WrapIndex(cardIndex, monsterNum);
    //         prevIndex    = WrapIndex(currentIndex - 1, monsterNum);
    //         nextIndex    = WrapIndex(currentIndex + 1, monsterNum);

    //         monsterDetailDataView.ShowInitial(
    //             inventoryMonsters.ownedMonsters[currentIndex],
    //             currentIndex,
    //             monsterNum,
    //             inventoryMonsters.ownedMonsters[prevIndex],
    //             inventoryMonsters.ownedMonsters[nextIndex]
    //         );
    //     }
    //     // ========== パーティ側 ==========
    //     else
    //     {
    //         SyncPartyIndexFromContext();
    //         var members = partyDatas[currentPartyIndex].members;
    //         int memberCount = members.Length; // 想定: 3

    //         if (memberCount == 0)
    //         {
    //             Debug.LogWarning("No members in party.");
    //             return;
    //         }

    //         currentIndex = WrapIndex(cardIndex, memberCount);
    //         prevIndex    = WrapIndex(currentIndex - 1, memberCount);
    //         nextIndex    = WrapIndex(currentIndex + 1, memberCount);

    //         monsterDetailDataView.ShowInitial(
    //             members[currentIndex],
    //             currentIndex,
    //             memberCount,
    //             members[prevIndex],
    //             members[nextIndex]
    //         );
    //     }
    //     detailSwipePager.SetPagePosition();
    // }

    private OwnedMonster GetDetailMonster(int i)
    {
        if (detailPartyIndex == -1)
            return inventoryMonsters.ownedMonsters[i];
        else
            return partyDatas[currentPartyIndex].members[i];
    }

    private void OpenDetail(int startIndex, int partyIndex)
    {
        ChangeState(PartyEditState.DETAIL);

        detailPartyIndex = partyIndex;

        if (partyIndex == -1)
        {
            detailTotal = inventoryMonsters.ownedMonsters.Count;
        }
        else
        {
            SyncPartyIndexFromContext();
            detailTotal = partyDatas[currentPartyIndex].members.Length;
        }

        detailIndex = WrapIndex(startIndex, detailTotal);

        var prev = GetDetailMonster(WrapIndex(detailIndex - 1, detailTotal));
        var cur  = GetDetailMonster(detailIndex);
        var next = GetDetailMonster(WrapIndex(detailIndex + 1, detailTotal));

        monsterDetailDataView.ShowInitial(prev, cur, next, detailIndex, detailTotal);

        detailSwipePager.Setup(OnDetailSwiped, OnSwipeEnd);
        detailSwipePager.SetPagePosition();
    }


    private void OnDetailSwiped(int direction)
    {
        if (direction == 0) return;
        if (monsterDetailDataView.IsTransitioning) return;
        if (detailTotal <= 0) return;

        int nextIndex = WrapIndex(detailIndex + direction, detailTotal);

        var prev = GetDetailMonster(WrapIndex(nextIndex - 1, detailTotal));
        var cur  = GetDetailMonster(nextIndex);
        var next = GetDetailMonster(WrapIndex(nextIndex + 1, detailTotal));

        monsterDetailDataView.PlaySwap(direction, prev, cur, next, nextIndex, detailTotal);

        // 状態更新
        detailIndex = nextIndex;
    }

    private void OnSwipeEnd()
    {
        detailSwipePager.SetPagePosition();
    }

    private void OnMonsterCardViewLongPressed(MonsterCardView card)
    {
        OpenDetail(card.Index, card.PartyIndex);
    }

    /// <summary>
    /// 詳細画面が閉じられたときに呼ばれる
    /// </summary>
    private void OnDetailClosed()
    {
        // 状態を LIST に戻して、上部パーティ & 所持リストを再表示
        ChangeState(PartyEditState.LIST);
        RefreshAllView();
    }

    // ================================
    // OKボタン
    // ================================
    public void OnClickOk()
    {
        // パーティが空か判定（中央のスロット）
        if (partyDataSlots[1].IsEmptyParty())
        {
            StartCoroutine(ShowPopup());
            return;
        }

        Debug.Log("パーティ決定！");

        var ctx = GameContext.Instance;
        if (ctx != null)
        {
            // 現在のパーティ番号を反映（ほぼ同じはずだが、念のため）
            ctx.CurrentPartyIndex = currentPartyIndex;
            // 便利関数として現在パーティを渡しておく
            ctx.SetCurrentParty(partyDatas[currentPartyIndex].members);
            // 必要ならここで SaveGame()
            // ctx.SaveGame();
        }

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

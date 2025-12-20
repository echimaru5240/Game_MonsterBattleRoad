using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using UnityEngine.UI;

// ================================
// パーティ編成メニュー状態
// ================================
public enum PartyEditState
{
    NONE,
    LIST,
    DETAIL,
}

// ★ 入れ替え状態（2段階 + none）
public enum ReplaceState
{
    None,
    CandidateSelected,   // 候補選択（入れ替えボタン表示）
    SelectingTarget      // 入れ替え先選択中（ハイライトのみ、所持タップでキャンセル）
}

public class PartyEditManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private int partyDataSize = 3; // パーティ数
    [SerializeField] private int partySize = 3;     // 1パーティの枠数

    [Header("UI - Party")]
    [SerializeField] private PartySwipePager partySwipePager;
    [SerializeField] private PartyDataView[] partyDataSlots;   // 上の3枚
    [SerializeField] private GameObject[] partyPointerObjs;    // 下のインジケータ

    [Header("UI - Owned List")]
    [SerializeField] private Transform listContentParent; // ScrollRect/Viewport/Content
    [SerializeField] private MonsterCardView listItemPrefab;

    [Header("UI - Detail Data")]
    [SerializeField] private DetailSwipePager detailSwipePager;
    [SerializeField] private MonsterDetailDataView monsterDetailDataView;

    [Header("UI - Popup")]
    [SerializeField] private PartyEditPopup popup;

    private PartyData[] partyDatas;
    private int currentPartyIndex;
    private PlayerMonsterInventory inventoryMonsters;

    // 状態管理
    private PartyEditState state = PartyEditState.NONE;

    // =========================
    // ★ Replace（入れ替え）状態（Managerのみが保持）
    // =========================
    private ReplaceState replaceState = ReplaceState.None;
    private OwnedMonster replaceCandidate;          // 候補（所持側）
    private int replaceCandidateOwnedIndex = -1;    // 所持リストでのIndex（ハイライト復元用）

    // =========================
    // 詳細画面用の状態（PartyEditManager が唯一の持ち主）
    // =========================
    private int detailPartyIndex;   // -1: 所持リスト, 0..: パーティ
    private int detailIndex;
    private int detailTotal;

    private void Start()
    {
        state = PartyEditState.LIST;

        AudioManager.Instance.PlayHomeBGM();
        popup.Setup();

        // 詳細画面クローズ通知
        monsterDetailDataView.Setup(OnDetailClosed);

        var ctx = GameContext.Instance;
        if (ctx != null)
        {
            inventoryMonsters = ctx.inventory;
            partyDatas = ctx.partyList;
            currentPartyIndex = ctx.CurrentPartyIndex;
        }
        else
        {
            Debug.LogError("GameContext が見つかりませんでした。");
        }

        partySwipePager.Setup(RefreshAllView);

        for (int i = 0; i < partyDataSize; i++)
        {
            // ★ PartyDataViewは “slotクリック通知” だけを上げる
            partyDataSlots[i].Setup(
                OnMonsterCardViewLongPressed,
                OnPartySlotClicked
            );
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
    /// PartySwipePager が GameContext.CurrentPartyIndex を更新する前提で同期
    /// </summary>
    private void SyncPartyIndexFromContext()
    {
        var ctx = GameContext.Instance;
        if (ctx == null) return;
        currentPartyIndex = ctx.CurrentPartyIndex;
    }

    // ================================
    // Replace（入れ替え）制御
    // ================================
    private void SetPartyMonsterCardReplaceState(bool enable)
    {
        // 今の実装は中央（current party）だけ入れ替え対象にする
        partyDataSlots[1].SetReplaceState(enable);
    }

    private void CancelReplace()
    {
        replaceState = ReplaceState.None;
        replaceCandidate = null;
        replaceCandidateOwnedIndex = -1;

        popup.SetReplacePopup(false);
        SetPartyMonsterCardReplaceState(false);
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
            var item = Instantiate(listItemPrefab, listContentParent);

            // 一覧のカード
            item.Setup(
                owned,
                -1,
                OnOwnedMonsterClicked,          // タップ
                OnMonsterCardViewLongPressed,   // 長押し
                OnPartyRemoveButtonClicked      // 「はずす」（isPartyのとき）
            );
            item.SetCardIndex(i);

            // ★ 候補のハイライト復元
            bool isCandidate =
                (i == replaceCandidateOwnedIndex) &&
                (replaceState == ReplaceState.SelectingTarget);

            if (isCandidate)
            {
                // ★ SelectingTarget：ハイライトだけ（ボタンは消す）
                item.SetReplaceState(
                    isSelected: true,
                    isOwnedList: true,
                    onReplace: null,
                    showButton: false
                );
            }
            else
            {
                item.SetReplaceState(false);
            }
        }
    }

    /// <summary>
    /// 所持カードタップ
    /// </summary>
    private void OnOwnedMonsterClicked(MonsterCardView card)
    {
        var monster = card.GetOwnedMonsterData();
        if (monster == null) return;

        // 念のためパーティインデックス同期
        SyncPartyIndexFromContext();

        var members = partyDatas[currentPartyIndex].members;

        // ★ まず重複防止：現在のパーティに既にいるなら何もしない（or 違う挙動にする）
        for (int i = 0; i < partySize; i++)
        {
            if (members[i] == monster)
            {
                // ここで「候補選択にする」等もできるが、まずは二重追加を確実に防ぐ
                return;
            }
        }

        // =========================================
        // 入れ替え対象選択中
        // =========================================
        if (replaceState == ReplaceState.SelectingTarget)
        {
            // 自分（現在の候補）をタップ → 完全キャンセル
            if (card.Index == replaceCandidateOwnedIndex)
            {
                CancelReplace();
                RefreshPartyDataView();
                RefreshOwnedMonsterListView();
                return;
            }

            // ★ 別の所持モンスターをタップ → 候補を差し替え（SelectingTarget継続）
            replaceCandidate = monster;
            replaceCandidateOwnedIndex = card.Index;

            // popup/パーティ側ハイライトは維持
            popup.SetReplacePopup(true);
            SetPartyMonsterCardReplaceState(true);

            RefreshOwnedMonsterListView();
            return;
        }

        // 空きがあれば即追加（従来通り）
        for (int i = 0; i < partySize; i++)
        {
            if (members[i] == null)
            {
                members[i] = monster;
                monster.isParty = true;
                AudioManager.Instance.PlayPartyEnterSE();

                RefreshAllView();
                return;
            }
        }

        // 空きがない → ★即 入れ替え先選択へ
        replaceState = ReplaceState.SelectingTarget;
        replaceCandidate = monster;
        replaceCandidateOwnedIndex = card.Index;

        popup.SetReplacePopup(true);
        SetPartyMonsterCardReplaceState(true);

        RefreshPartyDataView();
        RefreshOwnedMonsterListView();
        return;
    }

    /// <summary>
    /// 候補カード内の「入れ替え」ボタンが押された
    /// → 入れ替え先選択へ
    /// </summary>
    private void OnOwnedReplaceButtonClicked(MonsterCardView card)
    {
        if (replaceCandidate == null)
        {
            CancelReplace();
            RefreshAllView();
            return;
        }

        replaceState = ReplaceState.SelectingTarget;

        popup.SetReplacePopup(true);
        SetPartyMonsterCardReplaceState(true);

        // ★ ボタンを消してハイライトだけにするためにリスト更新
        RefreshOwnedMonsterListView();
    }

    // ================================
    // 上部のパーティ 3 枚
    // ================================
    private void OnPartySlotClicked(int slotIndex)
    {
        // 念のため同期
        SyncPartyIndexFromContext();

        var members = partyDatas[currentPartyIndex].members;

        // 入れ替え先選択中でなければ通常挙動（タップで外す）
        if (replaceState != ReplaceState.SelectingTarget)
        {
            if (members[slotIndex] != null)
            {
                members[slotIndex].isParty = false;
                members[slotIndex] = null;
                AudioManager.Instance.PlayPartyRemoveSE();
                RefreshAllView();
            }
            return;
        }

        // 入れ替え確定
        if (replaceCandidate == null)
        {
            CancelReplace();
            RefreshAllView();
            return;
        }

        var prev = members[slotIndex];
        if (prev != null) prev.isParty = false;

        members[slotIndex] = replaceCandidate;
        replaceCandidate.isParty = true;
        AudioManager.Instance.PlayPartyEnterSE();

        CancelReplace();
        RefreshAllView();
    }

    private void OnPartyRemoveButtonClicked(MonsterCardView card)
    {
        SyncPartyIndexFromContext();

        var monster = card.GetOwnedMonsterData();
        if (monster == null) return;

        var members = partyDatas[currentPartyIndex].members;
        for (int i = 0; i < partySize; i++)
        {
            if (members[i] == monster)
            {
                members[i] = null;
                monster.isParty = false;
                AudioManager.Instance.PlayPartyRemoveSE();
                break;
            }
        }

        // 入れ替え候補を外したらキャンセル
        if (replaceCandidate == monster)
        {
            CancelReplace();
        }

        RefreshAllView();
    }

    // ================================
    // 上部パーティ表示更新
    // ================================
    private void RefreshPartyDataView()
    {
        SyncPartyIndexFromContext();

        if (partyDatas == null || partyDatas.Length == 0)
        {
            Debug.LogWarning("partyDatas が未設定です。");
            return;
        }

        int leftPartyDataSlotIndex = currentPartyIndex - 1;
        int rightPartyDataSlotIndex = currentPartyIndex + 1;

        if (leftPartyDataSlotIndex < 0)
            leftPartyDataSlotIndex = partyDatas.Length - 1;

        if (rightPartyDataSlotIndex >= partyDatas.Length)
            rightPartyDataSlotIndex = 0;

        partyDataSlots[0].RefreshPartyData(partyDatas[leftPartyDataSlotIndex], false);
        partyDataSlots[2].RefreshPartyData(partyDatas[rightPartyDataSlotIndex], false);
        partyDataSlots[1].RefreshPartyData(partyDatas[currentPartyIndex], true);

        // Replace中ならパーティ側ハイライト復元
        if (replaceState == ReplaceState.SelectingTarget)
            SetPartyMonsterCardReplaceState(true);
        else
            SetPartyMonsterCardReplaceState(false);

        SetPartyPointer();
        partySwipePager.SetPagePosition();
    }

    private void RebuildIsPartyFlags()
    {
        if (inventoryMonsters?.ownedMonsters == null) return;

        // いったん全員 false
        for (int i = 0; i < inventoryMonsters.ownedMonsters.Count; i++)
        {
            var m = inventoryMonsters.ownedMonsters[i];
            if (m != null) m.isParty = false;
        }

        // ★ 現在選択中パーティだけ true
        var members = partyDatas[currentPartyIndex].members;
        if (members == null) return;

        for (int i = 0; i < members.Length; i++)
        {
            var m = members[i];
            if (m != null) m.isParty = true;
        }
    }

    private void RefreshAllView()
    {
        SyncPartyIndexFromContext();

        // ★ これが肝：スワイプ含め、どのタイミングでも整合する
        RebuildIsPartyFlags();
        if (replaceState != ReplaceState.None)
        {
            CancelReplace(); // popup OFF / パーティ対象OFF / 候補クリア
        }

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
    private int WrapIndex(int index, int count)
    {
        if (count <= 0) return 0;
        index %= count;
        if (index < 0) index += count;
        return index;
    }

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
        var cur = GetDetailMonster(detailIndex);
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
        var cur = GetDetailMonster(nextIndex);
        var next = GetDetailMonster(WrapIndex(nextIndex + 1, detailTotal));

        monsterDetailDataView.PlaySwap(direction, prev, cur, next, nextIndex, detailTotal);

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

    private void OnDetailClosed()
    {
        ChangeState(PartyEditState.LIST);
        RefreshAllView();
    }

    // ================================
    // OKボタン
    // ================================
    public void OnClickOk()
    {
        if (partyDataSlots[1].IsEmptyParty())
        {
            StartCoroutine(popup.ShowErrorPopup());
            return;
        }

        var ctx = GameContext.Instance;
        if (ctx != null)
        {
            ctx.CurrentPartyIndex = currentPartyIndex;
            ctx.SetCurrentParty(partyDatas[currentPartyIndex].members);
        }

        AudioManager.Instance.PlayButtonSE();
        SceneManager.LoadScene("HomeScene");
    }
}

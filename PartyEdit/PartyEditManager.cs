using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class PartyEditManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private int partySize = 3;

    [Header("UI - Party")]
    [SerializeField] private MonsterCardView[] partySlots; // 上の3枚
    [SerializeField] private TextMeshProUGUI partyNameText;
    [SerializeField] private TextMeshProUGUI teamHpText;

    [Header("UI - Owned List")]
    [SerializeField] private Transform listContentParent; // ScrollRect/Viewport/Content
    [SerializeField] private MonsterCardView listItemPrefab;

    private OwnedMonster[] partyMonsters;
    private PlayerMonsterInventory inventoryMonsters;

    private void Awake()
    {
        partyMonsters = new OwnedMonster[partySize];
    }

    private void Start()
    {
        // 戦闘BGM再生
        AudioManager.Instance.PlayHomeBGM();


        inventoryMonsters = GameContext.Instance.inventory;
        partyMonsters = GameContext.Instance.party;
        RefreshOwnedMonsterList();
        RefreshPartyUI();
    }

    // 下の一覧を生成
    private void RefreshOwnedMonsterList()
    {
        foreach (Transform child in listContentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var owned in inventoryMonsters.ownedMonsters)
        {
            if (!owned.isParty) {
                var item = Instantiate(listItemPrefab, listContentParent);
                // 一覧のカードを押したらパーティに入れる
                item.Setup(owned, OnOwnedMonsterClicked);
            }
        }
    }

    // 一覧から選択された
    private void OnOwnedMonsterClicked(OwnedMonster owned)
    {
        // 1. 空きスロットを探す
        for (int i = 0; i < partySize; i++)
        {
            if (partyMonsters[i] == null)
            {
                partyMonsters[i] = owned;
                owned.isParty = true;
                RefreshPartyUI();
                RefreshOwnedMonsterList();
                return;
            }
        }

        // 2. 全部埋まっていたらそのまま
        // 3. パーティ更新
        RefreshPartyUI();
    }

    // パーティ上部の3枚を更新
    private void RefreshPartyUI()
    {
        int totalHp = 0;

        for (int i = 0; i < partySize; i++)
        {
            // 上のスロットにも MonsterCardView を使う
            partySlots[i].Setup(partyMonsters[i], OnPartyMonsterClicked); // 上はクリック無効なら null
            if (partyMonsters[i] != null) {
                totalHp += partyMonsters[i].hp;
            }
        }

        partyNameText.text = "パーティ1";
        teamHpText.text    = totalHp.ToString();
    }

    // 一覧から選択された
    private void OnPartyMonsterClicked(OwnedMonster owned)
    {
        // 1. 空きスロットを探す
        for (int i = 0; i < partyMonsters.Length; i++)
        {
            if (partyMonsters[i] == owned)
            {
                partyMonsters[i] = null;
                owned.isParty = false;
                RefreshPartyUI();
                RefreshOwnedMonsterList();
                return;
            }
        }

        // 2. 全部埋まっていたらそのまま
        // 3. パーティ更新
        RefreshPartyUI();
    }

    // OKボタンから呼ぶ
    public void OnClickOk()
    {
        // partyMonsters の内容を、どこかの PartyManager やセーブデータに反映
        Debug.Log("パーティ決定！");
        // ① 現在のパーティを GameContext に保存
        if (GameContext.Instance != null)
        {
            GameContext.Instance.SetCurrentParty(partyMonsters);
        }
        else
        {
            Debug.LogWarning("GameContext が見つかりませんでした。");
        }

        AudioManager.Instance.PlayButtonSE();
        SceneManager.LoadScene("HomeScene");
    }
}

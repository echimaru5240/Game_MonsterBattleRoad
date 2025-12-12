using UnityEngine;
using System.Collections.Generic;

public class GameContext : MonoBehaviour
{
    public static GameContext Instance { get; private set; }

    private const string SaveKeyInitialized = "Save_Initialized";

    // 初期配布モンスター（インスペクタで MonsterData を入れておく）
    [SerializeField] private MonsterData[] initialMonsterData;

    // パーティ一覧（3パーティ想定）
    public PartyData[] partyList { get; private set; } = new PartyData[3];

    // 今表示中 / 使用中のパーティ番号
    public int CurrentPartyIndex { get; set; } = 0;

    // 所持モンスター
    public PlayerMonsterInventory inventory;

    private void Awake()
    {
        Debug.Log($"GameContext Awake");
        if (Instance != null && Instance != this)
        {
            Debug.Log($"Instance != null");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeIfNeeded();
        LoadGame();   // 今回はダミー実装
    }

    /// <summary>
    /// 初回起動時だけ、初期モンスターから所持データを生成する
    /// </summary>
    private void InitializeIfNeeded()
    {
        Debug.Log($"モンスターデータの初期化");

        // すでに初期化済みなら何もしない想定
        // if (PlayerPrefs.GetInt(SaveKeyInitialized, 0) == 1)
        //     return;

        if (inventory == null)
        {
            inventory = new PlayerMonsterInventory();
            Debug.LogWarning("GameContext: inventory が設定されていなかったため新規生成しました。");
        }

        if (inventory.ownedMonsters == null)
        {
            inventory.ownedMonsters = new List<OwnedMonster>();
        }

        // 所持モンスターリストをいったんクリア
        inventory.ownedMonsters.Clear();

        // 初期モンスターを所持リストに追加
        if (initialMonsterData != null)
        {
            foreach (var master in initialMonsterData)
            {
                AddMonsterToInventory(master);
            }
        }

        // パーティ一覧を初期化（3パーティ）
        const int partySize = 3;
        partyList = new PartyData[3];
        for (int i = 0; i < partyList.Length; i++)
        {
            partyList[i] = new PartyData();
            partyList[i].partyName = $"パーティ{i + 1}";
            partyList[i].members   = new OwnedMonster[partySize];
        }

        // パーティ1 に上から3体を入れる
        for (int i = 0; i < partyList[0].members.Length; i++)
        {
            if (i < inventory.ownedMonsters.Count)
            {
                var owned = inventory.ownedMonsters[i];
                partyList[0].members[i] = owned;
                owned.isParty = true;
            }
            else
            {
                partyList[0].members[i] = null;
            }
        }

        CurrentPartyIndex = 0;

        // 初期化フラグを立てて保存
        PlayerPrefs.SetInt(SaveKeyInitialized, 1);
        PlayerPrefs.Save();

        Debug.Log("初回起動の初期モンスター & パーティを生成しました。");
        SaveGame(); // ついでにセーブしておく（中身はダミー）
    }

    /// <summary>
    /// インベントリーにモンスター追加
    /// </summary>
    public void AddMonsterToInventory(MonsterData newMonster)
    {
        if (newMonster == null) return;

        var owned = OwnedMonsterFactory.CreateFromMaster(newMonster);

        if (inventory == null)
        {
            inventory = new PlayerMonsterInventory();
        }
        if (inventory.ownedMonsters == null)
        {
            inventory.ownedMonsters = new List<OwnedMonster>();
        }

        inventory.ownedMonsters.Add(owned);
    }

    /// <summary>
    /// 現在のパーティメンバーを設定（partyList[CurrentPartyIndex].members を置き換える）
    /// </summary>
    public void SetCurrentParty(OwnedMonster[] members)
    {
        if (partyList == null || partyList.Length == 0) return;

        partyList[CurrentPartyIndex].members = members;
    }

    public void SetCurrentPartyIndex(int nextIndex)
    {
        if (partyList == null || partyList.Length == 0)
        {
            CurrentPartyIndex = 0;
            return;
        }

        if (nextIndex < 0)
        {
            CurrentPartyIndex = partyList.Length - 1;
        }
        else if (nextIndex >= partyList.Length)
        {
            CurrentPartyIndex = 0;
        }
        else
        {
            CurrentPartyIndex = nextIndex;
        }

        Debug.Log($"CurrentPartyIndex={CurrentPartyIndex}");
    }

    /// <summary>
    /// 全パーティデータをまとめて設定（別シーンから呼びたい場合用）
    /// </summary>
    public void SetPartyData(PartyData[] partyList, int partyIndex)
    {
        if (partyList == null || partyList.Length == 0)
        {
            this.partyList = new PartyData[3];
            for (int i = 0; i < this.partyList.Length; i++)
            {
                this.partyList[i] = new PartyData { members = new OwnedMonster[3], partyName = $"パーティ{i + 1}" };
            }
            CurrentPartyIndex = 0;
            return;
        }

        // 参照をそのまま持つパターン（今の設計だとこれで十分）
        this.partyList = partyList;
        SetCurrentPartyIndex(partyIndex);

        // 必要ならここで SaveGame()
    }

    /// <summary>
    /// 現在選択中パーティのメンバーを取得
    /// </summary>
    public OwnedMonster[] GetCurrentPartyMembers()
    {
        if (partyList == null || partyList.Length == 0)
            return null;

        return partyList[CurrentPartyIndex].members;
    }

    // ▼ ここからはセーブ/ロードの枠だけ用意（中身は後でじっくりでOK）

    [System.Serializable]
    private class SaveData
    {
        public PlayerMonsterInventory saveDataInventory;
        public PartyData[]            saveDataPartyList;
        public int                    saveDataCurrentPartyIndex;
    }

    public void SaveGame()
    {
        // TODO: inventory / partyList / CurrentPartyIndex から SaveData を作って JSON 保存
    }

    private void LoadGame()
    {
        // TODO: JSON 読み込み → SaveData → inventory / partyList / CurrentPartyIndex に反映
        // Save がまだない場合は InitializeIfNeeded の結果が使われる前提
    }
}

using UnityEngine;
using System;
using System.Collections.Generic;

public class GameContext : MonoBehaviour
{
    public static GameContext Instance { get; private set; }

    private const string SaveKeyJson = "Save_GameJson";
    private const int SaveVersion = 2;

    [Header("Master Data")]
    [SerializeField] private MonsterData[] monsterDatabase;

    [Tooltip("初期配布モンスター（未指定なら monsterDatabase 全部）")]
    [SerializeField] private MonsterData[] initialMonsters;

    public PlayerMonsterInventory inventory = new();
    public PartyData[] partyList = new PartyData[3];
    public int CurrentPartyIndex { get; set; } = 0;

    public int OwnedSortKey { get; set; } = 1;
    public bool OwnedSortDescending { get; set; } = true;

    private Dictionary<int, MonsterData> monsterDb;

    private const int PartyCount = 3;
    private const int PartySize  = 3;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildMonsterDb();

        // ★ タイトル画面で選ばせるため、ここでは何もしない
        // LoadGame / InitializeNewGame は Title から呼ぶ
    }

    // ---------- Title から呼ぶ ----------
    public bool HasSave()
        => PlayerPrefs.HasKey(SaveKeyJson) && !string.IsNullOrEmpty(PlayerPrefs.GetString(SaveKeyJson));

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SaveKeyJson);
        PlayerPrefs.Save();
    }

    public bool ContinueGame()
    {
        bool ok = LoadGame();
        if (ok) ApplyVisualsToInventory();
        return ok;
    }

    public void StartNewGameAndSave()
    {
        DeleteSave();
        InitializeNewGame();
        ApplyVisualsToInventory();
        SaveGame();
    }

    public void DebugParty(string tag)
    {
        Debug.Log($"[{tag}]");
        for (int p = 0; p < partyList.Length; p++)
        {
            for (int s = 0; s < partyList[p].members.Length; s++)
            {
                var m = partyList[p].members[s];
                Debug.Log($"party{p}[{s}] = {(m==null ? "null" : $"id={m.monsterId}, name='{m.Name}'")}");
            }
        }
    }

    // =========================
    // Initialization
    // =========================
    private void InitializeNewGame()
    {
        EnsureInventory();
        inventory.ownedMonsters.Clear();

        MonsterData[] source =
            (initialMonsters != null && initialMonsters.Length > 0) ? initialMonsters : monsterDatabase;

        if (source != null)
        {
            foreach (var master in source)
            {
                var owned = OwnedMonsterFactory.CreateFromMaster(master);
                if (owned != null) inventory.ownedMonsters.Add(owned);
            }
        }

        partyList = CreateDefaultPartyList();

        // Party1 に最初の3体
        for (int i = 0; i < PartySize; i++)
            partyList[0].members[i] = (i < inventory.ownedMonsters.Count) ? inventory.ownedMonsters[i] : null;

        CurrentPartyIndex = 0;
        RebuildIsPartyFlags();
    }

    private PartyData[] CreateDefaultPartyList()
    {
        var list = new PartyData[PartyCount];
        for (int p = 0; p < PartyCount; p++)
        {
            list[p] = new PartyData
            {
                partyName = $"パーティ{p + 1}",
                members = new OwnedMonster[PartySize]
            };
            for (int i = 0; i < PartySize; i++)
            {
                list[p].members[i] = null;
            }
        }
        return list;
    }

    private void EnsureInventory()
    {
        if (inventory == null) inventory = new PlayerMonsterInventory();
        if (inventory.ownedMonsters == null) inventory.ownedMonsters = new List<OwnedMonster>();
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

    // =========================
    // Master / Visuals
    // =========================
    private void BuildMonsterDb()
    {
        monsterDb = new Dictionary<int, MonsterData>();
        if (monsterDatabase == null) return;
        foreach (var m in monsterDatabase)
            if (m != null) monsterDb[m.id] = m;
    }

    private void ApplyVisualsToInventory()
    {
        EnsureInventory();
        foreach (var o in inventory.ownedMonsters)
        {
            if (o == null) continue;
            if (!monsterDb.TryGetValue(o.monsterId, out var master) || master == null) continue;

            o.prefab = master.prefab;
            o.monsterFarSprite = master.monsterFarSprite;
            o.monsterNearSprite = master.monsterNearSprite;

            if (string.IsNullOrEmpty(o.Name)) o.Name = master.Name;
            if (o.skills == null || o.skills.Length == 0) o.skills = master.skills;
        }
    }

    // =========================
    // Party helper
    // =========================
    private void RebuildIsPartyFlags()
    {
        EnsureInventory();
        foreach (var o in inventory.ownedMonsters) if (o != null) o.isParty = false;
        if (partyList == null) return;

        foreach (var p in partyList)
            foreach (var m in p.members)
                if (m != null) m.isParty = true;
    }

    // =========================
    // Save / Load（Partyは index 保存）
    // =========================
    [Serializable]
    private class SaveData
    {
        public int version;
        public PlayerMonsterInventory inventory;

        public string[] partyNames;      // 3
        public int[] partyMemberIndex;   // 9

        public int currentPartyIndex;
        public int ownedSortKey;
        public bool ownedSortDescending;
    }

    public void SaveGame()
    {
        EnsureInventory();
        NormalizePartyList();

        var data = new SaveData
        {
            version = SaveVersion,
            inventory = inventory,
            partyNames = new string[PartyCount],
            partyMemberIndex = new int[PartyCount * PartySize],
            currentPartyIndex = CurrentPartyIndex,
            ownedSortKey = OwnedSortKey,
            ownedSortDescending = OwnedSortDescending,
        };

        for (int p = 0; p < PartyCount; p++)
            data.partyNames[p] = partyList[p]?.partyName ?? $"パーティ{p + 1}";

        for (int p = 0; p < PartyCount; p++)
        for (int s = 0; s < PartySize; s++)
        {
            var m = partyList[p].members[s];
            data.partyMemberIndex[p * PartySize + s] = (m == null) ? -1 : inventory.ownedMonsters.IndexOf(m);
        }

        PlayerPrefs.SetString(SaveKeyJson, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private bool LoadGame()
    {
        if (!HasSave()) return false;

        var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKeyJson));
        if (data == null || data.version != SaveVersion) return false;

        inventory = data.inventory ?? new PlayerMonsterInventory();
        EnsureInventory();

        partyList = CreateDefaultPartyList();

        if (data.partyNames != null && data.partyNames.Length == PartyCount)
            for (int p = 0; p < PartyCount; p++)
                partyList[p].partyName = string.IsNullOrEmpty(data.partyNames[p]) ? $"パーティ{p + 1}" : data.partyNames[p];

        if (data.partyMemberIndex != null && data.partyMemberIndex.Length == PartyCount * PartySize)
        {
            for (int p = 0; p < PartyCount; p++)
            for (int s = 0; s < PartySize; s++)
            {
                int idx = data.partyMemberIndex[p * PartySize + s];
                partyList[p].members[s] = (idx >= 0 && idx < inventory.ownedMonsters.Count) ? inventory.ownedMonsters[idx] : null;
            }
        }

        CurrentPartyIndex = Mathf.Clamp(data.currentPartyIndex, 0, PartyCount - 1);
        OwnedSortKey = data.ownedSortKey;
        OwnedSortDescending = data.ownedSortDescending;

        NormalizePartyList();
        RebuildIsPartyFlags();
        return true;
    }

    private void NormalizePartyList()
    {
        if (partyList == null || partyList.Length != PartyCount)
            partyList = CreateDefaultPartyList();

        for (int p = 0; p < PartyCount; p++)
        {
            if (partyList[p] == null)
                partyList[p] = new PartyData { partyName = $"パーティ{p + 1}", members = new OwnedMonster[PartySize] };

            if (partyList[p].members == null || partyList[p].members.Length != PartySize)
                partyList[p].members = new OwnedMonster[PartySize];

            if (string.IsNullOrEmpty(partyList[p].partyName))
                partyList[p].partyName = $"パーティ{p + 1}";
        }
    }
}

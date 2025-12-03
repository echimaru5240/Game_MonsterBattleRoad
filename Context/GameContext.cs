using UnityEngine;

public class GameContext : MonoBehaviour
{
    public static GameContext Instance { get; private set; }

    private const string SaveKeyInitialized = "Save_Initialized";

    // 初期配布モンスター（インスペクタで MonsterData を入れておく）
    [SerializeField] private MonsterData[] initialMonsterData;

    // 今回は3体想定
    public OwnedMonster[] party { get; private set; } = new OwnedMonster[3];

    // 所持モンスター（ScriptableObject でもクラスでもOK）
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
        LoadGame();   // 今回はダミー実装にしておく
    }

    /// <summary>
    /// 初回起動時だけ、初期モンスターから所持データを生成する
    /// </summary>
    private void InitializeIfNeeded()
    {
        Debug.Log($"モンスターデータの初期化");
        // すでに初期化済みなら何もしない
        // if (PlayerPrefs.GetInt(SaveKeyInitialized, 0) == 1)
        //     return;

        if (inventory == null)
        {
            inventory = new();
            Debug.LogError("GameContext: inventory が設定されていません。");
            // return;
        }

        // 所持モンスターリストをいったんクリア
        inventory.ownedMonsters.Clear();

        // 初期モンスターを所持リストに追加
        if (initialMonsterData != null)
        {
            foreach (var master in initialMonsterData)
            {
                if (master == null) continue;
                var owned = OwnedMonsterFactory.CreateFromMaster(master);
                inventory.ownedMonsters.Add(owned);
                Debug.Log($"モンスターを追加しました。{owned.Name}");
            }
        }
        else{

        }

        // パーティの初期値：上から3体を入れる
        party = new OwnedMonster[3];
        for (int i = 0; i < party.Length; i++)
        {
            if (i < inventory.ownedMonsters.Count)
            {
                party[i] = inventory.ownedMonsters[i];
                party[i].isParty = true;
            }
            else
            {
                party[i] = null;
            }
        }

        // 初期化フラグを立てて保存
        PlayerPrefs.SetInt(SaveKeyInitialized, 1);
        PlayerPrefs.Save();

        Debug.Log("初回起動の初期モンスターを生成しました。");
        SaveGame(); // ついでにセーブしておく（後述のダミー）
    }

    /// <summary>
    /// パーティ編成画面から現在のパーティを設定
    /// </summary>
    public void SetCurrentParty(OwnedMonster[] members)
    {
        if (members == null)
        {
            party = new OwnedMonster[3];
            return;
        }

        int size = Mathf.Min(party.Length, members.Length);
        for (int i = 0; i < size; i++)
        {
            party[i] = members[i];
        }

        // 必要ならここで SaveGame() してもOK
    }

    // ▼ ここからはセーブ/ロードの枠だけ用意（中身は後でじっくりでOK）

    [System.Serializable]
    private class SaveData
    {
        // 簡易例：所持モンスターの ID とステータス
        public PlayerMonsterInventory  saveDataInventory;
        public OwnedMonster[] saveDataParty; // パーティに入っているのが ownedMonsters の何番か
    }

    public void SaveGame()
    {
        // ★ 本格的なセーブ処理は後で詰めるとして、ここでは枠だけ

        // TODO: inventory.ownedMonsters と party から SaveData を作って JSON 保存
    }

    private void LoadGame()
    {
        // ★ Saveがまだない場合は InitializeIfNeeded の結果が使われる

        // TODO: JSON 読み込み → SaveData → inventory / party に反映
    }
}

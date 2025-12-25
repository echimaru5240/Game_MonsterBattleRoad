using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class OwnedMonster
{
    public MonsterData master;

    [Header("基礎情報")]
    public string Name;
    public bool isParty;

    // =========================
    // レベル / 経験値
    // =========================
    [Header("成長")]
    [Min(1)] public int level = 1;
    [Min(0)] public int exp = 0;
    [Min(0)] public int totalExp = 0;

    [Tooltip("未使用ステータスポイント")]
    [Min(0)] public int unspentStatPoints = 0;

    // =========================
    // 基礎ステ（masterからコピー）
    // =========================
    [Header("基礎ステータス（masterから生成）")]
    public int baseHp;
    public int baseAtk;
    public int baseMgc;
    public int baseDef;
    public int baseAgi;

    // =========================
    // 振り分け（保存するのは「何ポイント振ったか」）
    // =========================
    [Header("ステ振り（ポイント配分）")]
    [Min(0)] public int hpPoints;
    [Min(0)] public int atkPoints;
    [Min(0)] public int mgcPoints;
    [Min(0)] public int defPoints;
    [Min(0)] public int agiPoints;

    // =========================
    // 1ポイントあたりの上昇量
    // ※モンスター個体差を出したいなら master 側に持たせてもOK
    // =========================
    [Header("ポイント上昇量（1ptあたり）")]
    public float hpPerPoint = 18.3f;
    public float atkPerPoint = 2.8f;
    public float mgcPerPoint = 2.8f;
    public float defPerPoint = 3.7f;
    public float agiPerPoint = 1.6f;

    // =========================
    // 「現在ステータス」は計算で出す（保存しない）
    // =========================
    public float hp  => baseHp  + hpPoints  * hpPerPoint;
    public float atk => baseAtk + atkPoints * atkPerPoint;
    public float mgc => baseMgc + mgcPoints * mgcPerPoint;
    public float def => baseDef + defPoints * defPerPoint;
    public float agi => baseAgi + agiPoints * agiPerPoint;

    [Header("スキル")]
    public SkillID[] skills;

    [Header("表示用")]
    public GameObject prefab;
    public Sprite monsterFarSprite;
    public Sprite monsterNearSprite;

    // =========================
    // イベント（UI更新用）
    // =========================
    [NonSerialized]
    public Action OnChanged;

    private void NotifyChanged()
    {
        OnChanged?.Invoke();
    }

    // =========================
    // 経験値テーブル設定
    // =========================
    public const int MaxLevel = 100;

    /// <summary>
    /// 次のレベルまでに必要な経験値（例：緩やか→後半きついカーブ）
    /// 「次レベルに必要な量」を返すタイプ
    /// </summary>
    public static int GetRequiredExpForNext(int currentLevel)
    {
        // Lv1→2 から計算。MaxLevel 到達時は 0
        if (currentLevel >= MaxLevel) return 0;
        if (currentLevel == 0) return 0;

        // 例：ベース + 二次項（調整しやすい）
        // Lv1:  20 +  10 = 30
        // Lv2:  20 +  40 = 60
        // Lv3:  20 +  90 = 110
        // Lv4:  20 +  160 = 180
        // Lv10: 20 + 300 = 320
        // Lv30: 20 + 2700 = 2720
        int lv = Mathf.Max(1, currentLevel);
        return 20 + (lv * lv * 10);
    }

    public int RequiredExpToNext => GetRequiredExpForNext(level);

    // =========================
    // ステ振り
    // =========================
    public bool CanSpendPoint(int amount = 1) => unspentStatPoints >= amount;

    public bool TryAllocate(StatType type, int amount = 1)
    {
        if (amount <= 0) return false;
        if (unspentStatPoints < amount) return false;

        switch (type)
        {
            case StatType.HP:  hpPoints  += amount; break;
            case StatType.ATK: atkPoints += amount; break;
            case StatType.MGC: mgcPoints += amount; break;
            case StatType.DEF: defPoints += amount; break;
            case StatType.AGI: agiPoints += amount; break;
            default: return false;
        }

        unspentStatPoints -= amount;
        NotifyChanged();   // ★ここ
        return true;
    }

    // =========================
    // 経験値獲得 → レベルアップ
    // =========================
    /// <summary>
    /// 経験値を加算し、必要なら複数回レベルアップする。
    /// levelUpで得るポイントは levelUpPointsPerLevel で指定。
    /// </summary>
    public int GainExp(int amount, int levelUpPointsPerLevel = 5)
    {
        if (amount <= 0) return 0;
        if (level >= MaxLevel) return 0;
        Debug.Log("GainExp");

        exp += amount;
        totalExp += amount;

        int leveledUpCount = 0;
        while (level < MaxLevel)
        {
            int need = RequiredExpToNext;
            if (need <= 0) break;

            if (exp < need) break;

            exp -= need;
            level++;
            leveledUpCount++;
            Debug.Log("LevelUp");

            unspentStatPoints += Mathf.Max(0, levelUpPointsPerLevel);
        }

        // MaxLevel到達時のexp処理（好みで 0 にするなど）
        if (level >= MaxLevel)
        {
            // exp = 0; // ←Maxで余剰経験値を消すなら
        }

        if (leveledUpCount > 0)
        {
            NotifyChanged();   // ★ここ
        }

        return leveledUpCount;
    }

    // =========================
    // Factory
    // =========================
    public static OwnedMonster CreateOwnedFromMaster(MonsterData master)
        => OwnedMonsterFactory.CreateFromMaster(master);
}

public static class OwnedMonsterFactory
{
    public static OwnedMonster CreateFromMaster(MonsterData master)
    {
        if (master == null)
        {
            Debug.LogError("OwnedMonsterFactory.CreateFromMaster: master is null");
            return null;
        }

        var owned = new OwnedMonster();

        owned.master = master;
        owned.Name = master.Name;

        // 基礎ステ（master からコピー）
        owned.baseHp  = master.hp;
        owned.baseAtk = master.atk;
        owned.baseMgc = master.mgc;
        owned.baseDef = master.def;
        owned.baseAgi = master.agi;

        // 初期値
        owned.level = 1;
        owned.exp = 0;
        owned.unspentStatPoints = 5;

        // 表示用
        owned.prefab = master.prefab;
        owned.monsterFarSprite = master.monsterFarSprite;
        owned.monsterNearSprite = master.monsterNearSprite;

        // スキル
        owned.skills = master.skills;

        return owned;
    }
}


[CreateAssetMenu(fileName = "PlayerMonsterInventory", menuName = "BattleRoad/Player Monster Inventory")]
public class PlayerMonsterInventory : ScriptableObject
{
    public List<OwnedMonster> ownedMonsters = new();
}

public class PartyData
{
    public string partyName;
    public OwnedMonster[] members = new OwnedMonster[3];

    public PartyData()
    {
        partyName = "";
        members = new OwnedMonster[3];
    }
}
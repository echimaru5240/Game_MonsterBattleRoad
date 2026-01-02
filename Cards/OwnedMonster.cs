using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class OwnedMonster
{
    // =========================
    // 識別子（Master復元用）
    // =========================
    [Header("識別子")]
    public int monsterId = 0;   // MonsterData.id と一致させる

    // =========================
    // 基礎情報
    // =========================
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
    [Header("基礎ステータス")]
    public int baseHp;
    public int baseAtk;
    public int baseMgc;
    public int baseDef;
    public int baseAgi;

    // =========================
    // ステ振り（保存対象）
    // =========================
    [Header("ステ振り（ポイント配分）")]
    [Min(0)] public int hpPoints;
    [Min(0)] public int atkPoints;
    [Min(0)] public int mgcPoints;
    [Min(0)] public int defPoints;
    [Min(0)] public int agiPoints;

    // =========================
    // 1ポイントあたりの上昇量
    // =========================
    [Header("ポイント上昇量（1ptあたり）")]
    public float hpPerPoint  = 18.3f;
    public float atkPerPoint = 2.8f;
    public float mgcPerPoint = 2.8f;
    public float defPerPoint = 3.7f;
    public float agiPerPoint = 1.6f;

    // =========================
    // 現在ステ（計算値・非保存）
    // =========================
    public float hp  => baseHp  + hpPoints  * hpPerPoint;
    public float atk => baseAtk + atkPoints * atkPerPoint;
    public float mgc => baseMgc + mgcPoints * mgcPerPoint;
    public float def => baseDef + defPoints * defPerPoint;
    public float agi => baseAgi + agiPoints * agiPerPoint;

    // =========================
    // スキル
    // =========================
    [Header("スキル")]
    public SkillID[] skills;

    // =========================
    // 表示用（※セーブしない）
    // =========================
    [Header("表示用")]
    public GameObject prefab;
    public Sprite monsterFarSprite;
    public Sprite monsterNearSprite;

    // =========================
    // UI通知
    // =========================
    [NonSerialized]
    public Action OnChanged;

    private void NotifyChanged() => OnChanged?.Invoke();

    // =========================
    // 経験値テーブル
    // =========================
    public const int MaxLevel = 100;

    public static int GetRequiredExpForNext(int currentLevel)
    {
        if (currentLevel >= MaxLevel) return 0;
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
        if (amount <= 0 || unspentStatPoints < amount) return false;

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
        NotifyChanged();
        return true;
    }

    // =========================
    // 経験値獲得
    // =========================
    public int GainExp(int amount, int levelUpPointsPerLevel = 5)
    {
        if (amount <= 0 || level >= MaxLevel) return 0;

        exp += amount;
        totalExp += amount;

        int leveledUp = 0;
        while (level < MaxLevel && exp >= RequiredExpToNext)
        {
            exp -= RequiredExpToNext;
            level++;
            leveledUp++;
            unspentStatPoints += Mathf.Max(0, levelUpPointsPerLevel);
        }

        if (leveledUp > 0)
            NotifyChanged();

        return leveledUp;
    }
}

// =========================
// Factory
// =========================
public static class OwnedMonsterFactory
{
    public static OwnedMonster CreateFromMaster(MonsterData master)
    {
        if (master == null)
        {
            Debug.LogError("OwnedMonsterFactory: master is null");
            return null;
        }

        var owned = new OwnedMonster
        {
            monsterId = master.id,
            Name = master.Name,

            baseHp  = master.hp,
            baseAtk = master.atk,
            baseMgc = master.mgc,
            baseDef = master.def,
            baseAgi = master.agi,

            hpPerPoint  = master.hpPerPoint,
            atkPerPoint = master.atkPerPoint,
            mgcPerPoint = master.mgcPerPoint,
            defPerPoint = master.defPerPoint,
            agiPerPoint = master.agiPerPoint,

            level = 1,
            exp = 0,
            unspentStatPoints = 5,

            prefab = master.prefab,
            monsterFarSprite = master.monsterFarSprite,
            monsterNearSprite = master.monsterNearSprite,

            skills = master.skills
        };

        return owned;
    }

}

// =========================
// Inventory / Party
// =========================
[Serializable]
public class PlayerMonsterInventory
{
    public List<OwnedMonster> ownedMonsters = new();
}

[Serializable]
public class PartyData
{
    public string partyName;
    public OwnedMonster[] members = new OwnedMonster[3];

    public PartyData()
    {
        partyName = "";
        members = new OwnedMonster[3];
        for (int i = 0; i < members.Length; i++)
        {
            members[i] = null;
        }
    }
}

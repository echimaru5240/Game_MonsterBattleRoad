using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class OwnedMonster
{
    public MonsterData master;

    [Header("基礎情報")]
    public string Name;
    public bool isParty;

    [Header("ステータス")]
    public int hp;
    public int atk;
    public int mgc; // 魔法や回復用
    public int def;
    public int agi;

    [Header("スキル")]
    public SkillID[] skills;

    [Header("表示用")]
    public GameObject prefab;
    public Sprite monsterFarSprite;
    public Sprite monsterNearSprite;

    public static OwnedMonster CreateOwnedFromMaster(MonsterData master)
    {
        if (master == null)
        {
            Debug.LogError("CreateOwnedFromMaster: master is null");
            return null;
        }

        var owned = new OwnedMonster();

        // 参照
        owned.master = master;

        // 名前
        owned.Name = master.Name;

        // ステータス
        owned.hp  = master.hp ;
        owned.atk = master.atk;
        owned.mgc = master.mgc;
        owned.def = master.def;
        owned.agi = master.agi;

        // 表示用
        owned.prefab = master.prefab;
        owned.monsterFarSprite = master.monsterFarSprite;  // DQ風遠景差し替えがあるなら変更
        owned.monsterNearSprite = master.monsterNearSprite;

        // スキル
        owned.skills = master.skills;

        return owned;
    }

}

public static class OwnedMonsterFactory
{
    public static OwnedMonster CreateFromMaster(MonsterData master)
    {
        if (master == null) return null;

        var owned = new OwnedMonster();
        // 参照
        owned.master = master;

        // 名前
        owned.Name = master.Name;

        // ステータス
        owned.hp  = master.hp ;
        owned.atk = master.atk;
        owned.mgc = master.mgc;
        owned.def = master.def;
        owned.agi = master.agi;

        // 表示用
        owned.prefab = master.prefab;
        owned.monsterFarSprite = master.monsterFarSprite;  // DQ風遠景差し替えがあるなら変更
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
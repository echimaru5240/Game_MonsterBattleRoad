using UnityEngine;
using System.Collections.Generic;

public class MonsterBattleData: MonoBehaviour
{
    [Header("基礎情報")]
    public string Name;

    [Header("ステータス")]
    public float hp;
    public float atk;
    public float mgc; // 魔法や回復用
    public float def;
    public float agi;

    [Header("スキル")]
    public SkillID[] skills;

    [Header("表示用")]
    public GameObject prefab;
    public Sprite monsterFarSprite;
    public Sprite monsterNearSprite;

    public static MonsterBattleData CreateBattleFromMasterData(MonsterData master)
    {
        if (master == null)
        {
            Debug.LogError("CreateOwnedFromMaster: master is null");
            return null;
        }

        var battleData = new MonsterBattleData();

        // 名前
        battleData.Name = master.Name;

        // ステータス
        battleData.hp  = (float)master.hp ;
        battleData.atk = (float)master.atk;
        battleData.mgc = (float)master.mgc;
        battleData.def = (float)master.def;
        battleData.agi = (float)master.agi;

        // 表示用
        battleData.prefab = master.prefab;
        battleData.monsterFarSprite = master.monsterFarSprite;  // DQ風遠景差し替えがあるなら変更
        battleData.monsterNearSprite = master.monsterNearSprite;

        // スキル
        battleData.skills = master.skills;

        return battleData;
    }

    public static MonsterBattleData CreateBattleFromOwnedData(OwnedMonster master)
    {
        if (master == null)
        {
            Debug.LogError("CreateOwnedFromMaster: master is null");
            return null;
        }

        var battleData = new MonsterBattleData();

        // 名前
        battleData.Name = master.Name;

        // ステータス
        battleData.hp  = master.hp;
        battleData.atk = master.atk;
        battleData.mgc = master.mgc;
        battleData.def = master.def;
        battleData.agi = master.agi;

        // 表示用
        battleData.prefab = master.prefab;
        battleData.monsterFarSprite = master.monsterFarSprite;  // DQ風遠景差し替えがあるなら変更
        battleData.monsterNearSprite = master.monsterNearSprite;

        // スキル
        battleData.skills = master.skills;

        return battleData;
    }
}

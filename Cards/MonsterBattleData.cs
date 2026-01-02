using UnityEngine;
using System.Collections.Generic;

public class MonsterBattleData
{
    [Header("基礎情報")]
    public string Name;

    [Header("ステータス")]
    public float hp;
    public float atk;
    public float mgc; // 魔法や回復用
    public float def;
    public float agi;

    public StatusAilmentType statusAilmentType;

    [Header("スキル")]
    public SkillID[] skills;

    [Header("表示用")]
    public GameObject prefab;
    public Sprite monsterFarSprite;
    public Sprite monsterNearSprite;

    // =========================
    // 1ポイントあたりの上昇量
    // =========================
    private float hpPerPoint  = 18.3f;
    private float atkPerPoint = 2.8f;
    private float mgcPerPoint = 2.8f;
    private float defPerPoint = 3.7f;
    private float agiPerPoint = 1.6f;


    public static MonsterBattleData CreateBattleFromMasterData(MonsterData master, int stageLevel)
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

        battleData.hpPerPoint  = master.hpPerPoint;
        battleData.atkPerPoint = master.atkPerPoint;
        battleData.mgcPerPoint = master.mgcPerPoint;
        battleData.defPerPoint = master.defPerPoint;
        battleData.agiPerPoint = master.agiPerPoint;

        // 表示用
        battleData.prefab = master.prefab;
        battleData.monsterFarSprite = master.monsterFarSprite;  // DQ風遠景差し替えがあるなら変更
        battleData.monsterNearSprite = master.monsterNearSprite;

        // スキル
        battleData.skills = master.skills;

        // ステータスポイント割り振り
        ApplyAllocatedPoints(battleData, stageLevel * 5);

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

    // 共通：ポイント配分
    private static void ApplyAllocatedPoints(MonsterBattleData b, int totalPoints)
    {
        if (b == null || totalPoints <= 0) return;

        // profile未設定時のデフォルト比率（HP多め）
        float wHp  = 3f;
        float wAtk = 2f;
        float wMgc = 2f;
        float wDef = 2f;
        float wAgi = 1f;

        float sum = Mathf.Max(0.0001f, wHp + wAtk + wMgc + wDef + wAgi);

        int addHp  = Mathf.FloorToInt(totalPoints * (wHp  / sum));
        int addAtk = Mathf.FloorToInt(totalPoints * (wAtk / sum));
        int addMgc = Mathf.FloorToInt(totalPoints * (wMgc / sum));
        int addDef = Mathf.FloorToInt(totalPoints * (wDef / sum));
        int addAgi = Mathf.FloorToInt(totalPoints * (wAgi / sum));

        int used = addHp + addAtk + addMgc + addDef + addAgi;
        int rem  = totalPoints - used;

        // 余りの行き先
        StatType remTo = StatType.HP;
        if (rem > 0)
        {
            switch (remTo)
            {
                case StatType.HP:  addHp  += rem; break;
                case StatType.ATK: addAtk += rem; break;
                case StatType.MGC: addMgc += rem; break;
                case StatType.DEF: addDef += rem; break;
                case StatType.AGI: addAgi += rem; break;
            }
        }

        // 反映（ここは “ポイント=そのまま加算” の設計）
        b.hp  += (b.hpPerPoint  * addHp );
        b.atk += (b.atkPerPoint * addAtk);
        b.mgc += (b.mgcPerPoint * addMgc);
        b.def += (b.defPerPoint * addDef);
        b.agi += (b.agiPerPoint * addAgi);
    }
}

using UnityEngine;
using System.Collections.Generic;

public enum SkillAction
{
    ATTACK, // 攻撃
    HEAL,   // 回復
    SPECIAL    // 補助
}

public enum SkillCategory
{
    PHYSICAL,       // 物理ダメージ
    MAGICAL,        // 魔法
    SPECIAL,        // 特技
    BREATH,         // ブレス
    SKILL_CATEGORY_NUM
}

public enum TargetType
{
    ENEMY_SINGLE,  // 敵単体対象
    ENEMY_ALL,     // 敵全体対象
    PLAYER_SINGLE, // 味方単体対象
    PLAYER_ALL     // 味方全体対象
}

public enum ElementType
{
    NONE,
    FIRE,
    WATER,
    ICE,
    EARTH,
    WIND,
    LIGHT,
    DARK,
    ELEMENT_TYPE_NUM
}

public enum StatusAilmentType
{
    NONE,
    POISON,     // 毒
    PARALYSIS,  // マヒ
    SLEEP,      // ねむり
    STUN,       // ひるみ・気絶
    BURN,       // 火傷
    FREEZE,       // 凍結
    CONFUSION,  // 混乱
    CURSE       // 呪い
}

[System.Serializable]
public class StatusAilmentEffect
{
    public StatusAilmentType statusAilmentType; // 状態異常

    [Tooltip("状態異常付与率")]
    [Range(0f, 1f)]
    public float ailmentChance = 1f;         // 命中率(0?1)
}

public enum StatusBuffType
{
    NONE,
    ATTACK,
    DEFENSE,
    MAGICPOWER,
    SPEED,
    ACCURACY,
    CRITICALRATE
}

[System.Serializable]
public class StatusChangeEffect
{
    public StatusBuffType statType; // 攻撃/防御/素早さなど

    [Tooltip("段階や倍率の指標。+1で小アップ、+2で大アップなど好きに解釈してOK")]
    public int amountStage = 1;   // プラスならバフ、マイナスならデバフとして使う運用もアリ

    [Tooltip("効果ターン数。0ならこのターンのみ、1ならこのターン+次ターンなど自由に設計")]
    public int durationTurns = 3;
}

[CreateAssetMenu(
    fileName = "NewSkill",
    menuName = "Battle/Skill",
    order = 1)]
public class Skill : ScriptableObject
{
    public SkillID skillActionID;    // モーションパターン
    [Header("基本情報")]
    public string skillName;            // 技の名前
    [Header("行動の種類")]
    public SkillAction action;              // 技の種類（攻撃 / 回復 / 補助）
    public SkillCategory category;      //カテゴリー
    public TargetType targetType;       // 対象範囲（単体 / 全体）
    public ElementType elementType;     // 属性
    public StatusAilmentEffect statusAilmentEffect; // 状態異常
    public StatusChangeEffect statusChangeEffect; // ステータスバフ
    [Header("パラメータ")]
    public int power;                   // 威力（数値）
    [Range(0f, 1f)]
    public float accuracy = 1f;         // 命中率(0?1)
    [Range(0f, 1f)]
    public int criticalRate ;           // クリティカル率
}


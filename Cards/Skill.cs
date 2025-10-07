using UnityEngine;

public enum SkillType
{
    Attack, // 攻撃
    Heal,   // 回復
    Buff    // 補助
}

public enum SkillTargetType
{
    Single, // 単体対象
    All     // 全体対象
}

[System.Serializable]
public class Skill
{
    public string skillName;          // 技の名前
    public SkillType type;            // 技の種類（攻撃 / 回復 / 補助）
    public int power;                 // 威力（数値）
    public int criticalChance;        // クリティカル率
    public int missChance;            // ミス率
    public SkillTargetType targetType; // 対象範囲（単体 / 全体）
}

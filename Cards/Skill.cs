using UnityEngine;

public enum SkillType
{
    Attack, // �U��
    Heal,   // ��
    Buff    // �⏕
}

public enum SkillTargetType
{
    Single, // �P�̑Ώ�
    All     // �S�̑Ώ�
}

[System.Serializable]
public class Skill
{
    public string skillName;          // �Z�̖��O
    public SkillType type;            // �Z�̎�ށi�U�� / �� / �⏕�j
    public int power;                 // �З́i���l�j
    public int criticalChance;        // �N���e�B�J����
    public int missChance;            // �~�X��
    public SkillTargetType targetType; // �Ώ۔͈́i�P�� / �S�́j
}

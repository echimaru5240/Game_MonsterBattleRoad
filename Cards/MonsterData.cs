using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "MonsterData", menuName = "BattleRoad/Monster Data")]
public class MonsterData : ScriptableObject
{
    [Header("基礎情報")]
    public int id;
    public string Name;

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
}

public enum StatType
{
    HP,
    ATK,
    MGC,
    DEF,
    AGI
}

using UnityEngine;

[CreateAssetMenu(menuName = "Card/MonsterCard")]
public class MonsterCard : Card
{
    [Header("ステータス")]
    public int hp;
    public int attack;
    public int magicPower; // 魔法や回復用
    public int defense;
    public int speed;

    [Header("スキル")]
    public Skill[] skills;

    [Header("表示用")]
    public GameObject prefab;

    private void OnEnable()
    {
        type = CardType.Monster;
    }
}


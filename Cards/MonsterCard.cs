using UnityEngine;

[CreateAssetMenu(menuName = "Card/MonsterCard")]
public class MonsterCard : Card
{
    [Header("�X�e�[�^�X")]
    public int hp;
    public int attack;
    public int magicPower; // ���@��񕜗p
    public int defense;
    public int speed;

    [Header("�X�L��")]
    public Skill[] skills;

    [Header("�\���p")]
    public GameObject prefab;

    private void OnEnable()
    {
        type = CardType.Monster;
    }
}


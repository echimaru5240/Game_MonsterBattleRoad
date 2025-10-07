using UnityEngine;

public enum CardType
{
    Monster,
    Boss,
    SuperBoss,
    // Weapon, Special �� �����ǉ�
}

public abstract class Card : ScriptableObject
{
    [Header("��{���")]
    public string cardName;
    public CardType type;
    // public Sprite icon;
    // public string description;

    [Header("�t�B�j�b�V���[�i�Ƃǂ߂̈ꌂ�j")]
    public string finisherName;
    public int finisherDamage;
}

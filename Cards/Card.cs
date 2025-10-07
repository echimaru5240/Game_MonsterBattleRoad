using UnityEngine;

public enum CardType
{
    Monster,
    Boss,
    SuperBoss,
    // Weapon, Special ← 将来追加
}

public abstract class Card : ScriptableObject
{
    [Header("基本情報")]
    public string cardName;
    public CardType type;
    // public Sprite icon;
    // public string description;

    [Header("フィニッシャー（とどめの一撃）")]
    public string finisherName;
    public int finisherDamage;
}

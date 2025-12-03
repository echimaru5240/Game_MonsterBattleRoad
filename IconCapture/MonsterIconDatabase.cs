using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterIconDatabase", menuName = "BattleRoad/Monster Icon Database")]
public class MonsterIconDatabase : ScriptableObject
{
    public List<MonsterIconData> monsters = new();

    // •Ö—˜ƒƒ\ƒbƒh—á
    public MonsterIconData GetById(string id)
    {
        return monsters.Find(m => m.id == id);
    }
}

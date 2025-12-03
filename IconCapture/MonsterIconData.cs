using UnityEngine;

[CreateAssetMenu(fileName = "MonsterIconData", menuName = "BattleRoad/Monster Icon Data")]
public class MonsterIconData : ScriptableObject
{
    public string id;                // "slime_001" などユニークID
    public string displayName;       // 表示名

    [Header("3Dモデル")]
    public GameObject monsterPrefab; // バトル用の3Dプレハブ

    [Header("UI用アイコン")]
    public Sprite iconSprite;        // 生成されたアイコン画像
}

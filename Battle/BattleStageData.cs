using UnityEngine;

public enum BattleStageType
{
    First,     // 初戦
    Final,     // 決勝戦
    Boss,      // 魔王戦
    SuperBoss  // 大魔王戦
}

[CreateAssetMenu(fileName = "BattleStageData", menuName = "GameData/Battle Stage")]
public class BattleStageData : ScriptableObject
{
    [Header("敵チーム構成")]
    public MonsterData[] enemyTeam;

    [Header("ステージ演出")]
    public string stageName;   // 例: 「初戦」「決勝戦」「魔王戦」
    public Sprite background;  // ステージ背景（2Dなら）
    public AudioClip bgm;      // 戦闘BGM
    public int stageLevel;

    [Header("特殊設定")]
    public bool isBossStage;   // 魔王 or 大魔王戦なら true
}
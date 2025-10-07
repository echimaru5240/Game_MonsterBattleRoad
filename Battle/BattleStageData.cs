using UnityEngine;

public enum BattleStageType
{
    First,     // ����
    Final,     // ������
    Boss,      // ������
    SuperBoss  // �喂����
}

[CreateAssetMenu(fileName = "BattleStageData", menuName = "GameData/Battle Stage")]
public class BattleStageData : ScriptableObject
{
    [Header("�G�`�[���\��")]
    public MonsterCard[] enemyTeam;

    [Header("�X�e�[�W���o")]
    public string stageName;   // ��: �u����v�u������v�u������v
    public Sprite background;  // �X�e�[�W�w�i�i2D�Ȃ�j
    public AudioClip bgm;      // �퓬BGM

    [Header("����ݒ�")]
    public bool isBossStage;   // ���� or �喂����Ȃ� true
}
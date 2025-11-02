using UnityEngine;
using System.Collections.Generic;

public class BattleSpawner : MonoBehaviour
{
    [Header("Spawn Areas")]
    [SerializeField] private Transform playerArea;
    [SerializeField] private Transform enemyArea;
    public float posZ = 5f;
    public float bossPosZ = 8f;

    public List<MonsterController> PlayerControllers { get; private set; } = new();
    public List<MonsterController> EnemyControllers { get; private set; } = new();

    // ================================
    // �X�|�[��
    // ================================
    public void Spawn(MonsterCard[] playerCards, MonsterCard[] enemyCards)
    {
        Clear();

        SpawnSide(playerCards, playerArea, PlayerControllers, isPlayer: true);
        SpawnSide(enemyCards, enemyArea, EnemyControllers, isPlayer: false);
    }

    private void SpawnSide(MonsterCard[] cards, Transform area, List<MonsterController> controllerList, bool isPlayer)
    {
        if (cards == null || cards.Length == 0) return;

        float spacing = 2.2f;
        for (int i = 0; i < cards.Length; i++)
        {
            var card = cards[i];
            if (card?.prefab == null)
            {
                Debug.LogWarning($"�J�[�h {card?.name ?? "null"} ��Prefab���ݒ肳��Ă��܂���B");
                continue;
            }

            // ����
            var obj = Instantiate(card.prefab, area);
            float offset = (i - (cards.Length - 1) / 2f) * spacing;
            obj.transform.localPosition = new Vector3(offset, 0, 0);

            var ctrl = obj.GetComponent<MonsterController>();
            if (ctrl == null)
            {
                Debug.LogWarning($"{obj.name} �� MonsterController ������܂���B");
                continue;
            }

            // MonsterCard�̏���Controller�փR�s�[
            ctrl.InitializeFromCard(card, isPlayer);
            controllerList.Add(ctrl);
        }
    }

    // ================================
    // �z�u�ʒu
    // ================================
    public void SetSpawnAreaPositions(bool isBossStage)
    {
        posZ = isBossStage ? bossPosZ : posZ;
        if (playerArea != null) playerArea.localPosition = new Vector3(0, 0, -posZ);
        if (enemyArea != null) enemyArea.localPosition = new Vector3(0, 0, posZ);
    }

    // ================================
    // �������o�p
    // ================================
    public void SetResultObject()
    {
        foreach (var enemy in EnemyControllers)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }

        foreach (var player in PlayerControllers)
        {
            if (player == null) continue;
            Vector3 rot = player.transform.eulerAngles;
            rot.y += 180f;
            player.transform.eulerAngles = rot;
        }
    }

    public void SetSpawnAreaActive(bool active)
    {
        if (playerArea) playerArea.gameObject.SetActive(active);
        if (enemyArea) enemyArea.gameObject.SetActive(active);
    }

    // ================================
    // �N���A
    // ================================
    public void Clear()
    {
        foreach (var ctrl in PlayerControllers)
            if (ctrl != null) Destroy(ctrl.gameObject);
        foreach (var ctrl in EnemyControllers)
            if (ctrl != null) Destroy(ctrl.gameObject);

        PlayerControllers.Clear();
        EnemyControllers.Clear();
    }
}

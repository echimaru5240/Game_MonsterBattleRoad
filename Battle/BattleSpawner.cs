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
    // スポーン
    // ================================
    public void Spawn(MonsterBattleData[] playerMonsters, MonsterBattleData[] enemyMonsters)
    {
        Clear();

        SpawnSide(playerMonsters, playerArea, PlayerControllers, isPlayer: true);
        SpawnSide(enemyMonsters, enemyArea, EnemyControllers, isPlayer: false);
    }

    private void SpawnSide(MonsterBattleData[] monsters, Transform area, List<MonsterController> controllerList, bool isPlayer)
    {
        if (monsters == null || monsters.Length == 0) return;

        float spacing = 2.2f;
        for (int i = 0; i < monsters.Length; i++)
        {
            var monster = monsters[i];
            if (monster?.prefab == null)
            {
                Debug.LogWarning($"カード {monster?.Name ?? "null"} にPrefabが設定されていません。");
                continue;
            }

            // 生成
            var obj = Instantiate(monster.prefab, area);
            float offset = (i - (monsters.Length - 1) / 2f) * spacing;
            obj.transform.localPosition = new Vector3(offset, 0, 0);

            var ctrl = obj.GetComponent<MonsterController>();
            if (ctrl == null)
            {
                Debug.LogWarning($"{obj.name} に MonsterController がありません。");
                continue;
            }

            // MonsterCardの情報をControllerへコピー
            ctrl.InitializeFromData(monster, isPlayer);
            controllerList.Add(ctrl);
        }
    }

    // ================================
    // 配置位置
    // ================================
    public void SetSpawnAreaPositions(bool isBossStage)
    {
        posZ = isBossStage ? bossPosZ : posZ;
        if (playerArea != null) playerArea.localPosition = new Vector3(0, 0, -posZ);
        if (enemyArea != null) enemyArea.localPosition = new Vector3(0, 0, posZ);
    }

    // ================================
    // 勝利演出用
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
    // クリア
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

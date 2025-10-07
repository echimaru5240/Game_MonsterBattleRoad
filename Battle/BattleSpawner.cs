using UnityEngine;
using System.Collections.Generic;

public class BattleSpawner : MonoBehaviour
{
    [Header("Spawn Areas")]
    [SerializeField] private Transform playerArea;
    [SerializeField] private Transform enemyArea;

    [Header("Camera Manager")]
    [SerializeField] private CameraManager cameraManager; // ← 追加：CameraManager参照

    public List<GameObject> PlayerObjects { get; private set; } = new();
    public List<GameObject> EnemyObjects { get; private set; } = new();

    public List<MonsterCard> PlayerCards { get; private set; } = new();
    public List<MonsterCard> EnemyCards { get; private set; } = new();

    public List<MonsterController> PlayerControllers { get; private set; } = new();
    public List<MonsterController> EnemyControllers { get; private set; } = new();

    // カード → コントローラの対応表
    public Dictionary<MonsterCard, MonsterController> PlayerMap { get; private set; } = new();
    public Dictionary<MonsterCard, MonsterController> EnemyMap  { get; private set; } = new();

    // ================================
    // スポーン
    // ================================
    public void Spawn(MonsterCard[] playerCards, MonsterCard[] enemyCards)
    {
        Clear();

        SpawnSide(playerCards, playerArea, PlayerObjects, PlayerCards, PlayerControllers, PlayerMap, isEnemy: false);
        SpawnSide(enemyCards, enemyArea, EnemyObjects, EnemyCards, EnemyControllers, EnemyMap, isEnemy: true);
    }

    private void SpawnSide(
        MonsterCard[] cards, Transform parent,
        List<GameObject> objectList, List<MonsterCard> cardList,
        List<MonsterController> controllerList, Dictionary<MonsterCard, MonsterController> map,
        bool isEnemy)
    {
        if (cards == null || cards.Length == 0) return;

        float spacing = 2f;

        for (int i = 0; i < cards.Length; i++)
        {
            var card = cards[i];
            if (card?.prefab == null) continue;

            var obj = Object.Instantiate(card.prefab, parent);

            // 位置決定
            if (cards.Length == 1)
            {
                obj.transform.localPosition = Vector3.zero;
            }
            else
            {
                float offset = (i - (cards.Length - 1) / 2f) * spacing;
                obj.transform.localPosition = new Vector3(offset, 0, 0);
            }

            objectList.Add(obj);
            cardList.Add(card);

            var ctrl = obj.GetComponent<MonsterController>();
            if (ctrl != null)
            {
                ctrl.Init(cameraManager, isEnemy, card); // ← CameraManagerを渡すように修正
                controllerList.Add(ctrl);
                map[card] = ctrl;
            }
            else
            {
                Debug.LogWarning($"{obj.name} に MonsterController がアタッチされていません。");
            }
        }
    }

    // ================================
    // クリア
    // ================================
    public void Clear()
    {
        foreach (var obj in PlayerObjects) if (obj) Destroy(obj);
        foreach (var obj in EnemyObjects) if (obj) Destroy(obj);

        PlayerObjects.Clear(); EnemyObjects.Clear();
        PlayerCards.Clear(); EnemyCards.Clear();
        PlayerControllers.Clear(); EnemyControllers.Clear();
        PlayerMap.Clear(); EnemyMap.Clear();
    }
}

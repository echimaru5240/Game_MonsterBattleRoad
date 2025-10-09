using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ================================
// バトル状態構造体
// ================================
public enum BattleState
{
    NONE,
    TURN_START,
    PLAYER_ACTION_SELECT,
    EXECUTING,
    FINISHER,
    CUTSCENE,
    RESULT
}

// ================================
// 行動データ構造体
// ================================
public struct ActionData
{
    public MonsterCard user;
    public Skill skill;
    public bool isPlayer;
    public int priority;

    public ActionData(MonsterCard u, Skill s, bool p, int prio)
    {
        user = u;
        skill = s;
        isPlayer = p;
        priority = prio;
    }
}

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    private void Awake() => Instance = this;

    [Header("References")]
    public BattleUIManager ui;
    public BattleSpawner spawner;
    [SerializeField] private CameraManager cameraManager;

    private MonsterCard[] playerCards;
    private MonsterCard[] enemyCards;

    public int PlayerCurrentHP { get; set; }
    public int EnemyCurrentHP { get; set; }
    public int PlayerMaxHP { get; private set; }
    public int EnemyMaxHP { get; private set; }

    // 勇気ゲージ
    private int courageGauge;
    private const int MaxCourage = 100;
    private bool finisherReady = false;

    // 状態管理
    private BattleState state = BattleState.NONE;

    // 選択管理
    private int[] selectedByUser;
    private List<(MonsterCard user, Skill skill)> playerActions = new();

    private int currentTurn = 1;
    private System.Action<bool, int> onBattleEnd;

    private void Start() => Debug.Log("BattleManager Initialized.");

    // ================================
    // セットアップ
    // ================================
    public void SetupBattle(MonsterCard[] players, BattleStageData stage, System.Action<bool, int> onEnd, int initialCourage)
    {
        playerCards = players;
        enemyCards = stage.enemyTeam;
        onBattleEnd = onEnd;

        // HP初期化
        PlayerMaxHP = SumHP(players);
        PlayerCurrentHP = PlayerMaxHP;
        EnemyMaxHP = SumHP(stage.enemyTeam);
        EnemyCurrentHP = EnemyMaxHP;

        // ゲージ初期化
        courageGauge = initialCourage;
        finisherReady = (courageGauge >= MaxCourage);

        // UI初期化
        ui.Init(players, PlayerCurrentHP, PlayerMaxHP, EnemyCurrentHP, EnemyMaxHP, MaxCourage);
        ui.SetButtonsActive(false);
        ui.OnSkillSelected = OnSkillSelected;
        ui.ShowMainText("バトル開始！");

        // 出現
        spawner.SetSpawnAreaPositions(stage.isBossStage);
        spawner.Spawn(players, stage.enemyTeam);

        selectedByUser = new int[players.Length];
        for (int i = 0; i < selectedByUser.Length; i++) selectedByUser[i] = -1;
        playerActions.Clear();
        ResetSelections();

        // ? ステージ情報からカメラ設定
        cameraManager.StartOverviewRotation();

        ChangeState(BattleState.TURN_START);
    }

    private int SumHP(MonsterCard[] cards)
    {
        int sum = 0;
        foreach (var c in cards) sum += c.hp;
        return sum;
    }

    // ================================
    // 状態管理
    // ================================
    private void ChangeState(BattleState newState)
    {
        state = newState;
        Debug.Log($"[STATE] → {state}");

        switch (state)
        {
            case BattleState.TURN_START:
                if (finisherReady)
                    ChangeState(BattleState.FINISHER);
                else
                {
                    ChangeState(BattleState.PLAYER_ACTION_SELECT);
                }
                break;

            case BattleState.PLAYER_ACTION_SELECT:
                ui.SetButtonsActive(true);
                ui.ResetButtons();
                ui.ShowTurnText(currentTurn); // ← スキル選択中にターンを表示
                // ボタン入力を待つ → OnSkillSelected が呼ばれたら EXECUTING へ
                break;

            case BattleState.EXECUTING:
                StartCoroutine(ExecuteTurn());
                break;

            case BattleState.FINISHER:
                StartCoroutine(ExecuteFinisher());
                break;

            case BattleState.CUTSCENE:
                // 将来：特殊演出を再生
                break;

            case BattleState.RESULT:
                EndBattle(PlayerCurrentHP > 0);
                break;
        }
    }

    // ================================
    // スキル選択
    // ================================
    private void OnSkillSelected(int userIndex, int skillIndex)
    {
        if (selectedByUser[userIndex] != -1) return;

        selectedByUser[userIndex] = skillIndex;
        var user = playerCards[userIndex];
        var skill = user.skills[skillIndex];

        playerActions.Add((user, skill));
        Debug.Log($"{user.cardName} が {skill.skillName} を選択");

        if (AllUsersSelected())
        {
            ui.SetButtonsActive(false);
            ui.HideTurnText(); // ← ターン切替時は一旦非表示
            ChangeState(BattleState.EXECUTING);
        }
    }

    private bool AllUsersSelected()
    {
        for (int i = 0; i < selectedByUser.Length; i++)
            if (selectedByUser[i] == -1) return false;
        return true;
    }

    // ================================
    // フィニッシャー
    // ================================
    private System.Collections.IEnumerator ExecuteFinisher()
    {
        ui.ShowMainText("とどめの一撃発動！！");
        yield return new WaitForSeconds(1f);

        Card finisherCard = playerCards[0];
        int dmg = BattleCalculator.CalculateFinisherDamage(finisherCard);

        foreach (var enemy in spawner.EnemyObjects)
            ShowDamagePopup(dmg / spawner.EnemyObjects.Count, enemy, false);

        EnemyCurrentHP = Mathf.Max(0, EnemyCurrentHP - dmg);

        courageGauge = 0;
        finisherReady = false;
        ui.UpdateCourage(courageGauge, MaxCourage);

        yield return new WaitForSeconds(0.5f);

        if (CheckBattleEnd()) yield break;

        // プレイヤー行動は飛ばして敵だけ行動
        playerActions.Clear();
        ResetSelections();

        ChangeState(BattleState.EXECUTING);
    }

    // ================================
    // 通常ターン（敵味方混合行動順）
    // ================================
    private IEnumerator ExecuteTurn()
    {
        List<ActionData> turnActions = new();

        // プレイヤー行動（フィニッシャー後は空）
        foreach (var action in playerActions)
        {
            int priority = action.user.speed + Random.Range(0, action.user.speed / 2);
            turnActions.Add(new ActionData(action.user, action.skill, true, priority));
        }

        // 敵行動
        foreach (var enemy in enemyCards)
        {
            var skill = enemy.skills[Random.Range(0, enemy.skills.Length)];
            int priority = enemy.speed + Random.Range(0, enemy.speed / 2);
            turnActions.Add(new ActionData(enemy, skill, false, priority));
        }

        // 行動順ソート
        turnActions.Sort((first, second) => second.priority.CompareTo(first.priority));

        // 行動を順番に実行（完了を待つ）
        foreach (var act in turnActions)
        {
            yield return StartCoroutine(ExecuteSkill(act.user, act.skill, act.isPlayer));
            yield return new WaitForSeconds(0.4f); // 行動間の間を少し取る

            if (CheckBattleEnd()) yield break;
        }

        playerActions.Clear();
        currentTurn++;
        ResetSelections();
        ChangeState(BattleState.TURN_START);
    }

    // ================================
    // スキル実行
    // ================================
    private IEnumerator ExecuteSkill(MonsterCard user, Skill skill, bool isPlayerSide)
    {
        // 攻撃モーション
        MonsterController attackerCtrl = isPlayerSide
            ? spawner.PlayerMap[user]
            : spawner.EnemyMap[user];

        // MonsterController targetCtrl = isPlayerSide
        //     ? spawner.EnemyControllers[Random.Range(0, spawner.EnemyControllers.Count)]
        //     : spawner.PlayerControllers[Random.Range(0, spawner.PlayerControllers.Count)];

        ui.ShowAttackText(isPlayerSide, user.cardName, skill.skillName);

        List<MonsterController> targets = new();

        switch (skill.type)
        {
            case SkillType.Attack:
                if (skill.targetType == SkillTargetType.All)
                    targets.AddRange(isPlayerSide ? spawner.EnemyControllers : spawner.PlayerControllers);
                else
                {
                    var target = isPlayerSide
                        ? spawner.EnemyControllers[Random.Range(0, spawner.EnemyControllers.Count)]
                        : spawner.PlayerControllers[Random.Range(0, spawner.PlayerControllers.Count)];
                    targets.Add(target);
                }

                // 攻撃コルーチン実行
                yield return StartCoroutine(attackerCtrl.PerformAttack(targets, skill));
                // yield return new WaitForSeconds(0.4f);
                if (isPlayerSide) AddCourage(20);
                break;

            case SkillType.Heal:
                // int heal = BattleCalculator.CalculateHeal(user, skill);
                // if (isPlayerSide)
                // {
                //     playerCurrentHP = Mathf.Min(playerMaxHP, playerCurrentHP + heal);
                //     ShowDamagePopup(heal, spawner.PlayerObjects[0], true);
                //     AddCourage(10);
                // }
                // else
                // {
                //     EnemyCurrentHP = Mathf.Min(enemyMaxHP, EnemyCurrentHP + heal);
                //     ShowDamagePopup(heal, spawner.EnemyObjects[0], true);
                // }
                break;

            case SkillType.Buff:
                float buffValue = BattleCalculator.CalculateBuffValue(skill);
                Debug.Log($"{user.cardName} がバフ効果 {buffValue} を得た！");
                break;
        }

        ui.HideAttackText(isPlayerSide);
        UpdateHPBars();
    }

    // ================================
    // 勇気ゲージ
    // ================================
    public void AddCourage(int amount)
    {
        courageGauge = Mathf.Min(courageGauge + amount, MaxCourage);
        if (courageGauge >= MaxCourage) finisherReady = true;
        ui.UpdateCourage(courageGauge, MaxCourage);
    }

    // ================================
    // 勝敗判定
    // ================================
    private bool CheckBattleEnd()
    {
        if (PlayerCurrentHP <= 0)
        {
            ui.ShowMainText("敗北…");
            ui.DisableButtons();
            ChangeState(BattleState.RESULT);
            return true;
        }
        else if (EnemyCurrentHP <= 0)
        {
            ui.ShowMainText("勝利！");
            ui.DisableButtons();
            ChangeState(BattleState.RESULT);
            return true;
        }
        return false;
    }

    private void EndBattle(bool playerWon)
    {
        onBattleEnd?.Invoke(playerWon, courageGauge);
    }

    private void ResetSelections()
    {
        for (int i = 0; i < selectedByUser.Length; i++) selectedByUser[i] = -1;
    }

    public void UpdateHPBars()
    {
        ui.UpdateHP(PlayerCurrentHP, PlayerMaxHP, EnemyCurrentHP, EnemyMaxHP);
    }

    private void ShowDamagePopup(int value, GameObject target, bool isHeal = false)
    {
        ui.ShowDamagePopup(value, target, isHeal);
    }
}

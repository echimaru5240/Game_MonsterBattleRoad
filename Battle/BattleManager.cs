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
    public BattleUIManager battleUIManager;
    [SerializeField] private GameObject battleUIPanel;
    [SerializeField] private BattleResultUIManager resultUIManager;
    [SerializeField] private GameObject resultUIPanel;
    public BattleSpawner spawner;

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
    public IEnumerator SetupBattle(MonsterCard[] players, BattleStageData stage, System.Action<bool, int> onEnd, int initialCourage)
    {
        battleUIPanel.SetActive(true);
        resultUIPanel.SetActive(false);
        playerCards = players;
        enemyCards = stage.enemyTeam;
        onBattleEnd = onEnd;

        // 戦闘BGM再生
        AudioManager.Instance.PlayBGM(stage.bgm);

        // HP初期化
        PlayerMaxHP = SumHP(players);
        PlayerCurrentHP = PlayerMaxHP;
        EnemyMaxHP = SumHP(stage.enemyTeam);
        EnemyCurrentHP = EnemyMaxHP;

        // ターン初期化
        currentTurn = 1;

        // ゲージ初期化
        courageGauge = initialCourage;
        finisherReady = (courageGauge >= MaxCourage);

        // 出現
        spawner.SetSpawnAreaPositions(stage.isBossStage);
        spawner.Spawn(players, stage.enemyTeam);


        // UI初期化
        battleUIManager.Init(players, PlayerCurrentHP, PlayerMaxHP, EnemyCurrentHP, EnemyMaxHP, MaxCourage);
        battleUIManager.OnSkillSelected = OnSkillSelected;


        selectedByUser = new int[players.Length];
        for (int i = 0; i < selectedByUser.Length; i++) selectedByUser[i] = -1;
        playerActions.Clear();
        ResetSelections();

        // ? ステージ情報からカメラ設定
        CameraManager.Instance.SwitchToOverviewCamera();
        battleUIManager.ShowMainText("バトル開始！");
        yield return new WaitForSeconds(2.0f);

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
                battleUIManager.SetButtonsActive(true);
                battleUIManager.ResetButtons();
                battleUIManager.ShowTurnText(currentTurn); // ← スキル選択中にターンを表示
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
                StartCoroutine(EndBattle(PlayerCurrentHP > 0));
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

        if (IsAllUsersSelected())
        {
            StartCoroutine(AllUsersSelected());
        }
    }

    private IEnumerator AllUsersSelected()
    {
        // yield return new WaitForSeconds(0.5f); // 行動間の間を少し取る
        // for (int i = 0; i < selectedByUser.Length; i++)
        //     battleUIManager.SetSkillButtonFrameActive(i, selectedByUser[i], true);
        yield return new WaitForSeconds(1.0f); // 行動間の間を少し取る
        // for (int i = 0; i < selectedByUser.Length; i++)
        //     battleUIManager.SetSkillButtonFrameActive(i, selectedByUser[i], false);
        battleUIManager.SetButtonsActive(false);
        ChangeState(BattleState.EXECUTING);
    }

    private bool IsAllUsersSelected()
    {
        for (int i = 0; i < selectedByUser.Length; i++)
            if (selectedByUser[i] == -1) return false;
        return true;
    }

    // ================================
    // フィニッシャー
    // ================================
    private IEnumerator ExecuteFinisher()
    {
        battleUIManager.ShowMainText("とどめの一撃発動！！");
        yield return new WaitForSeconds(1f);

        Card finisherCard = playerCards[0];
        int dmg = BattleCalculator.CalculateFinisherDamage(finisherCard);

        foreach (var enemy in spawner.EnemyObjects)
            ShowDamagePopup(dmg / spawner.EnemyObjects.Count, enemy, false);

        EnemyCurrentHP = Mathf.Max(0, EnemyCurrentHP - dmg);

        UpdateHPBars();

        courageGauge = 0;
        finisherReady = false;
        battleUIManager.UpdateCourage(courageGauge, MaxCourage);

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
    private IEnumerator ExecuteSkill(MonsterCard user, Skill skill, bool isPlayer)
    {
        // 攻撃モーション
        MonsterController attackerCtrl = isPlayer
            ? spawner.PlayerMap[user]
            : spawner.EnemyMap[user];

        AudioManager.Instance.PlayActionSE();
        battleUIManager.ShowActionBack(isPlayer);
        battleUIManager.ShowAttackText(isPlayer, user.cardName, skill.skillName);
        CameraManager.Instance.SwitchToFrontCamera(attackerCtrl.transform, isPlayer);
        yield return new WaitForSeconds(1.5f);

        List<MonsterController> targets = new();

        switch (skill.type)
        {
            case SkillType.Attack:
                if (skill.targetType == SkillTargetType.All)
                    targets.AddRange(isPlayer ? spawner.EnemyControllers : spawner.PlayerControllers);
                else
                {
                    var target = isPlayer
                        ? spawner.EnemyControllers[Random.Range(0, spawner.EnemyControllers.Count)]
                        : spawner.PlayerControllers[Random.Range(0, spawner.PlayerControllers.Count)];
                    targets.Add(target);
                }

                // 攻撃コルーチン実行
                yield return StartCoroutine(attackerCtrl.PerformAttack(targets, skill));
                if (isPlayer) AddCourage(20);
                break;

            case SkillType.Heal:
                // int heal = BattleCalculator.CalculateHeal(user, skill);
                // if (isPlayer)
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

        battleUIManager.HideAttackText(isPlayer);
        UpdateHPBars();

        // 戻ったら全体カメラへ
        CameraManager.Instance.SwitchToOverviewCamera();
        attackerCtrl.ReturnToInitialPosition();
        foreach (var target in targets)
            target.ReturnToInitialPosition();

    }

    // ================================
    // 勇気ゲージ
    // ================================
    public void AddCourage(int amount)
    {
        courageGauge = Mathf.Min(courageGauge + amount, MaxCourage);
        if (courageGauge >= MaxCourage) finisherReady = true;
        battleUIManager.UpdateCourage(courageGauge, MaxCourage);
    }

    // ================================
    // 勝敗判定
    // ================================
    private bool CheckBattleEnd()
    {
        if (PlayerCurrentHP <= 0)
        {
            battleUIManager.DisableButtons();
            ChangeState(BattleState.RESULT);
            return true;
        }
        else if (EnemyCurrentHP <= 0)
        {
            battleUIManager.DisableButtons();
            ChangeState(BattleState.RESULT);
            return true;
        }
        return false;
    }

    private IEnumerator EndBattle(bool playerWon)
    {
        yield return new WaitForSeconds(1.5f); // 行動間の間を少し取る
        battleUIPanel.SetActive(false);
        resultUIPanel.SetActive(true);

        // リザルトBGM再生
        AudioManager.Instance.PlayBGM(playerWon ? AudioManager.Instance.victoryBGM : AudioManager.Instance.defeatBGM);

        spawner.SetResultObject();
        CameraManager.Instance.SwitchToResultCamera();
        List<MonsterController> playerControllers = new();
        playerControllers.AddRange(spawner.PlayerControllers);
        foreach (var playerController in playerControllers )
            playerController.PlayResultWin(true);

        resultUIManager.ShowResult(playerWon, () =>
        {
            foreach (var playerController in playerControllers )
                playerController.PlayResultWin(false);
            onBattleEnd?.Invoke(playerWon, courageGauge);
        });
    }

    private void ResetSelections()
    {
        for (int i = 0; i < selectedByUser.Length; i++) selectedByUser[i] = -1;
    }

    public void UpdateHPBars()
    {
        battleUIManager.UpdateHP(PlayerCurrentHP, EnemyCurrentHP);
    }

    private void ShowDamagePopup(int value, GameObject target, bool isHeal = false)
    {
        battleUIManager.ShowDamagePopup(value, target, isHeal);
    }
}

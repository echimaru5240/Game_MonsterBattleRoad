using UnityEngine;
using UnityEngine.SceneManagement;
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
    public MonsterController monster;
    public SkillData skill;
    public bool isPlayer;
    public int priority;

    public ActionData(MonsterController m, SkillData s, bool p, int prio)
    {
        monster = m;
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
    [SerializeField] private BattleSpawner spawner;

    [SerializeField] private Card finisherCard;

    private List<MonsterController> playerControllers = new();
    private List<MonsterController> enemyControllers = new();

    public int PlayerCurrentHP { get; set; }
    public int EnemyCurrentHP { get; set; }

    // 勇気ゲージ
    private int courageGauge;
    private const int MaxCourage = 100;
    private bool finisherReady = false;

    // 状態管理
    private BattleState state = BattleState.NONE;

    // 選択管理
    private int[] selectedByUser;
    private List<(MonsterController user, SkillData skill)> playerActions = new();

    private int currentTurn = 1;
    private System.Action<bool, int> onBattleEnd;

    private void Start() => Debug.Log("BattleManager Initialized.");

    // ================================
    // セットアップ
    // ================================
    public IEnumerator SetupBattle(MonsterCard[] playerCards, BattleStageData stage, System.Action<bool, int> onEnd, int initialCourage)
    {
        battleUIPanel.SetActive(true);
        resultUIPanel.SetActive(false);
        onBattleEnd = onEnd;

        // 戦闘BGM再生
        AudioManager.Instance.PlayBGM(stage.bgm);

        // モンスター配置設定
        spawner.SetSpawnAreaPositions(stage.isBossStage);
        spawner.Spawn(playerCards, stage.enemyTeam);

        // Controllerベースに移行
        playerControllers = spawner.PlayerControllers;
        enemyControllers  = spawner.EnemyControllers;

        // HP初期化
        PlayerCurrentHP = SumHP(playerControllers);
        EnemyCurrentHP = SumHP(enemyControllers);

        // UI初期化
        battleUIManager.Init(playerControllers, PlayerCurrentHP, EnemyCurrentHP, MaxCourage);
        battleUIManager.OnSkillSelected = OnSkillSelected;
        battleUIManager.OnBattleEnd = OnBattleEnd;


        selectedByUser = new int[playerCards.Length];
        for (int i = 0; i < selectedByUser.Length; i++) selectedByUser[i] = -1;
        playerActions.Clear();
        ResetSelections();

        // ターン初期化
        currentTurn = 1;

        // ゲージ初期化
        courageGauge = initialCourage;
        finisherReady = (courageGauge >= MaxCourage);

        // ? ステージ情報からカメラ設定
        CameraManager.Instance.SwitchToOrbitCamera();
        battleUIManager.ShowMainText("バトル開始！");
        yield return new WaitForSeconds(2.0f);

        ChangeState(BattleState.TURN_START);
    }

    private int SumHP(List<MonsterController> monsters)
    {
        int sum = 0;
        foreach (var monster in monsters) sum += monster.hp;
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
    private void OnSkillSelected(int monsterIndex, int skillIndex)
    {
        if (selectedByUser[monsterIndex] != -1) return;

        selectedByUser[monsterIndex] = skillIndex;
        var monster = playerControllers[monsterIndex];
        var skill = SkillDatabase.Get(monster.skills[skillIndex]);

        playerActions.Add((monster, skill));
        Debug.Log($"{monster.name} が {skill.skillName} を選択");

        if (IsAllUsersSelected())
        {
            StartCoroutine(AllUsersSelected());
        }
    }

    private IEnumerator AllUsersSelected()
    {
        yield return new WaitForSeconds(1.0f); // 行動間の間を少し取る
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
        CameraManager.Instance.SwitchToFixedBackCamera(enemyControllers[0].transform, false);
        yield return new WaitForSeconds(2f);

        // 攻撃エフェクトを呼び出す
        foreach (var enemy in enemyControllers)
        {
            Vector3 effectPos = enemy.transform.position + Vector3.up * 1f;
            EffectManager.Instance.PlayEffect(EffectID.EFFECT_ID_EXPLOSION_SMALL, effectPos);
            enemy.PlayLastHit();
        }
        int dmg = BattleCalculator.CalculateFinisherDamage(finisherCard);

        foreach (var enemy in enemyControllers)
            ShowDamagePopup(dmg / enemyControllers.Count, enemy, false);

        EnemyCurrentHP = Mathf.Max(0, EnemyCurrentHP - dmg);

        UpdateHPBars();

        courageGauge = 0;
        finisherReady = false;
        battleUIManager.UpdateCourage(courageGauge, MaxCourage);

        yield return new WaitForSeconds(1f);

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
        foreach (var enemy in enemyControllers)
        {
            var skill = SkillDatabase.Get(enemy.skills[Random.Range(0, enemy.skills.Count)]);
            int priority = enemy.speed + Random.Range(0, enemy.speed / 2);
            turnActions.Add(new ActionData(enemy, skill, false, priority));
        }

        // 行動順ソート
        turnActions.Sort((first, second) => second.priority.CompareTo(first.priority));

        // 行動を順番に実行（完了を待つ）
        foreach (var action in turnActions)
        {
            yield return StartCoroutine(ExecuteAction(action.monster, action.skill, action.isPlayer));

            if (CheckBattleEnd()) yield break;
        }
        // 戻ったら全体カメラへ
        CameraManager.Instance.SwitchToOrbitCamera();

        playerActions.Clear();
        currentTurn++;
        ResetSelections();
        ChangeState(BattleState.TURN_START);
    }

    // ================================
    // スキル実行
    // ================================
    private IEnumerator ExecuteAction(MonsterController attacker, SkillData skill, bool isPlayer)
    {
        AudioManager.Instance.PlayExecuteSkillSE();
        battleUIManager.ShowActionBack(isPlayer);
        battleUIManager.ShowAttackText(isPlayer, attacker.name, skill.skillName);
        CameraManager.Instance.SwitchToActionCameraFront(attacker.transform, isPlayer);
        yield return new WaitForSeconds(1.5f);

        List<MonsterController> targets = new();

        switch (skill.action)
        {
            case SkillAction.ATTACK:
                if (skill.targetType == TargetType.ENEMY_ALL)
                    targets.AddRange(isPlayer ? spawner.EnemyControllers : spawner.PlayerControllers);
                else
                {
                    var target = isPlayer
                        ? spawner.EnemyControllers[Random.Range(0, spawner.EnemyControllers.Count)]
                        : spawner.PlayerControllers[Random.Range(0, spawner.PlayerControllers.Count)];
                    targets.Add(target);
                }
                SetMonsterPositionForAttack(attacker, targets, skill.targetType);
                // 攻撃コルーチン実行
                yield return StartCoroutine(attacker.PerformAction(targets, skill));
                if (isPlayer) AddCourage(20);
                break;

            case SkillAction.HEAL:
                if (skill.targetType == TargetType.ENEMY_ALL)
                    targets.AddRange(isPlayer ? spawner.PlayerControllers : spawner.EnemyControllers);
                else
                {
                    var target = isPlayer
                        ? spawner.PlayerControllers[Random.Range(0, spawner.PlayerControllers.Count)]
                        : spawner.EnemyControllers[Random.Range(0, spawner.EnemyControllers.Count)];
                    targets.Add(target);
                }
                // 攻撃コルーチン実行
                yield return StartCoroutine(attacker.PerformAction(targets, skill));
                if (isPlayer) AddCourage(20);
                break;

            case SkillAction.SPECIAL:
                float buffValue = BattleCalculator.CalculateBuffValue(skill);
                Debug.Log($"{attacker.name} がバフ効果 {buffValue} を得た！");
                break;
        }

        battleUIManager.HideAttackText(isPlayer);
        UpdateHPBars();

        // モンスターの位置を戻す
        SetMonsterPositionForWaitinig();
    }

    // ================================
    // 行動時のモンスターの配置
    // ================================
    private void SetMonsterPositionForAttack(MonsterController attacker, List<MonsterController> targets, TargetType targetType)
    {
        foreach (var p in playerControllers)
            p.gameObject.SetActive(false);

        foreach (var e in enemyControllers)
            e.gameObject.SetActive(false);

        float attackerPosZ = attacker.transform.position.z;
        attacker.transform.position = new Vector3(0, 0, attackerPosZ);
        attacker.gameObject.SetActive(true);

        if (targetType == TargetType.ENEMY_ALL)
        {
            foreach (var t in targets)
                t.gameObject.SetActive(true);
        }
        else
        {
            foreach (var t in targets)
            {
                float targetPosZ = t.transform.position.z;
                t.transform.position = new Vector3(0, 0, targetPosZ);
                t.gameObject.SetActive(true);
            }
        }
    }

    private void SetMonsterPositionForWaitinig()
    {
        foreach (var p in playerControllers)
        {
            p.ReturnToInitialPosition();
            p.gameObject.SetActive(true);
        }

        foreach (var e in enemyControllers)
        {
            e.ReturnToInitialPosition();
            e.gameObject.SetActive(true);
        }
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
        CameraManager.Instance.SwitchToOrbitCamera();
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

    public void OnBattleEnd()
    {
        onBattleEnd?.Invoke(false, 0);
    }

    private void ResetSelections()
    {
        for (int i = 0; i < selectedByUser.Length; i++) selectedByUser[i] = -1;
    }

    public void UpdateHPBars()
    {
        battleUIManager.UpdateHP(PlayerCurrentHP, EnemyCurrentHP);
    }

    private void ShowDamagePopup(int value, MonsterController target, bool isHeal = false)
    {
        battleUIManager.ShowDamagePopup(value, target, isHeal);
    }
}

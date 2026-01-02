using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

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
    public static bool IsPaused { get; private set; }

    // 選択管理
    private int[] selectedByUser;
    private List<(MonsterController user, SkillData skill)> playerActions = new();

    private int currentTurn = 1;
    private System.Action<bool, int> onBattleEnd;
    private static float battleSpeed = 1.0f;
    private int exp = 0;
    // スキップを表す特別値（未選択=-1 と区別）
    private const int SKILL_SKIP = -2;

    private void Start() => Debug.Log("BattleManager Initialized.");

    // ================================
    // セットアップ
    // ================================
    public IEnumerator SetupBattle(MonsterBattleData[] playerMonsters, BattleStageData stage, System.Action<bool, int> onEnd, int initialCourage)
    {
        battleUIPanel.SetActive(true);
        resultUIPanel.SetActive(false);
        onBattleEnd = onEnd;

        // 戦闘BGM再生
        AudioManager.Instance.PlayBGM(stage.bgm);
        Time.timeScale = battleSpeed;
        DOTween.PlayAll();
        exp = stage.stageLevel * 150;
        if(stage.isBossStage) exp *= 15;

        // モンスター配置設定
        MonsterBattleData[] enemyBattleData = new MonsterBattleData[0];;
        if (stage.enemyTeam == null)
        {
            Debug.LogWarning("enemyTeam が null です");
            enemyBattleData = new MonsterBattleData[0];
        }
        else
        {
            enemyBattleData = new MonsterBattleData[stage.enemyTeam.Length];

            for (int i = 0; i < stage.enemyTeam.Length; i++)
            {
                var enemy = stage.enemyTeam[i];
                if (enemy == null)
                {
                    enemyBattleData[i] = null; // 空スロット
                    continue;
                }

                enemyBattleData[i] = MonsterBattleData.CreateBattleFromMasterData(enemy, stage.stageLevel);
            }
        }
        spawner.SetSpawnAreaPositions(stage.isBossStage);
        spawner.Spawn(playerMonsters, enemyBattleData);

        // Controllerベースに移行
        playerControllers = spawner.PlayerControllers;
        enemyControllers  = spawner.EnemyControllers;

        // HP初期化
        PlayerCurrentHP = SumHP(playerControllers);
        EnemyCurrentHP = SumHP(enemyControllers);

        // UI初期化
        battleUIManager.Init(playerMonsters, PlayerCurrentHP, EnemyCurrentHP, MaxCourage);
        battleUIManager.OnSkillSelected = OnSkillSelected;
        battleUIManager.OnBattleEnd = OnBattleEnd;


        selectedByUser = new int[playerMonsters.Length];
        for (int i = 0; i < selectedByUser.Length; i++) selectedByUser[i] = -1;
        playerActions.Clear();
        ResetSelections();

        // ターン初期化
        currentTurn = 1;

        // ゲージ初期化
        courageGauge = initialCourage;
        finisherReady = (courageGauge >= MaxCourage);

        // ? ステージ情報からカメラ設定
        CameraManager.Instance.StartOrbit();
        battleUIManager.ShowMainText("バトル開始！");
        yield return new WaitForSeconds(2.5f);

        ChangeState(BattleState.TURN_START);
    }

    private int SumHP(List<MonsterController> monsters)
    {
        float sum = 0;
        foreach (var monster in monsters) sum += monster.battleData.hp;
        return Mathf.FloorToInt(sum);
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

                // ★ここで行動不能を自動スキップ確定
                for (int i = 0; i < playerControllers.Count; i++)
                {
                    bool canAct = !IsUnableToAct(playerControllers[i]);

                    // UIに反映（押せない＆暗く）
                    battleUIManager.SetUserCanAct(i, canAct);

                    // 行動不能なら「選択済み扱い＝スキップ」を確定して進行を止めない
                    if (!canAct) ForceSelectSkip(i);
                }

                // ここで全員埋まったなら即実行へ（全員行動不能とか）
                if (IsAllUsersSelected())
                    StartCoroutine(AllUsersSelected());
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
                // StartCoroutine(EndBattle(PlayerCurrentHP > 0));
                break;
        }
    }

    // ================================
    // スキル選択
    // ================================
    private void OnSkillSelected(int monsterIndex, int skillIndex)
    {
        if (selectedByUser[monsterIndex] != -1) return;
        var monster = playerControllers[monsterIndex];

        // ★保険：行動不能なら選べない（自動スキップで確定）
        if (IsUnableToAct(monster))
        {
            ForceSelectSkip(monsterIndex);
            return;
        }

        selectedByUser[monsterIndex] = skillIndex;
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

    // 例：MonsterController 側に状態異常がある想定
    private bool IsUnableToAct(MonsterController m)
    {
        switch (m.battleData.statusAilmentType) {
            case StatusAilmentType.PARALYSIS:
            case StatusAilmentType.SLEEP:
            case StatusAilmentType.STUN:
                return true;
            default:
                return false;
        }
    }

    private void ForceSelectSkip(int monsterIndex)
    {
        if (selectedByUser[monsterIndex] != -1) return;

        selectedByUser[monsterIndex] = SKILL_SKIP;

        var monster = playerControllers[monsterIndex];
        Debug.Log($"{monster.name} は行動不能のためスキップ");

        if (IsAllUsersSelected())
        {
            StartCoroutine(AllUsersSelected());
        }
    }

    // ================================
    // フィニッシャー
    // ================================
    private IEnumerator ExecuteFinisher()
    {
        battleUIManager.ShowMainText("とどめの一撃発動！！");
        Vector3 fixedPos = new Vector3(-2f, 1f, 13f);
        CameraManager.Instance.CutAction_FixedWorldLookOnly(fixedPos, enemyControllers[0].transform, fov: 45f);

        yield return new WaitForSeconds(2f);

        // 攻撃エフェクトを呼び出す
        foreach (var enemy in enemyControllers)
        {
            Vector3 effectPos = enemy.transform.position + Vector3.up * 1f;
            EffectManager.Instance.PlayEffectByID(EffectID.EFFECT_ID_EXPLOSION_SMALL, effectPos);
            // enemy.PlayLastHit();
        }
        int dmg = BattleCalculator.CalculateFinisherDamage(finisherCard);


        foreach (var enemy in enemyControllers) {
            var result = new BattleCalculator.ActionResult
            {
                Target = enemy,
                Value = dmg / enemyControllers.Count,
                IsDamage = true
            };
            ShowDamagePopup(result);
        }

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
            int priority = Mathf.FloorToInt(action.user.battleData.agi + Random.Range(0, action.user.battleData.agi / 2));
            turnActions.Add(new ActionData(action.user, action.skill, true, priority));
        }

        // 敵行動
        foreach (var enemy in enemyControllers)
        {
            var skill = SkillDatabase.Get(enemy.skills[Random.Range(0, enemy.skills.Count)]);
            int priority = Mathf.FloorToInt(enemy.battleData.agi + Random.Range(0, enemy.battleData.agi / 2));
            turnActions.Add(new ActionData(enemy, skill, false, priority));
        }

        // 行動順ソート
        turnActions.Sort((first, second) => second.priority.CompareTo(first.priority));

        // 行動を順番に実行（完了を待つ）
        foreach (var action in turnActions)
        {
            yield return StartCoroutine(ExecuteAction(action.monster, action.skill, action.isPlayer));
            if (CheckBattleEnd()) yield break;
            yield return new WaitForSeconds(1.0f);
            // モンスターの位置を戻す
            SetMonsterPositionForWaitinig();
            if (action.monster.battleData.statusAilmentType != StatusAilmentType.NONE)
            {
                action.monster.battleData.statusAilmentType = StatusAilmentType.NONE;
                action.monster.RecoveryFromDizzy();
            }
        }
        // 戻ったら全体カメラへ
        CameraManager.Instance.StartOrbit();

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
        var offset = new Vector3(3f, 2f, isPlayer ? 10f : -10f);
        if (BoundsUtil.TryGetVisualBounds(attacker.gameObject, out var b))
        {
            Vector3 size = b.size;           // 幅/高さ/奥行き（ワールド）
            float radius = BoundsUtil.GetRadius(b);
            Debug.Log($"size={size}, radius={radius}");
            if (radius > 2) {
                offset *= Mathf.Sqrt(radius * 0.8f);
            }
        }
        CameraManager.Instance.CutAction_Follow(attacker.transform, offset, 0f, 30f);
        battleUIManager.ShowActionBack(isPlayer);

        switch (attacker.battleData.statusAilmentType) {
            case StatusAilmentType.PARALYSIS:
                battleUIManager.ShowAttackText(isPlayer, attacker.name, "マヒして動けない！");
                yield return new WaitForSeconds(1f);
                break;

            case StatusAilmentType.SLEEP:
                battleUIManager.ShowAttackText(isPlayer, attacker.name, "眠っている！");
                yield return new WaitForSeconds(1f);
                break;

            case StatusAilmentType.STUN:
                battleUIManager.ShowAttackText(isPlayer, attacker.name, "混乱している！");
                yield return new WaitForSeconds(1f);
                break;

            case StatusAilmentType.FREEZE:
                battleUIManager.ShowAttackText(isPlayer, attacker.name, "凍って動けない！");
                yield return new WaitForSeconds(1f);
                break;

            default:
                battleUIManager.ShowActionBack(isPlayer);
                battleUIManager.ShowAttackText(isPlayer, attacker.name, skill.skillName);

                yield return new WaitForSeconds(1.5f);
                List<MonsterController> targets = new();
                MonsterController target = new();

                switch (skill.targetType) {
                    case TargetType.ENEMY_SINGLE:
                        target = isPlayer
                            ? spawner.EnemyControllers[Random.Range(0, spawner.EnemyControllers.Count)]
                            : spawner.PlayerControllers[Random.Range(0, spawner.PlayerControllers.Count)];
                        targets.Add(target);
                        break;
                    case TargetType.ENEMY_ALL:
                        targets.AddRange(isPlayer ? spawner.EnemyControllers : spawner.PlayerControllers);
                        break;
                    case TargetType.PLAYER_SINGLE:
                        target = isPlayer
                            ? spawner.EnemyControllers[Random.Range(0, spawner.PlayerControllers.Count)]
                            : spawner.PlayerControllers[Random.Range(0, spawner.EnemyControllers.Count)];
                        targets.Add(target);
                        break;
                    case TargetType.PLAYER_ALL:
                        targets.AddRange(isPlayer ? spawner.PlayerControllers : spawner.EnemyControllers);
                        break;

                }
                SetMonsterPositionForAttack(attacker, targets, skill.targetType);
                // 攻撃コルーチン実行
                yield return StartCoroutine(attacker.PerformAction(targets, skill));
                break;
        }
        battleUIManager.HideAttackText(isPlayer);
        UpdateHPBars();
        yield return new WaitForSeconds(0.5f);
        // if (isPlayer) AddCourage(10);
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

    private void EndBattle(bool playerWon)
    {
        // モンスターの位置を戻す
        SetMonsterPositionForWaitinig();
        CameraManager.Instance.StartOrbit();
        battleUIPanel.SetActive(false);
        resultUIPanel.SetActive(true);
        Time.timeScale = 1f;
        DOTween.PlayAll();

        // リザルトBGM再生
        AudioManager.Instance.PlayBGM(playerWon ? AudioManager.Instance.victoryBGM : AudioManager.Instance.defeatBGM);

        spawner.SetResultObject();
        CameraManager.Instance.CutToResult();
        List<MonsterController> playerControllers = new();
        playerControllers.AddRange(spawner.PlayerControllers);

        resultUIManager.ShowResult(playerWon, playerControllers, currentTurn, exp, () =>
        {
            onBattleEnd?.Invoke(playerWon, courageGauge);
        });
    }

    public void OnBattleEnd()
    {
        Time.timeScale = 1f;   // 物理・Animatorを止める
        onBattleEnd?.Invoke(false, 0);
    }

    public static void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;   // 物理・Animatorを止める
        DOTween.PauseAll();    // DOTweenも停止
    }

    public static void Resume()
    {
        IsPaused = false;
        battleSpeed = 1f;
        Time.timeScale = 1f;
        DOTween.PlayAll();
    }

    public static void Playx2()
    {
        IsPaused = false;
        battleSpeed = 2f;
        Time.timeScale = 2f;
        DOTween.PlayAll();
    }

    public static void Playx3()
    {
        IsPaused = false;
        battleSpeed = 3f;
        Time.timeScale = 3f;
        DOTween.PlayAll();
    }

    private void ResetSelections()
    {
        for (int i = 0; i < selectedByUser.Length; i++) selectedByUser[i] = -1;
    }

    public void UpdateHPBars()
    {
        battleUIManager.UpdateHP(PlayerCurrentHP, EnemyCurrentHP);
    }

    private void ShowDamagePopup(BattleCalculator.ActionResult result)
    {
        battleUIManager.ShowDamagePopup(result);
    }
}

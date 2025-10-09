using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ================================
// �o�g����ԍ\����
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
// �s���f�[�^�\����
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

    // �E�C�Q�[�W
    private int courageGauge;
    private const int MaxCourage = 100;
    private bool finisherReady = false;

    // ��ԊǗ�
    private BattleState state = BattleState.NONE;

    // �I���Ǘ�
    private int[] selectedByUser;
    private List<(MonsterCard user, Skill skill)> playerActions = new();

    private int currentTurn = 1;
    private System.Action<bool, int> onBattleEnd;

    private void Start() => Debug.Log("BattleManager Initialized.");

    // ================================
    // �Z�b�g�A�b�v
    // ================================
    public void SetupBattle(MonsterCard[] players, BattleStageData stage, System.Action<bool, int> onEnd, int initialCourage)
    {
        playerCards = players;
        enemyCards = stage.enemyTeam;
        onBattleEnd = onEnd;

        // HP������
        PlayerMaxHP = SumHP(players);
        PlayerCurrentHP = PlayerMaxHP;
        EnemyMaxHP = SumHP(stage.enemyTeam);
        EnemyCurrentHP = EnemyMaxHP;

        // �Q�[�W������
        courageGauge = initialCourage;
        finisherReady = (courageGauge >= MaxCourage);

        // UI������
        ui.Init(players, PlayerCurrentHP, PlayerMaxHP, EnemyCurrentHP, EnemyMaxHP, MaxCourage);
        ui.SetButtonsActive(false);
        ui.OnSkillSelected = OnSkillSelected;
        ui.ShowMainText("�o�g���J�n�I");

        // �o��
        spawner.SetSpawnAreaPositions(stage.isBossStage);
        spawner.Spawn(players, stage.enemyTeam);

        selectedByUser = new int[players.Length];
        for (int i = 0; i < selectedByUser.Length; i++) selectedByUser[i] = -1;
        playerActions.Clear();
        ResetSelections();

        // ? �X�e�[�W��񂩂�J�����ݒ�
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
    // ��ԊǗ�
    // ================================
    private void ChangeState(BattleState newState)
    {
        state = newState;
        Debug.Log($"[STATE] �� {state}");

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
                ui.ShowTurnText(currentTurn); // �� �X�L���I�𒆂Ƀ^�[����\��
                // �{�^�����͂�҂� �� OnSkillSelected ���Ă΂ꂽ�� EXECUTING ��
                break;

            case BattleState.EXECUTING:
                StartCoroutine(ExecuteTurn());
                break;

            case BattleState.FINISHER:
                StartCoroutine(ExecuteFinisher());
                break;

            case BattleState.CUTSCENE:
                // �����F���ꉉ�o���Đ�
                break;

            case BattleState.RESULT:
                EndBattle(PlayerCurrentHP > 0);
                break;
        }
    }

    // ================================
    // �X�L���I��
    // ================================
    private void OnSkillSelected(int userIndex, int skillIndex)
    {
        if (selectedByUser[userIndex] != -1) return;

        selectedByUser[userIndex] = skillIndex;
        var user = playerCards[userIndex];
        var skill = user.skills[skillIndex];

        playerActions.Add((user, skill));
        Debug.Log($"{user.cardName} �� {skill.skillName} ��I��");

        if (AllUsersSelected())
        {
            ui.SetButtonsActive(false);
            ui.HideTurnText(); // �� �^�[���ؑ֎��͈�U��\��
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
    // �t�B�j�b�V���[
    // ================================
    private System.Collections.IEnumerator ExecuteFinisher()
    {
        ui.ShowMainText("�Ƃǂ߂̈ꌂ�����I�I");
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

        // �v���C���[�s���͔�΂��ēG�����s��
        playerActions.Clear();
        ResetSelections();

        ChangeState(BattleState.EXECUTING);
    }

    // ================================
    // �ʏ�^�[���i�G���������s�����j
    // ================================
    private IEnumerator ExecuteTurn()
    {
        List<ActionData> turnActions = new();

        // �v���C���[�s���i�t�B�j�b�V���[��͋�j
        foreach (var action in playerActions)
        {
            int priority = action.user.speed + Random.Range(0, action.user.speed / 2);
            turnActions.Add(new ActionData(action.user, action.skill, true, priority));
        }

        // �G�s��
        foreach (var enemy in enemyCards)
        {
            var skill = enemy.skills[Random.Range(0, enemy.skills.Length)];
            int priority = enemy.speed + Random.Range(0, enemy.speed / 2);
            turnActions.Add(new ActionData(enemy, skill, false, priority));
        }

        // �s�����\�[�g
        turnActions.Sort((first, second) => second.priority.CompareTo(first.priority));

        // �s�������ԂɎ��s�i������҂j
        foreach (var act in turnActions)
        {
            yield return StartCoroutine(ExecuteSkill(act.user, act.skill, act.isPlayer));
            yield return new WaitForSeconds(0.4f); // �s���Ԃ̊Ԃ��������

            if (CheckBattleEnd()) yield break;
        }

        playerActions.Clear();
        currentTurn++;
        ResetSelections();
        ChangeState(BattleState.TURN_START);
    }

    // ================================
    // �X�L�����s
    // ================================
    private IEnumerator ExecuteSkill(MonsterCard user, Skill skill, bool isPlayerSide)
    {
        // �U�����[�V����
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

                // �U���R���[�`�����s
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
                Debug.Log($"{user.cardName} ���o�t���� {buffValue} �𓾂��I");
                break;
        }

        ui.HideAttackText(isPlayerSide);
        UpdateHPBars();
    }

    // ================================
    // �E�C�Q�[�W
    // ================================
    public void AddCourage(int amount)
    {
        courageGauge = Mathf.Min(courageGauge + amount, MaxCourage);
        if (courageGauge >= MaxCourage) finisherReady = true;
        ui.UpdateCourage(courageGauge, MaxCourage);
    }

    // ================================
    // ���s����
    // ================================
    private bool CheckBattleEnd()
    {
        if (PlayerCurrentHP <= 0)
        {
            ui.ShowMainText("�s�k�c");
            ui.DisableButtons();
            ChangeState(BattleState.RESULT);
            return true;
        }
        else if (EnemyCurrentHP <= 0)
        {
            ui.ShowMainText("�����I");
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

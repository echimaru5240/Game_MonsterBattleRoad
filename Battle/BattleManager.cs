using UnityEngine;
using UnityEngine.SceneManagement;
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
    public MonsterController monster;
    public Skill skill;
    public bool isPlayer;
    public int priority;

    public ActionData(MonsterController m, Skill s, bool p, int prio)
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

    // �E�C�Q�[�W
    private int courageGauge;
    private const int MaxCourage = 100;
    private bool finisherReady = false;

    // ��ԊǗ�
    private BattleState state = BattleState.NONE;

    // �I���Ǘ�
    private int[] selectedByUser;
    private List<(MonsterController user, Skill skill)> playerActions = new();

    private int currentTurn = 1;
    private System.Action<bool, int> onBattleEnd;

    private void Start() => Debug.Log("BattleManager Initialized.");

    // ================================
    // �Z�b�g�A�b�v
    // ================================
    public IEnumerator SetupBattle(MonsterCard[] playerCards, BattleStageData stage, System.Action<bool, int> onEnd, int initialCourage)
    {
        battleUIPanel.SetActive(true);
        resultUIPanel.SetActive(false);
        onBattleEnd = onEnd;

        // �퓬BGM�Đ�
        AudioManager.Instance.PlayBGM(stage.bgm);

        // �����X�^�[�z�u�ݒ�
        spawner.SetSpawnAreaPositions(stage.isBossStage);
        spawner.Spawn(playerCards, stage.enemyTeam);

        // Controller�x�[�X�Ɉڍs
        playerControllers = spawner.PlayerControllers;
        enemyControllers  = spawner.EnemyControllers;

        // HP������
        PlayerCurrentHP = SumHP(playerControllers);
        EnemyCurrentHP = SumHP(enemyControllers);

        // UI������
        battleUIManager.Init(playerControllers, PlayerCurrentHP, EnemyCurrentHP, MaxCourage);
        battleUIManager.OnSkillSelected = OnSkillSelected;


        selectedByUser = new int[playerCards.Length];
        for (int i = 0; i < selectedByUser.Length; i++) selectedByUser[i] = -1;
        playerActions.Clear();
        ResetSelections();

        // �^�[��������
        currentTurn = 1;

        // �Q�[�W������
        courageGauge = initialCourage;
        finisherReady = (courageGauge >= MaxCourage);

        // ? �X�e�[�W��񂩂�J�����ݒ�
        CameraManager.Instance.SwitchToOrbitCamera();//SwitchToOverviewCamera();
        battleUIManager.ShowMainText("�o�g���J�n�I");
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
                battleUIManager.SetButtonsActive(true);
                battleUIManager.ResetButtons();
                battleUIManager.ShowTurnText(currentTurn); // �� �X�L���I�𒆂Ƀ^�[����\��
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
                StartCoroutine(EndBattle(PlayerCurrentHP > 0));
                break;
        }
    }

    // ================================
    // �X�L���I��
    // ================================
    private void OnSkillSelected(int monsterIndex, int skillIndex)
    {
        if (selectedByUser[monsterIndex] != -1) return;

        selectedByUser[monsterIndex] = skillIndex;
        var monster = playerControllers[monsterIndex];
        var skill = monster.skills[skillIndex];

        playerActions.Add((monster, skill));
        Debug.Log($"{monster.name} �� {skill.skillName} ��I��");

        if (IsAllUsersSelected())
        {
            StartCoroutine(AllUsersSelected());
        }
    }

    private IEnumerator AllUsersSelected()
    {
        yield return new WaitForSeconds(1.0f); // �s���Ԃ̊Ԃ��������
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
    // �t�B�j�b�V���[
    // ================================
    private IEnumerator ExecuteFinisher()
    {
        battleUIManager.ShowMainText("�Ƃǂ߂̈ꌂ�����I�I");
        yield return new WaitForSeconds(1f);

        int dmg = BattleCalculator.CalculateFinisherDamage(finisherCard);

        foreach (var enemy in enemyControllers)
            ShowDamagePopup(dmg / enemyControllers.Count, enemy, false);

        EnemyCurrentHP = Mathf.Max(0, EnemyCurrentHP - dmg);

        UpdateHPBars();

        courageGauge = 0;
        finisherReady = false;
        battleUIManager.UpdateCourage(courageGauge, MaxCourage);

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
        foreach (var enemy in enemyControllers)
        {
            var skill = enemy.skills[Random.Range(0, enemy.skills.Count)];
            int priority = enemy.speed + Random.Range(0, enemy.speed / 2);
            turnActions.Add(new ActionData(enemy, skill, false, priority));
        }

        // �s�����\�[�g
        turnActions.Sort((first, second) => second.priority.CompareTo(first.priority));

        // �s�������ԂɎ��s�i������҂j
        foreach (var action in turnActions)
        {
            yield return StartCoroutine(ExecuteAction(action.monster, action.skill, action.isPlayer));
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
    private IEnumerator ExecuteAction(MonsterController attacker, Skill skill, bool isPlayer)
    {
        AudioManager.Instance.PlayExecuteSkillSE();
        battleUIManager.ShowActionBack(isPlayer);
        battleUIManager.ShowAttackText(isPlayer, attacker.name, skill.skillName);
        CameraManager.Instance.SwitchToActionCamera(attacker.transform, isPlayer);
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
                SetMonsterPositionForAttack(attacker, targets, skill.targetType);
                // �U���R���[�`�����s
                yield return StartCoroutine(attacker.PerformAttack(targets, skill));
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
                Debug.Log($"{attacker.name} ���o�t���� {buffValue} �𓾂��I");
                break;
        }

        battleUIManager.HideAttackText(isPlayer);
        UpdateHPBars();

        // �����X�^�[�̈ʒu��߂�
        SetMonsterPositionForWaitinig();

        // �߂�����S�̃J������
        CameraManager.Instance.SwitchToOverviewCamera();
        attacker.ReturnToInitialPosition();
        foreach (var target in targets)
            target.ReturnToInitialPosition();
    }

    // ================================
    // �s�����̃����X�^�[�̔z�u
    // ================================
    private void SetMonsterPositionForAttack(MonsterController attacker, List<MonsterController> targets, SkillTargetType targetType)
    {
        foreach (var p in playerControllers)
            p.gameObject.SetActive(false);

        foreach (var e in enemyControllers)
            e.gameObject.SetActive(false);

        float attackerPosZ = attacker.transform.position.z;
        attacker.transform.position = new Vector3(0, 0, attackerPosZ);
        attacker.gameObject.SetActive(true);

        if (targetType == SkillTargetType.All)
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
    // �E�C�Q�[�W
    // ================================
    public void AddCourage(int amount)
    {
        courageGauge = Mathf.Min(courageGauge + amount, MaxCourage);
        if (courageGauge >= MaxCourage) finisherReady = true;
        battleUIManager.UpdateCourage(courageGauge, MaxCourage);
    }

    // ================================
    // ���s����
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
        yield return new WaitForSeconds(1.5f); // �s���Ԃ̊Ԃ��������
        battleUIPanel.SetActive(false);
        resultUIPanel.SetActive(true);

        // ���U���gBGM�Đ�
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

    private void ShowDamagePopup(int value, MonsterController target, bool isHeal = false)
    {
        battleUIManager.ShowDamagePopup(value, target, isHeal);
    }
}

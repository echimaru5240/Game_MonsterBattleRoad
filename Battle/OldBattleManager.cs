/*
using UnityEngine;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    private MonsterCard[] playerCards;
    private MonsterCard[] enemyCards;

    public BattleUIManager ui;
    public BattleSpawner spawner;

    private int playerCurrentHP;
    private int enemyCurrentHP;
    private int playerMaxHP;
    private int enemyMaxHP;

    // �E�C�Q�[�W
    private int courageGauge = 0;
    private const int maxCourageGauge = 100;
    private bool finisherReady = false;

    // �I���Ǘ�
    private int[] selectedByUser;
    private List<(MonsterCard user, Skill skill)> playerActions = new List<(MonsterCard, Skill)>();

    private int currentTurn = 1;
    private System.Action<bool, int> onBattleEnd;

    public void SetupBattle(MonsterCard[] players, MonsterCard[] enemies, System.Action<bool, int> onEnd, int initialCourage)
    {
        this.playerCards = players;
        this.enemyCards = enemies;
        this.onBattleEnd = onEnd;

        // HP������
        playerMaxHP = SumHP(players);
        playerCurrentHP = playerMaxHP;
        enemyMaxHP = SumHP(enemies);
        enemyCurrentHP = enemyMaxHP;

        playerActions.Clear();

        // UI�������i�E�C�Q�[�W���܂ށj
        courageGauge = initialCourage;  // �����p���l���Z�b�g
        finisherReady = (courageGauge >= maxCourageGauge);

        ui.Init(players, playerCurrentHP, playerMaxHP, enemyCurrentHP, enemyMaxHP, maxCourageGauge);
        ui.GenerateSkillButtons(players, finisherReady); // �� �C��
        ui.OnSkillSelected = OnSkillSelected;
        ui.ShowMessage("�o�g���J�n�I");

        spawner.Spawn(players, enemies);

        selectedByUser = new int[players.Length];
        for (int i = 0; i < selectedByUser.Length; i++) selectedByUser[i] = -1;
    }

    private int SumHP(MonsterCard[] cards)
    {
        int sum = 0;
        foreach (var c in cards) sum += c.hp;
        return sum;
    }

    private void OnSkillSelected(int userIndex, int skillIndex)
    {
        if (selectedByUser[userIndex] != -1) return;

        // �E�C�Q�[�W��MAX�Ȃ� �� �ʏ�X�L���������ăt�B�j�b�V���[����
        if (finisherReady)
        {
            // ui.DisableButtons();   // �� �����ǉ�
            StartCoroutine(ExecuteFinisher());
            return;
        }

        selectedByUser[userIndex] = skillIndex;
        var user = playerCards[userIndex];
        var skill = user.skills[skillIndex];

        playerActions.Add((user, skill));
        Debug.Log($"{user.cardName} �� {skill.skillName} ��I��");

        if (AllUsersSelected())
        {
            StartCoroutine(ExecuteTurn());
        }
    }

    private bool AllUsersSelected()
    {
        for (int i = 0; i < selectedByUser.Length; i++)
            if (selectedByUser[i] == -1) return false;
        return true;
    }

    // ================================
    // �ʏ�^�[������
    // ================================
    private System.Collections.IEnumerator ExecuteTurn()
    {
        ui.ShowMessage($"--- {currentTurn} �^�[�� ---");

        foreach (var action in playerActions)
        {
            ExecuteSkill(action.user, action.skill, true);
            yield return new WaitForSeconds(0.5f);
            if (CheckBattleEnd()) yield break;
        }

        for (int i = 0; i < enemyCards.Length; i++)
        {
            var enemy = enemyCards[i];
            var skill = enemy.skills[Random.Range(0, enemy.skills.Length)];
            ExecuteSkill(enemy, skill, false);
            yield return new WaitForSeconds(0.5f);
            if (CheckBattleEnd()) yield break;
        }

        playerActions.Clear();
        currentTurn++;
        ResetSelections();
        ResetButtons();
    }

    // ================================
    // �Ƃǂ߂̈ꌂ�i�t�B�j�b�V���[�j
    // ================================
    private System.Collections.IEnumerator ExecuteFinisher()
    {
        ui.ShowMessage("�E�C�Q�[�WMAX�I �Ƃǂ߂̈ꌂ�����I�I");
        yield return new WaitForSeconds(2f);

        // ���܂͉��Łu�v���C���[��1���ڂ̃J�[�h�v���t�B�j�b�V���[�Ɏg�p
        Card finisherCard = playerCards[0];
        int dmg = BattleCalculator.CalculateFinisherDamage(finisherCard);

        // �G�S�̂ɕ��z�_���[�W
        foreach (var enemy in spawner.EnemyObjects)
            ShowDamagePopup(dmg / spawner.EnemyObjects.Count, enemy, false);

        enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - dmg);

        // �E�C�Q�[�W���Z�b�g
        courageGauge = 0;
        finisherReady = false;
        ui.UpdateCourage(courageGauge, maxCourageGauge);

        yield return new WaitForSeconds(0.5f);

        if (CheckBattleEnd()) yield break;

        // �G�^�[���ֈڍs
        for (int i = 0; i < enemyCards.Length; i++)
        {
            var enemy = enemyCards[i];
            var skill = enemy.skills[Random.Range(0, enemy.skills.Length)];
            ExecuteSkill(enemy, skill, false);
            yield return new WaitForSeconds(0.5f);
            if (CheckBattleEnd()) yield break;
        }

        currentTurn++;
        ResetSelections();
        ResetButtons();
    }

    // ================================
    // �X�L�����s
    // ================================
    private void ExecuteSkill(MonsterCard user, Skill skill, bool isPlayerSide)
    {
        ui.ShowMessage($"<b><color=yellow>{user.cardName}</color></b> �� {skill.skillName}�I");

        switch (skill.type)
        {
            case SkillType.Attack:
                if (isPlayerSide)
                {
                    // �v���C���[�U��
                    if (skill.targetType == SkillTargetType.All)
                    {
                        for (int i = 0; i < spawner.EnemyObjects.Count; i++)
                        {
                            var enemyObj  = spawner.EnemyObjects[i];
                            var enemyCard = spawner.EnemyCards[i];

                            int dmg = BattleCalculator.CalculateDamage(user, enemyCard, skill);
                            ShowDamagePopup(dmg / spawner.EnemyObjects.Count, enemyObj, false);
                            enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - dmg);
                        }
                    }
                    else
                    {
                        int targetIndex = Random.Range(0, spawner.EnemyObjects.Count);
                        var targetObj  = spawner.EnemyObjects[targetIndex];
                        var targetCard = spawner.EnemyCards[targetIndex];

                        int dmg = BattleCalculator.CalculateDamage(user, targetCard, skill);
                        ShowDamagePopup(dmg, targetObj, false);
                        enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - dmg);
                    }

                    AddCourage(20); // �U�����ɗE�C�Q�[�W�㏸
                }
                else
                {
                    // �G�U��
                    if (skill.targetType == SkillTargetType.All)
                    {
                        for (int i = 0; i < spawner.PlayerObjects.Count; i++)
                        {
                            var playerObj  = spawner.PlayerObjects[i];
                            var playerCard = spawner.PlayerCards[i];

                            int dmg = BattleCalculator.CalculateDamage(user, playerCard, skill);
                            ShowDamagePopup(dmg / spawner.PlayerObjects.Count, playerObj, false);
                            playerCurrentHP = Mathf.Max(0, playerCurrentHP - dmg);
                        }
                    }
                    else
                    {
                        int targetIndex = Random.Range(0, spawner.PlayerObjects.Count);
                        var targetObj  = spawner.PlayerObjects[targetIndex];
                        var targetCard = spawner.PlayerCards[targetIndex];

                        int dmg = BattleCalculator.CalculateDamage(user, targetCard, skill);
                        ShowDamagePopup(dmg, targetObj, false);
                        playerCurrentHP = Mathf.Max(0, playerCurrentHP - dmg);
                    }
                }
                break;

            case SkillType.Heal:
                int heal = BattleCalculator.CalculateHeal(user, skill);
                if (isPlayerSide)
                {
                    playerCurrentHP = Mathf.Min(playerMaxHP, playerCurrentHP + heal);
                    ShowDamagePopup(heal, spawner.PlayerObjects[0], true);
                    AddCourage(10); // �񕜂ł��E�C�Q�[�W�㏸
                }
                else
                {
                    enemyCurrentHP = Mathf.Min(enemyMaxHP, enemyCurrentHP + heal);
                    ShowDamagePopup(heal, spawner.EnemyObjects[0], true);
                }
                break;

            case SkillType.Buff:
                float buffValue = BattleCalculator.CalculateBuffValue(skill);
                Debug.Log($"{user.cardName} ���o�t���� {buffValue} �𓾂��I");
                break;
        }

        UpdateHPBars();
    }


    // ================================
    // �E�C�Q�[�W
    // ================================
    public void AddCourage(int amount)
    {
        courageGauge = Mathf.Min(courageGauge + amount, maxCourageGauge);
        if (courageGauge >= maxCourageGauge)
        {
            finisherReady = true;
        }
        ui.UpdateCourage(courageGauge, maxCourageGauge);
    }

    // ================================
    // ���s����
    // ================================
    private bool CheckBattleEnd()
    {
        if (playerCurrentHP <= 0)
        {
            ui.ShowMessage("�s�k�c");
            ui.DisableButtons();
            EndBattle(false);
            return true;
        }
        else if (enemyCurrentHP <= 0)
        {
            ui.ShowMessage("�����I");
            ui.DisableButtons();
            EndBattle(true);
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


    private void ResetButtons()
    {
        ui.GenerateSkillButtons(playerCards, finisherReady); // �� �C��
    }

    private void UpdateHPBars()
    {
        ui.UpdateHP(playerCurrentHP, playerMaxHP, enemyCurrentHP, enemyMaxHP);
    }

    private void ShowDamagePopup(int value, GameObject target, bool isHeal = false)
    {
        ui.ShowDamagePopup(value, target, isHeal);
    }
}
*/
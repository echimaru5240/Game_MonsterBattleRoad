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

    // 勇気ゲージ
    private int courageGauge = 0;
    private const int maxCourageGauge = 100;
    private bool finisherReady = false;

    // 選択管理
    private int[] selectedByUser;
    private List<(MonsterCard user, Skill skill)> playerActions = new List<(MonsterCard, Skill)>();

    private int currentTurn = 1;
    private System.Action<bool, int> onBattleEnd;

    public void SetupBattle(MonsterCard[] players, MonsterCard[] enemies, System.Action<bool, int> onEnd, int initialCourage)
    {
        this.playerCards = players;
        this.enemyCards = enemies;
        this.onBattleEnd = onEnd;

        // HP初期化
        playerMaxHP = SumHP(players);
        playerCurrentHP = playerMaxHP;
        enemyMaxHP = SumHP(enemies);
        enemyCurrentHP = enemyMaxHP;

        playerActions.Clear();

        // UI初期化（勇気ゲージも含む）
        courageGauge = initialCourage;  // 引き継ぎ値をセット
        finisherReady = (courageGauge >= maxCourageGauge);

        ui.Init(players, playerCurrentHP, playerMaxHP, enemyCurrentHP, enemyMaxHP, maxCourageGauge);
        ui.GenerateSkillButtons(players, finisherReady); // ★ 修正
        ui.OnSkillSelected = OnSkillSelected;
        ui.ShowMessage("バトル開始！");

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

        // 勇気ゲージがMAXなら → 通常スキル無視してフィニッシャー強制
        if (finisherReady)
        {
            // ui.DisableButtons();   // ← ここ追加
            StartCoroutine(ExecuteFinisher());
            return;
        }

        selectedByUser[userIndex] = skillIndex;
        var user = playerCards[userIndex];
        var skill = user.skills[skillIndex];

        playerActions.Add((user, skill));
        Debug.Log($"{user.cardName} が {skill.skillName} を選択");

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
    // 通常ターン処理
    // ================================
    private System.Collections.IEnumerator ExecuteTurn()
    {
        ui.ShowMessage($"--- {currentTurn} ターン ---");

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
    // とどめの一撃（フィニッシャー）
    // ================================
    private System.Collections.IEnumerator ExecuteFinisher()
    {
        ui.ShowMessage("勇気ゲージMAX！ とどめの一撃発動！！");
        yield return new WaitForSeconds(2f);

        // いまは仮で「プレイヤーの1枚目のカード」をフィニッシャーに使用
        Card finisherCard = playerCards[0];
        int dmg = BattleCalculator.CalculateFinisherDamage(finisherCard);

        // 敵全体に分配ダメージ
        foreach (var enemy in spawner.EnemyObjects)
            ShowDamagePopup(dmg / spawner.EnemyObjects.Count, enemy, false);

        enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - dmg);

        // 勇気ゲージリセット
        courageGauge = 0;
        finisherReady = false;
        ui.UpdateCourage(courageGauge, maxCourageGauge);

        yield return new WaitForSeconds(0.5f);

        if (CheckBattleEnd()) yield break;

        // 敵ターンへ移行
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
    // スキル実行
    // ================================
    private void ExecuteSkill(MonsterCard user, Skill skill, bool isPlayerSide)
    {
        ui.ShowMessage($"<b><color=yellow>{user.cardName}</color></b> の {skill.skillName}！");

        switch (skill.type)
        {
            case SkillType.Attack:
                if (isPlayerSide)
                {
                    // プレイヤー攻撃
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

                    AddCourage(20); // 攻撃時に勇気ゲージ上昇
                }
                else
                {
                    // 敵攻撃
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
                    AddCourage(10); // 回復でも勇気ゲージ上昇
                }
                else
                {
                    enemyCurrentHP = Mathf.Min(enemyMaxHP, enemyCurrentHP + heal);
                    ShowDamagePopup(heal, spawner.EnemyObjects[0], true);
                }
                break;

            case SkillType.Buff:
                float buffValue = BattleCalculator.CalculateBuffValue(skill);
                Debug.Log($"{user.cardName} がバフ効果 {buffValue} を得た！");
                break;
        }

        UpdateHPBars();
    }


    // ================================
    // 勇気ゲージ
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
    // 勝敗判定
    // ================================
    private bool CheckBattleEnd()
    {
        if (playerCurrentHP <= 0)
        {
            ui.ShowMessage("敗北…");
            ui.DisableButtons();
            EndBattle(false);
            return true;
        }
        else if (enemyCurrentHP <= 0)
        {
            ui.ShowMessage("勝利！");
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
        ui.GenerateSkillButtons(playerCards, finisherReady); // ★ 修正
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
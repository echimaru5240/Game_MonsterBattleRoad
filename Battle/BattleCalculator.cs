using UnityEngine;
using System.Collections.Generic;

public static class BattleCalculator
{
    // 結果格納用構造体
    public struct DamageResult
    {
        public int Damage;
        public bool IsCritical;
        public bool IsMiss;
    }

    public static void OnAttackHit(MonsterController attacker, Skill skill, List<MonsterController> targets)
    {
        var mgr = BattleManager.Instance;
        bool isPlayerSide = !attacker.isEnemy;
        bool isMultiTarget = targets.Count > 1;

        int totalDamage = 0;

        // ① ダメージ計算とポップアップ表示
        foreach (var target in targets)
        {
            var targetCard = isPlayerSide
                ? mgr.spawner.EnemyCards[mgr.spawner.EnemyControllers.IndexOf(target)]
                : mgr.spawner.PlayerCards[mgr.spawner.PlayerControllers.IndexOf(target)];

            var result = CalculateAttackResult(attacker.cardData, targetCard, skill, disableCritical: isMultiTarget);

            if (!result.IsMiss)
            {
                mgr.ui.ShowDamagePopup(result.Damage, target.gameObject, false);
                totalDamage += result.Damage;

                // if (!isMultiTarget && result.IsCritical)
                    // mgr.ui.ShowMessage("クリティカルヒット！");
            }
            else
            {
                // mgr.ui.ShowMessage($"{attacker.cardData.cardName} の攻撃は外れた！");
            }
        }

        // ② HPを一括で反映
        if (isPlayerSide)
            mgr.EnemyCurrentHP = Mathf.Max(0, mgr.EnemyCurrentHP - totalDamage);
        else
            mgr.PlayerCurrentHP = Mathf.Max(0, mgr.PlayerCurrentHP - totalDamage);

        mgr.UpdateHPBars();
    }

    /// <summary>
    /// 攻撃結果をまとめて返す
    /// </summary>
    public static DamageResult CalculateAttackResult(MonsterCard attacker, MonsterCard defender, Skill skill, bool disableCritical = false)
    {
        int damage = CalculateDamage(attacker, defender, skill);
        bool isCritical = Random.value < skill.criticalChance; // スキル側に確率を持たせる想定
        if (isCritical)
        {
            damage = Mathf.RoundToInt(damage * 1.5f);
        }

        return new DamageResult
        {
            Damage = damage,
            IsCritical = isCritical,
            IsMiss = Random.value < skill.missChance // ミス判定などもここで
        };
    }

    // ================================
    // 通常攻撃ダメージ計算
    // ================================
    public static int CalculateDamage(MonsterCard attacker, MonsterCard defender, Skill skill)
    {
        int attack = attacker.attack;
        int defense = defender != null ? defender.defense : 0;

        float power = skill.power; // Skill 側で数値（100なら等倍）

        // 攻撃式： (攻撃力 × (倍率/100)) - (防御力の半分)
        float baseDamage = (attack * (power / 100f)) - (defense * 0.5f);

        // ランダム補正 ±10%
        float randomFactor = Random.Range(0.9f, 1.1f);

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * randomFactor));
        return finalDamage;
    }

    // ================================
    // 回復計算
    // ================================
    public static int CalculateHeal(MonsterCard user, Skill skill)
    {
        int heal = Mathf.RoundToInt(user.magicPower * (skill.power / 100f));
        return Mathf.Max(1, heal);
    }

    // ================================
    // バフ計算
    // ================================
    public static float CalculateBuffValue(Skill skill)
    {
        return (skill.power - 100) / 100f; // 120なら+20%
    }

    // ================================
    // とどめの一撃（固定値 or カード依存）
    // ================================
    public static int CalculateFinisherDamage(Card finisherCard)
    {
        // Card に「finisherPower」を持たせる場合
        return finisherCard.finisherDamage;
    }
}

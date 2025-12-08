using UnityEngine;
using System.Collections.Generic;

public static class BattleCalculator
{
    // 結果格納用構造体
    public struct ActionResult
    {
        public MonsterController Target;
        public bool IsDamage;
        public int Value;
        public bool IsCritical;
        public bool IsMiss;
    }

    /// <summary>
    /// （新）攻撃の事前計算
    /// モーション開始前に呼び出して、各ターゲット分の結果をリストで持っておく
    /// </summary>
    public static List<ActionResult> PrecalculateActionResults(
        MonsterController attacker,
        SkillData skill,
        List<MonsterController> targets
    )
    {
        var results = new List<ActionResult>();
        if (targets == null || targets.Count == 0) return results;

        bool isMultiTarget = targets.Count > 1;

        foreach (var target in targets)
        {
            if (target == null ) continue;

            ActionResult result = new();
            switch (skill.targetType) {
                case TargetType.ENEMY_SINGLE:
                case TargetType.ENEMY_ALL:
                    result = CalculateAttackResult(attacker, target, skill, disableCritical: isMultiTarget);
                    break;
                case TargetType.PLAYER_SINGLE:
                case TargetType.PLAYER_ALL:
                    result = CalculateHealResult(attacker, skill);
                    break;
            }
            results.Add(result);
        }

        return results;
    }


    /// <summary>
    /// （新）事前計算済みの結果を反映
    /// 当たりフレーム（アニメーションイベント）で呼び出す
    /// </summary>
    public static void ApplyActionResults( List<ActionResult> results )
    {
        if (results == null || results.Count == 0) return;

        var mgr = BattleManager.Instance;
        bool isPlayerSide = false;

        int totalDamage = 0;

        foreach (var result in results)
        {
            var target = result.Target;
            if (target == null) continue;

            isPlayerSide = target.isPlayer;

            // ダメージポップアップ表示
            mgr.battleUIManager.ShowDamagePopup(result);
            if (result.IsDamage) {
                totalDamage += result.Value;
            }
            else {
                totalDamage -= result.Value;
            }
        }

        // サイドHPへ一括反映（今の設計に合わせる）
        if (isPlayerSide)
            mgr.PlayerCurrentHP = Mathf.Max(0, mgr.PlayerCurrentHP - totalDamage);
        else
            mgr.EnemyCurrentHP = Mathf.Max(0, mgr.EnemyCurrentHP - totalDamage);

        mgr.UpdateHPBars();
    }

    /// <summary>
    /// 攻撃結果をまとめて返す
    /// </summary>
    public static ActionResult CalculateAttackResult(MonsterController attacker, MonsterController defender, SkillData skill, bool disableCritical = false)
    {
        int damage = 0;
        bool isCritical = Random.value < skill.criticalRate; // スキル側に確率を持たせる想定
        bool isMiss = Random.value > skill.accuracy; // ミス判定などもここで
        if (!isMiss) {
            damage = CalculateDamage(attacker, defender, skill);
            if (isCritical)
            {
                damage = Mathf.RoundToInt(damage * 1.5f);
            }
        }

        return new ActionResult
        {
            Target = defender,
            IsDamage = true,
            Value = damage,
            IsCritical = isCritical,
            IsMiss = isMiss
        };
    }

    /// <summary>
    /// 攻撃結果をまとめて返す
    /// </summary>
    public static ActionResult CalculateHealResult(MonsterController attacker, SkillData skill)
    {
        int value = CalculateHeal(attacker, skill);

        return new ActionResult
        {
            Target = attacker,
            IsDamage = false,
            Value = value,
            IsCritical = false,
            IsMiss = Random.value > skill.accuracy // ミス判定などもここで
        };
    }

    // ================================
    // 通常攻撃ダメージ計算
    // ================================
    public static int CalculateDamage(MonsterController attacker, MonsterController defender, SkillData skill)
    {
        int atk = 0;
        switch (skill.category) {
            case  SkillCategory.PHYSICAL:
                atk = attacker.atk;
                break;
            case  SkillCategory.MAGICAL:
                atk = attacker.mgc;
                break;
            case  SkillCategory.SPECIAL:
                atk = (attacker.atk + attacker.mgc) / 2;
                break;
            default:
                atk = attacker.atk;
                break;
        }
        int def = defender != null ? defender.def : 0;

        float power = skill.power; // Skill 側で数値（100なら等倍）

        // 攻撃式： (攻撃力 × (倍率/100)) - (防御力の半分)
        float baseDamage = (atk * (power / 50f)) - (def * 0.5f);

        // ランダム補正 ±10%
        float randomFactor = Random.Range(0.9f, 1.1f);

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * randomFactor));
        return finalDamage;
    }

    // ================================
    // 回復計算
    // ================================
    public static int CalculateHeal(MonsterController user, SkillData skill)
    {
        int heal = Mathf.RoundToInt(user.mgc * (skill.power / 100f));
        return Mathf.Max(1, heal);
    }

    // ================================
    // バフ計算
    // ================================
    public static float CalculateBuffValue(SkillData skill)
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

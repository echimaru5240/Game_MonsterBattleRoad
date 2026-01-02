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
        public float Multiplier;
        public bool IsCritical;
        public bool IsMiss;
        public StatusAilmentType statusAilmentType;
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
            switch (skill.action) {
                case SkillAction.ATTACK:
                    result = CalculateAttackResult(attacker, target, skill, disableCritical: isMultiTarget);
                    break;
                case SkillAction.HEAL:
                    result = CalculateHealResult(attacker, skill);
                    break;
                case SkillAction.SPECIAL:
                    result = CalculateSpecialResult(attacker, target, skill);
                    break;
            }
            results.Add(result);
        }

        return results;
    }

    public static void ApplyTimingTapResults(List<ActionResult> results, float timingMultiplier)
    {
        if (results == null || results.Count == 0) return;

        // 0以下は変な値なのでガード（任意）
        if (timingMultiplier <= 0f) timingMultiplier = 1f;

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];

            // Miss の結果はそのまま（倍率かけても 0 のまま）
            // もし「Missでも少し入る」にしたいなら IsMiss の時に value を作る設計に変える
            if (r.IsMiss)
            {
                r.Multiplier = 0f; // 表示用に 0 としておくなら
                results[i] = r;
                continue;
            }

            r.Multiplier = timingMultiplier;

            // ダメージ/回復どちらでも Value に倍率をかける（好みで分けてもOK）
            r.Value = Mathf.Max(0, Mathf.RoundToInt(r.Value * timingMultiplier));

            results[i] = r;
        }
    }

    /// <summary>
    /// （新）事前計算済みの結果を反映
    /// 当たりフレーム（アニメーションイベント）で呼び出す
    /// </summary>
    public static bool ApplyActionResults( List<ActionResult> results )
    {
        bool battleEnd = false;

        if (results == null || results.Count == 0) return false;

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

            if (result.statusAilmentType != StatusAilmentType.NONE) target.battleData.statusAilmentType = result.statusAilmentType;
        }

        // サイドHPへ一括反映（今の設計に合わせる）
        if (isPlayerSide)
        {
            mgr.PlayerCurrentHP = Mathf.Max(0, mgr.PlayerCurrentHP - totalDamage);
            if (mgr.PlayerCurrentHP <= 0)
            {
                battleEnd = true;
            }
        }
        else
        {
            mgr.EnemyCurrentHP = Mathf.Max(0, mgr.EnemyCurrentHP - totalDamage);
            if (mgr.EnemyCurrentHP <= 0)
            {
                battleEnd = true;
            }
        }
        mgr.UpdateHPBars();
        return battleEnd;
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

    /// <summary>
    /// 攻撃結果をまとめて返す
    /// </summary>
    public static ActionResult CalculateSpecialResult(MonsterController attacker, MonsterController target, SkillData skill, bool disableCritical = false)
    {
        bool isMiss = Random.value > skill.accuracy; // ミス判定などもここで

        return new ActionResult
        {
            Target = target,
            IsDamage = false,
            Value = 0,
            IsCritical = false,
            IsMiss = isMiss,
            statusAilmentType = isMiss ? StatusAilmentType.NONE : skill.statusAilmentType,
        };
    }

    // ================================
    // 通常攻撃ダメージ計算
    // ================================
    public static int CalculateDamage(MonsterController attacker, MonsterController defender, SkillData skill)
    {
        float atk = 0f;
        switch (skill.category) {
            case  SkillCategory.PHYSICAL:
                atk = attacker.battleData.atk;
                break;
            case  SkillCategory.MAGICAL:
                atk = attacker.battleData.mgc;
                break;
            case  SkillCategory.SPECIAL:
                atk = (attacker.battleData.atk + attacker.battleData.mgc) / 2;
                break;
            default:
                atk = attacker.battleData.atk;
                break;
        }
        float def = defender != null ? defender.battleData.def : 0f;

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
        int heal = Mathf.RoundToInt(user.battleData.mgc + skill.power);
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

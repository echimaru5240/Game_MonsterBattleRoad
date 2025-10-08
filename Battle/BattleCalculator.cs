using UnityEngine;
using System.Collections.Generic;

public static class BattleCalculator
{
    // ���ʊi�[�p�\����
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

        // �@ �_���[�W�v�Z�ƃ|�b�v�A�b�v�\��
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
                    // mgr.ui.ShowMessage("�N���e�B�J���q�b�g�I");
            }
            else
            {
                // mgr.ui.ShowMessage($"{attacker.cardData.cardName} �̍U���͊O�ꂽ�I");
            }
        }

        // �A HP���ꊇ�Ŕ��f
        if (isPlayerSide)
            mgr.EnemyCurrentHP = Mathf.Max(0, mgr.EnemyCurrentHP - totalDamage);
        else
            mgr.PlayerCurrentHP = Mathf.Max(0, mgr.PlayerCurrentHP - totalDamage);

        mgr.UpdateHPBars();
    }

    /// <summary>
    /// �U�����ʂ��܂Ƃ߂ĕԂ�
    /// </summary>
    public static DamageResult CalculateAttackResult(MonsterCard attacker, MonsterCard defender, Skill skill, bool disableCritical = false)
    {
        int damage = CalculateDamage(attacker, defender, skill);
        bool isCritical = Random.value < skill.criticalChance; // �X�L�����Ɋm������������z��
        if (isCritical)
        {
            damage = Mathf.RoundToInt(damage * 1.5f);
        }

        return new DamageResult
        {
            Damage = damage,
            IsCritical = isCritical,
            IsMiss = Random.value < skill.missChance // �~�X����Ȃǂ�������
        };
    }

    // ================================
    // �ʏ�U���_���[�W�v�Z
    // ================================
    public static int CalculateDamage(MonsterCard attacker, MonsterCard defender, Skill skill)
    {
        int attack = attacker.attack;
        int defense = defender != null ? defender.defense : 0;

        float power = skill.power; // Skill ���Ő��l�i100�Ȃ瓙�{�j

        // �U�����F (�U���� �~ (�{��/100)) - (�h��͂̔���)
        float baseDamage = (attack * (power / 100f)) - (defense * 0.5f);

        // �����_���␳ �}10%
        float randomFactor = Random.Range(0.9f, 1.1f);

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * randomFactor));
        return finalDamage;
    }

    // ================================
    // �񕜌v�Z
    // ================================
    public static int CalculateHeal(MonsterCard user, Skill skill)
    {
        int heal = Mathf.RoundToInt(user.magicPower * (skill.power / 100f));
        return Mathf.Max(1, heal);
    }

    // ================================
    // �o�t�v�Z
    // ================================
    public static float CalculateBuffValue(Skill skill)
    {
        return (skill.power - 100) / 100f; // 120�Ȃ�+20%
    }

    // ================================
    // �Ƃǂ߂̈ꌂ�i�Œ�l or �J�[�h�ˑ��j
    // ================================
    public static int CalculateFinisherDamage(Card finisherCard)
    {
        // Card �ɁufinisherPower�v����������ꍇ
        return finisherCard.finisherDamage;
    }
}

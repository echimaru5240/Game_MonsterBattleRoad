using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonsterController : MonoBehaviour
{
    private Animator animator;

    public bool isPlayer;
    public MonsterCard cardData;

    // �����X�^�[���Ƃ̍s���X�N���v�g�i���I�ɒǉ������j
    private MonsterActionBase actionBehavior;

    private Skill currentSkill;
    private List<MonsterController> currentTargets = new();

    // ? ���̈ʒu��ۑ����Ă���
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // �U��������҂t���O
    private bool attackEnded = false;

    /// <summary>
    /// ������
    /// </summary>
    public void Init(bool isPlayer, MonsterCard card)
    {
        this.isPlayer = isPlayer;
        this.cardData = card;
        animator = GetComponent<Animator>();

        // �e�����X�^�[�̍U���X�N���v�g���擾
        actionBehavior = GetComponent<MonsterActionBase>();
        if (actionBehavior == null)
        {
            Debug.LogWarning($"{name} �� MonsterActionBase �h���X�N���v�g������܂���B�ʏ�U�����g�p���܂��B");
            actionBehavior = gameObject.AddComponent<MonsterAction_Common>();
        }

        // �������̈ʒu���L�^
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public IEnumerator PerformAttack(List<MonsterController> targets, Skill skill)
    {
        if (actionBehavior == null || targets == null || targets.Count == 0)
            yield break;

        attackEnded = false;

        currentSkill = skill;
        currentTargets = targets;

        yield return StartCoroutine(actionBehavior.Execute(this, targets[0], skill));

        while (!attackEnded) yield return null;
        yield return new WaitForSeconds(1.5f);
    }

    public void ReturnToInitialPosition()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }


    /// <summary>
    /// ��e�A�j���[�V�����{�J�������o
    /// </summary>
    public void PlayHit(Transform attacker)
    {
        if (animator != null)
        {
            animator.SetTrigger("DoHit");

            // ������щ��o
            if (attacker != null)
                StartCoroutine(Knockback(attacker));
            // ��e���̊��J����
            // if (CameraManager.Instance != null)
            //     CameraManager.Instance?.PlayHitReactionCamera(transform, isPlayer, 1.2f);
        }
    }

    /// <summary>
    /// �U�����󂯂����ɐ�����ԉ��o
    /// </summary>
    public IEnumerator Knockback(Transform attacker, float power = 3f, float duration = 0.3f)
    {
        if (attacker == null) yield break;

        Vector3 start = transform.position;

        // �U���� �� ��e�� �����x�N�g��
        Vector3 dir = (transform.position - attacker.position).normalized;
        dir.y = 0f; // ������͕s�v�i�n�ʂŐ����ɔ�΂��j

        // ������ѐ�̈ʒu
        Vector3 end = start + dir * power;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, end, t);

            // �p���{�����ۂ�������ɕ������o
            float height = Mathf.Sin(t * Mathf.PI) * 0.5f;
            transform.position += Vector3.up * height;

            yield return null;
        }

        // �Ō�ɏ����o�E���h�i�I�v�V�����j
        yield return new WaitForSeconds(0.1f);
        transform.position = end;
}


    /// <summary>
    /// �퓬�s�\
    /// </summary>
    public void PlayDie()
    {
        if (animator != null)
        {
            animator.SetTrigger("DoDie");
        }
    }


    /// <summary>
    /// �퓬�������[�V����
    /// </summary>
    public void PlayResultWin(bool isResult)
    {
        if (animator != null)
        {
            animator.SetBool("IsResult", isResult);
        }
    }

    /// <summary>
    /// �U��������u�ԁi�A�j���[�V�����C�x���g�ŌĂ΂��j
    /// </summary>
    public void OnAttack()
    {
        AudioManager.Instance.PlayActionSE(cardData.attackSE);
        // ? �U���G�t�F�N�g���Ăяo��
        // Vector3 effectPos = target.transform.position + Vector3.up * 1f;
        // EffectManager.Instance.PlayEffect(actionBehavior.attackEffectType, effectPos);
        Debug.Log("OnAttack");
    }

    /// <summary>
    /// �U����������u�ԁi�A�j���[�V�����C�x���g�ŌĂ΂��j
    /// </summary>
    public void OnAttackHit()
    {
        BattleCalculator.OnAttackHit(this, currentSkill, currentTargets);
        foreach (var target in currentTargets)
        {
            target.PlayHit(transform); // �� ������n�����Ƃŕ��������܂�
        }
        Debug.Log("OnAttackHit");
    }

    /// <summary>
    /// �U���A�j���[�V�����I�[�C�x���g
    /// �i�A�j���[�V�����C�x���g����Ă΂��z��j
    /// </summary>
    public void OnAttackEnd()
    {
        attackEnded = true; // �� �t���OON��PerformAttack���ĊJ
        Debug.Log("OnAttackEnd");
    }


    /// <summary>
    /// �ړ��̃|�C���g
    /// �i�A�j���[�V�����C�x���g����Ă΂��z��j
    /// </summary>
    public void OnWalkPoint()
    {
        AudioManager.Instance.PlayActionSE(cardData.moveSE);
        Debug.Log("OnWalkPoint");
    }
}

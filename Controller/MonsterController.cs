using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonsterController : MonoBehaviour
{
    private Animator animator;

    public bool isPlayer;

    [Header("�X�e�[�^�X")]
    public string name;
    public Sprite sprite;
    public int hp;
    public int attack;
    public int magicPower; // ���@��񕜗p
    public int defense;
    public int speed;

    [Header("�s���֘A")]
    public List<Skill> skills = new();

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
    public void InitializeFromCard(MonsterCard card, bool isPlayer)
    {
        this.isPlayer = isPlayer;
        name = card.cardName;
        sprite = card.monsterSprite;

        hp = card.hp;
        attack = card.attack;
        magicPower = card.magicPower;
        defense = card.defense;
        speed = card.speed;

        skills = new List<Skill>(card.skills);

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

        yield return StartCoroutine(actionBehavior.Execute(this, targets, skill));

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
    public void PlayHit()
    {
        if (animator != null)
        {
            animator.SetTrigger("DoHit");

            // ������щ��o
            StartCoroutine(Knockback());
        }
    }

    /// <summary>
    /// �U�����󂯂����ɐ�����ԉ��o
    /// </summary>
    public IEnumerator Knockback(float power = 3f, float duration = 0.3f)
    {
        Vector3 start = transform.position;


        // ������ѐ�̈ʒu
        Vector3 end = start + (isPlayer ? Vector3.back : Vector3.forward)  * power;

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
    /// �U����������u�ԁi�A�j���[�V�����C�x���g�ŌĂ΂��j
    /// </summary>
    public void OnAttackHit()
    {
        BattleCalculator.OnAttackHit(this, currentSkill, currentTargets);
        foreach (var target in currentTargets)
        {
            target.PlayHit(); // �� ������n�����Ƃŕ��������܂�
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
}

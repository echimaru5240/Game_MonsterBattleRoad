using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonsterController : MonoBehaviour
{
    private Animator animator;
    private CameraManager cameraManager;

    public MonsterCard cardData;
    public bool isEnemy;

    private Skill currentSkill;
    private List<MonsterController> currentTargets = new();

    // ? ���̈ʒu��ۑ����Ă���
    private Vector3 initialPosition;

    // �U��������҂t���O
    private bool attackEnded = false;

    /// <summary>
    /// ������
    /// </summary>
    public void Init(CameraManager camMgr, bool isEnemy, MonsterCard card)
    {
        animator = GetComponent<Animator>();
        cameraManager = camMgr;
        this.isEnemy = isEnemy;
        cardData = card;

        // �������̈ʒu���L�^
        initialPosition = transform.position;
    }

    public IEnumerator PerformAttack(List<MonsterController> targets, Skill skill)
    {
        currentSkill = skill;
        currentTargets = targets;
        attackEnded = false; // �U���J�n���Ƀ��Z�b�g

        Quaternion startRot = transform.rotation;

        // �@ ���ʃV���b�g
        cameraManager?.SwitchToFrontCamera(transform, isEnemy);

        yield return new WaitForSeconds(1.5f); // ����������

        // �A �U���Ώۂֈړ�
        // �P�̍U�����͑ΏۂɈړ�
        if (targets.Count == 1)
        {
            var target = targets[0];
            Vector3 start = transform.position;
            Vector3 end = target.transform.position + (isEnemy ? Vector3.forward : Vector3.back) * 1.2f; // ������O��
            float t = 0;

            // ? �^�[�Q�b�g����������
            Vector3 dir = (end - start).normalized;
            dir.y = 0;
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = lookRot;
            cameraManager.SwitchToActionCamera(target.transform, isEnemy, transform);
            if (animator != null)
            {
                animator.SetBool("IsMove", true);
            }

            while (t < 1f)
            {
                t += Time.deltaTime * 0.5f; // �ړ����x
                transform.position = Vector3.Lerp(start, end, t);

                // �Ǐ]�J����
                // cameraManager?.SwitchToActionCamera(transform, isEnemy);

                yield return null;
            }

            if (animator != null)
            {
                animator.SetBool("IsMove", false);
            }
        }

        transform.rotation = startRot;
        // �B �U�����[�V����
        PlayAttack();
        // �U���A�j������ OnAttackHit() �� onHitCallback ���Ă΂��
        // ? �U�����[�V�������I���܂őҋ@
        while (!attackEnded)
            yield return null;
        yield return new WaitForSeconds(1f); // �A�j�����Ԃɍ��킹��

        // �C ���̈ʒu�֖߂�
        if (targets.Count == 1)
        {
            Vector3 start = transform.position;
            Vector3 end = initialPosition; // �U���O�̍��W�ɖ߂�
            transform.position = initialPosition;
        }

        // �߂�����S�̃J������
        cameraManager?.SwitchToOverviewCamera();


        // �D ���̍s���܂ŏ����Ԃ�u���i�]�C�^�C���j
        yield return null;//new WaitForSeconds(1.0f); // �A�j�����Ԃɍ��킹��
    }

    /// <summary>
    /// �U���A�j���[�V�����{�J�������o
    /// </summary>
    public void PlayAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger("DoAttack");

            // �U�����J�������o
            // if (cameraManager != null)
                // cameraManager.SwitchToActionCamera(transform, isEnemy);
                // StartCoroutine(cameraManager.MoveCameraAroundAttacker(transform, 3f));

                // cameraManager?.PlayAttackCamera(transform, isEnemy, 1.8f);
        }
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
            // if (cameraManager != null)
            //     cameraManager?.PlayHitReactionCamera(transform, isEnemy, 1.2f);
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
        yield return new WaitForSeconds(1f);
        transform.position = start;
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
    /// �퓬�s�\
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

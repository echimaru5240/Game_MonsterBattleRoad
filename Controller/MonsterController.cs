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

        yield return new WaitForSeconds(1.0f); // ����������

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

            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f; // �ړ����x
                transform.position = Vector3.Lerp(start, end, t);

                // �Ǐ]�J����
                // cameraManager?.SwitchToActionCamera(transform, isEnemy);

                yield return null;
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
    public void PlayHit()
    {
        if (animator != null)
        {
            animator.SetTrigger("DoHit");

            // ��e���̊��J����
            // if (cameraManager != null)
            //     cameraManager?.PlayHitReactionCamera(transform, isEnemy, 1.2f);
        }
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
    /// �U����������u�ԁi�A�j���[�V�����C�x���g�ŌĂ΂��j
    /// </summary>
    public void OnAttackHit()
    {
        BattleCalculator.OnAttackHit(this, currentSkill, currentTargets);
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

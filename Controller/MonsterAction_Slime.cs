using UnityEngine;
using System.Collections;
using DG.Tweening;

public class MonsterAction_Slime : MonsterActionBase
{
    [Header("�X���C�����o�ݒ�")]
    public float moveDuration = 0.3f;    // 1��̃W�O�U�O����
    public float zigzagAmplitude = 1.2f; // �W�O�U�O��
    public float jumpHeight = 2.5f;
    public float jumpDuration = 0.4f;
    public float diveDuration = 0.25f;   // �}�~������
    public float slideDistance = 1.2f;
    public float slideDuration = 0.3f;

    public AudioClip moveSE;
    public AudioClip jumpSE;
    public AudioClip attackSE;

    // ? �U���q�b�g���̃p�[�e�B�N���v���n�u

    [Header("�U���G�t�F�N�g�^�C�v")]
    public AttackEffectType attackEffectType = AttackEffectType.ExplosionSmall;

    public override IEnumerator Execute(MonsterController self, MonsterController target, Skill skill)
    {
        var anim = self.GetComponent<Animator>();
        Vector3 startPos = self.transform.position;
        Vector3 centerPos = Vector3.zero;

        Sequence seq = DOTween.Sequence();

        // =============================
        // ? 3��W�O�U�O
        // =============================
        for (int i = 0; i < 3; i++)
        {
            Vector3 dir = new Vector3((i % 2 == 0 ? 1 : -1) * zigzagAmplitude, 0, 0);
            Vector3 targetPos = Vector3.Lerp(startPos, centerPos, (i + 1) / 3f) + dir;

            seq.AppendCallback(() => {
                anim.SetTrigger("DoMove");
                if (moveSE) AudioManager.Instance.PlaySE(moveSE);
            });

            seq.Append(self.transform.DOMove(targetPos, moveDuration)
                .SetEase(Ease.OutSine));
        }

        // =============================
        // ? �W�����v�i�㏸���^�[�Q�b�g�����j
        // =============================
        seq.AppendCallback(() => {
            if (jumpSE) AudioManager.Instance.PlaySE(jumpSE);
        });

        Vector3 jumpApex = self.transform.position
                        + (self.isPlayer ? Vector3.forward : Vector3.back) * 3
                        + Vector3.up * jumpHeight;

        // �㏸
        seq.Append(self.transform.DOMove(jumpApex, jumpDuration)
            .SetEase(Ease.OutQuad));

        // �����i�^�[�Q�b�g�ʒu�ɒ��n�j
        seq.Append(self.transform.DOMove(target.transform.position, diveDuration)
            .SetEase(Ease.InCubic)
            .OnStart(() => {
                anim.SetTrigger("DoAttack");
                // if (attackSE) AudioManager.Instance.PlaySE(attackSE);
            })
        );

        // �U���̗]�C���ԁi�����Ȃǂ��o���Ȃ炱���j
        seq.AppendInterval(0.1f);

        // =============================
        // ? �����~
        // =============================
        // �^�[�Q�b�g������Z����Ŋ���
        Vector3 slideTarget = target.transform.position +
            new Vector3(0, 0, self.isPlayer ? +slideDistance : -slideDistance);

        // �h���t�g�J�[�u�iEase.OutCubic�j�Ō������Ȃ��犊��
        seq.Append(self.transform.DOMove(slideTarget, slideDuration)
            .SetEase(Ease.OutCubic));

        // ����Ȃ��炿����Ɖ�]�i�h���t�g���j
        seq.Join(self.transform.DORotate(new Vector3(0, self.isPlayer ? 20f : -20f, 0), slideDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutQuad));

        // =============================
        // ? �U���I��
        // =============================
        seq.OnComplete(() => {
            self.OnAttackEnd();
        });

        // �V�[�P���X�I����ҋ@
        yield return seq.WaitForCompletion();
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class MonsterAction_Slime : MonsterActionBase
{
    [Header("スライム演出設定")]
    public float moveDuration = 0.3f;    // 1回のジグザグ時間
    public float zigzagAmplitude = 1.2f; // ジグザグ幅
    public float jumpHeight = 2.5f;
    public float jumpDuration = 0.4f;
    public float diveDuration = 0.25f;   // 急降下時間
    public float slideDistance = 1.2f;
    public float slideDuration = 0.3f;

    [Header("モーションSE")]
    public SoundEffectID moveSE;
    public SoundEffectID attackSE;
    public SoundEffectID jumpSE;

    [Header("エフェクト")]
    public EffectID skill1Effect;

    private List<MonsterController> currentTargets = new();

    public override IEnumerator Execute(MonsterController self, List<MonsterController> targets, SkillData skill)
    {
        currentTargets = targets;

        switch (skill.skillID)
        {
            /* プルプルアタック */
            case SkillID.SKILL_ID_SLIME_STRIKE:
                yield return StartCoroutine(Execute_Skill1(self, targets));
                break;
            /* マジックショット */
            case SkillID.SKILL_ID_FIRE_BURST:
                yield return StartCoroutine(Execute_Skill2(self, targets));
                break;
            default:
                Debug.LogWarning($"{self.name} のスキル「{skill.skillName}」は未実装です。");
                yield return StartCoroutine(Execute_Skill2(self, targets));
                break;
                // yield break;
        }
    }

    private IEnumerator Execute_Skill1(MonsterController self, List<MonsterController> targets)
    {
        var anim = self.GetComponent<Animator>();
        Vector3 startPos = self.transform.position;
        Vector3 centerPos = Vector3.zero;
        MonsterController target = targets[0];

        Sequence seq = DOTween.Sequence();

        // =============================
        // ? 3回ジグザグ
        // =============================
        CameraManager.Instance.SwitchToActionCameraBack(self.transform, self.isPlayer);
        for (int i = 0; i < 3; i++)
        {
            Vector3 dir = new Vector3((i % 2 == 0 ? 1 : -1) * zigzagAmplitude, 0, 0);
            Vector3 targetPos = Vector3.Lerp(startPos, centerPos, (i + 1) / 3f) + dir;

            seq.AppendCallback(() => {
                anim.SetTrigger("DoMove");
            });

            seq.Append(self.transform.DOMove(targetPos, moveDuration)
                .SetEase(Ease.OutSine));
        }

        seq.AppendCallback(() => {
            CameraManager.Instance.SwitchToFixedBackCamera(self.transform, self.isPlayer);
        });

        // =============================
        // ? ジャンプ（上昇→ターゲット落下）
        // =============================
        Vector3 jumpApex = self.transform.position
                        + (self.isPlayer ? Vector3.forward : Vector3.back) * 3
                        + Vector3.up * jumpHeight;

         seq.AppendCallback(() => {
                anim.SetTrigger("DoJump");
            });

        // 上昇
        seq.Append(self.transform.DOMove(jumpApex, jumpDuration)
            .SetEase(Ease.OutQuad));

        // 落下（ターゲット位置に着地）
        seq.Append(self.transform.DOMove(target.transform.position, diveDuration)
            .SetEase(Ease.InCubic)
            .OnStart(() => {
                anim.SetTrigger("DoAttack");
            })
        );

        // 攻撃の余韻時間（砂煙などを出すならここ）
        seq.AppendInterval(0.1f);

        // =============================
        // ? 滑り停止
        // =============================
        // ターゲット方向のZ軸基準で滑る
        Vector3 slideTarget = target.transform.position +
            new Vector3(0, 0, self.isPlayer ? +slideDistance : -slideDistance);

        // ドリフトカーブ（Ease.OutCubic）で減速しながら滑る
        seq.Append(self.transform.DOMove(slideTarget, slideDuration)
            .SetEase(Ease.OutCubic));

        // 滑りながらちょっと回転（ドリフト感）
        seq.Join(self.transform.DORotate(new Vector3(0, self.isPlayer ? 20f : -20f, 0), slideDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutQuad));

        // =============================
        // ? 攻撃終了
        // =============================
        seq.OnComplete(() => {
            self.OnAttackEnd();
        });

        // シーケンス終了を待機
        yield return seq.WaitForCompletion();
    }

    /// <summary>
    /// 動く瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnMove_Skill1()
    {
        if (moveSE != null) AudioManager.Instance.PlayActionSE(moveSE);
        Debug.Log("OnMove");
    }

    /// <summary>
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttack_Skill1()
    {
        if (attackSE != null) AudioManager.Instance.PlayActionSE(attackSE);
        Debug.Log("OnAttack");
    }

    /// <summary>
    /// ジャンプをする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnJump_Skill1()
    {
        if (jumpSE != null) AudioManager.Instance.PlayActionSE(jumpSE);
        Debug.Log("OnJump");
    }


    /// <summary>
    /// 攻撃が当たる瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttackHit_Skill1()
    {
        // 攻撃エフェクトを呼び出す
        Vector3 effectPos = currentTargets[0].transform.position + Vector3.up * 1f;
        EffectManager.Instance.PlayEffectByID(skill1Effect, effectPos, Quaternion.Euler(-90f, 0, 0));
        Debug.Log("OnAttackHit");
    }


    public IEnumerator Execute_Skill2(MonsterController self, List<MonsterController> targets)
    {
        var anim = self.GetComponent<Animator>();

        // 前進
        anim.SetBool("IsMove", true);
        yield return MoveToTarget(self, targets[0]);
        anim.SetBool("IsMove", false);

        // 攻撃
        anim.SetTrigger("DoAttack");

        self.OnAttackEnd();
    }
}

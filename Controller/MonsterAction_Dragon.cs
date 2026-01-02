using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class MonsterAction_Dragon : MonsterActionBase
{
    [Header("演出設定")]
    public float moveSpeed = 0.5f;    // 移動速度
    public float stopOffset = 2f;

    [Header("モーションSE")]
    public AudioClip moveSE;
    public AudioClip attackSE;
    public AudioClip jumpSE;

    [Header("エフェクト")]
    public EffectID skill1Effect;
    public EffectID fireBreathEffect;

    private MonsterController selfController;
    private List<BattleCalculator.ActionResult> currentActionResults = new();

    private Animator anim;
    private int breathCount = 0;

    public override IEnumerator Execute(MonsterController self, List<BattleCalculator.ActionResult> results, SkillData skill)
    {
        selfController = self;
        currentActionResults = results;

        switch (skill.skillID)
        {
            /* トラップバイト */
            case SkillID.SKILL_ID_TRAP_BITE:
                yield return StartCoroutine(Execute_Skill2());
                break;
            /* ファイアブレス */
            case SkillID.SKILL_ID_FIRE_BREATH:
                yield return StartCoroutine(Execute_FireBreath());
                break;
            default:
                Debug.LogWarning($"{selfController.name} のスキル「{skill.skillName}」は未実装です。");
                yield return StartCoroutine(Execute_Skill2());
                break;
                // yield break;
        }
    }


    /// <summary>
    /// 動く瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnMove_Skill1()
    {
        AudioManager.Instance.PlaySE(moveSE);
        Debug.Log("OnMove");
    }

    /// <summary>
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttack_Skill1()
    {
        AudioManager.Instance.PlaySE(attackSE);
        Debug.Log("OnAttack");
    }

    /// <summary>
    /// 攻撃が当たる瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttackHit_Skill1()
    {
        // 攻撃エフェクトを呼び出す
        // Vector3 effectPos = currentActionResults[0].Target.transform.position + Vector3.up * 1f;
        // EffectManager.Instance.PlayEffectByID(skill1Effect, effectPos);
        Debug.Log("OnAttackHit");
    }


    public IEnumerator Execute_Skill2()
    {
        anim = selfController.GetComponent<Animator>();

        // 前進
        anim.SetBool("IsMove", true);
        yield return MoveToTarget(selfController, currentActionResults[0].Target);
        anim.SetBool("IsMove", false);

        // 攻撃
        anim.SetTrigger("DoAttackB");
    }

    public IEnumerator Execute_FireBreath()
    {
        anim = selfController.GetComponent<Animator>();

        Vector3 offset = new Vector3(-1f, 4f, selfController.isPlayer ? 30f : -30f); // ここは好きな位置
        CameraManager.Instance.CutAction_Follow(selfController.transform, offset);

        // 攻撃
        anim.SetBool("IsFireBreath", true);
        yield return new WaitForSeconds(1f);
        // 攻撃エフェクトを呼び出す
        Vector3 effectPos = selfController.transform.position + Vector3.up * 2.5f + Vector3.forward * (selfController.isPlayer ? 3.5f : -3.5f);
        float rot = selfController.isPlayer ? 0f : 180f;
        yield return new WaitForSeconds(0.2f);
        EffectManager.Instance.PlayEffectByID(fireBreathEffect, effectPos, Quaternion.Euler(20f, rot, 180f), 3f);
    }

    /// <summary>
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnFireBreathRPTStart()
    {
        anim.SetBool("IsFireBreathRPT", true);
    }

    /// <summary>
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnFireBreathRPT()
    {
        selfController.OnAttackHit();
        breathCount++;
        if (breathCount == 2) selfController.OnStartTimingTap();
        if (breathCount > 4) {
            // OnFireBreathEnd();
            anim.SetBool("IsFireBreath", false);
            anim.SetBool("IsFireBreathRPT", false);
            breathCount = 0;
        }
        // Debug.Log("OnNeedleDanceSpin");
    }

    /// <summary>
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnFireBreathEnd()
    {
        anim.SetBool("IsFireBreath", false);
    }
}

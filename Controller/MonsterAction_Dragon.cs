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

    private MonsterController selfController;
    private List<BattleCalculator.ActionResult> currentActionResults = new();

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
            /* フロストバイト */
            case SkillID.SKILL_ID_FROST_BITE:
                yield return StartCoroutine(Execute_Skill2());
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
        var anim = selfController.GetComponent<Animator>();

        // 前進
        anim.SetBool("IsMove", true);
        yield return MoveToTarget(selfController, currentActionResults[0].Target);
        anim.SetBool("IsMove", false);

        // 攻撃
        anim.SetTrigger("DoAttackB");
    }
}

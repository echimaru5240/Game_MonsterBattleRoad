using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class MonsterAction_Chest : MonsterActionBase
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
                yield return StartCoroutine(Execute_TrapBite());
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

    private IEnumerator Execute_TrapBite()
    {
        var anim = selfController.GetComponent<Animator>();
        Vector3 start = currentActionResults[0].Target.transform.position;
        Vector3 end = selfController.transform.position + (currentActionResults[0].Target.isPlayer ? Vector3.back : Vector3.forward) * stopOffset;
        Quaternion startRot = selfController.transform.rotation;
        Debug.Log($"プレイヤー？：{selfController.isPlayer}, スタート：{start}, エンド：{end}");

        // ? ターゲット方向を向く
        Vector3 dir = (start - end).normalized;
        dir.y = 0;
        Quaternion lookRot = Quaternion.LookRotation(dir);
        selfController.transform.rotation = lookRot;


        // 前進
        anim.SetBool("IsChest", true);

        Vector3 offset = new Vector3(0f, 1f, currentActionResults[0].Target.isPlayer ? 1f : -1f); // ここは好きな位置
        CameraManager.Instance.EnableFirstPerson(currentActionResults[0].Target.transform, offset);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            currentActionResults[0].Target.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        selfController.transform.rotation = startRot;

        // 攻撃
        Vector3 effectPos = selfController.transform.position
                        + (Vector3.forward * ((selfController.isPlayer) ? 0.3f : -0.3f)) +  Vector3.up * 0.87f;
        EffectManager.Instance.PlayEffectByID(skill1Effect, effectPos, null, 0.3f);
        yield return new WaitForSeconds(0.5f);
        anim.SetTrigger("DoAttack");
        anim.SetBool("IsChest", false);
        yield return new WaitForSeconds(0.5f);
        Vector3 worldPos = new Vector3(1f, 1.5f, selfController.isPlayer ? -10f : 10f); // ここは好きな位置
        CameraManager.Instance.CutAction_FixedWorldLookOnly(worldPos, selfController.transform);

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
        anim.SetTrigger("DoAttack");

        selfController.OnAttackEnd();
    }
}

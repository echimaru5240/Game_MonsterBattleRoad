using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class MonsterAction_Cactus : MonsterActionBase
{
    [Header("演出設定")]
    public float moveSpeed = 0.5f;    // 移動速度
    public float stopOffset = 1.2f;
    public float jumpHeight = 2.5f;
    public float jumpDuration = 0.4f;
    public float diveDuration = 0.25f;   // 急降下時間
    public float slideDistance = 1.2f;
    public float slideDuration = 0.3f;

    [Header("モーションSE")]
    public ActionSE moveSE;
    public ActionSE attackSE;
    public ActionSE jumpSE;

    private List<MonsterController> currentTargets = new();

    // ? 攻撃ヒット時のパーティクルプレハブ

    [Header("攻撃エフェクトタイプ")]
    public AttackEffectType attackEffectType = AttackEffectType.ExplosionSmall;

    public override IEnumerator Execute(MonsterController self, List<MonsterController> targets, Skill skill)
    {
        currentTargets = targets;

        switch (skill.skillName)
        {
            case "突き刺す":
                yield return StartCoroutine(Execute_Skill1(self, targets));
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
        Vector3 start = self.transform.position;
        Vector3 end = targets[0].transform.position + (self.isPlayer ? Vector3.back : Vector3.forward) * stopOffset;
        Quaternion startRot = self.transform.rotation;
        Debug.Log($"突き刺す！");

        // ? ターゲット方向を向く
        Vector3 dir = (end - start).normalized;
        dir.y = 0;
        Quaternion lookRot = Quaternion.LookRotation(dir);
        self.transform.rotation = lookRot;

        // 前進
        anim.SetBool("IsMove", true);
        Debug.Log($"IsMove");
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            self.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        self.transform.rotation = startRot;
        anim.SetBool("IsMove", false);

        // 攻撃
        anim.SetTrigger("DoAttack");
    }

    /// <summary>
    /// 動く瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnMove_Skill1()
    {
        if (attackSE != null) AudioManager.Instance.PlayActionSE(moveSE);
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
    /// 攻撃が当たる瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttackHit_Skill1()
    {
        // 攻撃エフェクトを呼び出す
        Vector3 effectPos = currentTargets[0].transform.position + Vector3.up * 1f;
        EffectManager.Instance.PlayEffect(attackEffectType, effectPos);
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

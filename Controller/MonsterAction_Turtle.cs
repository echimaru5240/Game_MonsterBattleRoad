using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class MonsterAction_Turtle : MonsterActionBase
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
            case "くるくるアタック":
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
        anim.SetTrigger("DoAttack");
            CameraManager.Instance.SwitchToActionCamera1(targets[0].transform, targets[0].isPlayer);
        // シーケンス終了を待機
        yield return null;
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

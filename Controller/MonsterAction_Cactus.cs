using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class MonsterAction_Cactus : MonsterActionBase
{
    [Header("演出設定")]
    public float moveDuration = 0.5f;    // 移動速度
    public float jumpHeight = 2.5f;
    public float jumpDuration = 0.4f;
    public float spinDuration = 3f;
    public float diveDuration = 1f;   // 急降下時間
    public float slideDistance = 1.2f;
    public float slideDuration = 0.3f;

    [Header("モーションSE")]
    public AudioClip moveSE;
    public AudioClip attackSE;
    public AudioClip jumpSE;
    public AudioClip needleSE;

    [Header("エフェクト")]
    public EffectID needleEffect;
    public EffectID healEffect;

    private Animator anim;
    private MonsterController selfController;
    private List<BattleCalculator.ActionResult> currentActionResults = new();

    private Sequence needleDanceSeq;
    private Vector3 needleEnd;  // 着地位置
    private int spinCount = 0;

    public override IEnumerator Execute(MonsterController self, List<BattleCalculator.ActionResult> results, SkillData skill)
    {
        selfController = self;
        currentActionResults = results;

        switch (skill.skillID)
        {
            /* ニードルダンス */
            case SkillID.SKILL_ID_TOUSAND_NEEDLE:
                yield return StartCoroutine(Execute_NeedleDance());
                break;
            /* サボテンジュース */
            case SkillID.SKILL_ID_ENERGY_CACTUS:
                yield return StartCoroutine(Execute_Heal());
                break;
            default:
                Debug.LogWarning($"{selfController.name} のスキル「{skill.skillName}」は未実装です。");
                yield return StartCoroutine(Execute_Heal());
                break;
                // yield break;
        }
    }

    private IEnumerator Execute_NeedleDance()
    {
        anim = selfController.GetComponent<Animator>();
        Vector3 start = selfController.transform.position;
        Vector3 end = Vector3.zero;
        Quaternion startRot = selfController.transform.rotation;
        spinCount = 0;

        Vector3 offset = new Vector3(0f, 2f, selfController.isPlayer ? 13f : -13f); // ここは好きな位置
        CameraManager.Instance.CutAction_FixedWorldLookOnly(offset, selfController.transform);
        // ? ターゲット方向を向く
        Vector3 dir = (end - start).normalized;
        dir.y = 0;
        Quaternion lookRot = Quaternion.LookRotation(dir);
        selfController.transform.rotation = lookRot;

        // 真ん中まで前進
        anim.SetBool("DoMove", true);
        selfController.transform.DOMove(end, moveDuration);
        selfController.transform.rotation = startRot;
        yield return null;
    }

    /// <summary>
    /// ニードルダンスのジャンプ開始（アニメーションイベントから呼ばれる）
    /// </summary>
    public void OnNeedleDanceJump()
    {
        Debug.Log("OnNeedleDanceJump");

        if (selfController == null || anim == null || currentActionResults == null || currentActionResults.Count == 0)
        {
            Debug.LogWarning("OnNeedleDanceJump: state not initialized.");
            return;
        }

        // 既存のシーケンスがあれば殺す
        if (needleDanceSeq != null && needleDanceSeq.IsActive())
        {
            needleDanceSeq.Kill();
        }

        Vector3 startPos = selfController.transform.position;

        needleDanceSeq = DOTween.Sequence();

        // =============================
        // ジャンプ頂点
        // =============================
        Vector3 jumpApex = startPos
                            + (selfController.isPlayer ? Vector3.forward : Vector3.back) * 3f
                            + Vector3.up * jumpHeight;

        // カメラ切り替え
        needleDanceSeq.AppendCallback(() =>
        {
            // CameraManager.Instance.SwitchToFixed8_13Camera(currentActionResults[0].Target.transform, !selfController.isPlayer);

            Vector3 worldPos = new Vector3(1f, jumpHeight, selfController.isPlayer ? -10f : 10f); // ここは好きな位置
            CameraManager.Instance.CutAction_FixedWorldLookOnly(worldPos, currentActionResults[1].Target.transform);
            anim.SetTrigger("DoJump");
        });

        // 上昇
        needleDanceSeq.Append(selfController.transform.DOMove(jumpApex, jumpDuration)
            .SetEase(Ease.OutQuad));

        needleDanceSeq.OnComplete(() =>
        {
            anim.SetBool("IsNeedleSpin", true);
        });
    }

    /// <summary>
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnNeedleDanceSpin()
    {
        // 攻撃エフェクトを呼び出す
        Vector3 effectPos = selfController.transform.position + Vector3.forward * 1f;
        float rot = selfController.isPlayer ? 0f : 180f;
        EffectManager.Instance.PlayEffectByID(needleEffect, effectPos, Quaternion.Euler(20f, rot, 180f), 0.3f);
        AudioManager.Instance.PlaySE(needleSE);
        selfController.OnAttackHit();
        spinCount++;
        if (spinCount > 9) {
            anim.SetBool("IsNeedleSpin", false);
            spinCount = 0;
        }
        Debug.Log("OnNeedleDanceSpin");
    }

    /// <summary>
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnNeedleDanceFall()
    {
        selfController.transform.DOMove(needleEnd, diveDuration);
        Debug.Log("OnNeedleDanceFall");
    }

    /// <summary>
    /// 動く瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnMove_Skill1()
    {
        AudioManager.Instance.PlaySE(moveSE);
        // Debug.Log("OnMove");
    }

    /// <summary>
    /// ジャンプする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnJump()
    {
        AudioManager.Instance.PlaySE(jumpSE);
        // Debug.Log("OnJump");
    }

    /// <summary>
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttack_Skill1()
    {
        AudioManager.Instance.PlaySE(attackSE);
        // Debug.Log("OnAttack");
    }

    /// <summary>
    /// 攻撃が当たる瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttackHit_Skill1()
    {
        // 攻撃エフェクトを呼び出す
        // Vector3 effectPos = selfController.transform.position + Vector3.up * 1f;
        // EffectManager.Instance.PlayEffectByID(needleEffect, effectPos);
        // Debug.Log("OnAttackHit");
    }


    public IEnumerator Execute_Heal()
    {
        var anim = selfController.GetComponent<Animator>();

        yield return null;

        // 攻撃
        anim.SetTrigger("DoHeal");
        // 攻撃エフェクトを呼び出す
        Vector3 effectPos = selfController.transform.position;
        EffectManager.Instance.PlayEffectByID(healEffect, effectPos, Quaternion.Euler(0f, 0f, 0f), 2.0f);
    }

    /// <summary>
    /// 攻撃が当たる瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAction_Heal()
    {
        // 攻撃エフェクトを呼び出す
        Vector3 effectPos = selfController.transform.position;
        EffectManager.Instance.PlayEffectByID(healEffect, effectPos, Quaternion.Euler(0f, 0f, 0f), 1.0f);
        // Debug.Log("OnAction_Heal");
    }

}

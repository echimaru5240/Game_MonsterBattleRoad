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
    public SoundEffectID moveSE;
    public SoundEffectID attackSE;
    public SoundEffectID jumpSE;

    [Header("エフェクト")]
    public EffectID needleEffect;
    public EffectID healEffect;

    private Animator anim;
    private MonsterController selfController;
    private List<MonsterController> currentTargets = new();

    private Sequence needleDanceSeq;
    private Vector3 needleEnd;  // 着地位置
    private int spinCount = 0;

    public override IEnumerator Execute(MonsterController self, List<MonsterController> targets, SkillData skill)
    {
        currentTargets = targets;
        selfController = self;

        switch (skill.skillID)
        {
            /* ニードルダンス */
            case SkillID.SKILL_ID_TOUSAND_NEEDLE:
                yield return StartCoroutine(Execute_NeedleDance(self, targets));
                break;
            /* サボテンジュース */
            case SkillID.SKILL_ID_ENERGY_CACTUS:
                yield return StartCoroutine(Execute_Skill2(self, targets));
                break;
            default:
                Debug.LogWarning($"{self.name} のスキル「{skill.skillName}」は未実装です。");
                yield return StartCoroutine(Execute_Skill2(self, targets));
                break;
                // yield break;
        }
    }

    private IEnumerator Execute_NeedleDance(MonsterController self, List<MonsterController> targets)
    {
        anim = self.GetComponent<Animator>();
        Vector3 start = self.transform.position;
        Vector3 end = Vector3.zero;
        Quaternion startRot = self.transform.rotation;
        spinCount = 0;

        // ? ターゲット方向を向く
        Vector3 dir = (end - start).normalized;
        dir.y = 0;
        Quaternion lookRot = Quaternion.LookRotation(dir);
        self.transform.rotation = lookRot;

        // 真ん中まで前進
        anim.SetBool("DoMove", true);
        self.transform.DOMove(end, moveDuration);
        self.transform.rotation = startRot;
        yield return null;
    }

    /// <summary>
    /// ニードルダンスのジャンプ開始（アニメーションイベントから呼ばれる）
    /// </summary>
    public void OnNeedleDanceJump()
    {
        Debug.Log("OnNeedleDanceJump");

        if (selfController == null || anim == null || currentTargets == null || currentTargets.Count == 0)
        {
            Debug.LogWarning("OnNeedleDanceJump: state not initialized.");
            return;
        }

        // 既存のシーケンスがあれば殺す
        if (needleDanceSeq != null && needleDanceSeq.IsActive())
        {
            needleDanceSeq.Kill();
        }

        var self = selfController;
        Vector3 startPos = self.transform.position;

        needleDanceSeq = DOTween.Sequence();

        // =============================
        // ジャンプ頂点
        // =============================
        Vector3 jumpApex = startPos
                            + (self.isPlayer ? Vector3.forward : Vector3.back) * 3f
                            + Vector3.up * jumpHeight;

        // カメラ切り替え
        needleDanceSeq.AppendCallback(() =>
        {
            CameraManager.Instance.SwitchToFixed8_13Camera(currentTargets[0].transform, !self.isPlayer);
            anim.SetTrigger("DoJump");
        });

        // 上昇
        needleDanceSeq.Append(self.transform.DOMove(jumpApex, jumpDuration)
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
        AudioManager.Instance.PlayActionSE(SoundEffectID.SOUND_EFFECT_ID_ANIME_CUT);
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
        // Vector3 effectPos = selfController.transform.position + Vector3.up * 1f;
        // EffectManager.Instance.PlayEffectByID(needleEffect, effectPos);
        // Debug.Log("OnAttackHit");
    }


    public IEnumerator Execute_Skill2(MonsterController self, List<MonsterController> targets)
    {
        var anim = self.GetComponent<Animator>();

        yield return null;

        // 攻撃
        anim.SetTrigger("DoHeal");
        // 攻撃エフェクトを呼び出す
        Vector3 effectPos = self.transform.position;
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
        Debug.Log("OnAction_Heal");
    }

}

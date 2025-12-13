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
    public float castTime = 0.5f;          // 詠唱時間
    public float fireballHeight = 2.0f;    // 頭上の高さ
    public float fireballTravelTime = 0.4f;// 飛んでいく時間

    [Header("モーションSE")]
    public AudioClip moveSE;
    public AudioClip attackSE;
    public AudioClip jumpSE;
    public AudioClip castSE;
    public AudioClip shotSE;

    [Header("エフェクト")]
    public GameObject fireballPrefab;      // 飛んでいく玉
    public EffectID fireballHitEffect;
    public EffectID beamEffect;

    private MonsterController selfController;
    private List<BattleCalculator.ActionResult> currentActionResults = new();
    // 今飛んでいるファイアーボール
    private GameObject currentFireball;

    public override IEnumerator Execute(MonsterController self, List<BattleCalculator.ActionResult> results, SkillData skill)
    {
        selfController = self;
        currentActionResults = results;

        switch (skill.skillID)
        {
            /* ローリングスパイク */
            case SkillID.SKILL_ID_SPIKE_ROLLING:
                yield return StartCoroutine(Execute_SpikeRolling());
                break;
            /* ファイアバースト */
            case SkillID.SKILL_ID_FIRE_BURST:
                yield return StartCoroutine(Execute_FireBurst());
                break;
            default:
                Debug.LogWarning($"{selfController.name} のスキル「{skill.skillName}」は未実装です。");
                yield return StartCoroutine(Execute_Skill2());
                break;
                // yield break;
        }
    }

    private IEnumerator Execute_SpikeRolling()
    {
        var anim = selfController.GetComponent<Animator>();
        if (currentActionResults == null || currentActionResults.Count == 0)
        {
            selfController.OnAttackEnd();
            yield break;
        }
        MonsterController target = currentActionResults[0].Target;

        Vector3 startPos = selfController.transform.position;
        Quaternion startRot = selfController.transform.rotation;

        // ターゲット方向を向く
        Vector3 dir = (target.transform.position - startPos).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            selfController.transform.rotation = Quaternion.LookRotation(dir);
        }
        CameraManager.Instance.SwitchToFixedBackCamera(target.transform, selfController.isPlayer);
        // 3. 頭上にファイアーボール生成
        Vector3 spawnPos = selfController.transform.position + Vector3.forward * 6f + Vector3.up * 1f;
        EffectManager.Instance.PlayEffectByID(beamEffect, spawnPos, Quaternion.Euler(0f, 0f, 0f));
        yield return new WaitForSeconds(2.5f);
        selfController.OnAttackLastHit();
        selfController.OnAttackEnd();
    }

    private IEnumerator Execute_FireBurst()
    {
        var anim = selfController.GetComponent<Animator>();
        if (currentActionResults == null || currentActionResults.Count == 0)
        {
            selfController.OnAttackEnd();
            yield break;
        }
        MonsterController target = currentActionResults[0].Target;

        Vector3 startPos = selfController.transform.position;
        Quaternion startRot = selfController.transform.rotation;

        // ターゲット方向を向く
        Vector3 dir = (target.transform.position - startPos).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            selfController.transform.rotation = Quaternion.LookRotation(dir);
        }
        CameraManager.Instance.SwitchToFixedBackCamera(target.transform, selfController.isPlayer);
        anim.SetTrigger("DoCast");
    }

    /// <summary>
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttack_Skill1()
    {
        if (attackSE != null) AudioManager.Instance.PlaySE(attackSE);
        Debug.Log("OnAttack");
    }

    /// <summary>
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttack_Skill2()
    {
        AudioManager.Instance.PlaySE(shotSE);
        Debug.Log("OnAttack2");
    }

    /// <summary>
    /// ファイアーボールを生成する瞬間に呼ばれる
    /// </summary>
    public void OnFireBallSpawn()
    {
        Debug.Log("FireBall Spawn!");
        // 3. 頭上にファイアーボール生成
        Vector3 spawnPos = selfController.transform.position + Vector3.up * fireballHeight;
        currentFireball = GameObject.Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
        AudioManager.Instance.PlaySE(castSE);

        if (currentFireball == null)
        {
            Debug.LogError("Fireball Instantiate failed: fireballPrefab is null?");
            selfController.OnAttackEnd();
        }

    }


    /// <summary>
    /// ファイアーボールがスケールアップする瞬間に呼ばれる
    /// </summary>
    public void OnFireBallScaleUp()
    {
        Debug.Log("FireBall ScaleUp!");
        // 出現演出（ふわっと大きくなる）
        currentFireball.transform.localScale = Vector3.zero;
        currentFireball.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// ファイアーボールを撃った瞬間に呼ばれる
    /// </summary>
    public void OnFireBallShot()
    {
        Debug.Log("FireBall Shot!");

        // =============================
        // ③ 敵に向かって飛ばす
        // =============================
        AudioManager.Instance.PlaySE(shotSE);
        Vector3 targetPos = currentActionResults[0].Target.transform.position + Vector3.up * 1.0f;
        Tween moveTween = currentFireball.transform.DOMove(targetPos, fireballTravelTime)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                OnFireBallHit(targetPos);
            });
    }

    /// <summary>
    /// ファイアーボールが当たった瞬間に呼ばれる
    /// </summary>
    private void OnFireBallHit(Vector3 hitPos)
    {
        Debug.Log("FireBall Hit!");
        selfController.OnAttackLastHit();

        // 爆発エフェクト
        if (fireballHitEffect != EffectID.None)
        {
            EffectManager.Instance.PlayEffectByID(fireballHitEffect, hitPos);
            if (currentActionResults[0].IsCritical){
                AudioManager.Instance.PlayCriticalSE();
            }
        }

        // 飛んでいたファイアーボール削除
        if (currentFireball != null)
        {
            GameObject.Destroy(currentFireball);
            currentFireball = null;
        }

        // =============================
        // ④ 後処理
        // =============================
        selfController.OnAttackEnd();
    }

    /// <summary>
    /// 攻撃が当たる瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttackHit_Skill1()
    {
        // 攻撃エフェクトを呼び出す
        Vector3 effectPos = currentActionResults[0].Target.transform.position + Vector3.up * 1f;
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

    public void OnMove()
    {
        AudioManager.Instance.PlaySE(moveSE);
        Debug.Log("OnMove");
    }
}

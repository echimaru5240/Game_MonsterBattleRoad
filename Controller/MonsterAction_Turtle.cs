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
    public SoundEffectID moveSE;
    public SoundEffectID attackSE;
    public SoundEffectID jumpSE;
    public SoundEffectID castSE;
    public SoundEffectID shotSE;

    [Header("エフェクト")]
    public GameObject fireballPrefab;      // 飛んでいく玉
    public EffectID fireballHitEffect;
    public EffectID skill1Effect;

    private MonsterController selfController;
    private List<MonsterController> currentTargets = new();
    // 今飛んでいるファイアーボール
    private GameObject currentFireball;

    public override IEnumerator Execute(MonsterController self, List<MonsterController> targets, SkillData skill)
    {
        selfController = self;
        currentTargets = targets;

        switch (skill.skillID)
        {
            /* ローリングスパイク */
            case SkillID.SKILL_ID_SPIKE_ROLLING:
                yield return StartCoroutine(Execute_SpikeRolling(self, targets));
                break;
            /* ファイアバースト */
            case SkillID.SKILL_ID_FIRE_BURST:
                yield return StartCoroutine(Execute_FireBurst(self, targets));
                break;
            default:
                Debug.LogWarning($"{self.name} のスキル「{skill.skillName}」は未実装です。");
                yield return StartCoroutine(Execute_Skill2(self, targets));
                break;
                // yield break;
        }
    }

    private IEnumerator Execute_SpikeRolling(MonsterController self, List<MonsterController> targets)
    {
        var anim = self.GetComponent<Animator>();
        if (targets == null || targets.Count == 0)
        {
            self.OnAttackEnd();
            yield break;
        }
        MonsterController target = targets[0];

        Vector3 startPos = self.transform.position;
        Quaternion startRot = self.transform.rotation;

        // ターゲット方向を向く
        Vector3 dir = (target.transform.position - startPos).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            self.transform.rotation = Quaternion.LookRotation(dir);
        }
        CameraManager.Instance.SwitchToFixedBackCamera(target.transform, self.isPlayer);
        anim.SetTrigger("DoCast");
    }

    private IEnumerator Execute_FireBurst(MonsterController self, List<MonsterController> targets)
    {
        var anim = self.GetComponent<Animator>();
        if (targets == null || targets.Count == 0)
        {
            self.OnAttackEnd();
            yield break;
        }
        MonsterController target = targets[0];

        Vector3 startPos = self.transform.position;
        Quaternion startRot = self.transform.rotation;

        // ターゲット方向を向く
        Vector3 dir = (target.transform.position - startPos).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            self.transform.rotation = Quaternion.LookRotation(dir);
        }
        CameraManager.Instance.SwitchToFixedBackCamera(target.transform, self.isPlayer);
        anim.SetTrigger("DoCast");
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
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttack_Skill2()
    {
        if (attackSE != null) AudioManager.Instance.PlayActionSE(shotSE);
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
        Vector3 targetPos = currentTargets[0].transform.position + Vector3.up * 1.0f;
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
        Vector3 effectPos = currentTargets[0].transform.position + Vector3.up * 1f;
        EffectManager.Instance.PlayEffectByID(skill1Effect, effectPos);
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

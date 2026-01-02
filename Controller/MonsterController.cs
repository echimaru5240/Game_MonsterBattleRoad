using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class MonsterController : MonoBehaviour
{
    private Animator animator;

    public bool isPlayer;

    public MonsterBattleData battleData;

    [Header("行動関連")]
    public List<SkillID> skills = new();

    // モンスターごとの行動スクリプト（動的に追加される）
    private MonsterActionBase actionBehavior;

    private List<BattleCalculator.ActionResult> currentActionResults = new();
    private SkillData currentSkill;
    private List<MonsterController> currentTargets = new();

    // ? 元の位置を保存しておく
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // 攻撃完了を待つフラグ
    private bool fAttackEnd = false;

    // 回避モーション済みかのフラグ
    private bool fDodge = false;

    /// <summary>
    /// 初期化
    /// </summary>
    public void InitializeFromData(MonsterBattleData monster, bool isPlayer)
    {
        this.isPlayer = isPlayer;
        battleData = monster;
        name = monster.Name;
        battleData.statusAilmentType = StatusAilmentType.NONE;

        skills = new List<SkillID>(monster.skills);

        animator = GetComponent<Animator>();

        // 各モンスターの攻撃スクリプトを取得
        actionBehavior = GetComponent<MonsterActionBase>();
        if (actionBehavior == null)
        {
            Debug.LogWarning($"{name} に MonsterActionBase 派生スクリプトがありません。通常攻撃を使用します。");
            actionBehavior = gameObject.AddComponent<MonsterAction_Common>();
        }

        // 生成時の位置を記録
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public IEnumerator PerformAction(List<MonsterController> targets, SkillData skill)
    {
        if (actionBehavior == null || targets == null || targets.Count == 0)
            yield break;

        fAttackEnd = false;
        fDodge = false;

        currentSkill = skill;
        currentTargets = targets;
        currentActionResults = BattleCalculator.PrecalculateActionResults(this, skill, targets);
        yield return StartCoroutine(actionBehavior.Execute(this, currentActionResults, skill));

        while (!fAttackEnd) yield return null;
    }

    public void ReturnToInitialPosition()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }


    /// <summary>
    /// 被弾アニメーション＋カメラ演出
    /// </summary>
    private void PlayHit(BattleCalculator.ActionResult result)
    {
        if (result.IsMiss && !fDodge) {
            StartCoroutine(Knockback());
            fDodge = true;
        }
        else {
            if (!fDodge)
            {
                if (animator != null)
                {
                    animator.SetTrigger("DoHit");
                }
            }
        }
    }

    /// <summary>
    /// 被弾アニメーション＋カメラ演出
    /// </summary>
    private void PlayLastHit(BattleCalculator.ActionResult result, bool battleEnd)
    {
        if (result.IsMiss) {
            if (!fDodge) StartCoroutine(Knockback());
        }
        else {
            if (result.IsDamage) {
                if (result.IsCritical)
                {
                    AudioManager.Instance.PlayCriticalSE();
                    CameraManager.Instance.ShakeCritical();
                }
                if (animator != null)
                {
                    if (battleEnd)
                    {
                        Time.timeScale = 0.2f;
                        DOTween.PlayAll();
                    }
                    // 吹き飛び演出
                    animator.SetTrigger("DoLastHit");
                    StartCoroutine(Knockback());
                }
            }
            else {
                if (battleData.statusAilmentType != StatusAilmentType.NONE)
                {
                    animator.SetBool("IsDizzy", true);
                }
            }
        }
    }

    public void RecoveryFromDizzy()
    {
        animator.SetBool("IsDizzy", false);
    }

    /// <summary>
    /// 攻撃を受けた時に吹き飛ぶ演出
    /// </summary>
    private IEnumerator Knockback(float power = 6f, float duration = 0.5f)
    {
        Vector3 start = transform.position;


        // 吹き飛び先の位置
        Vector3 end = start + (isPlayer ? Vector3.back : Vector3.forward)  * power;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, end, t);

            // パラボラっぽく少し上に浮く演出
            float height = Mathf.Sin(t * Mathf.PI) * 0.5f;
            transform.position += Vector3.up * height;

            yield return null;
        }

        // 最後に少しバウンド（オプション）
        yield return new WaitForSeconds(0.1f);
        transform.position = end;
}


    /// <summary>
    /// 戦闘不能
    /// </summary>
    public void PlayDie()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("DoDie");
        }
    }


    /// <summary>
    /// 戦闘勝利モーション
    /// </summary>
    public void PlayResult(int battleResult)
    {
        if (animator != null)
        {
            animator.SetInteger("BattleResult", battleResult);
        }
    }

    /// <summary>
    /// タップをスタートする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnStartTimingTap()
    {
        if (isPlayer)
        {
            StartCoroutine(PlayTimingTap());
        }
    }

    private IEnumerator PlayTimingTap()
    {

        float mul = 0f;
        yield return TimingTapManager.Instance.PlayTimingTap(r =>
        {
            mul = r.multiplier;
        });

        BattleCalculator.ApplyTimingTapResults(currentActionResults, mul);

    }

    /// <summary>
    /// 攻撃が当たる瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttackHit()
    {
        foreach (var result in currentActionResults)
        {
            result.Target.PlayHit(result); // ← 自分を渡すことで方向が決まる
        }
        // Debug.Log("OnAttackHit");
    }

    /// <summary>
    /// 攻撃が当たる瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttackLastHit()
    {
        bool battleEnd = BattleCalculator.ApplyActionResults(currentActionResults);
        foreach (var result in currentActionResults)
        {
            result.Target.PlayLastHit(result, battleEnd); // ← 自分を渡すことで方向が決まる
        }
        Debug.Log("OnAttackLastHit");
    }

    /// <summary>
    /// 攻撃アニメーション終端イベント
    /// （アニメーションイベントから呼ばれる想定）
    /// </summary>
    public void OnAttackEnd()
    {
        fAttackEnd = true; // ← フラグONでPerformActionが再開
        // Debug.Log("OnAttackEnd");
    }
}

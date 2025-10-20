using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonsterController : MonoBehaviour
{
    private Animator animator;

    public bool isPlayer;
    public MonsterCard cardData;

    // モンスターごとの行動スクリプト（動的に追加される）
    private MonsterActionBase actionBehavior;

    private Skill currentSkill;
    private List<MonsterController> currentTargets = new();

    // ? 元の位置を保存しておく
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // 攻撃完了を待つフラグ
    private bool attackEnded = false;

    /// <summary>
    /// 初期化
    /// </summary>
    public void Init(bool isPlayer, MonsterCard card)
    {
        this.isPlayer = isPlayer;
        this.cardData = card;
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

    public IEnumerator PerformAttack(List<MonsterController> targets, Skill skill)
    {
        if (actionBehavior == null || targets == null || targets.Count == 0)
            yield break;

        attackEnded = false;

        currentSkill = skill;
        currentTargets = targets;

        yield return StartCoroutine(actionBehavior.Execute(this, targets[0], skill));

        while (!attackEnded) yield return null;
        yield return new WaitForSeconds(1.5f);
    }

    public void ReturnToInitialPosition()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }


    /// <summary>
    /// 被弾アニメーション＋カメラ演出
    /// </summary>
    public void PlayHit(Transform attacker)
    {
        if (animator != null)
        {
            animator.SetTrigger("DoHit");

            // 吹き飛び演出
            if (attacker != null)
                StartCoroutine(Knockback(attacker));
            // 被弾時の寄りカメラ
            // if (CameraManager.Instance != null)
            //     CameraManager.Instance?.PlayHitReactionCamera(transform, isPlayer, 1.2f);
        }
    }

    /// <summary>
    /// 攻撃を受けた時に吹き飛ぶ演出
    /// </summary>
    public IEnumerator Knockback(Transform attacker, float power = 3f, float duration = 0.3f)
    {
        if (attacker == null) yield break;

        Vector3 start = transform.position;

        // 攻撃者 → 被弾者 方向ベクトル
        Vector3 dir = (transform.position - attacker.position).normalized;
        dir.y = 0f; // 上方向は不要（地面で水平に飛ばす）

        // 吹き飛び先の位置
        Vector3 end = start + dir * power;

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
        if (animator != null)
        {
            animator.SetTrigger("DoDie");
        }
    }


    /// <summary>
    /// 戦闘勝利モーション
    /// </summary>
    public void PlayResultWin(bool isResult)
    {
        if (animator != null)
        {
            animator.SetBool("IsResult", isResult);
        }
    }

    /// <summary>
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttack()
    {
        AudioManager.Instance.PlayActionSE(cardData.attackSE);
        // ? 攻撃エフェクトを呼び出す
        // Vector3 effectPos = target.transform.position + Vector3.up * 1f;
        // EffectManager.Instance.PlayEffect(actionBehavior.attackEffectType, effectPos);
        Debug.Log("OnAttack");
    }

    /// <summary>
    /// 攻撃が当たる瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttackHit()
    {
        BattleCalculator.OnAttackHit(this, currentSkill, currentTargets);
        foreach (var target in currentTargets)
        {
            target.PlayHit(transform); // ← 自分を渡すことで方向が決まる
        }
        Debug.Log("OnAttackHit");
    }

    /// <summary>
    /// 攻撃アニメーション終端イベント
    /// （アニメーションイベントから呼ばれる想定）
    /// </summary>
    public void OnAttackEnd()
    {
        attackEnded = true; // ← フラグONでPerformAttackが再開
        Debug.Log("OnAttackEnd");
    }


    /// <summary>
    /// 移動のポイント
    /// （アニメーションイベントから呼ばれる想定）
    /// </summary>
    public void OnWalkPoint()
    {
        AudioManager.Instance.PlayActionSE(cardData.moveSE);
        Debug.Log("OnWalkPoint");
    }
}

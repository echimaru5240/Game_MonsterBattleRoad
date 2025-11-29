using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonsterController : MonoBehaviour
{
    private Animator animator;

    public bool isPlayer;

    [Header("ステータス")]
    public string name;
    public Sprite sprite;
    public int hp;
    public int attack;
    public int magicPower; // 魔法や回復用
    public int defense;
    public int speed;

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
    private bool attackEnded = false;

    /// <summary>
    /// 初期化
    /// </summary>
    public void InitializeFromCard(MonsterCard card, bool isPlayer)
    {
        this.isPlayer = isPlayer;
        name = card.cardName;
        sprite = card.monsterSprite;

        hp = card.hp;
        attack = card.attack;
        magicPower = card.magicPower;
        defense = card.defense;
        speed = card.speed;

        skills = new List<SkillID>(card.skills);

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

        attackEnded = false;

        currentSkill = skill;
        currentTargets = targets;

        currentActionResults = BattleCalculator.PrecalculateActionResults(this, skill, targets);
        yield return StartCoroutine(actionBehavior.Execute(this, targets, skill));

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
    public void PlayHit()
    {
        if (animator != null)
        {
            animator.SetTrigger("DoHit");
        }
    }

    /// <summary>
    /// 被弾アニメーション＋カメラ演出
    /// </summary>
    public void PlayLastHit()
    {
        if (animator != null)
        {
            // 吹き飛び演出
            animator.SetTrigger("DoLastHit");
            StartCoroutine(Knockback());
        }
    }

    /// <summary>
    /// 攻撃を受けた時に吹き飛ぶ演出
    /// </summary>
    public IEnumerator Knockback(float power = 3f, float duration = 0.3f)
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
    /// 攻撃が当たる瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttackHit()
    {
        foreach (var target in currentTargets)
        {
            target.PlayHit(); // ← 自分を渡すことで方向が決まる
        }
        Debug.Log("OnAttackHit");
    }

    /// <summary>
    /// 攻撃が当たる瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttackLastHit()
    {
        BattleCalculator.ApplyActionResults(currentActionResults);
        foreach (var target in currentTargets)
        {
            target.PlayLastHit(); // ← 自分を渡すことで方向が決まる
        }
        Debug.Log("OnAttackLastHit");
    }

    /// <summary>
    /// 攻撃アニメーション終端イベント
    /// （アニメーションイベントから呼ばれる想定）
    /// </summary>
    public void OnAttackEnd()
    {
        attackEnded = true; // ← フラグONでPerformActionが再開
        Debug.Log("OnAttackEnd");
    }
}

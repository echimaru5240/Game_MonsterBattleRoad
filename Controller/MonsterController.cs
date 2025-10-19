using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonsterController : MonoBehaviour
{
    private Animator animator;
    private CameraManager cameraManager;

    public MonsterCard cardData;
    public bool isEnemy;

    private Skill currentSkill;
    private List<MonsterController> currentTargets = new();

    // ? 元の位置を保存しておく
    private Vector3 initialPosition;

    // 攻撃完了を待つフラグ
    private bool attackEnded = false;

    /// <summary>
    /// 初期化
    /// </summary>
    public void Init(CameraManager camMgr, bool isEnemy, MonsterCard card)
    {
        animator = GetComponent<Animator>();
        cameraManager = camMgr;
        this.isEnemy = isEnemy;
        cardData = card;

        // 生成時の位置を記録
        initialPosition = transform.position;
    }

    public IEnumerator PerformAttack(List<MonsterController> targets, Skill skill)
    {
        currentSkill = skill;
        currentTargets = targets;
        attackEnded = false; // 攻撃開始時にリセット

        Quaternion startRot = transform.rotation;

        // ① 正面ショット
        cameraManager?.SwitchToFrontCamera(transform, isEnemy);

        yield return new WaitForSeconds(1.5f); // 少し見せる

        // ② 攻撃対象へ移動
        // 単体攻撃時は対象に移動
        if (targets.Count == 1)
        {
            var target = targets[0];
            Vector3 start = transform.position;
            Vector3 end = target.transform.position + (isEnemy ? Vector3.forward : Vector3.back) * 1.2f; // 少し手前に
            float t = 0;

            // ? ターゲット方向を向く
            Vector3 dir = (end - start).normalized;
            dir.y = 0;
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = lookRot;
            cameraManager.SwitchToActionCamera(target.transform, isEnemy, transform);
            if (animator != null)
            {
                animator.SetBool("IsMove", true);
            }

            while (t < 1f)
            {
                t += Time.deltaTime * 0.5f; // 移動速度
                transform.position = Vector3.Lerp(start, end, t);

                // 追従カメラ
                // cameraManager?.SwitchToActionCamera(transform, isEnemy);

                yield return null;
            }

            if (animator != null)
            {
                animator.SetBool("IsMove", false);
            }
        }

        transform.rotation = startRot;
        // ③ 攻撃モーション
        PlayAttack();
        // 攻撃アニメ中の OnAttackHit() で onHitCallback が呼ばれる
        // ? 攻撃モーションが終わるまで待機
        while (!attackEnded)
            yield return null;
        yield return new WaitForSeconds(1f); // アニメ時間に合わせる

        // ④ 元の位置へ戻す
        if (targets.Count == 1)
        {
            Vector3 start = transform.position;
            Vector3 end = initialPosition; // 攻撃前の座標に戻す
            transform.position = initialPosition;
        }

        // 戻ったら全体カメラへ
        cameraManager?.SwitchToOverviewCamera();


        // ⑤ 次の行動まで少し間を置く（余韻タイム）
        yield return null;//new WaitForSeconds(1.0f); // アニメ時間に合わせる
    }

    /// <summary>
    /// 攻撃アニメーション＋カメラ演出
    /// </summary>
    public void PlayAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger("DoAttack");

            // 攻撃時カメラ演出
            // if (cameraManager != null)
                // cameraManager.SwitchToActionCamera(transform, isEnemy);
                // StartCoroutine(cameraManager.MoveCameraAroundAttacker(transform, 3f));

                // cameraManager?.PlayAttackCamera(transform, isEnemy, 1.8f);
        }
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
            // if (cameraManager != null)
            //     cameraManager?.PlayHitReactionCamera(transform, isEnemy, 1.2f);
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
        yield return new WaitForSeconds(1f);
        transform.position = start;
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
    /// 戦闘不能
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

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

        yield return new WaitForSeconds(1.0f); // 少し見せる

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

            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f; // 移動速度
                transform.position = Vector3.Lerp(start, end, t);

                // 追従カメラ
                // cameraManager?.SwitchToActionCamera(transform, isEnemy);

                yield return null;
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
    public void PlayHit()
    {
        if (animator != null)
        {
            animator.SetTrigger("DoHit");

            // 被弾時の寄りカメラ
            // if (cameraManager != null)
            //     cameraManager?.PlayHitReactionCamera(transform, isEnemy, 1.2f);
        }
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
    /// 攻撃が当たる瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttackHit()
    {
        BattleCalculator.OnAttackHit(this, currentSkill, currentTargets);
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
}

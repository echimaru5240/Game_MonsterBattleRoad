using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class MonsterAction_MushRoom : MonsterActionBase
{
    [Header("演出設定")]
    public float moveDuration = 0.5f;    // 移動速度
    public float jumpHeight = 8f;
    public float jumpDuration = 0.8f;
    public float spinDuration = 3f;
    public float diveDuration = 0.4f;   // 急降下時間
    public float slideDistance = 1.2f;
    public float slideDuration = 0.3f;

    [Header("モーションSE")]
    public AudioClip moveSE;
    public AudioClip attackSE;
    public AudioClip jumpSE;
    public AudioClip needleSE;

    [Header("エフェクト")]
    public EffectID crashEffect;
    public EffectID healEffect;
    public EffectID paralysisEffect;

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
            case SkillID.SKILL_ID_MUSH_CRUSHER:
                yield return StartCoroutine(Execute_MushCrasher());
                break;
            /* サボテンジュース */
            case SkillID.SKILL_ID_MUSH_POWDER:
                yield return StartCoroutine(Execute_MushPowder());
                break;
            default:
                Debug.LogWarning($"{selfController.name} のスキル「{skill.skillName}」は未実装です。");
                yield return StartCoroutine(Execute_MushPowder());
                break;
                // yield break;
        }
    }

    private IEnumerator Execute_MushCrasher()
    {
        anim = selfController.GetComponent<Animator>();
        MonsterController target = currentActionResults[0].Target;

        Vector3 start = selfController.transform.position;
        Quaternion startRot = selfController.transform.rotation;

        Vector3 targetPos = target.transform.position;

        anim.SetTrigger("DoJump"); // あれば

        Sequence seq = DOTween.Sequence();
        seq.AppendCallback(() => {
            // CameraManager.Instance.SwitchToFixedBackCamera(selfController.transform, selfController.isPlayer);

            Vector3 fixedPos = new Vector3(-4f, 2f, selfController.isPlayer ? 16f : -16f); // ここは好きな位置
            CameraManager.Instance.CutAction_FixedWorldLookOnly(
                fixedPos,
                selfController.transform,
                lookAtHeight: 1.2f,
                fov: 45f
            );
        });

        // 攻撃の余韻時間（砂煙などを出すならここ）
        seq.AppendInterval(0.4f);

        Vector3 jumpApex = Vector3.zero + Vector3.up * jumpHeight;
         seq.AppendCallback(() => {
                anim.SetTrigger("DoJump");
            });

        // 上昇
        seq.Append(selfController.transform.DOMove(jumpApex, jumpDuration)
            .SetEase(Ease.OutCirc));
        seq.Join(selfController.transform.DORotate(new Vector3(180f, 0, 0), jumpDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutQuad));

        // 落下開始と同時にタイミングタップ開始し、結果が出るまで進行を止める
        seq.AppendCallback(() => {
            selfController.OnStartTimingTap();
            });
        // 落下
        seq.Append(selfController.transform.DOMove(targetPos, jumpDuration*0.8f)
            .SetEase(Ease.InCirc));


        seq.AppendCallback(() => {
            // 攻撃エフェクトを呼び出す
            Vector3 effectPos = targetPos + Vector3.up * 1f;
            EffectManager.Instance.PlayEffectByID(crashEffect, effectPos);
            Debug.Log("OnAttackHit");
            selfController.OnAttackLastHit();
            });
        // 攻撃の余韻時間（砂煙などを出すならここ）
        seq.AppendInterval(0.4f);

        // =============================
        // ? 攻撃終了
        // =============================
        seq.OnComplete(() => {
            selfController.OnAttackEnd();
        });

        // シーケンス終了を待機
        yield return seq.WaitForCompletion();
    }

    /// <summary>
    /// 動く瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnMove_Skill1()
    {
        if (attackSE != null) AudioManager.Instance.PlaySE(moveSE);
        Debug.Log("OnMove");
    }

    /// <summary>
    /// ジャンプする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnJump()
    {
        if (jumpSE != null) AudioManager.Instance.PlaySE(jumpSE);
        Debug.Log("OnJump");
    }

    /// <summary>
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttack_Skill1()
    {
        if (attackSE != null) AudioManager.Instance.PlaySE(attackSE);
        Debug.Log("OnAttack");
    }


    public IEnumerator Execute_MushPowder()
    {
        anim = selfController.GetComponent<Animator>();

        Vector3 worldPos = new Vector3(-0.2f, 2f, selfController.isPlayer ? -13f : 13f); // ここは好きな位置
        CameraManager.Instance.CutAction_FixedWorldLookOnly(worldPos, selfController.transform);
        // 攻撃
        anim.SetTrigger("DoAttack");
        // 攻撃エフェクトを呼び出す
        Vector3 effectPos = selfController.transform.position + Vector3.up * 1.0f + Vector3.forward * (selfController.isPlayer ? 0.5f : -0.5f);
        float rot = selfController.isPlayer ? 0f : 180f;
        EffectManager.Instance.PlayEffectByID(paralysisEffect, effectPos, Quaternion.Euler(0f, rot, 180f), 3f);
        yield return new WaitForSeconds(1f);
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

    /// <summary>
    /// 動く瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnMove()
    {
        AudioManager.Instance.PlaySE(moveSE);
        Debug.Log("OnMove");
    }
}

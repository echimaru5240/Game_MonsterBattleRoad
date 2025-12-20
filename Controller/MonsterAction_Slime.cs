using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class MonsterAction_Slime : MonsterActionBase
{
    [Header("スライム演出設定")]
    public float moveDuration = 0.3f;    // 1回のジグザグ時間
    public float zigzagAmplitude = 1.2f; // ジグザグ幅
    public float jumpHeight = 2.5f;
    public float jumpDuration = 0.4f;
    public float diveDuration = 0.25f;   // 急降下時間
    public float slideDistance = 1.2f;
    public float slideDuration = 0.3f;

    [Header("モーションSE")]
    public AudioClip moveSE;
    public AudioClip attackSE;
    public AudioClip jumpSE;

    [Header("エフェクト")]
    public EffectID skill1Effect;

    private MonsterController selfController;
    private List<BattleCalculator.ActionResult> currentActionResults = new();

    public override IEnumerator Execute(MonsterController self, List<BattleCalculator.ActionResult> results, SkillData skill)
    {
        selfController = self;
        currentActionResults = results;

        switch (skill.skillID)
        {
            /* プルプルアタック */
            case SkillID.SKILL_ID_SLIME_STRIKE:
                yield return StartCoroutine(Execute_Skill1());
                break;
            /* マジックショット */
            case SkillID.SKILL_ID_FIRE_BURST:
                yield return StartCoroutine(Execute_Skill2());
                break;
            default:
                Debug.LogWarning($"{selfController.name} のスキル「{skill.skillName}」は未実装です。");
                yield return StartCoroutine(Execute_Skill2());
                break;
                // yield break;
        }
    }

    private IEnumerator Execute_Skill1()
    {
        var anim = selfController.GetComponent<Animator>();
        Vector3 startPos = selfController.transform.position;
        Vector3 centerPos = Vector3.zero;
        MonsterController target = currentActionResults[0].Target;

        Sequence seq = DOTween.Sequence();

        // =============================
        // ? 3回ジグザグ
        // =============================
        // 例：Back（あなたのスクショ相当）
        // PlayerBack: (0,2,-10) / EnemyBack: (0,2,10)
        Vector3 backOffset = selfController.isPlayer
            ? new Vector3(0f, 2f, -10f)
            : new Vector3(0f, 2f,  10f);

        // CameraManager.Instance.CutAction_FollowOffsetWorld(selfController.transform, backOffset, fov: 45f);

        Vector3 offset = new Vector3(0f, 2f, selfController.isPlayer ? -10f : 10f); // ここは好きな位置
        CameraManager.Instance.CutAction_Follow(selfController.transform, offset);
        // CameraManager.Instance.SwitchToActionCameraBack(selfController.transform, selfController.isPlayer);
        for (int i = 0; i < 3; i++)
        {
            Vector3 dir = new Vector3((i % 2 == 0 ? 1 : -1) * zigzagAmplitude, 0, 0);
            Vector3 targetPos = Vector3.Lerp(startPos, centerPos, (i + 1) / 3f) + dir;

            seq.AppendCallback(() => {
                anim.SetTrigger("DoMove");
            });

            seq.Append(selfController.transform.DOMove(targetPos, moveDuration)
                .SetEase(Ease.OutSine));
        }

        seq.AppendCallback(() => {
            Vector3 fixedPos = new Vector3(-3f, 0.5f, selfController.isPlayer ? 18f : -18f); // ここは好きな位置
            CameraManager.Instance.CutAction_FixedWorldLookOnly(
                fixedPos,
                selfController.transform,
                lookAtHeight: 1.2f,
                fov: 45f
            );
            // CameraManager.Instance.SwitchToFixedBackCamera(selfController.transform, selfController.isPlayer);
        });

        // =============================
        // ? ジャンプ（上昇→ターゲット落下）
        // =============================
        Vector3 jumpApex = selfController.transform.position
                        + (selfController.isPlayer ? Vector3.forward : Vector3.back) * 3
                        + Vector3.up * jumpHeight;

         seq.AppendCallback(() => {
                anim.SetTrigger("DoJump");
            });

        // 上昇
        seq.Append(selfController.transform.DOMove(jumpApex, jumpDuration)
            .SetEase(Ease.OutQuad));

        // 落下（ターゲット位置に着地）
        seq.Append(selfController.transform.DOMove(target.transform.position, diveDuration)
            .SetEase(Ease.InCubic)
            .OnStart(() => {
                anim.SetTrigger("DoAttack");
            })
        );

        // 攻撃の余韻時間（砂煙などを出すならここ）
        seq.AppendInterval(0.1f);

        // =============================
        // ? 滑り停止
        // =============================
        // ターゲット方向のZ軸基準で滑る
        Vector3 slideTarget = target.transform.position +
            new Vector3(0, 0, selfController.isPlayer ? +slideDistance : -slideDistance);

        // ドリフトカーブ（Ease.OutCubic）で減速しながら滑る
        seq.Append(selfController.transform.DOMove(slideTarget, slideDuration)
            .SetEase(Ease.OutCubic));

        // 滑りながらちょっと回転（ドリフト感）
        seq.Join(selfController.transform.DORotate(new Vector3(0, selfController.isPlayer ? 20f : -20f, 0), slideDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutQuad));

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
        if (moveSE != null) AudioManager.Instance.PlaySE(moveSE);
        // Debug.Log("OnMove");
    }

    /// <summary>
    /// 攻撃をする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttack_Skill1()
    {
        if (attackSE != null) AudioManager.Instance.PlaySE(attackSE);
        // Debug.Log("OnAttack");
    }

    /// <summary>
    /// ジャンプをする瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnJump_Skill1()
    {
        if (jumpSE != null) AudioManager.Instance.PlaySE(jumpSE);
        // Debug.Log("OnJump");
    }


    /// <summary>
    /// 攻撃が当たる瞬間（アニメーションイベントで呼ばれる）
    /// </summary>
    public void OnAttackHit_Skill1()
    {
        // 攻撃エフェクトを呼び出す
        Vector3 effectPos = currentActionResults[0].Target.transform.position + Vector3.up * 1f;
        EffectManager.Instance.PlayEffectByID(skill1Effect, effectPos, Quaternion.Euler(-90f, 0, 0));
        // Debug.Log("OnAttackHit");
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
}

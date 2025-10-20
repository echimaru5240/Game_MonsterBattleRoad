using UnityEngine;
using System.Collections;
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

    public AudioClip moveSE;
    public AudioClip jumpSE;
    public AudioClip attackSE;

    // ? 攻撃ヒット時のパーティクルプレハブ

    [Header("攻撃エフェクトタイプ")]
    public AttackEffectType attackEffectType = AttackEffectType.ExplosionSmall;

    public override IEnumerator Execute(MonsterController self, MonsterController target, Skill skill)
    {
        var anim = self.GetComponent<Animator>();
        Vector3 startPos = self.transform.position;
        Vector3 centerPos = Vector3.zero;

        Sequence seq = DOTween.Sequence();

        // =============================
        // ? 3回ジグザグ
        // =============================
        for (int i = 0; i < 3; i++)
        {
            Vector3 dir = new Vector3((i % 2 == 0 ? 1 : -1) * zigzagAmplitude, 0, 0);
            Vector3 targetPos = Vector3.Lerp(startPos, centerPos, (i + 1) / 3f) + dir;

            seq.AppendCallback(() => {
                anim.SetTrigger("DoMove");
                if (moveSE) AudioManager.Instance.PlaySE(moveSE);
            });

            seq.Append(self.transform.DOMove(targetPos, moveDuration)
                .SetEase(Ease.OutSine));
        }

        // =============================
        // ? ジャンプ（上昇→ターゲット落下）
        // =============================
        seq.AppendCallback(() => {
            if (jumpSE) AudioManager.Instance.PlaySE(jumpSE);
        });

        Vector3 jumpApex = self.transform.position
                        + (self.isPlayer ? Vector3.forward : Vector3.back) * 3
                        + Vector3.up * jumpHeight;

        // 上昇
        seq.Append(self.transform.DOMove(jumpApex, jumpDuration)
            .SetEase(Ease.OutQuad));

        // 落下（ターゲット位置に着地）
        seq.Append(self.transform.DOMove(target.transform.position, diveDuration)
            .SetEase(Ease.InCubic)
            .OnStart(() => {
                anim.SetTrigger("DoAttack");
                // if (attackSE) AudioManager.Instance.PlaySE(attackSE);
            })
        );

        // 攻撃の余韻時間（砂煙などを出すならここ）
        seq.AppendInterval(0.1f);

        // =============================
        // ? 滑り停止
        // =============================
        // ターゲット方向のZ軸基準で滑る
        Vector3 slideTarget = target.transform.position +
            new Vector3(0, 0, self.isPlayer ? +slideDistance : -slideDistance);

        // ドリフトカーブ（Ease.OutCubic）で減速しながら滑る
        seq.Append(self.transform.DOMove(slideTarget, slideDuration)
            .SetEase(Ease.OutCubic));

        // 滑りながらちょっと回転（ドリフト感）
        seq.Join(self.transform.DORotate(new Vector3(0, self.isPlayer ? 20f : -20f, 0), slideDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutQuad));

        // =============================
        // ? 攻撃終了
        // =============================
        seq.OnComplete(() => {
            self.OnAttackEnd();
        });

        // シーケンス終了を待機
        yield return seq.WaitForCompletion();
    }
}

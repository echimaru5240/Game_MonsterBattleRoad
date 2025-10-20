using UnityEngine;
using System.Collections;

public class MonsterAction_Common : MonsterActionBase
{
    public float moveSpeed = 0.5f;

    public override IEnumerator Execute(MonsterController self, MonsterController target, Skill skill)
    {
        var anim = self.GetComponent<Animator>();

        // �O�i
        anim.SetBool("IsMove", true);
        yield return MoveToTarget(self, target, moveSpeed);
        anim.SetBool("IsMove", false);

        // �U��
        anim.SetTrigger("DoAttack");

        self.OnAttackEnd();
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonsterAction_Common : MonsterActionBase
{
    public float moveSpeed = 0.5f;

    public override IEnumerator Execute(MonsterController self, List<MonsterController> targets, Skill skill)
    {
        var anim = self.GetComponent<Animator>();

        // �O�i
        anim.SetBool("IsMove", true);
        yield return MoveToTarget(self, targets[0], moveSpeed);
        anim.SetBool("IsMove", false);

        // �U��
        anim.SetTrigger("DoAttack");

        self.OnAttackEnd();
    }
}

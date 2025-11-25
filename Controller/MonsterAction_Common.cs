using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonsterAction_Common : MonsterActionBase
{
    public float moveSpeed = 0.5f;

    public override IEnumerator Execute(MonsterController self, List<MonsterController> targets, SkillData skill)
    {
        var anim = self.GetComponent<Animator>();

        // ëOêi
        anim.SetBool("IsMove", true);
        yield return MoveToTarget(self, targets[0], moveSpeed);
        anim.SetBool("IsMove", false);

        // çUåÇ
        anim.SetTrigger("DoAttack");

        self.OnAttackEnd();
    }
}

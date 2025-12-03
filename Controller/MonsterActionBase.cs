using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class MonsterActionBase : MonoBehaviour
{
    public abstract IEnumerator Execute(MonsterController self, List<BattleCalculator.ActionResult> results, SkillData skill);

    // 共通ヘルパー
    protected IEnumerator MoveToTarget(MonsterController self, MonsterController target, float agi = 0.5f, float stopOffset = 1.2f)
    {
        Vector3 start = self.transform.position;
        Vector3 end = target.transform.position + (self.isPlayer ? Vector3.back : Vector3.forward) * stopOffset;
        Quaternion startRot = self.transform.rotation;
        Debug.Log($"プレイヤー？：{self.isPlayer}, スタート：{start}, エンド：{end}");

        // ? ターゲット方向を向く
        Vector3 dir = (end - start).normalized;
        dir.y = 0;
        Quaternion lookRot = Quaternion.LookRotation(dir);
        self.transform.rotation = lookRot;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * agi;
            self.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        self.transform.rotation = startRot;
    }

    protected IEnumerator Jump(MonsterController self, float height, float duration)
    {
        Vector3 start = self.transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float y = Mathf.Sin(t * Mathf.PI) * height;
            self.transform.position = new Vector3(start.x, start.y + y, start.z);
            yield return null;
        }
    }
}

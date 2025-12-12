using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class MonsterDetailDataSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private SkillDataView[] skillDataView;

    private OwnedMonster monster;
    private GameObject monsterObj;
    private MonsterController monsterController;

    public void Setup(OwnedMonster monster)
    {
        if (monster == null)
        {
            Debug.LogWarning("MonsterDetailDataSlot.Setup: monster is null");
            return;
        }

        this.monster = monster;
        nameText.text = monster.Name;

        // スキル表示（とりあえず先頭2つ）
        for (int i = 0; i < skillDataView.Length && i < monster.skills.Length; i++)
        {
            skillDataView[i].Setup(SkillDatabase.Get(monster.skills[i]));
        }
    }
}

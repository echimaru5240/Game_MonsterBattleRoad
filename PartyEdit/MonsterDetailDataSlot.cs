using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class MonsterDetailDataSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private SkillDataView[] skillDataView;

    private OwnedMonster monster;
    private GameObject monsterObj;
    private MonsterController monsterController;

    public void Setup(OwnedMonster monster, Transform position, float spacing)
    {
        if (monster == null) {
            Debug.LogWarning("Show card null");
            return;
        }
        this.monster = monster;
        nameText.text = monster.Name.ToString();

        // ê∂ê¨
        monsterObj = Instantiate(monster.prefab, position);
        monsterObj.transform.localPosition = new Vector3(spacing, 0, 0);
        monsterController = monsterObj.GetComponent<MonsterController>();

        for (int i = 0; i < 2; i++)
        {
            skillDataView[i].Setup(SkillDatabase.Get(monster.skills[i]));
        }
    }

    public void OnMonsterTap()
    {

        Animator animator = monsterController.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("DoLastHit");
            Debug.Log($"{monster.Name}  DoLastHit");
        }
        else{
            Debug.Log("NULL");
        }
    }

    public void DestroyObj()
    {
        Destroy(monsterObj);
    }
}
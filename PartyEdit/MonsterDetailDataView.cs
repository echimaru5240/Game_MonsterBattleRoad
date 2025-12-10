using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class MonsterDetailDataView : MonoBehaviour
{
    [SerializeField] private GameObject root;          // MonsterDetailPanel 自身でもOK
    [SerializeField] private Transform monsterPosition;

    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI atkText;
    [SerializeField] private TextMeshProUGUI mgcText;
    [SerializeField] private TextMeshProUGUI defText;
    [SerializeField] private TextMeshProUGUI agiText;

    [SerializeField] private TextMeshProUGUI indexText;
    [SerializeField] private MonsterDetailDataSlot[] monsterDetailDataSlot;

    [SerializeField] private float spacing = 6f;

    public void Setup()
    {
        root.SetActive(false);
    }

    public void Show(OwnedMonster monster, int index, int total, OwnedMonster prevMonster, OwnedMonster nextMonster)
    {
        DestroyObj();

        if (monster == null) {
            Debug.LogWarning("Show card null");
            return;
        }
        hpText.text = monster.hp.ToString();
        atkText.text = monster.atk.ToString();
        mgcText.text = monster.mgc.ToString();
        defText.text = monster.def.ToString();
        agiText.text = monster.agi.ToString();

        indexText.text = $"{index+1}/{total}";

        monsterDetailDataSlot[0].Setup(prevMonster, monsterPosition, -6f);
        monsterDetailDataSlot[1].Setup(monster, monsterPosition, 0f);
        monsterDetailDataSlot[2].Setup(nextMonster, monsterPosition, 6f);

        root.SetActive(true);
    }

    public void OnMonsterTap()
    {
        Debug.Log("On Tap!");
        monsterDetailDataSlot[1].OnMonsterTap();
    }

    public void DestroyObj()
    {
        foreach (var slot in monsterDetailDataSlot)
        {
            slot.DestroyObj();
        }
    }

    public void Hide()
    {
        Debug.Log("Hide");
        DestroyObj();
        root.SetActive(false);
    }

    // 背景をタップしたとき用
    public void OnClickCloseButton()
    {
        Debug.Log("OnClickCloseButton");
        Hide();
    }
}
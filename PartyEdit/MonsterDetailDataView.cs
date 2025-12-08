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
    [SerializeField] private MonsterCardView bigCard;  // 大きめカード用
    [SerializeField] private SkillDataView[] skillDataView;

    private void Awake()
    {
        root.SetActive(false);
    }

    public void Show(MonsterCardView card)
    {
        if (card == null) return;

        root.SetActive(true);

        // カード部分に同じ情報を流し込む（クリック・長押しは不要なので null）
        bigCard.SetupDetailDataView(card);
        for (int i = 0; i < 2; i++)
        {
            skillDataView[i].Setup(SkillDatabase.Get(card.GetOwnedMonsterData().skills[i]));
        }
    }

    public void Hide()
    {
        root.SetActive(false);
    }

    // 背景をタップしたとき用
    public void OnClickCloseButton()
    {
        Debug.Log("OnClickCloseButton");
        Hide();
    }
}
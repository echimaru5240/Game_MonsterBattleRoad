using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class SkillDataView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image[] categoryIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private Image[] targetIcon;
    [SerializeField] private Image[] elementIcon;

    public void Setup(SkillData skillData)
    {
        nameText.text = skillData.skillName;
        powerText.text = skillData.power.ToString();
        for (int i = 0; i < categoryIcon.Length; i++)
        {
            categoryIcon[i].gameObject.SetActive(i == (int)skillData.category);
        }
        for (int i = 0; i < targetIcon.Length; i++)
        {
            targetIcon[i].gameObject.SetActive(i == (int)skillData.targetType);
        }
    }

}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BattleUISkillButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI skillName;
    [SerializeField] private GameObject selectedFrame;

    [Header("Colors")]
    [SerializeField] private Color enemyColor;
    [SerializeField] private Color playerColor;

    private int userIndex;
    private int skillIndex;
    private Color baseColor;

    public event Action<int, int, BattleUISkillButton> OnPressed;

    public void Setup(int userIndex, int skillIndex, SkillData skill)
    {
        this.userIndex = userIndex;
        this.skillIndex = skillIndex;

        if (button == null) button = GetComponent<Button>();
        if (buttonImage == null) buttonImage = GetComponent<Image>();

        skillName.text = skill.skillName;

        switch (skill.targetType)
        {
            case TargetType.ENEMY_SINGLE:
            case TargetType.ENEMY_ALL:
                buttonImage.color = enemyColor;
                break;
            case TargetType.PLAYER_SINGLE:
            case TargetType.PLAYER_ALL:
                buttonImage.color = playerColor;
                break;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            Debug.Log($"[SkillButton Click] user={userIndex} skill={skillIndex} name={skillName.text}");
            OnPressed?.Invoke(this.userIndex, this.skillIndex, this);
        });
        // êFê›íËå„Ç…ï€éù
        baseColor = buttonImage.color;

    }

    public void ClearListeners()
    {
        OnPressed = null;
        if (button != null) button.onClick.RemoveAllListeners();
    }

    public void SetSelected(bool on)
    {
        if (selectedFrame != null) selectedFrame.SetActive(on);
    }

    public void SetInteractable(bool on)
    {
        if (button != null) button.interactable = on;
    }

    public void SetVisible(bool on)
    {
        gameObject.SetActive(on);
    }

    public void SetDimmed(bool dim)
    {
        if (buttonImage == null) return;
        buttonImage.color = dim ? baseColor * 0.4f : baseColor;
    }
}

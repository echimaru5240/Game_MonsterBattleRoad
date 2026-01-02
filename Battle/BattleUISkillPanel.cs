using UnityEngine;
using UnityEngine.UI;
using System;

public class BattleUISkillPanel : MonoBehaviour
{
    [SerializeField] private BattleUISkillButton[] skillButtons;
    [SerializeField] private Image monsterImage;
    [SerializeField] private GameObject unableActObj;

    public event Action<int, int> OnSkillSelected; // userIndex, skillIndex

    private int userIndex;
    private int selectedSkillIndex = -1;

    public void Setup(int userIndex, MonsterBattleData monster)
    {
        bool canAct = monster.statusAilmentType == StatusAilmentType.NONE;
        this.userIndex = userIndex;
        selectedSkillIndex = -1;

        monsterImage.sprite = monster.monsterNearSprite;

        for (int i = 0; i < skillButtons.Length; i++)
        {
            bool exists = i < monster.skills.Length;
            skillButtons[i].gameObject.SetActive(exists);

            if (!exists) continue;

            SkillID skillId = monster.skills[i];
            SkillData skill = SkillDatabase.Get(skillId);

            var btn = skillButtons[i];

            btn.ClearListeners();
            btn.Setup(userIndex, i, skill);
            btn.OnPressed += HandlePressed;

            // 初期状態
            btn.SetSelected(false);
            btn.SetVisible(true);

            // ★ 行動不能なら押せない＆暗く
            btn.SetInteractable(canAct);
            btn.SetDimmed(!canAct);
        }
        unableActObj.SetActive(!canAct);
    }

    private void HandlePressed(int u, int skillIndex, BattleUISkillButton pressed)
    {
        Debug.Log($"[Panel Received] user={u} skill={skillIndex} pressed={pressed.name}");

        selectedSkillIndex = skillIndex;

        pressed.SetSelected(true);
        pressed.SetInteractable(false);
        pressed.SetDimmed(false); // 念のため

        // 他ボタンは非表示＆操作不可（旧挙動）
        foreach (var b in skillButtons)
        {
            if (b == null || !b.gameObject.activeSelf) continue;
            if (b == pressed) continue;

            b.SetInteractable(false);
            b.SetDimmed(true);     // ★ 暗くする
        }

        OnSkillSelected?.Invoke(u, skillIndex);
    }

    // ===== 外部操作API =====

    public void ResetButtons()
    {
        selectedSkillIndex = -1;

        foreach (var b in skillButtons)
        {
            if (b == null) continue;
            if (!b.gameObject.activeSelf) continue;

            b.SetSelected(false);
            b.SetInteractable(true);
            b.SetDimmed(false); // ★ 色を元に戻す
        }
        unableActObj.SetActive(false);
    }

    public void DisableButtons()
    {
        foreach (var b in skillButtons)
        {
            if (b == null) continue;
            if (!b.gameObject.activeSelf) continue;
            b.SetInteractable(false);
        }
    }

    public void SetSkillButtonFrameActive(int skillIndex, bool active)
    {
        if (skillIndex < 0 || skillIndex >= skillButtons.Length) return;
        var b = skillButtons[skillIndex];
        if (b == null) return;
        b.SetSelected(active);
    }

    public void SetCanAct(bool canAct)
    {
        if (canAct)
        {
            ResetButtons(); // 既存の「選択解除＆押せる＆明るい」に戻す
        }
        else
        {
            // 押せない＆暗く
            foreach (var b in skillButtons)
            {
                if (b == null) continue;
                if (!b.gameObject.activeSelf) continue;
                b.SetInteractable(false);
                b.SetDimmed(true);
                b.SetSelected(false);
            }
            unableActObj.SetActive(true);
        }
    }

    public BattleUISkillButton GetButton(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skillButtons.Length) return null;
        return skillButtons[skillIndex];
    }
}

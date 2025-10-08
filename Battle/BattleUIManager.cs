using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class BattleUIManager : MonoBehaviour
{
    [Header("HP UI")]
    public Slider playerHPBar;
    public Slider enemyHPBar;
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI enemyHPText;

    [Header("�e�L�X�g�Ǘ�")]
    public BattleTextManager textManager;

    [Header("Skill Buttons")]
    public GameObject skillButtonPrefab;
    public Transform skillPanel;

    [Header("Names")]
    public GameObject playerNamePrefab;
    public Transform namePanel;

    [Header("Damage Popup")]
    public GameObject damageTextPrefab;
    public Transform canvasTransform;

    [Header("�E�C�Q�[�W UI")]
    public Slider courageBar;

    // �R�[���o�b�N
    public Action<int, int> OnSkillSelected;

    // �����Ǘ�
    private Dictionary<int, List<Button>> buttonsByUser = new Dictionary<int, List<Button>>();
    private List<TextMeshProUGUI> playerNameLabels = new List<TextMeshProUGUI>();

    private readonly Color selectedBtnColor = new Color(1f, 111f / 255f, 0f, 1f);
    private readonly Color disabledBtnColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    // ================================
    // ������
    // ================================
    public void Init(MonsterCard[] playerCards, int playerHP, int playerMaxHP, int enemyHP, int enemyMaxHP, int courageMax = 100)
    {
        // HP�o�[������
        playerHPBar.maxValue = playerMaxHP;
        playerHPBar.value = playerHP;
        enemyHPBar.maxValue = enemyMaxHP;
        enemyHPBar.value = enemyHP;

        UpdateHP(playerHP, playerMaxHP, enemyHP, enemyMaxHP);

        // �E�C�Q�[�W������
        if (courageBar != null)
        {
            courageBar.maxValue = courageMax;
            courageBar.value = 0;
        }

        GenerateSkillButtons(playerCards);
        GeneratePlayerNames(playerCards);
    }

    // ================================
    // HP�X�V
    // ================================
    public void UpdateHP(int playerHP, int playerMax, int enemyHP, int enemyMax)
    {
        playerHPBar.value = playerHP;
        enemyHPBar.value = enemyHP;
        playerHPText.text = $"{playerHP} / {playerMax}";
        enemyHPText.text = $"{enemyHP} / {enemyMax}";
    }

    // ================================
    // �E�C�Q�[�W�X�V
    // ================================
    public void UpdateCourage(int current, int max)
    {
        if (courageBar == null) return;
        courageBar.maxValue = max;
        courageBar.value = current;
    }

    // ================================
    // �e�L�X�g�֘A���b�p�[
    // ================================

    public void ShowTurnText(int turn)
    {
        textManager.ShowTurnText(turn);
    }

    public void HideTurnText()
    {
        textManager.HideTurnText();
    }

    public void ShowAttackText(bool isPlayerSide, string monsterName, string skillName)
    {
        if (isPlayerSide) {
            textManager.ShowPlayerAttackText(monsterName, skillName);
        }
        else {
            textManager.ShowEnemyAttackText(monsterName, skillName);
        }
    }

    public void HideAttackText(bool isPlayerSide)
    {
        if (isPlayerSide) {
            textManager.HidePlayerAttackText();
        }
        else {
            textManager.HideEnemyAttackText();
        }
    }

    public void ShowMainText(string message)
    {
        textManager.ShowMainText(message);
    }

    public void ShowBattleResultText(bool playerWon)
    {
        string msg = playerWon ? "�����I" : "�s�k�c";
        textManager.ShowMainText(msg, 2.5f);
    }

    // ================================
    // �X�L���{�^������
    // ================================
    private void GenerateSkillButtons(MonsterCard[] playerCards)
    {
        foreach (Transform child in skillPanel) Destroy(child.gameObject);
        buttonsByUser.Clear();

        for (int userIndex = 0; userIndex < playerCards.Length; userIndex++)
        {
            buttonsByUser[userIndex] = new List<Button>();

            for (int skillIndex = 0; skillIndex < playerCards[userIndex].skills.Length; skillIndex++)
            {
                var skill = playerCards[userIndex].skills[skillIndex];
                GameObject buttonObj = Instantiate(skillButtonPrefab, skillPanel);
                buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = skill.skillName;

                Button btn = buttonObj.GetComponent<Button>();
                buttonsByUser[userIndex].Add(btn);

                int u = userIndex;
                int s = skillIndex;
                btn.onClick.AddListener(() =>
                {
                    SelectSkillButton(u, s, btn);
                });
            }
        }
    }

    private void SelectSkillButton(int userIndex, int skillIndex, Button pressed)
    {
        SetButtonImmediateColor(pressed, selectedBtnColor);
        pressed.interactable = false;

        foreach (var b in buttonsByUser[userIndex])
        {
            if (b == pressed) continue;
            SetButtonImmediateColor(b, disabledBtnColor);
            b.interactable = false;
        }

        OnSkillSelected?.Invoke(userIndex, skillIndex);
    }

    // ================================
    // �{�^���̗L�����E������
    // ================================
    public void ResetButtons()
    {
        foreach (var kv in buttonsByUser)
        {
            foreach (var btn in kv.Value)
            {
                if (!btn) continue;
                var graphic = btn.targetGraphic as Graphic;
                if (graphic) graphic.color = Color.white;
                btn.interactable = true;
                // btn.gameObject.SetActive(true); // �ĕ\��
            }
        }
    }

    public void DisableButtons()
    {
        foreach (var kv in buttonsByUser)
        {
            foreach (var btn in kv.Value)
            {
                if (!btn) continue;
                btn.interactable = false;
            }
        }
    }

    public void SetButtonsActive(bool active)
    {
        skillPanel.gameObject.SetActive(active);
        namePanel.gameObject.SetActive(active);
    }

    // ================================
    // �v���C���[�����x��
    // ================================
    private void GeneratePlayerNames(MonsterCard[] playerCards)
    {
        foreach (Transform child in namePanel) Destroy(child.gameObject);
        playerNameLabels.Clear();

        for (int i = 0; i < playerCards.Length; i++)
        {
            GameObject go = Instantiate(playerNamePrefab, namePanel);
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            label.text = playerCards[i].cardName;
            playerNameLabels.Add(label);
        }
    }

    public void FlashName(int index, float duration = 0.4f)
    {
        if (index < 0 || index >= playerNameLabels.Count) return;
        var label = playerNameLabels[index];
        StartCoroutine(FlashRoutine(label, duration));
    }

    private System.Collections.IEnumerator FlashRoutine(TextMeshProUGUI label, float duration)
    {
        var old = label.color;
        label.color = Color.yellow;
        yield return new WaitForSeconds(duration);
        label.color = old;
    }

    // ================================
    // �_���[�W�|�b�v�A�b�v
    // ================================
    public void ShowDamagePopup(int value, GameObject target, bool isHeal = false)
    {
        Vector3 worldPos = target.transform.position + Vector3.up * 2f;
        GameObject popup = Instantiate(damageTextPrefab, canvasTransform);
        popup.transform.position = Camera.main.WorldToScreenPoint(worldPos);

        var dmgText = popup.GetComponent<DamageText>().textMesh;
        dmgText.text = (isHeal ? "+" : "-") + value.ToString();
        dmgText.color = isHeal ? Color.green : Color.red;
    }

    // ================================
    // �{�^�������F�ύX
    // ================================
    private void SetButtonImmediateColor(Button btn, Color color)
    {
        var graphic = btn.targetGraphic as Graphic;
        if (graphic) graphic.color = color;
    }
}

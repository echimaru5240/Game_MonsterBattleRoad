using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class BattleUIManager : MonoBehaviour
{
    [Header("HP UI")]
    public Slider playerHPBar;
    public GameObject playerHPBarBackGround;
    public Slider enemyHPBar;
    public GameObject enemyHPBarBackGround;
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI enemyHPText;

    [Header("�e�L�X�g�Ǘ�")]
    public BattleTextManager textManager;

    [Header("Skill Buttons")]
    public Transform selectPanelParent;
    public GameObject selectPanelPrefab;

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

    private Coroutine playerHpRoutine;
    private Coroutine enemyHpRoutine;
    private int m_OldPlayerHP;
    private int m_OldEnemyHP;


    // ================================
    // ������
    // ================================
    public void Init(MonsterCard[] playerCards, int playerHP, int playerMaxHP, int enemyHP, int enemyMaxHP, int courageMax = 100)
    {
        // HP�o�[������
        m_OldPlayerHP = 0;
        m_OldEnemyHP = 0;
        UpdateHP(playerHP, playerMaxHP, enemyHP, enemyMaxHP);

        // �E�C�Q�[�W������
        if (courageBar != null)
        {
            courageBar.maxValue = courageMax;
            courageBar.value = 0;
        }

        selectPanelParent.gameObject.SetActive(false);
        GenerateSkillButtons(playerCards);
    }

    // ================================
    // HP�X�V
    // ================================
    public void UpdateHP(int playerHP, int playerMax, int enemyHP, int enemyMax)
    {
        playerHPText.text = $"{playerHP} / {playerMax}";
        enemyHPText.text = $"{enemyHP} / {enemyMax}";


        // HP�o�[�����X�ɕω�
        if (playerHpRoutine != null) StopCoroutine(playerHpRoutine);
        if (enemyHpRoutine != null) StopCoroutine(enemyHpRoutine);

        playerHpRoutine = StartCoroutine(SmoothHPChangeSegmented(playerHPBar, playerHPBarBackGround, playerHP, m_OldPlayerHP));
        enemyHpRoutine = StartCoroutine(SmoothHPChangeSegmented(enemyHPBar, enemyHPBarBackGround, enemyHP, m_OldEnemyHP));

        m_OldPlayerHP = playerHP;
        m_OldEnemyHP = enemyHP;
    }

    private IEnumerator SmoothHPChangeSegmented(Slider bar, GameObject background, int toHP, int fromHP)
    {
        if (bar == null || fromHP == toHP)
        {
            // �ŏI�F�E�w�i����������
            UpdateHPBarColor(bar, background, toHP);
            bar.maxValue = 1000;
            bar.value = HPInStageValue(toHP, true);
            yield break;
        }

        // ���A�j�����Ԃ͏�� 0.8�b
        const float totalDuration = 0.5f;
        int totalDelta = Mathf.Abs(toHP - fromHP);

        // ���݂̌����ڒl�i�r���Ŋ��荞�܂ꂽ���̘A������ۂ��߁j
        float visualStart = bar.value;
        bar.maxValue = 1000;

        // ��Ԑ����i1000���E�ŕ����j
        var segments = BuildSegments(fromHP, toHP);  // List<(int segFrom, int segTo)>

        bool first = true;
        foreach (var seg in segments)
        {
            int segFrom = seg.segFrom;
            int segTo   = seg.segTo;
            int segDelta = Mathf.Abs(segFrom - segTo);
            float segDuration = totalDuration * (segDelta / (float)totalDelta);

            // ��ԊJ�n���_�̐F��K�p
            UpdateHPBarColor(bar, background, segFrom);

            // ���i�K���ł̃o�[�l���v�Z
            float startVal = HPInStageValue(segFrom, false, segTo < segFrom);
            float endVal   = HPInStageValue(segTo, true, segTo < segFrom);

            Debug.Log($"Segment: {segFrom}��{segTo}, startVal={startVal}, endVal={endVal}, duration={segDuration}");

            // ��ԃA�j���[�V����
            float t = 0f;
            float start = startVal; // ���ݒl������
            while (t < segDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / segDuration);
                bar.value = Mathf.Lerp(start, endVal, u);
                yield return null;
            }
            bar.value = endVal;
        }


        // �ŏI�F�E�w�i���m��
        UpdateHPBarColor(bar, background, toHP);
    }

    private struct HpSeg { public int segFrom; public int segTo; public HpSeg(int f,int t){segFrom=f;segTo=t;} }

    private List<HpSeg> BuildSegments(int fromHP, int toHP)
    {
        var list = new List<HpSeg>();

        if (toHP < fromHP) // �_���[�W�i���������j
        {
            int cur = fromHP;
            while (cur > toHP)
            {
                int lowerBoundary = ((cur - 1) / 1000) * 1000; // ���̒i�̉����i��:1500��1000�j
                int segTo = Mathf.Max(toHP, lowerBoundary);
                list.Add(new HpSeg(cur, segTo));
                cur = segTo;
            }
        }
        else // �񕜁i���������j���ꉞ�Ή�
        {
            int cur = fromHP;
            while (cur < toHP)
            {
                int upperBoundary = ((cur) / 1000 + 1) * 1000; // ���̒i�̏���i��:1200��2000�j
                int segTo = Mathf.Min(toHP, upperBoundary);
                list.Add(new HpSeg(cur, segTo));
                cur = segTo;
            }
        }

        return list;
    }

    private int HPInStageValue(int hp, bool isEnd, bool isDamage = false)
    {
        int ret;

        if (hp <= 0) return 0;
        int v = hp % 1000;
        if (v == 0 && hp > 0) {
            if ( ( isEnd && isDamage )|| ( !isEnd && !isDamage ) ){
                ret = 0;
            }
            else {
                ret = 1000;
            }
        }
        else {
            ret =  v;
        }

        return ret;
    }

    private void UpdateHPBarColor(Slider bar, GameObject background, int hp)
    {
        if (bar == null) return;

        // HP���Ƃ̒i�K���v�Z�i1?1000:��1�i�K�A1001?2000:��2�i�K�A...�j
        int stage = Mathf.Clamp((hp - 1) / 1000, 0, 4); // 0?4�i�K�ɐ����i�ő�5000HP�z��j

        // Fill Area �̃C���[�W���擾
        var fill = bar.fillRect.GetComponentInChildren<Image>();
        if (fill == null) return;

        // �f�t�H���g�����w�i
        if (background != null)
            background.GetComponent<Image>().color = new Color(1, 1, 1, 0); // ���S����

        // HP�i�K�ɉ����ĐF��؂�ւ�
        switch (stage)
        {
            case 0: // 1?1000
                fill.color = Color.red;
                if (background != null)
                    background.GetComponent<Image>().color = new Color(1, 1, 1, 0); // �w�i����
                break;

            case 1: // 1001?2000
                fill.color = Color.yellow;
                if (background != null)
                    background.GetComponent<Image>().color = Color.red;
                break;

            case 2: // 2001?3000
                fill.color = Color.green;
                if (background != null)
                    background.GetComponent<Image>().color = Color.yellow;
                break;

            case 3: // 3001?4000
                fill.color = Color.cyan;
                if (background != null)
                    background.GetComponent<Image>().color = Color.green;
                break;

            default: // 4001�ȏ�
                fill.color = Color.magenta;
                if (background != null)
                    background.GetComponent<Image>().color = Color.cyan;
                break;
        }
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
        foreach (Transform child in selectPanelParent) Destroy(child.gameObject);
        buttonsByUser.Clear();

        for (int i = 0; i < playerCards.Length; i++)
        {
            MonsterCard card = playerCards[i];
            GameObject unit = Instantiate(selectPanelPrefab, selectPanelParent);

            // �����X�^�[�摜�ݒ�
            var image = unit.transform.Find("MonsterImageObj/MonsterImage").GetComponent<Image>();
            image.sprite = card.monsterSprite;

            // �{�^���擾
            Button btn1 = unit.transform.Find("SkillPanel/SkillButtonRedObj/SkillButtonRed").GetComponent<Button>();
            Button btn2 = unit.transform.Find("SkillPanel/SkillButtonBlueObj/SkillButtonBlue").GetComponent<Button>();
            var txt1 = btn1.GetComponentInChildren<TextMeshProUGUI>();
            var txt2 = btn2.GetComponentInChildren<TextMeshProUGUI>();

            txt1.text = card.skills[0].skillName;
            txt2.text = card.skills[1].skillName;

            // �o�^
            buttonsByUser[i] = new List<Button> { btn1, btn2 };

            // �N���b�N�C�x���g
            int u = i;
            btn1.onClick.AddListener(() => SelectSkillButton(u, 0, btn1));
            btn2.onClick.AddListener(() => SelectSkillButton(u, 1, btn2));
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
        selectPanelParent.gameObject.SetActive(active);
        // skillPanel.gameObject.SetActive(active);
        // namePanel.gameObject.SetActive(active);
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
        dmgText.text = value.ToString();
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

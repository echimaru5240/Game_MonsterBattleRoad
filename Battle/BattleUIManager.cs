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

    [Header("HP�o�[�i�K�F�ݒ�")]
    [SerializeField] private Color[] fillColors = new Color[5];       // HP�o�[�̐F
    [SerializeField] private Color transparent = new Color(1, 1, 1, 0); // �f�t�H���g����

    [Header("�e�L�X�g�Ǘ�")]
    public BattleTextManager textManager;

    [Header("Skill Buttons")]
    public Transform selectPanelParent;
    public GameObject selectPanelPrefab;

    [Header("�A�N�V�������̓G���������w�i")]
    public Transform playerActionBackParent;
    public Transform enemyActionBackParent;

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

    // private readonly Color selectedBtnColor = new Color(1f, 111f / 255f, 0f, 1f);
    [SerializeField] private Color disabledBtnColor = new Color();

    private Coroutine playerHpTextRoutine;
    private Coroutine enemyHpTextRoutine;
    private Coroutine playerHpBarRoutine;
    private Coroutine enemyHpBarRoutine;
    private int m_OldPlayerHP;
    private int m_OldEnemyHP;


    // ================================
    // ������
    // ================================
    public void Init(List<MonsterController> playerControllers, int playerHP, int enemyHP, int courageMax = 100)
    {
        // HP�o�[������
        m_OldPlayerHP = 0;
        m_OldEnemyHP = 0;
        UpdateHP(playerHP, enemyHP);

        playerActionBackParent.gameObject.SetActive(false);
        enemyActionBackParent.gameObject.SetActive(false);

        // �E�C�Q�[�W������
        if (courageBar != null)
        {
            courageBar.maxValue = courageMax;
            courageBar.value = 0;
        }

        selectPanelParent.gameObject.SetActive(false);
        GenerateSkillButtons(playerControllers);
    }

    // ================================
    // HP�X�V
    // ================================
    public void UpdateHP(int playerHP, int enemyHP)
    {
        // HP(����)�����X�ɕω�
        if (playerHpTextRoutine != null) StopCoroutine(playerHpTextRoutine);
        if (enemyHpTextRoutine != null) StopCoroutine(enemyHpTextRoutine);

        playerHpTextRoutine = StartCoroutine(SmoothHPTextChange(playerHPText, m_OldPlayerHP, playerHP));
        enemyHpTextRoutine = StartCoroutine(SmoothHPTextChange(enemyHPText, m_OldEnemyHP, enemyHP));

        // HP(�o�[)�����X�ɕω�
        if (playerHpBarRoutine != null) StopCoroutine(playerHpBarRoutine);
        if (enemyHpBarRoutine != null) StopCoroutine(enemyHpBarRoutine);

        playerHpBarRoutine = StartCoroutine(SmoothHPChangeSegmented(playerHPBar, playerHPBarBackGround, playerHP, m_OldPlayerHP));
        enemyHpBarRoutine = StartCoroutine(SmoothHPChangeSegmented(enemyHPBar, enemyHPBarBackGround, enemyHP, m_OldEnemyHP));

        m_OldPlayerHP = playerHP;
        m_OldEnemyHP = enemyHP;
    }

    /// <summary>
    /// HP(���l)�����炩�ɕω�������
    /// </summary>
    private IEnumerator SmoothHPTextChange(TMPro.TextMeshProUGUI text, int fromHP, int toHP)
    {
        if (text == null) yield break;

        float duration = 0.5f; // ���l�ω��̑��x�i�����\�j
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            int current = Mathf.RoundToInt(Mathf.Lerp(fromHP, toHP, t));
            text.text = $"{current}";
            yield return null;
        }

        // �ŏI�l���Œ�
        text.text = $"{toHP}";
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

        // HP���Ƃ̒i�K���v�Z�i��: 1?1000 = 0, 1001?2000 = 1 ...�j
        int stage = Mathf.Clamp((hp - 1) / 1000, 0, fillColors.Length - 1);

        // Fill Area �̃C���[�W���擾
        var fill = bar.fillRect.GetComponentInChildren<Image>();
        if (fill == null) return;

        // �f�t�H���g�w�i�i�����j
        if (background != null)
            background.GetComponent<Image>().color = transparent;

        // Inspector�Őݒ肳�ꂽ�F�𔽉f
        if (fillColors.Length > stage + 1)
            fill.color = fillColors[stage+1];

        if (fillColors.Length > stage && background != null)
            background.GetComponent<Image>().color = fillColors[stage];
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

    public void ShowActionBack(bool isPlayerSide)
    {
        GameObject targetObj = isPlayerSide ? playerActionBackParent.gameObject : enemyActionBackParent.gameObject;
        targetObj.SetActive(true);
        StartCoroutine(ShowActionBackRoutine(targetObj, 1f));
    }

    private IEnumerator ShowActionBackRoutine(GameObject targetObj, float duration)
    {
        yield return new WaitForSeconds(duration);
        targetObj.SetActive(false);
    }

    // ================================
    // �e�L�X�g�֘A���b�p�[
    // ================================

    public void ShowTurnText(int turn)
    {
        textManager.ShowTurnText(turn);
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

    // ================================
    // �X�L���{�^������
    // ================================
    private void GenerateSkillButtons(List<MonsterController> playerControllers)
    {
        foreach (Transform child in selectPanelParent) Destroy(child.gameObject);
        buttonsByUser.Clear();

        for (int i = 0; i < playerControllers.Count; i++)
        {
            Debug.Log($"playerCount: {playerControllers.Count}");
            var monster = playerControllers[i];
            GameObject unit = Instantiate(selectPanelPrefab, selectPanelParent);

            // �����X�^�[�摜�ݒ�
            var image = unit.transform.Find("MonsterImageObj/MonsterImage").GetComponent<Image>();
            image.sprite = monster.sprite;

            // �{�^���擾
            Button btn1 = unit.transform.Find("SkillPanel/SkillButtonRedObj/SkillButtonRed").GetComponent<Button>();
            Button btn2 = unit.transform.Find("SkillPanel/SkillButtonBlueObj/SkillButtonBlue").GetComponent<Button>();
            var txt1 = btn1.GetComponentInChildren<TextMeshProUGUI>();
            var txt2 = btn2.GetComponentInChildren<TextMeshProUGUI>();

            txt1.text = monster.skills[0].skillName;
            txt2.text = monster.skills[1].skillName;

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
        AudioManager.Instance.PlayButtonSE();
        SetSkillButtonFrameActive(pressed, true);
        // SetButtonImmediateColor(pressed, selectedBtnColor);
        pressed.interactable = false;

        foreach (var b in buttonsByUser[userIndex])
        {
            if (b == pressed) continue;
            // SetButtonImmediateColor(b, disabledBtnColor);
            b.interactable = false;
            b.gameObject.SetActive(false);
        }

        OnSkillSelected?.Invoke(userIndex, skillIndex);
    }

    /// <summary>
    /// ����̃X�L���{�^���̃t���[���\����؂�ւ���
    /// </summary>
    public void SetSkillButtonFrameActive(Button button, bool active)
    {
        if (button == null) return;

        // Frame �I�u�W�F�N�g��T���i�K�w���j
        Transform frame = button.transform.parent.Find("Frame");
        if (frame != null)
        {
            frame.gameObject.SetActive(active);
        }
        else
        {
            Debug.LogWarning($"Frame �I�u�W�F�N�g�� {button.name} ���Ɍ�����܂���ł����B");
        }
    }

    /// <summary>
    /// �O���������̃X�L���{�^���̃t���[���\����؂�ւ���
    /// </summary>
    public void SetSkillButtonFrameActive(int userIndex, int skillIndex, bool active)
    {
        Button button = buttonsByUser[userIndex][skillIndex];
        if (button == null) return;

        // Frame �I�u�W�F�N�g��T���i�K�w���j
        Transform frame = button.transform.parent.Find("Frame");
        if (frame != null)
        {
            frame.gameObject.SetActive(active);
        }
        else
        {
            Debug.LogWarning($"Frame �I�u�W�F�N�g�� {button.name} ���Ɍ�����܂���ł����B");
        }
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
                SetSkillButtonFrameActive(btn, false);
                btn.gameObject.SetActive(true); // �ĕ\��
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
    public void ShowDamagePopup(int value, MonsterController target, bool isHeal = false)
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

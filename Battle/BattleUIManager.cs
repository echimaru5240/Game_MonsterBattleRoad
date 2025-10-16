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

    [Header("テキスト管理")]
    public BattleTextManager textManager;

    [Header("Skill Buttons")]
    public Transform selectPanelParent;
    public GameObject selectPanelPrefab;

    [Header("Damage Popup")]
    public GameObject damageTextPrefab;
    public Transform canvasTransform;

    [Header("勇気ゲージ UI")]
    public Slider courageBar;

    // コールバック
    public Action<int, int> OnSkillSelected;

    // 内部管理
    private Dictionary<int, List<Button>> buttonsByUser = new Dictionary<int, List<Button>>();
    private List<TextMeshProUGUI> playerNameLabels = new List<TextMeshProUGUI>();

    private readonly Color selectedBtnColor = new Color(1f, 111f / 255f, 0f, 1f);
    private readonly Color disabledBtnColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    private Coroutine playerHpRoutine;
    private Coroutine enemyHpRoutine;
    private int m_OldPlayerHP;
    private int m_OldEnemyHP;


    // ================================
    // 初期化
    // ================================
    public void Init(MonsterCard[] playerCards, int playerHP, int playerMaxHP, int enemyHP, int enemyMaxHP, int courageMax = 100)
    {
        // HPバー初期化
        m_OldPlayerHP = 0;
        m_OldEnemyHP = 0;
        UpdateHP(playerHP, playerMaxHP, enemyHP, enemyMaxHP);

        // 勇気ゲージ初期化
        if (courageBar != null)
        {
            courageBar.maxValue = courageMax;
            courageBar.value = 0;
        }

        selectPanelParent.gameObject.SetActive(false);
        GenerateSkillButtons(playerCards);
    }

    // ================================
    // HP更新
    // ================================
    public void UpdateHP(int playerHP, int playerMax, int enemyHP, int enemyMax)
    {
        playerHPText.text = $"{playerHP} / {playerMax}";
        enemyHPText.text = $"{enemyHP} / {enemyMax}";


        // HPバーを徐々に変化
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
            // 最終色・背景だけ整える
            UpdateHPBarColor(bar, background, toHP);
            bar.maxValue = 1000;
            bar.value = HPInStageValue(toHP, true);
            yield break;
        }

        // 総アニメ時間は常に 0.8秒
        const float totalDuration = 0.5f;
        int totalDelta = Mathf.Abs(toHP - fromHP);

        // 現在の見た目値（途中で割り込まれた時の連続性を保つため）
        float visualStart = bar.value;
        bar.maxValue = 1000;

        // 区間生成（1000境界で分割）
        var segments = BuildSegments(fromHP, toHP);  // List<(int segFrom, int segTo)>

        bool first = true;
        foreach (var seg in segments)
        {
            int segFrom = seg.segFrom;
            int segTo   = seg.segTo;
            int segDelta = Mathf.Abs(segFrom - segTo);
            float segDuration = totalDuration * (segDelta / (float)totalDelta);

            // 区間開始時点の色を適用
            UpdateHPBarColor(bar, background, segFrom);

            // 現段階内でのバー値を計算
            float startVal = HPInStageValue(segFrom, false, segTo < segFrom);
            float endVal   = HPInStageValue(segTo, true, segTo < segFrom);

            Debug.Log($"Segment: {segFrom}→{segTo}, startVal={startVal}, endVal={endVal}, duration={segDuration}");

            // 区間アニメーション
            float t = 0f;
            float start = startVal; // 現在値から補間
            while (t < segDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / segDuration);
                bar.value = Mathf.Lerp(start, endVal, u);
                yield return null;
            }
            bar.value = endVal;
        }


        // 最終色・背景を確定
        UpdateHPBarColor(bar, background, toHP);
    }

    private struct HpSeg { public int segFrom; public int segTo; public HpSeg(int f,int t){segFrom=f;segTo=t;} }

    private List<HpSeg> BuildSegments(int fromHP, int toHP)
    {
        var list = new List<HpSeg>();

        if (toHP < fromHP) // ダメージ（減少方向）
        {
            int cur = fromHP;
            while (cur > toHP)
            {
                int lowerBoundary = ((cur - 1) / 1000) * 1000; // その段の下限（例:1500→1000）
                int segTo = Mathf.Max(toHP, lowerBoundary);
                list.Add(new HpSeg(cur, segTo));
                cur = segTo;
            }
        }
        else // 回復（増加方向）も一応対応
        {
            int cur = fromHP;
            while (cur < toHP)
            {
                int upperBoundary = ((cur) / 1000 + 1) * 1000; // その段の上限（例:1200→2000）
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

        // HPごとの段階を計算（1?1000:第1段階、1001?2000:第2段階、...）
        int stage = Mathf.Clamp((hp - 1) / 1000, 0, 4); // 0?4段階に制限（最大5000HP想定）

        // Fill Area のイメージを取得
        var fill = bar.fillRect.GetComponentInChildren<Image>();
        if (fill == null) return;

        // デフォルト透明背景
        if (background != null)
            background.GetComponent<Image>().color = new Color(1, 1, 1, 0); // 完全透明

        // HP段階に応じて色を切り替え
        switch (stage)
        {
            case 0: // 1?1000
                fill.color = Color.red;
                if (background != null)
                    background.GetComponent<Image>().color = new Color(1, 1, 1, 0); // 背景透明
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

            default: // 4001以上
                fill.color = Color.magenta;
                if (background != null)
                    background.GetComponent<Image>().color = Color.cyan;
                break;
        }
}


    // ================================
    // 勇気ゲージ更新
    // ================================
    public void UpdateCourage(int current, int max)
    {
        if (courageBar == null) return;
        courageBar.maxValue = max;
        courageBar.value = current;
    }

    // ================================
    // テキスト関連ラッパー
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
        string msg = playerWon ? "勝利！" : "敗北…";
        textManager.ShowMainText(msg, 2.5f);
    }

    // ================================
    // スキルボタン生成
    // ================================
    private void GenerateSkillButtons(MonsterCard[] playerCards)
    {
        foreach (Transform child in selectPanelParent) Destroy(child.gameObject);
        buttonsByUser.Clear();

        for (int i = 0; i < playerCards.Length; i++)
        {
            MonsterCard card = playerCards[i];
            GameObject unit = Instantiate(selectPanelPrefab, selectPanelParent);

            // モンスター画像設定
            var image = unit.transform.Find("MonsterImageObj/MonsterImage").GetComponent<Image>();
            image.sprite = card.monsterSprite;

            // ボタン取得
            Button btn1 = unit.transform.Find("SkillPanel/SkillButtonRedObj/SkillButtonRed").GetComponent<Button>();
            Button btn2 = unit.transform.Find("SkillPanel/SkillButtonBlueObj/SkillButtonBlue").GetComponent<Button>();
            var txt1 = btn1.GetComponentInChildren<TextMeshProUGUI>();
            var txt2 = btn2.GetComponentInChildren<TextMeshProUGUI>();

            txt1.text = card.skills[0].skillName;
            txt2.text = card.skills[1].skillName;

            // 登録
            buttonsByUser[i] = new List<Button> { btn1, btn2 };

            // クリックイベント
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
    // ボタンの有効化・無効化
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
                // btn.gameObject.SetActive(true); // 再表示
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
    // ダメージポップアップ
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
    // ボタン即時色変更
    // ================================
    private void SetButtonImmediateColor(Button btn, Color color)
    {
        var graphic = btn.targetGraphic as Graphic;
        if (graphic) graphic.color = color;
    }
}

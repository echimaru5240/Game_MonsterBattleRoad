using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class BattleUIManager : MonoBehaviour
{
    [Header("HP UI")]
    [SerializeField] private Slider playerHPBar;
    [SerializeField] private GameObject playerHPBarBackGround;
    [SerializeField] private Slider enemyHPBar;
    [SerializeField] private GameObject enemyHPBarBackGround;
    [SerializeField] private TextMeshProUGUI playerHPText;
    [SerializeField] private TextMeshProUGUI enemyHPText;

    [Header("HPバー段階色設定")]
    [SerializeField] private Color[] fillColors = new Color[5];       // HPバーの色
    [SerializeField] private Color transparent = new Color(1, 1, 1, 0); // デフォルト透明

    [Header("テキスト管理")]
    public BattleTextManager textManager;

    [Header("Skill Buttons")]
    [SerializeField] private GameObject skillPanelPrefab;
    [SerializeField] private Transform selectPanelParent;
    [SerializeField] private GameObject selectPanelPrefab;

    [Header("アクション時の敵味方強調背景")]
    [SerializeField] private Transform playerActionBackParent;
    [SerializeField] private Transform enemyActionBackParent;

    [Header("Damage Popup")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private float damageTextFadeDuration;

    [Header("勇気ゲージ UI")]
    [SerializeField] private Slider courageBar;

    // コールバック
    public Action<int, int> OnSkillSelected;
    public Action OnBattleEnd;

    // 内部管理
    private Dictionary<int, List<Button>> buttonsByUser = new Dictionary<int, List<Button>>();
    private List<TextMeshProUGUI> playerNameLabels = new List<TextMeshProUGUI>();

    [SerializeField] private Color popupTextDamageColor = new Color();
    [SerializeField] private Color popupTextHealColor = new Color();
    [SerializeField] private Color popupTextCriticalColor = new Color();
    [SerializeField] private Color popupTextCriticalTextColor = new Color();

    private Coroutine playerHpTextRoutine;
    private Coroutine enemyHpTextRoutine;
    private Coroutine playerHpBarRoutine;
    private Coroutine enemyHpBarRoutine;
    private int m_OldPlayerHP;
    private int m_OldEnemyHP;


    private readonly List<BattleUISkillPanel> panels = new();

    // ================================
    // 初期化
    // ================================
    public void Init(MonsterBattleData[] monsters, int playerHP, int enemyHP, int courageMax = 100)
    {
        // HPバー初期化
        m_OldPlayerHP = 0;
        m_OldEnemyHP = 0;
        UpdateHP(playerHP, enemyHP, 2.0f);

        playerActionBackParent.gameObject.SetActive(false);
        enemyActionBackParent.gameObject.SetActive(false);

        // 勇気ゲージ初期化
        if (courageBar != null)
        {
            courageBar.maxValue = courageMax;
            courageBar.value = 0;
        }

        selectPanelParent.gameObject.SetActive(false);
        GenerateSkillButtons(monsters);
    }

    // ================================
    // HP更新
    // ================================
    public void UpdateHP(int playerHP, int enemyHP, float totalDuration = 0.5f)
    {
        // HP(数字)を徐々に変化
        if (playerHpTextRoutine != null) StopCoroutine(playerHpTextRoutine);
        if (enemyHpTextRoutine != null) StopCoroutine(enemyHpTextRoutine);

        playerHpTextRoutine = StartCoroutine(SmoothHPTextChange(playerHPText, m_OldPlayerHP, playerHP, totalDuration));
        enemyHpTextRoutine = StartCoroutine(SmoothHPTextChange(enemyHPText, m_OldEnemyHP, enemyHP, totalDuration));

        // HP(バー)を徐々に変化
        if (playerHpBarRoutine != null) StopCoroutine(playerHpBarRoutine);
        if (enemyHpBarRoutine != null) StopCoroutine(enemyHpBarRoutine);

        playerHpBarRoutine = StartCoroutine(SmoothHPChangeSegmented(playerHPBar, playerHPBarBackGround, playerHP, m_OldPlayerHP, totalDuration));
        enemyHpBarRoutine = StartCoroutine(SmoothHPChangeSegmented(enemyHPBar, enemyHPBarBackGround, enemyHP, m_OldEnemyHP, totalDuration));

        m_OldPlayerHP = playerHP;
        m_OldEnemyHP = enemyHP;
    }

    /// <summary>
    /// HP(数値)を滑らかに変化させる
    /// </summary>
    private IEnumerator SmoothHPTextChange(TMPro.TextMeshProUGUI text, int fromHP, int toHP, float totalDuration)
    {
        if (text == null) yield break;

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalDuration);
            int current = Mathf.RoundToInt(Mathf.Lerp(fromHP, toHP, t));
            text.text = $"{current}";
            yield return null;
        }

        // 最終値を固定
        text.text = $"{toHP}";
    }

    private IEnumerator SmoothHPChangeSegmented(Slider bar, GameObject background, int toHP, int fromHP, float totalDuration)
    {
        if (bar == null || fromHP == toHP)
        {
            // 最終色・背景だけ整える
            UpdateHPBarColor(bar, background, toHP);
            bar.maxValue = 1000;
            bar.value = HPInStageValue(toHP, true);
            yield break;
        }

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

            int segColor = Mathf.Max(segFrom, segTo);

            // 区間開始時点の色を適用
            UpdateHPBarColor(bar, background, segColor);

            // 現段階内でのバー値を計算
            float startVal = HPInStageValue(segFrom, false, segTo < segFrom);
            float endVal   = HPInStageValue(segTo, true, segTo < segFrom);

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

    private struct HpSeg {
        public int segFrom;
        public int segTo;

        public HpSeg(int f,int t)
        {
            segFrom = f;
            segTo = t;
        }
    }

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

        // HPごとの段階を計算（例: 1?1000 = 0, 1001?2000 = 1 ...）
        int stage = Mathf.Clamp((hp - 1) / 1000, 0, fillColors.Length - 1);

        // Fill Area のイメージを取得
        var fill = bar.fillRect.GetComponentInChildren<Image>();
        if (fill == null) return;

        // デフォルト背景（透明）
        if (background != null)
            background.GetComponent<Image>().color = transparent;

        // Inspectorで設定された色を反映
        if (fillColors.Length > stage + 1)
            fill.color = fillColors[stage+1];

        if (fillColors.Length > stage && background != null)
            background.GetComponent<Image>().color = fillColors[stage];
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
    // テキスト関連ラッパー
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

    private void GenerateSkillButtons(MonsterBattleData[] monsters)
    {
        foreach (Transform child in selectPanelParent) Destroy(child.gameObject);
        panels.Clear();

        for (int i = 0; i < monsters.Length; i++)
        {
            var unit = Instantiate(skillPanelPrefab, selectPanelParent);
            var panel = unit.GetComponent<BattleUISkillPanel>();

            int userIndex = i;

            // 念のため多重登録防止：新規生成なので基本いらないが安全
            panel.OnSkillSelected -= HandleSkillSelected;
            panel.OnSkillSelected += HandleSkillSelected;

            panel.Setup(userIndex, monsters[i]);
            panels.Add(panel);
        }
    }

    private void HandleSkillSelected(int userIndex, int skillIndex)
    {

        Debug.Log($"[UIManager Selected] user={userIndex} skill={skillIndex}");
        AudioManager.Instance.PlayButtonSE();
        OnSkillSelected?.Invoke(userIndex, skillIndex);
    }

    // ================================
    // ボタンの有効化・無効化（panels版）
    // ================================
    public void ResetButtons()
    {
        foreach (var p in panels)
        {
            if (p == null) continue;
            p.ResetButtons();
        }
    }

    public void DisableButtons()
    {
        foreach (var p in panels)
        {
            if (p == null) continue;
            p.DisableButtons();
        }
    }

    public void SetSkillButtonFrameActive(int userIndex, int skillIndex, bool active)
    {
        if (userIndex < 0 || userIndex >= panels.Count) return;
        var p = panels[userIndex];
        if (p == null) return;
        p.SetSkillButtonFrameActive(skillIndex, active);
    }


    public void OnBattleEndButton()
    {
        OnBattleEnd?.Invoke();
    }

    /// <summary>
    /// 特定のスキルボタンのフレーム表示を切り替える
    /// </summary>
    // public void SetSkillButtonFrameActive(Button button, bool active)
    // {
    //     if (button == null) return;

    //     // Frame オブジェクトを探す（階層内）
    //     Transform frame = button.transform.parent.Find("Frame");
    //     if (frame != null)
    //     {
    //         frame.gameObject.SetActive(active);
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"Frame オブジェクトが {button.name} 内に見つかりませんでした。");
    //     }
    // }

    // ================================
    // ボタンの有効化・無効化
    // ================================
    // public void ResetButtons()
    // {
    //     foreach (var kv in buttonsByUser)
    //     {
    //         foreach (var btn in kv.Value)
    //         {
    //             if (!btn) continue;
    //             var graphic = btn.targetGraphic as Graphic;
    //             if (graphic) graphic.color = Color.white;
    //             btn.interactable = true;
    //             SetSkillButtonFrameActive(btn, false);
    //             btn.gameObject.SetActive(true); // 再表示
    //         }
    //     }
    // }

    // public void DisableButtons()
    // {
    //     foreach (var kv in buttonsByUser)
    //     {
    //         foreach (var btn in kv.Value)
    //         {
    //             if (!btn) continue;
    //             btn.interactable = false;
    //         }
    //     }
    // }

    public void SetButtonsActive(bool active)
    {
        selectPanelParent.gameObject.SetActive(active);
        // skillPanel.gameObject.SetActive(active);
        // namePanel.gameObject.SetActive(active);
    }

    // ================================
    // ダメージポップアップ
    // ================================
    public void ShowDamagePopup(BattleCalculator.ActionResult result)
    {
        Vector3 worldPos = result.Target.transform.position + Vector3.up * 2f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        GameObject popup = Instantiate(damageTextPrefab, canvasTransform);
        popup.transform.position = screenPos;

        var dmgText = popup.GetComponent<DamageText>().textMesh;
        dmgText.text = result.Value.ToString();

        if (result.IsDamage) {
            dmgText.color = popupTextDamageColor;
            if (result.IsMiss) {
                dmgText.color = Color.gray;
                dmgText.text = "Miss";
            }
            else if (result.IsCritical) {
                dmgText.color = popupTextCriticalColor;
                popup.transform.localScale = Vector3.one * 1.8f;
                popup.transform
                    .DOPunchScale(Vector3.one * 0.5f, 0.25f, 1, 0.5f);

                // クリティカルテキストの表示
                GameObject popupCritical = Instantiate(damageTextPrefab, canvasTransform);
                popupCritical.transform.position    = screenPos + new Vector3(0f, 100f, -10f);
                popupCritical.transform.localScale  = Vector3.one * 1.4f;
                popupCritical.transform.rotation  = Quaternion.Euler(0f, UnityEngine.Random.Range(-30f, 30f), UnityEngine.Random.Range(-30f, 30f));
                var criticalText = popupCritical.GetComponent<DamageText>().textMesh;
                criticalText.text = "CRITICAL!";
                criticalText.color = popupTextCriticalTextColor;
                // ふわっと上に浮かんでフェードアウト
                var cgCrit = popupCritical.AddComponent<CanvasGroup>();
                cgCrit.alpha = 1f;

                Sequence critSeq = DOTween.Sequence();
                critSeq.Append(popupCritical.transform.DOMoveY(popupCritical.transform.position.y + 40f, damageTextFadeDuration));
                critSeq.Join(cgCrit.DOFade(0f, damageTextFadeDuration));
                critSeq.OnComplete(() => Destroy(popupCritical));
            }
        }
        else {
            dmgText.color = Color.green;
        }
        var cg = popup.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // 少し残ってからスッと消える
        Sequence seq = DOTween.Sequence();
        seq.Append(cg.DOFade(0f, damageTextFadeDuration));
        seq.OnComplete(() => Destroy(popup));
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

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class StatAllocateRowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statNameText;
    [SerializeField] private TextMeshProUGUI riseValueText;
    [SerializeField] private TextMeshProUGUI allocatedPointText;
    [SerializeField] private TextMeshProUGUI projectedValueText;

    [Header("Buttons")]
    [SerializeField] private Button dec1Button;
    [SerializeField] private Button dec10Button;
    [SerializeField] private Button inc1Button;
    [SerializeField] private Button inc10Button;

    [Header("Display")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color sessionColor = new Color(1f, 0.82f, 0f); // 黄色
    [SerializeField] private string sessionColorHex = "#FFD200";


    private StatType statType;
    private Func<StatType, int, bool> tryAdd;
    private Func<StatType, int, bool> trySub;

    private Func<StatType, int> getBaseAllocated;    // 確定済み
    private Func<StatType, int> getSessionAllocated; // 今回分
    private Func<StatType, float> getProjectedValue;   // 反映後のステ値（確定 + セッション）

    private Func<int> getRemaining;
    private Action onChanged;

    public void Bind(
        StatType type,
        string statName,
        string riseValueStr,
        Func<StatType, int, bool> tryAdd,
        Func<StatType, int, bool> trySub,
        Func<StatType, int> getBaseAllocated,
        Func<StatType, int> getSessionAllocated,
        Func<StatType, float> getProjectedValue,      // ★追加
        Func<int> getRemaining,
        Action onChanged)
    {
        this.statType = type;
        this.tryAdd = tryAdd;
        this.trySub = trySub;
        this.getBaseAllocated = getBaseAllocated;
        this.getSessionAllocated = getSessionAllocated;
        this.getProjectedValue = getProjectedValue;      // ★
        this.getRemaining = getRemaining;
        this.onChanged = onChanged;

        statNameText.SetText(statName);
        riseValueText.SetText(riseValueStr);

        WireButtons();
        Refresh();
    }

    private void WireButtons()
    {
        if (dec1Button)
        {
            dec1Button.onClick.RemoveAllListeners();
            dec1Button.onClick.AddListener(() =>
            {
                if (trySub(statType, 1)) onChanged?.Invoke();
            });
        }

        // dec10: 今回分が10未満ならそれ全部戻す
        if (dec10Button)
        {
            dec10Button.onClick.RemoveAllListeners();
            dec10Button.onClick.AddListener(() =>
            {
                int session = getSessionAllocated(statType);
                int sub = Mathf.Min(10, session);
                if (sub <= 0) return;
                if (trySub(statType, sub)) onChanged?.Invoke();
            });
        }

        if (inc1Button)
        {
            inc1Button.onClick.RemoveAllListeners();
            inc1Button.onClick.AddListener(() =>
            {
                if (tryAdd(statType, 1)) onChanged?.Invoke();
            });
        }

        // inc10: 残りが10未満なら残り全部
        if (inc10Button)
        {
            inc10Button.onClick.RemoveAllListeners();
            inc10Button.onClick.AddListener(() =>
            {
                int remain = getRemaining();
                int add = Mathf.Min(10, remain);
                if (add <= 0) return;
                if (tryAdd(statType, add)) onChanged?.Invoke();
            });
        }
    }
    // public void Refresh()
    // {
    //     int baseAlloc = getBaseAllocated(statType);
    //     int sessionAlloc = getSessionAllocated(statType);
    //     int total = baseAlloc + sessionAlloc;

    //     // 数値は「合計のみ」
    //     allocatedPointText.SetText(total.ToString());

    //     // ★ 今回のセッションで振っていたら黄色
    //     allocatedPointText.color =
    //         sessionAlloc > 0 ? sessionColor : normalColor;

    //     int remain = getRemaining();

    //     // 減算はセッション分だけ
    //     if (dec1Button)  dec1Button.interactable  = sessionAlloc >= 1;
    //     if (dec10Button) dec10Button.interactable = sessionAlloc >= 1;

    //     if (inc1Button)  inc1Button.interactable  = remain >= 1;
    //     if (inc10Button) inc10Button.interactable = remain >= 1;
    // }


    public void Refresh()
    {
        int baseAlloc = getBaseAllocated(statType);
        int sessionAlloc = getSessionAllocated(statType);
        float proj = getProjectedValue(statType);
        int total = baseAlloc + sessionAlloc;

        // 表示：合計 +（今回分を黄色）
        if (sessionAlloc > 0)
        {
            // 反映後を黄色に
            projectedValueText.SetText($"<color={sessionColorHex}>{proj}</color>");
            allocatedPointText.SetText($"{total} <color={sessionColorHex}>(+{sessionAlloc})</color>");
        }
        else
        {
            projectedValueText.SetText($"{proj}");
            allocatedPointText.SetText(total.ToString());
        }

        int remain = getRemaining();

        // ★ 減算は「今回分」だけ可能
        if (dec1Button)  dec1Button.interactable  = sessionAlloc >= 1;
        if (dec10Button) dec10Button.interactable = sessionAlloc >= 1;

        if (inc1Button)  inc1Button.interactable  = remain >= 1;
        if (inc10Button) inc10Button.interactable = remain >= 1;
    }
}

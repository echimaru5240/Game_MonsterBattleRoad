using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class StatAllocateView : MonoBehaviour
{
    [Header("Top UI")]
    [SerializeField] private TextMeshProUGUI remainingPointText; // "残りポイント 40"
    [SerializeField] private Button resetButton;
    [SerializeField] private Button confirmButton;

    [Header("Rows")]
    [SerializeField] private StatAllocateRowView hpRow;
    [SerializeField] private StatAllocateRowView atkRow;
    [SerializeField] private StatAllocateRowView mgcRow;
    [SerializeField] private StatAllocateRowView defRow;
    [SerializeField] private StatAllocateRowView agiRow;

    private StatAllocateSession session;
    private Action onClosed;

    public void Open(OwnedMonster owned, Action onClosed)
    {
        session = new StatAllocateSession(owned);
        this.onClosed = onClosed;

        // 上昇値表示はとりあえず固定でもOK。後で owned.hpPerPoint から作れる
        hpRow.Bind(StatType.HP, "HP", owned.hpPerPoint.ToString(),
            TryAdd, TrySub,
            GetBaseAllocated, GetSessionAllocated, GetProjectedValue,
            GetRemaining, RefreshAll);

        atkRow.Bind(StatType.ATK, "攻撃力", owned.atkPerPoint.ToString(),
            TryAdd, TrySub,
            GetBaseAllocated, GetSessionAllocated, GetProjectedValue,
            GetRemaining, RefreshAll);

        mgcRow.Bind(StatType.MGC, "魔力", owned.mgcPerPoint.ToString(),
            TryAdd, TrySub,
            GetBaseAllocated, GetSessionAllocated, GetProjectedValue,
            GetRemaining, RefreshAll);

        defRow.Bind(StatType.DEF, "防御力", owned.defPerPoint.ToString(),
            TryAdd, TrySub,
            GetBaseAllocated, GetSessionAllocated, GetProjectedValue,
            GetRemaining, RefreshAll);

        agiRow.Bind(StatType.AGI, "速度", owned.agiPerPoint.ToString(),
            TryAdd, TrySub,
            GetBaseAllocated, GetSessionAllocated, GetProjectedValue,
            GetRemaining, RefreshAll);

        if (resetButton)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(() =>
            {
                session.Reset();
                RefreshAll();
            });
        }

        if (confirmButton)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() =>
            {
                session.ApplyToOwned(); // OwnedMonsterへ反映
                onClosed?.Invoke();
                // Close();
            });
        }

        gameObject.SetActive(true);
        RefreshAll();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        session = null;
    }

    private bool TryAdd(StatType type, int amount) => session != null && session.TryAdd(type, amount);
    private bool TrySub(StatType type, int amount) => session != null && session.TrySub(type, amount);

    // ★ 追加：既存（確定済み）ポイント
    private int GetBaseAllocated(StatType type)
        => session != null ? session.GetBaseAllocated(type) : 0;

    // ★ 追加：今回（セッション）ポイント
    private int GetSessionAllocated(StatType type)
        => session != null ? session.GetAdded(type) : 0;

    private float GetProjectedValue(StatType type)
    {
        if (session == null) return 0;
        var t = session.target;

        int added = session.GetAdded(type);

        return type switch
        {
            StatType.HP  => t.hp  + t.hpPerPoint * added,
            StatType.ATK => t.atk + t.atkPerPoint * added,
            StatType.MGC => t.mgc + t.mgcPerPoint * added,
            StatType.DEF => t.def + t.defPerPoint * added,
            StatType.AGI => t.agi + t.agiPerPoint * added,
            _ => 0
        };
    }

    private int GetRemaining() => session != null ? session.Remaining : 0;

    private void RefreshAll()
    {
        if (session == null) return;

        remainingPointText?.SetText($"{session.Remaining}");

        hpRow.Refresh();
        atkRow.Refresh();
        mgcRow.Refresh();
        defRow.Refresh();
        agiRow.Refresh();

        // 確定ボタン：何も振ってないなら押せない、なども可能
        // if (confirmButton) confirmButton.interactable = session.TotalAdded > 0;
    }
}

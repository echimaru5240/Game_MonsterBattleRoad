using System;
using UnityEngine;

[Serializable]
public class StatAllocateSession
{
    public OwnedMonster target { get; private set; }

    // 仮配分（今回の画面で何pt追加したか）
    public int addHp, addAtk, addMgc, addDef, addAgi;

    public int Remaining => Mathf.Max(0, target.unspentStatPoints - TotalAdded);
    public int TotalAdded => addHp + addAtk + addMgc + addDef + addAgi;

    public StatAllocateSession(OwnedMonster target)
    {
        this.target = target;
        Reset();
    }

    public int GetAdded(StatType type) => type switch
    {
        StatType.HP  => addHp,
        StatType.ATK => addAtk,
        StatType.MGC => addMgc,
        StatType.DEF => addDef,
        StatType.AGI => addAgi,
        _ => 0
    };

    // ★ 追加：確定済み（既に振ってある）ポイント
    public int GetBaseAllocated(StatType type)
    {
        if (target == null) return 0;

        // ↓↓↓ OwnedMonsterの持ち方に合わせてここを書き換える ↓↓↓
        return type switch
        {
            StatType.HP  => target.hpPoints,
            StatType.ATK => target.atkPoints,
            StatType.MGC => target.mgcPoints,
            StatType.DEF => target.defPoints,
            StatType.AGI => target.agiPoints,
            _ => 0
        };
    }

    public void Reset()
    {
        addHp = addAtk = addMgc = addDef = addAgi = 0;
    }

    public bool CanAdd(int amount)
    {
        if (amount <= 0) return false;
        return Remaining >= amount;
    }

    public bool TryAdd(StatType type, int amount)
    {
        if (amount <= 0) return false;
        if (!CanAdd(amount)) return false;

        switch (type)
        {
            case StatType.HP:  addHp  += amount; break;
            case StatType.ATK: addAtk += amount; break;
            case StatType.MGC: addMgc += amount; break;
            case StatType.DEF: addDef += amount; break;
            case StatType.AGI: addAgi += amount; break;
            default: return false;
        }
        return true;
    }

    public bool TrySub(StatType type, int amount)
    {
        if (amount <= 0) return false;

        int cur = GetAdded(type);
        int sub = Mathf.Min(cur, amount);
        if (sub <= 0) return false;

        switch (type)
        {
            case StatType.HP:  addHp  -= sub; break;
            case StatType.ATK: addAtk -= sub; break;
            case StatType.MGC: addMgc -= sub; break;
            case StatType.DEF: addDef -= sub; break;
            case StatType.AGI: addAgi -= sub; break;
        }
        return true;
    }

    /// <summary>
    /// 仮配分を OwnedMonster に反映（確定）
    /// </summary>
    public void ApplyToOwned()
    {
        // ここで直接ポイントを移す（OwnedMonster.TryAllocateを複数回呼んでもOK）
        ApplyOne(StatType.HP,  addHp);
        ApplyOne(StatType.ATK, addAtk);
        ApplyOne(StatType.MGC, addMgc);
        ApplyOne(StatType.DEF, addDef);
        ApplyOne(StatType.AGI, addAgi);

        Reset();
    }

    private void ApplyOne(StatType type, int amount)
    {
        if (amount <= 0) return;
        // OwnedMonsterのTryAllocateは amount まとめて対応している想定
        target.TryAllocate(type, amount);
    }
}

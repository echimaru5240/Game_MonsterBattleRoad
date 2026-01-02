using System;
using System.Collections.Generic;
using System.Linq;

public enum MonsterSortType
{
    ID,
    Name,
    Level,
    HP,
    ATK,
    MGC,
    DEF,
    AGI,
}

public enum MonsterFilterType
{
    None,
    PartyOnly,
    NotInParty,
    CanLevelUp,
}


public static class MonsterQuery
{
    public static List<OwnedMonster> Execute(
        IEnumerable<OwnedMonster> source,
        MonsterFilterType filter,
        MonsterSortType sort,
        bool ascending
    )
    {
        var filtered = MonsterFilter.Apply(source, filter);
        return MonsterSorter.Sort(filtered, sort, ascending);
    }
}

/// <summary>
/// モンスター並び替え専用クラス（UI非依存）
/// </summary>
public static class MonsterSorter
{
    public static List<OwnedMonster> Sort(
        IEnumerable<OwnedMonster> source,
        MonsterSortType sortType,
        bool ascending = true
    )
    {
        Func<OwnedMonster, object> keySelector = sortType switch
        {
            MonsterSortType.ID    => m => m.monsterId,
            MonsterSortType.Name  => m => m.Name,
            MonsterSortType.Level => m => m.level,
            MonsterSortType.HP    => m => m.hp,
            MonsterSortType.ATK   => m => m.atk,
            MonsterSortType.MGC   => m => m.mgc,
            MonsterSortType.DEF   => m => m.def,
            MonsterSortType.AGI   => m => m.agi,
            _ => m => m.monsterId,
        };

        return ascending
            ? source.OrderBy(keySelector).ToList()
            : source.OrderByDescending(keySelector).ToList();
    }
}


/// <summary>
/// モンスター絞り込み専用クラス（UI非依存）
/// </summary>
public static class MonsterFilter
{
    public static IEnumerable<OwnedMonster> Apply(
        IEnumerable<OwnedMonster> source,
        MonsterFilterType filterType
    )
    {
        return filterType switch
        {
            MonsterFilterType.None =>
                source,

            MonsterFilterType.PartyOnly =>
                source.Where(m => m.isParty),

            MonsterFilterType.NotInParty =>
                source.Where(m => !m.isParty),

            MonsterFilterType.CanLevelUp =>
                source.Where(m => m.unspentStatPoints > 0),

            _ => source
        };
    }
}
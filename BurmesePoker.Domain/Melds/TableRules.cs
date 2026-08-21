namespace BurmesePoker.Domain.Melds;

/// <summary>
/// What a declared hand must contain at this table size (RULES.md §7.1.1) — how many of its
/// melds must be series, how many of those must be joker-free, and whether sets are legal
/// melds at all.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>This is the first rule in the game whose content changes with the number of
/// players</b>, and it is the reason <see cref="HandEvaluator"/> takes a parameter. Thirteen
/// cards that partition into disjoint melds win at five seats and can lose at three.
/// </para>
/// <para>
/// <b>Purity is not a property of the hand.</b> It attaches to the series the table size
/// <em>requires</em> and stops there, so the two counts move together — nought required and
/// nought clean, one and one, two and two (RULES.md §7.1, §9 #28). A surplus series need not
/// be clean, and an all-joker series is a legal series that is never a clean one (§9 #29), so
/// it can discharge a surplus and never a requirement.
/// </para>
/// <para>
/// <b>Two-handed is a constraint of a different kind.</b> Sets are illegal <em>as melds</em>
/// (§9 #22) — nothing stops a player holding three of a kind, but a hand declared on a
/// partition that uses one is not a winning hand. That prunes the candidates before the
/// search starts; the series counts constrain the partition the search chooses.
/// </para>
/// <para>
/// <b>`DERIVED` — the shape of the table is a compensation.</b> The fewer the players, the
/// more of the shoe each one sees (§2.1), so the requirement tightens exactly as the deck
/// opens up. The game holds its difficulty roughly constant across table sizes by moving the
/// win condition rather than the hand size.
/// </para>
/// </remarks>
public readonly record struct TableRules
{
    private TableRules(int players, int requiredSeries, int requiredCleanSeries, bool setsAllowed)
    {
        Players = players;
        RequiredSeries = requiredSeries;
        RequiredCleanSeries = requiredCleanSeries;
        SetsAllowed = setsAllowed;
    }

    /// <summary>The smallest table these rules are defined for (RULES.md §2.1).</summary>
    public const int SmallestTable = 2;

    /// <summary>How many are playing.</summary>
    public int Players { get; }

    /// <summary>How many of the melds laid down must be series (RULES.md §7.1.1).</summary>
    public int RequiredSeries { get; }

    /// <summary>
    /// How many of the melds laid down must be <b>clean</b> series — a run with no joker in
    /// it (<see cref="Meld.IsClean"/>). Never more than <see cref="RequiredSeries"/>, and in
    /// the rules as recorded it is always equal to it.
    /// </summary>
    public int RequiredCleanSeries { get; }

    /// <summary>
    /// Whether a set may be one of the melds at all. False only two-handed, where the
    /// thirteen must partition into runs and nothing else (RULES.md §9 #22).
    /// </summary>
    public bool SetsAllowed { get; }

    /// <summary>
    /// The §7.1.1 table, as data. This is the only place it is written down.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Fewer than <see cref="SmallestTable"/> are playing.
    /// </exception>
    public static TableRules For(int players)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(players, SmallestTable);

        return players switch
        {
            2 => new TableRules(players, requiredSeries: 0, requiredCleanSeries: 0, setsAllowed: false),
            3 => new TableRules(players, requiredSeries: 2, requiredCleanSeries: 2, setsAllowed: true),
            4 => new TableRules(players, requiredSeries: 1, requiredCleanSeries: 1, setsAllowed: true),
            _ => new TableRules(players, requiredSeries: 0, requiredCleanSeries: 0, setsAllowed: true)
        };
    }

    /// <summary>
    /// Whether these rules ask anything of the partition beyond covering the hand — false
    /// only at five or more, where the win condition is the bare exact cover.
    /// </summary>
    public bool ConstrainsThePartition =>
        !SetsAllowed || RequiredSeries > 0 || RequiredCleanSeries > 0;

    public override string ToString() =>
        $"{Players}-handed: {(SetsAllowed ? "runs or sets" : "runs only")}, " +
        $"{RequiredSeries} series required ({RequiredCleanSeries} clean)";
}

namespace BurmesePoker.Domain.Melds;

/// <summary>
/// What a declared hand must contain at this table size (RULES.md §7.1.1) — how many of its
/// melds must be series, how many of those must be joker-free, and whether sets are legal
/// melds at all — and, since rev 26, what a <b>jokerless</b> declaration is worth here
/// (RULES.md §7.3).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>This is the first rule in the game whose content changes with the number of
/// players</b>, and it is the reason <see cref="HandEvaluator"/> takes a parameter. Thirteen
/// cards that partition into disjoint melds win at five seats and can lose at three.
/// ⚠️ <b>It is no longer the only one.</b> §7.3's clean bonus is the second, splits at the
/// same seam, and lives here for that reason — see <see cref="JokerlessMultiplier"/>. This
/// type is now what a table size means, not only what a winning hand must contain.
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
    private TableRules(
        int players,
        int requiredSeries,
        int requiredCleanSeries,
        bool setsAllowed,
        int jokerlessMultiplier)
    {
        Players = players;
        RequiredSeries = requiredSeries;
        RequiredCleanSeries = requiredCleanSeries;
        SetsAllowed = setsAllowed;
        JokerlessMultiplier = jokerlessMultiplier;
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
    /// What a <b>jokerless</b> declaration multiplies the round payment by at this table size
    /// (RULES.md §7.3): <b>2</b> at two, three or four seats and <b>3</b> at five or more. A
    /// declaration holding a joker anywhere pays ×1 — the flat value §7.2 has always named.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>This is the second rule in the game whose content changes with the number of
    /// players, and it splits at exactly the same seam as the first</b> — 2/3/4 against 5+,
    /// which is the line between <i>the table requires at least one series</i> and <i>the
    /// table requires nothing</i>. The bonus is largest exactly where the win condition asks
    /// least (RULES.md §7.3, `DERIVED`).
    /// </para>
    /// <para>
    /// ⚠️ <b>The qualifying condition is not <see cref="RequiredCleanSeries"/> and not
    /// <see cref="Meld.IsClean"/>.</b> Those implement §7.1.1's <i>required clean series</i>, a
    /// different rule that shares a word. §7.3 asks whether the declared <b>thirteen</b> hold a
    /// joker at all — a set counts exactly as a run does — which is
    /// <see cref="Money.Settlement.IsJokerless"/>. It is a property of the cards rather than of
    /// the partition, so it needs no evaluator and cannot depend on which cover was found.
    /// </para>
    /// <para>
    /// ⚠️ Six-plus paying ×3 is RULES.md §9 #37's recorded default, not a confirmed answer: the
    /// expert named <i>five players</i> and §7.1.1 groups five-or-more.
    /// </para>
    /// </remarks>
    public int JokerlessMultiplier { get; }

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
            2 => new TableRules(players, requiredSeries: 0, requiredCleanSeries: 0, setsAllowed: false, jokerlessMultiplier: 2),
            3 => new TableRules(players, requiredSeries: 2, requiredCleanSeries: 2, setsAllowed: true, jokerlessMultiplier: 2),
            4 => new TableRules(players, requiredSeries: 1, requiredCleanSeries: 1, setsAllowed: true, jokerlessMultiplier: 2),
            _ => new TableRules(players, requiredSeries: 0, requiredCleanSeries: 0, setsAllowed: true, jokerlessMultiplier: 3)
        };
    }

    /// <summary>
    /// Whether these rules ask anything of the partition beyond covering the hand — false
    /// only at five or more, where the win condition is the bare exact cover.
    /// </summary>
    /// <remarks>
    /// ⚠️ A deliberate observable with no production caller, kept so a test or a tool can ask
    /// the question directly. It is <b>not</b> the enforcement path: <c>HandEvaluator</c>
    /// enforces the requirements by carrying this table's counts along the cover search.
    /// </remarks>
    public bool ConstrainsThePartition =>
        !SetsAllowed || RequiredSeries > 0 || RequiredCleanSeries > 0;

    public override string ToString() =>
        $"{Players}-handed: {(SetsAllowed ? "runs or sets" : "runs only")}, " +
        $"{RequiredSeries} series required ({RequiredCleanSeries} clean), " +
        $"jokerless pays ×{JokerlessMultiplier}";
}

namespace BurmesePoker.Domain.Play;

/// <summary>
/// How many rounds in a row the last winner has now won (RULES.md §7.5).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The first thing in this game that is not a property of a round.</b> Every rule before
/// §7.5 settles a round from that round alone; a streak lives above one, which is why it is
/// counted by whatever owns a sequence of rounds (<see cref="MatchEngine"/>) and handed
/// <em>down</em> to the round rather than asked for by it.
/// </para>
/// <para>
/// ⚠️ <b>It counts wins and not seats.</b> The seating can move between the rounds of a streak —
/// by a house policy (P36) or because the table agreed to it (P37) — so the seat blamed for a
/// third consecutive win is read off the seating of <em>the round being settled</em>, which is
/// RULES.md §9 #46's recorded default. Nothing about the streak itself changes when the seats
/// do.
/// </para>
/// </remarks>
/// <param name="Player">Who won the last round, or null before any round has been won.</param>
/// <param name="Length">
/// How many in a row they have won. Zero exactly when <paramref name="Player"/> is null.
/// </param>
public readonly record struct WinStreak(PlayerId? Player, int Length)
{
    /// <summary>Nobody has won anything yet.</summary>
    public static readonly WinStreak None = default;

    /// <summary>
    /// How many consecutive wins RULES.md §7.5 blames the seat above for — <b>three</b>.
    /// </summary>
    public const int BlamedAt = 3;

    /// <summary>
    /// Whether a win by <paramref name="winner"/> right now would be their third in a row
    /// (or their fourth, or their tenth).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>It keeps firing</b>: a fourth consecutive win is blamed exactly as the third was.
    /// That is RULES.md §9 #41's recorded default and not a confirmed answer — the rule is
    /// stated as a property of a run rather than as a prize collected once. Fenced by
    /// <c>MatchEngineTests.TheStreakKeepsFiringUntilTheExpertSaysOtherwise</c>.
    /// </remarks>
    public bool BlamesTheSeatAboveIfWonBy(PlayerId winner) =>
        Player == winner && Length + 1 >= BlamedAt;

    /// <summary>The streak after <paramref name="winner"/> takes the next round.</summary>
    public WinStreak After(PlayerId winner) =>
        Player == winner ? new WinStreak(winner, Length + 1) : new WinStreak(winner, 1);

    public override string ToString() =>
        Player is { } player ? $"{player} × {Length}" : "no streak";
}

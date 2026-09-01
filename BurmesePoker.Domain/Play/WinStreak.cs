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
/// 🔥 <b>It counts wins, and a seat change ends the count.</b> The seating can move between
/// rounds — by a house policy (P36) or because the table agreed to it (P37) — and RULES.md §9
/// #46 was ruled on 2026-09-01: <em>a re-seating resets the streak</em>. So every round of a
/// run shares one seating, and the seat blamed is the seat above the winner in it. ⚠️ <b>The
/// question of which seating names the payer is therefore unreachable rather than defaulted</b>
/// — see <see cref="AfterAReseating"/>.
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
    /// ⚠️ <b>It asks about a run of three <em>unpaid</em> wins</b>, which is all it can ask:
    /// the count itself is put back to <see cref="None"/> the moment the blame is paid
    /// (RULES.md §9 #41, ruled by Nick on 2026-09-01 — <em>the streak resets when it pays</em>),
    /// so a fourth consecutive win is an ordinary round and it takes three more wins to fire
    /// again. The reset lives in <see cref="After(PlayerId, bool)"/>; this predicate is
    /// unchanged by the ruling and still reads <em>three or more</em>, because a run this
    /// object is still counting has never been paid for. Fenced by
    /// <c>MatchEngineTests.TheStreakResetsWhenItPaysUntilTheExpertSaysOtherwise</c>.
    /// </remarks>
    public bool BlamesTheSeatAboveIfWonBy(PlayerId winner) =>
        Player == winner && Length + 1 >= BlamedAt;

    /// <summary>The streak after <paramref name="winner"/> takes the next round.</summary>
    /// <param name="winner">Who won the round just settled.</param>
    /// <param name="blameWasPaid">
    /// Whether that round fired §7.5 — the seat above the winner having paid the whole of it.
    /// </param>
    /// <remarks>
    /// 🔥 <b>A round that paid the blame spends the run</b> (RULES.md §9 #41, ruled 2026-09-01):
    /// the count starts over at <see cref="None"/> rather than at a run of one, so the very next
    /// win is length 1 and the next firing is three wins away. ⚠️ <b>The reset is here and not in
    /// the caller</b> — <see cref="MatchEngine"/> says what happened and this type says what that
    /// leaves behind, which is the same division that keeps <see cref="Money.Settlement"/> told
    /// rather than counting.
    /// </remarks>
    public WinStreak After(PlayerId winner, bool blameWasPaid = false) =>
        blameWasPaid
            ? None
            : Player == winner ? new WinStreak(winner, Length + 1) : new WinStreak(winner, 1);

    /// <summary>
    /// The streak after the seats have been drawn again — <see cref="None"/>, whoever was
    /// winning (RULES.md §9 #46, ruled 2026-09-01).
    /// </summary>
    /// <remarks>
    /// 🔥 <b>A re-seating resets the streak</b>, which is what makes <em>which seating names the
    /// blamed seat</em> a question that cannot arise: a run can never span a seat change.
    /// ⚠️ <b>It is the event and not a comparison of orders</b> — a fresh draw may legitimately
    /// come out identical, and a table that got up and sat down again has changed seats whatever
    /// the result.
    /// </remarks>
    public WinStreak AfterAReseating() => None;

    public override string ToString() =>
        Player is { } player ? $"{player} × {Length}" : "no streak";
}

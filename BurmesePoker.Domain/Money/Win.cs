using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;

namespace BurmesePoker.Domain.Money;

/// <summary>
/// What this win <em>was</em> — everything RULES.md §7.2 step 1 needs about the winner beyond
/// the stakes and the table size.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Step 1 is the most amended sentence in the rules document, and this type is why it is
/// still one sentence.</b> The round payment was flat for twenty-four revisions; it is now not
/// flat in <em>how</em> the winner won (§7.3, a jokerless declaration), not flat in <em>when</em>
/// they won (§7.4, from the initial deal), not flat in <em>how many are playing</em> (§7.3
/// again), and since rev 27 it is not even a payment from everybody (§7.5, a third consecutive
/// win). ⚠️ <b>A fifth qualification should be expected rather than treated as a surprise</b>, so
/// the arithmetic takes a record of the win rather than a growing list of flags (BUILD-PLAN P35
/// build item 1).
/// </para>
/// <para>
/// ⚠️ <b>It is a reference type on purpose.</b> A record struct would default to
/// <c>(false, false, false)</c> — an ordinary flat win — so a caller who forgot it would settle
/// a bonus round as an ordinary one <em>in silence</em>, which is exactly the bug P33 removed
/// when it made the declared hand a required parameter of
/// <see cref="Settlement.ForRound"/>.
/// </para>
/// <para>
/// <b>Two of the three facts are not properties of the round at all.</b> Jokerlessness is read
/// off the thirteen laid down, but <see cref="FromTheInitialDeal"/> is a property of the shape
/// of the round and <see cref="ThirdConsecutiveWin"/> is a property of the <em>match</em> — the
/// first thing in this game that reaches outside a round (§7.5). Settlement is told the answer
/// rather than computing it, exactly as it is told whether the hand was jokerless.
/// </para>
/// </remarks>
/// <param name="Jokerless">
/// No joker anywhere in the declared thirteen, which pays ×2 at two, three or four seats and ×3
/// at five or more (RULES.md §7.3). See <see cref="Settlement.IsJokerless"/>.
/// </param>
/// <param name="FromTheInitialDeal">
/// The thirteen dealt to the winner already won, before anybody drew a card — which pays ×2
/// (RULES.md §7.4). ⚠️ <b>The dealt thirteen alone</b> is §9 #38's recorded default, not a
/// confirmed answer; the other reading is <em>the winner's first turn</em>, which is a far
/// commoner event.
/// </param>
/// <param name="ThirdConsecutiveWin">
/// The winner has now won three or more rounds in a row, so the seat immediately above them
/// pays the whole round payment and everybody else pays nothing (RULES.md §7.5).
/// ⚠️ <b>Three <em>or more</em></b> is §9 #41's recorded default — the rule is stated as a
/// property of a run rather than as a prize collected once.
/// </param>
public sealed record Win(bool Jokerless, bool FromTheInitialDeal, bool ThirdConsecutiveWin)
{
    /// <summary>An ordinary win: played for, jokered, and nobody's third in a row.</summary>
    /// <remarks>
    /// Named rather than defaulted, so that settling a round flat is something a caller
    /// <em>says</em>.
    /// </remarks>
    public static readonly Win Ordinary = new(false, false, false);

    /// <summary>
    /// What winning on the initial deal multiplies the round payment by (RULES.md §7.4).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Not in <see cref="TableRules"/>, and that is a decision.</b> §7.3's multiplier is a
    /// function of the number of players and lives in the per-seat-count table for that reason;
    /// this one is the same ×2 at every table size, and putting it beside the other would imply a
    /// seam it does not have (BUILD-PLAN P35 build item 1).
    /// </remarks>
    public const int DealBonusMultiplier = 2;

    /// <summary>
    /// The win as the winner's declared thirteen and the two facts the round and the match
    /// carry — the only constructor production code should need.
    /// </summary>
    /// <param name="declaredHand">
    /// The thirteen laid down. Passing the melds' cards instead is equivalent: they are the same
    /// thirteen (RULES.md §6.3).
    /// </param>
    /// <param name="fromTheInitialDeal">Nobody had played a card yet (RULES.md §7.4).</param>
    /// <param name="thirdConsecutiveWin">This is the winner's third in a row, or more (§7.5).</param>
    public static Win Declared(
        IEnumerable<Card> declaredHand,
        bool fromTheInitialDeal = false,
        bool thirdConsecutiveWin = false) =>
        new(Settlement.IsJokerless(declaredHand), fromTheInitialDeal, thirdConsecutiveWin);

    /// <summary>
    /// What the round value is multiplied by before anybody pays it (RULES.md §7.2 step 1, as
    /// amended by §7.3 and §7.4).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The two bonuses multiply</b> — a jokerless win from the deal at five seats pays ×6 —
    /// which is RULES.md §9 #39's recorded default and <b>not</b> a confirmed answer. The two
    /// rules pay for unrelated things and neither saying mentions the other; the competing
    /// reading is that <em>"double payout"</em> doubles the round <em>value</em> rather than the
    /// payout, which differs only once another multiplier exists. Fenced by
    /// <c>SettlementTests.TheTwoBonusesMultiplyUntilTheExpertSaysOtherwise</c>.
    /// </remarks>
    public int Multiplier(TableRules rules) =>
        (Jokerless ? rules.JokerlessMultiplier : 1) * (FromTheInitialDeal ? DealBonusMultiplier : 1);

    /// <summary>
    /// Whether the whole round payment falls on one seat — the one immediately above the winner
    /// in turn order (RULES.md §7.5).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>It applies to the round payment and not to the money cards</b> (§9 #44's recorded
    /// default), for the same reason the two multipliers do not (§9 #36, #40): §7.2 step 2 is a
    /// pairwise settlement in which the winner is one participant like anybody else, not a
    /// payout to them.
    /// </remarks>
    public bool PaidByTheSeatAboveAlone => ThirdConsecutiveWin;

    public override string ToString() =>
        (Jokerless, FromTheInitialDeal, ThirdConsecutiveWin) switch
        {
            (false, false, false) => "an ordinary win",
            _ => string.Join(
                ", ",
                new[]
                {
                    Jokerless ? "jokerless" : null,
                    FromTheInitialDeal ? "from the initial deal" : null,
                    ThirdConsecutiveWin ? "a third consecutive win" : null
                }.Where(part => part is not null))
        };
}

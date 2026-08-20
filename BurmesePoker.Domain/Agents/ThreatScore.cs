using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// How many still-available pairs of cards would put a card into a meld — the use a discard
/// is to whoever picks it up (BUILD-PLAN P15), and the one question in this solution whose
/// answer depends on <em>what is left in the shoe</em>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Extracted from <see cref="CautiousBotAgent"/> by P20, unchanged.</b> It moved because a
/// second rung needed it with one thing different — <see cref="CountingBotAgent"/> answers
/// <em>available</em> from what it has been shown rather than from its own hand — and two
/// copies of this arithmetic would be two places for the answer to drift, which is the reason
/// <see cref="CoverScore"/> exists at all.
/// </para>
/// <para>
/// 🔥 <b>The supply estimate is the whole of the difference between the two rungs</b>, which is
/// what makes a difference in results attributable to it and to nothing else (P15). Everything
/// below is shared: the windows, the pairing, and the ceiling of 24 that lets eight bits carry
/// the result under a tie-break key.
/// </para>
/// <para>
/// ⚠️ <b>The turned-up money cards are deliberately not an input to any of it.</b> They are
/// public and this seat has plainly seen them, but reading them would make which card
/// designates money an input to a discard, which RULES.md §4.4 says it must never be. That is a
/// rule, not an oversight, and it is why <see cref="Supply"/> is handed the cards to count
/// rather than reaching for a <c>TurnContext</c>.
/// </para>
/// </remarks>
internal static class ThreatScore
{
    /// <summary>How many copies of one card the two decks hold.</summary>
    internal const int CopiesInTheShoe = 2;

    /// <summary>The most this can ever come to: three pairs of suits and three windows, four ways each.</summary>
    internal const int Ceiling = 24;

    /// <summary>
    /// Every legal three-card window of consecutive ranks, in run order.
    /// </summary>
    /// <remarks>
    /// The ace is high or low but never both, so <c>A-2-3</c> and <c>Q-K-A</c> are here and
    /// <c>K-A-2</c> is not (RULES.md §6.1). Written out rather than computed, because the
    /// wrap-around this excludes is the exact defect the 2023 code shipped.
    /// </remarks>
    private static readonly Rank[][] RunWindows =
    [
        [Rank.Ace, Rank.Two, Rank.Three],
        [Rank.Two, Rank.Three, Rank.Four],
        [Rank.Three, Rank.Four, Rank.Five],
        [Rank.Four, Rank.Five, Rank.Six],
        [Rank.Five, Rank.Six, Rank.Seven],
        [Rank.Six, Rank.Seven, Rank.Eight],
        [Rank.Seven, Rank.Eight, Rank.Nine],
        [Rank.Eight, Rank.Nine, Rank.Ten],
        [Rank.Nine, Rank.Ten, Rank.Jack],
        [Rank.Ten, Rank.Jack, Rank.Queen],
        [Rank.Jack, Rank.Queen, Rank.King],
        [Rank.Queen, Rank.King, Rank.Ace]
    ];

    private static readonly Suit[] Suits = [Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades];

    /// <summary>
    /// How many copies of one value this player believes are still out there.
    /// </summary>
    /// <remarks>
    /// The one thing the two rungs that use this file disagree about.
    /// <see cref="NotInThisHand"/> is the estimate a player makes from its own cards alone;
    /// <see cref="CountingBotAgent"/> makes it from everything it has been shown.
    /// </remarks>
    internal delegate int Supply(Rank rank, Suit suit);

    /// <summary>
    /// How many still-available pairs of cards would put this one into a meld.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A meld needs three cards, so a card handed away is worth having only in company: the
    /// count is over <b>pairs</b>, not partners, which is what makes it more than
    /// <see cref="CoverScore.Potential"/> rewritten. A rank whose set is blocked in two suits
    /// at once scores lower than the sum of the two blockages suggests, and a rank at the end
    /// of the ladder sits in fewer runs however the hand falls.
    /// </para>
    /// </remarks>
    internal static int Of(Card card, Supply available)
    {
        if (card.IsJoker)
        {
            return int.MaxValue;
        }

        var rank = card.Rank!.Value;
        var suit = card.Suit!.Value;
        var threat = 0;

        // Sets: any two of the three suits this card is not, since duplicate suits are
        // forbidden (RULES.md §6.2).
        for (var one = 0; one < Suits.Length; one++)
        {
            if (Suits[one] == suit)
            {
                continue;
            }

            for (var other = one + 1; other < Suits.Length; other++)
            {
                if (Suits[other] == suit)
                {
                    continue;
                }

                threat += available(rank, Suits[one]) * available(rank, Suits[other]);
            }
        }

        // Runs: every window this rank falls in, needing the two ranks it does not hold.
        foreach (var window in RunWindows)
        {
            if (Array.IndexOf(window, rank) < 0)
            {
                continue;
            }

            var ways = 1;

            foreach (var needed in window)
            {
                if (needed != rank)
                {
                    ways *= available(needed, suit);
                }
            }

            threat += ways;
        }

        return threat;
    }

    /// <summary>
    /// The supply a player can estimate from its own cards alone: everything not in this hand
    /// might still be out there.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>It is knowingly optimistic, and P20 is the packet that says by how much.</b> A card
    /// this player threw away four turns ago is counted here as available again — which is
    /// true of the game only if nobody remembers anything.
    /// </remarks>
    internal static Supply NotInThisHand(IReadOnlyList<Card> hand) =>
        (rank, suit) =>
        {
            var held = 0;

            foreach (var card in hand)
            {
                if (card.Rank == rank && card.Suit == suit)
                {
                    held++;
                }
            }

            return Math.Max(0, CopiesInTheShoe - held);
        };
}

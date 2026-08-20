using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// How many of the values still out there would make this hand better if it drew one — the
/// first question in the ladder that is <b>combinatorial</b> rather than a sum over cards
/// (BUILD-PLAN P15, P21).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Why it has to be this and not another tie-break.</b> P15 proved that every
/// <em>pairwise-additive</em> measure is <see cref="GreedyBotAgent"/> again under another name:
/// partnership is symmetric, so the total partnership of the twelve kept is the hand's total
/// less twice the thrown card's, and "throw the fewest partners" and "keep the best-connected"
/// are the same rule. A count of live outs is not a sum over pairs at all — it asks, of each
/// value in turn, a question about the <em>whole</em> arrangement of the thirteen that would be
/// left, and two cards with identical partners can leave different numbers of outs behind.
/// </para>
/// <para>
/// <b>A value, not a card.</b> The two decks hold two copies of everything, so what a hand is
/// waiting for is a <em>value</em> — and the two jokers of a deck are interchangeable in every
/// meld there is, so all four count as one out and not four. What is counted is therefore the
/// 52 ranked values plus the joker, and never a <see cref="CardId"/>.
/// </para>
/// <para>
/// ⚠️ <b>Live means "not both copies in this hand", and nothing more.</b> This rung is
/// <see cref="GreedyBotAgent"/> with one decision changed (P15), so it remembers nothing and
/// sees nothing that greedy does not: the supply estimate is
/// <see cref="ThreatScore.NotInThisHand"/>, the same knowingly-optimistic one
/// <see cref="CautiousBotAgent"/> makes. <see cref="CountingBotAgent"/> is what a memory looks
/// like, and P20 measured what it is worth.
/// </para>
/// <para>
/// 🔥 <b>The cost is the packet.</b> A <see cref="PartialCover.Best"/> per value per candidate
/// is roughly 100× a greedy decision, so the work here is arranged around three prunes, in the
/// order they are cheap: the caller scores only the candidates already tied on cover count
/// (<see cref="CoverScore.Ranking"/>), <see cref="CouldJoinAMeld"/> throws out every value that
/// could not enter a meld at all, and <see cref="OutsCache"/> remembers an answer by the
/// <em>values</em> of the fourteen it was asked about. All three are <b>around</b> the
/// evaluator and none is inside it: <see cref="HandEvaluator.IsWinning"/> is the win authority
/// and its answers may not change (BUILD-PLAN §3.4).
/// </para>
/// </remarks>
internal static class LiveOuts
{
    /// <summary>
    /// A <see cref="CardId"/> no card of the shoe can have (it deals 0..107), so a probe can
    /// never be mistaken for a card the hand is holding.
    /// </summary>
    private const int ProbeId = 1_000;

    /// <summary>Jokers in the shoe: two to a deck (RULES.md §2).</summary>
    private const int JokersInTheShoe = 4;

    /// <summary>How far apart two ranks of a suit can be and still share a run of three.</summary>
    private const int RunReach = 2;

    /// <summary>
    /// How many distinct values would raise the cover count of these thirteen cards.
    /// </summary>
    /// <param name="kept">The thirteen that would be left after the discard being judged.</param>
    /// <param name="covered">
    /// What <see cref="CoverScore.Covered"/> already said about them — passed in rather than
    /// recomputed, because the caller has just paid for it.
    /// </param>
    /// <param name="cache">Answers this seat has already bought, or null to buy every one afresh.</param>
    internal static int Count(IReadOnlyList<Card> kept, int covered, OutsCache? cache = null)
    {
        var available = ThreatScore.NotInThisHand(kept);
        var search = new CoverProbe(kept);
        var bar = covered + 1;
        var outs = 0;

        // A joker fits anywhere, so it is never pruned — only asked.
        if (JokersHeld(kept) < JokersInTheShoe
            && Improves(search, kept, Card.Joker(new CardId(ProbeId), CardColor.Red), bar, cache))
        {
            outs++;
        }

        foreach (var suit in CardText.AllSuits)
        {
            foreach (var rank in CardText.AllRanks)
            {
                if (available(rank, suit) == 0 || !CouldJoinAMeld(rank, suit, kept))
                {
                    continue;
                }

                if (Improves(search, kept, Card.Ranked(new CardId(ProbeId), rank, suit), bar, cache))
                {
                    outs++;
                }
            }
        }

        return outs;
    }

    /// <summary>
    /// Would the fourteenth card leave more of the hand melded than the thirteen do?
    /// </summary>
    /// <remarks>
    /// <b>Asked as a bar to clear rather than as a score to compute</b>
    /// (<see cref="CoverProbe.CoversAtLeastWith"/>), because that is the difference between a
    /// rung that can be run and one that cannot.
    /// </remarks>
    private static bool Improves(
        CoverProbe search,
        IReadOnlyList<Card> kept,
        Card drawn,
        int bar,
        OutsCache? cache) =>
        cache is null
            ? search.CoversAtLeastWith(drawn, bar)
            : cache.CoversAtLeast(search, kept, drawn, bar);

    /// <summary>
    /// Could a meld containing this value be built out of this hand at all?
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>The prune has to be sound, and the reasoning is the licence for it.</b> A meld is
    /// three cards or more (RULES.md §6), so a meld containing the drawn value contains at
    /// least two others out of the thirteen. In a <b>set</b> those two are the same rank in
    /// other suits, or jokers. In a <b>run</b> they are the same suit, or jokers — and whichever
    /// end of the run the value sits at, two of the run's other places are within
    /// <see cref="RunReach"/> of it. So a value with fewer than two cards in the hand that could
    /// possibly sit beside it cannot raise the count, whatever the arrangement.
    /// </para>
    /// <para>
    /// It is deliberately <b>loose rather than tight</b>: a value that passes may still turn out
    /// to improve nothing, and only the full search settles that. What matters is that a value
    /// it rejects never could — <c>ThePruneNeverThrowsAwayARealOut</c> is the test that says so,
    /// against the unpruned count over real hands.
    /// </para>
    /// </remarks>
    private static bool CouldJoinAMeld(Rank rank, Suit suit, IReadOnlyList<Card> kept)
    {
        var companions = 0;

        foreach (var held in kept)
        {
            var fits = held.IsJoker
                || (held.Rank == rank && held.Suit != suit)
                || (held.Suit == suit && Reaches(rank, held.Rank!.Value));

            if (fits && ++companions == 2)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Near enough in the same suit to share a run — and not the same rank, since a run holds
    /// consecutive ranks and a set forbids a duplicate suit (RULES.md §6.1, §6.2), so a second
    /// copy of the very same card can never sit beside the first.
    /// </summary>
    private static bool Reaches(Rank rank, Rank held)
    {
        var distance = CoverScore.RunDistance(rank, held);

        return distance > 0 && distance <= RunReach;
    }

    private static int JokersHeld(IReadOnlyList<Card> kept)
    {
        var held = 0;

        foreach (var card in kept)
        {
            if (card.IsJoker)
            {
                held++;
            }
        }

        return held;
    }
}

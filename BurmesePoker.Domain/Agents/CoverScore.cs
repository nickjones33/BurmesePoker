using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// The one question every bot asks: <em>of the cards I would be left holding, how many meld?</em>
/// — together with the one comparison every bot resolves it with.
/// </summary>
/// <remarks>
/// <para>
/// Shared rather than copied, for the reason <c>MeldIndex</c> is shared between the two cover
/// searches: two copies of "does this card improve my hand" would be two places for the
/// answer to drift. A strategy's character lives in what it does with the score — the
/// tie-breaks, the risk it takes — not in the score itself.
/// </para>
/// <para>
/// <b>P15 made that literal.</b> Every rung of the ladder throws its card through
/// <see cref="Discard"/>, differing only in the tie-break it hands over, so a difference in
/// results attributes to that one function and to nothing else — which is the discipline the
/// whole comparison rests on (BUILD-PLAN P15).
/// </para>
/// </remarks>
internal static class CoverScore
{
    /// <summary>How many of these cards a best partial cover accounts for.</summary>
    internal static int Covered(IReadOnlyList<Card> hand) => PartialCover.Best(hand).CoveredCount;

    /// <summary>
    /// Would taking this card raise the count?
    /// </summary>
    /// <remarks>
    /// <b>Asked of the fourteen rather than of the thirteen that would be kept</b> — the same
    /// answer for a fourteenth of the work, because any improvement must use the new card, and
    /// every fourteen-card arrangement has a meld of four or more to give a card back from.
    /// </remarks>
    internal static bool Improves(IReadOnlyList<Card> hand, Card card) =>
        Covered([.. hand, card]) > Covered(hand);

    /// <summary>The hand without that exact card — instance identity, not value (BUILD-PLAN §3.1).</summary>
    internal static List<Card> Without(IReadOnlyList<Card> hand, Card card)
    {
        var kept = new List<Card>(hand.Count - 1);
        foreach (var held in hand)
        {
            if (held != card)
            {
                kept.Add(held);
            }
        }

        return kept;
    }

    /// <summary>
    /// Throw whichever card costs the fewest melded cards, breaking ties with the given
    /// preference — lower wins, and an equal preference leaves the first card seen.
    /// </summary>
    /// <remarks>
    /// <b>The head of <see cref="Ranking"/>, and defined as it</b> rather than as a second loop
    /// that agrees with it by inspection. P19 needed the runner-up as well as the winner, and
    /// two orderings that had to match would have been two places for the answer to drift —
    /// which is the reason this file exists at all.
    /// </remarks>
    internal static Card Discard(IReadOnlyList<Card> hand, Func<Card, IReadOnlyList<Card>, long> tieBreak) =>
        Ranking(hand, tieBreak) is [var best, ..]
            ? best
            : throw new InvalidOperationException("Asked to discard from an empty hand.");

    /// <summary>
    /// Every card worth considering throwing, <b>best first</b>: fewest melded cards lost,
    /// ties to the given preference, and an equal preference leaving the first card seen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two copies of one value are one candidate.</b> They leave the same thirteen behind,
    /// so the second is the first's answer already — and skipping it costs a
    /// <see cref="PartialCover.Best"/> call, which is the expensive thing here.
    /// </para>
    /// <para>
    /// The tie-break is a <see cref="long"/> so that a rung can pack more than one key into
    /// it; <see cref="NoPreference"/> is the constant that makes the order "first one wins".
    /// </para>
    /// <para>
    /// ⚠️ <b>The sort has to be stable, and that is load-bearing rather than tidy.</b> Where
    /// cost and preference both tie the order is the hand's own, which is what
    /// <see cref="SimpleBotAgent"/> plays by and what every seed published before P19 was
    /// dealt against. <see cref="Enumerable.OrderBy{TSource,TKey}(IEnumerable{TSource},Func{TSource,TKey})"/>
    /// is stable; <see cref="List{T}.Sort()"/> is not.
    /// </para>
    /// <para>
    /// 🔥 <b>Why a ranking and not just a winner</b> (BUILD-PLAN P19): a difficulty level is the
    /// strongest rung with a mistake rate, and a mistake that is worth making is <em>the next
    /// move down the agent's own ordering</em> rather than a random legal one. A bot that threw
    /// a joker away would read as broken instead of as weak, and second-best by the rung's own
    /// key can never be that unless the whole hand is jokers.
    /// </para>
    /// </remarks>
    internal static IReadOnlyList<Card> Ranking(
        IReadOnlyList<Card> hand,
        Func<Card, IReadOnlyList<Card>, long> tieBreak)
    {
        var judged = new List<Card>(hand.Count);
        var scored = new List<(Card Card, int Cost, long Preference)>(hand.Count);

        foreach (var card in hand)
        {
            if (judged.Exists(seen => seen.SameValueAs(card)))
            {
                continue;
            }

            judged.Add(card);
            scored.Add((card, Covered(Without(hand, card)), tieBreak(card, hand)));
        }

        return
        [
            .. scored
                .OrderByDescending(candidate => candidate.Cost)
                .ThenBy(candidate => candidate.Preference)
                .Select(candidate => candidate.Card)
        ];
    }

    /// <summary>A tie-break that breaks no ties: the first card of equal cost wins.</summary>
    internal static readonly Func<Card, IReadOnlyList<Card>, long> NoPreference = static (_, _) => 0;

    /// <summary>
    /// How much of a partner this card has in the hand it is held with — the higher, the more
    /// reason to keep it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It prefers to keep cards with partners in the hand (another suit of the same rank, or a
    /// neighbour in the same suit), and keeps jokers over everything, since a joker fits
    /// anywhere. A duplicate of the very same card counts for nothing: duplicate suits are
    /// forbidden in a set (RULES.md §6.2), so a second 9♥ is no nearer a meld than none.
    /// </para>
    /// <para>
    /// ⚠️ <b>It is symmetric, and that matters more than it looks.</b> If <c>a</c> is a partner
    /// of <c>b</c> then <c>b</c> is a partner of <c>a</c>, so the total partnership of the
    /// twelve cards kept is the hand's total less twice this card's — which means "throw the
    /// card with the fewest partners" and "keep the best-connected twelve" are the same rule,
    /// and a rung that tried to improve on it by looking at the whole kept hand would be
    /// <see cref="GreedyBotAgent"/> again under another name (BUILD-PLAN P15).
    /// </para>
    /// </remarks>
    internal static int Potential(Card card, IReadOnlyList<Card> hand)
    {
        if (card.IsJoker)
        {
            return int.MaxValue;
        }

        var potential = 0;

        foreach (var other in hand)
        {
            if (other.Id == card.Id || other.IsJoker)
            {
                continue;
            }

            if (other.Rank == card.Rank)
            {
                // Same rank, another suit, and so a set two thirds of the way there.
                potential += other.Suit == card.Suit ? 0 : 2;
            }
            else if (other.Suit == card.Suit)
            {
                potential += RunDistance(card.Rank!.Value, other.Rank!.Value) switch
                {
                    1 => 2,
                    2 => 1,
                    _ => 0
                };
            }
        }

        return potential;
    }

    /// <summary>
    /// How far apart two ranks are within a run, counting the ace as high or low but never
    /// both at once (RULES.md §6.1).
    /// </summary>
    internal static int RunDistance(Rank one, Rank other)
    {
        const int aceLow = 1;

        var straight = Math.Abs((int)one - (int)other);

        return one == Rank.Ace
            ? Math.Min(straight, Math.Abs(aceLow - (int)other))
            : other == Rank.Ace
                ? Math.Min(straight, Math.Abs((int)one - aceLow))
                : straight;
    }
}

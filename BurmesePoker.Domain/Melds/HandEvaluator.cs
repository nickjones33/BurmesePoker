using System.Numerics;
using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Melds;

/// <summary>
/// The only win authority in the codebase: whether a hand partitions into disjoint melds that
/// use <b>every</b> card exactly once (RULES.md §7.1 — all 13 cards must be melded).
/// </summary>
/// <remarks>
/// <para>
/// Declaring is <b>not</b> the question a meld generator answers (BUILD-PLAN §3.4). Candidates
/// overlap by design; going out asks whether some subset of them is an <b>exact cover</b> of
/// the hand. Enumeration is an input to that question, never an answer to it — which is why
/// the retired 2023 <c>CardPlaysFactory</c> was replaced rather than repaired.
/// </para>
/// <para>
/// The search is recursive backtracking pinned to the <b>lowest uncovered card</b>. Since
/// every card below it is already covered, any meld that covers it consists entirely of cards
/// at or above it — so indexing the candidates by their lowest card is both a filter and the
/// thing that stops the search re-exploring permutations of a cover it has already tried.
/// A hand of nine consecutive cards in one suit plus four jokers produces over four thousand
/// candidates, so the index is not a nicety.
/// </para>
/// <para>
/// Coverage is tracked as a bit per card rather than a set of <see cref="CardId"/>s, which
/// makes a dead end memoisable: a covered-set that has been proved unfinishable is never
/// explored again, bounding the search at 2^n states.
/// </para>
/// </remarks>
public static class HandEvaluator
{
    /// <summary>
    /// The largest hand the evaluator accepts — one bit per card in a <see cref="ulong"/>.
    /// The game deals 13 and never holds more than 14, so this is a guard, not a limit.
    /// </summary>
    public const int MaximumHandSize = 64;

    /// <summary>
    /// Whether the hand can be laid down: every card melded, no card used twice
    /// (RULES.md §7.1).
    /// </summary>
    public static bool IsWinning(IReadOnlyList<Card> hand) => TryFindCover(hand, out _);

    /// <summary>
    /// Finds one exact cover of the hand, so a declaration can be displayed and audited.
    /// </summary>
    /// <param name="melds">
    /// Pairwise disjoint melds whose cards are exactly the hand, or empty when there is no
    /// cover. Which cover is found is unspecified when there is more than one.
    /// </param>
    /// <returns>Whether a cover exists. An empty hand is covered by no melds at all.</returns>
    public static bool TryFindCover(IReadOnlyList<Card> hand, out IReadOnlyList<Meld> melds)
    {
        ArgumentNullException.ThrowIfNull(hand);
        if (hand.Count > MaximumHandSize)
        {
            throw new ArgumentException(
                $"A hand of more than {MaximumHandSize} cards cannot be evaluated.", nameof(hand));
        }

        // Ascending CardId order, so "the lowest uncovered card" is bit index order.
        var ordered = hand.OrderBy(card => card.Id.Value).ToArray();
        var position = new Dictionary<CardId, int>(ordered.Length);
        for (var index = 0; index < ordered.Length; index++)
        {
            if (!position.TryAdd(ordered[index].Id, index))
            {
                throw new ArgumentException(
                    "A hand cannot hold the same card instance twice.", nameof(hand));
            }
        }

        var full = ordered.Length == MaximumHandSize
            ? ulong.MaxValue
            : (1UL << ordered.Length) - 1;

        var byLowestCard = new List<(Meld Meld, ulong Mask)>[ordered.Length];
        for (var index = 0; index < byLowestCard.Length; index++)
        {
            byLowestCard[index] = [];
        }

        foreach (var meld in MeldCandidates.For(hand))
        {
            var mask = 0UL;
            foreach (var id in meld.CardIds)
            {
                mask |= 1UL << position[id];
            }

            byLowestCard[BitOperations.TrailingZeroCount(mask)].Add((meld, mask));
        }

        var chosen = new List<Meld>();
        var exhausted = new HashSet<ulong>();

        if (Search(0UL))
        {
            melds = chosen;
            return true;
        }

        melds = [];
        return false;

        bool Search(ulong covered)
        {
            if (covered == full)
            {
                return true;
            }

            if (exhausted.Contains(covered))
            {
                return false;
            }

            // Pin to the lowest uncovered card: it has to be melded somehow, and only melds
            // whose lowest card it is can still do it.
            var lowest = BitOperations.TrailingZeroCount(~covered & full);
            foreach (var (meld, mask) in byLowestCard[lowest])
            {
                if ((covered & mask) != 0)
                {
                    continue;
                }

                chosen.Add(meld);
                if (Search(covered | mask))
                {
                    return true;
                }

                chosen.RemoveAt(chosen.Count - 1);
            }

            exhausted.Add(covered);
            return false;
        }
    }
}

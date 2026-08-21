using System.Numerics;
using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Melds;

/// <summary>
/// The only win authority in the codebase: whether a hand partitions into disjoint melds that
/// use <b>every</b> card exactly once (RULES.md §7.1) <b>and satisfy what the table size asks
/// of the partition</b> (§7.1.1).
/// </summary>
/// <remarks>
/// <para>
/// Declaring is <b>not</b> the question a meld generator answers (BUILD-PLAN §3.4). Candidates
/// overlap by design; going out asks whether some subset of them is an <b>exact cover</b> of
/// the hand. Enumeration is an input to that question, never an answer to it — which is why
/// the retired 2023 <c>CardPlaysFactory</c> was replaced rather than repaired.
/// </para>
/// <para>
/// 🔥 <b>The table size is not a filter over the answer, and that is the whole difficulty</b>
/// (BUILD-PLAN P25). Since a longer run may be laid down split (RULES.md §9 #23), the series
/// requirement is a property of <b>the partition chosen</b> rather than of the hand: the same
/// thirteen cards can have one cover that satisfies a three-handed table and another that does
/// not. So the search carries the counts still owing <em>along</em> the partition, and can no
/// longer return the first cover it finds and audit it afterwards.
/// </para>
/// <para>
/// The search is recursive backtracking pinned to the <b>lowest uncovered card</b>, over the
/// <see cref="MeldIndex"/> — since every card below it is already covered, any meld that
/// covers it consists entirely of cards at or above it.
/// </para>
/// <para>
/// Coverage is tracked as a bit per card rather than a set of <see cref="CardId"/>s, which
/// makes a dead end memoisable: a state that has been proved unfinishable is never explored
/// again, bounding the search at 2^n states. ⚠️ <b>The state is now the covered-set
/// <em>and</em> what is still owing</b>, because a covered-set from which no cover can supply
/// two more clean series may perfectly well supply one.
/// </para>
/// <para>
/// <b>A hand that cannot be covered exactly comes back with nothing at all</b>, which is every
/// hand a player actually holds. Asking how <em>close</em> a hand is — what a bot needs on
/// every turn — is <see cref="PartialCover"/>, deliberately a separate type: this one is the
/// win authority and its answers may not change (BUILD-PLAN §3.4).
/// </para>
/// </remarks>
public static class HandEvaluator
{
    /// <summary>
    /// The largest hand the evaluator accepts — one bit per card in a <see cref="ulong"/>.
    /// The game deals 13 and never holds more than 14, so this is a guard, not a limit.
    /// </summary>
    public const int MaximumHandSize = MeldIndex.MaximumHandSize;

    /// <summary>
    /// Whether the hand can be laid down at this table: every card melded, no card used
    /// twice (RULES.md §7.1), and the partition containing what <paramref name="rules"/>
    /// requires of it (§7.1.1).
    /// </summary>
    public static bool IsWinning(IReadOnlyList<Card> hand, TableRules rules) =>
        TryFindCover(hand, rules, out _);

    /// <summary>
    /// Finds one exact cover of the hand, so a declaration can be displayed and audited.
    /// </summary>
    /// <param name="melds">
    /// Pairwise disjoint melds whose cards are exactly the hand, or empty when there is no
    /// cover. Which cover is found is unspecified when there is more than one.
    /// </param>
    /// <returns>
    /// Whether a cover exists that satisfies <paramref name="rules"/>. An empty hand is
    /// covered by no melds at all, so it wins only where nothing is required of the partition.
    /// </returns>
    public static bool TryFindCover(
        IReadOnlyList<Card> hand, TableRules rules, out IReadOnlyList<Meld> melds)
    {
        // Sets are the one requirement that is a property of a meld rather than of the
        // partition, so two-handed prunes the candidates and the search never sees them.
        var index = MeldIndex.Build(hand, rules.SetsAllowed);

        var chosen = new List<Meld>();
        var exhausted = new HashSet<(ulong Covered, int Series, int Clean)>();

        if (Search(0UL, rules.RequiredSeries, rules.RequiredCleanSeries))
        {
            melds = chosen;
            return true;
        }

        melds = [];
        return false;

        // `series` and `clean` are what is still owing, never negative — so a state is the
        // covered-set together with the requirement it still has to discharge.
        bool Search(ulong covered, int series, int clean)
        {
            var uncovered = ~covered & index.Full;

            if (uncovered == 0)
            {
                return series == 0 && clean == 0;
            }

            // Every meld still to come takes at least three cards, so a hand with fewer than
            // three per series still owing cannot pay for them however it is arranged.
            if (BitOperations.PopCount(uncovered) < 3 * Math.Max(series, clean))
            {
                return false;
            }

            if (exhausted.Contains((covered, series, clean)))
            {
                return false;
            }

            // Pin to the lowest uncovered card: it has to be melded somehow, and only melds
            // whose lowest card it is can still do it.
            var lowest = BitOperations.TrailingZeroCount(uncovered);
            foreach (var (meld, mask) in index.ByLowestCard[lowest])
            {
                if ((covered & mask) != 0)
                {
                    continue;
                }

                // A clean series discharges both counts; an impure one — an all-joker meld
                // included — discharges the series count alone (RULES.md §9 #28, #29).
                var stillSeries = meld.Kind == MeldKind.Run ? Math.Max(0, series - 1) : series;
                var stillClean = meld.IsClean ? Math.Max(0, clean - 1) : clean;

                chosen.Add(meld);
                if (Search(covered | mask, stillSeries, stillClean))
                {
                    return true;
                }

                chosen.RemoveAt(chosen.Count - 1);
            }

            exhausted.Add((covered, series, clean));
            return false;
        }
    }
}

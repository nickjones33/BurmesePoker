using System.Numerics;
using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Melds;

/// <summary>
/// How close a hand is: the disjoint melds that cover as many of its cards as any arrangement
/// can, and the cards left over.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not the win authority.</b> <see cref="HandEvaluator"/> is (BUILD-PLAN §3.4), and
/// it is all-or-nothing: it returns nothing at all for a hand that cannot be covered exactly,
/// which is every hand anybody actually holds. A player choosing what to throw away needs the
/// other question — <em>how much of this hand melds?</em> — so this is the same backtracking
/// over the same <see cref="MeldIndex"/>, maximising cards covered instead of demanding all
/// thirteen.
/// </para>
/// <para>
/// ⚠️ <b>Since P25 a complete cover is not the same thing as a win, and this type does not
/// know the difference.</b> <see cref="IsComplete"/> agrees with
/// <see cref="HandEvaluator.IsWinning"/> only at five or more players, where nothing is asked
/// of the partition; at two, three or four seats a hand can cover exactly and still lose
/// (RULES.md §7.1.1). That is deliberate — this is a count of cards, an input to a decision
/// about what to throw away, and the win authority is the other type.
/// </para>
/// <para>
/// The search is the evaluator's walk with one extra branch. At the lowest card not yet
/// settled it may either take some meld that covers it, or <b>give the card up</b> and move
/// on — the branch the exact-cover search does not have. Memoising on
/// <c>(position, covered)</c> keeps that from doubling the work; the best arrangement seen at
/// a leaf is kept, and a complete cover stops the search where it stands, so a winning hand
/// costs no more here than it does in the evaluator.
/// </para>
/// <para>
/// Which of several equally-large arrangements comes back is unspecified, exactly as it is
/// for <see cref="HandEvaluator.TryFindCover"/>. Only <see cref="CoveredCount"/> is a promise.
/// </para>
/// </remarks>
public sealed class PartialCover
{
    private PartialCover(IReadOnlyList<Meld> melds, IReadOnlyList<Card> uncovered)
    {
        Melds = melds;
        Uncovered = uncovered;
    }

    /// <summary>Pairwise disjoint melds, covering as many cards of the hand as possible.</summary>
    public IReadOnlyList<Meld> Melds { get; }

    /// <summary>
    /// The cards no meld took — the deadwood. In hand order, ascending
    /// <see cref="CardId"/>.
    /// </summary>
    public IReadOnlyList<Card> Uncovered { get; }

    /// <summary>How many of the hand's cards the melds account for. The score of the hand.</summary>
    public int CoveredCount => Melds.Sum(meld => meld.Count);

    /// <summary>
    /// Whether every card is covered. ⚠️ <b>Not the same question as winning</b> — that also
    /// asks what the partition contains, and the answer depends on the table size
    /// (RULES.md §7.1.1). <see cref="HandEvaluator.IsWinning"/> is the authority.
    /// </summary>
    public bool IsComplete => Uncovered.Count == 0;

    /// <summary>
    /// Could <paramref name="target"/> of these cards be melded at once? — the same question
    /// <see cref="Best"/> answers, asked as a <b>yes or no</b>, which is a far cheaper thing to
    /// ask.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>It exists because a rung that looks ahead asks it hundreds of times a turn</b>
    /// (BUILD-PLAN P21). <em>Would this card improve my hand?</em> is not "how good could this
    /// hand be" — it is "can it beat what I already have", and a search that only has to clear
    /// a bar can stop the moment it clears it and can abandon any branch that cannot reach it.
    /// <see cref="Best"/> can do neither, because it does not know what it is looking for until
    /// it has found it.
    /// </para>
    /// <para>
    /// <b>Two prunes, and the second is the one that pays.</b> The walk stops at the first
    /// arrangement that reaches the bar; and at any point, everything still to come is at most
    /// one card each, so a branch whose cards covered so far plus the cards left cannot reach
    /// the bar is abandoned unexplored. The failures are memoised rather than the values —
    /// sound because the bar is fixed for the whole search, so "how many more are still needed"
    /// is decided by <c>covered</c> alone.
    /// </para>
    /// <para>
    /// ⚠️ <b>This is a speed-up placed <em>beside</em> the evaluator and never inside it</b>
    /// (BUILD-PLAN §3.4, §3.7 item 4). <see cref="Best"/> is untouched, so every arrangement
    /// this solution has ever published is the arrangement it published before, and
    /// <see cref="HandEvaluator"/> — the win authority — does not know this method exists.
    /// What makes it trustworthy is that it is asserted against <see cref="Best"/> rather than
    /// reasoned about: <c>TheYesOrNoSearchAgreesWithTheFullOne</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// The hand is larger than <see cref="HandEvaluator.MaximumHandSize"/>, or holds the same
    /// physical card twice.
    /// </exception>
    public static bool CoversAtLeast(IReadOnlyList<Card> hand, int target)
    {
        if (target <= 0)
        {
            return true;
        }

        var index = MeldIndex.Build(hand);

        if (target > index.Count)
        {
            return false;
        }

        var hopeless = new HashSet<(int Position, ulong Covered)>();

        return Reaches(0, 0UL);

        bool Reaches(int position, ulong covered)
        {
            var have = BitOperations.PopCount(covered);

            if (have >= target)
            {
                return true;
            }

            // Everything from here on is one card at best, so a branch that cannot reach the
            // bar even by covering all of it is not worth walking.
            if (position == index.Count || have + index.Count - position < target)
            {
                return false;
            }

            if ((covered & (1UL << position)) != 0)
            {
                return Reaches(position + 1, covered);
            }

            if (hopeless.Contains((position, covered)))
            {
                return false;
            }

            // Leave this card uncovered, exactly as Best may.
            if (Reaches(position + 1, covered))
            {
                return true;
            }

            foreach (var (_, mask) in index.ByLowestCard[position])
            {
                if ((covered & mask) == 0 && Reaches(position + 1, covered | mask))
                {
                    return true;
                }
            }

            hopeless.Add((position, covered));

            return false;
        }
    }

    /// <summary>
    /// The largest cover the hand allows.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The hand is larger than <see cref="HandEvaluator.MaximumHandSize"/>, or holds the same
    /// physical card twice.
    /// </exception>
    public static PartialCover Best(IReadOnlyList<Card> hand)
    {
        var index = MeldIndex.Build(hand);

        var chosen = new List<Meld>();
        var memo = new Dictionary<(int Position, ulong Covered), int>();

        var bestCount = -1;
        var bestMelds = (IReadOnlyList<Meld>)[];
        var bestCovered = 0UL;
        var finished = false;

        Search(0, 0UL);

        return new PartialCover(bestMelds, index.CardsOutside(bestCovered));

        // How many more cards can be covered from here on, looking at cards at or above
        // `position` only. Everything below it has been settled one way or the other.
        int Search(int position, ulong covered)
        {
            if (finished)
            {
                return 0;
            }

            if (position == index.Count)
            {
                var count = BitOperations.PopCount(covered);

                if (count > bestCount)
                {
                    bestCount = count;
                    bestMelds = [.. chosen];
                    bestCovered = covered;
                    finished = count == index.Count;
                }

                return 0;
            }

            if ((covered & (1UL << position)) != 0)
            {
                return Search(position + 1, covered);
            }

            if (memo.TryGetValue((position, covered), out var known))
            {
                return known;
            }

            // The branch the exact-cover search does not have: leave this card uncovered.
            var best = Search(position + 1, covered);

            foreach (var (meld, mask) in index.ByLowestCard[position])
            {
                if ((covered & mask) != 0)
                {
                    continue;
                }

                chosen.Add(meld);
                var gained = BitOperations.PopCount(mask) + Search(position + 1, covered | mask);
                chosen.RemoveAt(chosen.Count - 1);

                if (gained > best)
                {
                    best = gained;
                }
            }

            // A memo written while unwinding from a completed cover would be a partial answer
            // recorded as a final one. Nothing else will read it, but nothing should.
            if (!finished)
            {
                memo[(position, covered)] = best;
            }

            return best;
        }
    }
}

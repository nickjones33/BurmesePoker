using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Melds;

/// <summary>
/// Generates every run candidate a hand can make: three or more cards of one suit in
/// contiguous rank order, with jokers free to stand in for any position (RULES.md §6.1).
/// </summary>
/// <remarks>
/// <para>
/// Generation is <b>by window</b>, never by a greedy walk (BUILD-PLAN §3.4): for each suit,
/// each window of contiguous ranks of length three or more, every way of filling each
/// position with either a held card of that rank and suit or a specific joker instance. The
/// window formulation is what makes joker substitution fall out for free, and it is why a
/// hand of all thirteen ranks in one suit terminates rather than looping forever.
/// </para>
/// <para>
/// <b>Substitutions matter even when the real card is held.</b> From <c>2♦ 3♦ 4♦ 🃏</c> the
/// generator emits <c>{2♦,4♦,🃏}</c> — the joker plays the 3♦ while the real 3♦ stays free
/// for another meld. Without candidates of that shape the exact-cover search rejects hands
/// that genuinely win (<c>docs/spec/RUN-CANDIDATES.md</c> §3).
/// </para>
/// <para>
/// Candidates are de-duplicated by <b>set of <see cref="CardId"/></b>, keeping the first
/// interpretation seen. So the reference hand above yields <b>5</b> candidates, not the 8 of
/// the retired 2023 test — that 8 counted joker <i>interpretations</i>, and cover cares only
/// about which cards a meld consumes (<c>docs/spec/RUN-CANDIDATES.md</c> §2).
/// </para>
/// </remarks>
public static class RunGenerator
{
    /// <summary>The shortest legal run (RULES.md §6.1).</summary>
    public const int MinimumLength = 3;

    /// <summary>The ordering value an ace takes at the <i>start</i> of a run — A-2-3.</summary>
    private const int AceLow = 1;

    private const int LowestRank = (int)Rank.Two;    // 2
    private const int HighestRank = (int)Rank.Ace;   // 14, ace high at the end of a run

    /// <summary>
    /// Every distinct run a hand can make, by set of cards consumed. Candidates overlap one
    /// another by design; the caller enforces disjointness.
    /// </summary>
    public static IReadOnlyList<Meld> Candidates(IReadOnlyList<Card> hand)
    {
        ArgumentNullException.ThrowIfNull(hand);

        var jokers = hand.Where(card => card.IsJoker).ToArray();
        var candidates = new List<Meld>();
        var seen = new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer());

        foreach (var suit in CardText.AllSuits)
        {
            var bySuitAndRank = hand
                .Where(card => card.Suit == suit)
                .ToLookup(card => card.Rank!.Value);

            foreach (var window in Windows())
            {
                Fill(window, suit, bySuitAndRank, jokers, position: 0, lastJokerUsed: -1,
                     slots: new MeldSlot[window.Length], candidates, seen);
            }
        }

        return candidates;
    }

    /// <summary>
    /// The rank sequences a run may occupy, in ascending start order. Ace handling is
    /// explicit rather than arithmetic (RULES.md §6.1, BUILD-PLAN §3.2): a window either
    /// begins with the ace played low and continues 2, 3, …, or ascends within 2..A with the
    /// ace only ever last. Nothing wraps, so K-A-2 is not a window at all.
    /// </summary>
    private static IEnumerable<Rank[]> Windows()
    {
        // Ace low: A-2-3 up to A-2-3-…-K.
        for (var length = MinimumLength; AceLow + length - 1 <= (int)Rank.King; length++)
        {
            var window = new Rank[length];
            window[0] = Rank.Ace;
            for (var offset = 1; offset < length; offset++)
            {
                window[offset] = (Rank)(LowestRank + offset - 1);
            }

            yield return window;
        }

        // Ace high, or no ace at all: 2-3-4 up to 2-3-…-A.
        for (var start = LowestRank; start + MinimumLength - 1 <= HighestRank; start++)
        {
            for (var length = MinimumLength; start + length - 1 <= HighestRank; length++)
            {
                var window = new Rank[length];
                for (var offset = 0; offset < length; offset++)
                {
                    window[offset] = (Rank)(start + offset);
                }

                yield return window;
            }
        }
    }

    /// <summary>
    /// Fills one window position at a time, emitting a candidate once every position is
    /// satisfied.
    /// </summary>
    /// <param name="lastJokerUsed">
    /// Index into <paramref name="jokers"/> of the last joker consumed. Jokers are only ever
    /// taken in ascending index order, which is what keeps the search enumerating each
    /// <i>combination</i> of joker instances once instead of every permutation of it. Two
    /// jokers filling the same two positions the other way round would be the same set of
    /// cards, and so the same candidate.
    /// </param>
    private static void Fill(
        Rank[] window,
        Suit suit,
        ILookup<Rank, Card> bySuitAndRank,
        Card[] jokers,
        int position,
        int lastJokerUsed,
        MeldSlot[] slots,
        List<Meld> candidates,
        HashSet<HashSet<CardId>> seen)
    {
        if (position == window.Length)
        {
            var meld = new Meld(MeldKind.Run, slots);
            if (seen.Add(meld.IdentityKey))
            {
                candidates.Add(meld);
            }

            return;
        }

        var rank = window[position];

        // Either a held card of that exact rank and suit — each duplicate copy is its own
        // candidate (defect D4) …
        foreach (var held in bySuitAndRank[rank])
        {
            slots[position] = new MeldSlot(held, rank, suit);
            Fill(window, suit, bySuitAndRank, jokers, position + 1, lastJokerUsed, slots,
                 candidates, seen);
        }

        // … or a joker standing in for it, whether or not the real card is held.
        for (var joker = lastJokerUsed + 1; joker < jokers.Length; joker++)
        {
            slots[position] = new MeldSlot(jokers[joker], rank, suit);
            Fill(window, suit, bySuitAndRank, jokers, position + 1, joker, slots,
                 candidates, seen);
        }
    }
}

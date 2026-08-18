using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Melds;

/// <summary>
/// Generates every set candidate a hand can make: three or more cards of one rank in
/// <b>distinct</b> suits, with jokers free to stand in for any absent suit (RULES.md §6.2).
/// </summary>
/// <remarks>
/// <para>
/// Duplicate suits are forbidden — <c>9♥ 9♥ 9♠</c> is not a set, confirmed by Mya Lay — so a
/// set holds <b>at most four cards</b>, one per suit, even though two decks make a fifth copy
/// physically holdable. Generation therefore walks the four suits once per rank, filling each
/// with either a held card, a specific joker instance, or nothing at all.
/// </para>
/// <para>
/// <b>Two copies of one suit are not a wider set; they are two candidates.</b> From
/// <c>9♥ 9♥ 9♠ 9♦</c> the generator emits the set that takes the first 9♥ and the set that
/// takes the second — the duplicate-copy case the retired 2023 code lost entirely (defect
/// D4). Which copy is used changes the cards consumed, and cover cares about nothing else.
/// </para>
/// <para>
/// Candidates are de-duplicated by <b>set of <see cref="CardId"/></b>, keeping the first
/// interpretation seen, exactly as <see cref="RunGenerator"/> does. That is what collapses
/// <c>9♥ 9♠ 🃏</c> — the joker plays the club or the diamond, one card set either way — into
/// a single candidate.
/// </para>
/// </remarks>
public static class SetGenerator
{
    /// <summary>The shortest legal set (RULES.md §6.2).</summary>
    public const int MinimumSize = 3;

    /// <summary>The longest legal set: one card per suit, because duplicate suits are forbidden.</summary>
    public const int MaximumSize = 4;

    /// <summary>
    /// Every distinct set a hand can make, by set of cards consumed. Candidates overlap one
    /// another by design; the caller enforces disjointness.
    /// </summary>
    public static IReadOnlyList<Meld> Candidates(IReadOnlyList<Card> hand)
    {
        ArgumentNullException.ThrowIfNull(hand);

        var jokers = hand.Where(card => card.IsJoker).ToArray();
        var candidates = new List<Meld>();
        var seen = new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer());

        foreach (var rank in CardText.AllRanks)
        {
            var bySuit = hand
                .Where(card => card.Rank == rank)
                .ToLookup(card => card.Suit!.Value);

            Fill(rank, bySuit, jokers, suitIndex: 0, lastJokerUsed: -1,
                 slots: new List<MeldSlot>(MaximumSize), candidates, seen);
        }

        return candidates;
    }

    /// <summary>
    /// Walks the four suits in order, taking each at most once: a held card of this rank and
    /// suit, a joker standing in for it, or neither.
    /// </summary>
    /// <param name="lastJokerUsed">
    /// Index into <paramref name="jokers"/> of the last joker consumed. Jokers are only ever
    /// taken in ascending index order, which enumerates each <i>combination</i> of joker
    /// instances once rather than every permutation of it: which joker covers which absent
    /// suit does not change the cards consumed, so the naive form emits duplicates only to
    /// throw them away.
    /// </param>
    private static void Fill(
        Rank rank,
        ILookup<Suit, Card> bySuit,
        Card[] jokers,
        int suitIndex,
        int lastJokerUsed,
        List<MeldSlot> slots,
        List<Meld> candidates,
        HashSet<HashSet<CardId>> seen)
    {
        if (suitIndex == CardText.AllSuits.Count)
        {
            if (slots.Count >= MinimumSize)
            {
                var meld = new Meld(MeldKind.Set, slots);
                if (seen.Add(meld.IdentityKey))
                {
                    candidates.Add(meld);
                }
            }

            return;
        }

        var suit = CardText.AllSuits[suitIndex];

        // Leave the suit out. A three-card set is a four-suit set with one suit unfilled.
        Fill(rank, bySuit, jokers, suitIndex + 1, lastJokerUsed, slots, candidates, seen);

        // Or a held card of that exact rank and suit — each duplicate copy is its own
        // candidate (defect D4) …
        foreach (var held in bySuit[suit])
        {
            slots.Add(new MeldSlot(held, rank, suit));
            Fill(rank, bySuit, jokers, suitIndex + 1, lastJokerUsed, slots, candidates, seen);
            slots.RemoveAt(slots.Count - 1);
        }

        // … or a joker standing in for it, whether or not the real card is held.
        for (var joker = lastJokerUsed + 1; joker < jokers.Length; joker++)
        {
            slots.Add(new MeldSlot(jokers[joker], rank, suit));
            Fill(rank, bySuit, jokers, suitIndex + 1, joker, slots, candidates, seen);
            slots.RemoveAt(slots.Count - 1);
        }
    }
}

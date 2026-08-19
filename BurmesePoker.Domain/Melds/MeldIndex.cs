using System.Numerics;
using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Melds;

/// <summary>
/// A hand's meld candidates, arranged for backtracking: the cards in a fixed order, one bit
/// per card, and every candidate filed under the <b>lowest</b> card it consumes.
/// </summary>
/// <remarks>
/// <para>
/// Filing by lowest card is what makes the search tractable. A search that pins itself to the
/// lowest card not yet dealt with knows that every card below it is already settled, so the
/// only melds that can still cover it are the ones whose lowest card it is — the index is both
/// the filter and the thing that stops permutations of an arrangement being re-explored. A
/// hand of nine consecutive cards in one suit plus four jokers produces over four thousand
/// candidates, so this is not a nicety.
/// </para>
/// <para>
/// Two searches share it: <see cref="HandEvaluator"/>, which demands that <em>every</em> card
/// be covered (RULES.md §7.1), and <see cref="PartialCover"/>, which covers as many as it can.
/// They are the same walk over the same index asking two different questions, and building
/// that index twice would be two places for the subtleties to drift apart.
/// </para>
/// </remarks>
internal sealed class MeldIndex
{
    /// <summary>
    /// The largest hand that can be indexed — one bit per card in a <see cref="ulong"/>.
    /// The game deals 13 and never holds more than 14, so this is a guard, not a limit.
    /// </summary>
    public const int MaximumHandSize = 64;

    private MeldIndex(Card[] cards, ulong full, List<(Meld Meld, ulong Mask)>[] byLowestCard)
    {
        Cards = cards;
        Full = full;
        ByLowestCard = byLowestCard;
    }

    /// <summary>The hand in bit order — ascending <see cref="CardId"/>, so bit <c>i</c> is <c>Cards[i]</c>.</summary>
    public Card[] Cards { get; }

    /// <summary>A set bit for every card in the hand.</summary>
    public ulong Full { get; }

    /// <summary>The candidates that could cover card <c>i</c> once every card below it is settled.</summary>
    public List<(Meld Meld, ulong Mask)>[] ByLowestCard { get; }

    /// <summary>How many cards the hand holds.</summary>
    public int Count => Cards.Length;

    /// <summary>The cards of the hand whose bits are not set in <paramref name="mask"/>.</summary>
    public IReadOnlyList<Card> CardsOutside(ulong mask)
    {
        var outside = new List<Card>(Count - BitOperations.PopCount(mask));

        for (var index = 0; index < Cards.Length; index++)
        {
            if ((mask & (1UL << index)) == 0)
            {
                outside.Add(Cards[index]);
            }
        }

        return outside;
    }

    /// <exception cref="ArgumentException">
    /// The hand is too large to index, or holds the same physical card twice.
    /// </exception>
    public static MeldIndex Build(IReadOnlyList<Card> hand)
    {
        ArgumentNullException.ThrowIfNull(hand);
        if (hand.Count > MaximumHandSize)
        {
            throw new ArgumentException(
                $"A hand of more than {MaximumHandSize} cards cannot be evaluated.", nameof(hand));
        }

        // Ascending CardId order, so "the lowest card" is bit index order.
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

        return new MeldIndex(ordered, full, byLowestCard);
    }
}

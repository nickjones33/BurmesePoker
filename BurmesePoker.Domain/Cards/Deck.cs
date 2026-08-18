using System.Runtime.InteropServices;

namespace BurmesePoker.Domain.Cards;

/// <summary>
/// A pile of cards drawn from either end — the shoe before the deal, and the draw pile
/// during a round.
/// </summary>
/// <remarks>
/// Deliberately a plain class wrapping a list rather than a <c>List&lt;Card&gt;</c>
/// subclass: a deck is not a general-purpose collection, and only the four operations
/// below are legal on one.
/// </remarks>
public sealed class Deck
{
    private readonly List<Card> _cards;

    /// <summary>Builds a deck from the given cards, with the first taken as the top.</summary>
    public Deck(IEnumerable<Card> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        _cards = [.. cards];
    }

    /// <summary>The shoe: two decks, unshuffled (<see cref="DeckBuilder.BuildTwoDecks"/>).</summary>
    public static Deck TwoDecks() => new(DeckBuilder.BuildTwoDecks());

    /// <summary>How many cards remain.</summary>
    public int Count => _cards.Count;

    /// <summary>True when no cards remain.</summary>
    public bool IsEmpty => _cards.Count == 0;

    /// <summary>
    /// The remaining cards, top first. A live view of the deck, so copy it before drawing
    /// or shuffling if you need a snapshot.
    /// </summary>
    public IReadOnlyList<Card> Cards => _cards;

    /// <summary>Removes and returns the top card.</summary>
    /// <exception cref="DeckExhaustedException">The deck is empty.</exception>
    public Card DrawFromTop()
    {
        if (_cards.Count == 0)
        {
            throw new DeckExhaustedException();
        }

        var card = _cards[0];
        _cards.RemoveAt(0);
        return card;
    }

    /// <summary>Removes and returns the bottom card.</summary>
    /// <exception cref="DeckExhaustedException">The deck is empty.</exception>
    public Card DrawFromBottom()
    {
        if (_cards.Count == 0)
        {
            throw new DeckExhaustedException();
        }

        var index = _cards.Count - 1;
        var card = _cards[index];
        _cards.RemoveAt(index);
        return card;
    }

    /// <summary>
    /// Shuffles in place into a uniform random permutation.
    /// </summary>
    /// <remarks>
    /// <see cref="Random.Shuffle{T}(Span{T})"/> is a Fisher–Yates shuffle. The retired 2023
    /// code used <c>OrderBy(_ =&gt; random.Next())</c>, which is not a uniform permutation —
    /// it biases towards the original order whenever two keys collide.
    /// </remarks>
    public void Shuffle(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        random.Shuffle(CollectionsMarshal.AsSpan(_cards));
    }
}

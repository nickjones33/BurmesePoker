using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Play;

/// <summary>
/// One seat: the cards a player holds and the cards they have thrown away.
/// </summary>
/// <remarks>
/// <para>
/// There is deliberately <b>no meld state here</b>. Play is fully concealed — nothing is
/// laid on the table until a player goes out and declares the whole hand at once
/// (RULES.md §6.3) — so a seat is a hand and a discard pile, and nothing else.
/// </para>
/// <para>
/// Ownership of money cards is <em>not</em> tracked here either. It is recorded once in
/// <see cref="Money.CardOwnership"/> and survives the card leaving the hand, so asking a
/// seat what it holds can never answer the settlement question (RULES.md §4.4).
/// </para>
/// <para>
/// The mutators are <c>internal</c>: only <see cref="RoundEngine"/> moves cards, so a front
/// end cannot deal itself a better hand.
/// </para>
/// </remarks>
public sealed class PlayerState
{
    private readonly List<Card> _hand = [];
    private readonly List<Card> _discards = [];

    internal PlayerState(PlayerId id) => Id = id;

    /// <summary>Whose seat this is.</summary>
    public PlayerId Id { get; }

    /// <summary>
    /// The cards held, in the order they arrived. A live view, like <see cref="Deck.Cards"/> —
    /// copy it if you need a snapshot.
    /// </summary>
    public IReadOnlyList<Card> Hand => _hand;

    /// <summary>This seat's discard pile, oldest first. A live view.</summary>
    /// <remarks>
    /// Per-player piles rather than one shared pile (RULES.md §9 #9). The distinction is
    /// invisible during play, because only <see cref="TopDiscard"/> is ever takeable; it
    /// matters only when P9 gathers every pile at deck exhaustion.
    /// </remarks>
    public IReadOnlyList<Card> Discards => _discards;

    /// <summary>The most recent discard — the only one the next player may take — or null.</summary>
    public Card? TopDiscard => _discards.Count == 0 ? null : _discards[^1];

    internal void Take(Card card) => _hand.Add(card);

    internal Card Discard(Card card)
    {
        var index = _hand.FindIndex(held => held.Id == card.Id);

        if (index < 0)
        {
            throw new InvalidOperationException(
                $"{Id} cannot discard {card}; it is not in that hand.");
        }

        var discarded = _hand[index];
        _hand.RemoveAt(index);
        _discards.Add(discarded);
        return discarded;
    }

    internal Card TakeTopDiscard()
    {
        if (_discards.Count == 0)
        {
            throw new InvalidOperationException($"{Id} has discarded nothing to take.");
        }

        var card = _discards[^1];
        _discards.RemoveAt(_discards.Count - 1);
        return card;
    }
}

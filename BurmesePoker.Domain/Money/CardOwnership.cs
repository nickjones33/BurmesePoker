using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Money;

/// <summary>
/// Who the deck gave each card to (RULES.md §4.4). The record settlement reads.
/// </summary>
/// <remarks>
/// <para>
/// The method name is the whole rule: <b><see cref="RecordFromDeck"/> is the
/// discriminator.</b> Dealing a card and drawing one blind both call it; picking a card off
/// a discard pile and claiming one from the table never do, so those cards are held but
/// never owned.
/// </para>
/// <para>
/// <b>Append-only is the rule, not a convenience.</b> Ownership is permanent and never
/// transfers: a money card you drew and then discarded still pays you at settlement, even
/// while an opponent holds it. So there is deliberately no transfer, no clear and no
/// removal, and settlement never inspects a hand — it walks <see cref="Records"/>.
/// </para>
/// </remarks>
public sealed class CardOwnership
{
    private readonly Dictionary<CardId, PlayerId> _owners = [];

    /// <summary>
    /// Every ownership recorded so far, as a live read-only view. This is what P6 settles
    /// from; where a card currently sits is irrelevant.
    /// </summary>
    public IReadOnlyDictionary<CardId, PlayerId> Records => _owners;

    /// <summary>
    /// Records that the deck gave <paramref name="card"/> to <paramref name="owner"/> — by
    /// dealing it or by a blind draw. Never called for a card taken from the table or from
    /// a discard pile.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The card is already owned by a different player. Ownership never transfers, so this
    /// can only mean one physical card reached the table twice; it is surfaced rather than
    /// silently resolved. Re-recording the same owner is a no-op.
    /// </exception>
    public void RecordFromDeck(CardId card, PlayerId owner)
    {
        if (_owners.TryGetValue(card, out var existing))
        {
            if (existing != owner)
            {
                throw new InvalidOperationException(
                    $"Card {card} is already owned by {existing}; ownership never transfers " +
                    $"(RULES.md §4.4), so it cannot be recorded for {owner}.");
            }

            return;
        }

        _owners.Add(card, owner);
    }

    /// <summary>The player the deck gave this card to, or <c>null</c> if it owns nothing.</summary>
    public PlayerId? OwnerOf(CardId card) =>
        _owners.TryGetValue(card, out var owner) ? owner : null;
}

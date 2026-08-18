namespace BurmesePoker.Domain.Cards;

/// <summary>
/// The instance identity of a physical card, unique across the whole 108-card shoe
/// (0..107, assigned once at deck construction).
/// </summary>
/// <remarks>
/// Two decks are shuffled together, so value-identical cards coexist — there are two 5♥,
/// two red jokers, and so on. <see cref="CardId"/> is what tells those copies apart, and
/// it is what the exact-cover search uses to enforce that melds are disjoint
/// (BUILD-PLAN §3.1, §3.4). Card *value* comparison is <see cref="Card.SameValueAs"/>.
/// </remarks>
public readonly record struct CardId(int Value)
{
    public override string ToString() => Value.ToString();
}

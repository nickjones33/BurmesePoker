namespace BurmesePoker.Domain.Cards;

/// <summary>
/// The suit of a ranked card. Jokers are suitless — they carry a <c>null</c> suit rather
/// than a Joker member, mirroring <see cref="Rank"/>.
/// </summary>
public enum Suit
{
    Hearts,
    Spades,
    Clubs,
    Diamonds
}

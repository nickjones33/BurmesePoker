namespace BurmesePoker.Domain.Cards;

/// <summary>
/// The colour of a card. Ranked cards derive it from their suit; jokers carry it in their
/// own right, since a deck holds one red and one black joker.
/// </summary>
public enum CardColor
{
    Red,
    Black
}

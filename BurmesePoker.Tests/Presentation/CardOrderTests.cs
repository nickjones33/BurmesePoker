using BurmesePoker.Domain.Cards;
using BurmesePoker.Presentation;

namespace BurmesePoker.Tests.Presentation;

/// <summary>
/// The order a hand is laid out in — hearts, spades, clubs, diamonds, low to high, jokers
/// last (packet P13.1, moved out of the console).
/// </summary>
public class CardOrderTests
{
    [Fact]
    public void SuitsComeOutInTheOrderTheSalvagedTableLists()
    {
        var sorted = CardOrder.Display(Hands.Of("5D", "5C", "5S", "5H")).ToList();

        Assert.Equal([.. CardText.AllSuits], [.. sorted.Select(card => card.Suit!.Value)]);
    }

    [Fact]
    public void WithinASuitTheCardsRunLowToHigh()
    {
        var sorted = CardOrder.Display(Hands.Of("KH", "AH", "2H", "10H")).ToList();

        // The ace is high, as the salvaged CardText.Order has it (RULES.md §6.1).
        Assert.Equal(
            [Rank.Two, Rank.Ten, Rank.King, Rank.Ace],
            [.. sorted.Select(card => card.Rank)]);
    }

    [Fact]
    public void JokersComeLast()
    {
        var sorted = CardOrder.Display(Hands.Of("RJ", "2D", "BJ", "AH")).ToList();

        Assert.Equal([false, false, true, true], [.. sorted.Select(card => card.IsJoker)]);
    }

    /// <remarks>
    /// <b>The order has to be total.</b> Two decks mean a hand can hold two cards of the same
    /// value (BUILD-PLAN §3.1); if they tied, a front end re-sorting a hand after a draw could
    /// swap them past each other — which in a browser drags focus with the DOM node (§3.11
    /// C14). Ties break on colour and then on <see cref="CardId"/>.
    /// </remarks>
    [Fact]
    public void IdenticalCardsStillHaveAStableOrderBetweenThem()
    {
        var hand = Hands.Of("5H", "5H", "5H");

        var once = CardOrder.Display(hand).Select(card => card.Id.Value).ToList();
        var again = CardOrder.Display(hand.Reverse()).Select(card => card.Id.Value).ToList();

        Assert.Equal(once, again);
        Assert.Equal([.. once.Order()], once);
    }

    [Fact]
    public void SortingKeepsEveryCardAndInventsNone()
    {
        var hand = Hands.Of("KH", "AH", "RJ", "2D", "5C", "5C");

        Assert.Equal(
            [.. hand.Select(card => card.Id.Value).Order()],
            [.. CardOrder.Display(hand).Select(card => card.Id.Value).Order()]);
    }
}

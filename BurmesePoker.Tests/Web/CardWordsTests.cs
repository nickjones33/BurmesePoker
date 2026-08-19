using BurmesePoker.Domain.Cards;
using BurmesePoker.Web;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// A card said out loud, for whoever is listening rather than looking (packet P13.3).
/// </summary>
/// <remarks>
/// <b>"Done when: you can watch a whole round in a browser, with a screen reader, without a
/// mouse."</b> A screen reader reading <c>7♦</c> says whatever its symbol table happens to say,
/// which is not a card — so every card on the page carries the glyph for the eye, hidden from
/// the accessibility tree, and these words for the ear.
/// </remarks>
public class CardWordsTests
{
    [Fact]
    public void ARankedCardIsSaidAsRankAndSuit()
    {
        Assert.Equal("seven of diamonds", CardWords.For(Card.Ranked(new CardId(0), Rank.Seven, Suit.Diamonds)));
        Assert.Equal("ace of spades", CardWords.For(Card.Ranked(new CardId(1), Rank.Ace, Suit.Spades)));
        Assert.Equal("ten of clubs", CardWords.For(Card.Ranked(new CardId(2), Rank.Ten, Suit.Clubs)));
    }

    /// <remarks>
    /// A joker is rankless and suitless (§3.2), so what it has to say for itself is its colour.
    /// </remarks>
    [Fact]
    public void AJokerIsSaidAsItsColour()
    {
        Assert.Equal("red joker", CardWords.For(Card.Joker(new CardId(3), CardColor.Red)));
        Assert.Equal("black joker", CardWords.For(Card.Joker(new CardId(4), CardColor.Black)));
    }

    /// <remarks>
    /// Every card in the shoe has words: a rank or a suit added later would fail here rather
    /// than reaching a page as silence.
    /// </remarks>
    [Fact]
    public void EveryCardInTheShoeCanBeSaid()
    {
        var shoe = DeckBuilder.BuildTwoDecks();

        Assert.All(shoe, card => Assert.False(string.IsNullOrWhiteSpace(CardWords.For(card))));
        Assert.Equal(108, shoe.Count);
    }
}

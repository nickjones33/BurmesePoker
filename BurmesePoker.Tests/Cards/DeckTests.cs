using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Tests.Cards;

/// <summary>Drawing from either end, and shuffling that is actually a permutation.</summary>
public class DeckTests
{
    [Fact]
    public void TwoDecks_Holds108Cards() => Assert.Equal(108, Deck.TwoDecks().Count);

    [Fact]
    public void NewDeck_IsNotEmpty() => Assert.False(Deck.TwoDecks().IsEmpty);

    [Fact]
    public void DrawFromTop_ReturnsTheFirstCardAndReducesCount()
    {
        var deck = Deck.TwoDecks();
        var top = deck.Cards[0];

        var drawn = deck.DrawFromTop();

        Assert.Equal(top, drawn);
        Assert.Equal(107, deck.Count);
        Assert.DoesNotContain(drawn, deck.Cards);
    }

    [Fact]
    public void DrawFromBottom_ReturnsTheLastCardAndReducesCount()
    {
        var deck = Deck.TwoDecks();
        var bottom = deck.Cards[^1];

        var drawn = deck.DrawFromBottom();

        Assert.Equal(bottom, drawn);
        Assert.Equal(107, deck.Count);
        Assert.DoesNotContain(drawn, deck.Cards);
    }

    [Fact]
    public void DrawFromTopAndBottom_ComeFromOppositeEnds()
    {
        var deck = Deck.TwoDecks();

        var top = deck.DrawFromTop();
        var bottom = deck.DrawFromBottom();

        Assert.NotEqual(top, bottom);
        Assert.Equal(106, deck.Count);
    }

    [Fact]
    public void DrawFromTop_OnAnEmptyDeck_ThrowsADomainException()
    {
        var deck = new Deck([]);

        Assert.Throws<DeckExhaustedException>(() => deck.DrawFromTop());
    }

    [Fact]
    public void DrawFromBottom_OnAnEmptyDeck_ThrowsADomainException()
    {
        var deck = new Deck([]);

        Assert.Throws<DeckExhaustedException>(() => deck.DrawFromBottom());
    }

    [Fact]
    public void DrawingTheWholeDeck_EmptiesItAndThenThrows()
    {
        var deck = Deck.TwoDecks();

        for (var i = 0; i < 108; i++)
        {
            deck.DrawFromTop();
        }

        Assert.True(deck.IsEmpty);
        Assert.Throws<DeckExhaustedException>(() => deck.DrawFromTop());
    }

    [Fact]
    public void Shuffle_PreservesTheMultisetOfCards()
    {
        var deck = Deck.TwoDecks();
        var before = deck.Cards.ToList();

        deck.Shuffle(new Random(1234));

        Assert.Equal(before.Count, deck.Count);
        Assert.Equal(before.Select(c => c.Id.Value).Order(), deck.Cards.Select(c => c.Id.Value).Order());
    }

    [Fact]
    public void Shuffle_ChangesTheOrder()
    {
        var deck = Deck.TwoDecks();
        var before = deck.Cards.ToList();

        deck.Shuffle(new Random(1234));

        Assert.NotEqual(before, deck.Cards);
    }

    [Fact]
    public void Shuffle_WithTheSameSeed_IsReproducible()
    {
        var first = Deck.TwoDecks();
        var second = Deck.TwoDecks();

        first.Shuffle(new Random(99));
        second.Shuffle(new Random(99));

        Assert.Equal(first.Cards, second.Cards);
    }

    [Fact]
    public void Deck_TakesACopyOfTheCardsItIsGiven()
    {
        var source = new List<Card> { Card.Ranked(new CardId(0), Rank.Ace, Suit.Spades) };
        var deck = new Deck(source);

        source.Clear();

        Assert.Equal(1, deck.Count);
    }
}

namespace BurmesePoker.Tests;

public class CardPlayFactoryTests
{
    [Fact]
    public void CardPlays_Runs_HappyPath()
    {
        var cardsToTest = new List<Card>()
        {
            new Card(CardRank.Two, CardSuit.Diamonds),
            new Card(CardRank.Three, CardSuit.Diamonds),
            new Card(CardRank.Four, CardSuit.Diamonds),
            new Card(CardRank.Five, CardSuit.Diamonds),
        };
        var result = CardPlaysFactory.MakeAllPossiblePlaysFromHand(cardsToTest);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}
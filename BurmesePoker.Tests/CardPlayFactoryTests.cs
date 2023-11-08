namespace BurmesePoker.Tests;

public class CardPlayFactoryTests
{
    [Fact]
    public void CardPlays_Runs_HappyPath_NoJokers()
    {
        List<Card> cardsToTest = new List<Card>()
        {
            new Card(CardRank.Two, CardSuit.Diamonds),
            new Card(CardRank.Three, CardSuit.Diamonds),
            new Card(CardRank.Four, CardSuit.Diamonds),
            new Card(CardRank.Five, CardSuit.Diamonds),
        };
        List<CardPlay> result = CardPlaysFactory.MakeAllPossiblePlaysFromHand(cardsToTest);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }
    [Fact]
    public void CardPlays_Runs_HappyPath_Jokers()
    {
        List<Card> cardsToTest = new List<Card>()
        {
            new Card(CardRank.Two, CardSuit.Diamonds), //2,3,4; 2,3,J; 2,J,4; 2,3,4,J
            new Card(CardRank.Three, CardSuit.Diamonds), //3,4,J
            new Card(CardRank.Four, CardSuit.Diamonds), //none
            new Card(CardRank.Joker, CardColor.Red), //J,2,3; J,3,4; J,2,3,4
        };
        List<CardPlay> result = CardPlaysFactory.MakeAllPossiblePlaysFromHand(cardsToTest);

        Assert.NotNull(result);
        Assert.Equal(8, result.Count);
    }
}
namespace BurmesePoker;

public static class CardFactory
{
    public static List<Card> MakeDecks(int numberOfDecks)
    {
        List<Card> cards = [];
        for (int i = 0; i < numberOfDecks; i++)
        {
            cards.AddRange(MakeDeck());
        }
        return cards;
    }
    public static List<Card> MakeDeck()
    {
        List<Card> deck = [];
        foreach (CardSuit suit in Common.CardSuits_NoJokers())
        {
            foreach (CardRank rank in Common.CardRankCodes_NoJokers())
            {
                deck.Add(new Card(rank, suit));
            }
        }
        deck.Add(new Card(CardRank.Joker, CardColor.Red));
        deck.Add(new Card(CardRank.Joker, CardColor.Black));
        return deck;
    }
}

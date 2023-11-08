using System.Data;

namespace BurmesePoker;

internal static class CardPlaysFactory
{
    public static List<CardPlay> MakeAllPossiblePlaysFromHand(List<Card> hand)
    {
        List<CardPlay> plays = [];
        plays.AddRange(MakeRunsFromHand(hand));
        plays.AddRange(MakeSetsFromHand(hand));
        return plays;
    }
    private static List<CardPlay> MakeRunsFromHand(List<Card> hand)
    {
        List<CardPlay> runs = [];

        var jokers = hand.Where(card => card.Rank == CardRank.Joker).ToList();
        var diamonds = hand.Where(card => card.Suit == CardSuit.Diamonds).OrderBy(card => card.Rank).ToList();
        var clubs = hand.Where(card => card.Suit == CardSuit.Clubs).OrderBy(card => card.Rank).ToList();
        var spades = hand.Where(card => card.Suit == CardSuit.Spades).OrderBy(card => card.Rank).ToList();
        var hearts = hand.Where(card => card.Suit == CardSuit.Hearts).OrderBy(card => card.Rank).ToList();

        if (diamonds.Count + jokers.Count >= 3) runs.AddRange(MakeRunsFromSuit(diamonds, jokers));
        if (clubs.Count + jokers.Count >= 3) runs.AddRange(MakeRunsFromSuit(clubs, jokers));
        if (spades.Count + jokers.Count >= 3) runs.AddRange(MakeRunsFromSuit(spades, jokers));
        if (hearts.Count + jokers.Count >= 3) runs.AddRange(MakeRunsFromSuit(hearts, jokers));

        return runs;
    }
    private static List<CardPlay> MakeRunsFromSuit(List<Card> suitedOrderedCards, List<Card> jokers)
    {
        List<CardPlay> sets = [];
        foreach (var card in suitedOrderedCards)
        {
            List<Card> potentialCardsInPlay = [card];
            var tmpJokers = jokers;
            bool outOfUsableCards = false;
            while (!outOfUsableCards)
            {
                var nextCardInRun = suitedOrderedCards.Where(c => c.Rank == card.Rank + 1).FirstOrDefault();
                if (nextCardInRun == null)
                {
                    if (card.Rank == CardRank.Ace)
                    {
                        nextCardInRun = suitedOrderedCards.Where(c => c.Rank == CardRank.Two).FirstOrDefault();
                    }
                    else if (tmpJokers.Count != 0)
                    {
                        nextCardInRun = tmpJokers.First();
                        tmpJokers.RemoveAt(0);
                    }
                    else
                    {
                        outOfUsableCards = true;
                    }
                }
                if (nextCardInRun != null) potentialCardsInPlay.Add(nextCardInRun);
                if (potentialCardsInPlay.Count >= 3) sets.Add(new CardPlay(CardPlayType.Run, potentialCardsInPlay));
            }
            
        }
        return sets;
    }
    private static List<CardPlay> MakeSetsFromHand(List<Card> hand)
    {
        List<CardPlay> sets = [];
        return sets;
    }
}

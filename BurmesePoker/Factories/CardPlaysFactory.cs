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

        List<Card> jokers = hand.Where(card => card.Rank == CardRank.Joker).ToList();
        List<Card> diamonds = [.. hand.Where(card => card.Suit == CardSuit.Diamonds).OrderBy(card => card.Rank)];
        List<Card> clubs = [.. hand.Where(card => card.Suit == CardSuit.Clubs).OrderBy(card => card.Rank)];
        List<Card> spades = [.. hand.Where(card => card.Suit == CardSuit.Spades).OrderBy(card => card.Rank)];
        List<Card> hearts = [.. hand.Where(card => card.Suit == CardSuit.Hearts).OrderBy(card => card.Rank)];

        if (diamonds.Count + jokers.Count >= 3) runs.AddRange(MakeRunsFromSuit(diamonds, jokers));
        if (clubs.Count + jokers.Count >= 3) runs.AddRange(MakeRunsFromSuit(clubs, jokers));
        if (spades.Count + jokers.Count >= 3) runs.AddRange(MakeRunsFromSuit(spades, jokers));
        if (hearts.Count + jokers.Count >= 3) runs.AddRange(MakeRunsFromSuit(hearts, jokers));

        return runs;
    }
    private static List<CardPlay> MakeRunsFromSuit(List<Card> suitedOrderedCards, List<Card> jokers)
    {
        List<CardPlay> runsStartingWithNonJokers = [];
        foreach (Card card in suitedOrderedCards)
        {
            List<Card> potentialCardsInPlay = [card];
            List<Card> tmpJokers = [.. jokers];
            bool outOfUsableCards = false;
            while (!outOfUsableCards)
            {
                Card mostRecentCardInRun = potentialCardsInPlay.Last();
                Card? nextCardInRun = suitedOrderedCards.Where(c => c.Rank == mostRecentCardInRun.Rank + 1).FirstOrDefault();
                if (nextCardInRun == null)
                {
                    if (mostRecentCardInRun.Rank == CardRank.Ace)
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
                if (!outOfUsableCards)
                {
                    if (nextCardInRun != null) potentialCardsInPlay.Add(nextCardInRun);
                    if (potentialCardsInPlay.Count >= 3) runsStartingWithNonJokers.Add(new CardPlay(CardPlayType.Run, potentialCardsInPlay.ToList()));
                }
            }
        }
        List<CardPlay> alternativePermutations = [];
        foreach (CardPlay cleanRun in runsStartingWithNonJokers)
        {
            List<Card> cardsInRun = [.. cleanRun.Cards.OrderBy(c => c.Rank)];

            List<CardPlay> permutations = [];

            alternativePermutations.AddRange(permutations);
        }
        List<CardPlay> runsStartingWithJokers = [];
        foreach (Card joker in jokers)
        {
            foreach (Card cardToFollowTheJoker in suitedOrderedCards)
            {
                List<Card> potentialCardsInPlay = [joker, cardToFollowTheJoker];
                List<Card> tmpJokers = [.. jokers];
                tmpJokers.RemoveAt(0); //exclude one to account for current joker
                bool outOfUsableCards = false;
                while (!outOfUsableCards)
                {
                    Card mostRecentCardInRun = potentialCardsInPlay.Last();
                    Card? nextCardInRun = suitedOrderedCards.Where(c => c.Rank == mostRecentCardInRun.Rank + 1).FirstOrDefault();
                    if (nextCardInRun == null)
                    {
                        if (mostRecentCardInRun.Rank == CardRank.Ace)
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
                    if (!outOfUsableCards)
                    {
                        if (nextCardInRun != null) potentialCardsInPlay.Add(nextCardInRun);
                        if (potentialCardsInPlay.Count >= 3) runsStartingWithJokers.Add(new CardPlay(CardPlayType.Run, potentialCardsInPlay.ToList()));
                    }
                }
            }
        }
        return [.. runsStartingWithNonJokers, .. runsStartingWithJokers];
    }
    private static void CalculatePermutationsRecursive(List<Card> originalCardsInPlay, List<Card> jokers,
        int index, List<Card> currentPlay, List<CardPlay> results)
    {
        if (index == originalCardsInPlay.Count) //base case check
        {
            results.Add(new CardPlay(CardPlayType.Run, currentPlay));
            return;
        }

        if (jokers.Count > 0)
        {
            var firstJoker = jokers.First();
            currentPlay.Add(firstJoker);
            
        }
    }
    private static List<CardPlay> MakeSetsFromHand(List<Card> hand)
    {
        List<CardPlay> sets = [];
        return sets;
    }
}

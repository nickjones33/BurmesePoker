namespace BurmesePoker;

internal class Table
{
    internal void SetupDeck()
    {
        Deck = new Deck(CardFactory.MakeDecks(2));
        Deck = Deck.Shuffle();
    }
    internal int RoundNumber { get; set; } = 0;
    internal PlayersInOrder? PlayersInOrder { get; set; }
    internal Deck Deck { get; set; } = [];
    internal IEnumerable<Card> AllCards => Deck.Concat(AllPlayerCards).Concat(CurrentRoundMoneyCards);
    internal IEnumerable<Card> AllPlayerCards => PlayersInOrder?.SelectMany(x => x.Hand) ?? [];
    internal IEnumerable<Card> CurrentRoundMoneyCards { get; set; } = [];

    internal void SetCurrentRoundMoneyCards()
    {
        var bottomCard = Deck.DrawFromBottom();
        var topCard = Deck.DrawFromTop();
        CurrentRoundMoneyCards = [bottomCard, topCard];
    }

    internal void MarkDeckAndPlayerMoneyCards()
    {
        var cardsToBeMarked = Deck.Concat(AllPlayerCards).Concat(CurrentRoundMoneyCards);
        foreach (Card card in cardsToBeMarked)
        {
            if (CurrentRoundMoneyCards.Any(mc => mc.ValueEqualTo(card)))
            {
                if (!card.IsMoneyCard)
                {
                    card.MoneyCardStatus = MoneyCardStatus.MoneyCard;
                }
                else
                {
                    card.MoneyCardStatus = MoneyCardStatus.DoubleMoneyCard;
                }
            }
        }
    }

    internal void DealCardsToPlayers(int cardsPerPlayer)
    {
        if (PlayersInOrder == null) throw new InvalidOperationException("Cannot deal cards to players when there are no players.");
        for (int i = 0; i < cardsPerPlayer; i++)
        {
            foreach (Player player in PlayersInOrder)
            {
                player.Hand.Add(Deck.DrawFromTop());
            }
        }
    }
}

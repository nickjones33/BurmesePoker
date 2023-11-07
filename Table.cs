namespace BurmesePoker;

internal class Table
{
    internal void SetupDeck()
    {
        Deck = new Deck(CardFactory.MakeDecks(2));
        Deck.Shuffle();
    }
    internal List<Player> Players { get; set; } = [];
    internal Deck Deck { get; set; } = [];
    internal List<Card> Discard { get; set; } = [];
    internal IEnumerable<Card> AllCards => Deck.Concat(Discard).Concat(Players.SelectMany(x => x.Hand)).Concat(CurrentRoundMoneyCards);

    internal IEnumerable<Card> CurrentRoundMoneyCards { get; set; }

    internal void SetCurrentRoundMoneyCards() {
        var bottomCard = Deck.DrawFromBottom();
        var topCard = Deck.DrawFromTop();
        CurrentRoundMoneyCards = [bottomCard, topCard];
    }

    internal void DealCardsToPlayers(int cardsPerPlayer)
    {
        for (int i = 0; i < cardsPerPlayer; i++)
        {
            foreach (Player player in Players)
            {
                player.Hand.Add(Deck.DrawFromTop());
            }
        }
    }
}

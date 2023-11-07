namespace BurmesePoker;
internal static class GameLoop
{
    internal static void Main()
    {
        Console.WriteLine("Welcome to Burmese Poker!");

        Table table = new();
        table.SetupDeck();
        table.Players = InitializePlayers();
        table.DealCardsToPlayers(13);
        table.SetCurrentRoundMoneyCards();
        table.MarkDeckAndPlayerMoneyCards();

        Console.WriteLine($"MoneyCards: {table.CurrentRoundMoneyCards.First().DisplayValue} and {table.CurrentRoundMoneyCards.Last().DisplayValue}");
        Console.WriteLine("Deck:");
        foreach (Card card in table.Deck)
        {
            Console.WriteLine(card.DisplayValue);
        }
        foreach (Player player in table.Players)
        {
            Console.WriteLine($"{player.Name} has {player.Hand.Count} cards and {player.Money} money. They start with the following cards:");
            foreach (Card card in player.Hand)
            {
                Console.WriteLine(card.DisplayValue);
            }
        }

        // Main game loop
        bool gameIsOver = false;
        while (!gameIsOver)
        {
            
            gameIsOver = true;
        }

        Console.WriteLine("Game over.");
    }

    private static List<Player> InitializePlayers() => [
        new Player("Su Htwe", 100),
        new Player("Aung", 100),
        new Player("Mya Lay", 100),
        new Player("Cobra", 100),
        new Player("Nick", 100),
    ];
}
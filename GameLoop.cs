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

        Console.WriteLine($"MoneyCards: {table.CurrentRoundMoneyCards.First().DisplayValue} and {table.CurrentRoundMoneyCards.Last().DisplayValue}");

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
            // Implement the game logic here, including player turns, drawing, discarding, sets, sequences, and scoring.
            // You would need to handle user input and update the game state accordingly.
            // This code is a simplified placeholder for the game loop.
            Console.WriteLine("Game loop (your logic here)");

            // Set gameIsOver to true when the game is finished.
            // For example, when a player goes out or after a certain number of rounds.
            gameIsOver = true;
        }

        Console.WriteLine("Game over. Display final scores and determine the winner.");
    }

    // Helper method to initialize players
    private static List<Player> InitializePlayers() => [
        new Player("Su Htwe", 100),
        new Player("Aung", 100),
        new Player("Mya Lay", 100),
        new Player("Cobra", 100),
        new Player("Nick", 100),
    ];

    
}
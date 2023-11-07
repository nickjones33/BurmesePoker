namespace BurmesePoker;
internal static class GameLoop
{
    internal static void Main()
    {
        Console.WriteLine("Welcome to Burmese Poker!");
        List<Card> deck = CardFactory.MakeDecks(2);
        Common.ShuffleDeck(deck);
        Console.WriteLine($"There are {deck.Count} cards in the deck.");

        List<Player> players = InitializePlayers();
        DealCardsToPlayers(deck, players, 13);

        foreach (var player in players) {
            Console.WriteLine($"{player.Name} has {player.Hand.Count} cards and {player.Money} money.");
            foreach (var card in player.Hand) {
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

    private static void DealCardsToPlayers(List<Card> deck, List<Player> players, int cardsPerPlayer)
    {
        for (int i = 0; i < cardsPerPlayer; i++)
        {
            foreach (Player player in players)
            {
                player.Hand.Add(deck.First());
                deck.RemoveAt(0);
            }
        }
    }
}
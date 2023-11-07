namespace BurmesePoker;
internal static class GameLoop
{
    internal static void Main()
    {
        Console.WriteLine("Welcome to Burmese Rummy!");

        // Initialize the deck with two decks of cards
        List<Card> deck = InitializeDeck();

        Console.WriteLine("Display the deck (unshuffled):");
        foreach (var card in deck)
        {
            Console.WriteLine(card.DisplayValue);
        }

        // Shuffle the deck
        ShuffleDeck(deck);

        // Deal cards to players
        List<Player> players = InitializePlayers();
        DealCards(deck, players);

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

    // Helper method to initialize the deck

    private static List<Card> InitializeDeck(int numberOfDecks = 2)
    {
        List<Card> deck = [];
        var suits = Common.CardSuits_NoJokers();
        var ranks = Common.CardRankCodes_NoJokers();

        foreach (CardSuit suit in suits)
        {
            foreach (CardRank rank in ranks)
            {
                for (var i = 0; i < numberOfDecks; i++)
                {
                    deck.Add(new Card(rank, suit));
                }
            }
        }

        for (var i = 0; i < numberOfDecks; i++)
        {
            deck.Add(new Card(CardRank.Joker, CardColor.Red));
            deck.Add(new Card(CardRank.Joker, CardColor.Black));
        }

        return deck;
    }

    // Helper method to shuffle the deck
    private static void ShuffleDeck(List<Card> deck)
    {
        // Add code to shuffle the deck
    }

    // Helper method to initialize players
    private static List<Player> InitializePlayers()
    {
        List<Player> players = new List<Player>();
        // Add code to create and name the players
        return players;
    }

    // Helper method to deal cards to players
    private static void DealCards(List<Card> deck, List<Player> players)
    {
        // Add code to deal cards to players
    }
}
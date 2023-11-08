namespace BurmesePoker;
internal class GameMaster
{
    private Table table { get; set; } = new();
    private bool gameIsOver = false;
    internal void StartGame()
    {
        Console.WriteLine("Welcome to Burmese Poker!");
        InitializeTable();

        // Main game loop
        while (!gameIsOver)
        {
            StartRound(); //start round
            //  start turn
            //      special first round/turn procedures
            //      present choices to player
            //      interpret/execute player choice
            //      check for win condition    
            //  end turn
            //end round


            gameIsOver = true;
        }

        Console.WriteLine("Game over.");
    }
    private void StartRound()
    {
        Console.WriteLine($"Round {table.RoundNumber} has begun.");
        table.RoundNumber++;
    }
    private void InitializeTable()
    {
        table.SetupDeck();
        table.PlayersInOrder = InitializePlayers();
        table.DealCardsToPlayers(13);
        table.SetCurrentRoundMoneyCards();
        table.MarkDeckAndPlayerMoneyCards();

        Console.WriteLine($"MoneyCards: {table.CurrentRoundMoneyCards.First().DisplayValue} and {table.CurrentRoundMoneyCards.Last().DisplayValue}");
    }
    private static PlayersInOrder InitializePlayers()
    {
        var players = new List<Player>{
            new("Su Htwe", 100),
            new("Aung", 100),
            new("Mya Lay", 100),
            new("Cobra", 100),
            new("Nick", 100)
        };
        var random = new Random();
        return new PlayersInOrder(players.OrderBy(x => random.Next()));
    }
}
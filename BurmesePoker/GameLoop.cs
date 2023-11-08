using Spectre.Console;

namespace BurmesePoker;
internal class GameMaster
{
    private Table Table { get; set; } = new();
    private bool gameIsOver = false;
    internal void StartGame()
    {
        Console.WriteLine("Welcome to Burmese Poker!");
        InitializeTable();
        if (Table.PlayersInOrder == null || Table.PlayersInOrder.Count < 2) throw new InvalidOperationException("Cannot start game when there are not at least 2 players.");
        while (!gameIsOver)
        {
            StartRound();
            gameIsOver = true;
        }

        Console.WriteLine("Game over.");
    }
    private void StartRound()
    {
        Console.WriteLine($"Round {Table.RoundNumber} has begun.");
        for (int i = 0; i < Table.PlayersInOrder!.Count; i++)
        {
            Player currentPlayer = Table.PlayersInOrder.ElementAt(i);
            Player nextPlayer = Table.PlayersInOrder.PlayerAfterIndex(i);
            Player? previousPlayer = null;
            if (Table.RoundNumber != 0 || i != 0)
            {
                previousPlayer = Table.PlayersInOrder.PlayerBeforeIndex(i);
            }

            StartTurn(currentPlayer, previousPlayer, nextPlayer);
        }

        Table.RoundNumber++;
    }
    private void StartTurn(Player currentPlayer, Player? previousPlayer, Player nextPlayer)
    {
        Console.WriteLine($"{currentPlayer.Name}'s turn.");
        Console.WriteLine($"Your hand: {currentPlayer.PrintOrderedHand}");
        if (previousPlayer == null) //special first round/turn moneycard procedures
        {
            Card topMoneyCard = Table.CurrentRoundMoneyCards.Last();
            bool playerWantsTopMoneyCard = UserPromptFactory.ResponseForTakingTopMoneyCard(currentPlayer.Name, topMoneyCard);
            //TODO - ask sorta-previous player for permission
            if (playerWantsTopMoneyCard)
            {
                var cloneOfTopMoneyCard = new Card(topMoneyCard.Rank, topMoneyCard.Suit)
                {
                    MoneyCardStatus = topMoneyCard.MoneyCardStatus,
                    MoneyCardOwner = null //belongs to the deck, doesn't score for the new steward
                };
                currentPlayer.Hand.Add(cloneOfTopMoneyCard);
            }
            else
            {
                var drawnCard = Table.Deck.DrawFromTop();
                drawnCard.MoneyCardOwner = currentPlayer;
                currentPlayer.Hand.Add(drawnCard);
            }
            PlayerDiscard(currentPlayer);
        }
        else
        {
            var discardOption = previousPlayer.Discard.Last();
            Console.WriteLine($"{previousPlayer.Name} discarded {discardOption.DisplayValue}. You may pick it up if you'd like.");
            var playerAction = UserPromptFactory.ResponseForAction(currentPlayer.Name);
 
            if (playerAction == PlayerAction.Draw)
            {
                var drawnCard = Table.Deck.DrawFromTop();
                drawnCard.MoneyCardOwner = currentPlayer;
                currentPlayer.Hand.Add(drawnCard);
                Console.WriteLine($"{currentPlayer.Name} drew {drawnCard.DisplayValue}.");
            }
            else if (playerAction == PlayerAction.Pickup)
            {
                previousPlayer.Discard.Remove(discardOption);
                currentPlayer.Hand.Add(discardOption);
                Console.WriteLine($"{currentPlayer.Name} picked up {discardOption.DisplayValue}.");
            }

            PlayerDiscard(currentPlayer);
        }

        //      present choices to player
        //      interpret/execute player choice
        //      check for win condition    
        //  end turn
        //end round
    }
    private void PlayerDiscard(Player currentPlayer)
    {
        var playerDiscardDescription = UserPromptFactory.ResponseForPlayerDiscard(currentPlayer);
        var discardMatch = currentPlayer.Hand.FirstOrDefault(x => x.Rank == playerDiscardDescription.Rank &&
            x.Color == playerDiscardDescription.Color && x.Suit == playerDiscardDescription.Suit);
        if (discardMatch == null) throw new InvalidOperationException("Card to discard was not found in player's hand.");
        currentPlayer.Hand.Remove(discardMatch);
        currentPlayer.Discard.Add(discardMatch);
        Console.WriteLine($"{currentPlayer.Name} discarded {discardMatch.DisplayValue}.");
    }

    private void InitializeTable()
    {
        Table.SetupDeck();
        Table.PlayersInOrder = InitializePlayers();
        Table.DealCardsToPlayers(13);
        Table.SetCurrentRoundMoneyCards();
        Table.MarkDeckAndPlayerMoneyCards();

        Console.WriteLine($"MoneyCards: {Table.CurrentRoundMoneyCards.First().DisplayValue} and {Table.CurrentRoundMoneyCards.Last().DisplayValue}");
    }
    private static PlayersInOrder InitializePlayers()
    {
        List<Player> players = new List<Player>{
            new("Su Htwe", 100),
            new("Aung", 100),
            new("Mya Lay", 100),
            new("Cobra", 100),
            new("Nick", 100)
        };
        Random random = new Random();
        return new PlayersInOrder(players.OrderBy(x => random.Next()));
    }
}
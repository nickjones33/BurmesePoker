using Spectre.Console;

namespace BurmesePoker;

internal static class UserPromptFactory
{
    internal static bool ResponseForTakingTopMoneyCard(string playerName, Card topMoneyCard) =>
        AnsiConsole.Confirm(TextForTakingTopMoneyCard(playerName, topMoneyCard));
    private static string TextForTakingTopMoneyCard(string playerName, Card topMoneyCard) =>
        $"{playerName}, would you like to take the top money card? ({topMoneyCard.DisplayValue})";

    internal static Card ResponseForPlayerDiscard(Player player) {
        var discardDisplayValue = AnsiConsole.Prompt(
            new SelectionPrompt<Card>()
                .Title($"{player.Name}, which card would you like to discard?")
                .AddChoices(player.Hand.OrderBy(x => x.RankOrder)));
        return discardDisplayValue;
    }

    internal static PlayerAction ResponseForAction(string playerName) => 
        AnsiConsole.Prompt(
            new SelectionPrompt<PlayerAction>()
                .Title(PromptForAction(playerName))
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more actions)[/]")
                .AddChoices(PlayerAction.Draw, PlayerAction.Pickup));
    private static string PromptForAction(string playerName) =>
        $"{playerName}, would you like to pick up from the discard pile or draw? (p/d)";
}

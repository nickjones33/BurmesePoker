using Spectre.Console;

namespace BurmesePoker;

internal static class UserPromptFactory
{
    internal static bool ResponseForTakingTopMoneyCard(string playerName, Card topMoneyCard) =>
        AnsiConsole.Confirm(TextForTakingTopMoneyCard(playerName, topMoneyCard));
    private static string TextForTakingTopMoneyCard(string playerName, Card topMoneyCard) =>
        $"{playerName}, would you like to take the top money card? ({topMoneyCard.DisplayValue})";

    internal static Card PlayerDiscard(string playerName)
    {
        var (discardRank, discardColor, discardSuit) = Common.DetermineCardRankSuitFromString(ResponseForDiscard(playerName));
        if (discardRank == CardRank.Joker) return new Card(discardRank, discardColor);
        return new Card(discardRank, discardSuit);
    }
    private static string ResponseForDiscard(string playerName) => AnsiConsole.Ask<string>(TextForDiscard(playerName));
    private static string TextForDiscard(string playerName) =>
        $"{playerName}, which card would you like to discard? (e.g. 'AH' Ace of Hearts, 'JoR' Red Joker)";

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

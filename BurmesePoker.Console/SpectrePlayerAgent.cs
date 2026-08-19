using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

using Spectre.Console;

namespace BurmesePoker.Console;

/// <summary>
/// A human seat: the four questions the engine asks, put to somebody at the keyboard.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every prompt here is offered unconditionally, and that is safe</b>, because the engine
/// only asks a question that has a legal answer (BUILD-PLAN §3.5): <see cref="ChooseAction"/>
/// only when there is a discard to take, <see cref="ClaimTurnedUpMoneyCard"/> only on the
/// opening turn, and <see cref="Declare"/> only when the hand already wins. Adding a legality
/// check on this side would be duplicating the rules outside the domain.
/// </para>
/// <para>
/// <b>Concealment is handled by the keyboard changing hands.</b> Play is fully concealed
/// (RULES.md §6.3) but every seat is at the same terminal, so a turn begins by clearing the
/// screen and waiting for the named player to say they are the one looking.
/// </para>
/// </remarks>
public sealed class SpectrePlayerAgent : IPlayerAgent
{
    private readonly IReadOnlyDictionary<PlayerId, string> _names;
    private (int Round, int Turn) _turnInProgress;

    public SpectrePlayerAgent(IReadOnlyDictionary<PlayerId, string> names) =>
        _names = names ?? throw new ArgumentNullException(nameof(names));

    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        BeginTurn(context);

        // The two turned-up cards lie bottom-first, so the last is the one off the top of
        // the deck — the claimable one (RULES.md §3 step 4, §4.5).
        var card = context.TurnedUpMoneyCards[^1];

        AnsiConsole.MarkupLine(
            $"The top money card is {CardFormatting.Of(card, context.MoneyCards)}. Taking it "
            + "costs you your draw, and the table — not the deck — gives it, so it [yellow]pays nobody[/].");

        return AnsiConsole.Confirm("Take it instead of drawing?", defaultValue: false);
    }

    public TurnAction ChooseAction(TurnContext context)
    {
        BeginTurn(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        return AnsiConsole.Prompt(
            new SelectionPrompt<TurnAction>()
                .Title($"{Who(context.Player)}, how will you take your card?")
                .UseConverter(action => action switch
                {
                    TurnAction.TakeDiscard =>
                        $"Take the discard {CardFormatting.Of(discard, context.MoneyCards)} "
                        + "[grey](you will not own it)[/]",
                    _ =>
                        $"Draw blind [grey]({context.DrawPileCount} left — a drawn money card pays you)[/]"
                })
                .AddChoices(TurnAction.TakeDiscard, TurnAction.DrawFromDeck));
    }

    public Card ChooseDiscard(TurnContext context)
    {
        BeginTurn(context);

        if (context.Taken is { } taken)
        {
            AnsiConsole.MarkupLine(
                $"You took {CardFormatting.Of(taken, context.MoneyCards, context.YouOwn(taken))}.");
        }

        ShowHand(context);

        return AnsiConsole.Prompt(
            new SelectionPrompt<Card>()
                .Title("Which card will you throw away?")
                .PageSize(15)
                .MoreChoicesText("[grey](move up and down for the rest of your hand)[/]")
                .UseConverter(card => CardFormatting.Of(card, context.MoneyCards, context.YouOwn(card)))
                .AddChoices(CardFormatting.Sorted(context.Hand)));
    }

    public bool Declare(TurnContext context)
    {
        BeginTurn(context);

        if (HandEvaluator.TryFindCover(context.Hand, out var cover))
        {
            AnsiConsole.MarkupLine($"[green]All thirteen melt.[/] {Who(context.Player)} can go out with:");

            foreach (var meld in CardFormatting.Cover(cover))
            {
                AnsiConsole.MarkupLine($"  {meld}");
            }
        }

        return AnsiConsole.Confirm("Declare and end the round?");
    }

    /// <summary>
    /// Hands the keyboard over once per turn, whichever of the four questions comes first.
    /// </summary>
    /// <remarks>
    /// The turn is tracked by <see cref="TurnContext.TurnNumber"/> rather than by counting
    /// calls, because a turn asks a different number of questions depending on what is
    /// available — the opening turn is offered the money card, later turns are offered the
    /// discard, and only a winning hand is offered the declaration.
    /// <b>The round has to be part of it</b>: an agent lives for the whole match, and turn 1
    /// of round 2 is a different turn from turn 1 of round 1 — tracking the number alone left
    /// the opener's cards on screen at the start of every round after the first.
    /// </remarks>
    private void BeginTurn(TurnContext context)
    {
        if ((context.Round, context.TurnNumber) == _turnInProgress)
        {
            return;
        }

        _turnInProgress = (context.Round, context.TurnNumber);

        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"Turn {context.TurnNumber} — {Who(context.Player)}").LeftJustified());
        AnsiConsole.MarkupLine("[grey]Nobody else should be looking.[/]");
        AnsiConsole.Confirm($"{Who(context.Player)}, are you at the keyboard?", defaultValue: true);

        ShowTable(context);
        ShowHand(context);
    }

    private void ShowTable(TurnContext context)
    {
        var turnedUp = context.TurnedUpMoneyCards.Count == 0
            ? "[grey]none left on the table[/]"
            : string.Join("  ", context.TurnedUpMoneyCards.Select(card => CardFormatting.Of(card, context.MoneyCards)));

        var table = new Grid().AddColumn().AddColumn();
        table.AddRow("[grey]Turned up[/]", turnedUp);
        table.AddRow("[grey]Draw pile[/]", $"{context.DrawPileCount} cards");
        table.AddRow(
            "[grey]Discard[/]",
            context.AvailableDiscard is { } discard
                ? CardFormatting.Of(discard, context.MoneyCards)
                : "[grey]nothing to take[/]");
        table.AddRow("[grey]Stakes[/]", $"${context.Stakes.RoundValue} a round · ${context.Stakes.MoneyCardValue} a money card");

        AnsiConsole.Write(new Panel(table).Header("The table").BorderColor(Color.Grey));
    }

    private static void ShowHand(TurnContext context)
    {
        AnsiConsole.MarkupLine(CardFormatting.Hand(context.Hand, context.MoneyCards, context.YouOwn));
        AnsiConsole.MarkupLine(CardFormatting.Legend);
        AnsiConsole.WriteLine();
    }

    private string Who(PlayerId player) => CardFormatting.Name(_names, player);
}

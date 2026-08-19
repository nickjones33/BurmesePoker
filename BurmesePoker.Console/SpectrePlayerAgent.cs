using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
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
/// screen and waiting for the named player to say they are the one looking. What the clear
/// destroys, the <see cref="RoundLog"/> puts back: everything public that was said while this
/// player was not looking (BUILD-PLAN P11).
/// </para>
/// <para>
/// <b>The hint is the computer's own answer, not a second opinion.</b> Asking
/// <see cref="GreedyBotAgent"/> what it would throw costs one call and cannot drift from how
/// the bots at this table actually play; re-deriving a recommendation here would be a second
/// strategy pretending to be the first. The per-card cost beside it is a reading of
/// <see cref="PartialCover"/> and agrees with it by construction (see <see cref="HandView"/>).
/// </para>
/// </remarks>
public sealed class SpectrePlayerAgent : IPlayerAgent
{
    private readonly IReadOnlyDictionary<PlayerId, string> _names;
    private readonly RoundLog _log;
    private readonly bool _hints;
    private readonly GreedyBotAgent _adviser = new();
    private (int Round, int Turn) _turnInProgress;

    public SpectrePlayerAgent(IReadOnlyDictionary<PlayerId, string> names, RoundLog log, bool hints = true)
    {
        _names = names ?? throw new ArgumentNullException(nameof(names));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _hints = hints;
    }

    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        BeginTurn(context);

        // The two turned-up cards lie bottom-first, so the last is the one off the top of
        // the deck — the claimable one (RULES.md §3 step 4, §4.5).
        var card = context.TurnedUpMoneyCards[^1];

        AnsiConsole.MarkupLine(
            $"The top money card is {CardFormatting.Of(card, context.MoneyCards)}. Taking it "
            + $"costs you your draw, and the table — not the deck — gives it, so it [{Palette.Money}]pays nobody[/].");

        if (_hints)
        {
            AnsiConsole.MarkupLine(Advice(
                _adviser.ClaimTurnedUpMoneyCard(context)
                    ? "take it — it melds more of your hand than what you are holding"
                    : "draw instead — it melds no more of your hand than what you are holding, and a blind draw might"));
        }

        return AnsiConsole.Confirm("Take it instead of drawing?", defaultValue: false);
    }

    public TurnAction ChooseAction(TurnContext context)
    {
        BeginTurn(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        if (_hints)
        {
            AnsiConsole.MarkupLine(Advice(
                _adviser.ChooseAction(context) == TurnAction.TakeDiscard
                    ? $"take {CardFormatting.Of(discard, context.MoneyCards)} — it melds more of your hand"
                    : "draw blind — the discard melds no more of your hand, and a drawn money card pays you"));
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<TurnAction>()
                .Title($"{Who(context.Player)}, how will you take your card?")
                .UseConverter(action => action switch
                {
                    TurnAction.TakeDiscard =>
                        $"Take the discard {CardFormatting.Of(discard, context.MoneyCards)} "
                        + $"[{Palette.Quiet}](you will not own it)[/]",
                    _ =>
                        $"Draw blind [{Palette.Quiet}]({context.DrawPileCount} left — a drawn money card pays you)[/]"
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

        var view = ShowHand(context);
        var advised = _hints ? _adviser.ChooseDiscard(context) : (Card?)null;

        return AnsiConsole.Prompt(
            new SelectionPrompt<Card>()
                .Title("Which card will you throw away?")
                .PageSize(15)
                .MoreChoicesText($"[{Palette.Quiet}](move up and down for the rest of your hand)[/]")
                .UseConverter(card => Choice(card, context, view, advised))
                .AddChoices(CardFormatting.Sorted(context.Hand)));
    }

    public bool Declare(TurnContext context)
    {
        BeginTurn(context);

        if (HandEvaluator.TryFindCover(context.Hand, out var cover))
        {
            AnsiConsole.MarkupLine($"[{Palette.Good}]All thirteen melt.[/] {Who(context.Player)} can go out with:");

            foreach (var meld in CardFormatting.Cover(cover))
            {
                AnsiConsole.MarkupLine($"  {meld}");
            }
        }

        return AnsiConsole.Confirm("Declare and end the round?");
    }

    /// <summary>
    /// One card as it appears in the discard list: the card, what throwing it costs, and
    /// whether the computer would throw it.
    /// </summary>
    private string Choice(Card card, TurnContext context, HandView view, Card? advised)
    {
        var face = CardFormatting.Of(card, context.MoneyCards, context.YouOwn(card));

        if (!_hints)
        {
            return face;
        }

        var cost = view.CostOfThrowing(card);

        var note = cost == 0
            ? $"[{Palette.Quiet}](melds nothing)[/]"
            : $"[{Palette.Bad}](breaks a meld — costs {cost})[/]";

        return card == advised
            ? $"{face} {note} [{Palette.Good}]{Palette.AdviceMark} the computer would throw this[/]"
            : $"{face} {note}";
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
        AnsiConsole.MarkupLine($"[{Palette.Quiet}]Nobody else should be looking.[/]");
        AnsiConsole.Confirm($"{Who(context.Player)}, are you at the keyboard?", defaultValue: true);

        // The log first: what happened while this player was away is the thing the clear just
        // destroyed, and it is the context every other panel is read against.
        AnsiConsole.Write(_log.AsPanel());
        ShowTable(context);
        ShowHand(context);
    }

    private void ShowTable(TurnContext context)
    {
        var turnedUp = context.TurnedUpMoneyCards.Count == 0
            ? $"[{Palette.Quiet}]none left on the table[/]"
            : string.Join("  ", context.TurnedUpMoneyCards.Select(card => CardFormatting.Of(card, context.MoneyCards)));

        var table = new Grid().AddColumn().AddColumn();
        table.AddRow($"[{Palette.Quiet}]Turned up[/]", turnedUp);
        table.AddRow($"[{Palette.Quiet}]Draw pile[/]", $"{context.DrawPileCount} cards");
        table.AddRow(
            $"[{Palette.Quiet}]Discard[/]",
            context.AvailableDiscard is { } discard
                ? CardFormatting.Of(discard, context.MoneyCards)
                : $"[{Palette.Quiet}]nothing to take[/]");
        table.AddRow($"[{Palette.Quiet}]Stakes[/]", $"${context.Stakes.RoundValue} a round · ${context.Stakes.MoneyCardValue} a money card");

        AnsiConsole.Write(new Panel(table).Header("The table").BorderColor(Palette.Frame));
    }

    /// <summary>
    /// Draws the hand as the melds it nearly is, and hands back the cover it worked out so the
    /// discard list can annotate itself without searching the hand a second time.
    /// </summary>
    private static HandView ShowHand(TurnContext context)
    {
        var view = HandView.Of(context.Hand);

        AnsiConsole.Write(view.AsPanel(context.MoneyCards, context.YouOwn));
        AnsiConsole.MarkupLine(Palette.Legend);
        AnsiConsole.WriteLine();

        return view;
    }

    private static string Advice(string text) =>
        $"[{Palette.Quiet}]The computer would {text}.[/]";

    private string Who(PlayerId player) => CardFormatting.Name(_names, player);
}

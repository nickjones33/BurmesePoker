using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;

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
/// <b>The hint is the computer's own answer, not a second opinion.</b>
/// <see cref="ComputerAdvice"/> puts the question to the bot on this very context, which costs
/// one call and cannot drift from how the bots at this table actually play; re-deriving a
/// recommendation here would be a second strategy pretending to be the first. The per-card
/// cost beside it is a reading of the same <see cref="PartialCover"/> and agrees with it by
/// construction (see <see cref="HandView"/>).
/// </para>
/// <para>
/// <b>Nothing in this file works out what to show; it works out how to show it</b>
/// (BUILD-PLAN P13.1). The near-melds, the costs and each card's display state all come from
/// <see cref="HandView"/>, which a browser will read exactly as this does.
/// </para>
/// </remarks>
public sealed class SpectrePlayerAgent : IPlayerAgent
{
    private readonly IAnsiConsole _console;
    private readonly IReadOnlyDictionary<PlayerId, string> _names;
    private readonly RoundLog _log;
    private readonly bool _hints;
    private readonly ComputerAdvice _adviser = new();
    private (int Round, int Turn) _turnInProgress;

    /// <param name="console">
    /// The terminal to talk to. Taken rather than reached for, so the front end can be driven
    /// by something other than a real screen (BUILD-PLAN P13.1).
    /// </param>
    public SpectrePlayerAgent(
        IAnsiConsole console,
        IReadOnlyDictionary<PlayerId, string> names,
        RoundLog log,
        bool hints = true)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
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

        _console.MarkupLine(
            $"The top money card is {CardFormatting.Of(card, context.MoneyCards)}. Taking it "
            + $"costs you your draw, and the table — not the deck — gives it, so it [{Palette.Money}]pays nobody[/].");

        if (_hints)
        {
            _console.MarkupLine(Advice(
                _adviser.ClaimTurnedUpMoneyCard(context)
                    ? "take it — it melds more of your hand than what you are holding"
                    : "draw instead — it melds no more of your hand than what you are holding, and a blind draw might"));
        }

        return _console.Confirm("Take it instead of drawing?", defaultValue: false);
    }

    public TurnAction ChooseAction(TurnContext context)
    {
        BeginTurn(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        if (_hints)
        {
            _console.MarkupLine(Advice(
                _adviser.Take(context) == TurnAction.TakeDiscard
                    ? $"take {CardFormatting.Of(discard, context.MoneyCards)} — it melds more of your hand"
                    : "draw blind — the discard melds no more of your hand, and a drawn money card pays you"));
        }

        return _console.Prompt(
            new SelectionPrompt<TurnAction>()
                .Title($"{Who(context.Player)}, how will you take your card?")
                .UseConverter(action => action switch
                {
                    TurnAction.TakeDiscard =>
                        $"Take the discard {CardFormatting.Of(discard, context.MoneyCards)} "
                        + $"[{Palette.Quiet}](you will not own it)[/]",
                    TurnAction.DrawFromDeck =>
                        $"Draw blind [{Palette.Quiet}]({context.DrawPileCount} left — a drawn money card pays you)[/]",
                    // ⚠️ Named cases only, never a default label: a residual arm here would put
                    // "Draw blind" on any TurnAction added later, and a mislabelled menu is
                    // worse than a crash a test can see.
                    _ => throw new InvalidOperationException($"No menu label for taking a card by '{action}'.")
                })
                // ⚠️ TakeDiscard stays listed first. Spectre opens a SelectionPrompt<T> on
                // default(T) whenever that value is among the choices, and default(TurnAction)
                // is TakeDiscard — the exact trap P18 found in the difficulty menu, benign here
                // only while the default value is also the first choice.
                .AddChoices(TurnAction.TakeDiscard, TurnAction.DrawFromDeck));
    }

    public Card ChooseDiscard(TurnContext context)
    {
        BeginTurn(context);

        if (context.Taken is { } taken)
        {
            _console.MarkupLine(
                $"You took {CardFormatting.Of(taken, context.MoneyCards, context.YouOwn(taken))}.");
        }

        // The advice is worked out before the hand is drawn only because the view model wants
        // it; asking costs one call and prints nothing, so the screen is unchanged by it.
        var view = ShowHand(context, _hints ? _adviser.Discard(context) : null);

        // ⚠️ The legal cards, not the hand (RULES.md §5.1). A rank the seat below you has taken in
        // the open is an impossible move rather than an infraction, so it is not on this list at
        // all — the hand above still draws it, marked, because a player has to be told why a card
        // they are holding is not among the choices.
        var choices = view.Cards.Where(card => card.CanBeThrown).Select(card => card.Card).ToArray();

        if (choices.Length < view.Cards.Count)
        {
            _console.MarkupLine(
                $"[{Palette.Quiet}]{DisplayTokens.Closed}: the player after you took that rank in the open, "
                + $"so you may not throw it back to them (RULES.md §5.1).[/]");
        }

        return _console.Prompt(
            new SelectionPrompt<Card>()
                .Title("Which card will you throw away?")
                .PageSize(15)
                .MoreChoicesText($"[{Palette.Quiet}](move up and down for the rest of your hand)[/]")
                .UseConverter(card => Choice(view.Of(card)))
                .AddChoices(choices));
    }

    /// <summary>
    /// The one question asked while it is somebody else's turn: the opener wants the turned-up
    /// money card, and this seat may refuse it (RULES.md §4.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>It hands the keyboard over on its own rather than through <see cref="BeginTurn"/>.</b>
    /// The turn number this arrives with is the <em>opener's</em>, so the usual header would
    /// announce a turn that belongs to somebody else — and it must not consume the turn either,
    /// because this seat's own turn is still coming and still wants its clear.
    /// </para>
    /// <para>
    /// ⚠️ <b>Offered unconditionally, and that is safe.</b> The engine asks only a seat that is
    /// holding the rank (RULES.md §4.5), exactly as it offers a declaration only to a hand that
    /// wins — checking again on this side would be the rules written twice.
    /// </para>
    /// </remarks>
    public bool ObjectToClaim(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var asking = context.PermissionAsked
            ?? throw new InvalidOperationException("Asked to permit a claim with no claim on the table.");

        _console.Clear();
        _console.Write(new Rule($"Permission — {Who(context.Player)}").LeftJustified());
        _console.MarkupLine($"[{Palette.Quiet}]Nobody else should be looking.[/]");
        _console.Confirm($"{Who(context.Player)}, are you at the keyboard?", defaultValue: true);

        _console.Write(_log.AsPanel());
        ShowHand(context);

        _console.MarkupLine(
            $"{Who(asking.Claimant)} wants the turned-up {CardFormatting.Of(asking.Card, context.MoneyCards)} "
            + $"off the table, and plays [bold]after[/] you. You are holding that rank, which is the only "
            + $"reason you may say no [{Palette.Quiet}](RULES.md §4.5)[/].");
        _console.MarkupLine(
            $"[{Palette.Quiet}]If they take it, you may not throw that rank to them for the rest of the "
            + $"round (RULES.md §5.1).[/]");

        if (_hints)
        {
            _console.MarkupLine(Advice(
                _adviser.ObjectToClaim(context)
                    ? "refuse — letting it through closes that rank in your hand until they release it"
                    : "allow it — the rank costs you less than the objection tells the table"));
        }

        return _console.Confirm("Refuse them the card?", defaultValue: true);
    }

    public bool Declare(TurnContext context)
    {
        BeginTurn(context);

        if (HandEvaluator.TryFindCover(context.Hand, context.Rules, out var cover))
        {
            _console.MarkupLine(
                context.TurnNumber == 0
                    ? $"[{Palette.Good}]All thirteen melt, straight off the deal.[/] "
                      + $"{Who(context.Player)} can go out before anybody draws — and RULES.md §7.4 "
                      + "pays double for it:"
                    : $"[{Palette.Good}]All thirteen melt.[/] {Who(context.Player)} can go out with:");

            foreach (var meld in CardFormatting.Cover(cover))
            {
                _console.MarkupLine($"  {meld}");
            }
        }

        return _console.Confirm("Declare and end the round?");
    }

    /// <summary>
    /// Shall we change seats (RULES.md §3 step 2, §9 #45)?
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>The sixth question, and the only one asked between rounds rather than on a turn.</b>
    /// Every seat is asked, and the seats move only if somebody asked and nobody refused — so the
    /// three answers are three answers here too. ⚠️ <b>Leaving them alone is first and is what
    /// return takes</b>, because that is the rule: a seating is held.
    /// </para>
    /// <para>
    /// ⚠️ <b>At a hotseat console every person is asked in turn, which is the point.</b> One
    /// player asking is not agreement (§9 #45, ruled by Nick), so the second person's chance to
    /// say <em>no</em> has to be a question they are actually put.
    /// </para>
    /// </remarks>
    public SeatingOpinion AskAboutTheSeating(SeatingQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        _console.WriteLine();

        return _console.Prompt(
            new SelectionPrompt<SeatingOpinion>()
                .Title($"{Who(question.Player)} — before round {question.Round}, change seats?")
                .UseConverter(opinion => opinion switch
                {
                    SeatingOpinion.Ask => $"yes, let's [{Palette.Quiet}](ask the table to change seats)[/]",
                    SeatingOpinion.Refuse => $"no [{Palette.Quiet}](and nobody else's asking can move them)[/]",
                    _ => $"leave them [{Palette.Quiet}](happy either way — the seating holds)[/]"
                })
                .AddChoices(SeatingOpinion.Consent, SeatingOpinion.Ask, SeatingOpinion.Refuse));
    }

    /// <summary>
    /// One card as it appears in the discard list: the card, what throwing it costs, and
    /// whether the computer would throw it.
    /// </summary>
    /// <remarks>
    /// All three come off the <see cref="CardView"/>; this only chooses words and colours for
    /// them.
    /// </remarks>
    private string Choice(CardView card)
    {
        var face = CardFormatting.Of(card);

        if (!_hints)
        {
            return face;
        }

        var note = card.CostOfThrowing == 0
            ? $"[{Palette.Quiet}](melds nothing)[/]"
            : $"[{Palette.Bad}](breaks a meld — costs {card.CostOfThrowing})[/]";

        return card.IsSuggestedThrow
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

        _console.Clear();

        // ⚠️ Turn 0 is not a turn: it is the one question asked before anybody draws, when the
        // thirteen dealt already win (RULES.md §7.4). Calling it "Turn 0" would be a worse lie
        // than calling it nothing.
        _console.Write(new Rule(
            context.TurnNumber == 0
                ? $"The deal — {Who(context.Player)}"
                : $"Turn {context.TurnNumber} — {Who(context.Player)}").LeftJustified());
        _console.MarkupLine($"[{Palette.Quiet}]Nobody else should be looking.[/]");
        _console.Confirm($"{Who(context.Player)}, are you at the keyboard?", defaultValue: true);

        // The log first: what happened while this player was away is the thing the clear just
        // destroyed, and it is the context every other panel is read against.
        _console.Write(_log.AsPanel());
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

        _console.Write(new Panel(table).Header("The table").BorderColor(Palette.Frame));
    }

    /// <summary>
    /// Draws the hand as the melds it nearly is, and hands back the cover it worked out so the
    /// discard list can annotate itself without searching the hand a second time.
    /// </summary>
    private HandView ShowHand(TurnContext context, Card? advised = null)
    {
        var view = HandView.Of(context, advised);

        _console.Write(HandPanel.Of(view));
        _console.MarkupLine(Palette.Legend);
        _console.WriteLine();

        return view;
    }

    private static string Advice(string text) =>
        $"[{Palette.Quiet}]The computer would {text}.[/]";

    private string Who(PlayerId player) => CardFormatting.Name(_names, player);
}

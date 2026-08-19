using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

using Spectre.Console;

namespace BurmesePoker.Console;

/// <summary>
/// Sets a round up and plays it. Everything else here is presentation.
/// </summary>
/// <remarks>
/// <para>
/// <b>Seating is randomised here, not in the engine</b> (RULES.md §3 step 2). A round engine
/// that reshuffled its own seating could not be scripted, so it takes the order it is given —
/// which is also the order settlement is handed.
/// </para>
/// <para>
/// <b>One round.</b> Repeated rounds with banks carrying over are P9's <c>MatchEngine</c>, and
/// so is the reshuffle when the draw pile runs dry — until then an exhausted deck ends the
/// programme with an explanation rather than a stack trace.
/// </para>
/// </remarks>
internal static class Program
{
    private static int Main()
    {
        AnsiConsole.Write(new Rule("[bold]Burmese Poker[/]").LeftJustified());

        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            AnsiConsole.MarkupLine(
                "[red]This game needs a terminal it can read keys from[/] — every seat is a "
                + "person at the keyboard. Run it directly rather than through a pipe.");
            return 1;
        }

        var names = AskWhoIsPlaying();
        var stakes = AskStakes();
        var seating = Seat(names);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            "Seating: " + string.Join(" → ", seating.Select(player => CardFormatting.Name(names, player))));
        AnsiConsole.MarkupLine($"[grey]{CardFormatting.Name(names, seating[0])} opens.[/]");
        AnsiConsole.WriteLine();

        var observer = new ConsoleObserver(names);
        var engine = RoundEngine.Shuffled(
            seating,
            seating.ToDictionary(player => player, IPlayerAgent (_) => new SpectrePlayerAgent(names)),
            stakes,
            Random.Shared,
            round: 1,
            observer);

        try
        {
            var result = engine.Play();
            ReportSettlement(result, engine.Table, names);
            return 0;
        }
        catch (DeckExhaustedException)
        {
            AnsiConsole.MarkupLine(
                "[yellow]The draw pile ran out before anybody went out.[/] Gathering the "
                + "discards and playing on is P9's job (RULES.md §5); for now the round ends here.");
            return 1;
        }
    }

    private static Dictionary<PlayerId, string> AskWhoIsPlaying()
    {
        var count = AnsiConsole.Prompt(
            new TextPrompt<int>($"How many players? [grey]({RoundEngine.MinimumPlayers}–{RoundEngine.MaximumPlayers})[/]")
                .DefaultValue(RoundEngine.MinimumPlayers)
                .Validate(value => value is >= RoundEngine.MinimumPlayers and <= RoundEngine.MaximumPlayers
                    ? ValidationResult.Success()
                    : ValidationResult.Error(
                        $"[red]A round is for {RoundEngine.MinimumPlayers} to {RoundEngine.MaximumPlayers} players (RULES.md §2.1).[/]")));

        return Enumerable.Range(1, count).ToDictionary(
            seat => new PlayerId(seat),
            seat => AnsiConsole.Prompt(
                new TextPrompt<string>($"Name for player {seat}?").DefaultValue($"Player {seat}")));
    }

    private static Stakes AskStakes()
    {
        AnsiConsole.WriteLine();

        var round = AskAmount("What does the round pay?", Stakes.Standard.RoundValue);
        var money = AskAmount("What does a money card pay, per player?", Stakes.Standard.MoneyCardValue);

        return new Stakes(round, money);
    }

    private static int AskAmount(string question, int standard) =>
        AnsiConsole.Prompt(
            new TextPrompt<int>($"{question} [grey]($)[/]")
                .DefaultValue(standard)
                .Validate(value => value > 0
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]A stake is an amount of money changing hands, so it must be positive.[/]")));

    /// <summary>Randomises the seating, which is also the turn order (RULES.md §3 step 2).</summary>
    private static PlayerId[] Seat(Dictionary<PlayerId, string> names)
    {
        var seating = names.Keys.ToArray();
        Random.Shared.Shuffle(seating);
        return seating;
    }

    /// <summary>
    /// Draws the money that moved, split into its two halves.
    /// </summary>
    /// <remarks>
    /// <b>Settlement hands over net deltas only</b> (BUILD-PLAN P8), so the split is worked
    /// out here: the round payment is flat and known from the winner alone (RULES.md §7.2),
    /// and whatever is left over is the money-card side bet. Doing it this way round means
    /// the two halves always add up to what the domain actually settled.
    /// </remarks>
    private static void ReportSettlement(
        RoundResult result,
        TableState table,
        IReadOnlyDictionary<PlayerId, string> names)
    {
        var players = table.Players;
        var owned = PayingCardsByOwner(table);

        var grid = new Table().Border(TableBorder.Rounded);
        grid.AddColumn("Player");
        grid.AddColumn("Money cards owned");
        grid.AddColumn(new TableColumn("Round").RightAligned());
        grid.AddColumn(new TableColumn("Money cards").RightAligned());
        grid.AddColumn(new TableColumn("Net").RightAligned());

        foreach (var player in players)
        {
            var net = result.Payouts[player];
            var round = player == result.Winner
                ? table.Stakes.RoundValue * (players.Count - 1)
                : -table.Stakes.RoundValue;

            grid.AddRow(
                CardFormatting.Name(names, player) + (player == result.Winner ? " [green](out)[/]" : string.Empty),
                owned.TryGetValue(player, out var cards) && cards.Count > 0
                    ? string.Join("  ", cards.Select(card => CardFormatting.Of(card, table.MoneyCards)))
                    : "[grey]—[/]",
                Amount(round),
                Amount(net - round),
                $"[bold]{Amount(net)}[/]");
        }

        AnsiConsole.Write(grid);
        AnsiConsole.MarkupLine(
            "[grey]A money card pays its owner — whoever the deck gave it to — whether they "
            + "still hold it or threw it away (RULES.md §4.4).[/]");
    }

    /// <summary>
    /// The money cards each player owns, resolved through the index-aligned shoe.
    /// </summary>
    /// <remarks>
    /// Ownership is recorded by <see cref="CardId"/> because it is about the physical card,
    /// while designation is by value — so the card itself has to be looked up before the
    /// registry can be asked what it pays (BUILD-PLAN §3.1). <see cref="TableState.Shoe"/> is
    /// the unshuffled shoe, where a card sits at the index of its own id.
    /// </remarks>
    private static Dictionary<PlayerId, List<Card>> PayingCardsByOwner(TableState table)
    {
        var owned = table.Players.ToDictionary(player => player, _ => new List<Card>());

        foreach (var (id, owner) in table.Ownership.Records)
        {
            var card = table.Shoe[id.Value];

            if (table.MoneyCards.Multiplier(card) > 0)
            {
                owned[owner].Add(card);
            }
        }

        return owned.ToDictionary(entry => entry.Key, entry => CardFormatting.Sorted(entry.Value).ToList());
    }

    private static string Amount(int money) => money switch
    {
        > 0 => $"[green]+${money}[/]",
        < 0 => $"[red]-${-money}[/]",
        _ => "[grey]$0[/]"
    };
}

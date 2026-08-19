using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
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
/// <b>Any seat the people do not fill is played by the computer.</b> A bot is just another
/// <see cref="IPlayerAgent"/> (BUILD-PLAN P10), so the engine cannot tell which seats are
/// which and neither can this file past the point where the agents are built — the whole of
/// solo play is one line in the dictionary below.
/// </para>
/// <para>
/// <b>Round after round, until the table says stop.</b> Nothing ends a match on its own
/// (RULES.md §7.2), so <em>"another round?"</em> is asked here rather than in the domain — it
/// is not a move, so it is not a question for an agent either. The banks are the
/// <see cref="MatchEngine"/>'s to keep and this file's to draw.
/// </para>
/// </remarks>
internal static class Program
{
    /// <summary>
    /// What the computer's seats are called. One per seat, because a table can be all bots — and
    /// named rather than numbered, since the narration reads as a table of players either way.
    /// </summary>
    private static readonly string[] BotNames = ["Ruby", "Sable", "Onyx", "Jade", "Coral", "Amber"];

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

        var (names, bots) = AskWhoIsPlaying();
        var stakes = AskStakes();
        var seating = Seat(names);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            "Seating: " + string.Join(" → ", seating.Select(player => CardFormatting.Name(names, player))));
        AnsiConsole.MarkupLine($"[grey]{CardFormatting.Name(names, seating[0])} opens.[/]");
        AnsiConsole.WriteLine();

        var match = new MatchEngine(
            seating,
            seating.ToDictionary(
                player => player,
                IPlayerAgent (player) => bots.Contains(player)
                    ? new GreedyBotAgent()
                    : new SpectrePlayerAgent(names)),
            stakes,
            Random.Shared,
            new ConsoleObserver(names));

        try
        {
            do
            {
                var played = match.PlayRound();
                ReportSettlement(played.Result, played.Table, names);
                ReportStandings(match, names);
            }
            while (AnsiConsole.Confirm("Another round?"));

            AnsiConsole.MarkupLine(
                $"[grey]{match.RoundsPlayed} round{(match.RoundsPlayed == 1 ? string.Empty : "s")} played. "
                + "Nothing ends a game but the players (RULES.md §7.2).[/]");

            return 0;
        }
        catch (DeckExhaustedException)
        {
            // The reshuffle (RULES.md §5) means this now takes the draw pile *and* every
            // discard pile being empty at once, which is a genuine end state rather than the
            // crash it used to be.
            AnsiConsole.MarkupLine(
                "[yellow]There is nothing left to draw anywhere[/] — not in the deck and not in "
                + "a discard pile — so the round cannot go on. The standings stand as they were.");

            ReportStandings(match, names);
            return 1;
        }
    }

    /// <summary>
    /// Draws the running banks. They start at zero and carry over, and nothing resets them —
    /// there is no target score and no round limit (RULES.md §7.2).
    /// </summary>
    private static void ReportStandings(MatchEngine match, IReadOnlyDictionary<PlayerId, string> names)
    {
        if (match.RoundsPlayed == 0)
        {
            return;
        }

        var grid = new Table().Border(TableBorder.Rounded)
            .Title("[bold]Standings[/]")
            .Caption($"[grey]after {match.RoundsPlayed} round{(match.RoundsPlayed == 1 ? string.Empty : "s")}[/]");

        grid.AddColumn("Player");
        grid.AddColumn(new TableColumn("Bank").RightAligned());

        foreach (var (player, bank) in match.Banks.OrderByDescending(entry => entry.Value))
        {
            grid.AddRow(CardFormatting.Name(names, player), $"[bold]{Amount(bank)}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Who is at the table, and which of them are people.
    /// </summary>
    /// <remarks>
    /// A round is for four to six (RULES.md §2.1) however many of them are breathing, so the
    /// table is sized first and the people are counted out of it. None is allowed: it leaves
    /// the computer playing itself, which is worth watching once.
    /// </remarks>
    private static (Dictionary<PlayerId, string> Names, HashSet<PlayerId> Bots) AskWhoIsPlaying()
    {
        var count = AnsiConsole.Prompt(
            new TextPrompt<int>($"How many at the table? [grey]({RoundEngine.MinimumPlayers}–{RoundEngine.MaximumPlayers})[/]")
                .DefaultValue(RoundEngine.MinimumPlayers)
                .Validate(value => value is >= RoundEngine.MinimumPlayers and <= RoundEngine.MaximumPlayers
                    ? ValidationResult.Success()
                    : ValidationResult.Error(
                        $"[red]A round is for {RoundEngine.MinimumPlayers} to {RoundEngine.MaximumPlayers} players (RULES.md §2.1).[/]")));

        var people = AnsiConsole.Prompt(
            new TextPrompt<int>($"How many of you are people? [grey](0–{count}; the rest are played by the computer)[/]")
                .DefaultValue(1)
                .Validate(value => value >= 0 && value <= count
                    ? ValidationResult.Success()
                    : ValidationResult.Error($"[red]There are only {count} seats.[/]")));

        var names = new Dictionary<PlayerId, string>(count);
        var bots = new HashSet<PlayerId>();

        for (var seat = 1; seat <= count; seat++)
        {
            var player = new PlayerId(seat);

            if (seat <= people)
            {
                names[player] = AnsiConsole.Prompt(
                    new TextPrompt<string>($"Name for player {seat}?").DefaultValue($"Player {seat}"));
            }
            else
            {
                names[player] = $"{BotNames[seat - people - 1]} (bot)";
                bots.Add(player);
            }
        }

        return (names, bots);
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

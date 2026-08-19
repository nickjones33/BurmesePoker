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
/// <para>
/// <b>Every match is seeded, and says so</b> (BUILD-PLAN P11). One <see cref="Random"/> drives
/// the seating and every shuffle, so <c>--seed</c> replays a strange match exactly; when none
/// is given one is drawn and printed, which means the match just played can always be asked
/// for again. This is the same determinism the simulation harness is built on (P12) — the
/// console simply has to refrain from reaching for <see cref="Random.Shared"/> twice.
/// </para>
/// </remarks>
internal static class Program
{
    /// <summary>
    /// What the computer's seats are called. One per seat, because a table can be all bots — and
    /// named rather than numbered, since the narration reads as a table of players either way.
    /// </summary>
    private static readonly string[] BotNames = ["Ruby", "Sable", "Onyx", "Jade", "Coral", "Amber"];

    /// <summary>How many rounds of history the between-round summary shows.</summary>
    private const int HistoryShown = 12;

    private static int Main(string[] args)
    {
        if (!Options.TryParse(args, out var options, out var complaint))
        {
            AnsiConsole.MarkupLine($"[{Palette.Bad}]{complaint}[/]");
            Options.Usage();
            return 1;
        }

        if (options.Help)
        {
            Options.Usage();
            return 0;
        }

        AnsiConsole.Write(new Rule("[bold]Burmese Poker[/]").LeftJustified());

        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            AnsiConsole.MarkupLine(
                $"[{Palette.Bad}]This game needs a terminal it can read keys from[/] — every seat is a "
                + "person at the keyboard. Run it directly rather than through a pipe.");
            return 1;
        }

        var random = new Random(options.Seed);
        var (names, bots) = AskWhoIsPlaying();
        var difficulty = bots.Count > 0 ? AskDifficulty() : Difficulty.Hard;
        var stakes = AskStakes();
        var seating = Seat(names, random);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            "Seating: " + string.Join(" → ", seating.Select(player => CardFormatting.Name(names, player))));
        AnsiConsole.MarkupLine($"[{Palette.Quiet}]{CardFormatting.Name(names, seating[0])} opens.[/]");
        AnsiConsole.MarkupLine(
            $"[{Palette.Quiet}]Seed {options.Seed} — [/][bold]--seed {options.Seed}[/]"
            + $"[{Palette.Quiet}] plays this exact match again.[/]");
        AnsiConsole.WriteLine();

        var log = new RoundLog();

        var match = new MatchEngine(
            seating,
            seating.ToDictionary(
                player => player,
                IPlayerAgent (player) => bots.Contains(player)
                    ? PacedAgent.Wrap(Bot(difficulty), options.Pace)
                    : new SpectrePlayerAgent(names, log, options.Hints)),
            stakes,
            random,
            new ConsoleObserver(names, log));

        var history = new List<RoundResult>();

        try
        {
            do
            {
                var played = match.PlayRound();
                history.Add(played.Result);

                ReportSettlement(played.Result, played.Table, names);
                ReportHistory(history, names);
                ReportStandings(match, history, names);
            }
            while (AnsiConsole.Confirm("Another round?"));

            AnsiConsole.MarkupLine(
                $"[{Palette.Quiet}]{match.RoundsPlayed} round{(match.RoundsPlayed == 1 ? string.Empty : "s")} played, "
                + $"seed {options.Seed}. Nothing ends a game but the players (RULES.md §7.2).[/]");

            return 0;
        }
        catch (DeckExhaustedException)
        {
            // The reshuffle (RULES.md §5) means this now takes the draw pile *and* every
            // discard pile being empty at once, which is a genuine end state rather than the
            // crash it used to be.
            AnsiConsole.MarkupLine(
                $"[{Palette.Money}]There is nothing left to draw anywhere[/] — not in the deck and not in "
                + "a discard pile — so the round cannot go on. The standings stand as they were.");

            ReportStandings(match, history, names);
            return 1;
        }
    }

    /// <summary>How hard the computer plays. Both seats already exist in the domain (BUILD-PLAN P12).</summary>
    private enum Difficulty
    {
        Easy,
        Hard
    }

    /// <summary>
    /// A bot of the chosen strength.
    /// </summary>
    /// <remarks>
    /// <b>A difficulty setting came for free out of the simulation.</b> <c>SimpleBotAgent</c>
    /// is <c>GreedyBotAgent</c> with the discard tie-break removed and nothing else changed,
    /// and it is measurably weaker for it — 19.3% of rounds against 30.7% over two thousand of
    /// them (BUILD-PLAN P12). It was built as a control and turns out to be the easy seat.
    /// </remarks>
    private static IPlayerAgent Bot(Difficulty difficulty) =>
        difficulty == Difficulty.Easy ? new SimpleBotAgent() : new GreedyBotAgent();

    /// <summary>
    /// Draws the running banks, with how many rounds each player has taken.
    /// </summary>
    /// <remarks>
    /// They start at zero and carry over, and nothing resets them — there is no target score
    /// and no round limit (RULES.md §7.2).
    /// </remarks>
    private static void ReportStandings(
        MatchEngine match,
        IReadOnlyList<RoundResult> history,
        IReadOnlyDictionary<PlayerId, string> names)
    {
        if (match.RoundsPlayed == 0)
        {
            return;
        }

        var grid = new Table().Border(TableBorder.Rounded)
            .Title("[bold]Standings[/]")
            .Caption($"[{Palette.Quiet}]after {match.RoundsPlayed} round{(match.RoundsPlayed == 1 ? string.Empty : "s")}[/]");

        grid.AddColumn("Player");
        grid.AddColumn(new TableColumn("Won").RightAligned());
        grid.AddColumn(new TableColumn("Bank").RightAligned());

        foreach (var (player, bank) in match.Banks.OrderByDescending(entry => entry.Value))
        {
            var won = history.Count(result => result.Winner == player);

            grid.AddRow(
                CardFormatting.Name(names, player),
                won == 0 ? $"[{Palette.Quiet}]—[/]" : won.ToString(),
                $"[bold]{Palette.Amount(bank)}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Who won each round and what it was worth.
    /// </summary>
    /// <remarks>
    /// <b>The history is remembered here because the domain deliberately does not keep one</b>
    /// (BUILD-PLAN §3.8): <see cref="MatchEngine"/> hands back a <c>RoundRecord</c> and drops
    /// the table, and a consumer that wants a history keeps the results it was given. One line
    /// a round rather than a column a player, so a six-handed table still fits an
    /// eighty-column terminal.
    /// </remarks>
    private static void ReportHistory(
        IReadOnlyList<RoundResult> history,
        IReadOnlyDictionary<PlayerId, string> names)
    {
        if (history.Count < 2)
        {
            return;
        }

        var shown = history.Count <= HistoryShown ? history : history.Skip(history.Count - HistoryShown).ToList();

        var lines = shown.Select(result =>
            $"[{Palette.Quiet}]Round {result.Round,2}[/]  {CardFormatting.Name(names, result.Winner)} "
            + $"won {Palette.Amount(result.Payouts[result.Winner])} "
            + $"[{Palette.Quiet}]in {result.Turns} turns[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Panel(string.Join(Environment.NewLine, lines))
                .Header(history.Count > HistoryShown
                    ? $"Rounds so far [{Palette.Quiet}](last {HistoryShown} of {history.Count})[/]"
                    : "Rounds so far")
                .BorderColor(Palette.Frame));
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
            new TextPrompt<int>($"How many at the table? [{Palette.Quiet}]({RoundEngine.MinimumPlayers}–{RoundEngine.MaximumPlayers})[/]")
                .DefaultValue(RoundEngine.MinimumPlayers)
                .Validate(value => value is >= RoundEngine.MinimumPlayers and <= RoundEngine.MaximumPlayers
                    ? ValidationResult.Success()
                    : ValidationResult.Error(
                        $"[{Palette.Bad}]A round is for {RoundEngine.MinimumPlayers} to {RoundEngine.MaximumPlayers} players (RULES.md §2.1).[/]")));

        var people = AnsiConsole.Prompt(
            new TextPrompt<int>($"How many of you are people? [{Palette.Quiet}](0–{count}; the rest are played by the computer)[/]")
                .DefaultValue(1)
                .Validate(value => value >= 0 && value <= count
                    ? ValidationResult.Success()
                    : ValidationResult.Error($"[{Palette.Bad}]There are only {count} seats.[/]")));

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

    private static Difficulty AskDifficulty() =>
        AnsiConsole.Prompt(
            new SelectionPrompt<Difficulty>()
                .Title("How well should the computer play?")
                .UseConverter(difficulty => difficulty switch
                {
                    Difficulty.Easy =>
                        $"Easy [{Palette.Quiet}](throws the first card that costs it nothing — wins about a fifth of rounds)[/]",
                    _ =>
                        $"Hard [{Palette.Quiet}](keeps the cards with partners — wins about a third)[/]"
                })
                .AddChoices(Difficulty.Hard, Difficulty.Easy));

    private static Stakes AskStakes()
    {
        AnsiConsole.WriteLine();

        var round = AskAmount("What does the round pay?", Stakes.Standard.RoundValue);
        var money = AskAmount("What does a money card pay, per player?", Stakes.Standard.MoneyCardValue);

        return new Stakes(round, money);
    }

    private static int AskAmount(string question, int standard) =>
        AnsiConsole.Prompt(
            new TextPrompt<int>($"{question} [{Palette.Quiet}]($)[/]")
                .DefaultValue(standard)
                .Validate(value => value > 0
                    ? ValidationResult.Success()
                    : ValidationResult.Error($"[{Palette.Bad}]A stake is an amount of money changing hands, so it must be positive.[/]")));

    /// <summary>
    /// Randomises the seating, which is also the turn order (RULES.md §3 step 2).
    /// </summary>
    /// <remarks>
    /// <b>From the match's own <see cref="Random"/></b>, not from <see cref="Random.Shared"/>:
    /// the seating is part of what a seed has to reproduce, and drawing it from anywhere else
    /// would leave <c>--seed</c> replaying the same deals to a different table.
    /// </remarks>
    private static PlayerId[] Seat(Dictionary<PlayerId, string> names, Random random)
    {
        var seating = names.Keys.ToArray();
        random.Shuffle(seating);
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
                CardFormatting.Name(names, player) + (player == result.Winner ? $" [{Palette.Good}](out)[/]" : string.Empty),
                owned.TryGetValue(player, out var cards) && cards.Count > 0
                    ? string.Join("  ", cards.Select(card => CardFormatting.Of(card, table.MoneyCards)))
                    : $"[{Palette.Quiet}]—[/]",
                Palette.Amount(round),
                Palette.Amount(net - round),
                $"[bold]{Palette.Amount(net)}[/]");
        }

        AnsiConsole.Write(grid);
        AnsiConsole.MarkupLine(
            $"[{Palette.Quiet}]A money card pays its owner — whoever the deck gave it to — whether they "
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
}

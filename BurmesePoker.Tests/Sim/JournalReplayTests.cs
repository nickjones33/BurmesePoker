using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Sim;

using BurmesePoker.Tests;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// Journals through the harness (packet P14): a run writes them, and <c>replay</c> reads them.
/// </summary>
/// <remarks>
/// <b>"It replays identically" is a diff here, not an impression.</b> A replayed run is
/// summarised by the same code a played one is (<see cref="Replay"/>), so the claim reduces to
/// two CSV files being the same text — which is exactly how P12 showed that a parallel run and
/// a serial one are the same run.
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class JournalReplayTests
{
    private static readonly SimulationOptions Run = new()
    {
        Strategies = [StrategyCatalog.Resolve("greedy"), StrategyCatalog.Resolve("simple")],
        Seats = 4,
        Games = 8,
        RoundsPerGame = 2,
        MasterSeed = 20260819
    };

    [Fact]
    public void AJournalledRunReplaysToTheSameRows()
    {
        // The packet's whole claim (BUILD-PLAN P14 acceptance 1), at the level a strategy
        // comparison is actually read at.
        var played = Simulator.Run(Run with { Journal = JournalFidelity.Thin });
        var journals = played.Games.Select(game => game.Journal!).ToList();

        Assert.Equal(CsvReport.Rows(played).ToList(), CsvReport.Rows(Replay.Run(journals)).ToList());
    }

    [Fact]
    public void JournallingDoesNotChangeTheGame()
    {
        // A decorator that altered what it observed would make every journal a record of a
        // game nobody played. Both fidelities, because the rich one copies a hand per
        // decision and is the likelier of the two to touch something.
        var quiet = CsvReport.Rows(Simulator.Run(Run)).ToList();

        Assert.Equal(quiet, CsvReport.Rows(Simulator.Run(Run with { Journal = JournalFidelity.Thin })).ToList());
        Assert.Equal(quiet, CsvReport.Rows(Simulator.Run(Run with { Journal = JournalFidelity.Rich })).ToList());
    }

    [Fact]
    public void NoJournalIsKeptUnlessOneIsAskedFor()
    {
        // §3.9's throughput constraint as a test: the default run holds nothing at all.
        Assert.All(Simulator.Run(Run).Games, game => Assert.Null(game.Journal));
    }

    /// <summary>
    /// ✅ <b>P36 — a journal from a table that re-seated replays under the policy it recorded</b>
    /// (RULES.md §3 step 2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>The harness cannot produce one of these</b> — every experiment runs one round a game
    /// and none offers the setting — so the journal is built the way a hosted table and the
    /// console build theirs: a <c>MatchEngine</c> under a policy, wrapped in the journalling
    /// agent, and a header that says which. That is the path <c>sim replay</c> is pointed at from
    /// a front end, and it is the path this field exists for.
    /// </para>
    /// <para>
    /// 🔥 <b>Without the field the replay deals different seats from round two on</b>, because the
    /// default is held and the recording was not: a replay would ask the right questions of the
    /// wrong player.
    /// </para>
    /// </remarks>
    [Fact]
    public void AJournalFromATableThatReseatedReplaysUnderThePolicyItRecorded()
    {
        var players = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, 4).Select(seat => new PlayerId(seat))];
        var builder = new GameJournalBuilder(JournalFidelity.Thin);

        var match = new MatchEngine(
            players,
            JournalingAgent.Wrap(
                players.ToDictionary(player => player, IPlayerAgent (_) => new GreedyBotAgent()), builder),
            Stakes.Standard,
            new Random(20260822),
            observer: null,
            SeatingPolicy.EveryRound);

        var played = Enumerable.Range(0, 3).Select(_ => match.PlayRound().Result).ToList();

        var journal = builder.Build(new JournalHeader(
            Seed: 20260822,
            Seats: [.. players.Select(player => new JournalSeat(player, "greedy", $"Seat {player.Value}"))],
            Stakes: Stakes.Standard,
            Rounds: played.Count,
            Game: 0,
            RoundsBetweenSeatings: SeatingPolicy.EveryRound.RoundsBetweenSeatings));

        // Through the file, because that is where a front end's journal comes from.
        var read = JournalFormat.Read(JournalFormat.Lines(journal).ToList());

        Assert.Equal(SeatingPolicy.EveryRound, read.Header.Seating);

        var replayed = GameRunner.Replay(read, game: 0);

        Assert.Equal(
            played.Select(result => $"{result.Round}:{result.Winner.Value}:{result.Turns}"),
            replayed.Rounds.Select(round =>
                $"{round.Round}:{round.Seats.Single(seat => seat.Won).Seat}:{round.Turns}"));
    }

    [Fact]
    public void AJournalWrittenToAFileReplaysFromIt()
    {
        // The packet's "done when", start to finish: --journal writes a file, replay reads it
        // back, and the two runs agree line for line.
        var played = Simulator.Run(Run with { Journal = JournalFidelity.Thin });
        var path = Path.Combine(Path.GetTempPath(), $"burmese-poker-{Guid.NewGuid():N}.jsonl");

        try
        {
            JournalReport.WriteTo(path, played);

            var replayed = Replay.Run(JournalReport.ReadFrom(path));

            Assert.Equal(CsvReport.Rows(played).ToList(), CsvReport.Rows(replayed).ToList());
            Assert.Equal(Run.Games, File.ReadLines(path).Count(line => line.Contains("\"type\":\"header\"", StringComparison.Ordinal)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AReplaySeatsNoStrategyAtAll()
    {
        // BUILD-PLAN P14 acceptance 2, and the argument for the whole packet: a replay does
        // not re-run the strategy that produced the game, so editing that strategy — or
        // deleting it — cannot change what the journal plays back. The seats here are named
        // after something that has never existed, and the game is unchanged.
        var played = Simulator.Run(Run with { Journal = JournalFidelity.Thin });

        var renamed = played.Games
            .Select(game => game.Journal!)
            .Select(journal => new GameJournal(
                journal.Header with
                {
                    Seats = [.. journal.Header.Seats.Select(seat => seat with { Strategy = "no-such-strategy" })]
                },
                journal.Decisions))
            .ToList();

        var replayed = Replay.Run(renamed);

        Assert.Equal(Outcomes(CsvReport.Rows(played)), Outcomes(CsvReport.Rows(replayed)));
        Assert.Equal("no-such-strategy", Assert.Single(replayed.Strategies).Name);
    }

    [Fact]
    public void ARunAndItsReplayAgreeOnEverySeatsTallies()
    {
        // The rows above are the whole of it, but the summary is what a person reads — so it
        // is worth saying that the per-strategy table comes back the same too.
        var played = Simulator.Run(Run with { Journal = JournalFidelity.Thin });
        var replayed = Replay.Run([.. played.Games.Select(game => game.Journal!)]);

        Assert.Equal(played.Rounds, replayed.Rounds);
        Assert.Equal(played.Turns, replayed.Turns);
        Assert.Equal(played.Reshuffles, replayed.Reshuffles);
        Assert.Equal(played.Strategies, replayed.Strategies);
    }

    /// <summary>The CSV with the strategy name dropped — everything a replay must reproduce regardless of who is named.</summary>
    /// <summary>
    /// Rows with every strategy name blanked out — what happened, rather than who did it.
    /// </summary>
    /// <remarks>
    /// <b>Three columns carry a name, not one</b> (P16): a row also says who fed the seat and
    /// who the seat fed, and both are the same names read off the seating. Renaming the seats
    /// therefore changes three fields, and blanking only the first would make this test fail
    /// for the one reason it is not looking for.
    /// </remarks>
    private static List<string> Outcomes(IEnumerable<string> rows) =>
    [
        .. rows.Select(row =>
        {
            var fields = row.Split(',');
            fields[5] = string.Empty;
            fields[6] = string.Empty;
            fields[7] = string.Empty;
            return string.Join(',', fields);
        })
    ];
}

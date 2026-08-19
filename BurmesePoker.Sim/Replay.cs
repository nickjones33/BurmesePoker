using System.Diagnostics;

using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Sim;

/// <summary>
/// Plays journalled games back and reports on them exactly as a run reports on itself
/// (BUILD-PLAN P14).
/// </summary>
/// <remarks>
/// <para>
/// <b>The report is the same shape on purpose.</b> A replay that printed its own summary would
/// be comparable to the original run only by eye; producing a <see cref="SimulationReport"/>
/// makes the comparison mechanical — the same table, and the same CSV, so
/// <c>--journal</c> then <c>replay --csv</c> either matches the original file or it does not.
/// </para>
/// <para>
/// <b>There are no strategies in a replay</b>, only names. The seats answer from a file, so
/// <see cref="Strategy.Create"/> is never called; it throws rather than pretending, because a
/// replay that quietly seated a live bot would be measuring today's code again — which is the
/// whole thing journals exist to stop (§3.9 point 2).
/// </para>
/// </remarks>
public static class Replay
{
    /// <summary>Replays every journal and adds the results up.</summary>
    public static SimulationReport Run(IReadOnlyList<GameJournal> journals)
    {
        ArgumentNullException.ThrowIfNull(journals);

        if (journals.Count == 0)
        {
            throw new ArgumentException("There is no journal to replay.", nameof(journals));
        }

        var clock = Stopwatch.StartNew();
        var games = new GameResult[journals.Count];

        for (var game = 0; game < journals.Count; game++)
        {
            games[game] = GameRunner.Replay(journals[game], game);
        }

        clock.Stop();

        return Simulator.Summarise(OptionsOf(journals), games, clock.Elapsed);
    }

    /// <summary>
    /// The run the journals describe, reconstructed from their headers.
    /// </summary>
    /// <remarks>
    /// Only the parts a report reads are meaningful — the master seed, the table, and the
    /// strategy names in the order they were first seen, which is what the summary tabulates by.
    /// </remarks>
    private static SimulationOptions OptionsOf(IReadOnlyList<GameJournal> journals)
    {
        var names = new List<string>();

        foreach (var seat in journals.SelectMany(journal => journal.Header.Seats))
        {
            if (!names.Contains(seat.Strategy))
            {
                names.Add(seat.Strategy);
            }
        }

        var first = journals[0].Header;

        return new SimulationOptions
        {
            Strategies = [.. names.Select(name => new Strategy(name, NotSeated))],
            Seats = first.TableSize,
            Games = journals.Count,
            RoundsPerGame = Math.Max(1, journals.Max(journal => journal.Header.Rounds)),
            MasterSeed = first.MasterSeed ?? 0,
            Stakes = first.Stakes,
            Parallel = false
        }.Validated();
    }

    private static IPlayerAgent NotSeated(int seed) =>
        throw new InvalidOperationException(
            "A replayed seat answers from its journal; there is no strategy to create.");
}

/// <summary>
/// Journals as a file. The one place in the harness that turns them into bytes.
/// </summary>
/// <remarks>
/// <b>The format is <see cref="JournalFormat"/>'s and the file is this class's</b>, exactly as
/// <see cref="CsvReport"/> splits the two (BUILD-PLAN §2): the domain hands over lines and
/// knows nothing about disks.
/// </remarks>
public static class JournalReport
{
    /// <summary>Every journal a run kept, in game order.</summary>
    public static IEnumerable<string> Lines(SimulationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return JournalFormat.Lines(report.Games.Select(game => game.Journal).OfType<GameJournal>());
    }

    /// <summary>Writes them to a file.</summary>
    public static void WriteTo(string path, SimulationReport report) =>
        File.WriteAllLines(path, Lines(report));

    /// <summary>Reads a file of journals back.</summary>
    public static IReadOnlyList<GameJournal> ReadFrom(string path) =>
        JournalFormat.ReadAll(File.ReadLines(path));
}

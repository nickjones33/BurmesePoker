using System.Collections.Concurrent;

using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Web;

/// <summary>
/// The tables this site is hosting: what is open, who is waiting, and how to open another.
/// </summary>
/// <remarks>
/// <para>
/// <b>The lobby is not a domain concept</b> (BUILD-PLAN P13.6). It decides seating and names and
/// then constructs what the console's <c>Program</c> constructs — nothing below it knows a lobby
/// exists, and <c>TableSession</c> is byte-for-byte the one P13.2 wrote apart from being able to
/// say who is sitting down.
/// </para>
/// <para>
/// ⚠️ <b>It replaces a singleton with one table in it.</b> P13.3 opened one table from
/// configuration, which was the whole of what a page that watched needed; a second table is a
/// dictionary of them and a route that names one. <b>Nothing else in the client counts
/// tables</b> — <c>TableView</c> takes a board and a seat.
/// </para>
/// <para>
/// <b>One table is opened at boot</b>, from the same command line the console takes, so that
/// <c>dotnet run --project BurmesePoker.Web</c> is still a game rather than an empty room with a
/// form in it.
/// </para>
/// </remarks>
public sealed class Lobby : IAsyncDisposable
{
    /// <summary>How many tables this site will hold at once.</summary>
    /// <remarks>
    /// A table is a parked thread and a paced bot loop (§3.6), and the page that opens one is a
    /// form anybody can press. A ceiling is cheaper than discovering the absence of one.
    /// </remarks>
    public const int MostTables = 12;

    private readonly ConcurrentDictionary<string, HostedTable> _tables = [];
    private readonly ILoggerFactory _logs;
    private readonly Random _seeds;
    private int _opened;

    public Lobby(IConfiguration configuration, ILoggerFactory logs)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _logs = logs ?? throw new ArgumentNullException(nameof(logs));

        // A table carries its seed, exactly as a console match does (P11, P14), and a seed given
        // on the command line names the *first* table — `--seed 20260819` deals it again.
        var seed = configuration.GetValue<int?>("seed");

        _seeds = seed is { } given ? new Random(given) : Random.Shared;

        Opening = new TablePlan
        {
            Title = configuration.GetValue<string>("table") ?? "The house table",
            Seats = configuration.GetValue<int?>("seats") ?? RoundEngine.MinimumPlayers,
            People = configuration.GetValue<int?>("people") ?? 1,
            Seed = seed ?? Random.Shared.Next(),
            Pace = TimeSpan.FromMilliseconds(configuration.GetValue<int?>("pace") ?? 1100),
            BetweenRounds = TimeSpan.FromSeconds(configuration.GetValue<int?>("between") ?? 12),
            // ⚠️ A person is given longer than the server's own default. What the clock is really
            // catching is a table nobody is left at (P13.2), and forty-five seconds is a short
            // time to look at fourteen cards and decide which one is worth the least.
            Patience = TimeSpan.FromSeconds(configuration.GetValue<int?>("patience") ?? 90),
            Hints = configuration.GetValue<bool?>("hints") ?? true,
            // ⚠️ `--journal run.jsonl` writes the house table down as it plays (P24.1), in the
            // format the console writes and `sim replay` reads. First table only — see the
            // plan's own remarks for why the form does not offer it.
            Journal = configuration.GetValue<string>("journal"),
            // ⚠️ Resolved through the dial rather than trusted (P18, P19): `--difficulty
            // rubbish` opens the house table on the default level rather than failing to boot,
            // and the name kept is the ladder's own spelling whatever case was typed.
            Difficulty = (DifficultyLadder.Find(configuration.GetValue<string>("difficulty"))
                ?? DifficultyLadder.Default).Name,
            // ⚠️ `--mixed true` is the command-line half of P19's per-seat difficulty: a spread
            // of levels across the computer's seats, strongest first. The lobby form offers the
            // same thing as a checkbox.
            //
            // ⚠️ It takes a value, exactly as `--hints` does, and a bare `--mixed` is *silently*
            // ignored — the command-line configuration provider records a switch only when it
            // carries one. Found by starting the site with the bare flag and reading the seat
            // names, which all said `expert`.
            Difficulties = configuration.GetValue<bool?>("mixed") is true
                ? [.. DifficultyLadder.Spread(RoundEngine.MaximumPlayers).Select(level => level.Name)]
                : null
        };

        You = configuration.GetValue<string>("name") ?? "You";
    }

    /// <summary>What a table opened from this site's command line looks like.</summary>
    public TablePlan Opening { get; }

    /// <summary>What the lobby suggests calling you, before you have said.</summary>
    public string You { get; }

    /// <summary>Every table open, oldest first.</summary>
    public IReadOnlyList<HostedTable> Tables =>
        [.. _tables.Values.OrderBy(table => table.Id, StringComparer.OrdinalIgnoreCase)];

    /// <summary>The table that URL names, or null if it has gone.</summary>
    public HostedTable? Find(string? id) =>
        id is not null && _tables.TryGetValue(id, out var table) ? table : null;

    /// <summary>Opens the table this site was started with. Idempotent, and does it once.</summary>
    public HostedTable OpenTheHouseTable() => _tables.Values.FirstOrDefault() ?? Open(Opening);

    /// <summary>
    /// Opens a table and gives it back. <b>Nothing is dealt until somebody is at it.</b>
    /// </summary>
    /// <exception cref="InvalidOperationException">The site is already holding <see cref="MostTables"/>.</exception>
    public HostedTable Open(TablePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (_tables.Count >= MostTables)
        {
            throw new InvalidOperationException(
                $"This site holds {MostTables} tables, and they are all open. Close one first.");
        }

        var id = Interlocked.Increment(ref _opened).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var table = new HostedTable(id, plan, _logs.CreateLogger<HostedTable>());

        _tables[id] = table;
        return table;
    }

    /// <summary>A seed for a table somebody opened from the page, drawn from the site's own.</summary>
    /// <remarks>
    /// <b>A seed is a pointer</b> (§3.9): the site is reproducible from one number, so a second
    /// table's seed comes out of the first one's sequence rather than out of the clock.
    /// </remarks>
    public int NextSeed()
    {
        lock (_seeds)
        {
            return _seeds.Next();
        }
    }

    /// <summary>Closes a table and lets go of it.</summary>
    public async ValueTask<bool> Close(string id)
    {
        if (!_tables.TryRemove(id, out var table))
        {
            return false;
        }

        await table.DisposeAsync().ConfigureAwait(false);
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var table in _tables.Values)
        {
            await table.DisposeAsync().ConfigureAwait(false);
        }

        _tables.Clear();
    }
}

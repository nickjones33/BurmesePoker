using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;
using BurmesePoker.Server;

namespace BurmesePoker.Web;

/// <summary>
/// One table the lobby is hosting: it opens it, deals while somebody is at it, and keeps the
/// public board every viewer of it draws.
/// </summary>
/// <remarks>
/// <para>
/// <b>What <c>Program</c> is to the console</b> — except that it is one of several, which is
/// the whole of what P13.6 changed. Nothing else in the client knows how many tables there
/// are: <c>TableView</c> takes a board and a seat.
/// </para>
/// <para>
/// 🔥 <b>Two kinds of connection, and the difference between them is the whole of the
/// concealment.</b> The watcher is told the public game and folded into <see cref="Board"/>,
/// which every viewer of this table draws; a seat is asked questions and folded into a
/// <see cref="SeatBoard"/>, which <b>belongs to the viewer sitting in it and to nobody
/// else</b>. That last part is P13.6's correction: until this packet there was one
/// <c>SeatBoard</c> built when the table opened and handed to every circuit, which is right for
/// solo play and wrong the moment two people are here.
/// </para>
/// <para>
/// 🔥 <b>It deals while somebody is at the table, and that sentence is now answerable.</b>
/// P13.4 stopped dealing at boot because every question an empty seat is asked spends the whole
/// of its patience before the stand-in plays — an unattended round is over an hour of nothing —
/// and nothing at the time knew who was connected. A table does now: it deals while at least
/// one viewer is attending <em>and</em> every seat is either the computer's or somebody's, and
/// it stops when that stops being true. <b>The patience was not shortened</b>, which is what
/// P13.4 asked for.
/// </para>
/// <para>
/// ⚠️ <b>The bots are paced here</b>, because pacing belongs to whatever is drawing a table and
/// never to a server that may be hosting many of them (P13.2).
/// </para>
/// </remarks>
public sealed class HostedTable : IAsyncDisposable
{
    /// <summary>
    /// What the computer's seats are called — the same list, in the same order, as the console's.
    /// </summary>
    private static readonly string[] BotNames =
    [
        "Mya Lay", "Cobra", "Su Htwe", "Aung Aung", "Myat Htwe", "Khine Myat Zin"
    ];

    /// <summary>
    /// How hard each of the computer's seats is, in seating order, resolved once out of the
    /// one dial there is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>The plan carries names and this carries the levels</b> (BUILD-PLAN P18). Resolving
    /// here means an unknown name is dealt with when the table is opened rather than on the
    /// first turn of the first round.
    /// </para>
    /// <para>
    /// ⚠️ <b>One entry per computer seat, even when they are all the same</b> (P19). A table
    /// may mix levels, so "the table's difficulty" is a list; the single-value shorthand on
    /// the plan fills it, and <see cref="Difficulty"/> reads the first back for a lobby that
    /// wants one word.
    /// </para>
    /// </remarks>
    private readonly IReadOnlyList<DifficultyLevel> _levels;

    private readonly Lock _gate = new();
    private readonly CancellationTokenSource _stopping = new();
    private readonly TableSession _table;
    private readonly SeatConnection _watcher;
    private readonly Dictionary<PlayerId, SeatBoard> _sitting = [];
    private readonly ILogger _log;

    /// <summary>
    /// What this table asks the time of.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Injected rather than <c>DateTimeOffset.UtcNow</c></b> (BUILD-PLAN P54). The one
    /// thing worth asserting about idle reaping is <em>when</em> it happens, and a test that
    /// asserted it against the wall clock would have to sleep for the interval it is testing.
    /// </remarks>
    private readonly TimeProvider _clock;

    private int _seen;
    private int _attending;
    private DateTimeOffset? _idleSince;
    private bool _stopped;
    private bool _journalTrouble;
    private Task? _dealer;

    public HostedTable(string id, TablePlan plan, ILogger log, TimeProvider? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(plan);

        Id = id;
        Plan = plan;
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _clock = clock ?? TimeProvider.System;
        _levels = Levelled(plan);

        // 🔥 Idle from the moment it opens, and not from the moment somebody leaves (P54). A
        // table opened by a form press nobody followed up is exactly the leak the reaper is
        // for, and it has never had a viewer to lose.
        _idleSince = _clock.GetUtcNow();

        if (plan.People < 0 || plan.People > plan.Seats)
        {
            throw new ArgumentOutOfRangeException(
                nameof(plan),
                plan.People,
                $"A table of {plan.Seats} seats can seat between 0 and {plan.Seats} people.");
        }

        _table = TableSession.Open(
            [.. Enumerable.Range(1, plan.Seats).Select(Fill)],
            new TableOptions
            {
                Seed = plan.Seed,
                Patience = plan.Patience,
                Hints = plan.Hints,
                Journal = plan.Journal,
                // A stand-in is paced too, so a seat the computer took over reads as a seat
                // playing rather than as a stutter (P13.4, §3.11 C16). The factory is why the
                // server never had to know what a pause was.
                //
                // ⚠️ The *hardest* rung, and deliberately not this table's difficulty setting
                // (P18). Somebody whose connection dropped mid-round should not also find their
                // seat playing badly; the setting is about the opponents you chose to sit down
                // against. Left as a `new` here would be four lists again.
                StandIn = () => PacedAgent.Wrap(BotCatalog.Hardest.Create(plan.Seed), plan.Pace),
                // A name off a plan is resolved here and nowhere else (P18's rule, P36's
                // setting). Held is the rule and is what an unknown name gets.
                Seating = SeatingPolicy.Resolve(plan.Seating)
            });

        Board = TableBoard.Of(_table.Players, _table.Names);

        _watcher = _table.Watch("The room");
        _watcher.Updated += Fold;
    }

    /// <summary>What names this table in a URL.</summary>
    public string Id { get; }

    /// <summary>What it was opened with.</summary>
    public TablePlan Plan { get; }

    /// <summary>What it is called in the lobby.</summary>
    public string Title => Plan.Title;

    /// <summary>The one seed this whole table is reproducible from (§3.9).</summary>
    public int Seed => Plan.Seed;

    /// <summary>How many seats it has.</summary>
    public int Seats => Plan.Seats;

    /// <summary>Whether the computer's suggestions are shown to begin with.</summary>
    public bool Hints => Plan.Hints;

    /// <summary>How hard the computer's seats are here, in seating order.</summary>
    /// <remarks>Empty at a table with no computer seats at all.</remarks>
    public IReadOnlyList<DifficultyLevel> Difficulties => _levels;

    /// <summary>
    /// How hard this table is in one word — the level its first computer seat plays.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A mixed table has no one answer</b> (BUILD-PLAN P19), which is what
    /// <see cref="IsMixed"/> is for; a lobby that shows one word must say so.
    /// </remarks>
    public DifficultyLevel Difficulty => _levels.Count > 0 ? _levels[0] : DifficultyLadder.Default;

    /// <summary>Whether the computer's seats are not all at the same level.</summary>
    public bool IsMixed => _levels.Distinct().Count() > 1;

    /// <summary>How long this table's seating holds (RULES.md §3 step 2, P36).</summary>
    /// <remarks>
    /// The plan carries the <em>name</em> and this is the resolved policy — the difficulty's own
    /// split (P18), and the reason a name off a form cannot reach the engine unread.
    /// </remarks>
    public SeatingPolicy Seating => _table.Options.Seating;

    /// <summary>The seats still waiting for somebody, in seating order.</summary>
    public IReadOnlyList<PlayerId> WaitingFor => _table.WaitingFor;

    /// <summary>How many viewers — seated or watching — have this table open.</summary>
    public int Attending
    {
        get
        {
            lock (_gate)
            {
                return _attending;
            }
        }
    }

    /// <summary>
    /// When this table last had nobody at it, or null if somebody is here now.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>What makes a long-lived site behave</b> (BUILD-PLAN P54). Nothing in this client
    /// closed a table before this packet, so a site left up fills <c>Lobby.MostTables</c> and
    /// then quietly refuses to open another — a failure that arrives weeks after the deploy and
    /// looks like a broken form. ⚠️ <b>A table is idle from the moment it opens</b>: the leak is
    /// as much the table nobody ever arrived at as the one everybody left.
    /// </remarks>
    public DateTimeOffset? IdleSince
    {
        get
        {
            lock (_gate)
            {
                return _idleSince;
            }
        }
    }

    /// <summary>The public game, as everything that has been said adds up to it.</summary>
    public TableBoard Board { get; private set; }

    /// <summary>Whether the table is dealing round after round right now.</summary>
    public bool IsDealing
    {
        get
        {
            lock (_gate)
            {
                return _dealer is not null;
            }
        }
    }

    /// <summary>
    /// Whether the table has everybody it is waiting for and somebody to play in front of.
    /// </summary>
    public bool ShouldDeal
    {
        get
        {
            lock (_gate)
            {
                return Ready;
            }
        }
    }

    /// <summary>
    /// Raised whenever the board changes — what a component turns into
    /// <c>InvokeAsync(StateHasChanged)</c> (§3.11 C15).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Raised on the round's own thread</b>, which is not the renderer's.
    /// </remarks>
    public event Action? Changed;

    /// <summary>
    /// Somebody opened this table's page. <b>Balanced by <see cref="Leave"/>.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Called from an interactive circuit and never from a prerender</b> (§3.11 C13).
    /// Prerendering runs a component's initialisation on the request and again when the circuit
    /// starts, and a viewer counted twice is a table that goes on dealing to an empty room.
    /// </remarks>
    public void Arrive()
    {
        lock (_gate)
        {
            _attending++;
            _idleSince = null;
        }

        Consider();
    }

    /// <summary>Somebody closed it.</summary>
    public void Leave()
    {
        lock (_gate)
        {
            _attending = Math.Max(0, _attending - 1);

            // ⚠️ The clock starts on the *last* viewer leaving, and a second arrival clears it
            // again: a table two people are watching is not idle because one of them left.
            _idleSince = _attending == 0 ? _clock.GetUtcNow() : null;
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Sits somebody down: the first free seat, or the one that name was already in.
    /// </summary>
    /// <returns>Their own seat, or null if every seat is the computer's or already taken.</returns>
    /// <remarks>
    /// <para>
    /// ✅ <b>§3.11 C16 — coming back is the ordinary case, not an error path.</b> A browser
    /// refresh is a new circuit, and Blazor holds the old one for minutes before disposing it,
    /// so a player who reloaded would otherwise find their own seat occupied by their own ghost.
    /// Sitting down under a name that already holds a seat here <b>takes that seat back</b>, and
    /// the board it was being played through is stood up from — a <see cref="SeatBoard"/> that
    /// has been stood up from answers nothing, so the ghost cannot press anything.
    /// </para>
    /// <para>
    /// ⚠️ <b>A name is not a credential, and this table does not pretend otherwise.</b> Two
    /// people who type the same name take each other's seat. A lobby with accounts in it is
    /// beyond this plan; what is here is the reconnection behaviour, honestly bounded.
    /// </para>
    /// </remarks>
    public SeatBoard? SitDown(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        SeatBoard? ghost = null;

        lock (_gate)
        {
            foreach (var (seat, board) in _sitting)
            {
                if (string.Equals(board.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    ghost = board;
                    _sitting.Remove(seat);
                    break;
                }
            }
        }

        if (ghost is not null)
        {
            ghost.Dispose();
            _table.StandUp(ghost.Player);
        }

        foreach (var seat in _table.WaitingFor)
        {
            if (_table.SitDown(seat, name) is not { } connection)
            {
                continue;
            }

            var board = new SeatBoard(connection);

            lock (_gate)
            {
                _sitting[seat] = board;
            }

            Consider();
            return board;
        }

        return null;
    }

    /// <summary>Stands somebody up: the seat is free again and the computer plays it meanwhile.</summary>
    public void StandUp(SeatBoard seat)
    {
        ArgumentNullException.ThrowIfNull(seat);

        lock (_gate)
        {
            if (!_sitting.TryGetValue(seat.Player, out var sitting) || !ReferenceEquals(sitting, seat))
            {
                // Already stood up from — a viewer whose seat was taken back by a reconnection
                // must not free the seat the reconnection is now sitting in.
                return;
            }

            _sitting.Remove(seat.Player);
        }

        seat.Dispose();
        _table.StandUp(seat.Player);
    }

    /// <summary>
    /// Deals if there is anybody to deal to and every seat is filled; stops when there is not.
    /// </summary>
    private void Consider()
    {
        lock (_gate)
        {
            if (_dealer is null && Ready)
            {
                _dealer = Task.Run(Deal);
            }
        }
    }

    /// <summary>The dealing rule. Call under the lock.</summary>
    private bool Ready => _attending > 0 && _table.IsFull && !_stopped;

    private async Task Deal()
    {
        var token = _stopping.Token;

        while (true)
        {
            lock (_gate)
            {
                // ⚠️ The decision and the handover are one critical section: a viewer arriving
                // between the two would find a dealer still set and start nothing.
                if (!Ready)
                {
                    _dealer = null;
                    break;
                }
            }

            try
            {
                await _table.PlayRoundAsync(token).ConfigureAwait(false);
                WriteJournal();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (TableAbandonedException)
            {
                // Announced to the table before it was thrown (P13.2); the board says so and
                // the next round is dealt on top of it.
                WriteJournal();
            }
            catch (Exception problem)
            {
                // A table that fell over must not take the site with it: the page keeps
                // whatever it was last told, and says nothing new.
                _log.LogError(problem, "Table {Table} stopped dealing.", Id);

                lock (_gate)
                {
                    _dealer = null;
                }

                break;
            }

            try
            {
                await Task.Delay(Plan.BetweenRounds, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Writes the table down, if the plan asked for it (P24.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The format is the domain's and the file is this file's</b> (BUILD-PLAN §2, P14) —
    /// the console's split, at the console's other half's layer. Rewritten whole after every
    /// settled round, because nothing ends a hosted match but the table closing: what is on
    /// disk is every round that has settled, whatever happens to the process after it.
    /// </para>
    /// <para>
    /// ⚠️ <b>Called on the dealer's own thread, between rounds, and nowhere else.</b> The
    /// decisions are appended by the round that is playing; a flush from any other thread —
    /// disposal included — could read a round mid-sentence.
    /// </para>
    /// </remarks>
    private void WriteJournal()
    {
        if (Plan.Journal is not { } path || _table.Journal() is not { } journal)
        {
            return;
        }

        try
        {
            File.WriteAllLines(path, JournalFormat.Lines(journal));
            _journalTrouble = false;
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            // Said once rather than every round, and the table plays on: a full disk must not
            // stop a game, and a log line a round would bury the one that mattered.
            if (!_journalTrouble)
            {
                _log.LogError(problem, "Table {Table} could not write its journal to {Path}.", Id, path);
            }

            _journalTrouble = true;
        }
    }

    /// <summary>One seat: a person's if the plan asked for one, and the computer's otherwise.</summary>
    /// <remarks>
    /// ⚠️ <b>A person's seat is named when somebody sits in it</b> (P13.6) — a lobby opens a
    /// table before it knows who is coming, so all it can put here is a placeholder.
    /// </remarks>
    /// <summary>
    /// A level for each computer seat, out of the plan's list or its single-value shorthand.
    /// </summary>
    /// <remarks>
    /// <b>Resolved and never trusted</b>: a name that is not on the dial falls back to the
    /// plan's own setting, and that to the default. A table opens on a typo rather than
    /// failing to (P18).
    /// </remarks>
    private static IReadOnlyList<DifficultyLevel> Levelled(TablePlan plan)
    {
        var shorthand = Seatable(plan.Difficulty) ?? DifficultyLadder.Default;
        var computers = Math.Max(0, plan.Seats - plan.People);

        return plan.Difficulties is not { Count: > 0 } named
            ? [.. Enumerable.Repeat(shorthand, computers)]
            :
            [
                .. Enumerable.Range(0, computers)
                    .Select(index => Seatable(named[index % named.Count]) ?? shorthand)
            ];
    }

    /// <summary>An opponent a seat can be filled with: a level, or a rung at a mistake rate.</summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b><c>FindOrProbe</c> and not <c>Find</c>, and this is the whole of what P56's
    /// advanced control needed below the form.</b> A probe — <c>sprinter@0</c> — is a rung
    /// named as a level, and the machinery to resolve one has been public since P19 because
    /// <c>--strategies</c> resolves both lists. So an advanced choice travels as a name like
    /// every other choice and nothing new had to be built to seat it.
    /// </para>
    /// <para>
    /// ⚠️ <b>Where a <em>level</em> is meant it is still <c>Find</c></b> — the site's
    /// <c>--difficulty</c> shorthand in <c>Lobby</c> — or a typo there would quietly open the
    /// house table against a research rung. And a malformed probe is caught rather than thrown:
    /// a plan is what a table is opened with, and it comes off a form and a command line.
    /// </para>
    /// </remarks>
    private static DifficultyLevel? Seatable(string? name)
    {
        try
        {
            return DifficultyLadder.FindOrProbe(name);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <remarks>
    /// 🔥 <b>A computer seat is named for how it plays</b> (BUILD-PLAN P19: a seat should read
    /// as a character rather than as a setting). It used to read <c>Mya Lay (bot)</c> at every
    /// seat of every table, which says nothing once a table can mix levels — and a mix nobody
    /// can see is a mix nobody asked for twice.
    /// </remarks>
    private TableSeat Fill(int seat) => seat <= Plan.People
        ? TableSeat.Person(new PlayerId(seat), $"Seat {seat}")
        : TableSeat.Computer(
            new PlayerId(seat),
            // ⚠️ The rung's own name where a person reads it, and the level's where a replay
            // does — a seat saying `Mya Lay (sprinter@0)` shows a person the machinery, and a
            // journal saying `sprinter` loses the mistake rate a replay needs (P56).
            $"{BotNames[(seat - 1) % BotNames.Length]} ({OpponentMenu.Called(_levels[seat - Plan.People - 1])})",
            // ⚠️ A seed per seat, out of the table's own (§3.9): a rung that decides anything
            // at random — only `random` does today — must still be reproducible from the one
            // number the table carries, and two seats of it must not play the same game.
            PacedAgent.Wrap(_levels[seat - Plan.People - 1].Create(Plan.Seed + seat), Plan.Pace),
            // The journal's attribution — a level's name, exactly as the console writes one.
            _levels[seat - Plan.People - 1].Name);

    /// <summary>
    /// Folds everything the watcher has been told since last time into the board.
    /// </summary>
    /// <remarks>
    /// A cursor rather than a re-fold: <see cref="SeatConnection.Events"/> is the whole match
    /// and this runs once per event.
    /// </remarks>
    private void Fold(SeatConnection connection)
    {
        var events = connection.Events;

        lock (_gate)
        {
            var board = Board;

            for (var index = _seen; index < events.Count; index++)
            {
                board = board.After(events[index]);
            }

            _seen = events.Count;
            Board = board;
        }

        Changed?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        SeatBoard[] sitting;

        lock (_gate)
        {
            _stopped = true;
            sitting = [.. _sitting.Values];
            _sitting.Clear();
        }

        foreach (var seat in sitting)
        {
            seat.Dispose();
        }

        _watcher.Updated -= Fold;
        _table.Leave(_watcher);

        await _stopping.CancelAsync().ConfigureAwait(false);

        // The round itself is bounded by the table's own clock (P13.2) and not by this token,
        // so the dealer is let go of rather than waited for — the process is going away.
        _stopping.Dispose();
    }
}

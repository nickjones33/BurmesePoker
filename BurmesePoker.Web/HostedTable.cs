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

    private readonly Lock _gate = new();
    private readonly CancellationTokenSource _stopping = new();
    private readonly TableSession _table;
    private readonly SeatConnection _watcher;
    private readonly Dictionary<PlayerId, SeatBoard> _sitting = [];
    private readonly ILogger _log;
    private int _seen;
    private int _attending;
    private bool _stopped;
    private Task? _dealer;

    public HostedTable(string id, TablePlan plan, ILogger log)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(plan);

        Id = id;
        Plan = plan;
        _log = log ?? throw new ArgumentNullException(nameof(log));

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
                // A stand-in is paced too, so a seat the computer took over reads as a seat
                // playing rather than as a stutter (P13.4, §3.11 C16). The factory is why the
                // server never had to know what a pause was.
                StandIn = () => PacedAgent.Wrap(new GreedyBotAgent(), plan.Pace)
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
        }

        Consider();
    }

    /// <summary>Somebody closed it.</summary>
    public void Leave()
    {
        lock (_gate)
        {
            _attending = Math.Max(0, _attending - 1);
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
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (TableAbandonedException)
            {
                // Announced to the table before it was thrown (P13.2); the board says so and
                // the next round is dealt on top of it.
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

    /// <summary>One seat: a person's if the plan asked for one, and the computer's otherwise.</summary>
    /// <remarks>
    /// ⚠️ <b>A person's seat is named when somebody sits in it</b> (P13.6) — a lobby opens a
    /// table before it knows who is coming, so all it can put here is a placeholder.
    /// </remarks>
    private TableSeat Fill(int seat) => seat <= Plan.People
        ? TableSeat.Person(new PlayerId(seat), $"Seat {seat}")
        : TableSeat.Computer(
            new PlayerId(seat),
            $"{BotNames[(seat - 1) % BotNames.Length]} (bot)",
            PacedAgent.Wrap(new GreedyBotAgent(), Plan.Pace));

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

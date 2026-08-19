using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;
using BurmesePoker.Server;

namespace BurmesePoker.Web;

/// <summary>
/// The one table this site is showing: it opens it, deals round after round, and keeps the
/// public board every page draws.
/// </summary>
/// <remarks>
/// <para>
/// <b>What <c>Program</c> is to the console.</b> The lobby is P13.5's; until then the table is
/// opened here with one seat yours and the rest played by the computer, which is exactly what
/// the console does for a solo game (BUILD-PLAN P13.4). <c>--seat 0</c> gives back P13.3's
/// table, where every seat is a bot and there is nothing to click.
/// </para>
/// <para>
/// 🔥 <b>Two connections, and the difference between them is the whole of the concealment.</b>
/// The watcher is told the public game and folded into <see cref="Board"/>, which every page
/// draws; your seat is asked questions and folded into <see cref="Yours"/>, which only your
/// half of the page draws. Nothing goes round either of them to the session.
/// </para>
/// <para>
/// ⚠️ <b>Starting is idempotent, and that is not tidiness</b> (§3.11 C13). Prerendering runs a
/// component's <c>OnInitialized</c> once on the request and again when the circuit starts, and
/// every browser that opens the page runs it too. Joining a table twice is a real bug, so
/// <see cref="Start"/> may be called any number of times from any number of threads and deals
/// one match.
/// </para>
/// <para>
/// 🔥 <b>One watcher connection serves every page, and the board is folded from what it was
/// told.</b> A watcher is sent nothing but the public game (P13.2), so there is nothing
/// per-viewer to keep — and going through the fan-out rather than around it to
/// <see cref="TableSession"/>'s own banks is what keeps <c>ConcealmentTests</c> standing in
/// front of everything this page can possibly draw.
/// </para>
/// <para>
/// ⚠️ <b>The bots are paced here</b>, because pacing belongs to whatever is drawing a table and
/// never to a server that may be hosting many of them (P13.2). <see cref="PacedAgent"/> moved
/// out of <c>BurmesePoker.Console</c> into the presentation layer in this packet for exactly
/// this call.
/// </para>
/// </remarks>
public sealed class TableHost : IAsyncDisposable
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
    private readonly ILogger<TableHost> _log;
    private int _seen;
    private Task? _dealer;

    public TableHost(IConfiguration configuration, ILogger<TableHost> log)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _log = log ?? throw new ArgumentNullException(nameof(log));

        // A table carries its seed, exactly as a console match does (P11, P14). `dotnet run
        // --project BurmesePoker.Web -- --seed 20260819` deals the same table again.
        Seed = configuration.GetValue<int?>("seed") ?? Random.Shared.Next();
        Pace = TimeSpan.FromMilliseconds(configuration.GetValue<int?>("pace") ?? 1100);
        BetweenRounds = TimeSpan.FromSeconds(configuration.GetValue<int?>("between") ?? 12);
        Seats = configuration.GetValue<int?>("seats") ?? RoundEngine.MinimumPlayers;
        You = configuration.GetValue<string>("name") ?? "You";
        Hints = configuration.GetValue<bool?>("hints") ?? true;

        // Which seat is yours, one-based, and zero for nobody — a table with no person at it is
        // still worth watching, which is the whole of P13.3.
        SeatNumber = configuration.GetValue<int?>("seat") ?? 1;

        if (SeatNumber < 0 || SeatNumber > Seats)
        {
            throw new ArgumentOutOfRangeException(
                nameof(configuration),
                SeatNumber,
                $"--seat names a seat between 1 and {Seats}, or 0 to watch without playing.");
        }

        // ⚠️ A person is given longer than the server's own default. What the clock is really
        // catching is a table nobody is left at (P13.2), and forty-five seconds is a short time
        // to look at fourteen cards and decide which one is worth the least.
        var patience = TimeSpan.FromSeconds(configuration.GetValue<int?>("patience") ?? 90);

        _table = TableSession.Open(
            [.. Enumerable.Range(1, Seats).Select(Fill)],
            new TableOptions
            {
                Seed = Seed,
                Patience = patience,
                // A stand-in is paced too, so a seat the computer took over reads as a seat
                // playing rather than as a stutter (P13.4, §3.11 C16). The factory is why the
                // server never had to know what a pause was.
                StandIn = () => PacedAgent.Wrap(new GreedyBotAgent(), Pace)
            });

        Board = TableBoard.Of(_table.Players, _table.Names);

        _watcher = _table.Watch("The room");
        _watcher.Updated += Fold;

        // ⚠️ Your own connection, never the watcher's. The watcher is told the public game and
        // no hand at all; a seat is asked questions, and a question is where a hand lives.
        Yours = SeatNumber == 0 ? null : new SeatBoard(_table.ConnectionFor(new PlayerId(SeatNumber)));
    }

    /// <summary>One seat: yours if it is the one you asked for, and the computer's otherwise.</summary>
    private TableSeat Fill(int seat) => seat == SeatNumber
        ? TableSeat.Person(new PlayerId(seat), You)
        : TableSeat.Computer(
            new PlayerId(seat),
            $"{BotNames[(seat - 1) % BotNames.Length]} (bot)",
            PacedAgent.Wrap(new GreedyBotAgent(), Pace));

    /// <summary>The one seed this whole table is reproducible from.</summary>
    public int Seed { get; }

    /// <summary>How long a computer seat waits before moving, so a person can follow it (P11).</summary>
    public TimeSpan Pace { get; }

    /// <summary>How long the table sits still between a settlement and the next deal.</summary>
    public TimeSpan BetweenRounds { get; }

    /// <summary>How many seats are at this table (RULES.md §2.1).</summary>
    public int Seats { get; }

    /// <summary>Which seat is yours, one-based; zero when you are only watching.</summary>
    public int SeatNumber { get; }

    /// <summary>
    /// Your seat, or null when nobody at this table is you.
    /// </summary>
    /// <remarks>
    /// What <see cref="Presentation.TableRing"/> turns the felt by (BUILD-PLAN P13.5), and the
    /// reason it is a <see cref="PlayerId"/> rather than the number: a seat number is
    /// configuration, and a table is laid out from seats.
    /// </remarks>
    public PlayerId? Seat => SeatNumber == 0 ? null : new PlayerId(SeatNumber);

    /// <summary>What the table calls you.</summary>
    public string You { get; }

    /// <summary>Whether the computer's suggestions are shown to begin with (<c>--hints false</c>).</summary>
    public bool Hints { get; }

    /// <summary>
    /// Your seat, or null when nobody is playing this one. <b>Folded from your own connection</b>,
    /// which is the only place a hand ever comes from.
    /// </summary>
    public SeatBoard? Yours { get; }

    /// <summary>The public game, as everything that has been said adds up to it.</summary>
    public TableBoard Board { get; private set; }

    /// <summary>
    /// Raised whenever the board changes — what a component turns into
    /// <c>InvokeAsync(StateHasChanged)</c> (§3.11 C15).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Raised on the round's own thread</b>, which is not the renderer's. A handler that
    /// touches component state directly rather than going through <c>InvokeAsync</c> is racing
    /// the renderer.
    /// </remarks>
    public event Action? Changed;

    /// <summary>
    /// Deals rounds until the site stops. Safe to call from anywhere, any number of times.
    /// </summary>
    public void Start()
    {
        lock (_gate)
        {
            _dealer ??= Task.Run(Deal);
        }
    }

    /// <summary>Whether the table has been asked to start dealing.</summary>
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

    private async Task Deal()
    {
        var token = _stopping.Token;

        while (!token.IsCancellationRequested)
        {
            try
            {
                await _table.PlayRoundAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
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
                _log.LogError(problem, "The table stopped dealing.");
                return;
            }

            try
            {
                await Task.Delay(BetweenRounds, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

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
        Yours?.Dispose();
        _watcher.Updated -= Fold;
        _table.Leave(_watcher);

        await _stopping.CancelAsync().ConfigureAwait(false);

        // The round itself is bounded by the table's own clock (P13.2) and not by this token,
        // so the dealer is let go of rather than waited for — the process is going away.
        _stopping.Dispose();
    }
}

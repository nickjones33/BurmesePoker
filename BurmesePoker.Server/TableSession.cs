using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;

namespace BurmesePoker.Server;

/// <summary>
/// One table, hosted: the seats, the connections that play and watch them, and the rounds.
/// </summary>
/// <remarks>
/// <para>
/// <b>What <c>Program</c> does in the console, without a console</b> — and with the two things
/// a hotseat game never needed: a seat may be played from somewhere else, and what each viewer
/// is told is decided here rather than by whoever is drawing (BUILD-PLAN §3.10).
/// </para>
/// <para>
/// ✅ <b>There is no protocol.</b> Blazor Server supplies the transport, so a browser circuit
/// holds the very <see cref="SeatConnection"/> a test holds: no wire format, no client-side
/// state to synchronise, nothing serialised. That is the whole of what the framework choice
/// buys and it is why this packet has a mechanical acceptance criterion at all.
/// </para>
/// <para>
/// <b>Nothing ends a match</b> (RULES.md §7.2). <see cref="PlayRound"/> plays one whenever it
/// is asked to; <em>"another round?"</em> is the host's question, exactly as it is the console's.
/// </para>
/// <para>
/// ⚠️ <b>One round at a time.</b> A table is one task (§3.6), and two rounds at once on the same
/// <see cref="MatchEngine"/> would race its banks. The second caller is refused rather than
/// queued.
/// </para>
/// </remarks>
public sealed class TableSession
{
    private readonly Dictionary<PlayerId, SeatConnection> _seats = [];
    private readonly TableFanOut _fanOut = new();
    private readonly TableClock _clock;
    private readonly MatchEngine _match;
    private readonly Lock _gate = new();
    private bool _playing;

    private TableSession(IReadOnlyList<TableSeat> seats, TableOptions options)
    {
        Options = options;
        Names = seats.ToDictionary(seat => seat.Player, seat => seat.Name);
        _clock = new TableClock(options.RoundTimeLimit);

        // One adviser for the whole table: it holds no state between turns, exactly as the
        // agent it asks does not (P13.1).
        var advice = options.Hints ? new ComputerAdvice() : null;

        var agents = new Dictionary<PlayerId, IPlayerAgent>(seats.Count);

        foreach (var seat in seats)
        {
            IPlayerAgent agent;

            if (seat.Agent is { } bot)
            {
                agent = bot;
            }
            else
            {
                var connection = new SeatConnection(seat.Player, seat.Name);
                _seats[seat.Player] = connection;
                _fanOut.Add(connection);
                agent = new RemotePlayerAgent(
                    connection, options.StandIn(), _fanOut, options.Patience, advice);
            }

            // Every seat is bounded, bots included: what the clock catches is a table nobody
            // is left at, and a bot at such a table would play on for ever quite happily.
            agents[seat.Player] = new BoundedAgent(agent, _clock);
        }

        // One Random per table, from the seed and from nothing else (P14).
        _match = new MatchEngine(
            [.. seats.Select(seat => seat.Player)],
            agents,
            options.Stakes,
            new Random(options.Seed),
            _fanOut);
    }

    /// <summary>
    /// Opens a table. The seating order is the order given (RULES.md §3 step 2), and every seat
    /// without an agent gets a connection waiting for somebody to play it.
    /// </summary>
    public static TableSession Open(IReadOnlyList<TableSeat> seats, TableOptions options)
    {
        ArgumentNullException.ThrowIfNull(seats);
        ArgumentNullException.ThrowIfNull(options);

        if (seats.Select(seat => seat.Player).Distinct().Count() != seats.Count)
        {
            throw new ArgumentException("The same player is seated twice.", nameof(seats));
        }

        // Four to six is the engine's rule (RULES.md §2.1) and it checks it itself; opening a
        // table that cannot be dealt should fail here rather than at the first round.
        return new TableSession(seats, options);
    }

    /// <summary>How this table is run.</summary>
    public TableOptions Options { get; }

    /// <summary>The seating order, which is also the turn order every round.</summary>
    public IReadOnlyList<PlayerId> Players => _match.Players;

    /// <summary>What each seat is called.</summary>
    public IReadOnlyDictionary<PlayerId, string> Names { get; }

    /// <summary>Each seat's running total, carried over from round to round (RULES.md §7.2).</summary>
    public IReadOnlyDictionary<PlayerId, int> Banks => _match.Banks;

    /// <summary>How many rounds have been played and banked.</summary>
    public int RoundsPlayed => _match.RoundsPlayed;

    /// <summary>Every connection attached, seated or watching.</summary>
    public IReadOnlyList<SeatConnection> Connections => _fanOut.Connections;

    /// <summary>The connection playing a seat.</summary>
    /// <exception cref="ArgumentException">That seat is played by the computer, or is not at this table.</exception>
    public SeatConnection ConnectionFor(PlayerId player) =>
        _seats.TryGetValue(player, out var connection)
            ? connection
            : throw new ArgumentException($"{player} is not a seat anybody connects to.", nameof(player));

    /// <summary>
    /// Attaches a watcher: the public narration, no seat, and never a question.
    /// </summary>
    /// <remarks>
    /// <b>The strictest concealment case there is</b> — there is no hand a watcher may
    /// legitimately be shown, so anything private reaching one is unambiguously a leak
    /// (BUILD-PLAN P13.3).
    /// </remarks>
    public SeatConnection Watch(string name = "Watching")
    {
        var connection = new SeatConnection(player: null, name);
        _fanOut.Add(connection);
        return connection;
    }

    /// <summary>Detaches a connection — a dropped circuit stops being written to.</summary>
    public bool Leave(SeatConnection connection) => _fanOut.Remove(connection);

    /// <summary>
    /// Plays one round, blocking until it ends. Remote seats are asked through their
    /// connections; a seat that does not answer in time is played by the computer.
    /// </summary>
    /// <exception cref="TableAbandonedException">
    /// The round ran past <see cref="TableOptions.RoundTimeLimit"/>. Announced to the table
    /// before it is thrown, so a client can say what happened rather than simply stop.
    /// </exception>
    public RoundRecord PlayRound()
    {
        lock (_gate)
        {
            if (_playing)
            {
                throw new InvalidOperationException("This table is already playing a round.");
            }

            _playing = true;
        }

        try
        {
            _clock.StartRound();
            return _match.PlayRound();
        }
        catch (TableAbandonedException abandoned)
        {
            _fanOut.Broadcast(new TableEvent.TableAbandoned(abandoned.Round, abandoned.Limit));
            throw;
        }
        finally
        {
            lock (_gate)
            {
                _playing = false;
            }
        }
    }

    /// <summary>
    /// Plays one round on a task, so the caller can go on answering prompts.
    /// </summary>
    /// <remarks>
    /// <b>One table is one task</b> (§3.6). The round blocks inside the seats that are waiting
    /// for answers; this is where that blocking is put so that a UI thread is not the thing
    /// doing it.
    /// </remarks>
    public Task<RoundRecord> PlayRoundAsync(CancellationToken cancellationToken = default) =>
        Task.Run(PlayRound, cancellationToken);
}

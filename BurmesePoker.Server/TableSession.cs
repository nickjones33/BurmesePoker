using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
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
    private readonly Dictionary<PlayerId, SeatChannel> _channels = [];
    private readonly Dictionary<PlayerId, string> _names;
    private readonly Dictionary<PlayerId, string> _strategies;
    private readonly HashSet<PlayerId> _taken = [];
    private readonly TableFanOut _fanOut = new();
    private readonly TableClock _clock;
    private readonly MatchEngine _match;
    private readonly GameJournalBuilder? _journal;
    private readonly Lock _gate = new();
    private bool _playing;
    private bool _abandoned;

    private TableSession(IReadOnlyList<TableSeat> seats, TableOptions options)
    {
        Options = options;
        _names = seats.ToDictionary(seat => seat.Player, seat => seat.Name);
        _strategies = seats.ToDictionary(seat => seat.Player, seat => seat.Attribution);
        _journal = options.Journal is null ? null : new GameJournalBuilder();
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
                // The channel is the seat; the connection is its first occupancy (review R8).
                // The agent holds this first connection for ever and asks through it, which
                // reaches the channel whoever occupies the seat by then.
                var channel = new SeatChannel();
                var connection = new SeatConnection(seat.Player, seat.Name, channel);
                channel.Occupy(connection);
                _channels[seat.Player] = channel;
                _seats[seat.Player] = connection;
                _fanOut.Add(connection);
                agent = new RemotePlayerAgent(
                    connection, options.StandIn(), _fanOut, options.Patience, advice);
            }

            // Every seat is bounded, bots included: what the clock catches is a table nobody
            // is left at, and a bot at such a table would play on for ever quite happily.
            agents[seat.Player] = new BoundedAgent(agent, _clock, _fanOut);
        }

        // One Random per table, from the seed and from nothing else (P14).
        _match = new MatchEngine(
            [.. seats.Select(seat => seat.Player)],
            // The journal wraps outermost, exactly as the console's does (P14): what it records
            // is the answer that reached the engine — a stand-in's or the clock's included —
            // because that is the answer a replay has to give.
            _journal is null ? agents : JournalingAgent.Wrap(agents, _journal),
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

    /// <summary>
    /// Who is at this table, in the order they were first seated.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Not the turn order, from the second round on</b> (review R12): seats are
    /// re-randomised every deal (RULES.md §3 step 2), and each round narrates its own order in
    /// <see cref="TableEvent.RoundStarted"/> — which is where a client must read it. This list
    /// is the table's membership, stable so that names, banks and connections do not shuffle
    /// under their holders.
    /// </remarks>
    public IReadOnlyList<PlayerId> Players => _match.Players;

    /// <summary>
    /// What each seat is called, right now.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A snapshot, because a name changes when somebody sits down</b> (P13.6). A lobby
    /// opens a table before it knows who is coming, so a seat waiting for a player is called
    /// whatever the lobby called it until <see cref="SitDown"/> renames it.
    /// </remarks>
    public IReadOnlyDictionary<PlayerId, string> Names
    {
        get
        {
            lock (_gate)
            {
                return new Dictionary<PlayerId, string>(_names);
            }
        }
    }

    /// <summary>The seats waiting for somebody to connect and play them, in membership order.</summary>
    public IReadOnlyList<PlayerId> RemoteSeats => [.. Players.Where(_seats.ContainsKey)];

    /// <summary>The remote seats nobody is sitting in, in seating order.</summary>
    /// <remarks>
    /// 🔥 <b>What makes <em>"deal while somebody is at the table"</em> answerable at all</b>
    /// (BUILD-PLAN P13.6). Every question an empty seat is asked spends the whole of its
    /// patience before the stand-in plays, so a host that dealt into one would deal an hour of
    /// nothing — P13.4 found that and had no honest way to fix it, because nothing knew who was
    /// connected. Something does now.
    /// </remarks>
    public IReadOnlyList<PlayerId> WaitingFor
    {
        get
        {
            lock (_gate)
            {
                return [.. RemoteSeats.Where(seat => !_taken.Contains(seat))];
            }
        }
    }

    /// <summary>Whether every seat at this table is either the computer's or somebody's.</summary>
    public bool IsFull => WaitingFor.Count == 0;

    /// <summary>Whether somebody is sitting in that seat.</summary>
    public bool IsTaken(PlayerId player)
    {
        lock (_gate)
        {
            return _taken.Contains(player);
        }
    }

    /// <summary>Each seat's running total, carried over from round to round (RULES.md §7.2).</summary>
    /// <remarks>
    /// ⚠️ <b>A snapshot, exactly as <see cref="Names"/> is</b>: the engine's own dictionary is
    /// live and settlement mutates it, and this property is read from UI threads while a round
    /// is being played.
    /// </remarks>
    public IReadOnlyDictionary<PlayerId, int> Banks
    {
        get
        {
            lock (_gate)
            {
                return new Dictionary<PlayerId, int>(_match.Banks);
            }
        }
    }

    /// <summary>How many rounds have been played and banked.</summary>
    public int RoundsPlayed => _match.RoundsPlayed;

    /// <summary>Every connection attached, seated or watching.</summary>
    public IReadOnlyList<SeatConnection> Connections => _fanOut.Connections;

    /// <summary>The connection currently occupying a seat — a handover replaces it (review R8).</summary>
    /// <exception cref="ArgumentException">That seat is played by the computer, or is not at this table.</exception>
    public SeatConnection ConnectionFor(PlayerId player)
    {
        lock (_gate)
        {
            return _seats.TryGetValue(player, out var connection)
                ? connection
                : throw new ArgumentException($"{player} is not a seat anybody connects to.", nameof(player));
        }
    }

    /// <summary>
    /// Sits somebody down in a seat that was waiting for a player, and gives them the
    /// connection it is played through.
    /// </summary>
    /// <returns>
    /// The seat's connection, or null if that seat is the computer's, is not at this table, or
    /// somebody is already in it.
    /// </returns>
    /// <remarks>
    /// <para>
    /// 🔥 <b>Exclusive, and that is the whole of why it is here rather than in the lobby.</b>
    /// Two viewers handed the same <see cref="SeatConnection"/> are two people answering one
    /// question — the first press wins and the second is refused, at random, for ever. A seat
    /// is a property of a table, so a table is what refuses the second person.
    /// </para>
    /// <para>
    /// ⚠️ <b>Sitting down renames the seat and says so to everybody.</b> A lobby opens a table
    /// before it knows who is coming (P13.6), so <c>TableSeat.Person</c> can only ever carry a
    /// placeholder; the real name arrives here and reaches every board through the fan-out,
    /// like everything else.
    /// </para>
    /// </remarks>
    public SeatConnection? SitDown(PlayerId player, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        SeatConnection connection;
        SeatConnection superseded;

        lock (_gate)
        {
            if (!_seats.TryGetValue(player, out var seat) || !_taken.Add(player))
            {
                return null;
            }

            // Each occupancy is a fresh connection over the seat's one channel (review R8):
            // the superseded connection is dead server-side from this moment — no more
            // events, prompt gone, answers refused — so a previous occupant's reference
            // cannot see or play the new occupant's game. A question already standing moves
            // to the new connection rather than away (P13.6).
            _names[player] = name;
            superseded = seat;
            connection = new SeatConnection(player, name, _channels[player]);
            _channels[player].Occupy(connection);
            _seats[player] = connection;

            // Swapped under the gate so not even one private draw can land on the superseded
            // connection between the handover and the fan-out noticing. Neither call runs
            // handlers — the new connection has no subscribers yet.
            _fanOut.Remove(superseded);
            _fanOut.Add(connection);
        }

        // ⚠️ Outside the lock: a broadcast runs every connection's handlers, and a handler that
        // asked this table anything would be waiting on the thread that is telling it.
        _fanOut.Broadcast(new TableEvent.SeatTaken(player, name));
        return connection;
    }

    /// <summary>
    /// Stands somebody up out of a seat, so somebody else may take it and the computer plays it
    /// meanwhile.
    /// </summary>
    /// <remarks>
    /// <b>The seat keeps their name</b> (§3.11 C16): the honest thing for the table to say is
    /// that <em>Nick's</em> seat is being played by the computer, and a seat that reverted to
    /// <em>"Seat 2"</em> mid-round would read as a different player arriving.
    /// </remarks>
    public bool StandUp(PlayerId player)
    {
        string name;

        lock (_gate)
        {
            if (!_taken.Remove(player))
            {
                return false;
            }

            name = _names.TryGetValue(player, out var called) ? called : $"Seat {player.Value}";
        }

        _fanOut.Broadcast(new TableEvent.SeatLeft(player, name));
        return true;
    }

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
            lock (_gate)
            {
                _abandoned = true;
            }

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
    /// What this table has written down so far, or null if it was not asked to keep a journal
    /// (P24.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The same record the console writes and <c>sim replay</c> reads</b> (P14): the header
    /// is enough to set the same game up again — <see cref="TableOptions.Seed"/> is what the
    /// match's own generator was seeded with, and nothing else draws from it — and the
    /// decisions are every answer every seat gave, a person's exactly as a bot's. A seat is
    /// attributed to <see cref="TableSeat.Attribution"/>, and to the name it carries <em>now</em>,
    /// which is the name of whoever sat in it.
    /// </para>
    /// <para>
    /// ⚠️ <b>Ask between rounds, like everything else here.</b> The decisions are appended on
    /// the round's own thread (§3.6), so a journal built mid-round would be torn. The host that
    /// flushes after <see cref="PlayRound"/> returns is on the right side of that by
    /// construction.
    /// </para>
    /// </remarks>
    public GameJournal? Journal()
    {
        if (_journal is null)
        {
            return null;
        }

        lock (_gate)
        {
            return _journal.Build(new JournalHeader(
                Seed: Options.Seed,
                Seats: [.. _match.Players.Select(player => new JournalSeat(
                    player, _strategies[player], _names[player]))],
                Stakes: Options.Stakes,
                Rounds: _match.RoundsPlayed,
                Fidelity: _journal.Fidelity,
                Game: 0,
                Abandoned: _abandoned));
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

using BurmesePoker.Domain.Play;

namespace BurmesePoker.Server;

/// <summary>
/// One connection to the table: everything it is allowed to hear, and the question it is
/// currently being asked.
/// </summary>
/// <remarks>
/// <para>
/// <b>A mailbox, not a transport.</b> There is no socket here and no serialisation — Blazor
/// Server supplies the wire (BUILD-PLAN §3.10 item 4), so the "connection" a browser circuit
/// holds is this object, in process, and a test holds exactly the same one. That is what gives
/// P13.2 a mechanical acceptance criterion at all.
/// </para>
/// <para>
/// <b>Seated or not.</b> <see cref="Player"/> is null for a watcher, which gets the public
/// narration and is never asked anything — the strictest concealment case there is, because
/// there is no hand it may legitimately see (BUILD-PLAN P13.3).
/// </para>
/// <para>
/// 🔥 <b>A connection is one occupancy of a seat, not the seat</b> (review R8). The seat's
/// question-and-answer state lives in a <see cref="SeatChannel"/> that survives handovers;
/// each person who sits down gets a fresh connection over the same channel, and a superseded
/// connection is dead server-side — its <see cref="Pending"/> reads null, its
/// <see cref="Answer"/> is refused, and the fan-out no longer writes to it. Concealment on a
/// handover is therefore the server's property, not a courtesy the client extends by
/// disposing its board.
/// </para>
/// <para>
/// ⚠️ <b><see cref="Updated"/> is raised on the round's own thread</b>, before the seat's agent
/// begins waiting. A handler that answers immediately therefore answers before the wait starts,
/// which is well defined: the answer is latched and the wait returns at once. A handler that
/// blocks, blocks the table.
/// </para>
/// </remarks>
public sealed class SeatConnection
{
    private readonly Lock _gate = new();
    private readonly List<TableEvent> _events = [];
    private readonly SeatChannel? _channel;
    private readonly TableFanOut? _table;

    internal SeatConnection(
        PlayerId? player, string name, SeatChannel? channel = null, TableFanOut? table = null)
    {
        Player = player;
        Name = name;
        _channel = channel;
        _table = table;
    }

    /// <summary>The seat this connection plays, or null for a watcher.</summary>
    public PlayerId? Player { get; }

    /// <summary>
    /// What to call this connection. Presentation only; the engine never sees it.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>It is fixed for the connection's life</b>: a connection is one occupancy, and the
    /// person who sits down next gets a fresh one carrying their own name
    /// (<see cref="TableSession.SitDown"/>, P13.6).
    /// </remarks>
    public string Name { get; internal set; }

    /// <summary>Whether this connection holds a seat, as opposed to watching.</summary>
    public bool IsSeated => Player is not null;

    /// <summary>
    /// Raised whenever an event arrives or the pending question changes — the signal a Blazor
    /// component turns into <c>InvokeAsync(StateHasChanged)</c> (§3.11 C15).
    /// </summary>
    public event Action<SeatConnection>? Updated;

    /// <summary>Everything this connection has been told, oldest first. A snapshot.</summary>
    /// <remarks>
    /// ⚠️ A connection hears everything from the moment it is attached and nothing from before
    /// — so a seat re-taken mid-round starts from that moment, exactly as a watcher who joins
    /// late has missed the deal.
    /// </remarks>
    public IReadOnlyList<TableEvent> Events
    {
        get
        {
            lock (_gate)
            {
                return [.. _events];
            }
        }
    }

    /// <summary>
    /// The question this seat is being asked, or null when it is not its turn — and always
    /// null once another connection has taken the seat over.
    /// </summary>
    public SeatPrompt? Pending => _channel?.PendingFor(this);

    /// <summary>
    /// Answers the pending question.
    /// </summary>
    /// <returns>
    /// False if there is nothing pending, or the answer does not fit what was asked, or it
    /// names a card this seat is not holding, or this connection no longer occupies the seat.
    /// <b>A refusal leaves the prompt standing</b>, so a client may correct itself; nothing
    /// about the round changes.
    /// </returns>
    public bool Answer(SeatAnswer answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        return _channel is not null && _channel.Answer(this, answer);
    }

    /// <summary>
    /// What this seat has said about changing the seating, and has not yet been asked for
    /// (RULES.md §3 step 2, §9 #45). <see cref="SeatingOpinion.Consent"/> when it has said
    /// nothing, and always that once another connection has taken the seat over.
    /// </summary>
    public SeatingOpinion Seating =>
        _channel is not null && ReferenceEquals(_channel.Current, this)
            ? _channel.Seating
            : SeatingOpinion.Consent;

    /// <summary>
    /// Says something about changing the seating.
    /// </summary>
    /// <returns>
    /// False for a watcher, and for a connection that no longer occupies its seat (review R8) —
    /// the same rule <see cref="Answer"/> follows, for the same reason.
    /// </returns>
    /// <remarks>
    /// ⚠️ <b>It answers nothing and blocks nothing.</b> The seating question is public and is put
    /// to every seat between rounds; this is a seat saying what it will say when it is asked, and
    /// saying it again replaces it. <b>Saying nothing is consent</b>, which is why a table nobody
    /// is at never moves its own seats.
    /// <para>
    /// 🔥 <b>And saying it tells the whole table, from here.</b> Agreeing to change seats is
    /// something people do in front of each other, so this is the one thing a connection does
    /// that every other connection hears — which is what lets a page say it without holding a
    /// route to the table session (BUILD-PLAN P13.4 acceptance 3).
    /// </para>
    /// </remarks>
    public bool SaysAboutTheSeating(SeatingOpinion opinion)
    {
        if (_channel is null || !_channel.Say(this, opinion))
        {
            return false;
        }

        _table?.Broadcast(new TableEvent.SeatingOpinionGiven(Player!.Value, opinion));
        return true;
    }

    /// <summary>
    /// Puts a question and blocks until it is answered or the patience runs out.
    /// </summary>
    /// <returns>The answer, or null if nobody answered in time.</returns>
    /// <remarks>
    /// <para>
    /// <b>Blocking is the design, not a shortcut</b> (BUILD-PLAN §3.6): an agent is synchronous,
    /// a remote player waits inside one, and one table is one task. The cost is a parked thread
    /// per table, which P13's own load note dismisses beside the ~20 ms of search a round costs.
    /// </para>
    /// <para>
    /// The block is against the seat's <see cref="SeatChannel"/>, so the agent may go on
    /// holding the connection it was built with: a question outlives a handover, and the
    /// answer is taken from whoever occupies the seat when it arrives.
    /// </para>
    /// </remarks>
    internal SeatAnswer? Ask(SeatPrompt prompt, TimeSpan patience)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentOutOfRangeException.ThrowIfLessThan(patience, TimeSpan.Zero);

        return _channel is null
            ? throw new InvalidOperationException("A watcher holds no seat and is never asked anything.")
            : _channel.Ask(prompt, patience);
    }

    /// <summary>
    /// Says that the connection playing this seat has dropped, so that a question standing in
    /// front of it is given its patience again — measured from now (P64).
    /// </summary>
    /// <returns>
    /// False for a watcher, for a connection that no longer occupies its seat, when nothing is
    /// standing, and once the question has been held as long as it may be.
    /// </returns>
    /// <remarks>
    /// <para>
    /// 🔥 <b>The one thing a seat is told about its own transport</b>, and it is told rather
    /// than asked because nothing here can see a socket (BUILD-PLAN §3.10 item 4). What drops is
    /// a Blazor circuit; what the framework then does with it — hold it for
    /// <c>DisconnectedCircuitRetentionPeriod</c> and give the seat back, or give up and dispose
    /// the component, which stands the player up — is none of this object's business. All that
    /// is claimed is that <b>the clock on the standing question restarts when the player stops
    /// being able to answer it.</b>
    /// </para>
    /// <para>
    /// ⚠️ <b>There is deliberately no counterpart for the connection coming back.</b> A returning
    /// player finds the question still standing, which is all they needed; a clock that had to be
    /// resumed would need a signal that a frozen tab cannot send (P63 finding 4).
    /// </para>
    /// </remarks>
    public bool ConnectionLost() => _channel is not null && _channel.CircuitDropped(this);

    /// <summary>Reads this seat's standing opinion and clears it — the engine's half.</summary>
    internal SeatingOpinion TakeSeatingOpinion() =>
        _channel?.TakeSeatingOpinion() ?? SeatingOpinion.Consent;

    internal void Post(TableEvent moment)
    {
        lock (_gate)
        {
            _events.Add(moment);
        }

        Updated?.Invoke(this);
    }

    /// <summary>Raised by the channel when a question arrives or moves here on a handover.</summary>
    internal void NotifyUpdated() => Updated?.Invoke(this);
}

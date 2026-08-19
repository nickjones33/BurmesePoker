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
    private readonly ManualResetEventSlim _answered = new(initialState: false);
    private SeatPrompt? _pending;
    private SeatAnswer? _answer;

    internal SeatConnection(PlayerId? player, string name)
    {
        Player = player;
        Name = name;
    }

    /// <summary>The seat this connection plays, or null for a watcher.</summary>
    public PlayerId? Player { get; }

    /// <summary>
    /// What to call this connection. Presentation only; the engine never sees it.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>It changes when somebody sits down</b> (<see cref="TableSession.SitDown"/>, P13.6).
    /// A lobby opens a table before it knows who is coming, so a seat waiting for a player is
    /// called whatever the lobby called it until somebody takes it.
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

    /// <summary>The question this seat is being asked, or null when it is not its turn.</summary>
    public SeatPrompt? Pending
    {
        get
        {
            lock (_gate)
            {
                return _pending;
            }
        }
    }

    /// <summary>
    /// Answers the pending question.
    /// </summary>
    /// <returns>
    /// False if there is nothing pending, or the answer does not fit what was asked, or it
    /// names a card this seat is not holding. <b>A refusal leaves the prompt standing</b>, so a
    /// client may correct itself; nothing about the round changes.
    /// </returns>
    public bool Answer(SeatAnswer answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        lock (_gate)
        {
            if (_pending is not { } prompt || !answer.Fits(prompt))
            {
                return false;
            }

            _answer = answer;
        }

        _answered.Set();
        return true;
    }

    /// <summary>
    /// Puts a question and blocks until it is answered or the patience runs out.
    /// </summary>
    /// <returns>The answer, or null if nobody answered in time.</returns>
    /// <remarks>
    /// <b>Blocking is the design, not a shortcut</b> (BUILD-PLAN §3.6): an agent is synchronous,
    /// a remote player waits inside one, and one table is one task. The cost is a parked thread
    /// per table, which P13's own load note dismisses beside the ~20 ms of search a round costs.
    /// </remarks>
    internal SeatAnswer? Ask(SeatPrompt prompt, TimeSpan patience)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentOutOfRangeException.ThrowIfLessThan(patience, TimeSpan.Zero);

        lock (_gate)
        {
            _answered.Reset();
            _answer = null;
            _pending = prompt;
        }

        Updated?.Invoke(this);

        var answered = _answered.Wait(patience);

        lock (_gate)
        {
            var answer = answered ? _answer : null;
            _pending = null;
            _answer = null;
            return answer;
        }
    }

    internal void Post(TableEvent moment)
    {
        lock (_gate)
        {
            _events.Add(moment);
        }

        Updated?.Invoke(this);
    }
}

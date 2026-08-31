using System.Diagnostics;

using BurmesePoker.Domain.Play;

namespace BurmesePoker.Server;

/// <summary>
/// One seat's question-and-answer state, stable across occupants: the engine asks here, and
/// whichever <see cref="SeatConnection"/> currently occupies the seat answers here.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Why the seat and its occupant are two objects</b> (review R8). A
/// <see cref="SeatConnection"/> used to be both, handed out once and for ever — so after a
/// stand-up and a new sit-down, the previous holder's reference still received the new
/// occupant's private draws and could still answer their prompts. The fan-out's own standard —
/// <em>a viewer is not sent what it may not see</em> — was being held up only by the Web
/// client disposing its board, which is exactly the client-hides-it posture the fan-out's doc
/// rejects. Now the seat's engine-facing state lives here, each occupant gets a fresh
/// connection, and a superseded connection is dead server-side: no events, no prompt, no
/// answer accepted.
/// </para>
/// <para>
/// <b>A standing question survives a handover.</b> The engine blocks in <see cref="Ask"/>
/// against this channel, not against any occupant — so when a seat is re-taken mid-question,
/// the prompt simply becomes visible on the new connection and the answer is accepted from it
/// (P13.6's reconnection behaviour, server-side).
/// </para>
/// <para>
/// ⚠️ The wait's own verdict is not consulted on the way out of <see cref="Ask"/> (review
/// R19): in the window between the wait timing out and the gate being retaken,
/// <see cref="Answer"/> can latch a move and return true — a press the UI was already told
/// landed. What is latched under the gate is the truth either way.
/// </para>
/// </remarks>
internal sealed class SeatChannel
{
    private readonly Lock _gate = new();
    private readonly ManualResetEventSlim _answered = new(initialState: false);
    private SeatPrompt? _pending;
    private SeatAnswer? _answer;
    private SeatConnection? _current;
    private SeatingOpinion _seating = SeatingOpinion.Consent;
    private long _deadline;
    private long _ceiling;
    private TimeSpan _patience;

    /// <summary>The connection currently occupying the seat.</summary>
    internal SeatConnection Current
    {
        get
        {
            lock (_gate)
            {
                return _current ?? throw new InvalidOperationException("The seat has no occupant yet.");
            }
        }
    }

    /// <summary>
    /// Makes <paramref name="handle"/> the seat's occupant. Every earlier handle is dead from
    /// this moment: its <see cref="SeatConnection.Pending"/> reads null and its answers are
    /// refused.
    /// </summary>
    internal void Occupy(SeatConnection handle)
    {
        bool standing;

        lock (_gate)
        {
            _current = handle;
            standing = _pending is not null;
        }

        // A question asked of the seat is a question for whoever sits in it now — the prompt
        // moves to the new board, not away (P13.6).
        if (standing)
        {
            handle.NotifyUpdated();
        }
    }

    /// <summary>The pending question, as seen by one handle — null unless it is the occupant.</summary>
    internal SeatPrompt? PendingFor(SeatConnection handle)
    {
        lock (_gate)
        {
            return ReferenceEquals(handle, _current) ? _pending : null;
        }
    }

    /// <summary>Answers the pending question — accepted only from the seat's occupant.</summary>
    internal bool Answer(SeatConnection handle, SeatAnswer answer)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(handle, _current)
                || _pending is not { } prompt
                || !answer.Fits(prompt))
            {
                return false;
            }

            _answer = answer;
        }

        _answered.Set();
        return true;
    }

    /// <summary>
    /// What this seat has said about changing the seating, until it is asked (RULES.md §3 step 2).
    /// </summary>
    /// <remarks>
    /// 🔥 <b>A standing answer rather than a pending question, and that is the packet's shape.</b>
    /// The other five questions block one seat while the table waits; this one is put to
    /// <em>everybody</em>, so a table that blocked on it would wait five patiences to settle one
    /// question. What a seat says instead stands here until the engine asks between rounds.
    /// ⚠️ <b>It lives on the channel and not on a connection</b> for the same reason a pending
    /// prompt does (review R8): a seat's opinion is the seat's, and a person who takes the seat
    /// over inherits it exactly as they inherit a standing question.
    /// </remarks>
    internal SeatingOpinion Seating
    {
        get
        {
            lock (_gate)
            {
                return _seating;
            }
        }
    }

    /// <summary>
    /// Says something about changing the seating — accepted only from the seat's occupant.
    /// </summary>
    internal bool Say(SeatConnection handle, SeatingOpinion opinion)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(handle, _current))
            {
                return false;
            }

            _seating = opinion;
        }

        handle.NotifyUpdated();
        return true;
    }

    /// <summary>
    /// Reads this seat's opinion and puts it back to <see cref="SeatingOpinion.Consent"/>.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Asking consumes the answer, so a table is never asked to agree twice to one
    /// request.</b> Wanting the seats changed again means asking again — which is what stops a
    /// single press re-seating the table every round for the rest of the evening.
    /// </remarks>
    internal SeatingOpinion TakeSeatingOpinion()
    {
        SeatingOpinion opinion;
        SeatConnection? current;

        lock (_gate)
        {
            opinion = _seating;
            _seating = SeatingOpinion.Consent;
            current = opinion == SeatingOpinion.Consent ? null : _current;
        }

        current?.NotifyUpdated();
        return opinion;
    }

    /// <summary>
    /// Tells the seat that the connection playing it has dropped, so that the question standing
    /// in front of it is given its patience again — measured from the drop (P64).
    /// </summary>
    /// <returns>
    /// True if a standing question's clock was restarted; false if there was nothing standing,
    /// this handle is not the occupant, or the ceiling below has been reached.
    /// </returns>
    /// <remarks>
    /// <para>
    /// 🔥 <b>The two clocks did not start at the same event, and that is the defect P63
    /// measured.</b> <c>CircuitOptions.DisconnectedCircuitRetentionPeriod</c> starts when the
    /// circuit <em>drops</em>; the patience started when the question was <em>asked</em>. P54
    /// paired the two constants and read the difference as a margin — but that margin is the
    /// whole of it only for a player who vanishes the instant they are asked, and <b>zero for
    /// one who vanishes with a patience's worth already spent</b>. P63 watched the failure on a
    /// real phone: <c>ran out of time</c> was logged <em>before</em> <c>left the table</c>, the
    /// computer playing the turn of somebody the framework was still holding. ⚠️ <b>No pair of
    /// constants can express the condition</b>, which is why the fix is here and neither
    /// constant moved.
    /// </para>
    /// <para>
    /// ⚠️ <b>It restarts the clock rather than holding it.</b> A held clock would need a
    /// resumption, and nothing reliably tells this seat that a circuit came back — a browser
    /// tab that is frozen runs no timers (P63 finding 4), and a circuit the framework gives up
    /// on never says so to the seat at all. A deadline that only ever moves forward is correct
    /// under every one of those.
    /// </para>
    /// <para>
    /// ⚠️ <b>The ceiling is why a flapping connection cannot hold the table.</b> A question is
    /// never held past twice the patience it was asked with — a bound derived from the number
    /// already chosen rather than a second constant to keep in step with it.
    /// </para>
    /// </remarks>
    internal bool CircuitDropped(SeatConnection handle)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(handle, _current) || _pending is null)
            {
                return false;
            }

            var restarted = Stopwatch.GetTimestamp() + (long)(_patience.TotalSeconds * Stopwatch.Frequency);

            if (restarted >= _ceiling)
            {
                restarted = _ceiling;
            }

            if (restarted <= _deadline)
            {
                return false;
            }

            _deadline = restarted;
        }

        // The wait is sitting on a timeout it must now recompute. Setting the gate wakes it;
        // it finds no answer, reads the new deadline and waits again.
        _answered.Set();
        return true;
    }

    /// <summary>
    /// Puts a question to the seat and blocks until it is answered or the patience runs out.
    /// The engine-facing half: it does not care who occupies the seat, or whether the occupant
    /// changes while it waits.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The patience is a deadline rather than a duration</b>, because
    /// <see cref="CircuitDropped"/> may move it: the wait is retaken until the deadline it reads
    /// under the gate has passed or an answer is latched. <b>A spurious wake costs one extra
    /// pass round the loop and decides nothing</b> — what is latched under the gate is the
    /// truth either way (review R19).
    /// </remarks>
    internal SeatAnswer? Ask(SeatPrompt prompt, TimeSpan patience)
    {
        SeatConnection? current;

        lock (_gate)
        {
            _answered.Reset();
            _answer = null;
            _pending = prompt;
            _patience = patience;
            _deadline = Stopwatch.GetTimestamp() + (long)(patience.TotalSeconds * Stopwatch.Frequency);
            _ceiling = Stopwatch.GetTimestamp() + (long)(2 * patience.TotalSeconds * Stopwatch.Frequency);
            current = _current;
        }

        current?.NotifyUpdated();

        while (true)
        {
            TimeSpan left;

            // ⚠️ Reset *before* reading, never after: an answer latched between the read and a
            // reset would have its signal thrown away and the seat would wait out a patience it
            // had already answered.
            _answered.Reset();

            lock (_gate)
            {
                if (_answer is not null)
                {
                    break;
                }

                left = Stopwatch.GetElapsedTime(Stopwatch.GetTimestamp(), _deadline);
            }

            if (left <= TimeSpan.Zero)
            {
                break;
            }

            _answered.Wait(left);
        }

        lock (_gate)
        {
            var answer = _answer;
            _pending = null;
            _answer = null;
            return answer;
        }
    }
}

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
    /// Puts a question to the seat and blocks until it is answered or the patience runs out.
    /// The engine-facing half: it does not care who occupies the seat, or whether the occupant
    /// changes while it waits.
    /// </summary>
    internal SeatAnswer? Ask(SeatPrompt prompt, TimeSpan patience)
    {
        SeatConnection? current;

        lock (_gate)
        {
            _answered.Reset();
            _answer = null;
            _pending = prompt;
            current = _current;
        }

        current?.NotifyUpdated();

        _answered.Wait(patience);

        lock (_gate)
        {
            var answer = _answer;
            _pending = null;
            _answer = null;
            return answer;
        }
    }
}

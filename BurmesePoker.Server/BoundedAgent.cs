using System.Diagnostics;

using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Server;

/// <summary>
/// The clock a round is played against: started when the round starts, and consulted by every
/// seat before every answer.
/// </summary>
/// <remarks>
/// <b>Wall clock, not turns.</b> A simulated round is bounded by turn count because what can go
/// wrong there is the play (P12); a hosted round is bounded by time because what can go wrong
/// here is the people. A table of nothing but bots would hit a turn cap and never a clock, and
/// that asymmetry is the point.
/// </remarks>
internal sealed class TableClock(TimeSpan? limit)
{
    private long _started = Stopwatch.GetTimestamp();

    public TimeSpan Limit { get; } = limit ?? TimeSpan.MaxValue;

    public bool IsBounded => limit is not null;

    public void StartRound() => _started = Stopwatch.GetTimestamp();

    public bool HasRunOut => limit is { } bound && Stopwatch.GetElapsedTime(_started) >= bound;
}

/// <summary>
/// Every seat, wrapped so that the table can say whose turn it is and stop when its time is up.
/// </summary>
/// <remarks>
/// <para>
/// The shape <c>SeatRecorder</c> established in P12 and P13 reserved for this: a decorator over
/// <see cref="IPlayerAgent"/> that watches every question and may refuse to let it be answered
/// normally. Every seat is wrapped, bots included — a table nobody is left at is abandoned
/// whether or not anybody was ever going to answer.
/// </para>
/// <para>
/// The check is at the question rather than around the round because there is no resume point
/// mid-turn — the same reason <c>RoundEngine</c> handles deck exhaustion where it does.
/// </para>
/// <para>
/// 🔥 <b>And it is where the table learns whose turn it is</b> (BUILD-PLAN P13.5). Every seat
/// passes through here and nothing else in the server sees every seat's every question, so this
/// is the one place that can announce a turn beginning for a bot and a person alike. It is the
/// first question of a turn that raises <see cref="TableEvent.TurnBegan"/>, keyed on
/// <c>(Round, TurnNumber)</c> so that a turn asking four questions is still one turn.
/// </para>
/// <para>
/// ⚠️ <b>Outermost, which is why the spotlight lands before the pause.</b> A host wraps a bot in
/// P11's pacing decorator and then hands it here, so the order is announce, then wait, then
/// move — a seat lights up and is seen to think, which is the whole reason the pause exists.
/// </para>
/// </remarks>
internal sealed class BoundedAgent(IPlayerAgent inner, TableClock clock, TableFanOut table) : IPlayerAgent
{
    private (int Round, int Turn) _announced;

    public TurnAction ChooseAction(TurnContext context)
    {
        Bound(context);
        return inner.ChooseAction(context);
    }

    public Card ChooseDiscard(TurnContext context)
    {
        Bound(context);
        return inner.ChooseDiscard(context);
    }

    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        Bound(context);
        return inner.ClaimTurnedUpMoneyCard(context);
    }

    public bool Declare(TurnContext context)
    {
        Bound(context);
        return inner.Declare(context);
    }

    private void Bound(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (clock.HasRunOut)
        {
            throw new TableAbandonedException(context.Round, clock.Limit);
        }

        Announce(context);
    }

    /// <remarks>
    /// ⚠️ <b>After the clock, never before it.</b> A table that has run out of time is not
    /// turning to anybody, and announcing a turn that is about to throw would leave every
    /// client spotlighting a seat that never played.
    /// </remarks>
    private void Announce(TurnContext context)
    {
        if ((context.Round, context.TurnNumber) == _announced)
        {
            return;
        }

        _announced = (context.Round, context.TurnNumber);
        table.Broadcast(new TableEvent.TurnBegan(context.Player, context.Round, context.TurnNumber));
    }
}

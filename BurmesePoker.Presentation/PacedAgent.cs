using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Presentation;

/// <summary>
/// Another agent, answering at a speed a person can read.
/// </summary>
/// <remarks>
/// <para>
/// <b>A bot's turn is instantaneous, and that reads as nothing having happened.</b> Three or
/// five computer turns pass between a player's own two, and without a beat between them the
/// whole table appears to move at once (BUILD-PLAN P11).
/// </para>
/// <para>
/// <b>The pause belongs here and nowhere near <c>GreedyBotAgent</c>.</b> A domain type that
/// slept would put a wait inside the simulation harness's hot loop, where two thousand games
/// run in parallel and take about twenty milliseconds a round — it would ruin P12 outright.
/// Waiting is a thing a front end does; a decorator is how it does it without the domain
/// learning that anybody is watching (BUILD-PLAN §0).
/// </para>
/// <para>
/// ⚠️ <b>It lives here, and not in the console, because two front ends now need it</b>
/// (BUILD-PLAN P13.3). P13.2 left the table server's stand-in deliberately unpaced — a sleep
/// belongs to whatever is <em>drawing</em> a table and never to a server that may be hosting
/// many of them, so <c>TableOptions.StandIn</c> is a factory and a host hands over an agent
/// that is already wrapped. That host is <c>BurmesePoker.Web</c>, which may not reference
/// <c>BurmesePoker.Console</c>, so the decorator moved to the project both front ends already
/// share. <b>The alternative was <c>BurmesePoker.Domain/Agents/</c></b>, which the plan named
/// first; it is rejected because the domain is the pure rules and a wall-clock sleep is not
/// one of them — and because <c>BurmesePoker.Sim</c> references Domain and must never be able
/// to reach a sleep at all, which was the exact mistake P11 wrote a layering test about.
/// </para>
/// <para>
/// <b>Once a turn, not once a question.</b> A turn asks a varying number of questions — the
/// opener is offered the money card, later turns the discard, only a winning hand the
/// declaration — so pausing per call would make some turns three times as long as others for
/// no reason a watcher could see. The turn is identified by
/// <c>(<see cref="TurnContext.Round"/>, <see cref="TurnContext.TurnNumber"/>)</c>, the same
/// pair <see cref="SpectrePlayerAgent"/> hands the keyboard over on and for the same reason:
/// an agent lives for the whole match, so the turn number alone repeats every round.
/// </para>
/// <para>
/// The beat falls <em>before</em> the answer, which puts it between the last player's discard
/// landing on screen and this one's move — which is where a watcher wants it.
/// </para>
/// </remarks>
public sealed class PacedAgent : IPlayerAgent
{
    private readonly IPlayerAgent _inner;
    private readonly TimeSpan _pause;
    private (int Round, int Turn) _paused;

    public PacedAgent(IPlayerAgent inner, TimeSpan pause)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _pause = pause;
    }

    /// <summary>The agent itself if the pause is nothing, so no wrapper is paid for.</summary>
    public static IPlayerAgent Wrap(IPlayerAgent inner, TimeSpan pause) =>
        pause <= TimeSpan.Zero ? inner : new PacedAgent(inner, pause);

    public TurnAction ChooseAction(TurnContext context)
    {
        Beat(context);
        return _inner.ChooseAction(context);
    }

    public Card ChooseDiscard(TurnContext context)
    {
        Beat(context);
        return _inner.ChooseDiscard(context);
    }

    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        Beat(context);
        return _inner.ClaimTurnedUpMoneyCard(context);
    }

    public bool ObjectToClaim(TurnContext context)
    {
        Beat(context);
        return _inner.ObjectToClaim(context);
    }

    public bool Declare(TurnContext context)
    {
        Beat(context);
        return _inner.Declare(context);
    }

    private void Beat(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if ((context.Round, context.TurnNumber) == _paused)
        {
            return;
        }

        _paused = (context.Round, context.TurnNumber);
        Thread.Sleep(_pause);
    }
}

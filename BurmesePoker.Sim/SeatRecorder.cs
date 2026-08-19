using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Sim;

/// <summary>
/// Wraps one seat's strategy: bounds the round, and writes down what it was offered.
/// </summary>
/// <remarks>
/// The decorator BUILD-PLAN §3.8 item 2 reserves for decision-level statistics, and the same
/// shape as <c>RecordingAgent</c> in the test project. It is here rather than in the observer
/// because only the agent sees what it was <em>asked</em>: the observer is told the money card
/// was claimed, but never that the offer was made and declined.
/// </remarks>
public sealed class SeatRecorder(IPlayerAgent inner, int turnCap) : IPlayerAgent
{
    private readonly IPlayerAgent _inner = inner;

    /// <summary>How many times this seat was offered the turned-up money card, this round.</summary>
    public int ClaimOffers { get; private set; }

    /// <summary>How many of those it took (RULES.md §4.5).</summary>
    public int Claims { get; private set; }

    /// <summary>Starts a fresh round's tallies.</summary>
    public void BeginRound()
    {
        ClaimOffers = 0;
        Claims = 0;
    }

    public TurnAction ChooseAction(TurnContext context)
    {
        Bound(context);
        return _inner.ChooseAction(context);
    }

    public Card ChooseDiscard(TurnContext context)
    {
        Bound(context);
        return _inner.ChooseDiscard(context);
    }

    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        Bound(context);
        ClaimOffers++;
        var claimed = _inner.ClaimTurnedUpMoneyCard(context);

        if (claimed)
        {
            Claims++;
        }

        return claimed;
    }

    public bool Declare(TurnContext context) => _inner.Declare(context);

    private void Bound(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TurnNumber > turnCap)
        {
            throw new RoundAbandonedException(context.Round, context.TurnNumber, turnCap);
        }
    }
}

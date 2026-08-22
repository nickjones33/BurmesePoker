using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
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
public sealed class SeatRecorder(IPlayerAgent inner, int turnCap, bool countLockBites = false) : IPlayerAgent
{
    private readonly IPlayerAgent _inner = inner;

    /// <summary>How many times this seat was offered the turned-up money card, this round.</summary>
    public int ClaimOffers { get; private set; }

    /// <summary>How many of those it asked to take — asked, not got: a claim may be refused (RULES.md §4.5).</summary>
    public int Claims { get; private set; }

    /// <summary>Discards this seat was asked to choose, this round — the denominator of the two below.</summary>
    public int DiscardsChosen { get; private set; }

    /// <summary>
    /// Of those, how many the feeding ban had taken a held card out of (RULES.md §5.1).
    /// </summary>
    /// <remarks>
    /// <b>The lock was <em>live</em></b>: the seat below had a rank closed and this seat was
    /// holding one. It is not yet a lock that did anything — a rank closed off a card this seat
    /// was never going to throw costs it nothing at all.
    /// </remarks>
    public int RestrictedTurns { get; private set; }

    /// <summary>
    /// Of those, how many the ban actually <b>changed the answer</b> on.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>The mechanism variable</b> (BUILD-PLAN P31 item 3), and the number that separates
    /// <em>the rule did nothing</em> from <em>the rung did nothing</em>. The seat is asked what it
    /// would throw out of its legal set and what it would throw out of its whole hand; where the
    /// two heads differ, §5.1 took the card this seat meant to play.
    /// ⚠️ <b>It costs a second ranking</b>, which on an <c>outs</c>-family rung is the expensive
    /// thing a turn does — so it is off unless a cell asks for it, and only ever paid on a turn the
    /// ban has actually restricted.
    /// </remarks>
    public int LockBites { get; private set; }

    /// <summary>Starts a fresh round's tallies.</summary>
    public void BeginRound()
    {
        ClaimOffers = 0;
        Claims = 0;
        DiscardsChosen = 0;
        RestrictedTurns = 0;
        LockBites = 0;
    }

    public TurnAction ChooseAction(TurnContext context)
    {
        Bound(context);
        return _inner.ChooseAction(context);
    }

    public Card ChooseDiscard(TurnContext context)
    {
        Bound(context);
        DiscardsChosen++;

        var legal = context.LegalDiscards;

        // The ban is free on an ordinary turn: nothing closed means the hand itself, same list.
        if (legal.Count == context.Hand.Count)
        {
            return _inner.ChooseDiscard(context);
        }

        RestrictedTurns++;

        // ⚠️ The counterfactual is asked of the rung and never of the engine: only the player
        // knows what it meant to throw. A rung with no ordering — `random` — has no answer to
        // give, so its turns count as restricted and never as bitten.
        // ⚠️ It compares the two *rankings*, not the card actually thrown: a difficulty level
        // slips to its own runner-up (P19), and a slip is not the ban doing something.
        if (countLockBites
            && _inner is IRanksDiscards ranker
            && ranker.RankDiscards(context) is [var meant, ..]
            && ranker.RankDiscards(context, context.Hand) is [var unbanned, ..]
            && !meant.SameValueAs(unbanned))
        {
            LockBites++;
        }

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

    public bool ObjectToClaim(TurnContext context) => _inner.ObjectToClaim(context);

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

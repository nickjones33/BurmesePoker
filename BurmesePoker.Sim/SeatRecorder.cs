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
public sealed class SeatRecorder(
    IPlayerAgent inner,
    int turnCap,
    bool countLockBites = false,
    bool countRaceReach = false) : IPlayerAgent
{
    private readonly IPlayerAgent _inner = inner;

    /// <summary>
    /// The race-reach reader, held for the life of the seat so its cover-search answers survive
    /// between turns (BUILD-PLAN P46 follow-up). Only built when a cell asks for the count.
    /// </summary>
    private readonly Endgame.Reader? _endgame = countRaceReach ? new Endgame.Reader() : null;

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

    /// <summary>
    /// Of the discards chosen, how many left this seat <b>within one card of covering</b> — the
    /// endgame regime <c>sprinter</c> steers in (RULES.md §7.1, BUILD-PLAN P46).
    /// </summary>
    /// <remarks>
    /// 🔥 <b>P46's mechanism variable, and the number that tells a rung that never raced apart
    /// from one that raced and gained nothing</b> (P31's discipline). A hand rarely gets one draw
    /// from a win on its own discard turn before somebody declares, so a flat reach rate is the
    /// finding rather than a failure. Measured off the card the seat actually threw
    /// (<see cref="Endgame.AfterDiscardIsWithinOneCard"/>), so a rung that keeps the more winnable
    /// thirteen shows a higher reach than one that keeps the more improvable one — or does not,
    /// which is the whole question. ⚠️ <b>It costs a cover search on a hand near the line</b>, so
    /// it is off unless a cell asks for it, and paid only at the crossed table.
    /// </remarks>
    public int WithinReachDiscards { get; private set; }

    /// <summary>Starts a fresh round's tallies.</summary>
    public void BeginRound()
    {
        ClaimOffers = 0;
        Claims = 0;
        DiscardsChosen = 0;
        RestrictedTurns = 0;
        LockBites = 0;
        WithinReachDiscards = 0;
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
            var free = _inner.ChooseDiscard(context);
            CountReach(context, free);
            return free;
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

        var chosen = _inner.ChooseDiscard(context);
        CountReach(context, chosen);
        return chosen;
    }

    /// <summary>
    /// Whether the thirteen this seat is left holding is one card from a win, counted once the
    /// discard is chosen — the reach half of P46's mechanism, paid only when a cell asks for it.
    /// </summary>
    private void CountReach(TurnContext context, Card discarded)
    {
        if (_endgame is not null && _endgame.AfterDiscardIsWithinOneCard(context.Hand, discarded))
        {
            WithinReachDiscards++;
        }
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

    /// <remarks>
    /// <b>Forwarded, because a default interface method would answer in this wrapper's name</b>
    /// and silently drop what it wraps (RULES.md §3 step 2, P37).
    /// </remarks>
    public SeatingOpinion AskAboutTheSeating(SeatingQuestion question) =>
        _inner.AskAboutTheSeating(question);
}

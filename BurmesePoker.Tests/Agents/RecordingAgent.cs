using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// Wraps a strategy and writes down what it was asked and what it answered.
/// </summary>
/// <remarks>
/// This is the seam BUILD-PLAN §3.8 reserves for "why did it throw that away": a decorator
/// over <see cref="IPlayerAgent"/>, which sees the exact <see cref="TurnContext"/> a strategy
/// was given and the answer it gave, and needs <b>no domain change whatever</b>. The domain
/// cannot supply it — a <c>TurnContext</c> is built by the engine and nothing else can make
/// one — so a bot's decisions are testable only from inside a real round, which is also the
/// only place they are worth testing.
/// </remarks>
internal sealed class RecordingAgent(IPlayerAgent inner) : IPlayerAgent
{
    private readonly IPlayerAgent _inner = inner;

    /// <summary>Every card thrown away, in turn order.</summary>
    public List<Card> Discards { get; } = [];

    /// <summary>The hand held when each discard was chosen — fourteen cards.</summary>
    public List<Card[]> HandsWhenDiscarding { get; } = [];

    /// <summary>
    /// What the seat would have thrown, in its own order, each time it was asked — empty for a
    /// player that does not rank.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Taken during the turn and never afterwards</b> (BUILD-PLAN P19). A
    /// <c>TurnContext</c>'s hand is the engine's own list, so a context kept and asked later is
    /// asked about the <em>thirteen</em> — which is P13.1's finding ("a view model that aliases
    /// the engine's hand list is not a view model") arriving in the test project. Found by
    /// writing the test that compares a slip with the ranking it came off.
    /// </remarks>
    public List<Card[]> Rankings { get; } = [];

    /// <summary>
    /// Which cards the turn actually offered, each time a discard was asked for (RULES.md §5.1).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Taken during the turn, for the same reason <see cref="Rankings"/> is</b>: it is read
    /// off the live hand, so a context asked afterwards answers about the thirteen.
    /// </remarks>
    public List<Card[]> LegalWhenDiscarding { get; } = [];

    /// <summary>How many ranks were closed to this seat, each time a discard was asked for.</summary>
    public List<int> ClosedWhenDiscarding { get; } = [];

    /// <summary>How the card was taken, each time there was a choice about it.</summary>
    public List<TurnAction> Actions { get; } = [];

    /// <summary>Every answer to the claim's permission (RULES.md §4.5).</summary>
    public List<bool> Objections { get; } = [];

    /// <summary>Whether the turned-up money card was claimed, each time it was offered.</summary>
    public List<bool> Claims { get; } = [];

    public TurnAction ChooseAction(TurnContext context)
    {
        var action = _inner.ChooseAction(context);
        Actions.Add(action);
        return action;
    }

    public Card ChooseDiscard(TurnContext context)
    {
        HandsWhenDiscarding.Add([.. context.Hand]);
        LegalWhenDiscarding.Add([.. context.LegalDiscards]);
        ClosedWhenDiscarding.Add(context.ClosedToYou.Count);
        Rankings.Add(_inner is IRanksDiscards ranker ? [.. ranker.RankDiscards(context)] : []);
        var discard = _inner.ChooseDiscard(context);
        Discards.Add(discard);
        return discard;
    }

    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        var claimed = _inner.ClaimTurnedUpMoneyCard(context);
        Claims.Add(claimed);
        return claimed;
    }

    public bool ObjectToClaim(TurnContext context)
    {
        var objected = _inner.ObjectToClaim(context);
        Objections.Add(objected);
        return objected;
    }

    public bool Declare(TurnContext context) => _inner.Declare(context);
}

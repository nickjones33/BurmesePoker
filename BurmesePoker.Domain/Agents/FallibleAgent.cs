using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// A player that plays as well as the one it wraps, except that now and then it throws the
/// second-best card instead of the best.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>This is the whole difficulty mechanism</b> (BUILD-PLAN §3.12, P19). A skill ladder is
/// a research instrument: its rungs differ in exactly one decision, they are far apart — 0.0%,
/// 26.7% and 36.1% at four balanced seats — and a lower rung plays a <em>different and worse
/// idea</em> rather than the right idea badly. None of that is what a person wants from a
/// difficulty setting, which has to be monotone, fine-grained enough to ask for <em>a bit
/// easier</em>, and to read as a weaker player rather than as an alien one. <b>So difficulty is
/// the strongest available rung with a mistake rate, and the ladder is what the mistakes are
/// made against.</b>
/// </para>
/// <para>
/// ⚠️ <b>A mistake is a legal, plausible move and never a random one</b> (§3.12 item 3).
/// Throwing a joker away is not a mistake anybody makes; throwing the second-best card is. So
/// the substitute comes off <see cref="IRanksDiscards.RankDiscards"/> — the inner player's own
/// ordering — and this class refuses an inner player that cannot be asked for one, which is
/// what stops an ε from silently doing nothing.
/// </para>
/// <para>
/// ⚠️ <b>It never fumbles a declaration, and never claims or refuses a card it would not have.</b>
/// Refusing a won hand is not a worse player but a different game — <see cref="RandomBotAgent"/>
/// settles that and the reasoning carries. Taking or leaving the discard is a strict-improvement
/// question with no second-best answer, so there is nothing here to slip on: <b>the mistake has
/// exactly one site</b>, which is what keeps ε one dial rather than three.
/// </para>
/// <para>
/// ⚠️ <b>Money is untouched, as it must be.</b> The substitute is drawn from a list the inner
/// player built without ever being shown which cards are money (RULES.md §4.4), so a decorator
/// cannot leak it in — which is the likeliest place for that leak, and is a test.
/// </para>
/// <para>
/// ⚠️ <b>The generator is handed in, and this class never reaches for <see cref="Random.Shared"/></b>
/// (BUILD-PLAN §3.7 item 1). One careless decorator takes every other seat's reproducibility
/// down with it, and this one is seated at three or four seats of every table.
/// </para>
/// </remarks>
/// <param name="inner">Who is really playing. Must be able to rank its own discards.</param>
/// <param name="mistakeRate">
/// How often it slips, between 0 and 1. <b>ε = 0 is the undecorated player, byte for byte</b> —
/// nothing is drawn from the generator and nothing is substituted (P19 acceptance 3).
/// </param>
/// <param name="random">This seat's own generator.</param>
public sealed class FallibleAgent : IPlayerAgent, IRanksDiscards
{
    private readonly IPlayerAgent _inner;
    private readonly IRanksDiscards _ranks;
    private readonly double _mistakeRate;
    private readonly Random _random;

    /// <inheritdoc cref="FallibleAgent"/>
    public FallibleAgent(IPlayerAgent inner, double mistakeRate, Random random)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfNegative(mistakeRate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mistakeRate, 1);

        _inner = inner;
        _ranks = inner as IRanksDiscards
            ?? throw new ArgumentException(
                $"{inner.GetType().Name} cannot say which card it would throw instead, so a mistake rate on "
                + "top of it would do nothing. A difficulty level is a rung that can be asked for its own "
                + "second-best move (BUILD-PLAN P19).",
                nameof(inner));
        _mistakeRate = mistakeRate;
        _random = random;
    }

    /// <summary>How often it throws the second-best card rather than the best.</summary>
    public double MistakeRate => _mistakeRate;

    /// <summary>The inner player's, unchanged.</summary>
    public TurnAction ChooseAction(TurnContext context) => _inner.ChooseAction(context);

    /// <summary>
    /// The inner player's card, or — with probability <see cref="MistakeRate"/> — the one it
    /// ranked next.
    /// </summary>
    /// <remarks>
    /// <b>The generator is not touched at ε = 0</b>, so a level at the top of the dial deals
    /// the same match from the same seed as the bare rung does. A hand with only one candidate
    /// — every card of one value — has no second-best, and slips to the same card it would
    /// have thrown anyway.
    /// </remarks>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_mistakeRate <= 0)
        {
            return _inner.ChooseDiscard(context);
        }

        var ranked = _ranks.RankDiscards(context);

        return ranked.Count > 1 && _random.NextDouble() < _mistakeRate ? ranked[1] : ranked[0];
    }

    /// <summary>The inner player's, unchanged. Money is not a thing to be wrong about.</summary>
    public bool ClaimTurnedUpMoneyCard(TurnContext context) => _inner.ClaimTurnedUpMoneyCard(context);

    /// <summary>
    /// The inner player's, unchanged. <b>A mistake is a card, not a veto</b> — the dial is
    /// calibrated on which thirteen a seat keeps (P19), and the objection has no runner-up to
    /// slip to (RULES.md §4.5).
    /// </summary>
    public bool ObjectToClaim(TurnContext context) => _inner.ObjectToClaim(context);

    /// <summary>The inner player's, unchanged. A won hand is always laid down.</summary>
    public bool Declare(TurnContext context) => _inner.Declare(context);

    /// <summary>
    /// The inner player's ordering, unchanged — <b>what it meant to do</b>, not what it did.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>So a level may be wrapped again without the mistakes compounding into something
    /// nobody chose.</b> The head of this list is still the best move; the slip happens in
    /// <see cref="ChooseDiscard"/> and is not part of what this player thinks.
    /// </remarks>
    public IReadOnlyList<Card> RankDiscards(TurnContext context) => _ranks.RankDiscards(context);

    /// <summary>
    /// The inner player's ordering over the given choice, unchanged.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The slip is not modelled here either, and for the counterfactual that is the point</b>
    /// (BUILD-PLAN P31 item 3). The question is whether RULES.md §5.1 changed what this seat
    /// <em>meant</em> to throw; a level that would have slipped off the banned card anyway has
    /// still had its answer changed by the ban.
    /// </remarks>
    public IReadOnlyList<Card> RankDiscards(TurnContext context, IReadOnlyList<Card> candidates) =>
        _ranks.RankDiscards(context, candidates);

    /// <remarks>
    /// <b>Forwarded, because a default interface method would answer in this wrapper's name</b>
    /// and silently drop what it wraps (RULES.md §3 step 2, P37).
    /// </remarks>
    public SeatingOpinion AskAboutTheSeating(SeatingQuestion question) =>
        _inner.AskAboutTheSeating(question);
}

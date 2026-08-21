using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// <see cref="OutsBotAgent"/> that would rather dig in the deck than pick up what you threw:
/// it takes a card from anywhere but the deck only when the hand it buys is worth more than the
/// <b>ownership</b> the blind draw would have conferred (BUILD-PLAN P22).
/// </summary>
/// <remarks>
/// <para>
/// <b>One change, and it is the take.</b> The discard, the ranking and the declaration are
/// <see cref="OutsBotAgent"/>'s, card for card, so a difference in results attributes to one
/// decision (P15). ⚠️ <b>It is one change made at both places it arises</b> — taking the
/// previous player's discard and claiming the turned-up money card are the same purchase, made
/// with the same currency: RULES.md §4.4 confers ownership on a blind draw and §4.5 confers none
/// on a claim, so both spend a draw and neither buys a card that pays.
/// </para>
/// <para>
/// 🔥 <b>The question is the one nobody in the programme had asked.</b> Every rung before this
/// is money-blind on purpose, and rightly: ownership never transfers, so what you hold is worth
/// nothing and what you throw away costs nothing (RULES.md §4.4). But money-blindness of the
/// <em>discard</em> does not settle the <em>draw</em>. <see cref="GreedyBotAgent"/> concedes
/// exactly one tie-break to this — an even choice goes to the deck — and this rung is that
/// tie-break turned into a comparison. <b>It is therefore judged on <c>$/round</c> and not on
/// win rate</b>, and it is the first rung in the programme where those two can come apart.
/// </para>
/// <para>
/// ⚠️ <b>The exchange rate is the rung's one modelling assumption and it is written down</b>
/// (<see cref="WinShareOfACoveredCard"/>). Everything else in the comparison is a rule or a
/// public fact — the stakes (§4.3), the designation (§4.1), how many players pay (§4.3), how
/// much of the shoe this seat can see (<see cref="MoneyOdds"/>). Turning <em>melded cards</em>
/// into <em>money</em> is the one step no rule supplies, so it is a single named constant rather
/// than four coefficients spread across a method.
/// </para>
/// <para>
/// ⚠️ <b>What it does not do is deliberate.</b> It remembers nothing, so a money card it watched
/// another seat throw away is still live to it; it never hoards a money card, because holding one
/// is worth exactly nothing; and it never lets the money layer touch a discard, which is the rule
/// <c>MoneyCardsDoNotChangeWhatABotThrowsAway</c> holds every rung to.
/// </para>
/// </remarks>
public sealed class ProspectorBotAgent : IPlayerAgent, IRanksDiscards
{
    /// <summary>
    /// What one more melded card is worth, as a share of winning the round.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>A stated model, not a measurement.</b> Thirteen melded cards is the win (RULES.md
    /// §7.1) and there is no public estimate of what the twelfth is worth against the third, so
    /// the rung values them alike: one card is one thirteenth of a round. That is certainly
    /// wrong in detail — the last card is worth far more than the first — and it is wrong in a
    /// known direction, because it <em>overstates</em> the early cards and so takes the discard
    /// more often than a sharper model would. <b>The bias is towards playing the hand rather
    /// than towards playing the money</b>, which is the conservative direction for a rung whose
    /// whole claim is that the money is worth chasing.
    /// </para>
    /// <para>
    /// It is deliberately not a tunable knob. A rung with a free parameter is a family of rungs,
    /// and a family cannot be measured against the one below it in a single cell (P15).
    /// </para>
    /// </remarks>
    internal const double WinShareOfACoveredCard = 1.0 / 13;

    /// <inheritdoc cref="OutsBotAgent"/>
    private readonly OutsCache _cache = new();

    /// <summary>
    /// Take the offered discard only when the melded cards it buys outweigh the ownership a
    /// blind draw would have conferred.
    /// </summary>
    /// <remarks>
    /// The improvement has to be strict first, exactly as every rung above <c>simple</c> requires
    /// — a card that melds nothing is never worth a turn's draw whatever the stakes. What is new
    /// is the second half: the improvement is priced, and the price of the lottery ticket it
    /// spends is <see cref="MoneyOdds.PerBlindDraw"/>.
    /// </remarks>
    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        return WorthGivingUpADraw(context, discard) ? TurnAction.TakeDiscard : TurnAction.DrawFromDeck;
    }

    /// <summary>Greedy's discard, with the tie decided by what the hand would still be waiting for.</summary>
    /// <remarks>⚠️ <b>Money is not an input here and must never become one</b> (RULES.md §4.4).</remarks>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Discard(context, Preference, Outs);
    }

    /// <inheritdoc cref="OutsBotAgent.RankDiscards"/>
    public IReadOnlyList<Card> RankDiscards(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Ranking(context, Preference, Outs);
    }

    /// <summary>
    /// Claim the turned-up money card only when the hand it buys outweighs the draw it costs.
    /// </summary>
    /// <remarks>
    /// <b>The same rule as <see cref="ChooseAction"/>, at the other place it arises.</b> A
    /// claimed card comes from the table and so pays nobody (RULES.md §4.5) — it buys melding
    /// utility and nothing else — while the draw it is taken instead of would have conferred
    /// ownership. Which card happens to designate money is not an input: what is read is only
    /// how much money is still unaccounted for in the shoe.
    /// </remarks>
    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TurnedUpMoneyCards.Count > 0
            && WorthGivingUpADraw(context, context.TurnedUpMoneyCards[^1]);
    }

    /// <summary>Always. The engine only asks when the hand genuinely wins (RULES.md §7.1).</summary>
    public bool Declare(TurnContext context) => true;

    /// <summary>
    /// Whether a card taken from anywhere but the deck is worth the draw it is taken instead of.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The ownership value is asked for only when the card is worth taking at all</b>, so
    /// a rung that would have drawn anyway pays nothing for the money arithmetic — which is what
    /// keeps this rung's cost <see cref="OutsBotAgent"/>'s cost.
    /// </remarks>
    private static bool WorthGivingUpADraw(TurnContext context, Card offered)
    {
        var gain = CoverScore.Gain(context.Hand, offered);

        return gain > 0 && HandValueOf(gain, context) >= MoneyOdds.PerBlindDraw(context);
    }

    /// <summary>What <paramref name="gain"/> more melded cards is worth, in money.</summary>
    /// <remarks>
    /// A round is won from every other player at the table (RULES.md §4.3), so what is at stake
    /// for this seat is the round value times the opponents — and a melded card is
    /// <see cref="WinShareOfACoveredCard"/> of it.
    /// </remarks>
    internal static double HandValueOf(int gain, TurnContext context) =>
        gain * WinShareOfACoveredCard * context.Stakes.RoundValue * (context.Players.Count - 1);

    /// <summary>More outs is better, and the ranking takes the lowest key first.</summary>
    private long Outs(IReadOnlyList<Card> kept, int covered) => -LiveOuts.Count(kept, covered, _cache);

    /// <summary>Greedy's key, unchanged, deciding everything the count of outs leaves tied.</summary>
    private static readonly Func<Card, IReadOnlyList<Card>, long> Preference =
        static (card, hand) => CoverScore.Potential(card, hand);
}

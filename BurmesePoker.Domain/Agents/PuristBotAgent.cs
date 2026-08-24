using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// <see cref="OutsBotAgent"/> that plays for the clean bonus: it works its jokers out of its
/// hand whenever that costs it nothing melded, because a jokerless declaration pays ×2 or ×3
/// (RULES.md §7.3) and every other rung forfeits that bonus by construction (BUILD-PLAN P44).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The gap it exists to price, stated exactly.</b> <see cref="CoverScore.Potential"/>
/// returns <see cref="int.MaxValue"/> for a joker, so every rung before this one holds a joker
/// over everything — including at the declaring discard, where a hand that could win clean by
/// throwing the joker throws an ordinary card instead and wins dirty for a third of the money.
/// <c>docs/STRATEGY.md</c> §14 measured what that forfeits: about one settled round in eight
/// comes out jokerless <em>by accident</em>, the bonus is worth +$40 over flat at the default
/// table, and a rung that turned one round in eight into one in four would collect about half a
/// round's prize every round. This rung is that experiment.
/// </para>
/// <para>
/// <b>One change, and it is where the jokerless preference sits in the ranking.</b> The take,
/// the claim, the objection and the declaration are <see cref="OutsBotAgent"/>'s card for card;
/// the discard ranking keeps <c>outs</c>' first key — fewest melded cards lost — and puts
/// <em>fewest jokers kept</em> between it and the count of live outs. So a difference in
/// results attributes to one preference (P15), and the preference can only ever act where the
/// melded cards already tie.
/// </para>
/// <para>
/// ⚠️ <b>The exchange rate is stated rather than tuned, and it is lexicographic rather than a
/// number</b> (<see cref="ProspectorBotAgent.WinShareOfACoveredCard"/>'s precedent — one
/// modelling assumption, written down). The bonus is priced <b>below one melded card and above
/// every live out</b>: this rung never gives up a melded card to shed a joker — the bonus
/// multiplies a win, so paying for it in the currency wins are made of is self-defeat, which is
/// <c>warden</c>'s lesson (§8) — but it will pay any number of live outs, which is the option
/// value of the joker it sheds. A numeric rate between the two would need an estimate of the
/// probability of winning from a given hand, which nothing in this project supplies; a knob
/// would make it a family of rungs, which cannot be measured (P15).
/// </para>
/// <para>
/// ⚠️ <b>It is ranked on money, not win rate</b> (<see cref="RankedOn.Money"/>): by design it
/// wins the same or fewer rounds — every out it pays is win probability spent — and banks more
/// when it wins clean, so a field played for one stakes and ranked on declarations would
/// misjudge it by construction. It is measured by the money sweep against
/// <see cref="BotCatalog.Hardest"/>, and its mechanism variable is the share of its own wins
/// that come out jokerless, published beside §14's accidental floor.
/// </para>
/// <para>
/// ⚠️ <b>Its ceiling is capped by RULES.md §9 #33's recorded default.</b> Where the seat above
/// has taken a joker in the open, this seat's jokers are locked (§5.1, and a joker closes the
/// other jokers — §9 #27) and may leave only as the declaring discard, so a hand in that state
/// cannot be worked clean a turn early however this rung ranks it. The ranking is filtered to
/// <see cref="TurnContext.LegalDiscards"/> like every rung's, so the cap costs no code — but if
/// the expert session flips #33, this rung must be re-measured (BUILD-PLAN P44).
/// </para>
/// </remarks>
public sealed class PuristBotAgent : IPlayerAgent, IRanksDiscards
{
    /// <summary>
    /// What separates the joker count from the outs count inside the packed refinement key —
    /// far above any count <see cref="LiveOuts"/> can return, so the two can never bleed into
    /// each other.
    /// </summary>
    private const long AJokerOutranksEveryOut = 1L << 32;

    /// <inheritdoc cref="OutsBotAgent"/>
    private readonly OutsCache _cache = new();

    /// <summary>
    /// <c>outs</c>' take, exactly — a card is taken because it improves the hand.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Deliberately not "never take a joker".</b> Refusing an improving joker would be a
    /// second change (P15), and a taken joker is not a forfeited bonus: the ranking sheds it
    /// again the first time that is free.
    /// </remarks>
    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        return CoverScore.Improves(context.Hand, discard) ? TurnAction.TakeDiscard : TurnAction.DrawFromDeck;
    }

    /// <summary><c>outs</c>' discard, with the jokerless preference between its two keys.</summary>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Discard(context, Preference, CleanlinessThenOuts);
    }

    /// <inheritdoc cref="OutsBotAgent.RankDiscards"/>
    public IReadOnlyList<Card> RankDiscards(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Ranking(context, Preference, CleanlinessThenOuts);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Card> RankDiscards(TurnContext context, IReadOnlyList<Card> candidates)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);

        return CoverScore.Ranking(context.Hand, candidates, Preference, CleanlinessThenOuts);
    }

    /// <inheritdoc cref="GreedyBotAgent.ClaimTurnedUpMoneyCard"/>
    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TurnedUpMoneyCards.Count > 0
            && CoverScore.Improves(context.Hand, context.TurnedUpMoneyCards[^1]);
    }

    /// <inheritdoc cref="GreedyBotAgent.ObjectToClaim"/>
    public bool ObjectToClaim(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return true;
    }

    /// <summary>Always. The engine only asks when the hand genuinely wins (RULES.md §7.1).</summary>
    public bool Declare(TurnContext context) => true;

    /// <summary>
    /// The refinement, asked only of candidates already tied on melded cards kept: fewest
    /// jokers kept first, then <c>outs</c>' own count of what the pack could still bring.
    /// </summary>
    /// <remarks>
    /// <b>One <see cref="long"/>, two keys</b> — the joker count sits in bits the outs count
    /// can never reach, so the sort reads "cleanliness, then outs" and the outs half is
    /// <see cref="OutsBotAgent"/>'s exactly. Where every candidate keeps the same jokers — which
    /// is every turn the hand holds none, and every tie among ordinary discards — the term is a
    /// constant and this rung <em>is</em> <c>outs</c>.
    /// </remarks>
    private long CleanlinessThenOuts(IReadOnlyList<Card> kept, int covered)
    {
        var jokers = 0;

        foreach (var card in kept)
        {
            if (card.IsJoker)
            {
                jokers++;
            }
        }

        return jokers * AJokerOutranksEveryOut - LiveOuts.Count(kept, covered, _cache);
    }

    /// <summary>Greedy's key, unchanged, deciding everything the refinement leaves tied.</summary>
    private static readonly Func<Card, IReadOnlyList<Card>, long> Preference =
        static (card, hand) => CoverScore.Potential(card, hand);
}

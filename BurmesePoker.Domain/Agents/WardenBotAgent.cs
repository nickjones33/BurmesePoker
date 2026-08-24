using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// <see cref="OutsBotAgent"/> that plays RULES.md §5.1 <b>at</b> the seat above it rather than
/// merely obeying it: it will take a card it does not want in order to close that rank against
/// the player who threw it, and then declines to throw that rank back (BUILD-PLAN P31).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The gap it fills, stated exactly.</b> Until this rung, §5.1 reached every player the
/// computer offers as a <em>legality filter and nothing else</em>:
/// <see cref="TurnContext.LegalDiscards"/> is read to avoid an illegal move, and no rung read
/// <see cref="TurnContext.ClosedByYou"/> at all. So the strongest rule in this game about
/// <em>other people's hands</em> was, to every computer player, a rule about its own.
/// </para>
/// <para>
/// 🔥 <b>The play, and it uses only public information.</b> Taking a card in the open closes that
/// <b>rank</b> against the seat that discards to you, for the rest of the round (§5.1). The card
/// most often in front of you is the one your upstream neighbour has <em>just thrown away</em> — a
/// card they have told the table they do not want. Take it, and they may never throw that rank
/// again; two decks put eight copies of it in the shoe, so a second is likely, and a card that
/// cannot be discarded and cannot be melded is a hand that cannot go out.
/// </para>
/// <para>
/// ⚠️ <b>The release is the interesting half, and it is under the taker's control.</b> The ban
/// lifts when the <em>protected</em> player throws that rank back — and then permanently (§5.1,
/// exception 1). So a rung that takes for denial and then throws the rank on its own next turn has
/// paid for a lock and opened it for good. <b>That is the trap, and the two clauses are one idea
/// rather than two</b>: <em>lock a rank and keep it locked</em>. Splitting them into two rungs
/// would measure the take against a version of itself that undoes the take, which is not a
/// comparison (P15).
/// </para>
/// <para>
/// ⚠️ <b>Its restraint is §5.1 turned inward, and it keeps the rule's own two escapes.</b> It will
/// throw a rank it has locked where that is the only thing it can legally throw (the floor), and
/// where the throw is its declaring discard (exception 2). <b>Going out outranks the lock for the
/// same reason it outranks the ban</b> — the round ends on that discard, so the gift is never
/// delivered.
/// </para>
/// <para>
/// ⚠️ <b>What it does not change is deliberate.</b> The discard <em>ordering</em>, the refinement
/// and the declaration are <see cref="OutsBotAgent"/>'s, card for card, so a difference in results
/// attributes to the take and to the restraint that make it worth anything. <b>The claim is
/// <c>outs</c>' and stays that way</b>, even though claiming the turned-up card also arms §5.1
/// (§4.5): the opening claim is the one take a seat may be <em>refused</em>, every rung refuses
/// whenever it may (P28), so a denial-motivated claim would mostly be vetoed by the very seat it is
/// aimed at — and what that veto is worth already has a published cell of its own
/// (<c>docs/STRATEGY.md</c> §12). Folding it in here would make this two changes and confound the
/// two measurements. It is a rung somebody could write next, not this one.
/// </para>
/// </remarks>
public sealed class WardenBotAgent : IPlayerAgent, IRanksDiscards
{
    /// <summary>
    /// How much of a rank must still be unaccounted for before a lock is worth a turn's draw.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>A stated model, not a measurement</b> — the same standing as
    /// <see cref="ProspectorBotAgent.WinShareOfACoveredCard"/>, and written down for the same
    /// reason. Two decks put <b>eight</b> copies of a rank in the shoe, so a bar of five is
    /// <em>most of the rank is still out there</em>: the seat above has just told the table it is
    /// shedding this rank, and the arithmetic asks whether it is likely to be holding another to
    /// shed. Against thirteen unseen cards in that seat's hand, five loose copies is a little
    /// better than an even chance the lock has something to bite on.
    /// </para>
    /// <para>
    /// It is deliberately not a tunable knob. A rung with a free parameter is a family of rungs,
    /// and a family cannot be measured against the one below it in a single cell (P15).
    /// </para>
    /// </remarks>
    internal const int LooseCopiesWorthLocking = 5;

    /// <inheritdoc cref="OutsBotAgent"/>
    private readonly OutsCache _cache = new();

    /// <summary>
    /// What this seat has been shown, which is where the denial estimate comes from.
    /// </summary>
    /// <remarks>
    /// ✅ <b><c>counting</c>'s memory, reused rather than rewritten</b> (BUILD-PLAN P31 item 2).
    /// P20 measured that memory as worth nothing when it sharpened a tie-break that did not matter;
    /// this is the first decision in the programme where it feeds a rule that might.
    /// ⚠️ <b>It is not a second change to the ladder</b>: the memory is an input to the take and to
    /// nothing else, so the thirteen this rung keeps are still chosen exactly as
    /// <see cref="OutsBotAgent"/> chooses them.
    /// </remarks>
    private readonly ShoeMemory _memory = new();

    /// <summary>
    /// <c>outs</c>' take, plus the one this rung was written for: take a card you do not want when
    /// it locks a rank the seat above you is still likely to be holding.
    /// </summary>
    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _memory.Observe(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        return CoverScore.Improves(context.Hand, discard) || WorthLocking(context, discard)
            ? TurnAction.TakeDiscard
            : TurnAction.DrawFromDeck;
    }

    /// <summary><c>outs</c>' discard, chosen from the cards it is still willing to throw.</summary>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _memory.Observe(context);

        return CoverScore.Ranking(context.Hand, Candidates(context), Preference, Outs) is [var best, ..]
            ? best
            : throw new InvalidOperationException("Asked to discard from an empty hand.");
    }

    /// <inheritdoc cref="OutsBotAgent.RankDiscards"/>
    public IReadOnlyList<Card> RankDiscards(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _memory.Observe(context);

        return CoverScore.Ranking(context.Hand, Candidates(context), Preference, Outs);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// ⚠️ <b>The restraint is not the ban and stays in force here</b>, so the counterfactual this
    /// answers is <em>what §5.1 cost me</em> and not <em>what my own locks cost me</em>
    /// (BUILD-PLAN P31 item 3).
    /// </remarks>
    public IReadOnlyList<Card> RankDiscards(TurnContext context, IReadOnlyList<Card> candidates)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);
        _memory.Observe(context);

        return CoverScore.Ranking(context.Hand, Willing(context, candidates), Preference, Outs);
    }

    /// <inheritdoc cref="OutsBotAgent.ClaimTurnedUpMoneyCard"/>
    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _memory.Observe(context);

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
    public bool Declare(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _memory.Observe(context);

        return true;
    }

    /// <summary>
    /// Whether taking this card would buy a lock worth having.
    /// </summary>
    /// <remarks>
    /// Three questions, cheapest first, and every one of them is answered from public facts: the
    /// seat's own takes and throws (<see cref="TurnContext.ClosedByYou"/>), the cards it has been
    /// shown, and the card lying in front of it.
    /// </remarks>
    private bool WorthLocking(TurnContext context, Card offered)
    {
        var locked = context.ClosedByYou;

        // A rank already closed gains nothing from a second take, and a rank this seat has thrown
        // away can never close again however many more it picks up (RULES.md §5.1, exception 1).
        if (locked.Closes(offered) || locked.HasReleased(offered))
        {
            return false;
        }

        // ⚠️ Never a joker, and not because a joker cannot be locked. Whether taking one closes
        // the others is a PLAYER house ruling (RULES.md §9 #27) — the rule says "the same rank"
        // and a joker has none — so a rung whose whole claim rests on the lock must not be built
        // on the one part of §5.1 nobody has confirmed. A joker taken purely to deny is also a
        // joker held for nothing, which is the most expensive card in the game to waste.
        if (offered.Rank is not { } rank)
        {
            return false;
        }

        return _memory.LooseCopiesOf(rank) >= LooseCopiesWorthLocking && CanAbsorb(context, offered);
    }

    /// <summary>
    /// Whether the hand can carry a card it does not want without giving up a melded one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Taking a card that melds nothing means throwing a card that might. This seat can afford the
    /// lock when something it may legally throw is <b>deadwood in the fourteen</b> — no partner of
    /// any kind, so no arrangement of the hand has it in a meld and parting with it costs nothing.
    /// </para>
    /// <para>
    /// ⚠️ <b>Two exclusions, and both are the lock protecting itself.</b> The card thrown may not
    /// be of the rank just locked, or the take and the throw cancel on the same turn; and it may
    /// not be of a rank locked earlier, or paying for one lock opens another.
    /// </para>
    /// <para>
    /// ⚠️ <b>Cheap and slightly optimistic, deliberately.</b> <see cref="CoverScore.Potential"/> is
    /// integer arithmetic over the hand and asks no cover search, which is what keeps this rung at
    /// <see cref="OutsBotAgent"/>'s price. It counts no joker as a partner, so a hand holding two
    /// of them could in principle meld a card this calls deadwood — the rare direction, and the one
    /// that makes the rung take a lock it should have declined rather than decline one it should
    /// have taken.
    /// </para>
    /// </remarks>
    private static bool CanAbsorb(TurnContext context, Card offered)
    {
        var locked = context.ClosedByYou;

        var fourteen = new List<Card>(context.Hand.Count + 1);
        fourteen.AddRange(context.Hand);
        fourteen.Add(offered);

        foreach (var card in context.LegalDiscards)
        {
            if (!card.SameRankAs(offered) && !locked.Closes(card) && CoverScore.Potential(card, fourteen) == 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The legal discards this seat is actually willing to make — the hold half of the rung,
    /// shared with <see cref="OpportunistBotAgent"/> (see <see cref="HeldLocks"/>).
    /// </summary>
    private static IReadOnlyList<Card> Candidates(TurnContext context) =>
        HeldLocks.Candidates(context);

    /// <inheritdoc cref="HeldLocks.Willing"/>
    private static IReadOnlyList<Card> Willing(TurnContext context, IReadOnlyList<Card> candidates) =>
        HeldLocks.Willing(context, candidates);

    /// <summary>More outs is better, and the ranking takes the lowest key first.</summary>
    private long Outs(IReadOnlyList<Card> kept, int covered) => -LiveOuts.Count(kept, covered, _cache);

    /// <summary>Greedy's key, unchanged, deciding everything the count of outs leaves tied.</summary>
    private static readonly Func<Card, IReadOnlyList<Card>, long> Preference =
        static (card, hand) => CoverScore.Potential(card, hand);
}

using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Abstractions;

/// <summary>
/// Whatever decides a player's moves — a human at the console, a bot, or a script in a test.
/// </summary>
/// <remarks>
/// <b>The domain drives; the agent only answers</b> (BUILD-PLAN §3.5). The engine calls these
/// in turn order and validates every answer, so an agent can be wrong but cannot cheat: a
/// discard that is not in the hand is rejected, and a declaration is only ever offered when
/// <see cref="Melds.HandEvaluator"/> says the hand is winning.
/// </remarks>
public interface IPlayerAgent
{
    /// <summary>
    /// Take the previous player's discard, or draw blind? Asked only when there is a discard
    /// to take; on the opening turn there is none and the engine draws.
    /// </summary>
    TurnAction ChooseAction(TurnContext context);

    /// <summary>
    /// Which of the 14 held cards to throw away. Must be one of
    /// <see cref="TurnContext.Hand"/>.
    /// </summary>
    Card ChooseDiscard(TurnContext context);

    /// <summary>
    /// Take the top money card instead of drawing? Asked of the opening player only
    /// (RULES.md §4.5). The card is taken from the table, so it is held but never owned and
    /// pays nothing.
    /// </summary>
    bool ClaimTurnedUpMoneyCard(TurnContext context);

    /// <summary>
    /// The player who plays after you wants the turned-up money card, and needs your
    /// permission (RULES.md §4.5). Do you object?
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>The one question asked of a seat that is not on turn</b>, and the only decision in
    /// the game that is not about your own move. Their claim is a public take, so it closes that
    /// rank against you for the rest of the round (RULES.md §5.1) — which is why the rule gives
    /// you a veto and why <b>only a holder may use it</b>.
    /// </para>
    /// <para>
    /// ⚠️ <b>Asked only when the answer is free.</b> The engine puts this to the upstream seat
    /// only when that seat is holding the rank, so an agent is never asked a question it cannot
    /// answer either way; <see cref="TurnContext.PermissionAsked"/> carries what is being asked
    /// for, and <see cref="TurnContext.MayObject"/> is the same test the engine applied.
    /// </para>
    /// <para>
    /// ⚠️ <b>Answering yes is a disclosure.</b> It tells the whole table that this seat holds
    /// that rank — the first thing a player reveals by choice in a game that conceals every hand
    /// until the declaration.
    /// </para>
    /// </remarks>
    bool ObjectToClaim(TurnContext context);

    /// <summary>
    /// Lay down all 13 and end the round? Asked only after the discard, and only when the
    /// hand actually wins (RULES.md §7.1).
    /// </summary>
    bool Declare(TurnContext context);

    /// <summary>
    /// Shall we change seats? Asked of every seat between rounds (RULES.md §3 step 2, §9 #45).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>The sixth question, and the first that is not about cards.</b> The other five are put
    /// to one seat about its own move; this one is put to <em>everybody</em>, its answers are
    /// public, and the seats move only if somebody <see cref="SeatingOpinion.Ask"/>ed and nobody
    /// <see cref="SeatingOpinion.Refuse"/>d.
    /// </para>
    /// <para>
    /// 🔥 <b>The default answer is <see cref="SeatingOpinion.Consent"/>, it lives here rather than
    /// on any rung, and that is a design decision rather than a rule</b> (BUILD-PLAN §3.13).
    /// A rung decides about cards, and <em>"shall we move seats"</em> is not a card decision — so
    /// a rung answering it on some invented basis would be a strategy claim nobody measured
    /// (P15's discipline), and a rung that <em>abstained</em> would make §3's rule dead at every
    /// table with a computer at it, which at a solo table is every table. ⚠️ <b>No rung overrides
    /// this</b>, and <c>SeatingAgreementTests</c> fails the build if one does.
    /// </para>
    /// <para>
    /// ⚠️ <b>A decorator must forward it.</b> This is the first member of this interface with an
    /// implementation, so a wrapper that does not override it answers <em>consent</em> in its own
    /// name and silently drops what it wraps — which for a journalling wrapper would mean a
    /// re-seating that never reached the file. <c>SeatingAgreementTests.EveryDecoratorForwards
    /// TheSeatingQuestion</c> is the guard.
    /// </para>
    /// </remarks>
    SeatingOpinion AskAboutTheSeating(SeatingQuestion question) => SeatingOpinion.Consent;
}

using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Play;

/// <summary>
/// The opening player wants the turned-up money card, and is asking the seat that plays before
/// them for permission (RULES.md §4.5).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The permission and the feeding ban are one mechanism, not two rules that agree.</b>
/// Claiming is a public take, so it arms §5.1 against whoever discards to the claimer — and that
/// seat is exactly <em>the player who goes before you in turn order</em>. If they hold a copy they
/// may never throw it again this round, so the rule hands them a veto. That is why the objection
/// is not a preference: <b>only a holder may refuse</b>, and the holding test is the ban's own
/// test, <see cref="Card.SameRankAs"/> (RULES.md §9 #30).
/// </para>
/// <para>
/// ⚠️ <b>An objection is a disclosure, and the first one in the game a player makes by choice.</b>
/// Only a holder may object, so objecting tells the table that seat holds that rank — everything
/// else about a hand stays concealed until the declaration (RULES.md §7.1). The engine therefore
/// asks nobody who could not refuse: a question with one possible answer is not a question, and
/// asking it of a table of watchers would say more than the rule does.
/// </para>
/// </remarks>
/// <param name="Claimant">The opening player, who wants the card.</param>
/// <param name="Card">
/// The card turned up from the top of the deck (RULES.md §3 step 4) — public, and lying face up on
/// the table while this is asked.
/// </param>
public sealed record ClaimRequest(PlayerId Claimant, Card Card)
{
    /// <summary>
    /// Whether a hand may refuse this claim: it holds a card of that rank, and so would be locked
    /// into holding it (RULES.md §4.5, §5.1).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Rank alone</b> (RULES.md §9 #30). A player holding the 9♣ is locked by a claimed 9♥
    /// exactly as one holding the other 9♥ is, because §5.1 closes a rank and not a card. Reaching
    /// for <see cref="Card.SameValueAs"/> here would implement the narrow reading the expert's own
    /// justification contradicts.
    /// </remarks>
    public bool MayBeRefusedBy(IReadOnlyList<Card> hand)
    {
        ArgumentNullException.ThrowIfNull(hand);

        foreach (var held in hand)
        {
            if (held.SameRankAs(Card))
            {
                return true;
            }
        }

        return false;
    }
}

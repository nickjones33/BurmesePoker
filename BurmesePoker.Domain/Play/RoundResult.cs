using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;

namespace BurmesePoker.Domain.Play;

/// <summary>
/// How a round ended: who declared, the cover they laid down, and the money that moved.
/// </summary>
/// <param name="Round">Which round of the match this was, counting from 1.</param>
/// <param name="Winner">The player who went out.</param>
/// <param name="Melds">
/// The cover found for the winning hand. <b>A cover, not the tidiest one</b> — thirteen cards
/// of one suit in sequence come back as four melds rather than one, because
/// <see cref="HandEvaluator.TryFindCover"/> returns the first partition that satisfies the
/// table (RULES.md §7.1.1), not the tidiest one that does. Presenting
/// it the way a player would lay it out is the front end's problem (BUILD-PLAN P8).
/// </param>
/// <param name="Payouts">
/// Each player's <b>net</b> movement for the round, positive to collect. Everyone at the
/// table appears, and the values sum to zero. There is no breakdown of the round payment
/// against the money-card side-bet — see BUILD-PLAN P8 if you want one.
/// </param>
/// <param name="Turns">
/// How many turns the round ran, counting the winner's last one. Carried here because the
/// engine has the number for free in the loop it already keeps; every other statistic is
/// derived by the consumer from the observer stream or the table (BUILD-PLAN §3.8).
/// ⚠️ <b>It is <c>0</c> on a win from the initial deal</b> (RULES.md §7.4): the thirteen dealt
/// already won and nobody ever took a card, which is the first round shape this engine has had
/// that contains no turn at all.
/// </param>
/// <param name="Win">
/// What this win <b>was</b> — jokerless, from the initial deal, a third consecutive win
/// (RULES.md §7.3, §7.4, §7.5). 🔥 <b>Carried because a consumer cannot re-derive it any more.</b>
/// Two of the three are not properties of the cards: whether the round had turns, and what
/// happened in the two rounds before it. A front end splitting a net delta into <i>the round</i>
/// and <i>the side bet</i> needs all three, and asks <see cref="Settlement.RoundPayments"/> with
/// this.
/// </param>
/// <param name="JackpotOwner">
/// The player paid ×5 apiece for owning <b>both</b> partners of a 7♦/A♠ turn-up, or
/// <c>null</c> — the ordinary case, and every turn-up that is not that pair (RULES.md §4.1).
/// 🔥 <b>Carried because a watcher cannot compute it</b>: ownership is conferred by the deck
/// and stays partly private until settlement — a blind draw is the one event whose card the
/// table is not shown — so the only honest source is the same
/// <see cref="MoneyCardRegistry.ConfigurationOf"/> the settlement itself reads. Required
/// rather than defaulted for <see cref="Win"/>'s reason: a defaulted null would settle a
/// jackpot round as an ordinary one in silence.
/// </param>
public sealed record RoundResult(
    int Round,
    PlayerId Winner,
    IReadOnlyList<Meld> Melds,
    IReadOnlyDictionary<PlayerId, int> Payouts,
    int Turns,
    Win Win,
    PlayerId? JackpotOwner)
{
    /// <summary>
    /// Whether the winner declared without a joker anywhere in the thirteen, which is what
    /// RULES.md §7.3 pays ×2 or ×3 for.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Read off <see cref="Win"/> since P35, and it used to be read off <see cref="Melds"/>.</b>
    /// Both answer the same — the melds partition exactly the declared thirteen (§6.3), and
    /// jokerlessness is a property of the cards rather than of a particular cover — but the win
    /// is now carried whole, and one place is better than two.
    /// </remarks>
    public bool Jokerless => Win.Jokerless;
}

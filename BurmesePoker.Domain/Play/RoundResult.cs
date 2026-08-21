using BurmesePoker.Domain.Melds;

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
/// </param>
public sealed record RoundResult(
    int Round,
    PlayerId Winner,
    IReadOnlyList<Meld> Melds,
    IReadOnlyDictionary<PlayerId, int> Payouts,
    int Turns);

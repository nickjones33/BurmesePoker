namespace BurmesePoker.Domain.Play;

/// <summary>
/// What one seat thinks about changing the seating (RULES.md §3 step 2, §9 #45).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Three answers and not two, because consent is not desire.</b> §3 says the seats change
/// <em>when the players agree to it</em> — somebody wants it and nobody objects. A yes-or-no
/// question cannot say that: a computer seat must consent (see <see cref="Consent"/>), and a
/// table of consenting bots answering <em>yes</em> would re-seat itself every deal, which is the
/// opposite of the rule.
/// </para>
/// <para>
/// ⚠️ <b><see cref="Consent"/> is the default and it moves nothing on its own.</b> That is what
/// makes silence safe: a seat nobody is answering, a table nobody is at, and every bot in the
/// game all consent, and the seats still do not move until somebody <see cref="Ask"/>s.
/// </para>
/// </remarks>
public enum SeatingOpinion
{
    /// <summary>
    /// <b>I do not mind either way</b> — the default, and the only answer a computer seat gives.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>It is not a yes.</b> Consent alone never changes a seating; it only declines to stop
    /// one.
    /// </remarks>
    Consent,

    /// <summary>
    /// <b>I would like the seats changed.</b> One of these, with no <see cref="Refuse"/>, is the
    /// whole of the agreement (§9 #47's recorded default — unanimous among the people).
    /// </summary>
    Ask,

    /// <summary>
    /// <b>I would rather they were not.</b> One of these stops the change, whatever anybody else
    /// said.
    /// </summary>
    Refuse
}

/// <summary>
/// What a seat is told when it is asked about the seating.
/// </summary>
/// <remarks>
/// 🔥 <b>Everything here is already public</b>, and that is the point: this is the first question
/// in the game that is not about a hand, so it carries no hand, no money and no context that
/// another seat may not see. ⚠️ <b>A question that carried a hand would make a public question
/// private by accident</b>, which is the mistake the packet was written to avoid.
/// </remarks>
/// <param name="Round">The round about to be dealt, counting from 1.</param>
/// <param name="Player">Which seat is being asked.</param>
/// <param name="Seating">The seating as it stands, in turn order.</param>
public sealed record SeatingQuestion(int Round, PlayerId Player, IReadOnlyList<PlayerId> Seating);

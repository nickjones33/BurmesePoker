using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// A player that can say not only which card it would throw but which it would throw
/// <em>instead</em> — its own discards in its own order, best first.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>This is what makes a difficulty dial possible</b> (BUILD-PLAN §3.12, P19). Difficulty
/// is the strongest rung with a mistake rate, and §3.12 item 3 is the constraint that shapes
/// this interface: <b>a mistake must be a plausible move</b>. Substituting a random legal card
/// produces a bot that throws jokers away, which no person does and which reads as broken
/// rather than as weak — a weaker player plays the right idea and slips. So the mistake is the
/// next card down the agent's <em>own</em> ordering, and only the agent can say what that is.
/// </para>
/// <para>
/// ⚠️ <b>Not every way of playing has an ordering, and that is not a gap.</b>
/// <see cref="RandomBotAgent"/> does not rank, because it does not choose; a level built on it
/// would be a mistake rate on top of nothing. <see cref="FallibleAgent"/> refuses an inner
/// player that cannot be asked, which is what stops a level's ε from silently doing nothing.
/// </para>
/// <para>
/// It is separate from <see cref="Abstractions.IPlayerAgent"/> on purpose: the engine asks for
/// a move and must never be able to ask for a ranking, or a front end would start rendering
/// one seat's private reasoning (§3.5).
/// </para>
/// </remarks>
public interface IRanksDiscards
{
    /// <summary>
    /// Every card this player would consider throwing from <see cref="TurnContext.Hand"/>,
    /// <b>best first</b>, with the head being exactly what
    /// <see cref="Abstractions.IPlayerAgent.ChooseDiscard"/> answers.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Two cards of one value appear once.</b> They leave the same hand behind, so they
    /// are the same move; the list is shorter than the hand whenever it holds a pair.
    /// </remarks>
    IReadOnlyList<Card> RankDiscards(TurnContext context);
}

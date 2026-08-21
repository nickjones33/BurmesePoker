using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// The floor of the ladder: a seat that plays legally and thinks about nothing
/// (BUILD-PLAN P15).
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists to be beaten.</b> A comparison between two thoughtful strategies says which
/// is better but not by how much it beats not thinking at all, and the whole point of a
/// ladder is that "skill" becomes a dial with a bottom to measure from.
/// </para>
/// <para>
/// ⚠️ <b>The generator is handed in, and this class never reaches for
/// <see cref="System.Random.Shared"/>.</b> BUILD-PLAN §3.7 item 1 is what makes a run of two
/// thousand games reproducible from one master seed, and one careless strategy would break it
/// for every other seat at the table as well as for itself. The harness derives a seat's seed
/// from the game's, so seat 2 of game 417 draws the same numbers however the run was
/// scheduled.
/// </para>
/// <para>
/// ⚠️ <b>It is the first rung with no monotone score, so it can genuinely stall.</b> Every
/// other rung's cover count can only rise, which is what makes a table of them finish; this
/// one throws away melds as happily as it builds them, and a table of nothing but these may
/// never reach a declaration. That is what <c>SimulationOptions.TurnCap</c> is for, and an
/// abandoned round is <b>reported, not dropped</b>.
/// </para>
/// <para>
/// <b>It declares whenever it may</b>, which is the one decision left un-random. Going out is
/// not a strategy choice — refusing a won hand is not a worse player but a different game —
/// and a floor that threw its own wins away would measure that instead of measuring chance.
/// </para>
/// </remarks>
public sealed class RandomBotAgent(Random random) : IPlayerAgent
{
    private readonly Random _random = random ?? throw new ArgumentNullException(nameof(random));

    /// <summary>A coin toss between the discard and the deck.</summary>
    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _random.Next(2) == 0 ? TurnAction.TakeDiscard : TurnAction.DrawFromDeck;
    }

    /// <summary>
    /// Any card the turn actually offers, with equal chance — including the one just taken.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The legal discards, not the fourteen</b> (RULES.md §5.1). This is the one rung that
    /// does not go through <see cref="CoverScore.Ranking"/>, so it is the one rung that has to say
    /// so itself: a rank the next seat has taken in the open is not a move, and a floor that thinks
    /// about nothing still may not make one. The list is never empty (§5.1, The floor), so the
    /// guard below is about an empty <em>hand</em> and nothing else.
    /// </remarks>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var legal = context.LegalDiscards;

        return legal.Count > 0
            ? legal[_random.Next(legal.Count)]
            : throw new InvalidOperationException("Asked to discard from an empty hand.");
    }

    /// <summary>
    /// Another coin toss.
    /// </summary>
    /// <remarks>
    /// The count is a guard rather than an input: it is checked so the agent cannot answer yes
    /// to an offer of nothing, and <b>which</b> cards are turned up is never looked at — money
    /// is not a strategy input (RULES.md §4.4).
    /// </remarks>
    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TurnedUpMoneyCards.Count > 0 && _random.Next(2) == 0;
    }

    /// <summary>Always. The engine only asks when the hand genuinely wins (RULES.md §7.1).</summary>
    public bool Declare(TurnContext context) => true;
}

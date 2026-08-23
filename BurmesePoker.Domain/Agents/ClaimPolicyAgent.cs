using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// A rung playing its own game, with one answer taken out of its hands: whether to refuse the
/// opener the turned-up money card (RULES.md §4.5).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The one decision in the engine that nothing has measured.</b> Every rung in
/// <see cref="BotCatalog"/> objects whenever it may, which P28 took from §4.5's own reasoning —
/// letting the claim through closes that rank against this seat for the rest of the round
/// (§5.1) — and <b>nothing prices the disclosure</b> an objection makes. That is a decision and
/// not a derivation, so this decorator exists to put the two policies at one table and let the
/// harness say which is worth more (BUILD-PLAN P29).
/// </para>
/// <para>
/// ⚠️ <b>A research instrument and not a rung</b>, for the reason <see cref="FallibleAgent"/>
/// is not one: it is a knob on somebody else's play, and a knob is a family of players rather
/// than a player (P15). It is never in <see cref="BotCatalog"/> and never in a menu.
/// </para>
/// </remarks>
/// <param name="inner">The rung playing every other decision.</param>
/// <param name="objects">
/// What it answers when the engine asks. <b>Asked only of a seat that may actually refuse</b>,
/// so <c>true</c> is "refuse every time you are allowed to" and <c>false</c> is "never refuse".
/// </param>
public sealed class ClaimPolicyAgent(IPlayerAgent inner, bool objects) : IPlayerAgent
{
    private readonly IPlayerAgent _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <summary>What this seat answers when asked for permission.</summary>
    public bool Objects { get; } = objects;

    public TurnAction ChooseAction(TurnContext context) => _inner.ChooseAction(context);

    public Card ChooseDiscard(TurnContext context) => _inner.ChooseDiscard(context);

    public bool ClaimTurnedUpMoneyCard(TurnContext context) => _inner.ClaimTurnedUpMoneyCard(context);

    /// <summary>The policy, and the whole of what this decorator is.</summary>
    /// <remarks>
    /// ⚠️ <b>The inner rung is not asked at all.</b> Consulting it and then overriding would make
    /// the two arms differ in a random draw as well as in the policy where a rung decides
    /// anything at random, and the point of the cell is that they differ in one thing.
    /// </remarks>
    public bool ObjectToClaim(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Objects;
    }

    public bool Declare(TurnContext context) => _inner.Declare(context);

    /// <remarks>
    /// <b>Forwarded, because a default interface method would answer in this wrapper's name</b>
    /// and silently drop what it wraps (RULES.md §3 step 2, P37).
    /// </remarks>
    public SeatingOpinion AskAboutTheSeating(SeatingQuestion question) =>
        _inner.AskAboutTheSeating(question);
}

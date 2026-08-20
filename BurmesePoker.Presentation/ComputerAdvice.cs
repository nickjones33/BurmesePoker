using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Presentation;

/// <summary>
/// What the computer would do, asked of the computer.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Every answer here is the hardest rung's (<see cref="BotCatalog.Hardest"/>), put to it
/// on the very
/// <see cref="TurnContext"/> the player is looking at</b> (BUILD-PLAN P11, P13.1). It costs one
/// call and it cannot drift from how the bots at this table actually play. <b>Do not re-derive
/// a recommendation</b> — a second implementation of the advice is a different strategy wearing
/// the first one's name, and it would start disagreeing with the per-card costs in
/// <see cref="HandView"/> the first time either changed.
/// </para>
/// <para>
/// <b>Why a type at all, over a bare agent:</b> so that both front ends ask the same question
/// of the same seat, and so the one sentence above has somewhere to live. It holds no state
/// between turns, exactly as the agent does not, so one instance serves a whole match — or a
/// whole table of them.
/// </para>
/// <para>
/// ⚠️ <b>The hardest seat is the one that advises, deliberately, and it is not the table's
/// difficulty setting</b> (BUILD-PLAN P18). A hint that got worse as you lowered the difficulty
/// would be absurd — you would be asking the computer what to do and being told what a weak
/// player would do — so this asks <see cref="BotCatalog.Hardest"/> whatever the opponents are
/// set to. Said out loud because it otherwise reads as a place somebody forgot to thread the
/// setting through.
/// </para>
/// </remarks>
public sealed class ComputerAdvice
{
    /// <remarks>
    /// ⚠️ <b>Built from the catalog and typed as the interface</b>, so that the strongest rung
    /// is a fact stated in one place (P18). The seed is a formality: a rung that decided
    /// anything at random would be a strange thing to take advice from, and the hardest one
    /// ignores it — the zero is a seed and it is never drawn from.
    /// </remarks>
    private readonly IPlayerAgent _adviser = BotCatalog.Hardest.Create(0);

    /// <summary>Whether it would claim the top turned-up money card instead of drawing (RULES.md §4.5).</summary>
    public bool ClaimTurnedUpMoneyCard(TurnContext context) => _adviser.ClaimTurnedUpMoneyCard(context);

    /// <summary>Whether it would take the discard or draw blind.</summary>
    public TurnAction Take(TurnContext context) => _adviser.ChooseAction(context);

    /// <summary>Which card it would throw away.</summary>
    public Card Discard(TurnContext context) => _adviser.ChooseDiscard(context);
}

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
/// ⚠️ <b>Every answer here is the hardest <em>rung</em>'s (<see cref="BotCatalog.Hardest"/>) and never a difficulty level's, put to it
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
public sealed class ComputerAdvice : ISecondOpinion
{
    /// <remarks>
    /// ⚠️ <b>Built from the catalog and typed as the interface</b>, so that the strongest rung
    /// is a fact stated in one place (P18). The seed is a formality: a rung that decided
    /// anything at random would be a strange thing to take advice from, and the hardest one
    /// ignores it — the zero is a seed and it is never drawn from.
    /// </remarks>
    private readonly IPlayerAgent _adviser;

    /// <summary>
    /// The same instance again, as the thing that can say <em>why</em> (BUILD-PLAN P24.2).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>One rung, held twice, and not two.</b> <see cref="OutsBotAgent"/> carries an
    /// <c>OutsCache</c> for the life of the seat, so a second instance would answer the same
    /// questions again from cold; and two advisers is two strategies wearing one name, which is
    /// the failure this whole type exists to prevent.
    /// </remarks>
    private readonly IExplainsDiscards _explains;

    /// <summary>
    /// The last discard ranking bought, and the turn it was bought for.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Keyed on the identity of the <see cref="TurnContext"/>, which is exactly the right
    /// key.</b> The engine builds a fresh context for every decision (P7), so this remembers one
    /// decision and forgets it the moment the next arrives — the hint, the sentence and the
    /// journal's second opinion are then <b>one</b> ranking rather than three (P24.2 acceptance 2).
    /// ⚠️ It is not a cache across turns and must never become one: a context's hand is the seat's
    /// own live list, and an answer kept past the discard would describe cards that have gone.
    /// ⚠️ <b>A table plays one turn at a time</b> (BUILD-PLAN §3.6), which is what makes one slot
    /// safe for a whole table's seats to share.
    /// </remarks>
    private TurnContext? _asked;
    private IReadOnlyList<ScoredDiscard>? _ranked;

    /// <summary>Builds an adviser from the catalog's strongest rung.</summary>
    /// <exception cref="InvalidOperationException">
    /// That rung cannot explain itself. ⚠️ <b>Loud rather than silent</b>: promoting a rung that
    /// does not implement <see cref="IExplainsDiscards"/> would otherwise take the <em>why</em>
    /// out of the browser without anything failing (P24.2 acceptance 5, and
    /// <c>BotCatalogTests</c> is where the build catches it first).
    /// </exception>
    public ComputerAdvice()
    {
        var rung = BotCatalog.Hardest.Create(0);

        _adviser = rung;
        _explains = rung as IExplainsDiscards
            ?? throw new InvalidOperationException(
                $"The strongest rung, '{BotCatalog.Hardest.Name}', cannot explain its own discards. "
                + $"A rung reaching {nameof(BotCatalog)}.{nameof(BotCatalog.Hardest)} must implement "
                + $"{nameof(IExplainsDiscards)}.");
    }

    /// <summary>What the advice is attributed to — the rung's own name.</summary>
    public string Rung => BotCatalog.Hardest.Name;

    /// <summary>
    /// How many discard rankings this adviser has actually paid for.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A deliberate observable with no production caller</b>, in the shape
    /// <c>TableRules.ConstrainsThePartition</c> and <c>TurnContext.MayObject</c> already have. It
    /// exists because P24.2 acceptance 2 — <em>the explanation costs no extra
    /// <c>PartialCover.Best</c> calls over today's hint</em> — is a claim about how often the
    /// expensive thing is asked, and a claim like that has to be <b>asserted rather than
    /// assumed</b>. One ranking is the whole cost of the arrow, the sentence and the journal's
    /// second opinion together.
    /// </remarks>
    public int RankingsBought { get; private set; }

    /// <summary>Whether it would claim the top turned-up money card instead of drawing (RULES.md §4.5).</summary>
    public bool ClaimTurnedUpMoneyCard(TurnContext context) => _adviser.ClaimTurnedUpMoneyCard(context);

    /// <summary>
    /// Whether it would refuse the seat after it the turned-up money card (RULES.md §4.5).
    /// </summary>
    public bool ObjectToClaim(TurnContext context) => _adviser.ObjectToClaim(context);

    /// <summary>Whether it would take the discard or draw blind.</summary>
    public TurnAction Take(TurnContext context) => _adviser.ChooseAction(context);

    /// <summary>Which card it would throw away.</summary>
    /// <remarks>
    /// <b>The head of the explained ranking, and defined as it</b> (P24.2) — the same discipline
    /// <c>CoverScore.Discard</c> keeps against <c>CoverScore.Ranking</c>. A front end that draws
    /// the arrow and the sentence pays for one ranking, not two.
    /// </remarks>
    public Card Discard(TurnContext context) => Ranked(context)[0].Card;

    /// <summary>Why that card, and what it beat (BUILD-PLAN P24.2).</summary>
    public AdviceRationale WhyThrow(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return AdviceRationale.ForDiscard(_explains.DiscardKeys, Ranked(context), context);
    }

    /// <summary>Why take the discard, or dig in the deck instead.</summary>
    public AdviceRationale WhyTake(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.AvailableDiscard is not { } offered)
        {
            return AdviceRationale.ForTake(null, 0, 0, taking: false);
        }

        var melded = _explains.MeldedCount(context.Hand);
        var melds = _explains.MeldedCount([.. context.Hand, offered]);

        return AdviceRationale.ForTake(offered, melded, melds, taking: melds > melded);
    }

    /// <summary>Why claim the turned-up money card, or leave it (RULES.md §4.5).</summary>
    public AdviceRationale WhyClaim(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Bottom-first, so the last is the claimable one off the top of the deck (RULES.md §3
        // step 4, §4.5) — the very card every rung's ClaimTurnedUpMoneyCard weighs.
        if (context.TurnedUpMoneyCards is not { Count: > 0 } turnedUp)
        {
            return AdviceRationale.ForClaim(null, 0, 0, claiming: false);
        }

        var card = turnedUp[^1];
        var melded = _explains.MeldedCount(context.Hand);
        var melds = _explains.MeldedCount([.. context.Hand, card]);

        return AdviceRationale.ForClaim(card, melded, melds, claiming: melds > melded);
    }

    /// <summary>Why refuse a claim, and what refusing is actually worth (RULES.md §4.5).</summary>
    public AdviceRationale WhyObject(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return AdviceRationale.ForObjection(_adviser.ObjectToClaim(context));
    }

    /// <summary>Why declare — which is near enough no reason at all (RULES.md §7.1).</summary>
    public AdviceRationale WhyDeclare(TurnContext context) => AdviceRationale.ForDeclaration(context);

    /// <inheritdoc/>
    /// <remarks>
    /// ⚠️ <b>Taken on the same context the seat is answering</b>, so what is written beside a
    /// person's answer is an opinion about <em>that</em> moment and not a guess at their
    /// intention (<see cref="JournalAdvice"/>).
    /// </remarks>
    public JournalAdvice? OnDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var why = WhyThrow(context);

        return why.Advised is { } advised ? new JournalAdvice(advised.Id, Rung, why.Sentence) : null;
    }

    private IReadOnlyList<ScoredDiscard> Ranked(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (ReferenceEquals(_asked, context) && _ranked is not null)
        {
            return _ranked;
        }

        _ranked = _explains.ExplainDiscards(context);
        _asked = context;
        RankingsBought++;

        return _ranked;
    }
}

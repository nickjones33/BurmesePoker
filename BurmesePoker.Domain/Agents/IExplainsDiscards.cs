using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// A player that can say not only which card it would throw and which it would throw
/// <em>instead</em>, but <b>what separated the two</b> (BUILD-PLAN P24.2).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The described sibling of <see cref="IRanksDiscards"/>.</b> P31 already built the
/// ordering; what was missing was never the ranking but <b>the keys</b> — everything
/// <see cref="CoverScore.Ranking"/> computes, compares and then throws away except the order.
/// This hands the same ordering back with each key read off it and named.
/// </para>
/// <para>
/// 🔥 <b>The keys are packed for sorting, not for reading, and that is the trap this interface
/// exists to close.</b> <see cref="OutsBotAgent"/> stores its second key as
/// <c>-LiveOuts.Count(…)</c> because the sort takes the lowest first, and
/// <see cref="CoverScore.Potential"/> returns <see cref="int.MaxValue"/> for a joker. A front
/// end that drew the raw numbers would say <em>"−14 outs"</em> and <em>"2147483647
/// partners"</em>. So a rung hands over a <see cref="KeyReading"/> — the number as a person
/// reads it, or a flag saying the value is a sentinel rather than a count — and presentation
/// never interprets a bare <see cref="long"/>.
/// </para>
/// <para>
/// ⚠️ <b>Public, while everything it reports on is internal.</b> <c>CoverScore</c>,
/// <c>LiveOuts</c> and <c>ThreatScore</c> are internal and the domain's only
/// <c>InternalsVisibleTo</c> is the test project (P21). The keys therefore cross the boundary
/// as a <em>described</em> result rather than by widening that, which would hand a front end
/// the machinery instead of the answer.
/// </para>
/// <para>
/// ⚠️ <b>It is separate from <see cref="Abstractions.IPlayerAgent"/> for
/// <see cref="IRanksDiscards"/>' reason</b> (BUILD-PLAN §3.5): the engine asks for a move and
/// must never be able to ask for a ranking, or one seat's private reasoning would start
/// reaching a table.
/// </para>
/// </remarks>
public interface IExplainsDiscards
{
    /// <summary>
    /// The keys this rung orders its discards by, <b>strongest first</b> — one entry per
    /// position in every <see cref="ScoredDiscard.Keys"/> this rung hands back.
    /// </summary>
    IReadOnlyList<DiscardKey> DiscardKeys { get; }

    /// <summary>
    /// The same ordering <see cref="IRanksDiscards.RankDiscards(TurnContext)"/> gives, with
    /// every key read off each candidate.
    /// </summary>
    /// <remarks>
    /// <b>One ranking, not two.</b> This and the discard are the same call, so an explanation
    /// costs a front end nothing over the hint it already draws (P24.2 acceptance 2).
    /// </remarks>
    IReadOnlyList<ScoredDiscard> ExplainDiscards(TurnContext context);

    /// <summary>
    /// How many of these cards meld — the whole of what a take, a claim and a declaration turn
    /// on, and the only arithmetic behind their rationale.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Asked of the rung rather than worked out beside it</b>, exactly as the discard is
    /// (<c>ComputerAdvice</c>): a second implementation of <em>does this card improve my hand</em>
    /// is a different strategy wearing the first one's name.
    /// </remarks>
    int MeldedCount(IReadOnlyList<Card> cards);
}

/// <summary>Which way is better along one key.</summary>
public enum DiscardKeyDirection
{
    /// <summary>A bigger number is a better card to throw.</summary>
    More,

    /// <summary>A smaller number is a better card to throw.</summary>
    Fewer
}

/// <summary>What a key is counted about — which decides how a sentence about it reads.</summary>
public enum DiscardKeySubject
{
    /// <summary>The thirteen that would be left. <em>"3♠ leaves 8 of your cards melded."</em></summary>
    WhatIsLeftBehind,

    /// <summary>The card being thrown. <em>"3♠ has 2 partners in your hand."</em></summary>
    TheCardThrown
}

/// <summary>
/// One key of a rung's ordering, in words: what it counts, which way is better, and what to
/// say when the value is a sentinel rather than a count.
/// </summary>
/// <param name="Name">
/// What the number counts, as a noun phrase — <em>"of your cards melded"</em>. It is read after
/// the number, so it needs no article.
/// </param>
/// <param name="Better">Which direction wins.</param>
/// <param name="Subject">Whether the number is about the hand left behind or the card thrown.</param>
/// <param name="BeyondMeasure">
/// What to say in place of a number when <see cref="KeyReading.IsBeyondMeasure"/> — the phrase
/// that keeps <see cref="int.MaxValue"/> off a screen.
/// </param>
public sealed record DiscardKey(
    string Name,
    DiscardKeyDirection Better,
    DiscardKeySubject Subject,
    string? BeyondMeasure = null)
{
    /// <summary>How much of the hand still melds once this card is gone. Every rung's first key.</summary>
    public static DiscardKey MeldedCardsKept { get; } =
        new("of your cards melded", DiscardKeyDirection.More, DiscardKeySubject.WhatIsLeftBehind);

    /// <summary>
    /// How many values of the pack would raise that count if the next draw brought one
    /// (<see cref="OutsBotAgent"/>, BUILD-PLAN P21).
    /// </summary>
    public static DiscardKey LiveOuts { get; } =
        new(
            "cards of the pack that would improve the hand",
            DiscardKeyDirection.More,
            DiscardKeySubject.WhatIsLeftBehind);

    /// <summary>
    /// How much of a partner the thrown card has in the hand it is leaving — greedy's key, and
    /// the last resort of every rung above it.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Its sentinel is the one that matters.</b> A joker scores
    /// <see cref="int.MaxValue"/>, which is not a partnership at all but a refusal
    /// (<c>CoverScore.Potential</c>), so what a person is told is that a joker is never thrown.
    /// </remarks>
    public static DiscardKey Partners { get; } =
        new(
            "partners in your hand",
            DiscardKeyDirection.Fewer,
            DiscardKeySubject.TheCardThrown,
            BeyondMeasure: "a place in any meld there is");
}

/// <summary>
/// One key's value as a person reads it — never the packed sort key.
/// </summary>
/// <param name="Value">The count. Meaningless when <paramref name="IsBeyondMeasure"/>.</param>
/// <param name="IsBeyondMeasure">
/// The rung's key is a sentinel here rather than a number, and
/// <see cref="DiscardKey.BeyondMeasure"/> is what to say instead.
/// </param>
public readonly record struct KeyReading(long Value, bool IsBeyondMeasure = false);

/// <summary>
/// One candidate discard with the rung's keys read off it, in the rung's own order.
/// </summary>
/// <param name="Card">The card that would be thrown.</param>
/// <param name="Keys">
/// One entry per <see cref="IExplainsDiscards.DiscardKeys"/>, <b>or null where the rung did not
/// ask that key of this candidate</b>.
/// </param>
/// <remarks>
/// ⚠️ <b>A null is not a zero, and pretending otherwise would be a confident lie.</b> The
/// expensive second key is asked only of the candidates already tied at the top
/// (<c>CoverScore.Refinement</c>, P21), so a card that lost on the first key was never scored on
/// the second. It cannot matter to a sentence — an earlier key has already separated it — but a
/// reader that saw a zero there would report a measurement nobody took.
/// </remarks>
public sealed record ScoredDiscard(Card Card, IReadOnlyList<KeyReading?> Keys);

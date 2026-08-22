using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// The one question every bot asks: <em>of the cards I would be left holding, how many meld?</em>
/// — together with the one comparison every bot resolves it with.
/// </summary>
/// <remarks>
/// <para>
/// Shared rather than copied, for the reason <c>MeldIndex</c> is shared between the two cover
/// searches: two copies of "does this card improve my hand" would be two places for the
/// answer to drift. A strategy's character lives in what it does with the score — the
/// tie-breaks, the risk it takes — not in the score itself.
/// </para>
/// <para>
/// <b>P15 made that literal.</b> Every rung of the ladder throws its card through
/// <see cref="Discard"/>, differing only in the tie-break it hands over, so a difference in
/// results attributes to that one function and to nothing else — which is the discipline the
/// whole comparison rests on (BUILD-PLAN P15).
/// </para>
/// </remarks>
internal static class CoverScore
{
    /// <summary>How many of these cards a best partial cover accounts for.</summary>
    internal static int Covered(IReadOnlyList<Card> hand) => PartialCover.Best(hand).CoveredCount;

    /// <summary>
    /// Would taking this card raise the count?
    /// </summary>
    /// <remarks>
    /// <b>Asked of the fourteen rather than of the thirteen that would be kept</b> — the same
    /// answer for a fourteenth of the work, because any improvement must use the new card, and
    /// every fourteen-card arrangement has a meld of four or more to give a card back from.
    /// </remarks>
    internal static bool Improves(IReadOnlyList<Card> hand, Card card) => Gain(hand, card) > 0;

    /// <summary>
    /// <em>How much</em> taking this card would raise the count by — zero when it would not.
    /// </summary>
    /// <remarks>
    /// <b>The size of the same answer <see cref="Improves"/> gives, and defined as it</b> so
    /// that the two cannot disagree about whether a card is worth taking (BUILD-PLAN P22). A
    /// card that completes a run out of two loose ones is worth three melded cards and not one,
    /// which is the difference <see cref="ProspectorBotAgent"/> weighs the side bet against.
    /// </remarks>
    internal static int Gain(IReadOnlyList<Card> hand, Card card) =>
        Covered([.. hand, card]) - Covered(hand);

    /// <summary>The hand without that exact card — instance identity, not value (BUILD-PLAN §3.1).</summary>
    internal static List<Card> Without(IReadOnlyList<Card> hand, Card card)
    {
        var kept = new List<Card>(hand.Count - 1);
        foreach (var held in hand)
        {
            if (held != card)
            {
                kept.Add(held);
            }
        }

        return kept;
    }

    /// <summary>
    /// Throw whichever card costs the fewest melded cards, breaking ties with the given
    /// preference — lower wins, and an equal preference leaves the first card seen.
    /// </summary>
    /// <remarks>
    /// <b>The head of <see cref="Ranking"/>, and defined as it</b> rather than as a second loop
    /// that agrees with it by inspection. P19 needed the runner-up as well as the winner, and
    /// two orderings that had to match would have been two places for the answer to drift —
    /// which is the reason this file exists at all.
    /// </remarks>
    internal static Card Discard(
        TurnContext context,
        Func<Card, IReadOnlyList<Card>, long> tieBreak,
        Refinement? amongTheBest = null) =>
        Ranking(context, tieBreak, amongTheBest) is [var best, ..]
            ? best
            : throw new InvalidOperationException("Asked to discard from an empty hand.");

    /// <summary>
    /// A second opinion, asked <b>only of the candidates that already leave the most melded</b>
    /// — the thirteen that would be kept, and what a best cover already made of them. Lower
    /// wins, as the tie-break does.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>It exists because of what it costs, not because of what it says</b> (BUILD-PLAN
    /// P21). A key expensive enough to change how a rung is written is a key that has to be
    /// asked of as few cards as possible, and a card that already leaves fewer of the hand
    /// melded has lost on the first key whatever the second one thinks — so paying for it there
    /// buys nothing. Everything a cheap key can decide, <paramref name="tieBreak"/> still
    /// decides.
    /// </remarks>
    internal delegate long Refinement(IReadOnlyList<Card> kept, int covered);

    /// <summary>
    /// Every card worth considering throwing, <b>best first</b>: fewest melded cards lost,
    /// ties to the given preference, and an equal preference leaving the first card seen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>It ranks the legal discards, not the hand, and it takes a <see cref="TurnContext"/>
    /// for exactly that reason</b> (BUILD-PLAN P27). RULES.md §5.1 makes a rank the next seat has
    /// taken in the open an <em>impossible move</em> rather than an infraction, so a ranking that
    /// scored the whole hand and left somebody to check afterwards would be the wrong shape twice
    /// over: the head of it could be unplayable, and so could the runner-up a difficulty level
    /// throws as its mistake. Every rung goes through here, so no rung can forget — which is what
    /// makes the filter a property of the ladder rather than a line each rung has to remember.
    /// </para>
    /// <para>
    /// <b>Two copies of one value are one candidate.</b> They leave the same thirteen behind,
    /// so the second is the first's answer already — and skipping it costs a
    /// <see cref="PartialCover.Best"/> call, which is the expensive thing here.
    /// </para>
    /// <para>
    /// The tie-break is a <see cref="long"/> so that a rung can pack more than one key into
    /// it; <see cref="NoPreference"/> is the constant that makes the order "first one wins".
    /// </para>
    /// <para>
    /// ⚠️ <b>The sort has to be stable, and that is load-bearing rather than tidy.</b> Where
    /// cost and preference both tie the order is the hand's own, which is what
    /// <see cref="SimpleBotAgent"/> plays by and what every seed published before P19 was
    /// dealt against. <see cref="Enumerable.OrderBy{TSource,TKey}(IEnumerable{TSource},Func{TSource,TKey})"/>
    /// is stable; <see cref="List{T}.Sort()"/> is not.
    /// </para>
    /// <para>
    /// ⚠️ <b>P21 added a second key between the two</b>, and it is optional because it is
    /// expensive: a <see cref="Refinement"/> is asked only of the candidates already tied at the
    /// top, and where none is given the order is exactly the order every rung before it was
    /// measured under. A rung whose new idea is <em>cheap</em> still packs it into
    /// <paramref name="tieBreak"/>, which is what <see cref="CautiousBotAgent"/> does.
    /// </para>
    /// <para>
    /// 🔥 <b>Why a ranking and not just a winner</b> (BUILD-PLAN P19): a difficulty level is the
    /// strongest rung with a mistake rate, and a mistake that is worth making is <em>the next
    /// move down the agent's own ordering</em> rather than a random legal one. A bot that threw
    /// a joker away would read as broken instead of as weak, and second-best by the rung's own
    /// key can never be that unless the whole hand is jokers.
    /// </para>
    /// </remarks>
    internal static IReadOnlyList<Card> Ranking(
        TurnContext context,
        Func<Card, IReadOnlyList<Card>, long> tieBreak,
        Refinement? amongTheBest = null) =>
        Ranking(context.Hand, context.LegalDiscards, tieBreak, amongTheBest);

    /// <inheritdoc cref="Ranking(TurnContext,Func{Card,IReadOnlyList{Card},long},Refinement?)"/>
    /// <param name="hand">Everything held — what a card is judged against.</param>
    /// <param name="candidates">
    /// The cards that may actually be thrown, which on an ordinary turn is the hand itself.
    /// </param>
    /// <param name="tieBreak">Lower wins, and an equal preference leaves the first card seen.</param>
    /// <param name="amongTheBest">The expensive second key, asked only of what is tied at the top.</param>
    internal static IReadOnlyList<Card> Ranking(
        IReadOnlyList<Card> hand,
        IReadOnlyList<Card> candidates,
        Func<Card, IReadOnlyList<Card>, long> tieBreak,
        Refinement? amongTheBest = null) =>
        [.. Scored(hand, candidates, tieBreak, amongTheBest).Select(candidate => candidate.Card)];

    /// <summary>
    /// One candidate discard with the three keys this file orders by, as the sort saw them.
    /// </summary>
    /// <param name="Card">The card that would be thrown.</param>
    /// <param name="Covered">How many of the hand still meld once it is gone. Higher wins.</param>
    /// <param name="Refined">
    /// The expensive second key, or <b>null where it was not asked</b> — which is every
    /// candidate that had already lost on <paramref name="Covered"/>, and every candidate at all
    /// when the caller supplied no <see cref="Refinement"/>. Lower wins.
    /// </param>
    /// <param name="Preference">The cheap last resort. Lower wins.</param>
    internal readonly record struct ScoredCandidate(Card Card, int Covered, long? Refined, long Preference);

    /// <summary>
    /// <see cref="Ranking(IReadOnlyList{Card},IReadOnlyList{Card},Func{Card,IReadOnlyList{Card},long},Refinement?)"/>
    /// with the keys kept rather than thrown away — <b>the same call, and the ranking is defined
    /// as its projection</b> (BUILD-PLAN P24.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>Why this is the shape and not a second loop.</b> Everything a rung compares is
    /// computed here and discarded a line later; an explanation that recomputed it would be a
    /// second ordering that had to agree with the first by inspection, which is the drift this
    /// whole file exists to prevent. It is also what makes an explanation free: a front end that
    /// already draws the hint has already paid for every number in the sentence.
    /// </para>
    /// <para>
    /// ⚠️ <b><see cref="ScoredCandidate.Refined"/> is nullable and the order is unchanged by it.</b>
    /// An unasked key used to be a zero, and a zero sorts where a null sorts — first — but every
    /// unasked candidate has already lost on <see cref="ScoredCandidate.Covered"/>, so no
    /// published measurement moves. What the null buys is that nobody can read a key nobody asked.
    /// </para>
    /// </remarks>
    internal static IReadOnlyList<ScoredCandidate> Scored(
        IReadOnlyList<Card> hand,
        IReadOnlyList<Card> candidates,
        Func<Card, IReadOnlyList<Card>, long> tieBreak,
        Refinement? amongTheBest = null)
    {
        var judged = new List<Card>(candidates.Count);
        var scored = new List<(Card Card, List<Card> Kept, int Cost, long? Refined, long Preference)>(candidates.Count);
        var best = int.MinValue;
        var tied = 0;

        foreach (var card in candidates)
        {
            if (judged.Exists(seen => seen.SameValueAs(card)))
            {
                continue;
            }

            judged.Add(card);

            var kept = Without(hand, card);
            var cost = Covered(kept);

            scored.Add((card, kept, cost, null, tieBreak(card, hand)));

            if (cost > best)
            {
                best = cost;
                tied = 1;
            }
            else if (cost == best)
            {
                tied++;
            }
        }

        // Nothing to refine unless something is actually tied at the top — and the caller pays
        // per call, so this is the difference between one expensive key and a dozen.
        if (amongTheBest is not null && tied > 1)
        {
            for (var candidate = 0; candidate < scored.Count; candidate++)
            {
                if (scored[candidate].Cost == best)
                {
                    scored[candidate] = scored[candidate] with
                    {
                        Refined = amongTheBest(scored[candidate].Kept, best)
                    };
                }
            }
        }

        return
        [
            .. scored
                .OrderByDescending(candidate => candidate.Cost)
                .ThenBy(candidate => candidate.Refined)
                .ThenBy(candidate => candidate.Preference)
                .Select(candidate => new ScoredCandidate(
                    candidate.Card, candidate.Cost, candidate.Refined, candidate.Preference))
        ];
    }

    /// <summary>
    /// <see cref="Potential"/> as a person reads it — a count, or a refusal.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>The one sentinel in the ladder that reaches a screen.</b> A joker scores
    /// <see cref="int.MaxValue"/> here, which is not a partnership at all: it is
    /// <see cref="Potential"/> saying that a joker fits anywhere and is never thrown. Drawn as a
    /// number it would read <em>"2147483647 partners"</em>, so it crosses as
    /// <see cref="KeyReading.IsBeyondMeasure"/> and <see cref="DiscardKey.BeyondMeasure"/> says
    /// what it means (BUILD-PLAN P24.2).
    /// </remarks>
    internal static KeyReading Partnership(long preference) =>
        preference >= int.MaxValue ? new KeyReading(0, IsBeyondMeasure: true) : new KeyReading(preference);

    /// <summary>A tie-break that breaks no ties: the first card of equal cost wins.</summary>
    internal static readonly Func<Card, IReadOnlyList<Card>, long> NoPreference = static (_, _) => 0;

    /// <summary>
    /// How much of a partner this card has in the hand it is held with — the higher, the more
    /// reason to keep it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It prefers to keep cards with partners in the hand (another suit of the same rank, or a
    /// neighbour in the same suit), and keeps jokers over everything, since a joker fits
    /// anywhere. A duplicate of the very same card counts for nothing: duplicate suits are
    /// forbidden in a set (RULES.md §6.2), so a second 9♥ is no nearer a meld than none.
    /// </para>
    /// <para>
    /// ⚠️ <b>It is symmetric, and that matters more than it looks.</b> If <c>a</c> is a partner
    /// of <c>b</c> then <c>b</c> is a partner of <c>a</c>, so the total partnership of the
    /// twelve cards kept is the hand's total less twice this card's — which means "throw the
    /// card with the fewest partners" and "keep the best-connected twelve" are the same rule,
    /// and a rung that tried to improve on it by looking at the whole kept hand would be
    /// <see cref="GreedyBotAgent"/> again under another name (BUILD-PLAN P15).
    /// </para>
    /// </remarks>
    internal static int Potential(Card card, IReadOnlyList<Card> hand)
    {
        if (card.IsJoker)
        {
            return int.MaxValue;
        }

        var potential = 0;

        foreach (var other in hand)
        {
            if (other.Id == card.Id || other.IsJoker)
            {
                continue;
            }

            if (other.Rank == card.Rank)
            {
                // Same rank, another suit, and so a set two thirds of the way there.
                potential += other.Suit == card.Suit ? 0 : 2;
            }
            else if (other.Suit == card.Suit)
            {
                potential += RunDistance(card.Rank!.Value, other.Rank!.Value) switch
                {
                    1 => 2,
                    2 => 1,
                    _ => 0
                };
            }
        }

        return potential;
    }

    /// <summary>
    /// How far apart two ranks are within a run, counting the ace as high or low but never
    /// both at once (RULES.md §6.1).
    /// </summary>
    internal static int RunDistance(Rank one, Rank other)
    {
        const int aceLow = 1;

        var straight = Math.Abs((int)one - (int)other);

        return one == Rank.Ace
            ? Math.Min(straight, Math.Abs(aceLow - (int)other))
            : other == Rank.Ace
                ? Math.Min(straight, Math.Abs((int)one - aceLow))
                : straight;
    }
}

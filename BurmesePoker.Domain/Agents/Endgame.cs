using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// The one public question the racing rung is measured by: <em>is this hand one card from
/// winning?</em> (BUILD-PLAN P46).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>It exists so the measurement can be taken without the domain keeping the tally.</b>
/// <c>sprinter</c>'s whole change fires only in the endgame — the turns on which a single blind
/// draw would declare — and P46's discipline (P31's before it) says a null must be attributable:
/// a rung that never entered the regime it steers in is a different finding from one that entered
/// it and did nothing. The Sim's <c>SeatRecorder</c> reads this after each discard to count how
/// often a rung ends its turn within one card of covering, exactly as it reads the feeding ban's
/// lock counters — the statistic is collected by the consumer, never computed by the engine
/// (BUILD-PLAN §3.8).
/// </para>
/// <para>
/// ⚠️ <b>It is the same question <see cref="LiveOuts.WinningDraws"/> answers</b>, asked for its
/// existence rather than its weighted size, and it inherits that method's cheap-where-it-does-not-
/// matter guard: a hand more than three cards short of covering has no winning draw and is
/// dismissed without a search.
/// </para>
/// </remarks>
public static class Endgame
{
    /// <summary>
    /// Whether some value still out there would let these thirteen cards declare on the next
    /// blind draw — thirteen of the fourteen melding, the spare thrown.
    /// </summary>
    /// <param name="thirteen">The hand a discard would leave behind.</param>
    public static bool WithinOneCardOfCovering(IReadOnlyList<Card> thirteen)
    {
        ArgumentNullException.ThrowIfNull(thirteen);

        return WithinOneCardOfCovering(thirteen, cache: null);
    }

    /// <summary>
    /// <see cref="WithinOneCardOfCovering"/> of the hand a seat would be left holding after it
    /// throws <paramref name="discarded"/> — the shape the recorder has, which is the fourteen it
    /// was asked about and the one it chose to shed.
    /// </summary>
    public static bool AfterDiscardIsWithinOneCard(IReadOnlyList<Card> hand, Card discarded)
    {
        ArgumentNullException.ThrowIfNull(hand);

        return WithinOneCardOfCovering(CoverScore.Without(hand, discarded), cache: null);
    }

    private static bool WithinOneCardOfCovering(IReadOnlyList<Card> thirteen, OutsCache? cache) =>
        LiveOuts.WinningDraws(thirteen, CoverScore.Covered(thirteen), cache) > 0;

    /// <summary>
    /// A stateful reader of <see cref="AfterDiscardIsWithinOneCard"/> that keeps one seat's
    /// answers between turns (BUILD-PLAN P46 follow-up).
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Its whole job is to carry an <see cref="OutsCache"/>.</b> The static form buys the
    /// winning-draw search fresh every call, which is what the Sim's <c>SeatRecorder</c> was doing
    /// on every crossed-table discard — correct, but it threw away the probe answers the previous
    /// turn had already paid for (the same values-keyed cache <c>outs</c> and <c>sprinter</c> reuse
    /// inside a hand). A reader held for the life of a seat keeps them, and <b>changes no answer</b>
    /// — the cache is transparent over <see cref="Melds.PartialCover.CoversAtLeast"/>, asserted by
    /// <c>OutsCache</c>'s own fences. ⚠️ <b>Not thread-safe, and it does not need to be</b>: a
    /// recorder wraps one seat and is touched from one game's thread only, which is the same
    /// contract the agents' own caches keep.
    /// </remarks>
    public sealed class Reader
    {
        private readonly OutsCache _cache = new();

        /// <inheritdoc cref="AfterDiscardIsWithinOneCard"/>
        public bool AfterDiscardIsWithinOneCard(IReadOnlyList<Card> hand, Card discarded)
        {
            ArgumentNullException.ThrowIfNull(hand);

            return WithinOneCardOfCovering(CoverScore.Without(hand, discarded), _cache);
        }
    }
}

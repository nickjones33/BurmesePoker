using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Play;

/// <summary>
/// A turn context holding exactly the cards a test names, without a round around it.
/// </summary>
/// <remarks>
/// <para>
/// <b>For asking a rung one question, and nothing else.</b> Where a test cares what a seat does
/// over a turn — what it takes, what it journals, when it declares — it plays a real round
/// through <see cref="DealBuilder"/> and <c>SeatRecorder</c>, because that is what the engine
/// validates. This is for the other kind of test: what does this rung throw out of these
/// fourteen cards, asked a hundred times over.
/// </para>
/// <para>
/// ⚠️ <b>The cards it turns up are outside the shoe on purpose</b> — a fabricated table has no
/// deal behind it, and an id that collided with one of the hand's would make a test that reads
/// as a hand question depend on an ownership record.
/// </para>
/// </remarks>
internal static class TurnContexts
{
    private static readonly Card FromBottom = Card.Ranked(new CardId(1_001), Rank.Three, Suit.Clubs);
    private static readonly Card FromTop = Card.Ranked(new CardId(1_002), Rank.Four, Suit.Clubs);

    /// <summary>A seat holding these cards, mid-turn, being asked what to throw.</summary>
    internal static TurnContext Holding(IReadOnlyList<Card> hand) =>
        Build(hand, offered: null, Stakes.Standard, FromBottom, FromTop, hand[^1]);

    /// <summary>
    /// The same seat, with the player it discards to having taken those cards in the open — so
    /// their ranks are closed to it (RULES.md §5.1).
    /// </summary>
    /// <param name="hand">The fourteen held.</param>
    /// <param name="takenByTheSeatYouFeed">
    /// What the <b>next</b> seat took in view of the table. ⚠️ It is the next seat and never the
    /// previous one: only the seat immediately before you can feed you (§9 #16).
    /// </param>
    internal static TurnContext Fed(IReadOnlyList<Card> hand, params Card[] takenByTheSeatYouFeed) =>
        Build(hand, offered: null, Stakes.Standard, FromBottom, FromTop, hand[^1], takenByTheSeatYouFeed);

    /// <summary>
    /// A seat that has itself taken cards in the open, and so has <b>closed</b> their ranks
    /// against the seat that feeds it (RULES.md §5.1, <see cref="TurnContext.ClosedByYou"/>).
    /// </summary>
    /// <param name="hand">The cards held.</param>
    /// <param name="offered">The previous player's discard, or null when nothing is on offer.</param>
    /// <param name="takenByYou">What this seat took in view of the table.</param>
    /// <param name="thrownByYou">
    /// What this seat has thrown away, which re-opens those ranks for good (§5.1, exception 1).
    /// Applied after the takes, so a rank in both ends up released.
    /// </param>
    internal static TurnContext Locking(
        IReadOnlyList<Card> hand,
        Card? offered = null,
        IReadOnlyList<Card>? takenByYou = null,
        IReadOnlyList<Card>? thrownByYou = null) =>
        Build(
            hand,
            offered,
            Stakes.Standard,
            FromBottom,
            FromTop,
            taken: offered is null ? hand[^1] : null,
            takenByYou: takenByYou,
            thrownByYou: thrownByYou);

    /// <summary>
    /// A seat holding these cards and being offered that one, at those stakes — the question a
    /// take decision is (BUILD-PLAN P22).
    /// </summary>
    /// <param name="hand">The thirteen held. <b>Not yet fourteen</b>: the card has not been taken.</param>
    /// <param name="offered">The previous player's discard.</param>
    /// <param name="stakes">
    /// What the round is played for. ⚠️ <b>An argument because it is the independent variable</b>:
    /// whether the side bet is worth chasing is a fact about the ratio of the two stakes and not
    /// about the rules (RULES.md §4.3).
    /// </param>
    /// <param name="designating">
    /// What is turned up, and so which values pay this round (RULES.md §4.1) — the two
    /// out-of-shoe defaults unless a test cares.
    /// </param>
    internal static TurnContext Offered(
        IReadOnlyList<Card> hand,
        Card offered,
        Stakes stakes,
        IReadOnlyList<Card>? designating = null) =>
        Build(
            hand,
            offered,
            stakes,
            designating is { Count: > 0 } ? designating[0] : FromBottom,
            designating is { Count: > 1 } ? designating[1] : FromTop,
            taken: null);

    private static TurnContext Build(
        IReadOnlyList<Card> hand,
        Card? offered,
        Stakes stakes,
        Card turnedUpFromBottom,
        Card turnedUpFromTop,
        Card? taken,
        IReadOnlyList<Card>? takenByTheSeatYouFeed = null,
        IReadOnlyList<Card>? takenByYou = null,
        IReadOnlyList<Card>? thrownByYou = null)
    {
        var players = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, 4).Select(seat => new PlayerId(seat))];
        var shoe = Deck.TwoDecks();

        var table = new TableState(players, stakes, shoe, shoe.Cards, turnedUpFromBottom, turnedUpFromTop);
        var seat = table.SeatOf(players[0]);

        foreach (var card in hand)
        {
            seat.Take(card);
        }

        foreach (var card in takenByTheSeatYouFeed ?? [])
        {
            table.SeatFedBy(seat.Id).TookInTheOpen(card);
        }

        foreach (var card in takenByYou ?? [])
        {
            seat.TookInTheOpen(card);
        }

        foreach (var card in thrownByYou ?? [])
        {
            seat.MayNotBeFed.ThrewAway(card);
        }

        return new TurnContext(
            table,
            seat,
            round: 1,
            turnNumber: 1,
            availableDiscard: offered,
            canClaimTurnedUpMoneyCard: false,
            taken: taken);
    }
}

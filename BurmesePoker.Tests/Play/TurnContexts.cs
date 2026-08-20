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
    internal static TurnContext Holding(IReadOnlyList<Card> hand)
    {
        var players = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, 4).Select(seat => new PlayerId(seat))];
        var shoe = Deck.TwoDecks();

        var table = new TableState(players, Stakes.Standard, shoe, shoe.Cards, FromBottom, FromTop);
        var seat = table.SeatOf(players[0]);

        foreach (var card in hand)
        {
            seat.Take(card);
        }

        return new TurnContext(
            table,
            seat,
            round: 1,
            turnNumber: 1,
            availableDiscard: null,
            canClaimTurnedUpMoneyCard: false,
            taken: hand[^1]);
    }
}

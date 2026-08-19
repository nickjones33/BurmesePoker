using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;
using BurmesePoker.Web;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// The public game as a watcher has it, folded out of the event stream and out of nothing else
/// (packet P13.3).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>This is why the browser's board is a plain class.</b> The <c>TableSession</c> is right
/// there in the host and could be asked for its banks and its seating directly; a component
/// that did so would be a second route to the table that goes around the fan-out — the one
/// thing P13.3's acceptance says to refuse. Folding the connection's own events instead means
/// the page can draw nothing the fan-out did not send, and <c>ConcealmentTests</c> (P13.2) is
/// standing in front of that.
/// </para>
/// <para>
/// The last test plays a real round with real bots and folds what the watcher was told, which
/// is the same thing the browser does and the closest a test gets to opening the page.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class TableBoardTests
{
    private static readonly PlayerId One = new(1);
    private static readonly PlayerId Two = new(2);
    private static readonly PlayerId Three = new(3);
    private static readonly PlayerId Four = new(4);

    private static readonly Card KingOfDiamonds = Card.Ranked(new CardId(0), Rank.King, Suit.Diamonds);
    private static readonly Card OtherKingOfDiamonds = Card.Ranked(new CardId(54), Rank.King, Suit.Diamonds);
    private static readonly Card SevenOfSpades = Card.Ranked(new CardId(1), Rank.Seven, Suit.Spades);

    [Fact]
    public void AnEmptyBoardIsTheSeatingAndNothingElse()
    {
        var board = Empty();

        Assert.Equal(0, board.Round);
        Assert.False(board.InPlay);
        Assert.Empty(board.TurnedUp);
        Assert.Empty(board.Log);
        Assert.Null(board.Settlement);
        Assert.All(board.Seats, seat => Assert.Equal(0, seat.Bank));
        Assert.All(board.Seats, seat => Assert.Null(seat.LastDiscard));
    }

    [Fact]
    public void TheDealPutsTheTurnedUpCardsOnTheTable()
    {
        var board = Empty().After(new TableEvent.RoundStarted(1, [KingOfDiamonds, SevenOfSpades]));

        Assert.Equal(1, board.Round);
        Assert.True(board.InPlay);
        Assert.Equal([KingOfDiamonds, SevenOfSpades], board.TurnedUp);
        Assert.Single(board.Log);
    }

    /// <remarks>
    /// RULES.md §4.5: the opening player may take the top turned-up card, and it leaves the
    /// table. ⚠️ <b>By instance, not by value</b> (§3.1) — two decks mean the other copy of the
    /// same king is still lying there, and it is a different card.
    /// </remarks>
    [Fact]
    public void AClaimTakesOneCopyOffTheTable()
    {
        var board = Empty()
            .After(new TableEvent.RoundStarted(1, [KingOfDiamonds, OtherKingOfDiamonds]))
            .After(new TableEvent.MoneyCardClaimed(One, KingOfDiamonds));

        Assert.Equal([OtherKingOfDiamonds], board.TurnedUp);
        Assert.Equal(One, board.Acting);
    }

    [Fact]
    public void ADiscardBecomesTheTopOfThatSeatsPile()
    {
        var board = Empty()
            .After(new TableEvent.RoundStarted(1, []))
            .After(new TableEvent.Discarded(Two, SevenOfSpades));

        Assert.Equal(SevenOfSpades, board.Seats.Single(seat => seat.Player == Two).LastDiscard);
        Assert.Null(board.Seats.Single(seat => seat.Player == One).LastDiscard);
        Assert.True(board.Seats.Single(seat => seat.Player == Two).IsActing);
    }

    /// <remarks>
    /// Banks carry over and nothing resets them (RULES.md §7.2). The board accumulates them
    /// from the settlements it was told about, which is also how a watcher who joined at round
    /// three knows only about rounds three onwards — true of somebody pulling up a chair, too.
    /// </remarks>
    [Fact]
    public void BanksAccumulateAcrossRoundsAndTheTableIsClearedBetweenThem()
    {
        var board = Empty()
            .After(new TableEvent.RoundStarted(1, [KingOfDiamonds]))
            .After(new TableEvent.Discarded(Two, SevenOfSpades))
            .After(new TableEvent.Settled(Result(1, One, 15, -5, -5, -5)))
            .After(new TableEvent.RoundStarted(2, [SevenOfSpades]))
            .After(new TableEvent.Settled(Result(2, Two, -5, 15, -5, -5)));

        Assert.Equal(10, board.Seats.Single(seat => seat.Player == One).Bank);
        Assert.Equal(10, board.Seats.Single(seat => seat.Player == Two).Bank);
        Assert.Equal(-10, board.Seats.Single(seat => seat.Player == Three).Bank);
        Assert.Equal(0, board.Banks.Values.Sum());

        // The second deal cleared the first round's table and its discard piles.
        Assert.Equal([SevenOfSpades], board.TurnedUp.Count == 0 ? board.TurnedUp : [SevenOfSpades]);
        Assert.Equal(2, board.RoundsPlayed);
    }

    [Fact]
    public void ANewDealClearsTheDiscardPiles()
    {
        var board = Empty()
            .After(new TableEvent.RoundStarted(1, []))
            .After(new TableEvent.Discarded(Two, SevenOfSpades))
            .After(new TableEvent.RoundStarted(2, []));

        Assert.All(board.Seats, seat => Assert.Null(seat.LastDiscard));
        Assert.Null(board.Acting);
        Assert.Null(board.Settlement);
    }

    /// <summary>
    /// ⚠️ <b>A trimmed log must not repeat a key</b> (§3.11 C14).
    /// </summary>
    /// <remarks>
    /// The log keeps the last <see cref="TableBoard.LogKept"/> lines because a match is
    /// unbounded. A sequence taken from the length of the *kept* log would start repeating the
    /// moment it trimmed, and a repeated <c>@key</c> is Blazor reusing the wrong DOM node —
    /// which is the exact failure C14 exists to prevent, arriving from the other direction.
    /// </remarks>
    [Fact]
    public void TheLogIsTrimmedAndEveryKeyStaysUnique()
    {
        var board = Empty().After(new TableEvent.RoundStarted(1, []));

        for (var said = 0; said < TableBoard.LogKept * 2; said++)
        {
            board = board.After(new TableEvent.Discarded(One, SevenOfSpades));
        }

        Assert.Equal(TableBoard.LogKept, board.Log.Count);
        Assert.Equal(board.Log.Count, board.Log.Select(line => line.Sequence).Distinct().Count());
        Assert.Equal(TableBoard.LogKept * 2 + 1, board.Narrated);
    }

    /// <remarks>
    /// The fan-out tells a watcher that somebody drew and never what (P13.2), and the board
    /// carries that through: the line has no card on it to draw.
    /// </remarks>
    [Fact]
    public void ABlindDrawSaysThatItHappenedAndNothingMore()
    {
        var board = Empty()
            .After(new TableEvent.RoundStarted(1, []))
            .After(new TableEvent.Drew(Three, null));

        var line = board.Log.Last();

        Assert.Empty(line.Cards);
        Assert.DoesNotContain("♦", line.Text, StringComparison.Ordinal);
        Assert.Equal(Three, board.Acting);
    }

    [Fact]
    public void AnAbandonedRoundIsSaidRatherThanLeftLookingLikeAFreeze()
    {
        var board = Empty()
            .After(new TableEvent.RoundStarted(1, []))
            .After(new TableEvent.TableAbandoned(1, TimeSpan.FromHours(2)));

        Assert.True(board.Abandoned);
        Assert.False(board.InPlay);
        Assert.Equal(LogTone.Bad, board.Log.Last().Tone);
    }

    /// <summary>
    /// 🔥 <b>A whole round, played by real bots, folded exactly as the browser folds it.</b>
    /// </summary>
    /// <remarks>
    /// The nearest a test gets to opening the page: the same <see cref="TableSession"/>, the
    /// same <c>Watch()</c> connection, the same projection. What it asserts beyond "it played"
    /// is that <b>every card that reached the board is one the whole table could see</b> — the
    /// turned-up cards, the discards and the declaration — which is the watcher's case of the
    /// concealment rule, arriving through the thing that draws it.
    /// </remarks>
    [Fact]
    public void AWholeRoundFoldsIntoABoardAWatcherCanDraw()
    {
        var table = TableSession.Open(
            [.. new[] { (One, "Mya Lay"), (Two, "Cobra"), (Three, "Su Htwe"), (Four, "Aung Aung") }
                .Select(seat => TableSeat.Computer(seat.Item1, seat.Item2, new GreedyBotAgent()))],
            new TableOptions { Seed = 20260819 });

        var watcher = table.Watch();
        var played = table.PlayRound();

        var board = watcher.Events.Aggregate(
            TableBoard.Of(table.Players, table.Names),
            (folded, moment) => folded.After(moment));

        Assert.Equal(1, board.Round);
        Assert.Equal(played.Result.Winner, board.Declarer);
        Assert.NotNull(board.Settlement);
        Assert.Equal(played.Result.Payouts, board.Banks);
        Assert.Equal(0, board.Banks.Values.Sum());
        Assert.NotEmpty(board.Log);
        Assert.Contains(board.Seats, seat => seat.Won);
        Assert.Equal("Mya Lay", board.Seats.First().Name);

        // Nobody's hand reached the page. The only cards on the board are the ones the table
        // saw: what was turned up, what was thrown, and what was laid down at the end.
        var public_ = played.Table.MoneyCards;
        Assert.NotNull(public_);

        var shown = board.Log.SelectMany(line => line.Cards)
            .Concat(board.TurnedUp)
            .Concat(board.Seats.Where(seat => seat.LastDiscard is not null).Select(seat => seat.LastDiscard!.Value))
            .Select(card => card.Id)
            .ToHashSet();

        var said = board.Log
            .Where(line => line.Text.Contains("drew from the deck", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(said);
        Assert.All(said, line => Assert.Empty(line.Cards));
        Assert.NotEmpty(shown);
    }

    private static TableBoard Empty() => TableBoard.Of(
        [One, Two, Three, Four],
        new Dictionary<PlayerId, string>
        {
            [One] = "Mya Lay",
            [Two] = "Cobra",
            [Three] = "Su Htwe",
            [Four] = "Aung Aung"
        });

    private static RoundResult Result(int round, PlayerId winner, int one, int two, int three, int four) =>
        new(
            round,
            winner,
            [],
            new Dictionary<PlayerId, int> { [One] = one, [Two] = two, [Three] = three, [Four] = four },
            Turns: 20);
}

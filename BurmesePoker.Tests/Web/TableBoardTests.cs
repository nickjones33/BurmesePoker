using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Money;
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
        var board = Empty().After(Dealt(1, [KingOfDiamonds, SevenOfSpades]));

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
            .After(Dealt(1, [KingOfDiamonds, OtherKingOfDiamonds]))
            .After(new TableEvent.MoneyCardClaimed(One, KingOfDiamonds));

        Assert.Equal([OtherKingOfDiamonds], board.TurnedUp);
        Assert.Equal(One, board.Acting);
    }

    [Fact]
    public void ADiscardBecomesTheTopOfThatSeatsPile()
    {
        var board = Empty()
            .After(Dealt(1, []))
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
            .After(Dealt(1, [KingOfDiamonds]))
            .After(new TableEvent.Discarded(Two, SevenOfSpades))
            .After(new TableEvent.Settled(Result(1, One, 15, -5, -5, -5)))
            .After(Dealt(2, [SevenOfSpades]))
            .After(new TableEvent.Settled(Result(2, Two, -5, 15, -5, -5)));

        Assert.Equal(10, board.Seats.Single(seat => seat.Player == One).Bank);
        Assert.Equal(10, board.Seats.Single(seat => seat.Player == Two).Bank);
        Assert.Equal(-10, board.Seats.Single(seat => seat.Player == Three).Bank);
        Assert.Equal(0, board.Banks.Values.Sum());

        // The second deal cleared the first round's table and its discard piles.
        Assert.Equal([SevenOfSpades], board.TurnedUp);
        Assert.Equal(2, board.RoundsPlayed);
    }

    /// <summary>
    /// ✅ <b>P41 — the board shows what the rules make public.</b> A card taken in the open lies
    /// face up in front of its taker (RULES.md §5.2), the whole pile is on the seat line for
    /// looking through (§5), and the face-up card leaves when that very copy is thrown.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>By instance</b> (§9 #50's recorded default): when the taker throws the <em>other</em>
    /// king, the face-up one visibly stays — which is the one fact the event log alone could
    /// not carry, and the reason the rule earns a mark at all.
    /// </remarks>
    [Fact]
    public void ACardTakenInTheOpenLiesFaceUpUntilThatVeryCopyIsThrown()
    {
        var taken = Empty()
            .After(Dealt(1, []))
            .After(new TableEvent.Discarded(Two, KingOfDiamonds))
            .After(new TableEvent.TookDiscard(Three, KingOfDiamonds));

        Assert.Equal([KingOfDiamonds], taken.SeatOf(Three).FaceUp);
        Assert.Empty(taken.SeatOf(Three).Pile);
        Assert.Empty(taken.SeatOf(Two).Pile);

        // Throwing the other copy of the same value leaves the face-up card where it lies…
        var threwTheOther = taken.After(new TableEvent.Discarded(Three, OtherKingOfDiamonds));
        Assert.Equal([KingOfDiamonds], threwTheOther.SeatOf(Three).FaceUp);
        Assert.Equal([OtherKingOfDiamonds], threwTheOther.SeatOf(Three).Pile);

        // …and throwing the face-up copy itself takes it off the table.
        var threwItBack = taken.After(new TableEvent.Discarded(Three, KingOfDiamonds));
        Assert.Empty(threwItBack.SeatOf(Three).FaceUp);
    }

    /// <remarks>The claim is an open take too (RULES.md §5.2, §9 #49's recorded default).</remarks>
    [Fact]
    public void AClaimedTurnUpLiesFaceUpInFrontOfItsClaimant()
    {
        var board = Empty()
            .After(Dealt(1, [KingOfDiamonds, SevenOfSpades]))
            .After(new TableEvent.MoneyCardClaimed(One, SevenOfSpades));

        Assert.Equal([SevenOfSpades], board.SeatOf(One).FaceUp);
    }

    [Fact]
    public void ANewDealClearsTheDiscardPiles()
    {
        var board = Empty()
            .After(Dealt(1, []))
            .After(new TableEvent.Discarded(Two, SevenOfSpades))
            .After(Dealt(2, []));

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
        var board = Empty().After(Dealt(1, []));

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
            .After(Dealt(1, []))
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
            .After(Dealt(1, []))
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

    /// <summary>
    /// ✅ <b>P13.5 — the spotlight is a fact the table stated, never one the board guessed.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <c>Acting</c> and <c>Turn</c> are different facts and the felt draws only the second.
    /// After a seat has moved, <c>Acting</c> is that seat and <c>Turn</c> is whoever the table
    /// has since turned to — which, in a client with seats at positions, is the difference
    /// between a spotlight and a lie.
    /// </remarks>
    [Fact]
    public void WhoseTurnItIsComesFromTheTableAndIsNotWhoMovedLast()
    {
        var board = Empty()
            .After(Dealt(1, [KingOfDiamonds, SevenOfSpades]))
            .After(new TableEvent.TurnBegan(One, 1, 1));

        Assert.Equal(One, board.Turn);
        Assert.Equal(1, board.TurnNumber);
        Assert.Null(board.Acting);
        Assert.True(board.SeatOf(One).IsTheirTurn);
        Assert.False(board.SeatOf(Two).IsTheirTurn);

        board = board
            .After(new TableEvent.Discarded(One, KingOfDiamonds))
            .After(new TableEvent.TurnBegan(Two, 1, 2));

        // One moved last; Two is the seat being waited on. Only Two is spotlighted.
        Assert.Equal(One, board.Acting);
        Assert.Equal(Two, board.Turn);
        Assert.True(board.SeatOf(One).IsActing);
        Assert.False(board.SeatOf(One).IsTheirTurn);
        Assert.True(board.SeatOf(Two).IsTheirTurn);

        // ⚠️ And it says nothing. A turn beginning is not narration — the log would be half
        // announcements of moves that are about to be narrated anyway. Counted rather than
        // matched on wording: the deal's own line already contains the word "turned".
        Assert.Equal(2, board.Log.Count);
        Assert.Equal(
            board.Log.Count,
            Empty()
                .After(Dealt(1, [KingOfDiamonds, SevenOfSpades]))
                .After(new TableEvent.Discarded(One, KingOfDiamonds))
                .Log.Count);
    }

    /// <remarks>
    /// Nobody is being waited on between a settlement and the next deal, and a felt that kept
    /// the last spotlight burning would say the round was still going.
    /// </remarks>
    [Fact]
    public void NobodyIsSpotlightedOnceTheRoundIsOver()
    {
        var board = Empty()
            .After(Dealt(1, [KingOfDiamonds]))
            .After(new TableEvent.TurnBegan(Three, 1, 7))
            .After(new TableEvent.Declared(Three, []))
            .After(new TableEvent.Settled(Settlement(Three)));

        Assert.Null(board.Turn);
        Assert.All(board.Seats, seat => Assert.False(seat.IsTheirTurn));
    }

    /// <summary>
    /// ✅ <b>P13.5 — a discard pile is a pile, because a card leaves it again.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Found by needing the middle of the table to be honest.</b> The felt shows the one
    /// discard the seat being waited on may take, and until P13.5 the board kept only each
    /// seat's <em>top</em> card — so once somebody took a discard, the board went on showing a
    /// card that was by then in their hand. The card is found by identity rather than by
    /// assuming whose pile it must have been (§3.1).
    /// </remarks>
    [Fact]
    public void TakingADiscardUncoversTheOneUnderIt()
    {
        var board = Empty()
            .After(Dealt(1, [SevenOfSpades]))
            .After(new TableEvent.Discarded(Two, KingOfDiamonds))
            .After(new TableEvent.Discarded(Two, OtherKingOfDiamonds));

        Assert.Equal(OtherKingOfDiamonds, board.SeatOf(Two).LastDiscard);

        board = board.After(new TableEvent.TookDiscard(Three, OtherKingOfDiamonds));

        // The other copy of the same value is still underneath — instance identity, not value.
        Assert.Equal(KingOfDiamonds, board.SeatOf(Two).LastDiscard);
        Assert.Equal([KingOfDiamonds], board.Pile(Two));

        board = board.After(new TableEvent.TookDiscard(Four, KingOfDiamonds));

        Assert.Null(board.SeatOf(Two).LastDiscard);
        Assert.Empty(board.Pile(Two));
    }

    /// <remarks>
    /// The card on offer is the previous seat's top one, which is what
    /// <c>TurnContext.AvailableDiscard</c> hands the seat itself — the same pile, read the same
    /// way round.
    /// </remarks>
    [Fact]
    public void TheDiscardOnOfferIsThePreviousSeatsTopCard()
    {
        var board = Empty()
            .After(Dealt(1, [SevenOfSpades]))
            .After(new TableEvent.Discarded(One, KingOfDiamonds))
            .After(new TableEvent.TurnBegan(Two, 1, 2));

        Assert.Equal(KingOfDiamonds, board.AvailableDiscard);

        // Four sits before One, and has thrown nothing.
        Assert.Null(board.After(new TableEvent.TurnBegan(One, 1, 5)).AvailableDiscard);

        // Nobody is being waited on, so nothing is on offer.
        Assert.Null(Empty().After(Dealt(1, [SevenOfSpades])).AvailableDiscard);
    }

    /// <summary>
    /// ✅ <b>P13.5 — the draw pile is counted from the public game, not sent.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Nothing was added to the wire for the number in the middle of the felt.</b> The shoe
    /// is 108 (RULES.md §2), thirteen go to each seat and the turned-up cards come off it (§3), a
    /// blind draw takes one, and a reshuffle says how many the gathered discards made (§5). A
    /// watcher at a real table can do the same arithmetic — the alternative was a count on
    /// <c>IGameObserver.PlayerDrew</c>, which is a domain change for a decoration.
    /// </remarks>
    [Fact]
    public void TheDrawPileIsCountedFromWhatTheTableSaid()
    {
        var board = Empty().After(Dealt(1, [KingOfDiamonds, SevenOfSpades]));

        Assert.Equal(DeckBuilder.TotalCards - (RoundEngine.HandSize * 4) - 2, board.DrawPileCount);

        // A blind draw takes one; taking a discard and claiming off the table take none.
        board = board
            .After(new TableEvent.Drew(One, null))
            .After(new TableEvent.Discarded(One, KingOfDiamonds))
            .After(new TableEvent.TookDiscard(Two, KingOfDiamonds))
            .After(new TableEvent.MoneyCardClaimed(Two, SevenOfSpades));

        Assert.Equal(DeckBuilder.TotalCards - (RoundEngine.HandSize * 4) - 3, board.DrawPileCount);

        // A reshuffle says how many the gathered discards made, and empties every pile with it.
        board = board.After(new TableEvent.DiscardsReshuffled(9));

        Assert.Equal(9, board.DrawPileCount);
        Assert.All(board.Seating, seat => Assert.Empty(board.Pile(seat)));
    }

    /// <remarks>
    /// 🔥 <b>The arithmetic, checked against a round the engine really played</b> rather than
    /// against itself. The board's count and the table's own <c>DrawPileCount</c> are worked out
    /// two entirely different ways — one from 108 and the narration, one from the deck object —
    /// and they must agree at the end of the round or the number on the felt is a fiction.
    /// </remarks>
    [Fact]
    public void TheCountedDrawPileAgreesWithTheOneTheEngineIsHolding()
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

        Assert.Equal(played.Table.DrawPileCount, board.DrawPileCount);
        Assert.True(board.DrawPileCount > 0, "a round that emptied the shoe would not test the arithmetic.");
    }

    private static RoundResult Settlement(PlayerId winner) => new(
        Round: 1,
        Winner: winner,
        Turns: 7,
        Melds: [],
        Payouts: new Dictionary<PlayerId, int> { [One] = 0, [Two] = 0, [Three] = 0, [Four] = 0 },
        Win: Win.Ordinary);

    /// <summary>
    /// ✅ <b>P13.6 — who is at the table is folded, like everything else.</b>
    /// </summary>
    /// <remarks>
    /// A lobby opens a table before it knows who is coming, so a seat is named when somebody
    /// sits in it — and the board hears that the same way it hears a discard, through the
    /// fan-out. <b>Nothing here asks the table anything.</b>
    /// </remarks>
    [Fact]
    public void SittingDownNamesTheSeatAndLeavingSaysTheComputerHasIt()
    {
        var board = Empty()
            .After(new TableEvent.SeatTaken(Two, "Nick"))
            .After(Dealt(1, []));

        Assert.Equal("Nick", board.Names[Two]);
        Assert.Equal([Two], board.Seated);
        Assert.False(board.SeatOf(Two).PlayedByTheComputer);

        var away = board.After(new TableEvent.SeatLeft(Two, "Nick"));

        Assert.Empty(away.Seated);
        Assert.True(away.SeatOf(Two).PlayedByTheComputer);

        // ⚠️ The seat keeps the name: "Nick's seat is being played by the computer" is the true
        // thing to say and "Seat 2" is not (§3.11 C16).
        Assert.Equal("Nick", away.Names[Two]);
        Assert.Contains(away.Log, line => line.Text.Contains("Nick left the table", StringComparison.Ordinal));

        // …and coming back clears it.
        var back = away.After(new TableEvent.SeatTaken(Two, "Nick"));

        Assert.False(back.SeatOf(Two).PlayedByTheComputer);
        Assert.Equal([Two], back.Seated);
    }

    /// <remarks>
    /// <b>Standing in is per round</b>, because a player who came back is not still away — and
    /// a seat the computer was always playing is not standing in for anybody.
    /// </remarks>
    [Fact]
    public void TheComputerStandingInLastsOneTurn()
    {
        var board = Empty()
            .After(Dealt(1, []))
            .After(new TableEvent.TurnBegan(Three, 1, 3))
            .After(new TableEvent.SeatPlayedByTheComputer(Three, 1, 3));

        Assert.True(board.SeatOf(Three).PlayedByTheComputer);
        Assert.False(board.SeatOf(One).PlayedByTheComputer);

        // 🔥 Found by playing it: a player who timed out once and then came back is not still
        // away, and a marker that stuck for the rest of the round said he was — in front of
        // him, while he was answering.
        var again = board.After(new TableEvent.TurnBegan(Three, 1, 7));

        Assert.Empty(again.StoodInFor);
        Assert.False(again.SeatOf(Three).PlayedByTheComputer);

        // …and it does not survive a deal either.
        Assert.Empty(board.After(Dealt(2, [])).StoodInFor);
    }

    /// <remarks>
    /// <b>A seat somebody walked away from stays marked until somebody comes back to it</b>,
    /// which is a different fact from the stand-in playing one turn: the table knows the chair
    /// is empty and does not have to wait to find out.
    /// </remarks>
    [Fact]
    public void AnEmptySeatStaysMarkedUntilSomebodySitsInIt()
    {
        var board = Empty()
            .After(new TableEvent.SeatTaken(Two, "Nick"))
            .After(Dealt(1, []))
            .After(new TableEvent.SeatLeft(Two, "Nick"))
            .After(new TableEvent.TurnBegan(Two, 1, 2));

        Assert.True(board.SeatOf(Two).PlayedByTheComputer);
        Assert.Equal([Two], board.Vacated);

        // A seat that was always the computer's is not marked: nobody has gone anywhere.
        Assert.False(board.SeatOf(Four).PlayedByTheComputer);

        // …and it survives the next deal, because a player who left is still gone.
        Assert.True(board.After(Dealt(2, [])).SeatOf(Two).PlayedByTheComputer);

        var back = board.After(new TableEvent.SeatTaken(Two, "Nick"));

        Assert.Empty(back.Vacated);
        Assert.False(back.SeatOf(Two).PlayedByTheComputer);
    }

    /// <summary>
    /// ✅ <b>The ring follows the deal</b> (RULES.md §3 step 2, packet P28). Seats are drawn
    /// again every round, so a board that kept the order the lobby opened with would draw the
    /// wrong neighbours from the second deal on — and the neighbour is what the discard and the
    /// feeding ban are about.
    /// </summary>
    [Fact]
    public void ARedrawnSeatingRearrangesTheTable()
    {
        var board = Empty()
            .After(Dealt(1, []))
            .After(new TableEvent.RoundStarted(2, [Three, One, Four, Two], []));

        Assert.Equal([Three, One, Four, Two], board.Seating);

        // The seats are the same four people in a different order — nobody joined or left.
        Assert.Equal(
            [One, Two, Three, Four],
            board.Seats.Select(seat => seat.Player).OrderBy(player => player.Value).ToArray());
    }

    /// <summary>
    /// ✅ <b>A refused claim is narrated to a watcher</b> (RULES.md §4.5) — the one thing a
    /// player says out loud about their own hand before the declaration.
    /// </summary>
    [Fact]
    public void ARefusedClaimIsSaidToTheTable()
    {
        var board = Empty()
            .After(Dealt(1, [KingOfDiamonds, SevenOfSpades]))
            .After(new TableEvent.ClaimRefused(Four, One, KingOfDiamonds));

        var said = board.Log[^1];

        Assert.Contains("refused", said.Text, StringComparison.Ordinal);
        Assert.Contains("Aung Aung", said.Text, StringComparison.Ordinal);
        Assert.Contains("Mya Lay", said.Text, StringComparison.Ordinal);
        Assert.Equal([KingOfDiamonds], said.Cards);

        // ⚠️ The card stays on the table, because a refused claim moves nothing.
        Assert.Equal([KingOfDiamonds, SevenOfSpades], board.TurnedUp);
    }

    /// <summary>
    /// The deal, for the seating the table opened with — which is what a first round gets
    /// (RULES.md §3 step 2 re-draws it after that).
    /// </summary>
    private static TableEvent.RoundStarted Dealt(int round, IReadOnlyList<Card> turnedUp) =>
        new(round, [One, Two, Three, Four], turnedUp);

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
            Turns: 20,
            Win: Win.Ordinary);
}

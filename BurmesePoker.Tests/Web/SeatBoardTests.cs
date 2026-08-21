using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;
using BurmesePoker.Tests.Server;
using BurmesePoker.Web;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// A match played from a seat, exactly as the browser plays one (packet P13.4).
/// </summary>
/// <remarks>
/// Four seats, all of them people, all of them driven by <see cref="ClickingPlayer"/> — which
/// presses only what a page can press and sees only what a page can see. Three rounds, so the
/// banks have to carry over and the hands have to be dealt again.
/// </remarks>
public sealed class PlayedMatch : IDisposable
{
    public const int Rounds = 3;

    public PlayedMatch()
    {
        Table = TableSession.Open(
            [
                TableSeat.Person(new PlayerId(1), "Nick"),
                TableSeat.Person(new PlayerId(2), "Mya Lay"),
                TableSeat.Person(new PlayerId(3), "Cobra"),
                TableSeat.Person(new PlayerId(4), "Su Htwe")
            ],
            TableSessionTests.Options(20260819));

        Watcher = Table.Watch();
        Seats = Table.Players.ToDictionary(player => player, player => new SeatBoard(Table.ConnectionFor(player)));
        Players = Seats.ToDictionary(entry => entry.Key, entry => new ClickingPlayer(entry.Value));

        for (var round = 0; round < Rounds; round++)
        {
            Played.Add(Table.PlayRound());
        }

        Board = Watcher.Events.Aggregate(
            TableBoard.Of(Table.Players, Table.Names),
            (folded, moment) => folded.After(moment));
    }

    public TableSession Table { get; }

    public SeatConnection Watcher { get; }

    public IReadOnlyDictionary<PlayerId, SeatBoard> Seats { get; }

    public IReadOnlyDictionary<PlayerId, ClickingPlayer> Players { get; }

    public List<RoundRecord> Played { get; } = [];

    public TableBoard Board { get; }

    public void Dispose()
    {
        foreach (var seat in Seats.Values)
        {
            seat.Dispose();
        }
    }
}

/// <summary>
/// ✅ <b>P13.4 acceptance 1 and 3</b> — a match played from a seat, and nothing on the page that
/// belongs to somebody else.
/// </summary>
[Collection(WallClockBudgets.Collection)]
public class SeatBoardTests(PlayedMatch match) : IClassFixture<PlayedMatch>
{
    /// <summary>
    /// ✅ <b>Acceptance 1 — a person plays a full match, several rounds, banks carrying over.</b>
    /// </summary>
    /// <remarks>
    /// It is a person in the only sense that matters here: every answer came back through a
    /// <see cref="SeatBoard"/> from something pressing controls, and no seat has an agent behind
    /// it at all. If the stand-in had played a single question the count below would be short.
    /// </remarks>
    [Fact]
    public void AMatchIsPlayedFromTheSeatsThemselves()
    {
        Assert.Equal(PlayedMatch.Rounds, match.Played.Count);
        Assert.Equal(PlayedMatch.Rounds, match.Board.RoundsPlayed);

        foreach (var (player, seat) in match.Seats)
        {
            Assert.Equal(match.Players[player].Asked.Count, seat.Answered);
            Assert.NotEmpty(match.Players[player].Asked);
        }

        // Banks carry over and nothing resets them (RULES.md §7.2). The board is the watcher's
        // fold, and it agrees with what the engine settled.
        var banked = match.Played
            .SelectMany(round => round.Result.Payouts)
            .GroupBy(payout => payout.Key)
            .ToDictionary(seat => seat.Key, seat => seat.Sum(payout => payout.Value));

        Assert.Equal(banked, match.Board.Banks);
        Assert.Equal(0, match.Board.Banks.Values.Sum());
    }

    /// <summary>
    /// ✅ <b>Acceptance 3 — the page does not widen the concealment.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Asserted over what the seat <em>drew</em>, not over what the fan-out sent.</b>
    /// <c>ConcealmentTests</c> guards the wire; this guards the thing that renders — every hand
    /// that was ever on any of these four pages, sharing no <c>CardId</c> that the table did not
    /// hand round. A component that found a second route to the table would show up here as an
    /// overlap.
    /// <para>
    /// ⚠️ <b>The allowance for what the table handed round is P21's, and it is not a relaxation
    /// of the rule</b> — it is the rule stated correctly. A round long enough to exhaust the
    /// draw pile shuffles the discards back into it (RULES.md §5), so a discarded card reaching
    /// a second hand is the game working. <see cref="PublicRelease"/> is where that is argued.
    /// </para>
    /// </remarks>
    [Fact]
    public void NoSeatEverHeldACardFromAnotherSeatsHandExceptByWayOfTheTable()
    {
        var released = PublicRelease.PerRound(match.Watcher.Events);
        var rounds = 0;

        for (var round = 1; round <= PlayedMatch.Rounds; round++)
        {
            // ⚠️ A round at a time. A CardId names a card in that round's shoe and the shoe is
            // rebuilt for every deal, so the same id is a different physical card next round —
            // comparing a whole match at once compares the eight of hearts with itself and
            // finds a leak that is not there.
            var at = round;

            var held = match.Players.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.Hands
                    .Where(hand => hand.Round == at)
                    .SelectMany(hand => hand.Hand.Hand)
                    .Select(card => card.Id)
                    .ToHashSet());

            Assert.All(held.Values, cards => Assert.NotEmpty(cards));
            rounds++;

            var public_ = PublicRelease.In(released, at);

            foreach (var mine in held)
            {
                foreach (var theirs in held.Where(other => other.Key != mine.Key))
                {
                    Assert.Empty(mine.Value.Intersect(theirs.Value).Except(public_));
                }
            }
        }

        Assert.Equal(PlayedMatch.Rounds, rounds);
    }

    /// <remarks>
    /// The other half of it: every card in every question a seat was asked is one that seat may
    /// see. A prompt carries the hand, the discard offered, the turned-up cards and the card
    /// just taken — all of them public or its own.
    /// </remarks>
    [Fact]
    public void EveryQuestionIsAskedAboutTheSeatsOwnHand()
    {
        foreach (var (player, driver) in match.Players)
        {
            Assert.All(driver.Asked, asking => Assert.Equal(player, asking.Player));
            Assert.All(driver.Asked, asking => Assert.NotEmpty(asking.Hand.Hand));
            Assert.All(
                driver.Asked,
                asking => Assert.All(
                    asking.Hand.Cards,
                    card => Assert.True(asking.Holds(card.Card), "a view of a card the seat is not holding.")));
        }
    }

    /// <summary>
    /// 🔥 <b>The hand stays on screen between turns, and it is right rather than stale.</b>
    /// </summary>
    /// <remarks>
    /// After you throw a card, your thirteen are fixed until your next turn: nobody draws from
    /// your hand, and what another player takes comes off your discard pile. So the resting hand
    /// is the prompt's hand minus the card thrown, with the near-melds worked out again over
    /// what is left — and never the card that has gone.
    /// </remarks>
    [Fact]
    public void TheHandLeftOnScreenIsTheOneYouKept()
    {
        var throws = match.Players.Values.SelectMany(driver => driver.Threw).ToList();

        Assert.NotEmpty(throws);

        foreach (var (thrown, kept) in throws)
        {
            Assert.DoesNotContain(kept.Hand, card => card.Id == thrown.Id);
            Assert.Equal(RoundEngine.HandSize, kept.Count);
        }
    }

    /// <remarks>
    /// <para>
    /// All five of <c>IPlayerAgent</c>'s questions reach a seat over a match, which is what makes
    /// the five branches of <c>TurnPrompt</c> worth having. The claim is asked only of whoever
    /// opens a round (RULES.md §4.5) and the declaration only of a seat that can go out (§7.1),
    /// so both need a match rather than a round to turn up.
    /// </para>
    /// <para>
    /// 🔥 <b>The fifth needs the fourth to be answered yes</b> (P28): the permission is put to the
    /// seat above only when somebody actually wants the card, and only when that seat is holding
    /// the rank. <c>ClickingPlayer</c> therefore claims — a page can press <em>Claim</em> as
    /// easily as <em>Leave it</em>, and declining every time would have left a whole branch of
    /// the prompt unreachable and this assertion quietly weaker than it reads.
    /// </para>
    /// </remarks>
    [Fact]
    public void EverySeatIsAskedEveryQuestionOverAMatch()
    {
        var asked = match.Players.Values
            .SelectMany(driver => driver.Asked)
            .Select(question => question.Question)
            .ToHashSet();

        Assert.Equal(Enum.GetValues<SeatQuestion>().ToHashSet(), asked);
    }

    /// <remarks>
    /// The prompt itself is fourteen cards — the one you took and the thirteen you had — which
    /// is what makes the resting hand thirteen.
    /// </remarks>
    [Fact]
    public void ADiscardIsChosenFromFourteen()
    {
        var discards = match.Players.Values
            .SelectMany(driver => driver.Asked)
            .Where(asking => asking.Question == SeatQuestion.Discard)
            .ToList();

        Assert.NotEmpty(discards);
        Assert.All(discards, asking => Assert.Equal(RoundEngine.HandSize + 1, asking.Hand.Count));
    }

    /// <remarks>
    /// A hand belongs to a round. Between the settlement and the next deal there is nothing to
    /// draw, and the seat says so rather than leaving the last round's cards lying on the page.
    /// </remarks>
    [Fact]
    public void TheHandIsPutAwayWhenTheRoundSettles()
    {
        Assert.All(match.Seats.Values, seat => Assert.Null(seat.Hand));
        Assert.All(match.Seats.Values, seat => Assert.Null(seat.Asking));
        Assert.All(match.Seats.Values, seat => Assert.False(seat.IsYourTurn));
    }
}

/// <summary>
/// What the seat does with an answer that does not fit, and what it does before a round starts.
/// </summary>
/// <remarks>
/// ⚠️ <b>A client bug must not end somebody's round</b> (P13.2). Refusal is the seat's whole
/// error handling, and it is worth its own tests because it is the one path a half-built
/// component takes.
/// </remarks>
public class SeatBoardRefusalTests
{
    [Fact]
    public void ASeatWithNoQuestionAnswersNothing()
    {
        using var seat = Fresh(out _);

        Assert.Null(seat.Asking);
        Assert.Null(seat.Hand);
        Assert.False(seat.IsYourTurn);
        Assert.False(seat.Declare(true));
        Assert.Equal(0, seat.Answered);
    }

    /// <remarks>
    /// The question standing is the first one of the round — how to take a card — and a claim is
    /// not an answer to it. The seat says no and the question is still there.
    /// </remarks>
    [Fact]
    public void AnAnswerThatDoesNotFitIsRefusedAndTheQuestionStands()
    {
        using var seat = Fresh(out var table);

        var refused = 0;
        var answered = 0;

        seat.Changed += () =>
        {
            if (seat.Asking is not { } asking)
            {
                return;
            }

            if (asking.Question != SeatQuestion.Take)
            {
                answered += seat.Answer(ScriptedSeat.Reply(asking)) ? 1 : 0;
                return;
            }

            // Wrong shape, then a card it is not holding, then the real answer.
            if (!seat.Claim(true))
            {
                refused++;
            }

            Assert.NotNull(seat.Asking);
            Assert.Equal(SeatQuestion.Take, seat.Asking!.Question);

            answered += seat.Take(TurnAction.DrawFromDeck) ? 1 : 0;
        };

        table.PlayRound();

        Assert.True(refused > 0, "the round never asked this seat how to take a card.");
        Assert.Equal(answered, seat.Answered);
    }

    /// <remarks>
    /// A watcher holds no seat and is never asked anything, so it is not a thing a page can be
    /// built on — the type refuses it rather than rendering an empty hand for ever.
    /// </remarks>
    [Fact]
    public void AWatcherIsNotASeat()
    {
        var table = Table();

        Assert.Throws<ArgumentException>(() => new SeatBoard(table.Watch()));
    }

    private static SeatBoard Fresh(out TableSession table)
    {
        table = Table();
        return new SeatBoard(table.ConnectionFor(new PlayerId(1)));
    }

    private static TableSession Table() => TableSession.Open(
        [
            TableSeat.Person(new PlayerId(1), "Nick"),
            TableSeat.Computer(new PlayerId(2), "Mya Lay (bot)", new GreedyBotAgent()),
            TableSeat.Computer(new PlayerId(3), "Cobra (bot)", new GreedyBotAgent()),
            TableSeat.Computer(new PlayerId(4), "Su Htwe (bot)", new GreedyBotAgent())
        ],
        TableSessionTests.Options(20260819));
}

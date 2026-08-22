using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;
using BurmesePoker.Tests.Server;
using BurmesePoker.Web;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// ✅ <b>P30.2 acceptance 4: the browser plays a round to a declaration, with concealment
/// asserted throughout and the settlement drawn matching the engine's.</b>
/// </summary>
/// <remarks>
/// <para>
/// The seats are played through <see cref="SeatBoard"/> by <see cref="ClickingPlayer"/> — the
/// thing the page holds, so every card asserted over is a card a real page would have drawn
/// (P13.4, TwoPeopleTests). What this adds over TwoPeopleTests is the strict
/// <c>ConcealmentTests</c> form applied at the boards over a whole declared round, and the
/// settlement closed in a loop: <b>the board that drew it, the engine that computed it, and
/// the journal that replays it all agree</b> (P24.1's plumbing doing the job it was built for).
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class BrowserRoundTests
{
    private static readonly PlayerId One = new(1);
    private static readonly PlayerId Two = new(2);
    private static readonly PlayerId Three = new(3);
    private static readonly PlayerId Four = new(4);

    [Fact]
    public void ARoundPlayedAtTheBoardsIsDeclaredConcealedAndSettledAsTheEngineSays()
    {
        var table = TableSession.Open(
            [
                TableSeat.Person(One, "Nick"),
                TableSeat.Computer(Two, "Ruby (bot)", new GreedyBotAgent()),
                TableSeat.Person(Three, "Mya Lay"),
                TableSeat.Computer(Four, "Onyx (bot)", new GreedyBotAgent())
            ],
            new TableOptions
            {
                Seed = 20260819,
                RoundTimeLimit = TimeSpan.FromMinutes(2),
                // Kept in memory and read back below; nothing here writes a file (P24.1).
                Journal = "kept"
            });

        var watcher = table.Watch();
        var nickBoard = new SeatBoard(table.SitDown(One, "Nick")!);
        var myaBoard = new SeatBoard(table.SitDown(Three, "Mya Lay")!);
        var nick = new ClickingPlayer(nickBoard);
        var mya = new ClickingPlayer(myaBoard);

        var played = table.PlayRound();

        // 1. The round reached a declaration through the boards: both people were asked their
        //    own questions and answered them, and somebody won.
        Assert.Contains(played.Result.Winner, table.Players);
        Assert.NotEmpty(nick.Asked);
        Assert.NotEmpty(mya.Asked);
        Assert.All(nick.Asked, prompt => Assert.Equal(One, prompt.Player));
        Assert.All(mya.Asked, prompt => Assert.Equal(Three, prompt.Player));
        Assert.Single(watcher.Events.OfType<TableEvent.Declared>());

        // 2. Concealment, the strict form, at the boards: any card both pages showed passed
        //    through the table on the way (RULES.md §5, §6.3; PublicRelease carries the
        //    argument), and nothing entered the allowance without being named in a public event.
        var released = PublicRelease.In(PublicRelease.PerRound(watcher.Events), round: 1);
        var nicksScreen = Shown(nick);
        var myasScreen = Shown(mya);

        Assert.NotEmpty(nicksScreen);
        Assert.NotEmpty(myasScreen);
        Assert.Empty(nicksScreen.Intersect(myasScreen).Except(released));

        // 3. The settlement drawn is the engine's. The public board is folded from exactly the
        //    events a page folds, and its payouts are the result's, to the dollar.
        var board = watcher.Events.Aggregate(
            TableBoard.Of(table.Players, table.Names), (folded, moment) => folded.After(moment));

        Assert.NotNull(board.Settlement);
        Assert.Equal(played.Result.Winner, board.Settlement!.Winner);
        Assert.All(table.Players, player => Assert.Equal(
            played.Result.Payouts[player], board.PayoutOf(player)));

        // 4. And the journal closes the loop: the round the boards played replays to the same
        //    settlement through the ordinary replay path (P24.1).
        var journal = table.Journal()!;
        var replay = new MatchEngine(
            journal.Header.Players,
            JournalPlayerAgent.SeatsOf(journal),
            journal.Header.Stakes,
            new Random(journal.Header.Seed));

        Assert.Equal(played.Result.Payouts, replay.PlayRound().Result.Payouts);
    }

    /// <summary>
    /// 🔥 <b>The browser half of R8: a seat handed over mid-table shows its old occupant
    /// nothing more, and the new occupant plays it.</b> Server-side the superseded connection
    /// is dead (<c>TableSessionTests</c>); what belongs to the Web layer is that the handover
    /// arrives through the ordinary lobby flow and the new board is the one that gets asked.
    /// </summary>
    [Fact]
    public async Task AHandedOverSeatShowsItsOldOccupantNothingMore()
    {
        await using var table = HostedTableTests.Open(seats: 4, people: 2, betweenSeconds: 60);

        var ghost = table.SitDown("Nick");
        Assert.NotNull(ghost);
        table.StandUp(ghost!);

        var replacement = table.SitDown("Somebody else");
        var mya = table.SitDown("Mya Lay");

        Assert.NotNull(replacement);
        Assert.NotNull(mya);
        Assert.Equal(ghost!.Player, replacement!.Player);

        var one = new ClickingPlayer(replacement);
        var two = new ClickingPlayer(mya!);

        table.Arrive();

        var settled = await Settles(table);

        table.StandUp(replacement);
        table.StandUp(mya!);
        table.Leave();
        await Task.Delay(50, TestContext.Current.CancellationToken);

        // The round was really played, by the replacement — while the ghost's board stayed
        // exactly as it was stood up: no question, no hand, nothing to press.
        Assert.NotNull(settled.Settlement);
        Assert.NotEmpty(one.Asked);
        Assert.Null(ghost.Asking);
        Assert.Null(ghost.Hand);
        Assert.False(ghost.Answer(new SeatAnswer.Take(TurnAction.DrawFromDeck)));
    }

    /// <summary>Every card either page put on screen for this person in round 1.</summary>
    private static HashSet<CardId> Shown(ClickingPlayer player) =>
    [
        .. player.Hands.Where(hand => hand.Round is 0 or 1).SelectMany(hand => hand.Hand.Hand).Select(card => card.Id),
        .. player.Asked.SelectMany(prompt => prompt.Hand.Hand).Select(card => card.Id)
    ];

    /// <summary>The public board once the table settles a round, polled (§3.6).</summary>
    private static async Task<TableBoard> Settles(HostedTable table)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            if (table.Board is { Settlement: not null } done)
            {
                return done;
            }

            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.Fail("The table never settled a round.");
        return table.Board;
    }
}

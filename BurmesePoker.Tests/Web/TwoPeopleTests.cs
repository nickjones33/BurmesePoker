using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;
using BurmesePoker.Web;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// 🔥 <b>P13's original "done when", and P13.6's acceptance: two people and two bots play a
/// round.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Everything under it was already tested</b> — the fan-out's concealment in P13.2, the
/// public board in P13.3, one seat's own board in P13.4 — so what is left to prove is the thing
/// P13.6 changed: <b>two viewers at one table hold two different seats</b>, are asked their own
/// questions, and see their own hands and nobody else's.
/// </para>
/// <para>
/// ⚠️ <b>There is no socket here and there does not need to be.</b> Blazor Server supplies the
/// transport (§3.10 item 4), so a browser circuit holds the very <see cref="SeatBoard"/> this
/// test holds. What a wire would add is latency, and latency is what <c>BoundedAgent</c> and
/// the stand-in already exist for.
/// </para>
/// <para>
/// ⚠️ <b>It is driven by <see cref="ClickingPlayer"/>, which is the thing the page holds.</b>
/// Every card it is shown is a card the page would draw, so the disjointness asserted below is
/// asserted over what would really have been on two screens.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class TwoPeopleTests
{
    [Fact]
    public async Task TwoPeopleAndTwoBotsPlayARound()
    {
        // ⚠️ A long gap between rounds, and it is not padding. 🔥 A SETTLEMENT IS NOT A RESTING
        // STATE: the board clears it at the next deal, and with a one-millisecond gap this test
        // caught round 17 rather than round 1 — by which time the 240 lines the log keeps had
        // trimmed away the two lines saying who had sat down. A table that stops between rounds
        // is also what a person actually sees.
        await using var table = HostedTableTests.Open(seats: 4, people: 2, seed: 20260819, betweenSeconds: 60);

        var nick = table.SitDown("Nick");
        var mya = table.SitDown("Mya Lay");

        Assert.NotNull(nick);
        Assert.NotNull(mya);
        Assert.NotEqual(nick!.Player, mya!.Player);

        var one = new ClickingPlayer(nick);
        var two = new ClickingPlayer(mya);

        // Somebody has the page open, every seat is accounted for, and the table deals.
        table.Arrive();
        Assert.True(table.IsDealing);

        var settled = await Settles(table);

        // ⚠️ Stop before reading what the two of them wrote down. The table deals on its own
        // thread and a settlement is not the end of anything — the next round is a millisecond
        // away — so a list read while a round is running is a list being appended to.
        // Standing up disposes each seat board, which is what stops the appends: a seat
        // somebody has stood up from is told nothing more.
        table.StandUp(nick);
        table.StandUp(mya);
        table.Leave();
        await Task.Delay(50, TestContext.Current.CancellationToken);

        // 1. The first round was really played, by four seats, and somebody won it.
        Assert.NotNull(settled.Settlement);
        Assert.Equal(4, settled.Seating.Count);
        Assert.True(settled.Settlement!.Turns > 0);
        Assert.Equal(1, settled.Settlement.Round);

        // 2. Both people were asked their own questions, and answered them.
        Assert.NotEmpty(one.Asked);
        Assert.NotEmpty(two.Asked);
        Assert.All(one.Asked, prompt => Assert.Equal(nick.Player, prompt.Player));
        Assert.All(two.Asked, prompt => Assert.Equal(mya.Player, prompt.Player));
        Assert.True(nick.Answered > 0);
        Assert.True(mya.Answered > 0);

        // 3. 🔥 Neither person was shown a card in the other's hand. A CardId names a card in a
        //    round's shoe and the shoe is rebuilt every deal (P13.4), so the comparison is made
        //    a round at a time.
        foreach (var round in one.Hands.Select(hand => hand.Round).Distinct())
        {
            var mine = Held(one, round);
            var theirs = Held(two, round);

            Assert.NotEmpty(mine);

            if (theirs.Count > 0)
            {
                Assert.Empty(mine.Intersect(theirs));
            }
        }

        // 4. The money adds up: a round is a transfer between the seats at it (RULES.md §7.3).
        Assert.Equal(0, settled.Banks.Values.Sum());

        // 5. And the table said who was in it, to everybody.
        Assert.Contains(settled.Log, line => line.Text.Contains("Nick sat down.", StringComparison.Ordinal));
        Assert.Contains(settled.Log, line => line.Text.Contains("Mya Lay sat down.", StringComparison.Ordinal));
    }

    /// <summary>
    /// 🔥 <b>A seat somebody has stood up from answers nothing — with a question standing on
    /// it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>This needs a live round, and the first version of it did not have one.</b> Asserting
    /// that a stood-up board refuses an answer is vacuous unless it was being asked something:
    /// with no round running there is no question to refuse, and the assertion passes however
    /// broken the board is. <b>Found by mutating <c>Dispose</c> and watching the test stay
    /// green.</b>
    /// </para>
    /// <para>
    /// What it protects: a browser refresh is a new circuit and Blazor holds the old one for
    /// minutes, so the ghost of the page you just left is still holding a live
    /// <see cref="SeatConnection"/> — and a ghost that could still press a card would be
    /// playing your hand for you (§3.11 C16).
    /// </para>
    /// </remarks>
    [Fact]
    public async Task AGhostCannotAnswerTheQuestionItWasLookingAt()
    {
        await using var table = HostedTableTests.Open(
            seats: 4, people: 2, seed: 20260819, betweenSeconds: 60, patienceSeconds: 30);

        var ghost = table.SitDown("Nick");
        var mya = table.SitDown("Mya Lay");

        // Mya answers; Nick sits there with the question in front of him, which is the state a
        // person is in when they hit reload.
        _ = new ClickingPlayer(mya!);
        table.Arrive();

        var asked = await Standing(ghost!);

        Assert.NotNull(asked);

        var back = table.SitDown("Nick");

        Assert.Equal(ghost!.Player, back!.Player);

        // The question is still standing — it moved to the new board, not away.
        Assert.NotNull(back.Asking);

        // …and the ghost is inert, both in what it shows and in what it will do.
        Assert.Null(ghost.Asking);
        Assert.False(ghost.Answer(new SeatAnswer.Take(TurnAction.DrawFromDeck)));
        Assert.False(ghost.Declare(true));

        table.StandUp(back);
        table.Leave();
    }

    /// <summary>The question standing in front of a seat, once there is one.</summary>
    private static async Task<SeatPrompt?> Standing(SeatBoard seat)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            if (seat.Asking is { } asking)
            {
                return asking;
            }

            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.Fail("The seat was never asked anything.");
        return null;
    }

    /// <summary>Every card either person held in one round.</summary>
    private static IReadOnlySet<CardId> Held(ClickingPlayer player, int round) =>
        player.Hands
            .Where(hand => hand.Round == round)
            .SelectMany(hand => hand.Hand.Hand.Select(card => card.Id))
            .ToHashSet();

    /// <summary>
    /// The board once a round has been settled.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Polled rather than awaited, because the table deals on its own thread</b> — which
    /// is the whole point of §3.6 and is exactly what a circuit does. The budget is generous:
    /// a round of four seats is milliseconds of search, and this is only failing if it never
    /// happens at all.
    /// </remarks>
    private static async Task<TableBoard> Settles(HostedTable table)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            if (table.Board is { Settlement: not null } settled)
            {
                return settled;
            }

            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.Fail("The table never settled a round.");
        return table.Board;
    }
}

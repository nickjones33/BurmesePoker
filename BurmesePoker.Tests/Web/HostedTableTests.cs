using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;
using BurmesePoker.Web;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// One table the lobby is hosting (packet P13.6): who is in it, whose connection your half of
/// the page is drawn from, and when it deals.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>The seat's connection is your own, never the watcher's.</b> A watcher is sent the
/// public game and no hand at all (P13.2), so a page that drew a hand from one would draw
/// nothing — and a page that drew the public board from a <em>seat</em> would put your blind
/// draws in front of anyone else looking at it. The two are separate here and stay separate.
/// </para>
/// <para>
/// 🔥 <b>And a <see cref="SeatBoard"/> belongs to a viewer.</b> Until this packet there was one
/// per table, built when the table opened and handed to every circuit — right for solo play and
/// wrong the moment two people are here.
/// </para>
/// </remarks>
public class HostedTableTests
{
    [Fact]
    public async Task ASeatIsYoursAndItIsYourOwnConnection()
    {
        await using var table = Open(seats: 4, people: 1);

        var mine = table.SitDown("Nick");

        Assert.NotNull(mine);
        Assert.Equal(new PlayerId(1), mine!.Player);
        Assert.Equal("Nick", mine.Name);

        // The public board is the watcher's fold and knows the seating; your own half knows a
        // hand, and nothing has been dealt yet.
        Assert.Equal(4, table.Board.Seating.Count);
        Assert.Equal("Nick", table.Board.Names[new PlayerId(1)]);
        Assert.Null(mine.Hand);
    }

    /// <summary>
    /// ✅ <b>P18 — a table is opened on a rung of the one catalog, by name.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>An unknown name is fallen back from and never thrown over.</b> A difficulty comes
    /// off a command line or out of a form field, and neither is worth failing to open a table
    /// for; the hardest rung is what a table plays at when nobody has said otherwise.
    /// </remarks>
    [Fact]
    public async Task TheComputerPlaysTheRungTheTableWasOpenedOn()
    {
        await using var easy = Open(seats: 4, people: 0, difficulty: "simple");
        await using var strange = Open(seats: 4, people: 0, difficulty: "thoughtful");
        await using var silent = Open(seats: 4, people: 0);

        Assert.Equal("simple", easy.Difficulty.Name);
        Assert.Equal(BotCatalog.Hardest.Name, strange.Difficulty.Name);
        Assert.Equal(BotCatalog.Hardest.Name, silent.Difficulty.Name);

        // Case is the person's; the name kept is the catalog's own spelling.
        await using var shouted = Open(seats: 4, people: 0, difficulty: "SIMPLE");
        Assert.Equal("simple", shouted.Difficulty.Name);
    }

    /// <remarks>
    /// P13.3's table, still there: a table with no people at it is a room with nobody in it,
    /// which is the strictest case for concealment and the one the leak test was written
    /// against.
    /// </remarks>
    [Fact]
    public async Task NobodyHasToBePlaying()
    {
        await using var table = Open(seats: 4, people: 0);

        Assert.Empty(table.WaitingFor);
        Assert.Null(table.SitDown("Nick"));
        Assert.All(table.Board.Names.Values, name => Assert.EndsWith("(bot)", name, StringComparison.Ordinal));
    }

    [Fact]
    public async Task EverySeatBeyondThePeopleIsTheComputers()
    {
        await using var table = Open(seats: 5, people: 2);

        Assert.Equal([new PlayerId(1), new PlayerId(2)], table.WaitingFor);
        Assert.Equal(3, table.Board.Names.Count(name => name.Value.EndsWith("(bot)", StringComparison.Ordinal)));
    }

    /// <remarks>
    /// 🔥 <b>The bug P13.6 exists to fix, stated as a test.</b> Two circuits handed the same
    /// connection are two people answering one question.
    /// </remarks>
    [Fact]
    public async Task TwoViewersGetTwoDifferentSeats()
    {
        await using var table = Open(seats: 4, people: 2);

        var first = table.SitDown("Nick");
        var second = table.SitDown("Mya Lay");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first!.Player, second!.Player);
        Assert.Empty(table.WaitingFor);

        // …and a third person finds nowhere to sit, rather than sharing.
        Assert.Null(table.SitDown("Somebody else"));

        Assert.Equal("Nick", table.Board.Names[first.Player]);
        Assert.Equal("Mya Lay", table.Board.Names[second.Player]);
        Assert.Contains(first.Player, table.Board.Seated);
        Assert.Contains(second.Player, table.Board.Seated);
        Assert.Equal(2, table.Board.Seated.Count);
    }

    /// <remarks>
    /// ✅ <b>§3.11 C16 — coming back is the ordinary case.</b> A browser refresh is a new
    /// circuit and Blazor holds the old one for minutes, so a player who reloaded would
    /// otherwise find their own seat occupied by their own ghost.
    /// </remarks>
    [Fact]
    public async Task ComingBackUnderTheSameNameTakesYourOwnSeatBack()
    {
        await using var table = Open(seats: 4, people: 2);

        var ghost = table.SitDown("Nick");
        var other = table.SitDown("Mya Lay");
        var back = table.SitDown("nick");

        Assert.NotNull(back);
        Assert.Equal(ghost!.Player, back!.Player);
        Assert.NotSame(ghost, back);
        Assert.Empty(table.WaitingFor);

        // The seat that was never a ghost is untouched.
        Assert.Equal(new PlayerId(2), other!.Player);
    }

    /// <remarks>
    /// A viewer whose seat was taken back by a reconnection must not free the seat the
    /// reconnection is now sitting in — otherwise closing the stale tab throws the player out.
    /// </remarks>
    [Fact]
    public async Task AGhostStandingUpDoesNotEmptyTheSeatItLost()
    {
        await using var table = Open(seats: 4, people: 1);

        var ghost = table.SitDown("Nick");
        var back = table.SitDown("Nick");

        table.StandUp(ghost!);

        Assert.Empty(table.WaitingFor);
        Assert.Equal(back!.Player, Assert.Single(table.Board.Seated));
    }

    [Fact]
    public async Task StandingUpFreesTheSeatAndSaysSo()
    {
        await using var table = Open(seats: 4, people: 1);

        var mine = table.SitDown("Nick");
        table.StandUp(mine!);

        Assert.Equal([new PlayerId(1)], table.WaitingFor);
        Assert.Empty(table.Board.Seated);

        // The seat keeps the name, because "Nick's seat is being played by the computer" is the
        // true thing to say and "Seat 1" is not (§3.11 C16).
        Assert.Equal("Nick", table.Board.Names[new PlayerId(1)]);
        Assert.True(table.Board.SeatOf(new PlayerId(1)).PlayedByTheComputer);
        Assert.Contains(
            table.Board.Log,
            line => line.Text.Contains("Nick left the table", StringComparison.Ordinal));

        // …and coming back clears it, in the seat as well as in the log. ✅ §3.11 C16: a
        // dropped circuit must not look like a dropped game, and coming back must not look
        // like an apology.
        var back = table.SitDown("Nick");

        Assert.Equal(new PlayerId(1), back!.Player);
        Assert.False(table.Board.SeatOf(new PlayerId(1)).PlayedByTheComputer);
    }

    /// <summary>
    /// 🔥 <b>Deal while somebody is at the table — P13.4's finding, finally answerable.</b>
    /// </summary>
    /// <remarks>
    /// Every question an unoccupied seat is asked spends the whole of its patience before the
    /// stand-in plays, so an unattended round is over an hour of nothing. <b>The fix is not a
    /// shorter patience</b>: it is that a lobby knows whether anybody is here.
    /// </remarks>
    [Fact]
    public async Task ItDealsOnlyWhenSomebodyIsHereAndEverySeatIsAccountedFor()
    {
        await using var table = Open(seats: 4, people: 2);

        Assert.False(table.ShouldDeal);
        Assert.False(table.IsDealing);

        // Somebody is looking at it, but two seats are empty.
        table.Arrive();
        Assert.False(table.ShouldDeal);

        var first = table.SitDown("Nick");
        Assert.False(table.ShouldDeal);

        var second = table.SitDown("Mya Lay");
        Assert.True(table.ShouldDeal);
        Assert.True(table.IsDealing);

        // …and one of them walking away stops it again, once the round in flight is over.
        table.StandUp(second!);
        Assert.False(table.ShouldDeal);

        table.StandUp(first!);
        table.Leave();
        Assert.False(table.ShouldDeal);
    }

    /// <remarks>
    /// A table with nobody at it is P13.3's, and it still deals the moment somebody watches —
    /// there is no seat to wait for.
    /// </remarks>
    [Fact]
    public async Task ATableOfBotsDealsAsSoonAsSomebodyWatches()
    {
        await using var table = Open(seats: 4, people: 0);

        Assert.False(table.ShouldDeal);

        table.Arrive();

        Assert.True(table.ShouldDeal);
        Assert.Equal(1, table.Attending);

        table.Leave();

        Assert.False(table.ShouldDeal);
        Assert.Equal(0, table.Attending);
    }

    internal static HostedTable Open(
        int seats,
        int people,
        int seed = 20260819,
        int betweenSeconds = 0,
        int patienceSeconds = 0,
        string? difficulty = null) => new(
        "test",
        new TablePlan
        {
            Title = "A table",
            Seats = seats,
            People = people,
            Seed = seed,
            // ⚠️ Nothing here plays a round, so the pacing only has to be small enough that a
            // table which does start dealing does not hold the test up.
            Pace = TimeSpan.Zero,
            BetweenRounds = betweenSeconds > 0
                ? TimeSpan.FromSeconds(betweenSeconds)
                : TimeSpan.FromMilliseconds(1),
            // ⚠️ Zero patience is how a test makes a takeover deterministic (P13.2); a test
            // that wants a question to *stand* in front of a seat has to give it time to.
            Patience = TimeSpan.FromSeconds(patienceSeconds),
            Difficulty = difficulty ?? BotCatalog.Hardest.Name
        },
        Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
}

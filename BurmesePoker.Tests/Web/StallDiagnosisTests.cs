using System.Collections.Concurrent;
using BurmesePoker.Web;
using Microsoft.Extensions.Logging;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// A hosted table that is not taking turns says why (packet P65).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The gap this closes is an instrument gap, not a bug.</b> P63 watched the deployed table
/// take no turn by any seat for about three minutes, with the circuit open, the seat claimed,
/// <c>/healthz</c> answering and no exception anywhere — and the cause could not be established,
/// because <b>the application logs requests and never turns</b>. From outside the process
/// <em>the engine is parked in <c>Ask</c></em>, <em>the round was abandoned on its time
/// limit</em> and <em>the table stopped dealing</em> are the same silence.
/// </para>
/// <para>
/// ⚠️ <b>Three lines tell them apart</b>, and each is asserted here: a round beginning and a
/// round settling (so a silence between the two is the engine parked), a round given up on, and
/// the dealer stopping with the facts that stopped it.
/// </para>
/// <para>
/// ⚠️ <b>Polled rather than awaited</b>, like everything about a table that deals on its own
/// thread (§3.6, <c>TwoPeopleTests</c>).
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class StallDiagnosisTests
{
    [Fact]
    public async Task ARoundGivenUpOnSaysSoRatherThanGoingQuiet()
    {
        var log = new Recording();

        // A round limit of nothing at all is how a test abandons a round deterministically
        // (TableSessionTests' idiom); the long gap afterwards keeps it to one.
        await using var table = HostedTableTests.Open(
            seats: 4, people: 0, betweenSeconds: 60, roundTimeLimit: TimeSpan.Zero, log: log);

        table.Arrive();

        var said = await log.Says("gave up on round");

        table.Leave();

        Assert.Contains("gave up on round 1", said);
        Assert.Equal(LogLevel.Warning, log.LevelOf(said));
    }

    [Fact]
    public async Task ATableNobodyHasSatDownAtSaysWhyItIsNotDealing()
    {
        var log = new Recording();

        await using var table = HostedTableTests.Open(
            seats: 4, people: 1, betweenSeconds: 60, log: log);

        // Somebody looking at a table whose person-seat nobody has claimed: the exact shape of a
        // table that will never deal, and the one that reads from outside as a stall.
        // ⚠️ It never starts a dealer at all, so the line has to come from the decision rather
        // than from the loop — which is what writing this test found.
        table.Arrive();

        var said = await log.Says("is not dealing");

        table.Leave();

        Assert.Contains("1 watching", said);
        Assert.Contains("1 seat(s) still to be claimed", said);
        Assert.Contains("closed: False", said);
    }

    [Fact]
    public async Task ATableTheLastViewerLeavesSaysWhyItIsNotDealing()
    {
        var log = new Recording();

        // Every seat the computer's, so the only thing holding it up is the viewer.
        await using var table = HostedTableTests.Open(
            seats: 4, people: 0, betweenSeconds: 0, log: log);

        table.Arrive();
        await log.Says("is dealing round 1");

        table.Leave();

        var said = await log.Says("is not dealing");

        Assert.Contains("0 watching", said);
        Assert.Contains("0 seat(s) still to be claimed", said);
    }

    [Fact]
    public async Task ARoundIsAnnouncedWhenItBeginsAndAgainWhenItSettles()
    {
        var log = new Recording();

        await using var table = HostedTableTests.Open(
            seats: 4, people: 0, betweenSeconds: 60, log: log);

        table.Arrive();

        var dealing = await log.Says("is dealing round 1");
        var settled = await log.Says("settled round 1");

        table.Leave();

        Assert.Contains("is dealing round 1", dealing);
        Assert.Matches("settled round 1 in [0-9]+ turns", settled);
    }

    /// <summary>
    /// P63's own discriminating experiment, run where it can be run: does a seat that is stood up
    /// and sat down in again leave the table dealing?
    /// </summary>
    /// <remarks>
    /// 🔥 <b>This is the third hypothesis for P63's stall, and it is the one a test can settle.</b>
    /// A connection dropped past the retention window disposes <c>TableView</c>, which stands the
    /// player up <em>and</em> leaves the table; coming back arrives and sits down again. If those
    /// four calls did not balance, <c>_attending</c> would fall to zero under a viewer who is
    /// still there, and the table would go quiet with the page open in front of it — which is
    /// exactly what was seen. ⚠️ <b>It does balance</b>: recorded as a hypothesis eliminated, not
    /// as a defect found.
    /// </remarks>
    [Fact]
    public async Task StandingUpAndSittingDownAgainLeavesTheTableDealing()
    {
        await using var table = HostedTableTests.Open(seats: 4, people: 1, betweenSeconds: 60);

        table.Arrive();
        var first = table.SitDown("Nick");
        Assert.NotNull(first);
        Assert.True(table.ShouldDeal);

        // Past the retention window: the framework disposes the component, which stands the
        // player up and then leaves.
        table.StandUp(first!);
        table.Leave();

        Assert.Equal(0, table.Attending);
        Assert.False(table.ShouldDeal);

        // And the player comes back, in whichever order the two claims are made.
        table.Arrive();
        var again = table.SitDown("Nick");

        Assert.NotNull(again);
        Assert.Equal(1, table.Attending);
        Assert.True(table.ShouldDeal);

        table.Leave();
    }

    /// <summary>Every line this table said, and at what level.</summary>
    private sealed class Recording : ILogger
    {
        private readonly ConcurrentQueue<(LogLevel Level, string Message)> _said = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            _said.Enqueue((logLevel, formatter(state, exception)));
        }

        /// <summary>The first line containing this, once it has been said.</summary>
        public async Task<string> Says(string fragment)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);

            while (DateTime.UtcNow < deadline)
            {
                if (Find(fragment) is { } line)
                {
                    return line;
                }

                await Task.Delay(25, TestContext.Current.CancellationToken);
            }

            Assert.Fail($"The table never said anything containing \"{fragment}\".");
            return null!;
        }

        public LogLevel LevelOf(string message) =>
            _said.First(said => said.Message == message).Level;

        private string? Find(string fragment) =>
            _said.FirstOrDefault(said => said.Message.Contains(fragment, StringComparison.Ordinal))
                .Message;
    }
}

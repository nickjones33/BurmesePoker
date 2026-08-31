using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;

namespace BurmesePoker.Tests.Server;

/// <summary>
/// 🔥 <b>The clock on a standing question, against the moment a player stops being able to
/// answer it</b> (P64).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The defect these tests state.</b> P54 joined the table's patience to
/// <c>CircuitOptions.DisconnectedCircuitRetentionPeriod</c> and read the difference as a margin:
/// 180 s against 120 s, so a player whose connection drops has a minute in hand. ⚠️ <b>The two
/// clocks do not start at the same event.</b> The retention period starts when the circuit
/// <em>drops</em>; the patience started when the question was <em>asked</em>. The margin is
/// therefore 60 s only for a player who vanishes the instant they are asked, and <b>zero for one
/// who vanishes with a minute of their patience already spent</b> — who is then played for while
/// the framework is still holding their circuit, and comes back to a table that moved without
/// them.
/// </para>
/// <para>
/// ✅ <b>P63 watched exactly that happen on a phone.</b> A backgrounded Chrome tab lost its
/// circuit in 5.6 s, and the round log carried <c>ran out of time — the computer is playing this
/// seat</c> <em>before</em> <c>left the table</c>. ⚠️ <b>No pair of constants can express the
/// condition</b>, which is why P63 moved neither and P64 moved the clock instead.
/// </para>
/// <para>
/// ⚠️ <b>Seconds, and a real one at a time.</b> Nothing here can be faked: the wait under test is
/// <c>SeatChannel.Ask</c> blocking a round's own thread (§3.6), so the patience is scaled down to
/// seconds and the class runs in the wall-clock collection so that a busy suite is not what
/// decides it.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class PatienceClockTests
{
    private static readonly PlayerId One = new(1);
    private static readonly PlayerId Two = new(2);
    private static readonly PlayerId Three = new(3);
    private static readonly PlayerId Four = new(4);

    /// <summary>Long enough to divide into thirds and still be a wall clock. </summary>
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(3);

    /// <summary>
    /// 🔥 <b>The packet's whole claim: where in its patience a connection goes makes no
    /// difference to what the player is given afterwards.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same drop is run twice, at two points in one patience, and the question is read back
    /// at four fifths of a patience <em>after the drop</em> each time. ⚠️ <b>Today's code passes
    /// the first case and fails the second</b>, which is the inequality stated as a test: a
    /// player who vanishes early keeps their turn and a player who vanishes late loses it, for no
    /// reason either of them can see.
    /// </para>
    /// <para>
    /// ⚠️ <b>Identity, not presence.</b> A seat is asked twice a turn (P59), so a non-null
    /// <see cref="SeatConnection.Pending"/> is no evidence at all — the question that was
    /// standing may have been played by the stand-in and replaced. The prompt object is the fact.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.6)]
    public void AConnectionThatDropsIsGivenItsPatienceAgainWhereverInItTheDropFalls(double spent)
    {
        using var table = Sitting();

        table.WaitToBeAsked();
        Thread.Sleep(Patience * spent);

        Assert.True(table.Connection.ConnectionLost(), "the drop bought the standing question nothing.");

        Thread.Sleep(Patience * 0.8);

        Assert.Same(table.FirstAsked, table.Connection.Pending);
    }

    /// <summary>
    /// ⚠️ <b>And a connection that flaps cannot hold the table for ever.</b>
    /// </summary>
    /// <remarks>
    /// A question is never held past twice the patience it was asked with — a ceiling derived
    /// from the number already chosen rather than a second constant that would have to be kept in
    /// step with it. <b>Without it the restart is a denial of service written into the server</b>:
    /// a client that dropped every second would stop the round.
    /// </remarks>
    [Fact]
    public void AConnectionThatKeepsDroppingStillRunsOutOfPatienceInTheEnd()
    {
        using var table = Sitting();

        table.WaitToBeAsked();

        var ceiling = DateTime.UtcNow + (Patience * 2.5);

        while (DateTime.UtcNow < ceiling)
        {
            table.Connection.ConnectionLost();
            Thread.Sleep(Patience / 8);
        }

        Assert.NotSame(table.FirstAsked, table.Connection.Pending);
    }

    /// <summary>A table with one person's seat in it, playing a round nobody has answered yet.</summary>
    private static Sat Sitting() => new();

    /// <summary>
    /// The fixture: a real round on a real thread, stopped at this seat's first question.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The seat is away for that one question and answers every other</b> (P13.2's
    /// <c>APlayerWhoComesBackIsPlayingAgain</c> idiom), so the round finishes in a moment once
    /// the test has read what it came for rather than spending a patience on every question left
    /// in it.
    /// </remarks>
    private sealed class Sat : IDisposable
    {
        private readonly TableSession _table;
        private readonly ManualResetEventSlim _asked = new(initialState: false);
        private readonly Task<RoundRecord> _round;

        internal Sat()
        {
            _table = TableSession.Open(
                [
                    TableSeat.Person(One, "Nick"),
                    TableSeat.Computer(Two, "Ruby (bot)", new GreedyBotAgent()),
                    TableSeat.Computer(Three, "Sable (bot)", new GreedyBotAgent()),
                    TableSeat.Computer(Four, "Onyx (bot)", new GreedyBotAgent())
                ],
                new TableOptions
                {
                    Seed = 20260831,
                    Patience = Patience,
                    RoundTimeLimit = TimeSpan.FromMinutes(2)
                });

            Connection = _table.ConnectionFor(One);

            // Subscribed before the scripted seat is, so the first prompt is written down here
            // before anything has a chance to answer it.
            Connection.Updated += Noticed;

            _ = new ScriptedSeat(Connection) { Away = prompt => ReferenceEquals(prompt, FirstAsked) };

            _round = _table.PlayRoundAsync();
        }

        /// <summary>The seat's one connection.</summary>
        internal SeatConnection Connection { get; }

        /// <summary>The first question this seat was asked — the one every test here is about.</summary>
        internal SeatPrompt? FirstAsked { get; private set; }

        internal void WaitToBeAsked() =>
            Assert.True(_asked.Wait(TimeSpan.FromSeconds(30)), "the seat was never asked anything.");

        public void Dispose()
        {
            Connection.Updated -= Noticed;

            // The round is still running and holds a thread; let it finish rather than leaving
            // it to trip another test's wall clock.
            _round.Wait(TimeSpan.FromMinutes(2));
            _asked.Dispose();
        }

        private void Noticed(SeatConnection connection)
        {
            if (FirstAsked is not null || connection.Pending is not { } prompt)
            {
                return;
            }

            FirstAsked = prompt;
            _asked.Set();
        }
    }
}

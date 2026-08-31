using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;
using BurmesePoker.Tests.Server;
using BurmesePoker.Web;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// ✅ <b>What a returning player is told about the gap they were away for</b> (P64).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The silence three packets recorded and none fixed.</b> P54's finding (4), P59's finding
/// (3) and P63's finding (5) all say the same sentence: a player whose connection drops comes
/// back to a board the computer has moved for them <em>with nothing on screen saying so</em>.
/// P63 saw it on hardware — the round log carried the whole story and the returning screen said
/// none of it.
/// </para>
/// <para>
/// ⚠️ <b>It is not a new event.</b> <c>TableEvent.SeatPlayedByTheComputer</c> has been broadcast
/// once a turn since P13.2 and this seat's own connection already heard it; what was missing was
/// a mark for <em>where the player was</em> when it arrived. So the count is read off the public
/// narration and the log is where the detail stays.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class AwayFromTheSeatTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(1);

    /// <summary>
    /// ✅ <b>A player who was away while the computer played for them is told, and told how
    /// much.</b>
    /// </summary>
    [Fact]
    public void AReturningConnectionIsToldWhatWasPlayedForItWhileItWasGone()
    {
        using var sat = new Sat();

        sat.WaitToBeAsked();
        sat.Board.ConnectionLost();
        sat.WaitForTheComputerToPlayTheSeat();
        sat.Board.ConnectionBack();

        Assert.True(
            sat.Board.PlayedForYouWhileAway > 0,
            "the returning player was told nothing about the turn played for them.");

        // ⚠️ Put down by pressing it and by nothing else: a notice that cleared itself on the
        // next render would be gone before somebody who has been away had looked up.
        sat.Board.SeenWhatWasPlayedForYou();
        Assert.Equal(0, sat.Board.PlayedForYouWhileAway);
    }

    /// <summary>
    /// ⚠️ <b>And it is about the gap, not about the stand-in.</b>
    /// </summary>
    /// <remarks>
    /// A seat played for while its player was sitting right there — the ordinary
    /// <em>too slow</em> — has nothing to say on a return that never happened. Without this the
    /// count would be a second, worse copy of <c>TableBoard.StoodInFor</c>, which the table
    /// already draws.
    /// </remarks>
    [Fact]
    public void AConnectionThatNeverWentAwayIsToldNothing()
    {
        using var sat = new Sat();

        sat.WaitToBeAsked();
        sat.WaitForTheComputerToPlayTheSeat();

        Assert.Equal(0, sat.Board.PlayedForYouWhileAway);
    }

    /// <summary>
    /// ⚠️ <b>And the page says it.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>A count nothing draws is the defect this packet exists to fix, one layer along.</b>
    /// P54, P59 and P63 each recorded the silence and each left the facts sitting in a structure
    /// nobody was pointed at; a <see cref="SeatBoard"/> property with no reader would be the same
    /// mistake with a test in front of it. ⚠️ <b>A scan, because nothing here renders a
    /// component</b> — the same reason <c>JackpotSpokenTests</c> is one.
    /// </remarks>
    [Fact]
    public void TheSeatDrawsWhatWasPlayedForItWhileItWasAway()
    {
        var markup = Sources.Read("Components/Table/YourSeat.razor");

        Assert.Contains("PlayedForYouWhileAway", markup, StringComparison.Ordinal);
        Assert.Contains("SeenWhatWasPlayedForYou", markup, StringComparison.Ordinal);

        // ⚠️ Visible and polite: P13.5 found that a hidden live region announces nothing, and a
        // notice that interrupted a question the returning player can now answer would be worse
        // than the silence it replaces.
        Assert.Contains("role=\"status\"", markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// One person's seat, drawn by a <see cref="SeatBoard"/> exactly as a circuit draws it, in a
    /// round it is away for its first question and answering after that.
    /// </summary>
    private sealed class Sat : IDisposable
    {
        private static readonly PlayerId One = new(1);

        private readonly TableSession _table;
        private readonly SeatConnection _connection;
        private readonly ManualResetEventSlim _asked = new(initialState: false);
        private readonly Task<RoundRecord> _round;

        internal Sat()
        {
            _table = TableSession.Open(
                [
                    TableSeat.Person(One, "Nick"),
                    TableSeat.Computer(new PlayerId(2), "Ruby (bot)", new GreedyBotAgent()),
                    TableSeat.Computer(new PlayerId(3), "Sable (bot)", new GreedyBotAgent()),
                    TableSeat.Computer(new PlayerId(4), "Onyx (bot)", new GreedyBotAgent())
                ],
                TableSessionTests.Options(20260831) with { Patience = Patience });

            _connection = _table.ConnectionFor(One);
            _connection.Updated += Noticed;

            Board = new SeatBoard(_connection);
            _ = new ScriptedSeat(_connection) { Away = prompt => ReferenceEquals(prompt, _first) };

            _round = _table.PlayRoundAsync();
        }

        internal SeatBoard Board { get; }

        private SeatPrompt? _first;

        internal void WaitToBeAsked() =>
            Assert.True(_asked.Wait(TimeSpan.FromSeconds(30)), "the seat was never asked anything.");

        internal void WaitForTheComputerToPlayTheSeat()
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);

            while (DateTime.UtcNow < deadline)
            {
                if (_connection.Events.OfType<TableEvent.SeatPlayedByTheComputer>().Any())
                {
                    return;
                }

                Thread.Sleep(25);
            }

            Assert.Fail("the computer never played this seat.");
        }

        public void Dispose()
        {
            _connection.Updated -= Noticed;
            Board.Dispose();
            _round.Wait(TimeSpan.FromMinutes(2));
            _asked.Dispose();
        }

        private void Noticed(SeatConnection connection)
        {
            if (_first is not null || connection.Pending is not { } prompt)
            {
                return;
            }

            _first = prompt;
            _asked.Set();
        }
    }
}

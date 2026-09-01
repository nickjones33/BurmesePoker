using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;
using BurmesePoker.Tests.Server;
using BurmesePoker.Web;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// ✅ <b>The question the server was holding and the screen was not</b> (P67, from P66's
/// observation on the deployed table).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>What was measured, before it was named.</b> A person's round took eighteen minutes for
/// forty turns while the same table's later rounds took thirty-four seconds; the whole of the
/// difference was questions put to a person that the person was never shown. After the seat
/// answered <em>Take</em> the hand went to <b>fourteen</b> cards and the action bar read
/// <c>It is not your turn</c> — and reloading eighteen seconds in came straight back with
/// <c>Throw one away</c> and the same fourteen cards. The question had been standing the whole
/// time; only the board's <see cref="SeatBoard.Asking"/> was gone. ⚠️ <b>The fourteen-card hand
/// is the proof</b>: only the throw prompt carries fourteen, so <c>Read</c> had installed the new
/// prompt and something wiped it afterwards.
/// </para>
/// <para>
/// 🔥 <b>The interleaving is played out rather than raced for, and that is a finding about the
/// defect.</b> <see cref="SeatBoard.Answer"/> lets go of its gate to hand the answer to the
/// connection and takes it again a few instructions later; nothing outside the class can be made
/// to run in that gap, so a test that hoped to win the race would be a test that passed by luck.
/// What can be done exactly is the <em>order</em>: latch the answer on the connection, let the
/// engine ask the next question and let the board hear it, and only then run the step the
/// answering thread runs when it arrives late.
/// </para>
/// <para>
/// ⚠️ <b>The pair matters.</b> <em>Take</em> → <em>Throw</em> is asked by the same thread
/// microseconds apart, which is why it is the pair that lost prompts on the deployed table;
/// <em>Throw</em> → the next <em>Take</em> waits for four computer seats and is the same shape at
/// a longer distance. Both are asserted, and both are answered through the one
/// <see cref="SeatBoard.Answer"/> every control on the page uses.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class LostPromptTests
{
    /// <summary>
    /// 🔥 <b>Take, answered late: the throw prompt that arrived in the gap still stands.</b>
    /// </summary>
    [Fact]
    public void AnswerThatArrivesAfterTheNextQuestionDoesNotWipeIt()
    {
        using var sat = new Sitting();

        var take = sat.WaitFor(SeatQuestion.Take, notThis: null);
        var answer = ScriptedSeat.Reply(take);

        // The connection latching the answer is exactly what SeatBoard.Answer does outside its
        // own gate — and it is where the engine gets in.
        Assert.True(sat.Connection.Answer(answer), "the seat's own answer was refused.");

        var throwing = sat.WaitFor(SeatQuestion.Discard, notThis: take);
        Assert.Equal(14, throwing.Hand.Cards.Count);

        // …and now the answering thread arrives with the gate.
        sat.Board.Settled(take, answer);

        Assert.Same(throwing, sat.Board.Asking);
        Assert.Same(throwing.Hand, sat.Board.Hand);
        Assert.Equal(1, sat.Board.Answered);
    }

    /// <summary>
    /// ⚠️ <b>And the same shape at the far end of the turn, where the answer is a throw.</b>
    /// </summary>
    /// <remarks>
    /// The throw is the answer that also rewrites the hand, so it is the one that could put a
    /// twelve-card hand under a question about fourteen.
    /// </remarks>
    [Fact]
    public void AThrowAnsweredLateDoesNotWipeTheQuestionThatFollowedIt()
    {
        using var sat = new Sitting();

        var take = sat.WaitFor(SeatQuestion.Take, notThis: null);
        Assert.True(sat.Connection.Answer(ScriptedSeat.Reply(take)), "the take was refused.");

        var throwing = sat.WaitFor(SeatQuestion.Discard, notThis: take);
        var answer = ScriptedSeat.Reply(throwing);
        Assert.True(sat.Connection.Answer(answer), "the throw was refused.");

        var next = sat.WaitFor(SeatQuestion.Take, notThis: throwing);

        sat.Board.Settled(throwing, answer);

        Assert.Same(next, sat.Board.Asking);
        Assert.Same(next.Hand, sat.Board.Hand);
    }

    /// <summary>
    /// ⚠️ <b>One guard, because there is one answer.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Build item 3 of the packet, asserted rather than reviewed.</b> All five of the
    /// questions a seat is asked — take, claim, permit, throw, declare — are answered through the
    /// single <see cref="SeatBoard.Answer"/>, so the gap is closed once for every control on the
    /// page. A question that grew its own answering path would be a second place for the same
    /// defect to live, and this is what would notice.
    /// </remarks>
    [Fact]
    public void EveryQuestionIsAnsweredThroughTheOneAnswer()
    {
        var source = Sources.Read("SeatBoard.cs");

        foreach (var control in new[] { "Take(", "Claim(", "Object(", "Throw(", "Declare(" })
        {
            var at = source.IndexOf("public bool " + control, StringComparison.Ordinal);
            Assert.True(at >= 0, $"{control} is not a control on the seat any more.");

            var line = source[at..source.IndexOf('\n', at)];
            Assert.Contains("=> Answer(", line, StringComparison.Ordinal);
        }

        // And the guard itself: the question is put down only if it is still the question that
        // was answered (P67). Nulling it unconditionally is what lost the throw prompt.
        Assert.Contains("ReferenceEquals(Asking, asked)", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// One person's seat with the engine on its own thread, and a script that answers everything
    /// except the take-and-throw pair this test is driving by hand.
    /// </summary>
    private sealed class Sitting : IDisposable
    {
        private static readonly PlayerId One = new(1);

        private readonly TableSession _table;
        private readonly Task<RoundRecord> _round;
        private readonly ScriptedSeat _script;
        private bool _handedOver;
        private bool _reached;

        internal Sitting()
        {
            _table = TableSession.Open(
                [
                    TableSeat.Person(One, "Nick"),
                    TableSeat.Computer(new PlayerId(2), "Ruby (bot)", new GreedyBotAgent()),
                    TableSeat.Computer(new PlayerId(3), "Sable (bot)", new GreedyBotAgent()),
                    TableSeat.Computer(new PlayerId(4), "Onyx (bot)", new GreedyBotAgent())
                ],
                // Long enough that a question this test is holding is never played for the seat
                // while the test thread is looking at it.
                TableSessionTests.Options(20260831) with { Patience = TimeSpan.FromSeconds(30) });

            Connection = _table.ConnectionFor(One);
            Board = new SeatBoard(Connection);

            _script = new ScriptedSeat(Connection) { Away = Holding };

            _round = _table.PlayRoundAsync();
        }

        internal SeatConnection Connection { get; }

        /// <summary>
        /// Which prompts the script leaves standing for the test to drive: every take and throw
        /// from the seat's first take onwards, until the seat is handed back.
        /// </summary>
        /// <remarks>
        /// ⚠️ <b>Not simply <em>every</em> take and throw</b>, and finding that out cost a run.
        /// The opening seat is not asked to take at all — it is asked whether to claim the
        /// turned-up money card (RULES.md §4.5) and then throws — so a script holding the first
        /// throw it sees would be holding one with no take in front of it, which is not the pair
        /// this is about. ⚠️ <b>And a claim's permission is never held</b>: it is put to this seat
        /// while somebody else is playing (P28), and holding one would stop the table rather than
        /// the turn.
        /// </remarks>
        private bool Holding(SeatPrompt prompt)
        {
            _reached |= prompt.Question == SeatQuestion.Take;

            return !_handedOver
                && _reached
                && prompt.Question is SeatQuestion.Take or SeatQuestion.Discard;
        }

        internal SeatBoard Board { get; }

        /// <summary>
        /// The next question of a kind that stands in front of this seat — read off the board,
        /// because the board is what the page draws.
        /// </summary>
        internal SeatPrompt WaitFor(SeatQuestion question, SeatPrompt? notThis)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);

            while (DateTime.UtcNow < deadline)
            {
                if (Board.Asking is { } asking
                    && asking.Question == question
                    && !ReferenceEquals(asking, notThis))
                {
                    return asking;
                }

                Thread.Sleep(5);
            }

            Assert.Fail($"the seat was never asked to {question} Seen: {string.Join(", ", _script.Prompts.Select(shown => shown.Question))}; standing: {Board.Asking?.Question}; round: {_round.Status}");
            return null!;
        }

        public void Dispose()
        {
            // Hand the seat back to the script and answer whatever is standing, so the round
            // finishes on its own rather than on a patience per question.
            _handedOver = true;

            if (Board.Asking is { } standing)
            {
                Connection.Answer(ScriptedSeat.Reply(standing));
            }

            _round.Wait(TimeSpan.FromMinutes(2));
            Board.Dispose();
        }
    }
}

using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;

namespace BurmesePoker.Tests.Server;

/// <summary>
/// A journal for the hosted table (packet P24.1): a table asked to keep one records what every
/// seat was asked and what it answered, in the format the console writes and replay reads.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim is the console's claim, one layer out</b> (P14): a hosted round is written down
/// completely enough to be played again — a person's answers exactly as a bot's — so a browser
/// round is auditable at all, which is what P30.2's browser half is written against.
/// </para>
/// <para>
/// <b>Nothing here is a new format.</b> The record round-trips through <see cref="JournalFormat"/>
/// and replays through <see cref="JournalPlayerAgent"/>, both untouched by this packet.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class TableJournalTests
{
    private static readonly PlayerId One = new(1);
    private static readonly PlayerId Two = new(2);
    private static readonly PlayerId Three = new(3);
    private static readonly PlayerId Four = new(4);

    /// <summary>
    /// ✅ <b>The packet's whole claim</b>: a round played through the server's own plumbing —
    /// remote seats, bounded seats, the fan-out — is written down completely enough that the
    /// ordinary replay plays it back as itself.
    /// </summary>
    [Fact]
    public void AJournalledTableReplaysIdentically()
    {
        var table = TableSession.Open(Seats(), Options(20260819));

        var nick = new ScriptedSeat(table.ConnectionFor(One));
        var myaLay = new ScriptedSeat(table.ConnectionFor(Three));

        var first = table.PlayRound().Result;
        var second = table.PlayRound().Result;

        var journal = table.Journal();

        Assert.NotNull(journal);
        Assert.Equal(2, journal!.Header.Rounds);
        Assert.False(journal.Header.Abandoned);

        // The lines are the identity (P14): what is compared is what would be written.
        var read = JournalFormat.Read(JournalFormat.Lines(journal).ToList());

        Assert.Equal(Summarise([first, second]), Summarise(Replay(read)));

        // And both people's answers really are in it — a journal that quietly recorded only
        // the computer's seats would replay a different table.
        Assert.Contains(read.Decisions, decision => decision.Player == One);
        Assert.Contains(read.Decisions, decision => decision.Player == Three);
        Assert.True(nick.Answered > 0);
        Assert.True(myaLay.Answered > 0);
    }

    /// <summary>
    /// A seat is attributed to what sat in it: <c>human</c> for a remote seat, the strategy the
    /// host named for a computer one, and <c>bot</c> where it named none — half of a CSV row's
    /// join key (§3.8 item 4), exactly as the console writes it.
    /// </summary>
    [Fact]
    public void TheHeaderSaysWhoWasInEachSeat()
    {
        var table = TableSession.Open(Seats(), Options(7));

        _ = new ScriptedSeat(table.ConnectionFor(One));
        _ = new ScriptedSeat(table.ConnectionFor(Three));

        table.PlayRound();

        var header = table.Journal()!.Header;

        Assert.Equal(["human", "greedy", "human", "bot"], header.Seats.Select(seat => seat.Strategy));
        Assert.Equal(["Nick", "Ruby (bot)", "Mya Lay", "Sable (bot)"], header.Seats.Select(seat => seat.Name));
        Assert.Equal([One, Two, Three, Four], header.Players);
    }

    /// <summary>A table not asked to keep a journal keeps none, and says so with a null.</summary>
    [Fact]
    public void ATableNotAskedToKeepAJournalHasNone()
    {
        var table = TableSession.Open(Seats(), TableSessionTests.Options(11));

        _ = new ScriptedSeat(table.ConnectionFor(One));
        _ = new ScriptedSeat(table.ConnectionFor(Three));

        table.PlayRound();

        Assert.Null(table.Journal());
    }

    /// <summary>
    /// A round the table gave up on marks the record rather than poisoning it: the header says
    /// <c>Abandoned</c>, and <c>Rounds</c> stops at what settled — the unfinished decisions are
    /// data, not something to play back (P14).
    /// </summary>
    [Fact]
    public void AnAbandonedRoundIsWrittenDownAsAbandoned()
    {
        var table = TableSession.Open(
            Seats(),
            Options(15) with { RoundTimeLimit = TimeSpan.Zero });

        _ = new ScriptedSeat(table.ConnectionFor(One));
        _ = new ScriptedSeat(table.ConnectionFor(Three));

        Assert.Throws<TableAbandonedException>(table.PlayRound);

        var journal = table.Journal();

        Assert.NotNull(journal);
        Assert.True(journal!.Header.Abandoned);
        Assert.Equal(0, journal.Header.Rounds);
    }

    /// <summary>
    /// ✅ <b>P24.2 acceptance 3 — the artifact the packet exists for.</b> A person's decisions
    /// carry the adviser's card and its rationale, and <c>Answer != Advice.Card</c> picks out the
    /// turns on which they and it chose differently.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>What is recorded is an opinion beside an answer, not a rationale on a decision.</b>
    /// <c>JournalingAgent</c> writes down the answer <em>the seat gave</em>; this is a different
    /// agent's opinion about the same moment. That is what turns <em>where the expert disagreed
    /// with the computer</em> from something a person has to notice into a query.
    /// </para>
    /// <para>
    /// ⚠️ <b>By <see cref="Domain.Cards.CardId"/> and never by value</b> (§3.1): two decks hold two
    /// 5♥, and a comparison that said <em>"she agreed"</em> because the values matched would be
    /// wrong on precisely the hands worth studying.
    /// </para>
    /// </remarks>
    [Fact]
    public void APersonsDecisionsCarryTheComputersOpinionBesideThem()
    {
        var table = TableSession.Open(Seats(), Options(20260822));

        // One follows the hint and one refuses to; the file has to tell them apart.
        _ = new ScriptedSeat(table.ConnectionFor(One));
        _ = new ScriptedSeat(table.ConnectionFor(Three)) { Contrarian = true };

        table.PlayRound();

        // The lines are the identity (P14): the advice has to survive the file, not merely exist
        // in memory.
        var journal = JournalFormat.Read(JournalFormat.Lines(table.Journal()!).ToList());

        var advised = journal.Decisions
            .Where(decision => decision.Question == JournalQuestion.Discard && decision.Advice is not null)
            .ToArray();

        Assert.NotEmpty(advised);

        // ⚠️ Only seats a person is playing. A bot's advice is its own answer, so recording it
        // would run the adviser twice a turn to learn nothing.
        Assert.All(advised, decision => Assert.Contains(decision.Player, new[] { One, Three }));
        Assert.DoesNotContain(
            journal.Decisions,
            decision => decision.Advice is not null && (decision.Player == Two || decision.Player == Four));

        // Whose opinion it was, and the sentence as the seat was shown it.
        Assert.All(advised, decision => Assert.Equal(BotCatalog.Hardest.Name, decision.Advice!.Rung));
        Assert.All(advised, decision => Assert.False(string.IsNullOrWhiteSpace(decision.Advice!.Why)));

        // 🔥 The query. The seat that followed the hint never disagrees; the one that would not
        // does, on every turn it was asked.
        Assert.DoesNotContain(
            advised.Where(decision => decision.Player == One),
            decision => decision.DisagreedWithTheComputer);

        Assert.Contains(
            advised.Where(decision => decision.Player == Three),
            decision => decision.DisagreedWithTheComputer);
    }

    /// <summary>
    /// ⚠️ <b>A record of where somebody disagreed with the computer must not depend on whether
    /// they had the hints box ticked.</b> The hint is a thing you are shown; the opinion is a
    /// thing that is written down.
    /// </summary>
    [Fact]
    public void TheOpinionIsRecordedEvenWithTheHintsOff()
    {
        var table = TableSession.Open(Seats(), Options(3) with { Hints = false });

        _ = new ScriptedSeat(table.ConnectionFor(One));
        _ = new ScriptedSeat(table.ConnectionFor(Three));

        table.PlayRound();

        var journal = table.Journal()!;

        // Nothing was marked on any hand, and nothing was explained on any prompt…
        Assert.All(
            journal.Decisions,
            decision => Assert.True(decision.Question != JournalQuestion.Discard || decision.Advice is not null
                || decision.Player == Two || decision.Player == Four));

        Assert.Contains(
            journal.Decisions,
            decision => decision.Player == One && decision.Advice is not null);
    }

    /// <summary>
    /// ✅ <b>Replay is unaffected</b> (P24.2): <c>JournalPlayerAgent</c> ignores the field, so a
    /// file with advice in it plays back as the same game a file without it would.
    /// </summary>
    [Fact]
    public void AdviceChangesNothingAboutReplay()
    {
        var table = TableSession.Open(Seats(), Options(41));

        _ = new ScriptedSeat(table.ConnectionFor(One));
        _ = new ScriptedSeat(table.ConnectionFor(Three)) { Contrarian = true };

        var played = table.PlayRound().Result;
        var journal = JournalFormat.Read(JournalFormat.Lines(table.Journal()!).ToList());

        Assert.Contains(journal.Decisions, decision => decision.Advice is not null);
        Assert.Equal(Summarise([played]), Summarise(Replay(journal)));

        // …and stripping the advice out replays to the very same thing, which is what makes it a
        // note in the margin rather than part of the record.
        var stripped = new GameJournal(
            journal.Header,
            [.. journal.Decisions.Select(decision => decision with { Advice = null })]);

        Assert.Equal(Summarise(Replay(journal)), Summarise(Replay(stripped)));
    }

    private static TableOptions Options(int seed) =>
        TableSessionTests.Options(seed) with { Journal = "table.jsonl" };

    /// <remarks>
    /// One computer seat carries a strategy name and one does not, so the attribution's both
    /// halves are in every test's header.
    /// </remarks>
    private static TableSeat[] Seats() =>
    [
        TableSeat.Person(One, "Nick"),
        TableSeat.Computer(Two, "Ruby (bot)", new GreedyBotAgent(), "greedy"),
        TableSeat.Person(Three, "Mya Lay"),
        TableSeat.Computer(Four, "Sable (bot)", new GreedyBotAgent())
    ];

    /// <summary>Plays a journal back — which is playing the game with different seats (P14).</summary>
    private static List<RoundResult> Replay(GameJournal journal)
    {
        var match = new MatchEngine(
            journal.Header.Players,
            JournalPlayerAgent.SeatsOf(journal),
            journal.Header.Stakes,
            new Random(journal.Header.Seed));

        return [.. Enumerable.Range(0, journal.Header.Rounds).Select(_ => match.PlayRound().Result)];
    }

    /// <summary>A round as the text of everything that has to match.</summary>
    private static List<string> Summarise(IEnumerable<RoundResult> results) =>
    [
        .. results.Select(result =>
            $"round {result.Round}: {result.Winner} won in {result.Turns} turns, "
            + string.Join(" ", result.Payouts.OrderBy(payout => payout.Key.Value).Select(payout => $"{payout.Key}={payout.Value}")))
    ];
}

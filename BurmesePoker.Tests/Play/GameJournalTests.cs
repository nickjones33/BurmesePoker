using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Play;

/// <summary>
/// Game journals (packet P14): a game written down completely enough to be played again.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim these tests exist for is that a journal replays a game and a seed does not
/// always</b> (BUILD-PLAN §3.9). A seed is a pointer into the build that produced it; edit a
/// strategy and it points somewhere else, silently. The journal holds the answers, so it
/// replays against any build — <see cref="AJournalReplaysWhatASeedNoLongerCan"/> is that
/// argument in one method.
/// </para>
/// <para>
/// <b>Nothing here touches the engine.</b> Journalling is a decorator over
/// <see cref="IPlayerAgent"/> and replaying is a seat that answers from a file, so these tests
/// play ordinary matches through the ordinary <see cref="MatchEngine"/> (P14).
/// </para>
/// </remarks>
public class GameJournalTests
{
    private static readonly PlayerId[] Table =
        [new PlayerId(0), new PlayerId(1), new PlayerId(2), new PlayerId(3)];

    [Fact]
    public void AJournalledGameReplaysIdentically()
    {
        // The packet's whole claim, and unlike P11's it is mechanically checkable: the same
        // rounds, the same winners, the same money, and the same narration event for event.
        var played = Play(4242, () => new GreedyBotAgent(), journal: new GameJournalBuilder());
        var replayed = Replay(played.Journal!);

        Assert.Equal(Summarise(played.Results), Summarise(replayed.Results));
        Assert.Equal(played.Observer.Events, replayed.Observer.Events);
        Assert.Equal(played.Observer.Draws, replayed.Observer.Draws);
    }

    [Fact]
    public void AJournalReplaysWhatASeedNoLongerCan()
    {
        // §3.9 point 2 in one test. The same seed played by a different strategy is a
        // different game — which is what happens to yesterday's seeds the day a bot is
        // edited — while the journal of the first game still plays back as itself.
        var greedy = Play(99, () => new GreedyBotAgent(), journal: new GameJournalBuilder());
        var simple = Play(99, () => new SimpleBotAgent());

        Assert.NotEqual(Summarise(greedy.Results), Summarise(simple.Results));
        Assert.Equal(Summarise(greedy.Results), Summarise(Replay(greedy.Journal!).Results));
    }

    [Fact]
    public void AJournalRoundTripsThroughItsFormat()
    {
        // Write, read, replay, same game. The format is the durable part — a journal that
        // only replayed in the process that made it would be a memory, not a record.
        var played = Play(7, () => new GreedyBotAgent(), journal: new GameJournalBuilder());
        var lines = JournalFormat.Lines(played.Journal!).ToList();
        var read = JournalFormat.Read(lines);

        // The lines are the identity, not the object graph: a journal is only worth having
        // because it survives being written down, so what is compared is what is written.
        Assert.Equal(lines, JournalFormat.Lines(read).ToList());
        Assert.Equal(played.Journal!.Decisions, read.Decisions);
        Assert.Equal(Summarise(played.Results), Summarise(Replay(read).Results));
    }

    [Fact]
    public void SeveralJournalsShareAFileAndComeBackApart()
    {
        var first = Play(11, () => new GreedyBotAgent(), journal: new GameJournalBuilder()).Journal!;
        var second = Play(12, () => new GreedyBotAgent(), journal: new GameJournalBuilder()).Journal!;

        var read = JournalFormat.ReadAll(JournalFormat.Lines([first, second]));

        Assert.Equal(2, read.Count);
        Assert.Equal(first.Decisions, read[0].Decisions);
        Assert.Equal(second.Decisions, read[1].Decisions);
    }

    [Fact]
    public void ThinIsTheDefaultAndRecordsAnswersAlone()
    {
        // The fidelity that a throughput run leaves on. Snapshotting a hand at every decision
        // is the allocation-heavy part (§3.7), so it must not arrive by accident.
        var thin = Play(4242, () => new GreedyBotAgent(), journal: new GameJournalBuilder()).Journal!;

        Assert.Equal(JournalFidelity.Thin, thin.Header.Fidelity);
        Assert.All(thin.Decisions, decision => Assert.Null(decision.Snapshot));
    }

    [Fact]
    public void RichAlsoRecordsWhatTheSeatCouldSee()
    {
        var rich = Play(4242, () => new GreedyBotAgent(), journal: new GameJournalBuilder(JournalFidelity.Rich)).Journal!;

        Assert.Equal(JournalFidelity.Rich, rich.Header.Fidelity);
        Assert.All(rich.Decisions, decision => Assert.NotNull(decision.Snapshot));

        // Thirteen cards while the seat is still taking one, and fourteen once it has.
        Assert.All(
            rich.Decisions.Where(decision => decision.Question == JournalQuestion.Discard),
            decision => Assert.Equal(RoundEngine.HandSize + 1, decision.Snapshot!.Hand.Count));

        Assert.All(
            rich.Decisions.Where(decision => decision.Question is JournalQuestion.Action or JournalQuestion.Claim),
            decision => Assert.Equal(RoundEngine.HandSize, decision.Snapshot!.Hand.Count));

        // And it survives the format, which is the only reason to record it.
        var read = JournalFormat.Read(JournalFormat.Lines(rich));

        Assert.Equal(JournalFormat.Lines(rich).ToList(), JournalFormat.Lines(read).ToList());
        Assert.Equal(
            rich.Decisions.Select(decision => decision.Snapshot!.Hand),
            read.Decisions.Select(decision => decision.Snapshot!.Hand));
    }

    [Fact]
    public void TheAnswersAreCardIdentitiesRatherThanCardValues()
    {
        // Two decks mean two 5♥ (BUILD-PLAN §3.1). A journal that recorded the value would
        // replay as a different game whenever the seat held both copies, and would do it
        // without complaining.
        var journal = Play(4242, () => new GreedyBotAgent(), journal: new GameJournalBuilder()).Journal!;

        Assert.All(
            journal.Decisions.Where(decision => decision.Question == JournalQuestion.Discard),
            decision => Assert.InRange(decision.AsCardId().Value, 0, 107));
    }

    [Fact]
    public void AShortJournalFailsWhereItRunsOut()
    {
        var played = Play(4242, () => new GreedyBotAgent(), journal: new GameJournalBuilder());
        var whole = played.Journal!;
        var truncated = new GameJournal(whole.Header, [.. whole.Decisions.Take(whole.Decisions.Count / 2)]);

        var problem = Assert.Throws<JournalException>(() => Replay(truncated));

        Assert.Contains("runs out", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AJournalThatDisagreesWithTheGameSaysWhere()
    {
        // The failure that matters: silent divergence would make a replay look successful
        // while being a different game (BUILD-PLAN P14).
        var whole = Play(4242, () => new GreedyBotAgent(), journal: new GameJournalBuilder()).Journal!;
        var opening = whole.Decisions.First(decision => decision.Question == JournalQuestion.Discard);
        var elsewhere = whole.Decisions.Where(decision => decision != opening).ToList();

        var problem = Assert.Throws<JournalException>(() => Replay(new GameJournal(whole.Header, elsewhere)));

        Assert.Contains("diverged", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AJournalThatThrowsACardTheSeatIsNotHoldingSaysSo()
    {
        var whole = Play(4242, () => new GreedyBotAgent(), journal: new GameJournalBuilder()).Journal!;

        var swapped = whole.Decisions
            .Select(decision => decision.Question == JournalQuestion.Discard && decision.Turn == 1
                ? decision with { Answer = "107" }
                : decision)
            .ToList();

        var problem = Assert.Throws<JournalException>(() => Replay(new GameJournal(whole.Header, swapped)));

        Assert.Contains("not in the hand", problem.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not json at all", "readable JSON")]
    [InlineData("{\"type\":\"decision\",\"round\":1,\"turn\":1,\"player\":0,\"question\":\"discard\",\"answer\":\"3\"}", "no journal has begun")]
    [InlineData("{\"type\":\"something-else\"}", "unknown type")]
    public void ACorruptJournalFailsOnTheLineThatIsWrong(string line, string complaint)
    {
        var problem = Assert.Throws<JournalException>(() => JournalFormat.ReadAll([line]));

        Assert.Contains(complaint, problem.Message, StringComparison.Ordinal);
        Assert.Contains("Line 1", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AHeaderCarriesTheKeysThatJoinAJournalToARun()
    {
        // Every CSV row repeats the master seed, the game, the game's seed, the round and the
        // strategy (BUILD-PLAN §3.8 item 4). A journal that could not be joined to them would
        // be an island.
        var header = new JournalHeader(
            Seed: 4242,
            Seats: [.. Table.Select(player => new JournalSeat(player, "greedy", $"Seat {player.Value}"))],
            Stakes: Stakes.Standard,
            Rounds: 2,
            MasterSeed: 20260818,
            Game: 17);

        var read = JournalFormat.Read(JournalFormat.Lines(new GameJournal(header, []))).Header;

        Assert.Equal(header.Seed, read.Seed);
        Assert.Equal(header.MasterSeed, read.MasterSeed);
        Assert.Equal(header.Game, read.Game);
        Assert.Equal(header.Rounds, read.Rounds);
        Assert.Equal(header.Stakes, read.Stakes);
        Assert.Equal(header.Seats, read.Seats);
        Assert.Equal(4, read.TableSize);
        Assert.Equal(Table, read.Players);
        Assert.Equal(JournalHeader.CurrentRulesRevision, read.RulesRevision);
    }

    /// <summary>
    /// Plays a two-round match, optionally writing it down. Two rounds rather than one so that
    /// a journal has to keep the rounds apart.
    /// </summary>
    private static (GameJournal? Journal, RecordingObserver Observer, List<RoundResult> Results) Play(
        int seed,
        Func<IPlayerAgent> strategy,
        GameJournalBuilder? journal = null,
        int rounds = 2)
    {
        var observer = new RecordingObserver();
        var seats = Table.ToDictionary(player => player, _ => strategy());

        var match = new MatchEngine(
            Table,
            journal is null
                ? seats.ToDictionary(seat => seat.Key, IPlayerAgent (seat) => seat.Value)
                : JournalingAgent.Wrap(seats.ToDictionary(seat => seat.Key, IPlayerAgent (seat) => seat.Value), journal),
            Stakes.Standard,
            new Random(seed),
            observer);

        var results = new List<RoundResult>();

        for (var round = 0; round < rounds; round++)
        {
            results.Add(match.PlayRound().Result);
        }

        return (
            journal?.Build(new JournalHeader(
                Seed: seed,
                Seats: [.. Table.Select(player => new JournalSeat(player, "greedy"))],
                Stakes: Stakes.Standard,
                Rounds: rounds,
                Fidelity: journal.Fidelity)),
            observer,
            results);
    }

    /// <summary>Plays a journal back — which is playing the game with different seats.</summary>
    private static (RecordingObserver Observer, List<RoundResult> Results) Replay(GameJournal journal)
    {
        var observer = new RecordingObserver();

        var match = new MatchEngine(
            journal.Header.Players,
            JournalPlayerAgent.SeatsOf(journal),
            journal.Header.Stakes,
            new Random(journal.Header.Seed),
            observer);

        var results = new List<RoundResult>();

        for (var round = 0; round < journal.Header.Rounds; round++)
        {
            results.Add(match.PlayRound().Result);
        }

        return (observer, results);
    }

    /// <summary>A round as the text of everything that has to match: who won, and every payout.</summary>
    private static List<string> Summarise(IEnumerable<RoundResult> results) =>
    [
        .. results.Select(result =>
            $"round {result.Round}: {result.Winner} won in {result.Turns} turns, "
            + $"{result.Melds.Count} melds, "
            + string.Join(" ", result.Payouts.OrderBy(payout => payout.Key.Value).Select(payout => $"{payout.Key}={payout.Value}")))
    ];
}

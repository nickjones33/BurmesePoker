using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;

namespace BurmesePoker.Tests.Server;

/// <summary>
/// The table server: seats played from somewhere else, seats played by the computer, and the
/// round that comes out of the mixture (packet P13.2).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The first mechanical acceptance criterion a multiplayer packet in this plan has had.</b>
/// It exists because of the framework choice: Blazor Server supplies the transport
/// (BUILD-PLAN §3.10), so the "server" is a seat and a fan-out with nothing serialised, and a
/// test holds the same <c>SeatConnection</c> a browser circuit will hold. There is no socket
/// here because there is no socket anywhere.
/// </para>
/// <para>
/// <b>The engine is untouched, again.</b> A remote player blocks inside a synchronous
/// <c>IPlayerAgent</c> — the bet §3.6 took before P10, now collected — so the round these tests
/// play is <c>RoundEngine</c> exactly as P7 left it, and <c>MatchEngine</c> exactly as P9 left
/// it.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class TableSessionTests
{
    private static readonly PlayerId One = new(1);
    private static readonly PlayerId Two = new(2);
    private static readonly PlayerId Three = new(3);
    private static readonly PlayerId Four = new(4);

    /// <summary>
    /// ✅ <b>Acceptance 1.</b> Two scripted remote seats and two bots play a full round through
    /// the server's own plumbing, with no sockets involved.
    /// </summary>
    [Fact]
    public void TwoRemoteSeatsAndTwoBotsPlayAFullRound()
    {
        var table = TableSession.Open(TwoPeopleAndTwoBots(), Options(20260819));
        var watcher = table.Watch();

        var nick = new ScriptedSeat(table.ConnectionFor(One));
        var myaLay = new ScriptedSeat(table.ConnectionFor(Three));

        var played = table.PlayRound();

        Assert.Contains(played.Result.Winner, table.Players);
        Assert.Equal(0, played.Result.Payouts.Values.Sum());
        Assert.Equal(1, table.RoundsPlayed);
        Assert.Equal(0, table.Banks.Values.Sum());

        // The remote seats really were asked, and really did answer — a round that quietly
        // fell back to the computer everywhere would otherwise look identical.
        Assert.NotEmpty(nick.Prompts);
        Assert.NotEmpty(myaLay.Prompts);
        Assert.Equal(nick.Prompts.Count, nick.Answered);
        Assert.Equal(myaLay.Prompts.Count, myaLay.Answered);
        Assert.Empty(watcher.Events.OfType<TableEvent.SeatPlayedByTheComputer>());

        // And the table saw the round it played.
        Assert.Single(watcher.Events.OfType<TableEvent.RoundStarted>());
        Assert.Single(watcher.Events.OfType<TableEvent.Declared>());
        Assert.Single(watcher.Events.OfType<TableEvent.Settled>());
    }

    /// <remarks>
    /// A table carries its seed and nothing else draws from it (P11, P14), so the same seed and
    /// the same script are the same match. This is the console's <c>--seed</c> property, one
    /// layer out, and it is what makes a bug report from a browser worth anything.
    /// </remarks>
    [Fact]
    public void TheSameSeedAndTheSameScriptPlayTheSameRound()
    {
        var first = PlayOnce(4242);
        var second = PlayOnce(4242);
        var other = PlayOnce(4243);

        Assert.Equal(first.Winner, second.Winner);
        Assert.Equal(first.Turns, second.Turns);
        Assert.Equal(first.Payouts, second.Payouts);
        Assert.NotEqual((first.Winner, first.Turns), (other.Winner, other.Turns));

        static RoundResult PlayOnce(int seed)
        {
            var table = TableSession.Open(TwoPeopleAndTwoBots(), Options(seed));
            _ = new ScriptedSeat(table.ConnectionFor(One));
            _ = new ScriptedSeat(table.ConnectionFor(Three));
            return table.PlayRound().Result;
        }
    }

    /// <summary>
    /// ✅ <b>Acceptance 3.</b> A seat that stops answering is played by the computer, the round
    /// finishes, and the takeover is reported rather than silent.
    /// </summary>
    [Fact]
    public void ASeatThatStopsAnsweringIsPlayedByTheComputerAndTheTableIsTold()
    {
        var table = TableSession.Open(
            OnePersonAndThreeBots(),
            Options(11) with { Patience = TimeSpan.Zero });

        var watcher = table.Watch();
        var absent = new ScriptedSeat(table.ConnectionFor(One)) { Silent = true };

        var played = table.PlayRound();

        Assert.Contains(played.Result.Winner, table.Players);
        Assert.NotEmpty(absent.Prompts);
        Assert.Equal(0, absent.Answered);

        var takeovers = watcher.Events.OfType<TableEvent.SeatPlayedByTheComputer>().ToList();
        Assert.NotEmpty(takeovers);
        Assert.All(takeovers, moment => Assert.Equal(One, moment.Player));

        // Once a turn, however many of that turn's questions went unanswered — the same
        // reasoning, and the same key, as P11's pacing decorator.
        Assert.Equal(
            takeovers.Count,
            takeovers.Select(moment => (moment.Round, moment.Turn)).Distinct().Count());

        // The seat whose player walked away is told about it too, not only the rest of us.
        Assert.NotEmpty(table.ConnectionFor(One).Events.OfType<TableEvent.SeatPlayedByTheComputer>());
    }

    /// <remarks>
    /// A player who misses one prompt and comes back for the next one is simply back: the
    /// takeover is per question, and the stand-in keeps no state to hand over (P15).
    /// </remarks>
    [Fact]
    public void APlayerWhoComesBackIsPlayingAgain()
    {
        var table = TableSession.Open(
            OnePersonAndThreeBots(),
            Options(12) with { Patience = TimeSpan.Zero });

        var connection = table.ConnectionFor(One);

        // Away for their opening turn, back for every one after it.
        var seat = new ScriptedSeat(connection) { Away = prompt => prompt.TurnNumber == 1 };

        table.PlayRound();

        Assert.True(seat.Answered > 0, "the returning player answered nothing");
        Assert.All(
            connection.Events.OfType<TableEvent.SeatPlayedByTheComputer>(),
            moment => Assert.Equal(1, moment.Turn));
        Assert.Single(connection.Events.OfType<TableEvent.SeatPlayedByTheComputer>());
    }

    /// <remarks>
    /// <b>A client bug must not be able to end a table's round.</b> The engine rejects a
    /// discard the seat is not holding by throwing (P7), which is right in a domain and wrong
    /// over a connection; here the answer is refused, the prompt stands, and the client may
    /// try again.
    /// </remarks>
    [Fact]
    public void AnAnswerThatDoesNotFitTheQuestionIsRefusedAndThePromptStands()
    {
        var table = TableSession.Open(OnePersonAndThreeBots(), Options(13));
        var connection = table.ConnectionFor(One);

        Assert.False(connection.Answer(new SeatAnswer.Claim(true)));

        var refusals = 0;
        var accepted = 0;

        connection.Updated += _ =>
        {
            if (connection.Pending is not { } prompt)
            {
                return;
            }

            // The wrong question entirely.
            if (!connection.Answer(new SeatAnswer.Take((TurnAction)99)))
            {
                refusals++;
            }

            if (prompt.Question == SeatQuestion.Discard && NotHeld(prompt) is { } stranger)
            {
                // The right question, naming a card this seat has never seen.
                if (!connection.Answer(new SeatAnswer.Discard(stranger)))
                {
                    refusals++;
                }
            }

            if (connection.Answer(ScriptedSeat.Reply(prompt)))
            {
                accepted++;
            }
        };

        var played = table.PlayRound();

        Assert.True(refusals > 0, "no answer was refused");
        Assert.True(accepted > 0, "no answer was accepted");
        Assert.Contains(played.Result.Winner, table.Players);
        Assert.Empty(connection.Events.OfType<TableEvent.SeatPlayedByTheComputer>());
    }

    /// <remarks>
    /// The real case: the round blocks on the table's thread while the answer arrives from
    /// somewhere else, which is what a circuit does. Everything else in this class answers
    /// inside the notification, which is deterministic but not the shape a browser has.
    /// </remarks>
    [Fact]
    public async Task AnAnswerFromAnotherThreadIsWaitedFor()
    {
        var table = TableSession.Open(OnePersonAndThreeBots(), Options(14));
        var connection = table.ConnectionFor(One);
        var answered = 0;

        connection.Updated += _ =>
        {
            if (connection.Pending is { } prompt)
            {
                Task.Run(() =>
                {
                    if (connection.Answer(ScriptedSeat.Reply(prompt)))
                    {
                        Interlocked.Increment(ref answered);
                    }
                });
            }
        };

        var played = await table.PlayRoundAsync(TestContext.Current.CancellationToken);

        Assert.Contains(played.Result.Winner, table.Players);
        Assert.True(Volatile.Read(ref answered) > 0, "no answer arrived from the other thread");
        Assert.Empty(connection.Events.OfType<TableEvent.SeatPlayedByTheComputer>());
    }

    /// <remarks>
    /// Only a declaration ends a round (RULES.md §7.1) and the cards circulate for ever after
    /// the reshuffle, so a table nobody is left at plays until it is killed. P12 bounds a
    /// simulated round by turn count; a host bounds a real one by the clock, and
    /// <b>reports it</b> — the round is announced abandoned before the exception leaves.
    /// </remarks>
    [Fact]
    public void ATableThatRunsPastItsLimitIsAbandonedAndTheTableIsTold()
    {
        var table = TableSession.Open(
            OnePersonAndThreeBots(),
            Options(15) with { RoundTimeLimit = TimeSpan.Zero });

        var watcher = table.Watch();

        var abandoned = Assert.Throws<TableAbandonedException>(table.PlayRound);

        Assert.Equal(1, abandoned.Round);
        Assert.Equal(TimeSpan.Zero, abandoned.Limit);
        Assert.Single(watcher.Events.OfType<TableEvent.TableAbandoned>());

        // And the table is playable again afterwards: giving up on a round is not giving up on
        // the session.
        Assert.Equal(0, table.RoundsPlayed);
    }

    /// <summary>
    /// 🔥 <b>The rule P13.2 inherits from P13.1: everything handed to a seat is a snapshot,
    /// never a reference.</b>
    /// </summary>
    /// <remarks>
    /// <c>TableState.TurnedUpOnTable</c> is the table's own list and the opening player's claim
    /// takes a card out of it, so a prompt that aliased it would go on changing after it was
    /// sent — and a connection holds what it was given until it next renders. The same defect
    /// P13.1 found in a hand, one layer out: the console never showed it because it draws and
    /// drops in one breath, and a browser never does.
    /// </remarks>
    [Fact]
    public void EverythingASeatWasSentStaysWhatItWasWhenItWasSent()
    {
        var table = TableSession.Open(OnePersonAndThreeBots(), Options(21));
        var watcher = table.Watch();
        var connection = table.ConnectionFor(One);
        var prompts = new List<SeatPrompt>();

        connection.Updated += _ =>
        {
            if (connection.Pending is not { } prompt)
            {
                return;
            }

            prompts.Add(prompt);

            // The opening seat, taking the top money card off the table (RULES.md §4.5) — which
            // is the move that mutates the list a prompt could have aliased.
            connection.Answer(prompt.Question == SeatQuestion.ClaimMoneyCard
                ? new SeatAnswer.Claim(true)
                : ScriptedSeat.Reply(prompt));
        };

        table.PlayRound();

        var opening = prompts[0];
        Assert.Equal(SeatQuestion.ClaimMoneyCard, opening.Question);
        Assert.Equal(2, opening.TurnedUpMoneyCards.Count);
        Assert.Contains(prompts, prompt => prompt.TurnedUpMoneyCards.Count == 1);

        // The claim happened, and the first prompt still says what it said before it did.
        Assert.Single(watcher.Events.OfType<TableEvent.MoneyCardClaimed>());
        Assert.Equal(2, watcher.Events.OfType<TableEvent.RoundStarted>().Single().TurnedUp.Count);

        // And a hand sent at the discard still holds fourteen, though the engine took one back
        // the instant the answer returned (P13.1).
        Assert.All(
            prompts.Where(prompt => prompt.Question == SeatQuestion.Discard),
            prompt => Assert.Equal(14, prompt.Hand.Count));
    }

    /// <remarks>
    /// A watcher holds no seat, so there is nothing to ask it — and asking would be the first
    /// step towards a viewer being handed a hand.
    /// </remarks>
    [Fact]
    public void AWatcherIsNeverAskedAnything()
    {
        var table = TableSession.Open(OnePersonAndThreeBots(), Options(16));
        var watcher = table.Watch();

        Assert.Null(watcher.Player);
        Assert.False(watcher.IsSeated);
        Assert.False(watcher.Answer(new SeatAnswer.Claim(true)));
        Assert.Throws<ArgumentException>(() => table.ConnectionFor(Two));
    }

    /// <remarks>
    /// A table is one task (§3.6), and two rounds at once on one <c>MatchEngine</c> would race
    /// its banks. The second caller is refused rather than queued.
    /// </remarks>
    [Fact]
    public void ATablePlaysOneRoundAtATime()
    {
        var table = TableSession.Open(OnePersonAndThreeBots(), Options(17));
        var connection = table.ConnectionFor(One);
        Exception? second = null;

        connection.Updated += _ =>
        {
            if (connection.Pending is not { } prompt)
            {
                return;
            }

            second ??= Record.Exception(table.PlayRound);
            connection.Answer(ScriptedSeat.Reply(prompt));
        };

        table.PlayRound();

        Assert.IsType<InvalidOperationException>(second);
    }

    /// <remarks>
    /// The engine's own rule (RULES.md §2.1), enforced when the table opens rather than at the
    /// first round: a table that cannot be dealt should fail while somebody is still setting
    /// it up.
    /// </remarks>
    [Fact]
    public void ATableTooSmallToDealIsRefusedWhenItOpens()
    {
        var seats = new[]
        {
            TableSeat.Person(One, "Nick"),
            TableSeat.Computer(Two, "Ruby (bot)", new GreedyBotAgent())
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => TableSession.Open(seats, Options(18)));
    }

    [Fact]
    public void TheSameSeatCannotBeSeatedTwice()
    {
        var seats = new[]
        {
            TableSeat.Person(One, "Nick"),
            TableSeat.Person(One, "Nick again"),
            TableSeat.Computer(Three, "Ruby (bot)", new GreedyBotAgent()),
            TableSeat.Computer(Four, "Sable (bot)", new GreedyBotAgent())
        };

        Assert.Throws<ArgumentException>(() => TableSession.Open(seats, Options(19)));
    }

    /// <summary>
    /// 🔥 <b>A seat is claimed exclusively, and that is why the claim lives here</b>
    /// (BUILD-PLAN P13.6).
    /// </summary>
    /// <remarks>
    /// Two viewers handed the same <see cref="SeatConnection"/> are two people answering one
    /// question — the first press wins and the second is refused, at random, for ever. A seat is
    /// a property of a table, so a table is what refuses the second person; a lobby decides
    /// which table and nothing else.
    /// </remarks>
    [Fact]
    public void ASeatIsTakenByOnePersonAtATime()
    {
        var table = TableSession.Open(TwoPeopleAndTwoBots(), Options(20260819));

        Assert.Equal([One, Three], table.RemoteSeats);
        Assert.Equal([One, Three], table.WaitingFor);
        Assert.False(table.IsFull);

        var mine = table.SitDown(One, "Nick");

        Assert.NotNull(mine);
        Assert.Equal(One, mine!.Player);
        Assert.True(table.IsTaken(One));
        Assert.Equal([Three], table.WaitingFor);

        // The same seat again is refused rather than shared.
        Assert.Null(table.SitDown(One, "Somebody else"));

        // A seat the computer plays is not a seat anybody sits in.
        Assert.Null(table.SitDown(Two, "Nick"));

        // …and the last one fills the table.
        Assert.NotNull(table.SitDown(Three, "Mya Lay"));
        Assert.True(table.IsFull);
        Assert.Empty(table.WaitingFor);
    }

    /// <remarks>
    /// ⚠️ <b>A seat is named when somebody sits in it, not when the table is opened.</b> A lobby
    /// opens a table before it knows who is coming, so <c>TableSeat.Person</c> can only carry a
    /// placeholder — and the real name reaches every board through the fan-out, like everything
    /// else at this table.
    /// </remarks>
    [Fact]
    public void SittingDownNamesTheSeatAndTellsTheWholeTable()
    {
        var table = TableSession.Open(
            [
                TableSeat.Person(One, "Seat 1"),
                TableSeat.Computer(Two, "Ruby (bot)", new GreedyBotAgent()),
                TableSeat.Computer(Three, "Sable (bot)", new GreedyBotAgent()),
                TableSeat.Computer(Four, "Onyx (bot)", new GreedyBotAgent())
            ],
            Options(20260819));

        var watcher = table.Watch();
        var mine = table.SitDown(One, "Nick");

        Assert.Equal("Nick", table.Names[One]);
        Assert.Equal("Nick", mine!.Name);

        var taken = Assert.IsType<TableEvent.SeatTaken>(Assert.Single(watcher.Events));

        Assert.Equal(One, taken.Player);
        Assert.Equal("Nick", taken.Name);

        // …and standing up says so, keeping the name: "Nick's seat is being played by the
        // computer" is the true thing to say and "Seat 1" is not (§3.11 C16).
        Assert.True(table.StandUp(One));
        Assert.False(table.StandUp(One));

        var left = Assert.IsType<TableEvent.SeatLeft>(watcher.Events[^1]);

        Assert.Equal(One, left.Player);
        Assert.Equal("Nick", left.Name);
        Assert.Equal("Nick", table.Names[One]);
        Assert.Equal([One], table.WaitingFor);
    }

    /// <summary>
    /// 🔥 <b>R8 — a handed-over seat is dead to its previous occupant, server-side.</b> The old
    /// connection used to keep receiving the new occupant's private draws and could still answer
    /// their prompts, held up only by the Web client disposing its board — exactly the
    /// client-hides-it posture the fan-out's own standard rejects.
    /// </summary>
    [Fact]
    public void AHandedOverSeatIsDeadToItsPreviousOccupant()
    {
        var table = TableSession.Open(TwoPeopleAndTwoBots(), Options(20260819));

        var first = table.SitDown(One, "Nick");
        Assert.True(table.StandUp(One));
        var second = table.SitDown(One, "Somebody else");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
        Assert.Same(second, table.ConnectionFor(One));

        // The new occupant plays the round; the other person's seat is scripted too.
        var succeeded = new ScriptedSeat(second!);
        _ = new ScriptedSeat(table.ConnectionFor(Three));
        var sizeBefore = first!.Events.Count;

        table.PlayRound();

        // The superseded connection heard nothing of the round — not one event, private or
        // public — and cannot answer; the new occupant was asked, answered, and was told its
        // own blind draws.
        Assert.Equal(sizeBefore, first.Events.Count);
        Assert.Null(first.Pending);
        Assert.False(first.Answer(new SeatAnswer.Take(TurnAction.DrawFromDeck)));
        Assert.True(succeeded.Answered > 0);
        Assert.Contains(
            second!.Events.OfType<TableEvent.Drew>(),
            drew => drew.Player == One && drew.Card is not null);
    }

    /// <remarks>
    /// A watcher is not a seat and never becomes one — the strictest concealment case there is
    /// (P13.2), and nothing P13.6 added may soften it.
    /// </remarks>
    [Fact]
    public void AWatcherIsNeverSeated()
    {
        var table = TableSession.Open(OnePersonAndThreeBots(), Options(20260819));
        var watcher = table.Watch();

        Assert.Null(watcher.Player);
        Assert.False(watcher.IsSeated);
        Assert.DoesNotContain(watcher, table.RemoteSeats.Select(table.ConnectionFor));
    }

    internal static TableOptions Options(int seed) => new()
    {
        Seed = seed,
        // Generous enough that a scripted seat never trips it, tight enough that a round which
        // will not finish fails the suite instead of hanging it.
        RoundTimeLimit = TimeSpan.FromMinutes(2)
    };

    internal static TableSeat[] TwoPeopleAndTwoBots() =>
    [
        TableSeat.Person(One, "Nick"),
        TableSeat.Computer(Two, "Ruby (bot)", new GreedyBotAgent()),
        TableSeat.Person(Three, "Mya Lay"),
        TableSeat.Computer(Four, "Sable (bot)", new GreedyBotAgent())
    ];

    private static TableSeat[] OnePersonAndThreeBots() =>
    [
        TableSeat.Person(One, "Nick"),
        TableSeat.Computer(Two, "Ruby (bot)", new GreedyBotAgent()),
        TableSeat.Computer(Three, "Sable (bot)", new GreedyBotAgent()),
        TableSeat.Computer(Four, "Onyx (bot)", new GreedyBotAgent())
    ];

    /// <summary>A card of the shoe this seat is definitely not holding.</summary>
    private static Card? NotHeld(SeatPrompt prompt) =>
        DeckBuilder.BuildTwoDecks().FirstOrDefault(card => !prompt.Holds(card));
}

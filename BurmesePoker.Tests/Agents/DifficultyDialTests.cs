using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Sim;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// ✅ <b>P19 — difficulty as a dial, not a list.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The design decision these tests are the shape of</b> (BUILD-PLAN §3.12): a skill
/// ladder is a research instrument and a difficulty dial is a product, and the same rungs
/// cannot be both. A ladder's rungs differ in exactly one decision, are unevenly spaced, are
/// entitled to be incomplete, and — <c>simple</c> being the example — play a <em>different and
/// worse idea</em> rather than the right idea badly. A dial must be monotone, must be
/// fine-grained enough for somebody to ask for <em>a bit easier</em>, and must read as a weaker
/// player. <b>So difficulty is the strongest rung with a mistake rate, and the ladder is what
/// the mistakes are made against.</b>
/// </para>
/// <para>
/// ⚠️ <b>How well each level actually plays is not tested here.</b> That is a measurement over
/// tens of thousands of games with an interval on it, it lives in
/// <c>docs/strategy/measurements.csv</c>, and <see cref="Sim.DifficultyCalibrationTests"/> is
/// the affordable re-run that checks the published ordering has not silently inverted.
/// </para>
/// </remarks>
public class DifficultyDialTests
{
    private static readonly PlayerId Alice = new(0);
    private static readonly PlayerId[] FourPlayers = [.. Enumerable.Range(0, 4).Select(seat => new PlayerId(seat))];

    /// <summary>Twelve that partition, and the Q♣, which joins nothing — the P10 fixture.</summary>
    private static readonly string[] TwelveAndADeadQueen =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QC"];

    /// <summary>Every level, by the name a menu and a command line both know it as.</summary>
    public static TheoryData<string> Levels => ["easy", "medium", "hard", "expert"];

    /// <summary>
    /// The dial, weakest first, as every CSV row and journal header spells it.
    /// </summary>
    /// <remarks>
    /// Written out rather than derived, for the reason <c>BotCatalogTests.Ladder</c> is: a test
    /// that read the list to check the list would agree with any rename at all, and a name is
    /// half of every published number's join key (BUILD-PLAN §3.8 item 4).
    /// </remarks>
    private static readonly string[] Dial = ["easy", "medium", "hard", "expert"];

    [Fact]
    public void TheDialIsFourLevelsWeakestFirst()
    {
        Assert.Equal(Dial, DifficultyLadder.All.Select(level => level.Name));

        // 🔥 Strongest first is what a menu is drawn in, and the head of it is the default —
        // a SelectionPrompt opens on the first entry drawn (P18's finding, which would now be
        // a difficulty dial silently defaulting to its bottom).
        Assert.Equal(Dial.Reverse(), DifficultyLadder.ByStrength.Select(level => level.Name));
        Assert.Same(DifficultyLadder.ByStrength[0], DifficultyLadder.Hardest);
        Assert.Same(DifficultyLadder.Hardest, DifficultyLadder.Default);
        Assert.Equal(0, DifficultyLadder.Hardest.MistakeRate);
    }

    /// <remarks>
    /// ⚠️ <b>Monotone by construction, which is the half of "monotone" a test can hold.</b> The
    /// other half is measured, and is <see cref="Sim.DifficultyCalibrationTests"/>.
    /// </remarks>
    [Fact]
    public void EachLevelSlipsLessOftenThanTheOneBelowIt()
    {
        var rates = DifficultyLadder.All.Select(level => level.MistakeRate).ToList();

        Assert.Equal(rates.OrderByDescending(rate => rate), rates);
        Assert.Equal(rates.Distinct().Count(), rates.Count);
        Assert.All(rates, rate => Assert.InRange(rate, 0, 1));

        // Every level is the strongest rung there is, handicapped. A rung that raises the
        // ceiling raises the whole dial and moves the calibration (§3.12 item 1).
        Assert.All(DifficultyLadder.All, level => Assert.Same(BotCatalog.Hardest, level.Rung));
    }

    /// <summary>
    /// 🔥 <b>A level is not a rung, and no name belongs to both.</b>
    /// </summary>
    /// <remarks>
    /// <c>--strategies</c> resolves rungs, levels and probes out of one flag, so a name in two
    /// lists would silently be one of them; and a front end that offered both lists is the one
    /// thing §3.12 forbids.
    /// </remarks>
    [Fact]
    public void NoNameIsBothARungAndALevel()
    {
        Assert.Empty(DifficultyLadder.All.Select(level => level.Name)
            .Intersect(BotCatalog.All.Select(rung => rung.Name), StringComparer.OrdinalIgnoreCase));

        Assert.All(BotCatalog.All, rung => Assert.Null(DifficultyLadder.Find(rung.Name)));
        Assert.All(DifficultyLadder.All, level => Assert.Null(BotCatalog.Find(level.Name)));
    }

    [Theory]
    [MemberData(nameof(Levels))]
    public void ALevelResolvesByNameWhateverCaseItIsWrittenIn(string written)
    {
        Assert.Equal(written, DifficultyLadder.Resolve(written).Name);
        Assert.Equal(written, DifficultyLadder.Resolve(written.ToUpperInvariant()).Name);
        Assert.NotNull(DifficultyLadder.Find(written));
        Assert.Equal(written, StrategyCatalog.Resolve(written).Name);
    }

    [Fact]
    public void ANameNobodyKnowsIsRefusedRatherThanGuessedAt()
    {
        Assert.Null(DifficultyLadder.Find("gentle"));
        Assert.Null(DifficultyLadder.Find(null));

        var complaint = Assert.Throws<ArgumentException>(() => DifficultyLadder.Resolve("gentle"));

        Assert.Contains("easy", complaint.Message, StringComparison.Ordinal);
        Assert.Contains("expert", complaint.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⚠️ <b>A reserved character is a rule and not a tidiness</b>, for the reason
    /// <c>BotRung.Reserved</c> is: <c>#</c> labels the copy the harness makes of a strategy and
    /// <c>@</c> names a calibration probe. Neither is a setting anybody should be offered.
    /// </summary>
    [Fact]
    public void NoLevelCanWearALabelThatIsNotAWayOfPlaying()
    {
        Assert.All(DifficultyLadder.All, level =>
        {
            Assert.DoesNotContain(BotRung.Reserved, level.Name);
            Assert.DoesNotContain(DifficultyLevel.Reserved, level.Name);
        });

        // 🔥 Checked in the accessor rather than in an initialiser, which is the difference
        // between a rule and a suggestion — `with` copies the backing fields and then sets
        // what changed, so an initialiser would let a copy straight through (P18's finding).
        Assert.Throws<ArgumentException>(
            () => DifficultyLadder.Hardest with { Name = "expert" + Tournament.MirrorSuffix });
        Assert.Throws<ArgumentException>(
            () => DifficultyLadder.Hardest with { Name = $"expert{DifficultyLevel.Reserved}notarate" });

        // …and the one shape that carries it is allowed, because that is what a probe is called.
        Assert.Equal("greedy@0.35", DifficultyLevel.Probe(BotCatalog.Resolve("greedy"), 0.35).Name);
    }

    /// <summary>
    /// 🔥 <b>The instrument the ε values were found with, and it is deliberately not a
    /// level.</b>
    /// </summary>
    /// <remarks>
    /// §3.12's closing rule is that a published figure carries the command that made it. ε is a
    /// dial that can be turned to any value, so the sweep that placed these four has to be
    /// re-runnable — and it must be impossible for a form field or a query string to reach an
    /// uncalibrated opponent through the same door.
    /// </remarks>
    [Fact]
    public void ACalibrationProbeIsNameableByTheHarnessAndByNobodyElse()
    {
        var probe = DifficultyLadder.FindOrProbe("greedy@0.35");

        Assert.NotNull(probe);
        Assert.Equal("greedy@0.35", probe!.Name);
        Assert.Equal(0.35, probe.MistakeRate);
        Assert.Same(BotCatalog.Resolve("greedy"), probe.Rung);
        Assert.Equal("greedy@0.35", StrategyCatalog.Resolve("greedy@0.35").Name);

        // A front end asks Find, which knows the four names and nothing else.
        Assert.Null(DifficultyLadder.Find("greedy@0.35"));

        Assert.Throws<ArgumentException>(() => DifficultyLadder.FindOrProbe("greedy@2"));
        Assert.Throws<ArgumentException>(() => DifficultyLadder.FindOrProbe("greedy@rubbish"));
        Assert.Throws<ArgumentException>(() => DifficultyLadder.FindOrProbe("nobody@0.5"));
    }

    /// <remarks>
    /// A spread is what "a mixed table" means (P19): strongest first, cycling, so one list fits
    /// whatever shape the table turns out to be.
    /// </remarks>
    [Fact]
    public void ASpreadDealsTheDialOutStrongestFirstAndCycles()
    {
        Assert.Equal(["expert", "hard", "medium"], DifficultyLadder.Spread(3).Select(level => level.Name));
        Assert.Equal(
            ["expert", "hard", "medium", "easy", "expert"],
            DifficultyLadder.Spread(5).Select(level => level.Name));
        Assert.Empty(DifficultyLadder.Spread(0));
    }

    /// <summary>
    /// ✅ <b>P19 acceptance 3 — a decorator that does nothing is transparent.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>The cheap mutation-proof test of the whole mechanism.</b> If ε = 0 played even one
    /// card differently from the bare rung, every published number for the top of the dial
    /// would be measuring the decorator rather than the player — and the console half of this
    /// is a <c>cmp</c> of two <c>drive-console.py</c> captures.
    /// </remarks>
    [Fact]
    public void AtZeroMistakesTheDecoratorPlaysExactlyTheRungItWraps()
    {
        var bare = Played(new GreedyBotAgent());
        var wrapped = Played(new FallibleAgent(new GreedyBotAgent(), 0, new Random(1)));
        var levelled = Played(DifficultyLadder.Resolve("expert").Create(4242));

        Assert.Equal(bare, wrapped);
        Assert.Equal(bare, levelled);

        // And it is not vacuous: a level that does slip plays a different game from the same
        // deal.
        Assert.NotEqual(bare, Played(DifficultyLadder.Resolve("easy").Create(4242)));
    }

    /// <summary>
    /// ✅ <b>§3.12 item 3 — a mistake is a plausible move, not a random one.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>A bot that threw jokers away would read as broken rather than as weak.</b> A weaker
    /// human plays the right idea and slips, so the mistake is the next card down the agent's
    /// <em>own</em> ordering — which is why <c>CoverScore.Discard</c> had to become the head of
    /// a ranking rather than a second loop that agrees with one.
    /// </remarks>
    [Fact]
    public void AMistakeIsTheCardTheRungItselfRankedSecond()
    {
        // ⚠️ The ranking is taken during the turn, off the seat that played it. A TurnContext's
        // hand is the engine's own list, so a context kept and asked afterwards is asked about
        // the thirteen — P13.1's finding, and it is what made this test fail first time.
        var slipping = Watch(new FallibleAgent(new GreedyBotAgent(), 1.0, new Random(7)), 1, "3C");
        var thinking = Watch(new GreedyBotAgent(), 1, "3C");

        var ranked = slipping.Rankings[0];

        Assert.True(ranked.Length > 1);
        Assert.Equal(ranked[1], slipping.Discards[0]);
        Assert.NotEqual(ranked[0], slipping.Discards[0]);

        // The head of the ranking is exactly what the rung throws, which is what makes "second
        // best" mean anything at all — and it is why CoverScore.Discard is defined as the head
        // of the ranking rather than as a second loop that agrees with one.
        Assert.True(ranked[0].SameValueAs(thinking.Discards[0]));
        Assert.True(ranked[0].SameValueAs(thinking.Rankings[0][0]));

        // Two copies of one value are one move — they leave the same thirteen behind — so a
        // ranking is shorter than the hand whenever it holds a pair.
        Assert.Equal(14, slipping.HandsWhenDiscarding[0].Length);
        Assert.All(ranked, card => Assert.Equal(1, ranked.Count(other => other.SameValueAs(card))));
    }

    /// <summary>
    /// ⚠️ <b>A level that could not be asked for its second-best move would be an ε that
    /// silently did nothing</b>, which is worse than no level: a menu entry that changes
    /// nothing is a lie told to everybody who reads it.
    /// </summary>
    [Fact]
    public void APlayerThatCannotRankItsOwnDiscardsCannotCarryAMistakeRate()
    {
        var complaint = Assert.Throws<ArgumentException>(
            () => new FallibleAgent(new RandomBotAgent(new Random(1)), 0.5, new Random(1)));

        Assert.Contains("second-best", complaint.Message, StringComparison.Ordinal);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FallibleAgent(new GreedyBotAgent(), 1.5, new Random(1)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FallibleAgent(new GreedyBotAgent(), -0.1, new Random(1)));
    }

    /// <summary>
    /// ⚠️ <b>It never fumbles a declaration, and never slips on taking or claiming.</b>
    /// </summary>
    /// <remarks>
    /// Refusing a won hand is not a worse player but a different game — <c>RandomBotAgent</c>
    /// settles that and the reasoning carries. Taking or leaving the discard is a
    /// strict-improvement question with no second-best answer, so <b>the mistake has exactly
    /// one site</b>, which is what keeps ε one dial rather than three.
    /// </remarks>
    [Fact]
    public void TheOnlyThingItGetsWrongIsWhichCardToThrow()
    {
        var inner = new Deferring();
        var always = new FallibleAgent(inner, 1.0, new Random(1));

        Assert.True(always.Declare(null!));
        Assert.True(always.ClaimTurnedUpMoneyCard(null!));
        Assert.Equal(TurnAction.TakeDiscard, always.ChooseAction(null!));
        Assert.Equal(3, inner.Asked);
    }

    /// <summary>
    /// ✅ <b>P19 acceptance 5 — <c>MoneyCardsDoNotChangeWhatABotThrowsAway</c> holds for the
    /// decorator too.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A new wrapper is the likeliest place for money to leak into a discard</b>
    /// (RULES.md §4.4): ownership never transfers, so a money card pays the player the deck
    /// gave it to whether they are holding it or threw it away on turn one. The two rounds
    /// below differ in nothing but which card is turned up.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Levels))]
    public void MoneyCardsDoNotChangeWhatALevelThrowsAway(string level)
    {
        var ordinary = Turn(DifficultyLadder.Resolve(level).Create(4242), turnedUpFromTop: "3C");
        var itsAMoneyCard = Turn(DifficultyLadder.Resolve(level).Create(4242), turnedUpFromTop: "QC");

        Assert.True(ordinary.Discard.SameValueAs(itsAMoneyCard.Discard));

        Assert.Equal(0, ordinary.Money.Multiplier(ordinary.Discard));
        Assert.Equal(1, itsAMoneyCard.Money.Multiplier(Hands.Value("QC")));
    }

    /// <summary>
    /// ✅ <b>P19 acceptance 4 — every level is deterministic from its seat's seed.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>One careless decorator takes every other seat's reproducibility down with it</b>
    /// (BUILD-PLAN §3.7 item 1), and this one is seated at three or four seats of every table
    /// this project deals. A level that reached for <see cref="Random.Shared"/> would pass
    /// every other test in this file.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Levels))]
    public void ALevelPlaysTheSameGameFromTheSameSeed(string level)
    {
        var once = Played(DifficultyLadder.Resolve(level).Create(4242));
        var again = Played(DifficultyLadder.Resolve(level).Create(4242));

        Assert.Equal(once, again);

        // ⚠️ And the seed is really an input rather than decoration — some other seed plays a
        // different game. It is "some" and not "the next one" on purpose: a level that slips
        // nine times in ten slips on everything under most seeds, so demanding that any
        // particular seed differ would be a coin toss dressed as an assertion.
        int[] others = [4243, 4244, 4245, 4246];

        if (DifficultyLadder.Resolve(level).MistakeRate > 0)
        {
            Assert.Contains(others, seed => Played(DifficultyLadder.Resolve(level).Create(seed)) != once);
        }
        else
        {
            // `expert` never slips, which is exactly what acceptance 3 says it must not do, so
            // it is the one level for which every seed is the same game.
            Assert.All(others, seed => Assert.Equal(once, Played(DifficultyLadder.Resolve(level).Create(seed))));
        }
    }

    /// <summary>An inner player that answers the same way every time, and counts the asking.</summary>
    private sealed class Deferring : IPlayerAgent, IRanksDiscards
    {
        public int Asked { get; private set; }

        public TurnAction ChooseAction(TurnContext context)
        {
            Asked++;
            return TurnAction.TakeDiscard;
        }

        public Card ChooseDiscard(TurnContext context)
        {
            Asked++;
            throw new InvalidOperationException("Not asked for in this test.");
        }

        public bool ClaimTurnedUpMoneyCard(TurnContext context)
        {
            Asked++;
            return true;
        }

        public bool Declare(TurnContext context)
        {
            Asked++;
            return true;
        }

        public IReadOnlyList<Card> RankDiscards(TurnContext context)
        {
            Asked++;
            return [];
        }
    }

    /// <summary>
    /// What one seat did over its first several turns of one deal, as text to compare.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The cap is the <em>round's</em> turn number and not the seat's</b>, so twenty
    /// turns at a table of four is five decisions from the seat being watched — which is what
    /// makes a determinism test more than a coin toss. A one-turn capture of a level that slips
    /// nine times in ten agrees with the wrong seed 81% of the time.
    /// </remarks>
    private static string Played(IPlayerAgent agent)
    {
        var watched = Watch(agent, turnCap: 20, turnedUpFromTop: "3C");

        return string.Join(
            " ",
            watched.Claims.Select(claim => claim.ToString())
                .Concat(watched.Actions.Select(action => action.ToString()))
                .Concat(watched.Discards.Select(discard => discard.ToString())));
    }

    /// <summary>One seat's opening turn: what it threw, and what the money was.</summary>
    private static (Card Discard, MoneyCardRegistry Money) Turn(IPlayerAgent agent, string turnedUpFromTop)
    {
        var watched = Watch(agent, turnCap: 1, turnedUpFromTop, out var money);

        return (watched.Discards[0], money);
    }

    private static RecordingAgent Watch(IPlayerAgent agent, int turnCap, string turnedUpFromTop) =>
        Watch(agent, turnCap, turnedUpFromTop, out _);

    /// <summary>
    /// The P10 fixture, played until the cap.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Every seat is capped</b>, because only a declaration ends a round (RULES.md §7.1):
    /// a table of players that never go out passes the cards round for ever, and a test without
    /// a bound is a test that can hang the suite.
    /// </remarks>
    private static RecordingAgent Watch(
        IPlayerAgent agent,
        int turnCap,
        string turnedUpFromTop,
        out MoneyCardRegistry money)
    {
        var watched = new RecordingAgent(agent);

        var engine = new RoundEngine(
            FourPlayers,
            FourPlayers.ToDictionary(
                player => player,
                IPlayerAgent (player) => player == Alice
                    ? new SeatRecorder(watched, turnCap)
                    : new SeatRecorder(new GreedyBotAgent(), turnCap)),
            Stakes.Standard,
            DealBuilder.ForPlayers(4)
                .Give(0, TwelveAndADeadQueen)
                .ThenDraw("2C")
                .TurnUpFromTop(turnedUpFromTop)
                .TurnUpFromBottom("4C")
                .Build(),
            new Random(1));

        // ⚠️ Either ending is fine and the cap is what makes that true: at twenty turns this
        // fixture usually reaches a declaration, and at one turn it never can. What must not
        // happen is a round that runs for ever, which is what only a declaration ending a round
        // (RULES.md §7.1) otherwise allows.
        try
        {
            engine.Play();
        }
        catch (RoundAbandonedException)
        {
        }

        Assert.NotEmpty(watched.Discards);

        money = engine.Table.MoneyCards;
        return watched;
    }
}

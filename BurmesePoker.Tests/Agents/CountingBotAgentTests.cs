using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Sim;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// The rung that remembers (BUILD-PLAN P20) — and the three things a rung that remembers owes
/// that the four before it did not.
/// </summary>
/// <remarks>
/// <para>
/// <b>How well it plays is not here.</b> That is a measurement over thousands of games with an
/// interval on it, published in <c>docs/STRATEGY.md</c> from <c>docs/strategy/measurements.csv</c>.
/// What belongs in a test is what no run of any size could settle: <em>what it is allowed to
/// know</em>, <em>that it forgets between deals</em>, and <em>that memory is the only thing
/// separating it from the rung below</em>.
/// </para>
/// <para>
/// ⚠️ <b>Acceptance 2 is stated one way in the plan and asserted another, and the difference is
/// real.</b> The plan asks that "the bot's information set is a strict subset of a watcher's".
/// It is not, quite: a seat sees its own blind draws and a watcher does not (<c>TableFanOut</c>
/// — exactly one event is private, and it is the draw). What is true, and what is asserted
/// below, is the claim that matters: <b>a counting seat remembers exactly the cards it was
/// shown</b> — its own hand and the one discard offered to it — and <b>nothing else that
/// happened at the table</b>, though a watcher saw plenty of it.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class CountingBotAgentTests
{
    private static readonly PlayerId Alice = new(0);
    private static readonly PlayerId[] FourPlayers = [.. Enumerable.Range(0, 4).Select(seat => new PlayerId(seat))];

    /// <summary>Twelve that partition, and the Q♣, which joins nothing — the P10 fixture.</summary>
    private static readonly string[] TwelveAndADeadQueen =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QC"];

    [Fact]
    public void ACountingSeatRemembersExactlyTheCardsItWasShown()
    {
        // ✅ P20 acceptance 2. RULES.md §9 #15 — whether a discard pile may be read, or only
        // its top card — is open, so the safe default is that this seat counts only what has
        // actually been put in front of it: its own hand at each decision, everything that
        // passed through it, and the one discard offered to it each turn whether or not it
        // took it. Wrong in the direction that makes it weak, never in the one that cheats.
        var (bot, shown, table) = SixTurns();

        Assert.Equal(shown.Ids.OrderBy(id => id.Value), bot.Remembered.OrderBy(id => id.Value));

        // And it really was shown a good deal — a memory of thirteen would pass the line above
        // while remembering nothing at all.
        Assert.True(bot.Remembered.Count > 13, $"remembered only {bot.Remembered.Count} cards.");
    }

    [Fact]
    public void ACountingSeatDoesNotRememberWhatTheRestOfTheTableThrewAway()
    {
        // The other half of acceptance 2, and the half that would catch a bot reaching past
        // its own seat: the discards are public (RULES.md §6.3) and a watcher of the table
        // sees every one of them (TableFanOut), but only the seat before this one hands it
        // anything. Everything the other two threw is public and unremembered.
        var (bot, shown, table) = SixTurns();

        var elsewhere = table.Discards
            .Where(thrown => thrown.Player != Alice && thrown.Player != Upstream)
            .Select(thrown => thrown.Card)
            .Where(card => !shown.Ids.Contains(card.Id))
            .ToList();

        Assert.NotEmpty(elsewhere);
        Assert.DoesNotContain(elsewhere, card => bot.Remembered.Contains(card.Id));
    }

    [Fact]
    public void ACountingSeatDoesNotCountTheTurnedUpMoneyCards()
    {
        // RULES.md §4.4 outranks the strength it would be worth. The turned-up cards are
        // public and this seat has plainly seen them, but counting them would make which card
        // designates money an input to a discard — and MoneyCardsDoNotChangeWhatABotThrowsAway
        // (SkillLadderTests, which now covers this rung too) is the behaviour that follows.
        var (bot, shown, table) = SixTurns();

        var untouched = table.TurnedUp.Where(card => !shown.Ids.Contains(card.Id)).ToList();

        Assert.NotEmpty(untouched);
        Assert.DoesNotContain(untouched, card => bot.Remembered.Contains(card.Id));
    }

    [Fact]
    public void TheSupplyEstimateFallsBelowWhatTheRungBelowWouldSay()
    {
        // 🔥 This is the whole of the difference between counting and cautious, and the only
        // test that shows it doing anything: cautious answers "how many are left?" from its
        // own hand, so a card it threw away four turns ago is available again. This one has
        // watched it go.
        var (bot, shown, table) = SixTurns();

        var gone = shown.Cards
            .Where(card => !card.IsJoker)
            .Where(card => !shown.FinalHand.Exists(held => held.SameValueAs(card)))
            .ToList();

        Assert.NotEmpty(gone);

        // Cautious would say two of each of these are out there, because none is in its hand.
        Assert.Contains(gone, card => bot.Available(card.Rank!.Value, card.Suit!.Value) < 2);

        // And every value it has never been shown is still worth the full two — a memory that
        // decayed towards zero would pass the line above and be worthless.
        var neverShown = AllValues()
            .Where(value => !shown.Cards.Any(card => card.SameValueAs(value)))
            .ToList();

        Assert.NotEmpty(neverShown);
        Assert.All(neverShown, value => Assert.Equal(2, bot.Available(value.Rank!.Value, value.Suit!.Value)));
    }

    [Fact]
    public void ACountingSeatForgetsEverythingWhenTheDealMovesOn()
    {
        // ✅ P20 acceptance 3, and the trap the packet names: a CardId names a card in a
        // *round's* shoe, which is rebuilt at every deal (P13.4). A memory that survived a
        // deal would not be stale — it would be a memory of cards that no longer exist. So
        // this cannot be asserted by comparing ids across rounds, which is precisely why it is
        // asserted on the size of the memory at the first decision of the second round.
        var bot = new CountingBotAgent();
        var probe = new MemoryProbe(bot);

        PlayCappedRound(probe, round: 1, turnCap: TableTurns, AnOrdinaryDeal());
        var afterOne = probe.Sizes[^1];

        // The same deal a second time, so the two rounds are directly comparable — and so that
        // every CardId the memory holds is a live one in the round that follows, which is what
        // makes a memory that survived the deal *look* plausible rather than crash.
        PlayCappedRound(probe, round: 2, turnCap: TableTurns, AnOrdinaryDeal());
        var openingTheSecond = probe.SizesByRound[2][0];

        // Round one leaves it holding far more than a hand.
        Assert.True(afterOne > 20, $"only {afterOne} cards remembered by the end of round one; sizes {string.Join(",", probe.Sizes)}.");

        // Round two opens with a hand and nothing else. Delete the clear in Forget and this is
        // the line that goes red — the memory simply carries on where it left off.
        Assert.True(
            openingTheSecond <= 13,
            $"round two opened remembering {openingTheSecond} cards, which is more than a hand.");
    }

    [Fact]
    public void ACountingSeatOpensARoundPlayingTheCautiousCard()
    {
        // The mechanism's transparency test, and P15's "one change" made mechanical: before it
        // has been shown anything but its own thirteen, "two, less the ones I have seen" and
        // "two, less the ones I hold" are the same number — so the two rungs are the same
        // player, and any difference in results between them is memory and nothing else.
        Assert.True(OpeningDiscard("counting").SameValueAs(OpeningDiscard("cautious")));

        // And greedy, whose tie-break this rung still runs first, threw something else — so
        // the line above is not merely "every rung agrees on this hand".
        Assert.False(OpeningDiscard("counting").SameValueAs(OpeningDiscard("greedy")));
    }

    [Fact]
    public void ACountingRunJournalsAndReplaysToTheSameRows()
    {
        // ✅ P20 acceptance 4. It is the first rung that carries state, so it is the first that
        // could make a game depend on the order the harness happened to schedule it in, or
        // make a journal a record of a game nobody played. Two rounds a game, so the round
        // boundary is inside the claim.
        var run = new SimulationOptions
        {
            Strategies = [StrategyCatalog.Resolve("counting"), StrategyCatalog.Resolve("greedy")],
            Seats = 4,
            Games = 8,
            RoundsPerGame = 2,
            MasterSeed = 20260819
        };

        var played = Simulator.Run(run with { Journal = JournalFidelity.Thin });

        Assert.Equal(
            CsvReport.Rows(Simulator.Run(run)).ToList(),
            CsvReport.Rows(played).ToList());

        Assert.Equal(
            CsvReport.Rows(played).ToList(),
            CsvReport.Rows(Replay.Run([.. played.Games.Select(game => game.Journal!)])).ToList());
    }

    /// <summary>The seat that feeds Alice — the only one whose discards ever reach her.</summary>
    private static readonly PlayerId Upstream = new(3);

    /// <summary>What a counting seat was put in front of, gathered by a decorator.</summary>
    private sealed class Shown(IPlayerAgent inner) : IPlayerAgent
    {
        private readonly Dictionary<CardId, Card> _cards = [];

        public IReadOnlyCollection<CardId> Ids => _cards.Keys;

        public IReadOnlyCollection<Card> Cards => _cards.Values;

        public List<Card> FinalHand { get; private set; } = [];

        public TurnAction ChooseAction(TurnContext context)
        {
            Watch(context);
            return inner.ChooseAction(context);
        }

        public Card ChooseDiscard(TurnContext context)
        {
            Watch(context);
            return inner.ChooseDiscard(context);
        }

        public bool ClaimTurnedUpMoneyCard(TurnContext context)
        {
            Watch(context);
            return inner.ClaimTurnedUpMoneyCard(context);
        }

        public bool ObjectToClaim(TurnContext context)
        {
            Watch(context);
            return inner.ObjectToClaim(context);
        }

        public bool Declare(TurnContext context)
        {
            Watch(context);
            return inner.Declare(context);
        }

        /// <remarks>
        /// Exactly what <c>CountingBotAgent.Observe</c> is entitled to, written out
        /// independently rather than shared with it — a fixture that called the same code
        /// would agree with any information set at all. Jokers are skipped because the supply
        /// estimate is only ever asked about a ranked value.
        /// </remarks>
        private void Watch(TurnContext context)
        {
            FinalHand = [.. context.Hand];

            foreach (var card in context.Hand.Concat(context.AvailableDiscard is { } offered ? [offered] : []))
            {
                if (!card.IsJoker)
                {
                    _cards[card.Id] = card;
                }
            }
        }
    }

    /// <summary>Writes down how much the bot remembered at each decision, round by round.</summary>
    private sealed class MemoryProbe(CountingBotAgent inner) : IPlayerAgent
    {
        public List<int> Sizes { get; } = [];

        public Dictionary<int, List<int>> SizesByRound { get; } = [];

        public TurnAction ChooseAction(TurnContext context)
        {
            var action = inner.ChooseAction(context);
            Note(context);
            return action;
        }

        public Card ChooseDiscard(TurnContext context)
        {
            var discard = inner.ChooseDiscard(context);
            Note(context);
            return discard;
        }

        public bool ClaimTurnedUpMoneyCard(TurnContext context)
        {
            var claimed = inner.ClaimTurnedUpMoneyCard(context);
            Note(context);
            return claimed;
        }

        public bool ObjectToClaim(TurnContext context)
        {
            var objected = inner.ObjectToClaim(context);
            Note(context);
            return objected;
        }

        public bool Declare(TurnContext context)
        {
            var declared = inner.Declare(context);
            Note(context);
            return declared;
        }

        /// <remarks>
        /// <b>After the inner call, never before.</b> The memory is taken in as the decision is
        /// made, so a probe that read it first would be reading the turn before — which is
        /// P19's <c>RecordingAgent</c> finding wearing a different hat.
        /// </remarks>
        private void Note(TurnContext context)
        {
            Sizes.Add(inner.Remembered.Count);

            if (!SizesByRound.TryGetValue(context.Round, out var sizes))
            {
                SizesByRound[context.Round] = sizes = [];
            }

            sizes.Add(inner.Remembered.Count);
        }
    }

    /// <summary>Every discard anybody made, and what was turned up — a watcher's view.</summary>
    private sealed class Public : IGameObserver
    {
        public List<(PlayerId Player, Card Card)> Discards { get; } = [];

        public List<Card> TurnedUp { get; } = [];

        public void PlayerDiscarded(PlayerId player, Card card) => Discards.Add((player, card));

        public void RoundStarted(int round, IReadOnlyList<PlayerId> seating, IReadOnlyList<Card> turnedUp) =>
            TurnedUp.AddRange(turnedUp);
    }

    /// <summary>Six turns of a real round with a counting seat, and everything it was shown.</summary>
    /// <remarks>
    /// ⚠️ <b>An ordinary deal, and that is not incidental.</b> The P10 fixture below hands
    /// Alice twelve cards that partition, so she declares on the opening turn and the round is
    /// over before there is anything to remember — which is how the first draft of every test
    /// in this class failed.
    /// </remarks>
    private static (CountingBotAgent Bot, Shown Shown, Public Table) SixTurns()
    {
        var bot = new CountingBotAgent();
        var shown = new Shown(bot);
        var table = PlayCappedRound(shown, round: 1, turnCap: TableTurns, AnOrdinaryDeal());

        return (bot, shown, table);
    }

    /// <summary>
    /// How many turns the whole table takes before the round is abandoned.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>It is the round's turn number and not this seat's</b> —
    /// <see cref="SeatRecorder"/> bounds <c>TurnContext.TurnNumber</c>, which counts turns
    /// around the table. Twenty-five is six turns for Alice at a table of four, and short
    /// enough that a table of greedy bots has not declared: P12 measured 27.9 turns a round.
    /// </remarks>
    private const int TableTurns = 25;

    /// <summary>All fifty-two ranked values, one of each.</summary>
    private static IEnumerable<Card> AllValues() =>
        from rank in Enum.GetValues<Rank>()
        from suit in Enum.GetValues<Suit>()
        select Card.Ranked(new CardId(-1), rank, suit);

    /// <summary>A deal nobody can win from quickly: whatever the builder's filler hands out.</summary>
    private static IReadOnlyList<Card> AnOrdinaryDeal() =>
        DealBuilder.ForPlayers(4).TurnUpFromTop("3C").TurnUpFromBottom("4C").Build();

    /// <summary>The P10 fixture: twelve that partition, the dead Q♣, and the 2♣ drawn.</summary>
    private static IReadOnlyList<Card> TheOpeningFixture() =>
        DealBuilder.ForPlayers(4)
            .Give(0, TwelveAndADeadQueen)
            .ThenDraw("2C")
            .TurnUpFromTop("3C")
            .TurnUpFromBottom("4C")
            .Build();

    /// <summary>Plays a round with the given agent at Alice's seat, abandoned at the turn cap.</summary>
    /// <remarks>
    /// Only a declaration ends a round (RULES.md §7.1), so a round without a cap is a test that
    /// can hang the suite — <see cref="SeatRecorder"/> is the bound the harness uses and the
    /// one used here.
    /// </remarks>
    private static Public PlayCappedRound(
        IPlayerAgent watched,
        int round,
        int turnCap,
        IReadOnlyList<Card> deal)
    {
        var observer = new Public();

        var engine = new RoundEngine(
            FourPlayers,
            FourPlayers.ToDictionary(
                player => player,
                IPlayerAgent (player) => player == Alice
                    ? new SeatRecorder(watched, turnCap)
                    : new SeatRecorder(StrategyCatalog.Resolve("greedy").Create(7), turnCap)),
            Stakes.Standard,
            deal,
            new Random(round),
            round,
            observer);

        Assert.Throws<RoundAbandonedException>(() => engine.Play());

        return observer;
    }

    /// <summary>What the named rung throws on the opening turn of the P10 fixture.</summary>
    private static Card OpeningDiscard(string rung)
    {
        var watched = new RecordingAgent(StrategyCatalog.Resolve(rung).Create(4242));

        PlayCappedRound(watched, round: 1, turnCap: 1, TheOpeningFixture());

        return watched.Discards[0];
    }
}

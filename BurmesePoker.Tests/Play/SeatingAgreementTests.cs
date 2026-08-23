using System.Reflection;

using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Sim;

namespace BurmesePoker.Tests.Play;

/// <summary>
/// ✅ <b>Packet P37: a held seating is re-drawn when the players agree to it</b> (RULES.md §3
/// step 2, §9 #45; §10 #23).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Three answers, because consent is not desire.</b> P36 held the seating and left the
/// changing owed; §3 says the seats change <em>when the players agree</em>, which is somebody
/// wanting it and nobody objecting. A yes-or-no question cannot say that — a computer seat must
/// consent (BUILD-PLAN §3.13), and a table of consenting bots answering <em>yes</em> would
/// re-seat itself every deal.
/// </para>
/// <para>
/// ⚠️ <b>The interesting failure is the quiet one.</b> The sixth question is the first member of
/// <see cref="IPlayerAgent"/> with a default implementation, so a decorator that forgets to
/// forward it answers <em>consent</em> in its own name and silently drops what it wraps.
/// </para>
/// </remarks>
public class SeatingAgreementTests
{
    private static readonly PlayerId[] FourPlayers =
        [.. Enumerable.Range(1, 4).Select(seat => new PlayerId(seat))];

    /// <summary>
    /// ✅ <b>Nobody asked, so nothing moved</b> — including at a table where every seat is the
    /// computer's, which is the case the consent decision could most easily have broken.
    /// </summary>
    [Fact]
    public void ATableOfConsentingSeatsNeverChangesItsOwn()
    {
        var observer = new RecordingObserver();
        var match = Match(observer, _ => new GreedyBotAgent());

        for (var round = 0; round < 6; round++)
        {
            match.PlayRound();
        }

        Assert.All(observer.Seatings, seating => Assert.Equal(FourPlayers, seating));
        Assert.Equal(FourPlayers, match.Seating);
    }

    /// <summary>
    /// ✅ <b>Acceptance 1 and 5: somebody asks, nobody refuses, and the next deal is to a new
    /// seating</b> — with the asking seat the only person at a table of three bots.
    /// </summary>
    [Fact]
    public void SomebodyAsksAndTheSeatsChangeBeforeTheNextDeal()
    {
        var observer = new RecordingObserver();
        var asker = new SaysAboutTheSeating(SeatingOpinion.Ask, onlyBefore: 3);

        var match = Match(
            observer,
            player => player == FourPlayers[0] ? asker : new GreedyBotAgent());

        for (var round = 0; round < 4; round++)
        {
            match.PlayRound();
        }

        // Round 1 is the opening seating; the question before round 3 is the only one asked for.
        Assert.Equal(FourPlayers, observer.Seatings[0]);
        Assert.Equal(FourPlayers, observer.Seatings[1]);
        Assert.NotEqual(FourPlayers, observer.Seatings[2]);

        // …and it holds after it, because asking is not a standing arrangement.
        Assert.Equal(observer.Seatings[2], observer.Seatings[3]);

        // The whole table is told, by name, and told once.
        Assert.Equal([$"{FourPlayers[0]} asked"], observer.SeatingChanges);
        Assert.Empty(observer.SeatingRefusals);
    }

    /// <summary>
    /// ⚠️ <b>§9 #47 — <em>agree</em> means everybody, and that is a recorded default rather than
    /// a settled rule.</b>
    /// </summary>
    /// <remarks>
    /// <b>The fence this packet owes.</b> §9 #47 asks whether agreement is unanimous or a
    /// majority; the recommendation, taken here, is <b>unanimous among the people at the
    /// table</b> — because a majority moves somebody's seat against their will in a game with
    /// money on it. Three asking and one refusing is exactly the case that separates the two
    /// readings, and under a majority the seats would move. <b>They do not.</b> If an expert
    /// overturns #47, this test is the one that fails, and it says so.
    /// </remarks>
    [Fact]
    public void AgreementIsUnanimousUntilTheExpertSaysOtherwise()
    {
        var observer = new RecordingObserver();

        var match = Match(
            observer,
            player => player == FourPlayers[3]
                ? new SaysAboutTheSeating(SeatingOpinion.Refuse)
                : new SaysAboutTheSeating(SeatingOpinion.Ask));

        for (var round = 0; round < 3; round++)
        {
            match.PlayRound();
        }

        // Three of four wanted it. The seating stands, and the table is told why by name.
        Assert.All(observer.Seatings, seating => Assert.Equal(FourPlayers, seating));
        Assert.Equal(
            [$"{FourPlayers[0]} asked, {FourPlayers[3]} refused",
             $"{FourPlayers[0]} asked, {FourPlayers[3]} refused"],
            observer.SeatingRefusals);
        Assert.Empty(observer.SeatingChanges);
    }

    /// <summary>
    /// ✅ <b>Every seat is asked, before every round but the first, and told the truth about
    /// which round and which seating it is being asked about.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Not asked before the first round</b>: whoever opened the table has already seated
    /// it, and there is nobody to have agreed with yet.
    /// </remarks>
    [Fact]
    public void EverySeatIsAskedBeforeEveryRoundButTheFirst()
    {
        var asked = new Dictionary<PlayerId, List<SeatingQuestion>>();
        var observer = new RecordingObserver();

        var match = Match(observer, player => new RecordsTheQuestion(asked[player] = []));

        match.PlayRound();
        match.PlayRound();
        match.PlayRound();

        foreach (var player in FourPlayers)
        {
            Assert.Equal([2, 3], asked[player].Select(question => question.Round));
            Assert.All(asked[player], question => Assert.Equal(player, question.Player));
            Assert.All(asked[player], question => Assert.Equal(FourPlayers, question.Seating));
        }
    }

    /// <summary>
    /// ✅ <b>Acceptance 7, at the root: no published figure can move.</b>
    /// </summary>
    /// <remarks>
    /// The harness plays one round a game (<c>RoundsPerGame = 1</c>), and the question is never
    /// put before the first round — so there is no game in <c>docs/strategy/measurements.csv</c>
    /// in which a seat could have been asked at all. <b>Asserted rather than argued</b>: a
    /// one-round game is the same game whatever every seat would have said.
    /// </remarks>
    [Fact]
    public void AOneRoundGameCannotReachTheQuestion()
    {
        Assert.Equal(1, new SimulationOptions { Strategies = StrategyCatalog.Ladder }.RoundsPerGame);

        static List<string> Played(SeatingOpinion opinion)
        {
            var observer = new RecordingObserver();
            var match = new MatchEngine(
                FourPlayers,
                FourPlayers.ToDictionary(
                    player => player,
                    IPlayerAgent (_) => new SaysAboutTheSeating(opinion, plays: new GreedyBotAgent())),
                Stakes.Standard,
                new Random(20260822),
                observer);

            match.PlayRound();
            return observer.Events;
        }

        var consenting = Played(SeatingOpinion.Consent);

        Assert.Equal(consenting, Played(SeatingOpinion.Ask));
        Assert.Equal(consenting, Played(SeatingOpinion.Refuse));
    }

    /// <summary>
    /// 🔥 <b>No rung answers the seating question, and that is the design decision</b>
    /// (BUILD-PLAN §3.13).
    /// </summary>
    /// <remarks>
    /// A rung decides about cards. <em>Shall we move seats</em> is not a card decision, so a rung
    /// answering it on some invented basis would be a strategy claim nobody measured (P15's
    /// discipline) — and a rung that refused would make §3's rule dead at every table with a
    /// computer at it. <b>The default consent lives on the interface and nowhere else</b>, so a
    /// new rung is covered the day it is written.
    /// </remarks>
    [Fact]
    public void NoRungAnswersTheSeatingQuestionForItself()
    {
        var players = BotCatalog.All
            .Select(rung => rung.Create(0))
            .Concat(DifficultyLadder.All.Select(level => level.Create(0)))
            .ToList();

        Assert.NotEmpty(players);

        foreach (var player in players)
        {
            // Every rung answers consent, level or bare — a level is the strongest rung with a
            // mistake rate, and a mistake about a card is not an opinion about the furniture.
            Assert.Equal(
                SeatingOpinion.Consent,
                player.AskAboutTheSeating(new SeatingQuestion(2, FourPlayers[0], FourPlayers)));

            // …and none of them says so for itself. A level is a FallibleAgent, which is a
            // decorator and must forward; it is checked by the test below rather than here.
            if (player is FallibleAgent)
            {
                continue;
            }

            Assert.Null(player.GetType().GetMethod(
                nameof(IPlayerAgent.AskAboutTheSeating),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        }
    }

    /// <summary>
    /// 🔥 <b>Every decorator forwards the seating question</b> — the quiet failure this packet
    /// could most easily have shipped.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the first member of <see cref="IPlayerAgent"/> with a default implementation.</b>
    /// A wrapper that does not override it does not fail to compile and does not throw: it answers
    /// <em>consent</em> in its own name and drops what it wraps — which for
    /// <c>JournalingAgent</c> means a re-seating that never reached the file, and for
    /// <c>JournalPlayerAgent</c> means a replay that quietly deals to different seats.
    /// </para>
    /// <para>
    /// ⚠️ <b>Found by type rather than by list</b>, in <c>LayeringTests</c>' spirit: anything
    /// taking an <see cref="IPlayerAgent"/> in its constructor is a decorator, so a decorator
    /// written next year is covered without anybody remembering this file.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryDecoratorForwardsTheSeatingQuestion()
    {
        var assemblies = new[]
        {
            typeof(IPlayerAgent).Assembly,
            typeof(BurmesePoker.Presentation.HandView).Assembly,
            typeof(BurmesePoker.Server.TableSession).Assembly,
            typeof(Simulator).Assembly,
            typeof(BurmesePoker.Web.SeatBoard).Assembly
        };

        var decorators = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.IsAssignableTo(typeof(IPlayerAgent)))
            .Where(type => type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(constructor => constructor.GetParameters()
                    .Any(parameter => parameter.ParameterType == typeof(IPlayerAgent))))
            .ToList();

        // A guard on the guard: the journalling wrapper, the replay seat's sibling, the dial's
        // wrapper, the pacing wrapper, the clock and the harness's recorder are all this shape.
        Assert.True(decorators.Count >= 5, $"only {decorators.Count} decorators found.");

        foreach (var decorator in decorators)
        {
            Assert.NotNull(decorator.GetMethod(
                nameof(IPlayerAgent.AskAboutTheSeating),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        }
    }

    private static MatchEngine Match(RecordingObserver observer, Func<PlayerId, IPlayerAgent> seat) =>
        new(FourPlayers,
            FourPlayers.ToDictionary(player => player, seat),
            Stakes.Standard,
            new Random(20260822),
            observer);

    /// <summary>A seat with an opinion about the seating and a bot's opinion about everything else.</summary>
    private sealed class SaysAboutTheSeating(
        SeatingOpinion opinion, int? onlyBefore = null, IPlayerAgent? plays = null) : IPlayerAgent
    {
        private readonly IPlayerAgent _plays = plays ?? new GreedyBotAgent();

        public TurnAction ChooseAction(TurnContext context) => _plays.ChooseAction(context);

        public Card ChooseDiscard(TurnContext context) => _plays.ChooseDiscard(context);

        public bool ClaimTurnedUpMoneyCard(TurnContext context) => _plays.ClaimTurnedUpMoneyCard(context);

        public bool ObjectToClaim(TurnContext context) => _plays.ObjectToClaim(context);

        public bool Declare(TurnContext context) => _plays.Declare(context);

        public SeatingOpinion AskAboutTheSeating(SeatingQuestion question) =>
            onlyBefore is null || question.Round == onlyBefore ? opinion : SeatingOpinion.Consent;
    }

    /// <summary>A seat that writes down what it was asked about the seating and consents.</summary>
    private sealed class RecordsTheQuestion(List<SeatingQuestion> asked) : IPlayerAgent
    {
        private readonly GreedyBotAgent _plays = new();

        public TurnAction ChooseAction(TurnContext context) => _plays.ChooseAction(context);

        public Card ChooseDiscard(TurnContext context) => _plays.ChooseDiscard(context);

        public bool ClaimTurnedUpMoneyCard(TurnContext context) => _plays.ClaimTurnedUpMoneyCard(context);

        public bool ObjectToClaim(TurnContext context) => _plays.ObjectToClaim(context);

        public bool Declare(TurnContext context) => _plays.Declare(context);

        public SeatingOpinion AskAboutTheSeating(SeatingQuestion question)
        {
            asked.Add(question);
            return SeatingOpinion.Consent;
        }
    }
}

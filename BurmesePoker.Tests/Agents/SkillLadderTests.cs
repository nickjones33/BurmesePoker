using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Sim;
using BurmesePoker.Tests.Play;

using BurmesePoker.Tests;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// What every rung of the ladder owes, whatever it plays like (packet P15).
/// </summary>
/// <remarks>
/// <para>
/// <b>These are the properties, not the play.</b> How well a rung plays is measured in the
/// harness over thousands of games and reported with an interval; what belongs here is the
/// handful of things that must hold of each of them for that measurement to mean anything —
/// that they answer legally, that money is not an input, and that the random one is random
/// only in the sense the harness can reproduce.
/// </para>
/// <para>
/// ⚠️ <b>Rounds here are bounded by <see cref="SeatRecorder"/>.</b> Only a declaration ends a
/// round (RULES.md §7.1) and the cards circulate for ever, so a table holding a rung with no
/// monotone score can play until it is killed. The harness's turn cap is the only bound that
/// exists, and a test that plays a round without one is a test that can hang the suite.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class SkillLadderTests
{
    private static readonly PlayerId Alice = new(0);
    private static readonly PlayerId[] FourPlayers = [.. Enumerable.Range(0, 4).Select(seat => new PlayerId(seat))];

    /// <summary>Every rung, by the name the command line knows it as.</summary>
    public static TheoryData<string> Rungs => ["random", "simple", "greedy", "cautious", "counting"];

    /// <summary>Twelve that partition, and the Q♣, which joins nothing — the P10 fixture.</summary>
    private static readonly string[] TwelveAndADeadQueen =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QC"];

    [Theory]
    [MemberData(nameof(Rungs))]
    public void MoneyCardsDoNotChangeWhatABotThrowsAway(string rung)
    {
        // P15 acceptance 4, and RULES.md §4.4 as behaviour: ownership never transfers, so a
        // money card pays the player the deck gave it to whether they are holding it or threw
        // it away on turn one. A new rung is the likeliest place for that to be got wrong —
        // the two rounds below differ in nothing but which card is turned up.
        var ordinary = OpeningTurn(rung, turnedUpFromTop: "3C");
        var itsAMoneyCard = OpeningTurn(rung, turnedUpFromTop: "QC");

        Assert.True(ordinary.Discard.SameValueAs(itsAMoneyCard.Discard));

        // And it really was money, and really was hers: she threw a card that pays her.
        Assert.Equal(0, ordinary.Money.Multiplier(ordinary.Discard));
        Assert.Equal(1, itsAMoneyCard.Money.Multiplier(Hands.Value("QC")));
    }

    [Theory]
    [MemberData(nameof(Rungs))]
    public void EveryRungThrowsACardItIsActuallyHolding(string rung)
    {
        // The engine rejects anything else, so this is really a statement that a rung can be
        // seated at all — but it is the one thing a strategy can get wrong that stops the
        // round rather than losing it.
        var turn = OpeningTurn(rung, "3C");

        Assert.Contains(turn.Hand, held => held == turn.Discard);
        Assert.Equal(14, turn.Hand.Count);
    }

    [Fact]
    public void TheRandomRungPlaysTheSameGameFromTheSameSeed()
    {
        // P15 acceptance 2, at the level it is easiest to break: an agent that reached for
        // Random.Shared would pass every other test in this file and quietly cost the whole
        // harness its reproducibility (BUILD-PLAN §3.7 item 1).
        var once = RandomOpening(seed: 4242);
        var again = RandomOpening(seed: 4242);
        var elsewhere = RandomOpening(seed: 4243);

        Assert.Equal(once, again);
        Assert.NotEqual(once, elsewhere);
    }

    [Fact]
    public void ACautiousBotDiffersFromAGreedyOneOnlyWhereGreedyWasGuessing()
    {
        // The rungs are comparable only if each differs from the one below it in one thing,
        // and this is that one thing (BUILD-PLAN P15). Alice holds twelve cards that
        // partition, the dead Q♣, and the 2♣ she drew. Both loose cards cost her nothing, so
        // the cover count ties; both have exactly one partner in the hand — the Q♣ has the
        // K♣ beside it and the 2♣ has the 2♥ — so greedy's own tie-break ties as well, and
        // greedy falls back on whichever card it reached first.
        var greedy = OpeningTurn("greedy", "3C");
        var cautious = OpeningTurn("cautious", "3C");

        var kept = Covered(greedy.Hand, greedy.Discard);
        Assert.Equal(12, kept);
        Assert.Equal(kept, Covered(cautious.Hand, cautious.Discard));

        // Cautious fills that arbitrary place in with a reason: a 2 sits in two runs (A-2-3
        // and 2-3-4) where a queen sits in three, so the 2♣ is the less useful card to hand
        // to whoever picks it up.
        Assert.True(greedy.Discard.SameValueAs(Hands.Value("QC")));
        Assert.True(cautious.Discard.SameValueAs(Hands.Value("2C")));
    }

    /// <summary>How many of the thirteen kept would meld, had that card been thrown.</summary>
    private static int Covered(IReadOnlyList<Card> hand, Card thrown) =>
        PartialCover.Best([.. hand.Where(held => held != thrown)]).CoveredCount;

    /// <summary>The named rung's opening turn, with the round abandoned the moment it is over.</summary>
    private static (Card Discard, IReadOnlyList<Card> Hand, MoneyCardRegistry Money) OpeningTurn(
        string rung,
        string turnedUpFromTop)
    {
        var watched = new RecordingAgent(StrategyCatalog.Resolve(rung).Create(4242));

        var engine = new RoundEngine(
            FourPlayers,
            FourPlayers.ToDictionary(
                player => player,
                IPlayerAgent (player) => player == Alice
                    ? new SeatRecorder(watched, turnCap: 1)
                    : new SeatRecorder(new GreedyBotAgent(), turnCap: 1)),
            Stakes.Standard,
            DealBuilder.ForPlayers(4)
                .Give(0, TwelveAndADeadQueen)
                .ThenDraw("2C")
                .TurnUpFromTop(turnedUpFromTop)
                .TurnUpFromBottom("4C")
                .Build(),
            new Random(1));

        // Every seat is capped at one turn, so the round ends where the opening turn does
        // however badly the watched rung plays.
        Assert.Throws<RoundAbandonedException>(() => engine.Play());

        return (watched.Discards[0], watched.HandsWhenDiscarding[0], engine.Table.MoneyCards);
    }

    /// <summary>What a random rung does with its opening turn, as text to compare.</summary>
    private static string RandomOpening(int seed)
    {
        var watched = new RecordingAgent(new RandomBotAgent(new Random(seed)));

        var engine = new RoundEngine(
            FourPlayers,
            FourPlayers.ToDictionary(
                player => player,
                IPlayerAgent (player) => player == Alice
                    ? new SeatRecorder(watched, turnCap: 3)
                    : new SeatRecorder(new GreedyBotAgent(), turnCap: 3)),
            Stakes.Standard,
            DealBuilder.ForPlayers(4).Give(0, TwelveAndADeadQueen).ThenDraw("2C")
                .TurnUpFromTop("3C").TurnUpFromBottom("4C").Build(),
            new Random(1));

        Assert.Throws<RoundAbandonedException>(() => engine.Play());

        return string.Join(
            " ",
            watched.Claims.Select(claim => claim.ToString())
                .Concat(watched.Actions.Select(action => action.ToString()))
                .Concat(watched.Discards.Select(discard => discard.ToString())));
    }
}

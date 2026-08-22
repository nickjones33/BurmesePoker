using System.Diagnostics;
using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// The computer's seat (packet P10) — played through real rounds, because a
/// <see cref="TurnContext"/> is the engine's to build and a strategy is only meaningful
/// inside one.
/// </summary>
public class GreedyBotAgentTests
{
    private static readonly PlayerId Alice = new(0);
    private static readonly PlayerId Bob = new(1);
    private static readonly PlayerId Carol = new(2);
    private static readonly PlayerId Dan = new(3);
    private static readonly PlayerId[] FourPlayers = [Alice, Bob, Carol, Dan];

    /// <summary>
    /// Twelve cards that partition — 2♥–7♥, 8♦9♦10♦, three kings — and the Q♣, which joins
    /// nothing: no other queen, and the nearest club four ranks away.
    /// </summary>
    private static readonly string[] TwelveAndADeadQueen =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QC"];

    [Fact]
    public void ARoundOfNothingButBotsReachesADeclarationAndSettles()
    {
        // Alice draws the J♦, which carries 8♦9♦10♦ to four, so all thirteen she keeps meld.
        var bot = new RecordingAgent(new GreedyBotAgent());
        var engine = Engine(Deal().Give(0, TwelveAndADeadQueen).ThenDraw("JD"), Bots(bot));

        var result = engine.Play();

        Assert.Equal(Alice, result.Winner);
        Assert.Equal(1, result.Turns);
        Assert.Equal(13, result.Melds.Sum(meld => meld.Count));
        Assert.True(Assert.Single(bot.Discards).SameValueAs(Hands.Value("QC")));

        // ⚠️ This said "nobody owns a money card" until P26 and the deal has not changed —
        // **jokers became permanent money cards** (RULES.md §4.1, rev 21), and this deal hands
        // one to Carol and one to Dan. $1 a head each way on top of the round payment.
        // 🔥 And the round payment doubled at P33: the thirteen Alice keeps hold no joker, so
        // RULES.md §7.3 pays ×2 at four seats — $10 a loser rather than $5. **A greedy bot won
        // the bonus without knowing it exists**, which is exactly what §14's "floor" means.
        Assert.True(result.Jokerless);
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = 28, [Bob] = -12, [Carol] = -8, [Dan] = -8 },
            result.Payouts);
    }

    [Fact]
    public void MoneyCardsDoNotChangeWhatABotThrowsAway()
    {
        // RULES.md §4.4 as behaviour, and the one test in this packet worth more than the
        // rest of the heuristic together. Ownership never transfers, so the Q♣ Alice was
        // dealt pays her at settlement whether she is holding it or threw it away on turn
        // one — and a bot that hoarded it would simply be playing twelve cards.
        var ordinary = PlaysTheSameOpeningTurn(turnedUpFromTop: "3C");
        var itsAMoneyCard = PlaysTheSameOpeningTurn(turnedUpFromTop: "QC");

        Assert.True(ordinary.Discard.SameValueAs(itsAMoneyCard.Discard));
        Assert.True(ordinary.Discard.SameValueAs(Hands.Value("QC")));

        // And it really was money, and really was hers: she threw away a card that pays her.
        Assert.Equal(0, ordinary.Money.Multiplier(ordinary.Discard));
        Assert.Equal(1, itsAMoneyCard.Money.Multiplier(itsAMoneyCard.Discard));
        Assert.Equal(Alice, itsAMoneyCard.Owner);
    }

    [Fact]
    public void ABotNeverThrowsACardOneOfItsMeldsNeeds()
    {
        // Alice draws a 2♣, which melds with nothing, so she is holding twelve melded cards
        // and two loose ones. Throwing any of the twelve would break a meld of at least
        // three, which the cover score can never prefer.
        var bot = new RecordingAgent(new GreedyBotAgent());
        var engine = Engine(Deal().Give(0, TwelveAndADeadQueen).ThenDraw("2C"), Bots(bot));

        engine.Play();

        var thrown = bot.Discards[0];
        Assert.DoesNotContain(
            TwelveAndADeadQueen[..12], code => thrown.SameValueAs(Hands.Value(code)));
    }

    [Fact]
    public void ABotTakesTheDiscardOnlyWhenItMeldsMoreOfTheHand()
    {
        // Bob is the one being watched. Alice throws first, so what lands in front of him is
        // the test's to choose.
        var wanted = OpeningDiscardOfferedToBob("JD");
        var useless = OpeningDiscardOfferedToBob("5S");

        // The J♦ carries 8♦9♦10♦ to four; the 5♠ makes a pair with the 5♥ and nothing more.
        Assert.Equal(TurnAction.TakeDiscard, wanted.Actions[0]);
        Assert.Equal(TurnAction.DrawFromDeck, useless.Actions[0]);
    }

    [Fact]
    public void AnEvenChoiceGoesToTheDeck()
    {
        // The tie-break, and the only place the money layer touches a decision at all: a
        // blind draw comes from the deck and so confers ownership (RULES.md §4.4), while a
        // card taken from a discard pile never does. The 5♠ leaves the hand exactly as good
        // as it was, so Bob draws instead — and the card he draws is his.
        var bob = OpeningDiscardOfferedToBob("5S");

        Assert.Equal(TurnAction.DrawFromDeck, bob.Actions[0]);
    }

    [Fact]
    public void ABotClaimsTheTurnedUpMoneyCardOnlyWhenItHelpsTheHand()
    {
        // It costs the turn's draw and the table gives it, so it pays nobody (RULES.md §4.5).
        // The only reason to want it is the melding.
        var helps = OpensAgainst(turnedUpFromTop: "JD");
        var doesNot = OpensAgainst(turnedUpFromTop: "QS");

        Assert.True(Assert.Single(helps.Claims));
        Assert.False(Assert.Single(doesNot.Claims));
    }

    [Fact]
    public void ABotOnlyMatchTerminatesAndConservesMoney()
    {
        // Not a formality. With the reshuffle built (RULES.md §5) only a declaration ends a
        // round, so a strategy that never improves its hand plays for ever — termination is
        // a property of the heuristic, and this is where it is checked.
        var match = new MatchEngine(
            FourPlayers,
            FourPlayers.ToDictionary(player => player, IPlayerAgent (_) => new GreedyBotAgent()),
            Stakes.Standard,
            new Random(2026));

        var clock = Stopwatch.StartNew();
        var rounds = Enumerable.Range(0, 5).Select(_ => match.PlayRound()).ToList();
        clock.Stop();

        Assert.Equal(5, match.RoundsPlayed);
        Assert.All(rounds, round => Assert.Contains(round.Result.Winner, FourPlayers));
        Assert.All(rounds, round => Assert.Equal(0, round.Result.Payouts.Values.Sum()));
        Assert.All(rounds, round => Assert.Equal(13, round.Result.Melds.Sum(meld => meld.Count)));
        Assert.Equal(0, match.Banks.Values.Sum());

        // Generous — the point is that it finishes at all, not how fast. Anything near this
        // means the heuristic is thrashing rather than converging.
        Assert.True(clock.Elapsed < TimeSpan.FromMinutes(1),
            $"Five bot rounds took {clock.Elapsed.TotalSeconds:0.0} s "
            + $"over {rounds.Sum(round => round.Result.Turns)} turns.");
    }

    /// <summary>Alice's opening turn, played by a bot against the given designation.</summary>
    private static (Card Discard, MoneyCardRegistry Money, PlayerId? Owner) PlaysTheSameOpeningTurn(
        string turnedUpFromTop)
    {
        var bot = new RecordingAgent(new GreedyBotAgent());
        var engine = Engine(
            Deal().Give(0, TwelveAndADeadQueen).ThenDraw("JD"), Bots(bot), turnedUpFromTop);

        engine.Play();

        var discard = bot.Discards[0];
        return (discard, engine.Table.MoneyCards, engine.Table.Ownership.OwnerOf(discard.Id));
    }

    /// <summary>
    /// Bob's opening turn, with the named card thrown in front of him by Alice — who is
    /// scripted, so that what he is offered is the test's choice and not another bot's.
    /// </summary>
    private static RecordingAgent OpeningDiscardOfferedToBob(string offered)
    {
        var bob = new RecordingAgent(new GreedyBotAgent());
        var agents = FourPlayers.ToDictionary(
            player => player,
            IPlayerAgent (player) => player == Alice
                ? new ScriptedPlayerAgent(new ScriptedTurn { Discard = offered })
                : player == Bob ? bob : new GreedyBotAgent());

        var engine = Engine(
            Deal().Give(1, TwelveAndADeadQueen).ThenDraw(offered, "2C"), agents);

        engine.Play();
        return bob;
    }

    /// <summary>Alice opening against a given turned-up card, with the claim on offer.</summary>
    private static RecordingAgent OpensAgainst(string turnedUpFromTop)
    {
        var bot = new RecordingAgent(new GreedyBotAgent());
        var engine = Engine(
            Deal().Give(0, TwelveAndADeadQueen).ThenDraw("2C"), Bots(bot), turnedUpFromTop);

        engine.Play();
        return bot;
    }

    private static DealBuilder Deal() => DealBuilder.ForPlayers(4);

    private static RoundEngine Engine(
        DealBuilder deal,
        IReadOnlyDictionary<PlayerId, IPlayerAgent> agents,
        string turnedUpFromTop = "3C") =>
        new(FourPlayers, agents, Stakes.Standard,
            deal.TurnUpFromTop(turnedUpFromTop).TurnUpFromBottom("4C").Build(), new Random(1));

    /// <summary>The named agent in seat one; everybody else is an unwatched bot.</summary>
    private static IReadOnlyDictionary<PlayerId, IPlayerAgent> Bots(IPlayerAgent watched) =>
        FourPlayers.ToDictionary(
            player => player,
            IPlayerAgent (player) => player == Alice ? watched : new GreedyBotAgent());
}

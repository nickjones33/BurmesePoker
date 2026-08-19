using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Play;

/// <summary>
/// A whole session at one table: rounds repeating, banks carrying over, nothing ending it
/// (RULES.md §7.2).
/// </summary>
/// <remarks>
/// Every round here is scripted through <see cref="MatchEngine.PlayRound(IReadOnlyList{Card})"/>
/// so the deals are known; the shuffling overload is exercised separately.
/// </remarks>
public class MatchEngineTests
{
    private static readonly PlayerId Alice = new(0);
    private static readonly PlayerId Bob = new(1);
    private static readonly PlayerId Carol = new(2);
    private static readonly PlayerId Dan = new(3);
    private static readonly PlayerId[] FourPlayers = [Alice, Bob, Carol, Dan];

    /// <summary>Thirteen cards that partition exactly: 2♥–7♥, 8♦9♦10♦, and all four kings.</summary>
    private static readonly string[] WinningHand =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "KD"];

    [Fact]
    public void ThreeRoundsRunOneAfterAnotherWithTheBanksCarryingOver()
    {
        // Alice goes out on the opening turn of every round, so nobody has drawn anything and
        // no money card is owned: only the flat round value moves (RULES.md §7.2).
        var match = Match(WinsImmediately());

        for (var round = 1; round <= 3; round++)
        {
            var played = match.PlayRound(Deal());

            Assert.Equal(round, played.Result.Round);
            Assert.Equal(round, match.RoundsPlayed);
            Assert.Equal(Alice, played.Result.Winner);
            Assert.Equal(round * 15, match.Banks[Alice]);
            Assert.Equal(round * -5, match.Banks[Bob]);
        }

        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = 45, [Bob] = -15, [Carol] = -15, [Dan] = -15 },
            match.Banks);
    }

    [Fact]
    public void EverybodyStartsAtZeroAndTheBanksAlwaysSumToZero()
    {
        // Conservation reduces to banking the deltas correctly: a round's payouts sum to zero
        // for any configuration (P6), so the banks do too, however many rounds are played.
        var match = Match(WinsImmediately());

        Assert.Equal(FourPlayers, match.Banks.Keys.OrderBy(player => player.Value).ToArray());
        Assert.All(match.Banks.Values, bank => Assert.Equal(0, bank));

        for (var round = 1; round <= 5; round++)
        {
            match.PlayRound(Deal());
            Assert.Equal(0, match.Banks.Values.Sum());
        }
    }

    [Fact]
    public void MoneyCardsAreDesignatedAfreshEachRoundAndDoNotAccumulate()
    {
        // The registry is a pure function of the round's turned-up cards, rebuilt with the
        // round (BUILD-PLAN §3.3) — which is where the 2023 design's double-marking bug lived.
        var match = Match(WinsImmediately());

        var first = match.PlayRound(Deal(top: "3C", bottom: "4C")).Table.MoneyCards;
        var second = match.PlayRound(Deal(top: "5H", bottom: "6S")).Table.MoneyCards;

        Assert.Equal(1, first.Multiplier(Hands.Value("3C")));
        Assert.Equal(0, second.Multiplier(Hands.Value("3C")));
        Assert.Equal(1, second.Multiplier(Hands.Value("5H")));

        // The permanent designators pay the same in every round — never twice as much in the
        // second (RULES.md §4.1).
        Assert.Equal(1, first.Multiplier(Hands.Value("7D")));
        Assert.Equal(1, second.Multiplier(Hands.Value("7D")));
        Assert.Equal(1, second.Multiplier(Hands.Value("AS")));
    }

    [Fact]
    public void ARoundHandsBackTheTableItSettledFrom()
    {
        // Two of the five things a strategy comparison wants are reachable only this way
        // (BUILD-PLAN §3.8): how much of the money was the side bet, which needs the ownership
        // record and the shoe, and how close the losers were, which needs their hands.
        var match = Match(WinsImmediately());

        var played = match.PlayRound(Deal());
        var table = played.Table;

        Assert.Equal(FourPlayers, table.Players);
        // Fifty-two dealt, plus the one card Alice drew before going out.
        Assert.Equal(53, table.Ownership.Records.Count);
        Assert.Equal(DeckBuilder.TotalCards, table.Shoe.Count);

        // The losers' hands are still there to be looked at.
        foreach (var seat in table.Seats.Where(seat => seat.Id != played.Result.Winner))
        {
            Assert.Equal(13, seat.Hand.Count);
        }

        // And the side bet is the remainder once the flat round payment is taken out.
        var sideBet = table.Players.ToDictionary(
            player => player,
            player => played.Result.Payouts[player] - (player == played.Result.Winner
                ? table.Stakes.RoundValue * (table.Players.Count - 1)
                : -table.Stakes.RoundValue));

        Assert.All(sideBet.Values, share => Assert.Equal(0, share));
    }

    [Fact]
    public void TheSameSeatingIsPlayedEveryRound()
    {
        // RULES.md §3 step 2 randomises seating at setup and says nothing about changing it
        // between rounds, so the match keeps the order it was given (RULES.md §9 #14).
        var match = Match(WinsImmediately());

        Assert.Equal(FourPlayers, match.PlayRound(Deal()).Table.Players);
        Assert.Equal(FourPlayers, match.PlayRound(Deal()).Table.Players);
    }

    [Fact]
    public void AMatchNeedsNoObserverAtAll()
    {
        // P12 runs matches with nothing watching (BUILD-PLAN §3.8), so an observer is optional
        // here exactly as it is on a round.
        var match = new MatchEngine(FourPlayers, Agents(WinsImmediately()), Stakes.Standard, new Random(7));

        var played = match.PlayRound(Deal());

        Assert.Equal(1, match.RoundsPlayed);
        Assert.Equal(0, match.Banks.Values.Sum());
        Assert.Equal(DeckBuilder.TotalCards, played.Table.AllCards.Count());
    }

    [Fact]
    public void TheShufflingOverloadDealsFromTheMatchsOwnRandom()
    {
        // A real game deals from a shuffle, and the match's seed is the only randomness in it
        // — so the same seed deals the same round (BUILD-PLAN §3.7). The round is abandoned
        // on its first discard, because random play essentially never produces a winner and
        // nothing but a declaration ends a round (RULES.md §7.2).
        static List<string> DealtWith(int seed)
        {
            var observer = new RecordingObserver();
            var match = new MatchEngine(
                FourPlayers, Agents(new AbandonsTheRound()), Stakes.Standard, new Random(seed), observer);

            Assert.Throws<AbandonedException>(() => match.PlayRound());
            return observer.Events;
        }

        Assert.Equal(DealtWith(3), DealtWith(3));
        Assert.NotEqual(DealtWith(3), DealtWith(4));
    }

    [Fact]
    public void NothingEndsAMatchOfItsOwnAccord()
    {
        // There is no target score and no round limit (RULES.md §7.2); the engine plays a
        // round whenever it is asked and never decides the session is over.
        var match = Match(WinsImmediately());

        for (var round = 0; round < 12; round++)
        {
            match.PlayRound(Deal());
        }

        Assert.Equal(12, match.RoundsPlayed);
        Assert.Equal(180, match.Banks[Alice]);
    }

    [Fact]
    public void ATableThatCannotBeDealtIsRefusedBeforeARoundIsPlayed()
    {
        var agents = Agents(ScriptedPlayerAgent.Passive());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new MatchEngine([Alice, Bob, Carol], agents, Stakes.Standard, new Random(1)));
        Assert.Throws<ArgumentException>(
            () => new MatchEngine([Alice, Bob, Carol, Carol], agents, Stakes.Standard, new Random(1)));
        Assert.Throws<ArgumentException>(
            () => new MatchEngine(
                FourPlayers,
                new Dictionary<PlayerId, IPlayerAgent> { [Alice] = ScriptedPlayerAgent.Passive() },
                Stakes.Standard,
                new Random(1)));
    }

    [Fact]
    public void EveryRoundIsNarratedToTheOneObserver()
    {
        var observer = new RecordingObserver();
        var match = new MatchEngine(
            FourPlayers, Agents(WinsImmediately()), Stakes.Standard, new Random(1), observer);

        match.PlayRound(Deal());
        match.PlayRound(Deal());

        Assert.Contains(observer.Events, entry => entry.StartsWith("round 1 started"));
        Assert.Contains(observer.Events, entry => entry.StartsWith("round 2 started"));
        Assert.Equal(2, observer.Events.Count(entry => entry.Contains("declared")));
    }

    [Fact]
    public void APlayerIsToldWhichRoundTheyAreIn()
    {
        // An agent lives for the whole match, so turn 1 of round 2 must be tellable from turn 1
        // of round 1. The hotseat console clears the screen on that boundary, and without the
        // round it left the opening player's hand on the screen at the start of every round
        // after the first.
        var seen = new List<(int Round, int Turn)>();
        var match = new MatchEngine(FourPlayers, Agents(new RecordsTheRound(seen)), Stakes.Standard, new Random(1));

        match.PlayRound(Deal());
        match.PlayRound(Deal());

        Assert.Equal([(1, 1), (2, 1)], seen.Distinct().ToArray());
    }

    private static MatchEngine Match(IPlayerAgent alice) =>
        new(FourPlayers, Agents(alice), Stakes.Standard, new Random(1));

    /// <summary>The named agent in Alice's seat; the rest of the table plays passively.</summary>
    private static IReadOnlyDictionary<PlayerId, IPlayerAgent> Agents(IPlayerAgent alice) =>
        FourPlayers.ToDictionary(
            player => player,
            player => player == Alice ? alice : ScriptedPlayerAgent.Passive());

    private static ScriptedPlayerAgent WinsImmediately() => new(new ScriptedTurn { Declare = true });

    private static IReadOnlyList<Card> Deal(string top = "3C", string bottom = "4C") =>
        DealBuilder.ForPlayers(4).Give(0, WinningHand).TurnUpFromTop(top).TurnUpFromBottom(bottom).Build();

    /// <summary>
    /// Walks away mid-turn. The only way to look at a shuffled deal without playing it out:
    /// with the reshuffle in place (RULES.md §5) a round of players who never improve their
    /// hands runs for ever, because nothing but a declaration ends one (RULES.md §7.2).
    /// </summary>
    private sealed class AbandonsTheRound : IPlayerAgent
    {
        public TurnAction ChooseAction(TurnContext context) => TurnAction.DrawFromDeck;

        public Card ChooseDiscard(TurnContext context) => throw new AbandonedException();

        public bool ClaimTurnedUpMoneyCard(TurnContext context) => false;

        public bool Declare(TurnContext context) => false;
    }

    private sealed class AbandonedException : Exception;

    /// <summary>Wins on its first turn, writing down which round and turn it was asked about.</summary>
    private sealed class RecordsTheRound(List<(int Round, int Turn)> seen) : IPlayerAgent
    {
        public TurnAction ChooseAction(TurnContext context) => Note(context, TurnAction.DrawFromDeck);

        public Card ChooseDiscard(TurnContext context) =>
            Note(context, context.Taken ?? throw new InvalidOperationException("Nothing was taken."));

        public bool ClaimTurnedUpMoneyCard(TurnContext context) => Note(context, false);

        public bool Declare(TurnContext context) => Note(context, true);

        private T Note<T>(TurnContext context, T answer)
        {
            seen.Add((context.Round, context.TurnNumber));
            return answer;
        }
    }
}

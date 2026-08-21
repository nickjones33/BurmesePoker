using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Tests.Agents;

namespace BurmesePoker.Tests.Play;

/// <summary>
/// The feeding ban wired into a round: what arms it, which seat it binds, and what the turn
/// stops offering once it is armed (RULES.md §5.1).
/// </summary>
/// <remarks>
/// <b>The rule itself is <c>FeedingBanTests</c>.</b> This file is the join — the three public
/// takes, the seat the engine reads it from, and the guard that catches an agent answering with a
/// card the turn never offered.
/// </remarks>
public class FeedingBanInPlayTests
{
    private static readonly PlayerId Alice = new(0);
    private static readonly PlayerId Bob = new(1);
    private static readonly PlayerId Carol = new(2);
    private static readonly PlayerId Dan = new(3);
    private static readonly PlayerId[] FourPlayers = [Alice, Bob, Carol, Dan];

    /// <summary>
    /// Thirteen cards that partition exactly — 2♥–7♥, 8♦9♦10♦, all four kings.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Every round here has to be given a way to end.</b> A table of seats that draw blind and
    /// throw back what they drew never finishes: only a declaration ends a round (§7.1) and §5's
    /// reshuffle keeps the cards circulating. So Carol holds this and goes out on a stated turn,
    /// which is also what fixes how many turns the seats under test get.
    /// </remarks>
    private static readonly string[] WinningHand =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "KD"];

    /// <remarks>
    /// The playtest, in miniature. Alice opens and throws a Queen; Bob — the seat she discards to
    /// — takes it where everybody can see. Queens are closed to Alice from that moment, and the
    /// two she is still holding are simply not among the cards her next turn offers.
    /// </remarks>
    [Fact]
    public void TakingTheDiscardInTheOpenClosesThatRankToTheSeatThatFedIt()
    {
        // ⚠️ A script advances per turn the seat itself is asked, so Alice's two entries are her
        // turns 1 and 5, and Bob's one entry is his turn 2.
        var alice = new RecordingAgent(new ScriptedPlayerAgent(
            new ScriptedTurn { Discard = "QD" },
            new ScriptedTurn { Discard = "2H" }));

        var bob = new ScriptedPlayerAgent(
            new ScriptedTurn { Action = TurnAction.TakeDiscard, Discard = "8C" });

        var order = DealBuilder.ForPlayers(4)
            .Give(0, "QD", "QC", "QH", "2H", "3H", "4H", "5D", "6D", "7C", "8S", "9S", "10S", "JS")
            .Give(1, "8C", "2S", "3S", "4S", "5C", "6C", "9H", "10H", "JH", "2C", "3D", "4D", "5S")
            .Give(2, WinningHand)
            .TurnUpFromTop("3C")
            .TurnUpFromBottom("4C")
            .Build();

        Engine(order, Agents(alice, bob, GoesOutOnItsSecondTurn())).Play();

        // Turn 1 offered her everything; turn 5 offers neither Queen she is still holding.
        Assert.Equal(0, alice.ClosedWhenDiscarding[0]);
        Assert.Contains(alice.LegalWhenDiscarding[0], card => card.Rank == Rank.Queen);

        Assert.Equal(1, alice.ClosedWhenDiscarding[1]);
        Assert.Contains(alice.HandsWhenDiscarding[1], card => card.Rank == Rank.Queen);
        Assert.DoesNotContain(alice.LegalWhenDiscarding[1], card => card.Rank == Rank.Queen);
    }

    /// <remarks>
    /// §9 #17, <c>EXPERT</c>: <b>only public takes arm the ban.</b> The rule is about announcing
    /// what you are collecting, and a card off the deck announces nothing.
    /// </remarks>
    [Fact]
    public void ABlindDrawArmsNothingAtAnySeat()
    {
        var order = Order(DealBuilder.ForPlayers(4).Give(2, WinningHand));
        var engine = Engine(order, Agents(
            ScriptedPlayerAgent.Passive(), ScriptedPlayerAgent.Passive(), GoesOutOnItsSecondTurn()));

        // Seven turns of nothing but blind draws, and then Carol goes out.
        Assert.Equal(7, engine.Play().Turns);

        // A passive round is nothing but blind draws, so not one rank is closed anywhere.
        Assert.All(engine.Table.Seats, seat => Assert.True(seat.MayNotBeFed.IsEmpty));
    }

    /// <remarks>
    /// RULES.md §5.1 counts the claim as a public take, and §4.5 is why: taking the turned-up money
    /// card is done in view of the table exactly as taking a discard is.
    /// </remarks>
    [Fact]
    public void ClaimingTheTurnedUpMoneyCardClosesItsRankToTheSeatAbove()
    {
        var alice = new ScriptedPlayerAgent(new ScriptedTurn { Claim = true, Discard = "2H" });

        var order = DealBuilder.ForPlayers(4)
            .Give(0, "2H", "3H", "4H", "5D", "6D", "7C", "8S", "9S", "10S", "JS", "QS", "KS", "AH")
            .Give(2, WinningHand)
            .TurnUpFromTop("9D")
            .TurnUpFromBottom("4C")
            .Build();

        var engine = Engine(order, Agents(
            alice, ScriptedPlayerAgent.Passive(), new ScriptedPlayerAgent(new ScriptedTurn { Declare = true })));
        engine.Play();

        // Alice claimed the 9♦, so nines are closed to Dan — the seat that discards to her.
        Assert.True(engine.Table.SeatFedBy(Dan).MayNotBeFed.Closes(Hands.Value("9C")));
        Assert.False(engine.Table.SeatFedBy(Alice).MayNotBeFed.Closes(Hands.Value("9C")));
    }

    /// <remarks>
    /// ⚠️ <b>A guard on a broken agent, not the enforcement.</b> §5.1 is enforced by construction —
    /// the card is never offered — so this is the same class of mistake as naming a card you do not
    /// hold. A front end that drew a banned card as a button would land here rather than quietly
    /// feeding the seat below.
    /// </remarks>
    [Fact]
    public void TheEngineRefusesADiscardTheTurnNeverOffered()
    {
        var alice = new ScriptedPlayerAgent(
            new ScriptedTurn { Discard = "QD" },
            new ScriptedTurn { Discard = "QC" });

        var bob = new ScriptedPlayerAgent(
            new ScriptedTurn { Action = TurnAction.TakeDiscard, Discard = "8C" });

        var order = DealBuilder.ForPlayers(4)
            .Give(0, "QD", "QC", "QH", "2H", "3H", "4H", "5D", "6D", "7C", "8S", "9S", "10S", "JS")
            .Give(1, "8C", "2S", "3S", "4S", "5C", "6C", "9H", "10H", "JH", "2C", "3D", "4D", "5S")
            .Give(2, WinningHand)
            .TurnUpFromTop("3C")
            .TurnUpFromBottom("4C")
            .Build();

        var refused = Assert.Throws<InvalidOperationException>(
            () => Engine(order, Agents(alice, bob, GoesOutOnItsSecondTurn())).Play());

        Assert.Contains("§5.1", refused.Message, StringComparison.Ordinal);
    }

    /// <remarks>
    /// 🔥 <b>§9 #19, and it is a house ruling rather than a recollection</b> (*"nobody really knows"*).
    /// A release survives the reshuffle — which is only possible because the set is <b>kept</b>
    /// rather than read back off a discard pile that §5 has just swept into the draw pile.
    /// </remarks>
    [Fact]
    public void AReleasedRankSurvivesTheReshuffleThatTakesTheEvidenceAway()
    {
        var deck = new Deck(Hands.Of("2D", "3D"));
        var table = new TableState(
            FourPlayers, Stakes.Standard, deck, Deck.TwoDecks().Cards, Hands.Value("3C"), Hands.Value("4C"));

        var bob = table.SeatOf(Bob);
        bob.TookInTheOpen(Hands.Value("QD"));
        bob.Take(Hands.Value("QH"));
        bob.Discard(bob.Hand[0]);

        Assert.True(bob.MayNotBeFed.HasReleased(Hands.Value("QS")));
        Assert.Single(bob.Discards);

        // The deck runs out and every pile is gathered (RULES.md §5) — the Queen Bob threw is now
        // back in the draw pile and there is nothing on the table to read the release off.
        table.DrawPile.DrawFromTop();
        table.DrawPile.DrawFromTop();
        table.ReplaceDrawPileWithTheDiscards(new Random(1));

        Assert.Empty(bob.Discards);
        Assert.True(bob.MayNotBeFed.HasReleased(Hands.Value("QS")));

        // And it cannot be re-armed: a release does not come back, however many more Queens Bob
        // picks up (§5.1, exception 1).
        bob.TookInTheOpen(Hands.Value("QC"));
        Assert.False(bob.MayNotBeFed.Closes(Hands.Value("QC")));
    }

    /// <remarks>
    /// 🔥 <b>The test that stops the rest of this file being vacuous.</b> Everything above scripts
    /// the ban into existence; this plays an ordinary round of thinking players and asserts that
    /// §5.1 <em>binds somebody</em> in it — otherwise a filter that quietly did nothing would pass
    /// every other test here.
    /// </remarks>
    [Fact]
    public void AnOrdinaryRoundClosesRanksAndNeverOffersOne()
    {
        var recorders = FourPlayers.ToDictionary(
            player => player,
            _ => new RecordingAgent(new GreedyBotAgent()));

        var engine = new RoundEngine(
            FourPlayers,
            recorders.ToDictionary(seat => seat.Key, seat => (IPlayerAgent)seat.Value),
            Stakes.Standard,
            Order(DealBuilder.ForPlayers(4)),
            new Random(20260821));

        engine.Play();

        var bound = 0;

        foreach (var recorder in recorders.Values)
        {
            for (var turn = 0; turn < recorder.Discards.Count; turn++)
            {
                var legal = recorder.LegalWhenDiscarding[turn];

                Assert.NotEmpty(legal);
                Assert.Contains(legal, card => card.Id == recorder.Discards[turn].Id);

                if (recorder.ClosedWhenDiscarding[turn] > 0)
                {
                    bound++;
                }
            }
        }

        Assert.True(bound > 0, "no seat was ever bound by §5.1, so this round proves nothing about it.");
    }

    private static IReadOnlyList<Card> Order(DealBuilder deal) =>
        deal.TurnUpFromTop("3C").TurnUpFromBottom("4C").Build();

    /// <summary>A seat that plays passively and then declares the second time it is asked.</summary>
    private static ScriptedPlayerAgent GoesOutOnItsSecondTurn() =>
        new(new ScriptedTurn(), new ScriptedTurn { Declare = true });

    private static IReadOnlyDictionary<PlayerId, IPlayerAgent> Agents(params IPlayerAgent[] agents) =>
        FourPlayers.ToDictionary(
            player => player,
            player => player.Value < agents.Length ? agents[player.Value] : ScriptedPlayerAgent.Passive());

    private static RoundEngine Engine(
        IReadOnlyList<Card> order,
        IReadOnlyDictionary<PlayerId, IPlayerAgent> agents) =>
        new(
            [.. agents.Keys.OrderBy(player => player.Value)],
            agents,
            Stakes.Standard,
            order,
            new Random(1));
}

using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// The rung that reads the side bet (BUILD-PLAN P22) — and the two claims a run of any size
/// could not settle: that money reaches <b>only</b> the take, and that at the stakes the game is
/// actually played for it reaches nothing at all.
/// </summary>
/// <remarks>
/// <para>
/// <b>How much it is worth is not here.</b> That is a sweep over four stakes ratios with an
/// interval on each, published in <c>docs/STRATEGY.md</c> §10 from
/// <c>docs/strategy/money.csv</c>. What belongs in a test is the shape of the rung: which
/// decisions the money layer may touch, which it may not, and that the arithmetic it touches
/// them with is arithmetic about the <em>shoe</em> rather than about the hand.
/// </para>
/// <para>
/// 🔥 <b>The rule that matters is RULES.md §4.4, and it cuts both ways.</b> Ownership never
/// transfers, so what a seat holds is worth nothing and what it throws away costs nothing —
/// which is why every rung's discard is money-blind and why
/// <c>MoneyCardsDoNotChangeWhatABotThrowsAway</c> holds for this one too. What the rule does not
/// settle is where a card comes <em>from</em>: the deck confers ownership and a discard pile
/// never does. That asymmetry is the only door money has into a decision, and this rung is what
/// walks through it.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class ProspectorBotAgentTests
{
    /// <summary>Twelve that partition, and the Q♣, which joins nothing — the P10 fixture.</summary>
    private static readonly string[] TwelveAndADeadQueen =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QC"];

    /// <summary>
    /// A money card worth eight rounds. Nowhere near the game as played, and that is the point:
    /// the rung's rule fires only somewhere out here, which is itself the packet's answer.
    /// </summary>
    private static readonly Stakes SideBetWorthChasing = new(5, 40);

    [Fact]
    public void AtTheStakesTheGameIsPlayedForItTakesEverythingOutsTakes()
    {
        // 🔥 The headline, as a property rather than as a measurement. At $5/$1 a money card
        // pays its owner $1 from each of three opponents and about six of them are still loose
        // in a 108-card shoe, so a blind draw is worth pennies — while one more melded card is
        // worth a thirteenth of a $15 round. The comparison is never close, so the rung is
        // `outs` under another name at the only stakes anybody plays for, and the sweep exists
        // to say how far from here the answer changes (docs/STRATEGY.md §10).
        var outs = new OutsBotAgent();
        var prospector = new ProspectorBotAgent();

        var asked = 0;

        foreach (var (hand, offered) in Positions(200))
        {
            var context = TurnContexts.Offered(hand, offered, Stakes.Standard);

            Assert.Equal(outs.ChooseAction(context), prospector.ChooseAction(context));
            asked++;
        }

        Assert.Equal(200, asked);
    }

    [Fact]
    public void WhenAMoneyCardIsWorthMoreThanAMeldedCardItDigsInTheDeckInstead()
    {
        // The other half of the same claim: the rule is a rule and not a dead branch. At $5/$40
        // the ownership a blind draw confers outweighs one melded card, so every take that
        // bought exactly one is refused — and the rung differs from `outs` on a real position
        // rather than only in principle.
        var outs = new OutsBotAgent();
        var prospector = new ProspectorBotAgent();

        var differed = 0;

        foreach (var (hand, offered) in Positions(200))
        {
            var rich = TurnContexts.Offered(hand, offered, SideBetWorthChasing);

            if (outs.ChooseAction(rich) != prospector.ChooseAction(rich))
            {
                // Only ever in this direction: the rung refuses a take, never invents one.
                Assert.Equal(TurnAction.TakeDiscard, outs.ChooseAction(rich));
                Assert.Equal(TurnAction.DrawFromDeck, prospector.ChooseAction(rich));
                differed++;
            }
        }

        Assert.True(differed > 0, "The rule never fired, so the sweep would be measuring nothing.");
    }

    [Fact]
    public void TheDiscardIsWhateverOutsWouldThrow_AtEveryStakes()
    {
        // ⚠️ P22 acceptance 3, in the form the packet's own build list demands: only the take
        // changes. A rung whose *discard* moved with the stakes would break RULES.md §4.4 —
        // holding a money card is worth nothing, so nothing about money may reach the card
        // thrown — and it would put two changes between this rung and the one below it, which
        // measures nothing (P15).
        var outs = new OutsBotAgent();
        var prospector = new ProspectorBotAgent();

        foreach (var (hand, offered) in Positions(60))
        {
            IReadOnlyList<Card> fourteen = [.. hand, offered];

            foreach (var stakes in (Stakes[])[Stakes.Standard, SideBetWorthChasing, new(1, 100)])
            {
                var context = TurnContexts.Offered(fourteen, offered, stakes);

                Assert.Equal(outs.ChooseDiscard(context), prospector.ChooseDiscard(context));
                Assert.Equal(outs.RankDiscards(context), prospector.RankDiscards(context));
            }
        }
    }

    [Fact]
    public void TheTakeReadsHowMuchMoneyIsLeftAndNeverWhichCardPaysIt()
    {
        // 🔥 The sharpest form of the money-blindness rule, and the one the theory in
        // SkillLadderTests cannot reach: two rounds that differ only in which card was turned
        // up, at stakes high enough that the take genuinely differs between them — and the
        // discard still does not. What the rung reads is *how much money is unaccounted for*,
        // never *which card it is*.
        var prospector = new ProspectorBotAgent();

        // Two designations, one hand. `loose` turns up cards this seat holds no copy of, so
        // both of their partners are still out there to be drawn; `spent` turns up two kings
        // the seat is already holding, so the money they designate is accounted for and none of
        // it is left in the deck to draw.
        var loose = Designating("3C", "4C");
        var spent = Designating("KC", "KH");

        var hand = Hands.Of([.. TwelveAndADeadQueen, "2C"]);
        IReadOnlyList<Card> thirteen = [.. hand.Take(13)];
        var offered = hand[13];

        var rich = TurnContexts.Offered(thirteen, offered, new Stakes(5, 30), loose);
        var poor = TurnContexts.Offered(thirteen, offered, new Stakes(5, 30), spent);

        // The money still in the shoe moved, so what a draw is worth moved with it.
        Assert.True(MoneyOdds.PerBlindDraw(rich) > MoneyOdds.PerBlindDraw(poor));

        // And the discard did not move, at either designation.
        var fourteenRich = TurnContexts.Offered(hand, offered, new Stakes(5, 30), loose);
        var fourteenPoor = TurnContexts.Offered(hand, offered, new Stakes(5, 30), spent);

        Assert.True(prospector.ChooseDiscard(fourteenRich).SameValueAs(prospector.ChooseDiscard(fourteenPoor)));
    }

    [Fact]
    public void WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe()
    {
        // The arithmetic, on its own. Every input is a rule or a public fact — the stakes
        // (RULES.md §4.3), the designation (§4.1), how many players pay (§4.3) — and the only
        // thing that varies with play is how much of the shoe this seat has already seen.
        var hand = Hands.Of(TwelveAndADeadQueen);
        var offered = Hands.Value("2C");

        var standard = TurnContexts.Offered(hand, offered, Stakes.Standard);
        var tenfold = TurnContexts.Offered(hand, offered, new Stakes(5, 10));

        // Linear in the money card value, because that is what a money card pays.
        Assert.Equal(10 * MoneyOdds.PerBlindDraw(standard), MoneyOdds.PerBlindDraw(tenfold), 9);

        // Positive even with nothing turned up that pays: the 7♦ and the A♠ pay in every round.
        Assert.True(MoneyOdds.PerBlindDraw(standard) > 0);

        // And a seat already holding the money is a seat with less left to draw for. Both
        // copies of the 7♦ and both of the A♠ in one hand empties the shoe of everything the
        // permanent designators pay for.
        var holdingTheLot = Hands.Of("7D", "7D", "AS", "AS", "2H", "3H", "4H", "5H", "6H", "8D", "9D", "10D", "KC");
        var drained = TurnContexts.Offered(holdingTheLot, offered, Stakes.Standard);

        Assert.True(MoneyOdds.PerBlindDraw(drained) < MoneyOdds.PerBlindDraw(standard));

        // 🔥 And the one this packet inverted. P22 derived — correctly, from the *double* rule
        // that RULES.md rev 20 struck — that a designation landing on a permanent money card
        // leaves *less* money in the deck than an ordinary one, and asserted it here. Under the
        // **triple** the two come out **equal**, and that is not a coincidence: the shown copy
        // can never be owned (§4.4), so ×3 is exactly 1 for the partner's own permanence, 1 for
        // the designation, and 1 inherited from the copy nobody can be paid for. Every
        // designation is worth the same $1 of live money wherever it lands.
        //
        // ⚠️ **The reasoning was sound and the premise moved**, which is the difference between
        // a mistake and a dependency — and the claim was written down as an assertion rather
        // than as prose, which is why the rules change could not pass silently.
        var tripled = TurnContexts.Offered(hand, offered, Stakes.Standard, Designating("3C", "7D"));
        var spread = TurnContexts.Offered(hand, offered, Stakes.Standard, Designating("3C", "4C"));

        Assert.Equal(MoneyOdds.PerBlindDraw(spread), MoneyOdds.PerBlindDraw(tripled), 9);
    }

    [Fact]
    public void ClaimingTheTurnedUpCardIsTheSamePurchaseAndIsPricedTheSameWay()
    {
        // RULES.md §4.5: a claimed card comes from the table, so it is held but never owned —
        // exactly like a card off a discard pile. It costs the turn's draw and buys no
        // ownership, so the rung weighs it the same way, and this is the second place the one
        // change lands rather than a second change.
        var hand = Hands.Of(TwelveAndADeadQueen);

        // The 8♥ completes 6♥-7♥-8♥ out of cards already melding, so it is worth taking.
        var wanted = Hands.Value("8H");

        var cheap = Claimable(hand, wanted, Stakes.Standard);
        var dear = Claimable(hand, wanted, SideBetWorthChasing);

        Assert.True(new OutsBotAgent().ClaimTurnedUpMoneyCard(cheap));
        Assert.True(new ProspectorBotAgent().ClaimTurnedUpMoneyCard(cheap));

        // Same card, same hand, and now the draw it would cost is worth more than it is.
        Assert.True(new OutsBotAgent().ClaimTurnedUpMoneyCard(dear));
        Assert.False(new ProspectorBotAgent().ClaimTurnedUpMoneyCard(dear));
    }

    /// <summary>A seat that may claim <paramref name="wanted"/> off the top of the table.</summary>
    private static TurnContext Claimable(IReadOnlyList<Card> hand, Card wanted, Stakes stakes) =>
        TurnContexts.Offered(hand, Hands.Value("2C"), stakes, [Hands.Value("3C"), wanted]);

    /// <summary>The two cards a designation is, named the way a round turns them up.</summary>
    private static IReadOnlyList<Card> Designating(string fromBottom, string fromTop) =>
        [Hands.Value(fromBottom), Hands.Value(fromTop)];

    /// <summary>
    /// Thirteen dealt cards and a fourteenth being offered, from a seeded shoe.
    /// </summary>
    /// <remarks>
    /// <b>Random positions rather than a fixture</b>, because the claim under test is about
    /// every position and not about one: a rung that agreed with <c>outs</c> on the P10 hand and
    /// nowhere else would pass a fixture and fail the game.
    /// </remarks>
    private static IEnumerable<(IReadOnlyList<Card> Hand, Card Offered)> Positions(
        int count,
        int seed = 20260820)
    {
        var random = new Random(seed);

        for (var position = 0; position < count; position++)
        {
            var deck = Deck.TwoDecks();
            deck.Shuffle(random);

            yield return ([.. deck.Cards.Take(13)], deck.Cards[13]);
        }
    }
}

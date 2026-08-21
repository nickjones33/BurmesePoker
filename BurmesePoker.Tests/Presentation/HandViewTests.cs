using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;
using BurmesePoker.Presentation;

namespace BurmesePoker.Tests.Presentation;

/// <summary>
/// The view model a front end draws a hand from (packet P13.1).
/// </summary>
/// <remarks>
/// <para>
/// <b>The first presentation code in this project that a test can reach.</b> Everything the
/// console showed about a hand used to be worked out inside <c>BurmesePoker.Console</c>, which
/// the test project deliberately cannot reference (BUILD-PLAN §2) — so the near-melds, the
/// per-card costs and the markers were verified only by looking at a terminal. Extracting the
/// decisions into <c>BurmesePoker.Presentation</c> is what makes them assertable, and that is
/// the point of the packet rather than a side effect of it.
/// </para>
/// <para>
/// The hands below are the ones <c>PartialCoverTests</c> already reasons about, so a
/// disagreement between the two would be a real one rather than two different puzzles.
/// </para>
/// </remarks>
public class HandViewTests
{
    /// <summary>Nothing pays: no turned-up cards, and neither 7♦ nor A♠ is in these hands.</summary>
    private static readonly MoneyCardRegistry NoMoney = new([]);

    /// <summary>
    /// Twelve cards that partition — 2♥–7♥, 8♦9♦10♦, three kings — and the Q♣, which joins
    /// nothing.
    /// </summary>
    private static readonly string[] TwelveAndADeadQueen =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QC"];

    [Fact]
    public void AHandIsSplitIntoTheMeldsItNearlyIsAndTheCardsLeftOver()
    {
        var view = View(TwelveAndADeadQueen);

        Assert.Equal(13, view.Count);
        Assert.Equal(12, view.Covered);
        Assert.False(view.IsComplete);
        Assert.Equal(12, view.Melds.Sum(meld => meld.Count));

        var loose = Assert.Single(view.Loose);
        Assert.True(loose.Card.SameValueAs(Hands.Value("QC")));
    }

    [Fact]
    public void EveryCardAppearsExactlyOnceAcrossTheMeldsAndTheDeadwood()
    {
        var view = View(TwelveAndADeadQueen);

        var placed = view.Melds
            .SelectMany(meld => meld.Cards)
            .Concat(view.Loose)
            .Select(card => card.Card.Id.Value)
            .ToList();

        Assert.Equal(13, placed.Count);
        Assert.Equal(13, placed.Distinct().Count());
        Assert.Equal([.. view.Cards.Select(card => card.Card.Id.Value).Order()], [.. placed.Order()]);
    }

    [Fact]
    public void TheMeldsComeBackLongestFirst()
    {
        var counts = View(TwelveAndADeadQueen).Melds.Select(meld => meld.Count).ToList();

        Assert.Equal([.. counts.OrderDescending()], counts);
    }

    /// <remarks>
    /// The measurement the console's advice rests on: the cover of the twelve that would be
    /// left, against the cover of the thirteen. Deadwood is free and a meld's card is not.
    /// </remarks>
    [Fact]
    public void ThrowingDeadwoodCostsNothingAndBreakingAMeldCostsTheWholeMeld()
    {
        var view = View(TwelveAndADeadQueen);

        Assert.Equal(0, view.CostOfThrowing(Card(view, "QC")));

        // The three kings are a set of exactly three: take one and none of them meld.
        Assert.Equal(3, view.CostOfThrowing(Card(view, "KC")));

        // 2♥–7♥ is six long, so losing an end shortens it rather than destroying it.
        Assert.Equal(1, view.CostOfThrowing(Card(view, "2H")));
    }

    /// <remarks>
    /// <para>
    /// <b>Instance identity, not value</b> (BUILD-PLAN §3.1). Two decks mean a hand can hold
    /// both copies of a card, and the view keys on <c>CardId</c>: two entries, not one, and
    /// the cover took exactly one of them.
    /// </para>
    /// <para>
    /// <b>They cost the same, and that is the correct answer rather than a coincidence.</b>
    /// A spare copy is a standing replacement for the one in the meld, so throwing either
    /// leaves the same thirteen melding — which is the measured cost doing its job. The one
    /// thing that <em>does</em> differ between them is which the arrangement happened to use.
    /// </para>
    /// </remarks>
    [Fact]
    public void TwoCopiesOfTheSameCardAreTwoCardsAndOnlyOneOfThemMelds()
    {
        // A second king of clubs. Three kings already meld; the fourth is spare.
        var hand = Hands.Of([.. TwelveAndADeadQueen, "KC"]);
        var view = HandView.Of(hand, NoMoney, _ => false);

        var kings = view.Cards.Where(card => card.Card.SameValueAs(Hands.Value("KC"))).ToList();
        Assert.Equal(2, kings.Count);
        Assert.Equal(2, kings.Select(king => king.Card.Id.Value).Distinct().Count());

        Assert.Equal(1, kings.Count(king => king.IsMelded));
        Assert.Equal([0, 0], [.. kings.Select(king => king.CostOfThrowing)]);
    }

    [Fact]
    public void ACardInTheCoverIsMeldedAndACardOutsideItIsLoose()
    {
        var view = View(TwelveAndADeadQueen);

        Assert.True(Card(view, "QC") is var queen && view.Of(queen).State.HasFlag(CardDisplayState.Loose));
        Assert.False(view.Of(queen).IsMelded);

        Assert.True(view.Of(Card(view, "KC")).IsMelded);
        Assert.False(view.Of(Card(view, "KC")).State.HasFlag(CardDisplayState.Loose));
    }

    /// <remarks>
    /// A money card pays once for each designation of its value (RULES.md §4.2, §4.3), and
    /// the state and the multiplier are built from one lookup so they cannot disagree.
    /// </remarks>
    [Fact]
    public void AMoneyCardCarriesItsMultiplierAndTheMatchingState()
    {
        // Tripling is the overlap of the two ways a value is designated: turning up the 7♦,
        // which already pays permanently, is what makes its partner pay three times
        // (RULES.md §4.1, §4.3). ⚠️ Never five here — the jackpot is a property of the round's
        // ownership and no view that draws one card at a time can compute it.
        var hand = Hands.Of("2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "7D");
        var money = new MoneyCardRegistry([Hands.Value("7D"), Hands.Value("KH")]);
        var view = HandView.Of(hand, money, _ => false);

        var seven = view.Of(Card(view, "7D"));
        Assert.Equal(3, seven.Multiplier);
        Assert.True(seven.State.HasFlag(CardDisplayState.PaysTriple));
        Assert.False(seven.State.HasFlag(CardDisplayState.PaysOnce));

        var king = view.Of(Card(view, "KH"));
        Assert.Equal(1, king.Multiplier);
        Assert.True(king.State.HasFlag(CardDisplayState.PaysOnce));

        var plain = view.Of(Card(view, "2H"));
        Assert.Equal(0, plain.Multiplier);
        Assert.False(plain.IsMoneyCard);
    }

    /// <remarks>
    /// <b>The star is only ever on a money card</b> (P11). Every card dealt is owned, so
    /// marking ownership on its own would mark the whole hand and say nothing; what a player
    /// needs to see is which of the cards that <em>pay</em> pay them (RULES.md §4.4).
    /// </remarks>
    [Fact]
    public void OwnershipIsShownOnlyWhereItChangesWhoGetsPaid()
    {
        var money = new MoneyCardRegistry([Hands.Value("QC")]);
        var view = HandView.Of(Hands.Of(TwelveAndADeadQueen), money, _ => true);

        Assert.True(view.Of(Card(view, "QC")).IsOwned);
        Assert.False(view.Of(Card(view, "2H")).IsOwned);
        Assert.Equal(1, view.Cards.Count(card => card.IsOwned));
    }

    [Fact]
    public void TheSuggestedThrowMarksOneCardAndOnlyWhenThereIsAHint()
    {
        var hand = Hands.Of(TwelveAndADeadQueen);
        var advised = hand.Single(card => card.SameValueAs(Hands.Value("QC")));

        var hinted = HandView.Of(hand, NoMoney, _ => false, advised);
        Assert.Equal(advised.Id, Assert.Single(hinted.Cards, card => card.IsSuggestedThrow).Card.Id);

        var silent = HandView.Of(hand, NoMoney, _ => false);
        Assert.DoesNotContain(silent.Cards, card => card.IsSuggestedThrow);
    }

    [Fact]
    public void AWinningHandIsCompleteWithNothingLeftOver()
    {
        // 2♥–5♥, 3♦–5♦, three aces and three sevens: thirteen that partition exactly.
        var view = View(["2H", "3H", "4H", "5H", "3D", "4D", "5D", "AC", "AH", "AD", "7C", "7H", "7S"]);

        Assert.True(view.IsComplete);
        Assert.Equal(13, view.Covered);
        Assert.Empty(view.Loose);
        Assert.True(HandEvaluator.IsWinning(view.Hand, TableRules.For(5)));
    }

    [Fact]
    public void TheCardsComeBackInDisplayOrderAndTheDeadwoodDoesToo()
    {
        var view = View(TwelveAndADeadQueen);

        Assert.Equal(
            [.. CardOrder.Display(view.Hand).Select(card => card.Id)],
            [.. view.Cards.Select(card => card.Card.Id)]);

        Assert.Equal(
            [.. CardOrder.Display(view.Loose.Select(card => card.Card)).Select(card => card.Id)],
            [.. view.Loose.Select(card => card.Card.Id)]);
    }

    /// <remarks>
    /// A meld's slots keep the joker's interpretation, which is the one thing a front end
    /// cannot work out for itself — <c>{2♦,3♦,🃏}</c> looks the same whichever end the joker
    /// is playing (see <c>MeldSlot</c>).
    /// </remarks>
    [Fact]
    public void AMeldsSlotsLineUpWithItsCardsAndKeepWhatEachJokerStandsFor()
    {
        var view = View(["2D", "3D", "RJ", "KC", "KH", "KS", "2H", "3H", "4H", "5H", "6H", "7H", "QC"]);

        foreach (var meld in view.Melds)
        {
            Assert.Equal(meld.Count, meld.Cards.Count);
            Assert.All(meld.Slots, pair => Assert.Equal(pair.Slot.Card.Id, pair.Card.Card.Id));
        }

        var withJoker = Assert.Single(view.Melds, meld => meld.Meld.JokerCount > 0);
        var substitute = Assert.Single(withJoker.Slots, pair => pair.Slot.IsSubstitute);
        Assert.Equal(Suit.Diamonds, substitute.Slot.InSuit);
    }

    [Fact]
    public void AskingAboutACardTheHandDoesNotHoldIsAMistake()
    {
        var view = View(TwelveAndADeadQueen);

        Assert.Throws<ArgumentException>(() => view.CostOfThrowing(Hands.Value("9C")));
    }

    private static HandView View(string[] codes) => HandView.Of(Hands.Of(codes), NoMoney, _ => false);

    /// <summary>The one card of this hand with the named value.</summary>
    private static Card Card(HandView view, string code) =>
        view.Hand.First(card => card.SameValueAs(Hands.Value(code)));
}

using System.Diagnostics;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;

using BurmesePoker.Tests;

namespace BurmesePoker.Tests.Melds;

/// <summary>
/// The exact-cover evaluator (packet P5) — the only win authority in the codebase
/// (RULES.md §7.1: all thirteen cards melded, each used once).
/// </summary>
[Collection(WallClockBudgets.Collection)]
public class HandEvaluatorTests
{
    /// <summary>
    /// Five or more at the table: nothing is asked of the partition beyond covering the hand
    /// (RULES.md §7.1.1). ⚠️ <b>Every test written before P25 is a five-handed test</b> — the
    /// bare exact cover was the only question the evaluator knew how to ask — and they are
    /// pinned here rather than rewritten, because "a five-handed hand is judged exactly as it
    /// is today" is one of P25's acceptance criteria.
    /// </summary>
    private static readonly TableRules FiveHanded = TableRules.For(5);

    // ---- the acceptance cases of BUILD-PLAN §5 P5 ----------------------------------------

    [Fact]
    public void AHandOfThreeThreeThreeAndFourIsWinning()
    {
        var hand = Hands.Of(
            "2D", "3D", "4D",        // run
            "9H", "9S", "9C",        // set
            "5S", "6S", "7S",        // run
            "KH", "KS", "KC", "KD"); // set of four

        Assert.True(HandEvaluator.IsWinning(hand, FiveHanded));

        Assert.True(HandEvaluator.TryFindCover(hand, FiveHanded, out var cover));
        Assert.Equal(4, cover.Count);
        AssertCoversExactly(cover, hand);
    }

    [Fact]
    public void ThirteenUnrelatedCardsAreNotWinning()
    {
        // No three of a rank anywhere, and no three consecutive cards in either suit.
        var hand = Hands.Of(
            "2D", "4D", "6D", "8D", "10D", "QD",
            "3H", "5H", "7H", "9H", "JH", "KH",
            "AS");

        Assert.Equal(13, hand.Count);
        Assert.False(HandEvaluator.IsWinning(hand, FiveHanded));
        Assert.False(HandEvaluator.TryFindCover(hand, FiveHanded, out var cover));
        Assert.Empty(cover);
    }

    [Fact]
    public void TwelveMeldableCardsAndOneOrphanAreNotWinning()
    {
        // The twelve partition cleanly; the 8H belongs to nothing. A partial cover is not a
        // win — every card must be melded (RULES.md §7.1).
        var hand = Hands.Of(
            "2D", "3D", "4D",
            "9H", "9S", "9C",
            "5S", "6S", "7S",
            "KH", "KS", "KC",
            "8H");

        Assert.False(HandEvaluator.IsWinning(hand, FiveHanded));
        Assert.True(HandEvaluator.IsWinning(hand.Take(12).ToList(), FiveHanded));
    }

    [Fact]
    public void AHandWinsOnlyByPlayingAJokerAsACardItAlsoHolds()
    {
        // Five fives — two decks make the second 5♦ — and one joker. The fives cannot be
        // covered without the joker (five cards split into no legal melds, and a set holds at
        // most four), so every cover puts the joker in a set of fives. A set never repeats a
        // suit, so the joker plays hearts, spades, clubs or diamonds — and the hand holds all
        // four. The joker therefore *must* stand in for a card the hand is holding, which is
        // exactly the candidate shape BUILD-PLAN §3.4 says the generators must emit.
        var hand = Hands.Of(
            "5D", "5D", "5H", "5S", "5C", "RJ",
            "9H", "9S", "9C",
            "KD", "KH", "KS", "KC");

        Assert.True(HandEvaluator.TryFindCover(hand, FiveHanded, out var cover));
        AssertCoversExactly(cover, hand);

        var substitution = Assert.Single(
            cover.SelectMany(meld => meld.Slots), slot => slot.IsSubstitute);
        Assert.Contains(hand, card =>
            !card.IsJoker && card.Rank == substitution.PlaysAs && card.Suit == substitution.InSuit);
    }

    [Fact]
    public void AJokerFillsARunGapWhoseRealCardIsMeldedElsewhere()
    {
        // 2♦ 3♦ _ 5♦ 6♦ with the joker as the 4♦, while the real 4♦ is held in the set of
        // fours. This is the candidate the retired 2023 generator never produced — a joker
        // standing in for a card the hand holds — and the run is a five-card window, so the
        // joker can only be the gap.
        var hand = Hands.Of(
            "2D", "3D", "5D", "6D", "RJ",
            "4D", "4H", "4S", "4C",
            "9D", "9H", "9S", "9C");

        Assert.True(HandEvaluator.IsWinning(hand, FiveHanded));

        var gapRun = Assert.Single(
            MeldCandidates.For(hand),
            meld => meld.Kind == MeldKind.Run
                && meld.CardIds.SetEquals(new[] { hand[0], hand[1], hand[2], hand[3], hand[4] }
                    .Select(card => card.Id)));
        var joker = Assert.Single(gapRun.Slots, slot => slot.IsSubstitute);
        Assert.Equal(Rank.Four, joker.PlaysAs);
        Assert.Equal(Suit.Diamonds, joker.InSuit);
    }

    [Fact]
    public void TheCoverIsPairwiseDisjointAndCoversTheHandExactly()
    {
        // A hand with several covers to choose from, so the assertion is about the shape of
        // whichever one comes back rather than about a particular answer.
        var hand = Hands.Of(
            "2D", "3D", "4D", "5D", "6D", "7D",
            "9H", "9S", "9C", "9D",
            "KH", "KS", "RJ");

        Assert.True(HandEvaluator.TryFindCover(hand, FiveHanded, out var cover));
        AssertCoversExactly(cover, hand);
    }

    [Fact]
    public void EvaluatingThirteenCardsIsFast()
    {
        // The pathological shape of docs/spec/RUN-CANDIDATES.md: nine consecutive cards of one
        // suit plus four jokers, over four thousand run candidates on its own.
        var winning = Hands.Of(
            "2D", "3D", "4D", "5D", "6D", "7D", "8D", "9D", "10D", "RJ", "BJ", "RJ", "BJ");

        // Saying *no* is the slower half of the question: the search has to exhaust every
        // way of carving up eleven consecutive diamonds before it reaches the two cards that
        // can never meld at all. No jokers here — with two of them spare, any orphan finds a
        // set, and the hand would simply win.
        var losing = Hands.Of(
            "2D", "3D", "4D", "5D", "6D", "7D", "8D", "9D", "10D", "JD", "QD",
            "5C", "8H");

        // And the two-deck shape: every diamond twice over, so the same run can be carved out
        // many ways, with one card that can never meld at the end of it.
        var doubled = Hands.Of(
            "2D", "2D", "3D", "3D", "4D", "4D", "5D", "5D", "6D", "6D", "7D", "7D", "5C");

        var clock = Stopwatch.StartNew();
        Assert.False(HandEvaluator.IsWinning(doubled, FiveHanded));
        Assert.True(HandEvaluator.IsWinning(winning, FiveHanded));
        Assert.False(HandEvaluator.IsWinning(losing, FiveHanded));
        clock.Stop();

        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(1),
            $"Evaluating three thirteen-card hands took {clock.ElapsedMilliseconds} ms.");
    }

    // ---- edges ----------------------------------------------------------------------------

    [Fact]
    public void AWholeSuitInSequenceIsWinning()
    {
        // The hand that hung the retired code forever, because its rank order was a cycle
        // (RULES.md §6.1). Thirteen ace-low hearts cover themselves several ways — as one
        // thirteen-card run, or as 3+3+3+4 — so the test asks only that every meld is a run.
        var hand = Hands.Of("AH", "2H", "3H", "4H", "5H", "6H", "7H", "8H", "9H", "10H", "JH",
                            "QH", "KH");

        Assert.True(HandEvaluator.TryFindCover(hand, FiveHanded, out var cover));
        AssertCoversExactly(cover, hand);
        Assert.All(cover, meld => Assert.Equal(MeldKind.Run, meld.Kind));
    }

    [Fact]
    public void AceDoesNotWrapSoKingAceTwoIsNotAMeld()
    {
        // RULES.md §6.1. The retired code allowed it — and hung on the hand above.
        Assert.False(HandEvaluator.IsWinning(Hands.Of("KD", "AD", "2D"), FiveHanded));
    }

    [Fact]
    public void TooFewCardsToMeldIsNotWinning()
    {
        Assert.False(HandEvaluator.IsWinning(Hands.Of("9H", "9S"), FiveHanded));
        Assert.False(HandEvaluator.IsWinning(Hands.Of("RJ", "BJ"), FiveHanded));
    }

    [Fact]
    public void AnEmptyHandIsCoveredByNoMeldsAtAll()
    {
        // The base case of the search, stated rather than stumbled over: there is nothing
        // left uncovered. The game only ever asks about thirteen cards.
        Assert.True(HandEvaluator.TryFindCover([], FiveHanded, out var cover));
        Assert.Empty(cover);
    }

    [Fact]
    public void TheSameCardInstanceTwiceIsRejected()
    {
        var card = Card.Ranked(new CardId(1), Rank.Nine, Suit.Hearts);

        Assert.Throws<ArgumentException>(() => HandEvaluator.IsWinning([card, card], FiveHanded));
    }

    [Fact]
    public void IsWinningAndTryFindCoverAlwaysAgree()
    {
        foreach (var hand in new[]
                 {
                     Hands.Of("2D", "3D", "4D"),
                     Hands.Of("2D", "3D", "5D"),
                     Hands.Of("9H", "9S", "9C", "9D", "9H"),
                     Hands.Of("RJ", "BJ", "RJ", "BJ"),
                 })
        {
            Assert.Equal(HandEvaluator.IsWinning(hand, FiveHanded), HandEvaluator.TryFindCover(hand, FiveHanded, out _));
        }
    }

    // ---- the win condition is a function of the table size (BUILD-PLAN §5 P25) -------------

    private static readonly TableRules TwoHanded = TableRules.For(2);
    private static readonly TableRules ThreeHanded = TableRules.For(3);
    private static readonly TableRules FourHanded = TableRules.For(4);

    [Fact]
    public void AHandThatCoversOnlyWithSetsWinsAtFiveAndNowhereElse()
    {
        // Four sets and no run anywhere: 9, K, 4 and 7 are nowhere near consecutive, and no
        // suit holds two cards. So every cover of these thirteen is four sets, and the number
        // of series it contains is nought whichever cover comes back.
        var hand = Hands.Of(
            "9H", "9S", "9C",
            "KH", "KS", "KC",
            "4H", "4S", "4C",
            "7H", "7S", "7C", "7D");

        Assert.True(HandEvaluator.IsWinning(hand, FiveHanded));
        Assert.False(HandEvaluator.IsWinning(hand, FourHanded));   // one series required
        Assert.False(HandEvaluator.IsWinning(hand, ThreeHanded));  // two
        Assert.False(HandEvaluator.IsWinning(hand, TwoHanded));    // sets are not melds at all
    }

    [Fact]
    public void ALongRunLaidDownSplitCountsAsTwoSeries()
    {
        // 🔥 The case that makes the requirement a property of the *partition* (RULES.md
        // §9 #23). Six consecutive diamonds cover themselves as one series or as two, and
        // three-handed only the second reading wins — so an evaluator that returns the first
        // cover it finds and audits it afterwards would call this hand a loser.
        var hand = Hands.Of(
            "2D", "3D", "4D", "5D", "6D", "7D",
            "9H", "9S", "9C",
            "KH", "KS", "KC", "KD");

        Assert.True(HandEvaluator.IsWinning(hand, ThreeHanded));

        Assert.True(HandEvaluator.TryFindCover(hand, ThreeHanded, out var cover));
        AssertCoversExactly(cover, hand);

        // The two sets are not series, so the two clean series can only be the diamonds cut
        // in half — 3 + 3 out of a run of six.
        var series = cover.Where(meld => meld.IsClean).ToList();
        Assert.Equal(2, series.Count);
        Assert.All(series, meld => Assert.Equal(3, meld.Count));

        // Four-handed the same thirteen need only one series, which the whole six-card run
        // supplies; two-handed the sets are illegal and nothing can be done with the kings.
        Assert.True(HandEvaluator.IsWinning(hand, FourHanded));
        Assert.True(HandEvaluator.IsWinning(hand, FiveHanded));
        Assert.False(HandEvaluator.IsWinning(hand, TwoHanded));
    }

    [Fact]
    public void TwoHandedAHandHoldingASetIsNotAWinHoweverItsRunsFall()
    {
        // RULES.md §9 #22: a set is illegal *as a meld*, not merely unnecessary. Two clean
        // runs and three kings — the runs fall perfectly and the hand still loses, because
        // the kings can only ever be a set.
        var hand = Hands.Of(
            "2D", "3D", "4D",
            "5H", "6H", "7H",
            "9S", "10S", "JS", "QS",
            "KC", "KH", "KD");

        Assert.False(HandEvaluator.IsWinning(hand, TwoHanded));

        // Everywhere else it is a win, and three-handed its two required clean series are the
        // two three-card runs.
        Assert.True(HandEvaluator.IsWinning(hand, ThreeHanded));
        Assert.True(HandEvaluator.IsWinning(hand, FourHanded));
        Assert.True(HandEvaluator.IsWinning(hand, FiveHanded));
    }

    [Fact]
    public void TwoHandedAHandOfRunsAloneIsAWinAndJokersDoNotMatterThere()
    {
        // The other side of §9 #22, and the case that shows purity is not a two-handed rule:
        // thirteen cards partitioning into runs alone, one of them carrying a joker, win.
        var hand = Hands.Of(
            "2D", "3D", "4D",
            "5H", "6H", "RJ",           // the joker plays the 7♥
            "9S", "10S", "JS", "QS",
            "4C", "5C", "6C");

        Assert.True(HandEvaluator.IsWinning(hand, TwoHanded));

        Assert.True(HandEvaluator.TryFindCover(hand, TwoHanded, out var cover));
        AssertCoversExactly(cover, hand);
        Assert.All(cover, meld => Assert.Equal(MeldKind.Run, meld.Kind));
    }

    [Fact]
    public void AHandWhoseOnlySeriesCarriesAJokerLosesWhereASeriesIsRequired()
    {
        // Purity, and it bites exactly where a series is required (RULES.md §7.1, §9 #28).
        // The only run these thirteen can make is 2♦ 3♦ + joker; put the joker in a set
        // instead and the two diamonds meld with nothing.
        var hand = Hands.Of(
            "2D", "3D", "RJ",
            "9H", "9S", "9C",
            "KH", "KS", "KC",
            "7H", "7S", "7C", "7D");

        Assert.True(HandEvaluator.IsWinning(hand, FiveHanded));
        Assert.False(HandEvaluator.IsWinning(hand, FourHanded));
        Assert.False(HandEvaluator.IsWinning(hand, ThreeHanded));
    }

    [Fact]
    public void AnAllJokerSeriesCanNeverDischargeARequiredOne()
    {
        // RULES.md §9 #29. Three jokers laid down together are a series — so a hand that
        // needs one *more* meld has it — and they are never a clean series, so at four seats
        // this hand has no required series at all and loses.
        var hand = Hands.Of(
            "RJ", "BJ", "RJ",
            "9H", "9S", "9C",
            "KH", "KS", "KC",
            "7H", "7S", "7C", "7D");

        Assert.True(HandEvaluator.IsWinning(hand, FiveHanded));
        Assert.False(HandEvaluator.IsWinning(hand, FourHanded));

        // And it really is a series while it is there: the same hand with a clean run in it
        // wins four-handed, with the jokers spending themselves on the surplus.
        var withACleanRun = Hands.Of(
            "RJ", "BJ", "RJ",
            "2D", "3D", "4D",
            "9H", "9S", "9C",
            "7H", "7S", "7C", "7D");

        Assert.True(HandEvaluator.IsWinning(withACleanRun, FourHanded));
    }

    [Fact]
    public void ASurplusSeriesNeedNotBeCleanWhereTheRequiredOnesAreCovered()
    {
        // RULES.md §9 #28, three-handed: three runs, two of them clean, one carrying a joker.
        var hand = Hands.Of(
            "2D", "3D", "4D",           // clean
            "5H", "6H", "7H",           // clean
            "9S", "10S", "RJ",          // the joker plays the J♠ — a surplus series
            "KC", "KH", "KD", "KS");    // and a set, which three-handed is allowed

        Assert.True(HandEvaluator.IsWinning(hand, ThreeHanded));
    }

    [Fact]
    public void ThreeHandedThirteenCardsWithOnlyOneCleanSeriesLose()
    {
        // One clean run, one impure one, and the rest sets: three-handed wants two clean and
        // has one.
        var hand = Hands.Of(
            "2D", "3D", "4D",           // clean
            "5H", "6H", "RJ",           // the joker plays the 7♥ — a series, not a clean one
            "9S", "9C", "9D",
            "KC", "KH", "KD", "KS");

        Assert.True(HandEvaluator.IsWinning(hand, FourHanded));
        Assert.False(HandEvaluator.IsWinning(hand, ThreeHanded));
    }

    [Fact]
    public void TheHarderQuestionIsStillFast()
    {
        // The same pathological shapes as EvaluatingThirteenCardsIsFast, asked at every table
        // size — where the search carries the counts along the partition and its memo is
        // keyed on what is still owing rather than on the covered set alone.
        var winning = Hands.Of(
            "2D", "3D", "4D", "5D", "6D", "7D", "8D", "9D", "10D", "RJ", "BJ", "RJ", "BJ");
        var losing = Hands.Of(
            "2D", "3D", "4D", "5D", "6D", "7D", "8D", "9D", "10D", "JD", "QD",
            "5C", "8H");
        var doubled = Hands.Of(
            "2D", "2D", "3D", "3D", "4D", "4D", "5D", "5D", "6D", "6D", "7D", "7D", "5C");

        var clock = Stopwatch.StartNew();

        foreach (var players in new[] { 2, 3, 4, 5, 6 })
        {
            var rules = TableRules.For(players);

            Assert.False(HandEvaluator.IsWinning(doubled, rules));
            Assert.True(HandEvaluator.IsWinning(winning, rules));
            Assert.False(HandEvaluator.IsWinning(losing, rules));
        }

        clock.Stop();

        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(5),
            $"Evaluating three hands at five table sizes took {clock.ElapsedMilliseconds} ms.");
    }

    // ---- helpers -----------------------------------------------------------------------------

    /// <summary>
    /// The definition of an exact cover: the melds are pairwise disjoint by
    /// <see cref="CardId"/>, and between them they consume the hand and nothing else.
    /// </summary>
    private static void AssertCoversExactly(IReadOnlyList<Meld> cover, IReadOnlyList<Card> hand)
    {
        for (var left = 0; left < cover.Count; left++)
        {
            for (var right = left + 1; right < cover.Count; right++)
            {
                Assert.False(cover[left].Overlaps(cover[right]),
                    $"{cover[left]} and {cover[right]} share a card.");
            }
        }

        var covered = cover.SelectMany(meld => meld.CardIds).ToList();
        Assert.Equal(hand.Count, covered.Count);
        Assert.True(covered.ToHashSet().SetEquals(hand.Select(card => card.Id)));
        Assert.All(cover, meld => Assert.True(meld.Count >= 3));
    }
}

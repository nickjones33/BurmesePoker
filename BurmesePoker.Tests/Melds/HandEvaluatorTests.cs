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
    // ---- the acceptance cases of BUILD-PLAN §5 P5 ----------------------------------------

    [Fact]
    public void AHandOfThreeThreeThreeAndFourIsWinning()
    {
        var hand = Hands.Of(
            "2D", "3D", "4D",        // run
            "9H", "9S", "9C",        // set
            "5S", "6S", "7S",        // run
            "KH", "KS", "KC", "KD"); // set of four

        Assert.True(HandEvaluator.IsWinning(hand));

        Assert.True(HandEvaluator.TryFindCover(hand, out var cover));
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
        Assert.False(HandEvaluator.IsWinning(hand));
        Assert.False(HandEvaluator.TryFindCover(hand, out var cover));
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

        Assert.False(HandEvaluator.IsWinning(hand));
        Assert.True(HandEvaluator.IsWinning(hand.Take(12).ToList()));
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

        Assert.True(HandEvaluator.TryFindCover(hand, out var cover));
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

        Assert.True(HandEvaluator.IsWinning(hand));

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

        Assert.True(HandEvaluator.TryFindCover(hand, out var cover));
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
        Assert.False(HandEvaluator.IsWinning(doubled));
        Assert.True(HandEvaluator.IsWinning(winning));
        Assert.False(HandEvaluator.IsWinning(losing));
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

        Assert.True(HandEvaluator.TryFindCover(hand, out var cover));
        AssertCoversExactly(cover, hand);
        Assert.All(cover, meld => Assert.Equal(MeldKind.Run, meld.Kind));
    }

    [Fact]
    public void AceDoesNotWrapSoKingAceTwoIsNotAMeld()
    {
        // RULES.md §6.1. The retired code allowed it — and hung on the hand above.
        Assert.False(HandEvaluator.IsWinning(Hands.Of("KD", "AD", "2D")));
    }

    [Fact]
    public void TooFewCardsToMeldIsNotWinning()
    {
        Assert.False(HandEvaluator.IsWinning(Hands.Of("9H", "9S")));
        Assert.False(HandEvaluator.IsWinning(Hands.Of("RJ", "BJ")));
    }

    [Fact]
    public void AnEmptyHandIsCoveredByNoMeldsAtAll()
    {
        // The base case of the search, stated rather than stumbled over: there is nothing
        // left uncovered. The game only ever asks about thirteen cards.
        Assert.True(HandEvaluator.TryFindCover([], out var cover));
        Assert.Empty(cover);
    }

    [Fact]
    public void TheSameCardInstanceTwiceIsRejected()
    {
        var card = Card.Ranked(new CardId(1), Rank.Nine, Suit.Hearts);

        Assert.Throws<ArgumentException>(() => HandEvaluator.IsWinning([card, card]));
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
            Assert.Equal(HandEvaluator.IsWinning(hand), HandEvaluator.TryFindCover(hand, out _));
        }
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

using System.Diagnostics;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;

using BurmesePoker.Tests;

namespace BurmesePoker.Tests.Melds;

/// <summary>
/// The scored partial cover (packet P10) — how close a hand is, which is what a bot asks on
/// every decision and what <see cref="HandEvaluator"/> deliberately will not answer.
/// </summary>
/// <remarks>
/// The one thing that must never break is agreement with the evaluator, which is the win
/// authority (BUILD-PLAN §3.4): a complete cover here and a winning hand there are the same
/// claim, and this type may not make it on a hand the evaluator rejects.
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class PartialCoverTests
{
    /// <summary>Thirteen cards that partition exactly: 2♥–7♥, 8♦9♦10♦, and three kings.</summary>
    private static readonly string[] WinningHand =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS"];

    [Fact]
    public void AWinningHandIsCoveredCompletely()
    {
        var hand = Hands.Of([.. WinningHand, "KD"]);
        var cover = PartialCover.Best(hand);

        Assert.True(cover.IsComplete);
        Assert.Equal(13, cover.CoveredCount);
        Assert.Empty(cover.Uncovered);
        Assert.True(HandEvaluator.IsWinning(hand, TableRules.For(5)));
    }

    [Fact]
    public void TheDeadwoodIsWhatNoMeldTook()
    {
        // Twelve cards that partition, and one that joins nothing: no other queen, and the
        // nearest club is the king four ranks away in a hand with no jokers to bridge it.
        var hand = Hands.Of([.. WinningHand, "QC"]);
        var cover = PartialCover.Best(hand);

        Assert.False(cover.IsComplete);
        Assert.Equal(12, cover.CoveredCount);
        Assert.True(Assert.Single(cover.Uncovered).SameValueAs(Hands.Value("QC")));
    }

    [Fact]
    public void TheMeldsAreDisjointAndAllComeFromTheHand()
    {
        var hand = Hands.Of("2D", "3D", "4D", "9H", "9S", "9C", "KH", "KS", "RJ", "5C", "8H", "2S", "JD");
        var cover = PartialCover.Best(hand);

        var used = cover.Melds.SelectMany(meld => meld.CardIds).ToList();

        Assert.Equal(used.Count, used.Distinct().Count());
        Assert.All(used, id => Assert.Contains(hand, card => card.Id == id));
        Assert.Equal(cover.CoveredCount, used.Count);
        Assert.Equal(hand.Count, cover.CoveredCount + cover.Uncovered.Count);
    }

    [Fact]
    public void NothingLeftOverIsCoveredByNoMeldsAtAll()
    {
        var cover = PartialCover.Best([]);

        Assert.Empty(cover.Melds);
        Assert.Empty(cover.Uncovered);
        Assert.True(cover.IsComplete);
        Assert.Equal(0, cover.CoveredCount);
    }

    [Fact]
    public void AHandThatMeldsNothingIsCoveredNotAtAll()
    {
        // Thirteen cards, no three of a rank, no three in sequence, no joker to bridge one.
        var cover = PartialCover.Best(
            Hands.Of("2H", "4S", "6C", "8D", "10H", "QS", "AC", "3D", "5H", "7S", "9C", "JD", "KH"));

        Assert.Equal(0, cover.CoveredCount);
        Assert.Empty(cover.Melds);
        Assert.Equal(13, cover.Uncovered.Count);
    }

    [Fact]
    public void ItFindsTheLargestCoverAndNotMerelyAGreedyOne()
    {
        // The trap is the 2♥, which two melds want. Taking the longest run first — 2♥ to 5♥,
        // four cards — strands the other two twos and scores four. Giving the 2♥ to the set
        // instead scores six: three twos and 3♥4♥5♥. Nothing else in the hand melds at all.
        var cover = PartialCover.Best(
            Hands.Of("2H", "3H", "4H", "5H", "2S", "2C", "8C", "10D", "QS", "AH", "7D", "JC", "9S"));

        Assert.Equal(6, cover.CoveredCount);
        Assert.Equal(2, cover.Melds.Count);
    }

    [Fact]
    public void ACompleteCoverIsExactlyWhatTheEvaluatorCallsWinning()
    {
        // The whole shoe dealt out into thirteen-card hands, over and over: the strongest
        // form of "never a claim of thirteen on a hand the evaluator rejects", because a
        // disagreement anywhere would be a bot declaring a hand the engine will not take.
        var random = new Random(20260818);
        var shoe = DeckBuilder.BuildTwoDecks().ToArray();

        for (var deal = 0; deal < 40; deal++)
        {
            random.Shuffle(shoe);

            for (var seat = 0; seat < 8; seat++)
            {
                var hand = shoe[(seat * 13)..((seat + 1) * 13)];
                var cover = PartialCover.Best(hand);

                // ⚠️ Five-handed, and that is the whole of the agreement since P25. A partial
                // cover is a count of cards, not a judgement: at three or four seats a
                // complete cover can still lose (RULES.md §7.1.1), which is why
                // PartialCover is not the win authority.
                Assert.Equal(HandEvaluator.IsWinning(hand, TableRules.For(5)), cover.IsComplete);
                Assert.InRange(cover.CoveredCount, 0, 13);
            }
        }
    }

    [Fact]
    public void ContrivedWinningHandsAreCoveredCompletely()
    {
        // Dealing at random never produces a winner, so the agreement above only ever sees
        // the "no" answer. These are the "yes".
        foreach (var hand in new[]
                 {
                     Hands.Of("AH", "2H", "3H", "4H", "5H", "6H", "7H", "8H", "9H", "10H", "JH", "QH", "KH"),
                     Hands.Of("2D", "3D", "4D", "5D", "6D", "7D", "8D", "9D", "10D", "RJ", "BJ", "RJ", "BJ"),
                     Hands.Of("9H", "9S", "9C", "9D", "KH", "KS", "RJ", "2D", "3D", "4D", "5C", "6C", "7C"),
                 })
        {
            var cover = PartialCover.Best(hand);

            Assert.True(cover.IsComplete, $"Left {cover.Uncovered.Count} uncovered.");
            Assert.True(HandEvaluator.IsWinning(hand, TableRules.For(5)));
        }
    }

    [Fact]
    public void ScoringThirteenCardsIsFast()
    {
        // A bot runs this up to fourteen times per decision, so the pathological shapes of
        // docs/spec/RUN-CANDIDATES.md matter far more here than they do to the evaluator —
        // and the "no" answers, which the evaluator gives up on early, are the ones this type
        // has to search to the end.
        var hands = new[]
        {
            Hands.Of("2D", "3D", "4D", "5D", "6D", "7D", "8D", "9D", "10D", "RJ", "BJ", "RJ", "BJ"),
            Hands.Of("2D", "3D", "4D", "5D", "6D", "7D", "8D", "9D", "10D", "JD", "QD", "5C", "8H"),
            Hands.Of("2D", "2D", "3D", "3D", "4D", "4D", "5D", "5D", "6D", "6D", "7D", "7D", "5C"),
        };

        var clock = Stopwatch.StartNew();
        var covered = hands.Sum(hand => PartialCover.Best(hand).CoveredCount);
        clock.Stop();

        Assert.Equal(13 + 11 + 12, covered);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(1),
            $"Scoring three thirteen-card hands took {clock.ElapsedMilliseconds} ms.");
    }

    [Fact]
    public void TheSameCardInstanceTwiceIsRejected()
    {
        var card = Card.Ranked(new CardId(1), Rank.Nine, Suit.Hearts);

        Assert.Throws<ArgumentException>(() => PartialCover.Best([card, card]));
    }
}

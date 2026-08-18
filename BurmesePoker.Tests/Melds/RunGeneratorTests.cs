using System.Diagnostics;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;

namespace BurmesePoker.Tests.Melds;

/// <summary>
/// Run candidate generation (packet P3). The acceptance criteria are the worked spec in
/// <c>docs/spec/RUN-CANDIDATES.md</c> §4 — in particular that the reference hand yields
/// <b>5</b> candidates and not the 8 the retired 2023 test asserted.
/// </summary>
public class RunGeneratorTests
{
    // ---- the reference hand: 2♦ 3♦ 4♦ + one red joker (spec §1) --------------------------

    [Fact]
    public void ReferenceHand_YieldsFiveCandidates_NotEight()
    {
        var hand = Hands.Of("2D", "3D", "4D", "RJ");

        var candidates = RunGenerator.Candidates(hand);

        // 8 is the number of joker *interpretations*; 5 is the number of distinct card sets,
        // and cover only ever cares about which cards a meld consumes (spec §2).
        Assert.Equal(5, candidates.Count);
    }

    [Fact]
    public void ReferenceHand_YieldsExactlyTheFiveCardSetsOfTheSpec()
    {
        var hand = Hands.Of("2D", "3D", "4D", "RJ");
        var (two, three, four, joker) = (hand[0], hand[1], hand[2], hand[3]);

        var candidates = RunGenerator.Candidates(hand);

        Assert.Collection(
            candidates.OrderBy(meld => meld.Count).ThenBy(Signature),
            meld => AssertConsumes(meld, two, three, four),   // S1
            meld => AssertConsumes(meld, two, three, joker),  // S2
            meld => AssertConsumes(meld, two, four, joker),   // S3
            meld => AssertConsumes(meld, three, four, joker), // S4
            meld => AssertConsumes(meld, two, three, four, joker)); // S5
    }

    [Fact]
    public void ReferenceHand_OffersTheJokerAsACardTheHandAlreadyHolds()
    {
        // S3 — the joker plays the 3♦ while the real 3♦ stays free for another meld. This is
        // the candidate that makes the exact-cover search able to accept hands that win
        // (spec §3); without it the whole feature is cosmetic.
        var hand = Hands.Of("2D", "3D", "4D", "RJ");

        var candidates = RunGenerator.Candidates(hand);

        var substitution = Assert.Single(candidates, meld => Consumes(meld, hand[0], hand[2], hand[3]));
        var jokerSlot = Assert.Single(substitution.Slots, slot => slot.IsSubstitute);
        Assert.Equal(Rank.Three, jokerSlot.PlaysAs);
        Assert.Equal(Suit.Diamonds, jokerSlot.InSuit);
    }

    // ---- runs without jokers -------------------------------------------------------------

    [Fact]
    public void FourCardRun_YieldsItsThreeSubRuns()
    {
        // Ports the one 2023 test that passed: candidates deliberately overlap, so every
        // sub-run of length three or more is its own candidate (BUILD-PLAN §3.4).
        var hand = Hands.Of("2D", "3D", "4D", "5D");
        var (two, three, four, five) = (hand[0], hand[1], hand[2], hand[3]);

        var candidates = RunGenerator.Candidates(hand);

        Assert.Equal(3, candidates.Count);
        Assert.Contains(candidates, meld => Consumes(meld, two, three, four));
        Assert.Contains(candidates, meld => Consumes(meld, three, four, five));
        Assert.Contains(candidates, meld => Consumes(meld, two, three, four, five));
    }

    [Fact]
    public void TwoCardsAreNeverARun()
    {
        Assert.Empty(RunGenerator.Candidates(Hands.Of("2D", "3D")));
    }

    [Fact]
    public void ARunIsNeverSpreadAcrossSuits()
    {
        Assert.Empty(RunGenerator.Candidates(Hands.Of("2D", "3H", "4S")));
    }

    [Fact]
    public void NonContiguousRanksAreNeverARun()
    {
        Assert.Empty(RunGenerator.Candidates(Hands.Of("2D", "4D", "6D")));
    }

    // ---- aces do not wrap (RULES.md §6.1) ------------------------------------------------

    [Fact]
    public void AceIsLowAtTheStartOfARun()
    {
        var hand = Hands.Of("AD", "2D", "3D");

        var run = Assert.Single(RunGenerator.Candidates(hand));

        AssertConsumes(run, hand[0], hand[1], hand[2]);
        Assert.Equal(new[] { Rank.Ace, Rank.Two, Rank.Three }, run.Slots.Select(slot => slot.PlaysAs));
    }

    [Fact]
    public void AceIsHighAtTheEndOfARun()
    {
        var hand = Hands.Of("QD", "KD", "AD");

        var run = Assert.Single(RunGenerator.Candidates(hand));

        AssertConsumes(run, hand[0], hand[1], hand[2]);
        Assert.Equal(new[] { Rank.Queen, Rank.King, Rank.Ace }, run.Slots.Select(slot => slot.PlaysAs));
    }

    [Fact]
    public void ARunNeverPassesThroughTheAce()
    {
        // K-A-2 is the case the retired code allowed, and the cause of the verified infinite
        // loop (RULES.md §6.1).
        var hand = Hands.Of("KD", "AD", "2D");

        Assert.Empty(RunGenerator.Candidates(hand));
    }

    [Fact]
    public void AceLowAndAceHighRunsCanBothComeOutOfOneSuit()
    {
        var hand = Hands.Of("AD", "2D", "3D", "QD", "KD");

        var candidates = RunGenerator.Candidates(hand);

        Assert.Equal(2, candidates.Count);
        Assert.Contains(candidates, meld => Consumes(meld, hand[0], hand[1], hand[2]));
        Assert.Contains(candidates, meld => Consumes(meld, hand[3], hand[4], hand[0]));
    }

    // ---- duplicate copies (defect D4) ----------------------------------------------------

    [Fact]
    public void DuplicateCopiesEachProduceTheirOwnCandidate()
    {
        // Two decks, so a hand can hold both 3♦s. The retired code took the first match and
        // lost the second candidate entirely.
        var hand = Hands.Of("2D", "3D", "3D", "4D");
        var (two, firstThree, secondThree, four) = (hand[0], hand[1], hand[2], hand[3]);

        var candidates = RunGenerator.Candidates(hand);

        Assert.Equal(2, candidates.Count);
        Assert.Contains(candidates, meld => Consumes(meld, two, firstThree, four));
        Assert.Contains(candidates, meld => Consumes(meld, two, secondThree, four));
    }

    [Fact]
    public void EachJokerInstanceIsItsOwnCandidate()
    {
        var hand = Hands.Of("2D", "3D", "RJ", "BJ");

        var candidates = RunGenerator.Candidates(hand);

        Assert.Contains(candidates, meld => Consumes(meld, hand[0], hand[1], hand[2]));
        Assert.Contains(candidates, meld => Consumes(meld, hand[0], hand[1], hand[3]));
    }

    // ---- termination and scale -----------------------------------------------------------

    [Fact]
    public void AllThirteenRanksOfOneSuitTerminates()
    {
        // Direct regression test for the verified infinite loop: the 2023 walk hangs on this
        // hand because its rank order wraps K-A-2.
        var hand = Hands.Of("AD", "2D", "3D", "4D", "5D", "6D", "7D",
                            "8D", "9D", "10D", "JD", "QD", "KD");

        var candidates = RunGenerator.Candidates(hand);

        // 11 ace-low windows (A-2-3 … A-2-…-K) plus 66 ascending ones (11 starting at the 2,
        // 10 at the 3, … 1 at the queen) is 77 windows, each satisfied exactly once because
        // every rank is held once and no joker is in hand. The count is 76, not 77: the
        // whole suit is one card set that reads two ways — A-2-…-K and 2-…-K-A — and
        // de-duplication by card set keeps it once (spec §2).
        Assert.Equal(76, candidates.Count);
        Assert.All(candidates, AssertIsAValidRun);
    }

    [Fact]
    public void AJokerHeavyHandStaysBounded()
    {
        // All four jokers plus nine consecutive cards of one suit — the worst case a 13-card
        // hand can reach. It yields 4,032 candidates: thousands, not the millions that
        // permuting joker *placements* rather than choosing joker *sets* would produce
        // (BUILD-PLAN §7). The count is inherent to the hand, not to the algorithm — the
        // brute-force cross-check below agrees on it card set for card set.
        var hand = Hands.Of("2D", "3D", "4D", "5D", "6D", "7D", "8D", "9D", "10D",
                            "RJ", "BJ", "RJ", "BJ");

        var stopwatch = Stopwatch.StartNew();
        var candidates = RunGenerator.Candidates(hand);
        stopwatch.Stop();

        Assert.Equal(4032, candidates.Count);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"Generation took {stopwatch.Elapsed}.");
        Assert.All(candidates, AssertIsAValidRun);
    }

    [Fact]
    public void AHandOfNothingButJokersStillTerminates()
    {
        var hand = Hands.Of("RJ", "BJ", "RJ", "BJ");

        var candidates = RunGenerator.Candidates(hand);

        // Four jokers make four three-card sets plus the one four-card set. Melds made
        // entirely of jokers are legal only because RULES.md §9 #8 recommends *unlimited*
        // jokers per meld; if that ever settles otherwise, this is the test to change.
        Assert.Equal(5, candidates.Count);
        Assert.All(candidates, AssertIsAValidRun);
    }

    // ---- invariants over every candidate ---------------------------------------------------

    [Fact]
    public void EveryCandidateIsAValidRunWithAUsableInterpretation()
    {
        var hand = Hands.Of("AD", "2D", "3D", "4D", "9H", "10H", "JH", "QH",
                            "5S", "6S", "7S", "RJ", "BJ");

        Assert.All(RunGenerator.Candidates(hand), AssertIsAValidRun);
    }

    [Fact]
    public void NoCandidateIsRepeatedByCardIdSet()
    {
        var hand = Hands.Of("2D", "3D", "4D", "5D", "3D", "RJ", "BJ");

        var candidates = RunGenerator.Candidates(hand);

        var seen = new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer());
        Assert.All(candidates, meld =>
            Assert.True(seen.Add([.. meld.CardIds]), $"{meld} was generated twice."));
    }

    [Fact]
    public void AnEmptyHandProducesNoCandidates()
    {
        Assert.Empty(RunGenerator.Candidates(Hands.Of()));
    }

    // ---- cross-check against an independent enumeration --------------------------------

    [Theory]
    // A joker substituting for a card the hand holds, plus a second suit to overlap with.
    [InlineData("2D", "3D", "4D", "RJ", "9H", "10H", "JH")]
    // Duplicate copies, two jokers, and a broken suit.
    [InlineData("2D", "3D", "3D", "5D", "RJ", "BJ", "KS", "AS", "QS")]
    // The worst case: nine consecutive cards of one suit and all four jokers.
    [InlineData("2D", "3D", "4D", "5D", "6D", "7D", "8D", "9D", "10D", "RJ", "BJ", "RJ", "BJ")]
    public void GeneratorFindsExactlyTheCardSetsThatCanFormARun(params string[] codes)
    {
        // The generator works forwards — window first, then fill. This works backwards —
        // every subset of the hand, asked whether some window could accept it. Agreement
        // means the generator neither misses a candidate nor invents one.
        var hand = Hands.Of(codes);

        var generated = new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer());
        foreach (var meld in RunGenerator.Candidates(hand))
        {
            generated.Add([.. meld.CardIds]);
        }

        var runnable = new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer());
        foreach (var subset in Subsets(hand))
        {
            if (CouldFormARun(subset))
            {
                runnable.Add([.. subset.Select(card => card.Id)]);
            }
        }

        Assert.Equal(runnable.Count, generated.Count);
        Assert.True(generated.SetEquals(runnable), "The two enumerations disagree.");
    }

    private static IEnumerable<List<Card>> Subsets(IReadOnlyList<Card> hand)
    {
        for (var mask = 0; mask < 1 << hand.Count; mask++)
        {
            var subset = new List<Card>();
            for (var index = 0; index < hand.Count; index++)
            {
                if ((mask & (1 << index)) != 0)
                {
                    subset.Add(hand[index]);
                }
            }

            yield return subset;
        }
    }

    /// <summary>
    /// Whether some contiguous same-suit window could hold exactly these cards, jokers
    /// filling whatever the real cards do not supply. Deliberately written subset-first, the
    /// opposite way round from <see cref="RunGenerator"/>.
    /// </summary>
    private static bool CouldFormARun(List<Card> subset)
    {
        if (subset.Count < RunGenerator.MinimumLength)
        {
            return false;
        }

        var real = subset.Where(card => !card.IsJoker).ToList();
        if (real.Select(card => card.Suit).Distinct().Count() > 1)
        {
            return false;
        }

        var ranks = real.Select(card => (int)card.Rank!.Value).ToList();
        if (ranks.Distinct().Count() != ranks.Count)
        {
            // A window's ranks are distinct, so two copies of one card cannot both sit in it.
            return false;
        }

        var length = subset.Count;

        // Ace low: the window covers 1..length, where 1 is the ace and can never recur.
        if (length <= 13 && ranks.All(rank => (rank == (int)Rank.Ace ? 1 : rank) <= length))
        {
            return true;
        }

        // Ace high or no ace: the window covers start..start+length-1 within 2..14.
        for (var start = (int)Rank.Two; start + length - 1 <= (int)Rank.Ace; start++)
        {
            if (ranks.All(rank => rank >= start && rank <= start + length - 1))
            {
                return true;
            }
        }

        return false;
    }

    // ---- helpers ---------------------------------------------------------------------------

    private static void AssertIsAValidRun(Meld meld)
    {
        Assert.Equal(MeldKind.Run, meld.Kind);
        Assert.True(meld.Count >= 3, "A run holds at least three cards.");
        Assert.Single(meld.Slots.Select(slot => slot.InSuit).Distinct());
        Assert.Equal(meld.Count, meld.CardIds.Count);

        for (var position = 0; position < meld.Count; position++)
        {
            var slot = meld.Slots[position];
            if (!slot.IsSubstitute)
            {
                // A real card always plays as itself.
                Assert.Equal(slot.PlaysAs, slot.Card.Rank);
                Assert.Equal(slot.InSuit, slot.Card.Suit);
            }

            if (position == 0)
            {
                continue;
            }

            // Strictly ascending by one, with the ace worth 1 only in the first position —
            // so no joker ever stands for a rank the run already supplies, and no run passes
            // through the ace (RULES.md §6.1).
            Assert.Equal(OrderingValue(meld.Slots[position - 1].PlaysAs, position - 1) + 1,
                         OrderingValue(slot.PlaysAs, position));
        }
    }

    private static int OrderingValue(Rank rank, int position) =>
        rank == Rank.Ace && position == 0 ? 1 : (int)rank;

    private static bool Consumes(Meld meld, params Card[] cards) =>
        meld.CardIds.SetEquals(cards.Select(card => card.Id));

    private static void AssertConsumes(Meld meld, params Card[] cards) =>
        Assert.True(Consumes(meld, cards),
            $"Expected [{string.Join(" ", cards.AsEnumerable())}] but the meld was {meld}.");

    private static string Signature(Meld meld) =>
        string.Join(",", meld.CardIds.Select(id => id.Value).Order());
}

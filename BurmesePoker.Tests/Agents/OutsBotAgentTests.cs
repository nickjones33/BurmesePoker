using System.Diagnostics;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// The rung that looks ahead (BUILD-PLAN P21) — and the three shortcuts that make it
/// affordable, each asserted against the search it stands in for.
/// </summary>
/// <remarks>
/// <para>
/// <b>How well it plays is not here.</b> That is a measurement over thousands of games with an
/// interval on it, published in <c>docs/STRATEGY.md</c> from <c>docs/strategy/measurements.csv</c>.
/// What belongs in a test is what no run of any size could settle: that the count of outs is
/// the count the definition asks for, and that <em>none of the three things done for speed
/// changed an answer</em>.
/// </para>
/// <para>
/// 🔥 <b>That last claim is the packet.</b> P21 spends itself on cost — a prune that skips a
/// value, a search that stops at a bar instead of finding a maximum, and a probe that reuses
/// one hand's melds across thirty-four questions. Each is a shortcut past a search that already
/// works, so each is asserted against that search over real hands rather than reasoned about in
/// a comment. A rung that is fast and wrong measures nothing.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class OutsBotAgentTests
{
    /// <summary>Twelve that partition, and the Q♣, which joins nothing — the P10 fixture.</summary>
    private static readonly string[] TwelveAndADeadQueen =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QC"];

    [Fact]
    public void TheYesOrNoSearchAgreesWithTheFullOne()
    {
        // ✅ P21 acceptance 3, at the bottom of the stack. PartialCover.CoversAtLeast stops at
        // the first arrangement that clears the bar and abandons branches that cannot reach
        // it; PartialCover.Best does neither and is what everything published so far was
        // measured with. Every bar from "none of them" to "one more than there are cards".
        foreach (var hand in RandomHands(60, cards: 13))
        {
            var best = PartialCover.Best(hand).CoveredCount;

            for (var bar = 0; bar <= hand.Count + 1; bar++)
            {
                Assert.Equal(best >= bar, PartialCover.CoversAtLeast(hand, bar));
            }
        }
    }

    [Fact]
    public void TheProbeAgreesWithTheFullSearch()
    {
        // ✅ P21 acceptance 3, and the shortcut with the most to go wrong: one CoverProbe is
        // built per candidate discard and then asked about value after value, filing each
        // one's melds into the shared index and taking them out again. A mask left behind
        // would be a card melding with a card that is not in the hand — and it would show up
        // as a *later* probe answering yes, which is why the same probe is reused below
        // rather than rebuilt.
        foreach (var kept in RandomHands(25, cards: 13))
        {
            var probe = new CoverProbe(kept);

            foreach (var drawn in EveryValue())
            {
                var full = PartialCover.Best([.. kept, drawn]).CoveredCount;

                for (var bar = full - 1; bar <= full + 1; bar++)
                {
                    Assert.Equal(full >= bar, probe.CoversAtLeastWith(drawn, bar));
                }
            }
        }
    }

    [Fact]
    public void ThePruneNeverThrowsAwayARealOut()
    {
        // ✅ P21's second shortcut, and the one the packet said to verify rather than assume:
        // a value with fewer than two cards in the hand that could sit beside it is never
        // searched at all. This counts the outs the long way — every value the shoe still
        // holds, through PartialCover.Best — and demands the same number.
        foreach (var kept in RandomHands(20, cards: 13))
        {
            var covered = PartialCover.Best(kept).CoveredCount;

            Assert.Equal(OutsTheLongWay(kept, covered), LiveOuts.Count(kept, covered));
        }
    }

    [Fact]
    public void TheCacheAnswersExactlyWhatTheSearchWouldHave()
    {
        // ✅ P21's third shortcut. The cache keys on the *values* of the cards it was asked
        // about and on the bar, never on a CardId — which is what lets it outlive a deal, and
        // what would make it wrong if a covered count depended on anything else.
        var cache = new OutsCache();

        foreach (var kept in RandomHands(15, cards: 13))
        {
            var covered = PartialCover.Best(kept).CoveredCount;

            Assert.Equal(LiveOuts.Count(kept, covered), LiveOuts.Count(kept, covered, cache));

            // And again, now that every answer is one it has already bought.
            Assert.Equal(LiveOuts.Count(kept, covered), LiveOuts.Count(kept, covered, cache));
        }

        Assert.InRange(cache.Count, 1, OutsCache.Capacity);
    }

    [Fact]
    public void TheCacheIsBounded()
    {
        // An agent lives for one seat of one game and a game may be any number of rounds, so
        // an unbounded table would cost the harness more memory than the searches it saves.
        var cache = new OutsCache();
        var thrown = 0;

        foreach (var kept in RandomHands(40, cards: 13, seed: 21))
        {
            var before = cache.Count;

            LiveOuts.Count(kept, PartialCover.Best(kept).CoveredCount, cache);

            if (cache.Count < before)
            {
                thrown++;
            }

            Assert.InRange(cache.Count, 0, OutsCache.Capacity);
        }

        // Forty sweeps is far short of the capacity, so nothing should have been thrown away
        // yet — a cache that emptied itself constantly would pass the bound and buy nothing.
        Assert.Equal(0, thrown);
    }

    [Fact]
    public void TheCountIsOfValuesTheHandCouldStillDraw()
    {
        // The definition, on a hand it can be read off by hand. Twelve cards partition into
        // 2♥-7♥, 8♦9♦10♦ and three kings; the Q♣ joins nothing, so twelve of thirteen meld and
        // the hand is one card from winning. Eight values would finish it — and it is worth
        // naming all eight, because two of them are the ones a person would miss.
        var kept = Hands.Of(TwelveAndADeadQueen);

        Assert.Equal(12, PartialCover.Best(kept).CoveredCount);
        Assert.Equal(8, LiveOuts.Count(kept, 12));

        // The fourth king finishes the set; the 8♥ and the ace extend the heart run at either
        // end; the 7♦ and the J♦ extend the diamond one; and a joker does any of it.
        Assert.All(
            new[] { "KD", "8H", "AH", "7D", "JD", "RJ" },
            code => Assert.Equal(13, PartialCover.Best([.. kept, Hands.Value(code)]).CoveredCount));

        // 🔥 And the two nobody sees coming: a *second* 4♥ or 5♥ splits the six-card heart run
        // into two legal runs, of three and four, so seven hearts meld where six did. That is
        // what P15 meant by combinatorial — no count of a card's partners says it.
        Assert.All(
            new[] { "4H", "5H" },
            code => Assert.Equal(13, PartialCover.Best([.. kept, Hands.Value(code)]).CoveredCount));
    }

    [Fact]
    public void AnOutsBotDiffersFromAGreedyOneOnlyWhereGreedyWasGuessing()
    {
        // The rungs are comparable only if each differs from the one below it in one thing
        // (BUILD-PLAN P15), and this is that one thing. Five of these fourteen cards leave
        // nine melded and none leaves more, so greedy's first key ties five ways; three of
        // those five — the 6♣, the 2♦ and the 3♣ — have no partner anywhere in the hand, so
        // greedy's own tie-break ties as well and it throws whichever it reached first.
        var hand = Hands.Of("6C", "QC", "RJ", "QS", "KD", "2D", "10S", "8D", "KC", "QD", "3C", "AS", "9D", "KS");

        var greedy = new GreedyBotAgent().ChooseDiscard(Turn(hand));
        var outs = new OutsBotAgent().ChooseDiscard(Turn(hand));

        Assert.True(greedy.SameValueAs(Hands.Value("6C")));
        Assert.True(outs.SameValueAs(Hands.Value("2D")));

        Assert.Equal(9, Covered(hand, greedy));
        Assert.Equal(9, Covered(hand, outs));

        // This rung fills that arbitrary place with a count of what the hand would still be
        // waiting for: letting the 2♦ go leaves nine values that would improve what is left,
        // and letting the 6♣ go leaves seven.
        Assert.Equal(7, LiveOuts.Count(Kept(hand, Hands.Value("6C")), 9));
        Assert.Equal(9, LiveOuts.Count(Kept(hand, Hands.Value("2D")), 9));

        // ⚠️ The new key sits *above* greedy's and not below it, which is where this rung
        // parts company with cautious and counting — those two decide only what greedy had
        // already given up on, and neither measured anything (docs/STRATEGY.md §7, §8). The
        // 10♠ and the A♠ leave nine outs as well and lose to the 2♦ on greedy's key, which is
        // what "ties fall back to greedy" means.
        Assert.Equal(9, LiveOuts.Count(Kept(hand, Hands.Value("10S")), 9));
        Assert.Equal(9, LiveOuts.Count(Kept(hand, Hands.Value("AS")), 9));
    }

    [Fact]
    public void TheRungIsAPureFunctionOfWhatItIsHolding()
    {
        // 🔥 The one hazard a cache introduces. This rung remembers answers between decisions,
        // so a seat that has been playing for a while must still answer a question exactly as
        // a seat seeing it for the first time would — otherwise a run would depend on the
        // order its games were scheduled in, which is the determinism P12 stands on
        // (BUILD-PLAN §3.7).
        var fresh = new OutsBotAgent();
        var used = new OutsBotAgent();

        foreach (var kept in RandomHands(6, cards: 14, seed: 909))
        {
            _ = used.ChooseDiscard(Turn(kept));
        }

        foreach (var kept in RandomHands(6, cards: 14, seed: 31))
        {
            Assert.Equal(fresh.ChooseDiscard(Turn(kept)), used.ChooseDiscard(Turn(kept)));
        }
    }

    [Fact]
    public void TheRungStaysInsideItsThroughputBudget()
    {
        // ✅ P21 acceptance 2, as the coarsest possible guard. The measured figure is in
        // docs/STRATEGY.md and comes from `sim bench`, which times whole rounds; what a test
        // can say is only that a decision has not become something nobody could afford to
        // run — the packet's budget is ten greedy rounds, and this allows far more slack than
        // that because a test machine is a busy machine.
        var hands = RandomHands(8, cards: 14, seed: 77).ToList();
        var agent = new OutsBotAgent();

        _ = agent.ChooseDiscard(Turn(hands[0]));

        var clock = Stopwatch.StartNew();
        var thrown = hands.Select(hand => agent.ChooseDiscard(Turn(hand))).ToList();
        clock.Stop();

        Assert.Equal(hands.Count, thrown.Count);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(4),
            $"Eight outs decisions took {clock.ElapsedMilliseconds} ms.");
    }

    /// <summary>The outs of a hand, counted without a prune, a bar or a cache.</summary>
    private static int OutsTheLongWay(IReadOnlyList<Card> kept, int covered)
    {
        var outs = 0;

        foreach (var drawn in EveryValue())
        {
            var copies = kept.Count(held => held.SameValueAs(drawn));

            // A value the hand already holds both copies of is not out there to be drawn —
            // and the jokers are four cards but one value, so they are counted as one.
            var gone = drawn.IsJoker
                ? kept.Count(held => held.IsJoker) >= 4
                : copies >= 2;

            if (!gone && PartialCover.Best([.. kept, drawn]).CoveredCount > covered)
            {
                outs++;
            }
        }

        return outs;
    }

    /// <summary>
    /// Every value that can be drawn: the 52 ranked ones and the joker, which is one value
    /// however many physical jokers the shoe holds.
    /// </summary>
    private static IEnumerable<Card> EveryValue()
    {
        yield return Card.Joker(new CardId(1_000), CardColor.Red);

        foreach (var suit in CardText.AllSuits)
        {
            foreach (var rank in CardText.AllRanks)
            {
                yield return Card.Ranked(new CardId(1_000), rank, suit);
            }
        }
    }

    /// <summary>Hands dealt off a shuffled shoe, so the shapes are the ones play produces.</summary>
    private static IEnumerable<IReadOnlyList<Card>> RandomHands(int count, int cards, int seed = 20260820)
    {
        var random = new Random(seed);

        for (var hand = 0; hand < count; hand++)
        {
            var deck = Deck.TwoDecks();
            deck.Shuffle(random);

            yield return [.. deck.Cards.Take(cards)];
        }
    }

    /// <summary>
    /// ✅ <b>P24.2 — the explained ranking <em>is</em> the ranking, and the discard is its head.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>The claim is that an explanation is free, and this is what makes it true</b>: the keys
    /// are kept rather than recomputed, so a front end drawing the arrow has already paid for every
    /// number in the sentence. Two orderings that had to agree by inspection would be exactly the
    /// drift <c>CoverScore</c> exists to prevent.
    /// </remarks>
    [Fact]
    public void TheExplainedOrderingIsTheOrderingAndTheDiscardIsItsHead()
    {
        var rung = new OutsBotAgent();
        var checkedHands = 0;

        foreach (var hand in RandomHands(count: 40, cards: 14, seed: 20260822))
        {
            var context = Turn(hand);
            var explained = rung.ExplainDiscards(context);

            Assert.Equal(rung.RankDiscards(context).Select(card => card.Id), explained.Select(card => card.Card.Id));
            Assert.Equal(rung.ChooseDiscard(context).Id, explained[0].Card.Id);
            Assert.All(explained, candidate => Assert.Equal(rung.DiscardKeys.Count, candidate.Keys.Count));

            // ⚠️ The expensive middle key is asked only of what is tied at the top, and a null is
            // not a zero: reading one back as a measurement would report a number nobody took.
            var best = explained[0].Keys[0]!.Value.Value;
            Assert.All(
                explained.Where(candidate => candidate.Keys[0]!.Value.Value < best),
                candidate => Assert.Null(candidate.Keys[1]));

            checkedHands++;
        }

        Assert.Equal(40, checkedHands);
    }

    /// <summary>The hand less the first card of that value.</summary>
    private static IReadOnlyList<Card> Kept(IReadOnlyList<Card> hand, Card value)
    {
        var thrown = hand.First(held => held.SameValueAs(value));

        return [.. hand.Where(held => held != thrown)];
    }

    /// <summary>How many of the thirteen kept would meld, had that card been thrown.</summary>
    private static int Covered(IReadOnlyList<Card> hand, Card thrown) =>
        PartialCover.Best([.. hand.Where(held => held != thrown)]).CoveredCount;

    /// <summary>A turn context holding exactly these cards, and asking only what to throw.</summary>
    private static TurnContext Turn(IReadOnlyList<Card> hand) => TurnContexts.Holding(hand);
}
